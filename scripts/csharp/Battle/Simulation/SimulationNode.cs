using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Transport;
using Fateforged.Session;
using Fateforged.Cards;
using Fateforged.Units;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;

namespace Fateforged.Simulation;

/// <summary>
/// Scene-tree bridge for the session/simulation stack.
/// Owns match state initialization and delegates runtime behavior to IGameSession.
///
/// Runs Tick() in _PhysicsProcess() with ProcessPriority = -100
/// so it executes before visual nodes.
///
/// Singleton via SimulationNode.Current.
/// </summary>
[GlobalClass]
public partial class SimulationNode : Node, IGameSession
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static SimulationNode? Current { get; private set; }

    // =========================================================================
    // STATE
    // =========================================================================

    public MatchState State { get; private set; } = new();
    private Simulation? _simulation;
    private CommandRouter _commandRouter = new();
    private IGameSession? _session;
    private bool _initialized;
    private bool _firstSnapshotReceived;

    public const float FIXED_DELTA = global::Fateforged.Simulation.Simulation.FixedDeltaSeconds;
    private const int SimulationProcessPriority = -100;
    private const int DefaultAiDifficulty = 3;
    private const float DefaultAiIntervalMin = 3.0f;
    private const float DefaultAiIntervalMax = 6.0f;

    private float _accumulator;

    private int _localPlayerIndex;
    public int LocalPlayerIndex
    {
        get => _localPlayerIndex;
        set
        {
            _localPlayerIndex = Mathf.Clamp(value, 0, 1);
            LocalPlayer.Initialize(_localPlayerIndex);
        }
    }

    public bool IsHost
    {
        get => LocalPlayerIndex == 0;
        set => LocalPlayerIndex = value ? 0 : 1;
    }

    // =========================================================================
    // TEAM / COORDINATE TRANSFORMS
    // =========================================================================

    public int RemapTeam(int team)
    {
        if (LocalPlayerIndex == 0) return team;
        return MatchState.GetEnemyTeam(team);
    }

    private int ToNetworkTeam(int localTeam) => RemapTeam(localTeam);

    private Vector3 ToCanonical(Vector3 localPos) => CoordinateTransform.LocalToCanonical(localPos);

    /// <summary>
    /// Convert a SimVector3 to local Godot.Vector3 (public, for visual layers).
    /// </summary>
    public Vector3 SimToLocal(SimVector3 simPos) =>
        CoordinateTransform.CanonicalToLocal(new Vector3(simPos.X, simPos.Y, simPos.Z));

    private SimVector3 ToSimCanonical(Vector3 localPos)
    {
        var c = CoordinateTransform.LocalToCanonical(localPos);
        return new SimVector3(c.X, c.Y, c.Z);
    }

    // =========================================================================
    // IGameSession IMPLEMENTATION
    // =========================================================================

    public MatchState GetState() => _session?.GetState() ?? State;

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public void SubmitCommand(ICommand cmd)
    {
        if (_session != null)
        {
            _session.SubmitCommand(cmd);
            return;
        }

        GD.PrintErr("[SimulationNode] SubmitCommand called before initialization — command rejected");
    }

    public void Tick(float delta)
    {
        _session?.Tick(delta);
    }

    // =========================================================================
    // SIGNALS (minimal — only those still awaited by BattleScene)
    // =========================================================================

    [Signal] public delegate void FirstSnapshotAppliedEventHandler();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        ProcessPriority = SimulationProcessPriority;
        ProcessMode = ProcessModeEnum.Always;
        Current = this;
        AddToGroup("simulation_node");
    }

    public override void _ExitTree()
    {
        ClearSession();
        if (Current == this)
            Current = null;
    }

    private bool _invariantsChecked;

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized || _session == null)
            return;

#if DEBUG
        if (!_invariantsChecked)
        {
            _invariantsChecked = true;
            var violations = MatchStateInvariants.ValidatePostInit(GetState());
            foreach (var v in violations)
                GD.PrintErr($"[SimulationNode] Post-init invariant violation: {v}");
        }
#endif

        if (IsHost)
        {
            _accumulator += (float)delta;
            while (_accumulator >= FIXED_DELTA)
            {
                _session.Tick(FIXED_DELTA);
                _accumulator -= FIXED_DELTA;
            }
            return;
        }

        // Client sessions don't run deterministic simulation ticks.
        _session.Tick((float)delta);
    }

    /// <summary>
    /// Whether the active network session is currently waiting for reconnection.
    /// </summary>
    public bool IsReconnecting()
    {
        return _session is NetworkSession network && network.IsAwaitingReconnect;
    }

    /// <summary>
    /// Remaining reconnect grace time in seconds.
    /// </summary>
    public float GetReconnectRemainingSeconds()
    {
        return _session is NetworkSession network ? network.ReconnectRemainingSeconds : 0f;
    }

    /// <summary>
    /// Human-readable reconnect reason from the network session.
    /// </summary>
    public string GetReconnectReason()
    {
        return _session is NetworkSession network ? network.ReconnectReason : "";
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public void Initialize(float prepDuration, float matchDuration, WinConditionType winCondition,
        float winConditionTimeLimit = 0f, int winConditionKillTarget = 0, long seed = 0)
    {
        if (seed == 0)
        {
            seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            GD.PushWarning("[SimulationNode] No explicit seed provided — falling back to wall-clock time. This is non-deterministic and will cause desync in multiplayer.");
        }

        State = new MatchState
        {
            PrepTimeRemaining = prepDuration,
            WinCondition = winCondition,
            WinConditionTimeLimit = winConditionTimeLimit > 0 ? winConditionTimeLimit : matchDuration,
            WinConditionKillTarget = winConditionKillTarget,
            Phase = GamePhase.Preparation,
            Rng = new DeterministicRng(seed)
        };

        Simulation.Log = msg => GD.Print(msg);
        _simulation = new Simulation(State);
        _commandRouter = new CommandRouter();

        SetSession(new LocalSession(_simulation, _commandRouter, State));
        _initialized = true;

        GD.Print($"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, timeLimit={State.WinConditionTimeLimit}s, killTarget={winConditionKillTarget}, seed={seed})");
    }

    /// <summary>
    /// Replace LocalSession with host/client session for multiplayer battles.
    /// Called by BattleScene after transport setup.
    /// </summary>
    public void ConfigureMultiplayerSession(IMatchTransport transport, bool isHost)
    {
        if (!_initialized || _simulation == null)
        {
            GD.PrintErr("[SimulationNode] ConfigureMultiplayerSession called before Initialize");
            return;
        }

        IsHost = isHost;

        IGameSession session = isHost
            ? new HostSession(_simulation, _commandRouter, State, transport)
            : new ClientSession(State, transport, LocalPlayerIndex);

        SetSession(session);
    }

    /// <summary>
    /// Send an authoritative match-ended message to remote peers (host only).
    /// </summary>
    public void BroadcastMatchEnded(int localWinnerTeam, string reason = "MatchEnded")
    {
        if (_session is not HostSession hostSession)
            return;

        hostSession.BroadcastMatchEnd(ToNetworkTeam(localWinnerTeam), reason, State.MatchTime);
    }

    private void SetSession(IGameSession session)
    {
        if (_session != null)
        {
            _session.SimEventsEmitted -= OnSessionSimEvents;
            if (_session is ClientSession oldClientSession)
                oldClientSession.FirstSnapshotApplied -= OnClientFirstSnapshotApplied;
            if (_session is IDisposable disposable)
                disposable.Dispose();
        }

        _session = session;
        _session.SimEventsEmitted += OnSessionSimEvents;

        _firstSnapshotReceived = false;
        if (_session is ClientSession clientSession)
            clientSession.FirstSnapshotApplied += OnClientFirstSnapshotApplied;

        _accumulator = 0f;
    }

    private void ClearSession()
    {
        if (_session == null)
            return;

        _session.SimEventsEmitted -= OnSessionSimEvents;
        if (_session is ClientSession clientSession)
            clientSession.FirstSnapshotApplied -= OnClientFirstSnapshotApplied;
        if (_session is IDisposable disposable)
            disposable.Dispose();
        _session = null;
    }

    private void OnSessionSimEvents(IReadOnlyList<SimEvent> events)
    {
        SimEventsEmitted?.Invoke(events);
    }

    private void OnClientFirstSnapshotApplied()
    {
        if (_firstSnapshotReceived)
            return;

        _firstSnapshotReceived = true;
        EmitSignal(SignalName.FirstSnapshotApplied);
    }

    // =========================================================================
    // SUMMONER REGISTRATION (BattleScene calls these during init)
    // =========================================================================

    public void RegisterSummoner(int team, float hp, float maxHp, float mana, float maxMana, float castSpeed, string[] deckCatalogIds, int maxHandSize, Vector3 position)
    {
        int networkTeam = ToNetworkTeam(team);
        position = ToCanonical(position);

        if (networkTeam < 0 || networkTeam > 1)
        {
            GD.PrintErr($"[SimulationNode] Invalid team {networkTeam} for RegisterSummoner");
            return;
        }

        var summoner = State.Summoners[networkTeam];
        summoner.Team = (Team)networkTeam;
        summoner.CurrentHp = hp;
        summoner.MaxHp = maxHp;
        summoner.Mana = mana;
        summoner.MaxMana = maxMana;
        summoner.CastSpeed = castSpeed;
        summoner.IsAlive = true;
        summoner.MaxHandSize = maxHandSize;
        summoner.Position = new SimVector3(position.X, position.Y, position.Z);

        summoner.Deck.Clear();
        summoner.Deck.AddRange(deckCatalogIds);

        GD.Print($"[SimulationNode] Registered summoner team={networkTeam} (local={team}): HP={maxHp}, Mana={maxMana}, CastSpeed={castSpeed}, Deck={deckCatalogIds.Length} cards, Position={position}");
    }

    public void SetSummonerHand(int team, string[] handCatalogIds)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.Hand.Clear();
        summoner.Hand.AddRange(handCatalogIds);
    }

    // =========================================================================
    // CARD DATA (BattleScene calls during init)
    // =========================================================================

    public void PopulateCardData()
    {
        State.CardDataMap.Clear();
        var processed = new HashSet<string>();

        foreach (var summoner in State.Summoners)
        {
            foreach (var catalogId in summoner.Deck)
                PopulateSingleCard(catalogId, processed);
            foreach (var catalogId in summoner.Hand)
                PopulateSingleCard(catalogId, processed);
            foreach (var catalogId in summoner.DiscardPile)
                PopulateSingleCard(catalogId, processed);
        }

        GD.Print($"[SimulationNode] Populated CardDataMap with {State.CardDataMap.Count} cards");
    }

    private void PopulateSingleCard(string catalogId, HashSet<string> processed)
    {
        if (string.IsNullOrEmpty(catalogId) || !processed.Add(catalogId))
            return;

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PrintErr($"[SimulationNode] Card not found in catalog: {catalogId}");
            return;
        }

        var simCard = SimCardData.FromCardDefinition(card);

        if (card.Type == CardType.Summon)
        {
            if (card.Summon != null)
            {
                foreach (var entry in card.Summon.Units)
                    simCard.UnitTemplates.Add(UnitDefinitions.BuildSimTemplate(entry.UnitId, entry.Count, entry.Modifier));
            }
            else if (card.UnitId.HasValue)
            {
                simCard.UnitTemplates.Add(UnitDefinitions.BuildSimTemplate(card.UnitId, card.SpawnCount, card.UnitModifier));
            }
        }

        State.CardDataMap[catalogId] = simCard;
    }

    // =========================================================================
    // AI CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Configure AI for a summoner. Called by BattleScene during init.
    /// </summary>
    public void ConfigureAi(int team, AiType aiType, AiPersonality personality = AiPersonality.Balanced,
        int difficulty = DefaultAiDifficulty, float intervalMin = DefaultAiIntervalMin, float intervalMax = DefaultAiIntervalMax,
        Godot.Collections.Array? scriptSteps = null)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];

        if (aiType == AiType.None)
        {
            summoner.Ai = null;
            return;
        }

        var config = new AiConfig
        {
            Type = aiType,
            Personality = personality,
            Difficulty = difficulty,
            PlayIntervalMin = intervalMin,
            PlayIntervalMax = intervalMax
        };

        // Parse scripted steps
        if (aiType == AiType.Scripted && scriptSteps != null && scriptSteps.Count > 0)
        {
            var steps = new ScriptedAiStep[scriptSteps.Count];
            for (int i = 0; i < scriptSteps.Count; i++)
            {
                var stepDict = scriptSteps[i].AsGodotDictionary();
                float triggerTime = (float)stepDict.GetValueOrDefault("delay", 0.0f);
                string cardId = stepDict.GetValueOrDefault("card_name", "").ToString();

                SimVector3 spawnPos = SimVector3.Zero;
                var posVar = stepDict.GetValueOrDefault("position", default);
                if (posVar.VariantType == Variant.Type.Dictionary)
                {
                    var posDict = posVar.AsGodotDictionary();
                    float px = (float)posDict.GetValueOrDefault("x", 0.0f);
                    float pz = (float)posDict.GetValueOrDefault("y", 0.0f);
                    spawnPos = ToSimCanonical(new Godot.Vector3(px, 0f, pz));
                }
                else if (posVar.VariantType == Variant.Type.Vector2)
                {
                    var v2 = posVar.AsVector2();
                    spawnPos = ToSimCanonical(new Godot.Vector3(v2.X, 0f, v2.Y));
                }

                steps[i] = new ScriptedAiStep(triggerTime, cardId, spawnPos);
            }
            config.Script = steps;
        }

        summoner.Ai = config;
        SimAi.InitializeTimer(State, summoner);

        GD.Print($"[SimulationNode] Configured AI: team={networkTeam} type={aiType} personality={personality} difficulty={difficulty} interval=[{intervalMin},{intervalMax}]");
    }

    // =========================================================================
    // GDSCRIPT-CALLABLE ACCESSORS (BattleScene polls these)
    // =========================================================================

    public int GetPhase() => (int)GetState().Phase;
    public float GetPrepTimeRemaining() => GetState().PrepTimeRemaining;
    public float GetMatchTime() => GetState().MatchTime;
    public int GetWinnerTeam() => GetState().WinnerTeam ?? -1;

    public void SkipPreparation()
    {
        var state = GetState();
        if (state.Phase == GamePhase.Preparation)
            state.PrepTimeRemaining = 0f;
    }

    // =========================================================================
    // COMMAND QUEUE
    // =========================================================================

    public void QueuePlayCard(int team, int cardIndex, Vector3 spawnPosition, int networkId = -1)
    {
        var cmd = new PlayCardCommand(ToNetworkTeam(team), cardIndex, ToSimCanonical(spawnPosition), networkId);
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Queue a direct unit spawn (no mana, no casting, no hand management).
    /// Used by debug arena, event sequencer, scripted AI, tutorials.
    /// </summary>
    public void QueueSpawnUnit(string catalogId, int team, Vector3 position,
        bool activateImmediately = true, Godot.Collections.Dictionary? statOverrides = null)
    {
        EnsureCardDataPopulated(catalogId);

        var cmd = new SpawnUnitCommand(catalogId, ToNetworkTeam(team), ToSimCanonical(position))
        {
            ActivateImmediately = activateImmediately,
            StatOverrides = ConvertStatOverrides(statOverrides)
        };
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Ensure a single card's data is in CardDataMap.
    /// Called by QueueSpawnUnit for cards that may not be in any summoner's deck.
    /// </summary>
    public void EnsureCardDataPopulated(string catalogId)
    {
        if (State.CardDataMap.ContainsKey(catalogId))
            return;

        var processed = new HashSet<string>(State.CardDataMap.Keys);
        PopulateSingleCard(catalogId, processed);
    }

    private static System.Collections.Generic.Dictionary<Stats.StatKey, float>? ConvertStatOverrides(
        Godot.Collections.Dictionary? gdDict)
    {
        if (gdDict == null || gdDict.Count == 0)
            return null;

        var result = new System.Collections.Generic.Dictionary<Stats.StatKey, float>();
        foreach (var key in gdDict.Keys)
        {
            var keyStr = key.AsString();
            var parsed = Stats.StatKeyExtensions.FromString(keyStr);
            if (parsed == null)
            {
                GD.PushWarning($"[SimulationNode] Unknown stat override key: '{keyStr}'");
                continue;
            }
            var val = gdDict[key];
            if (val.VariantType == Variant.Type.Float || val.VariantType == Variant.Type.Int)
                result[parsed.Value] = val.AsSingle();
        }
        return result.Count > 0 ? result : null;
    }
}
