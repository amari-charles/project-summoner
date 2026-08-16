using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Fateforged.Cards;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Traits.Unified;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Transport;
using Fateforged.Session;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Fateforged.Stats;
using Fateforged.Units;
using Godot;

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
    private long _matchSeed;

    // Rolling repro-capture buffer (opt-in via dev console).
    private bool _reproCaptureEnabled;
    private int _reproCaptureWindowFrames = 600;
    private string _reproCaptureSessionId = "";
    private int _reproCaptureDroppedFrames;
    private float _reproCaptureEndMatchTime;
    private string _reproCaptureLastSavedPath = "";
    private readonly List<Godot.Collections.Dictionary> _reproCaptureFrameBuffer = new();

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
        if (LocalPlayerIndex == 0)
            return team;
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

    public int GetSummonPlacementMode() => (int)GetState().SummonPlacementMode;

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public void SubmitCommand(ICommand cmd)
    {
        if (_session != null)
        {
            _session.SubmitCommand(cmd);
            return;
        }

        GD.PrintErr(
            "[SimulationNode] SubmitCommand called before initialization — command rejected"
        );
    }

    public void Tick(float delta)
    {
        _session?.Tick(delta);
    }

    // =========================================================================
    // SIGNALS (minimal — only those still awaited by BattleScene)
    // =========================================================================

    [Signal]
    public delegate void FirstSnapshotAppliedEventHandler();

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
            MaybeAutoEndReproCapture();
            return;
        }

        // Client sessions don't run deterministic simulation ticks.
        _session.Tick((float)delta);
        MaybeAutoEndReproCapture();
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

    public void Initialize(
        float prepDuration,
        float matchDuration,
        WinConditionType winCondition,
        float winConditionTimeLimit = 0f,
        int winConditionKillTarget = 0,
        long seed = 0
    )
    {
        EndReproCapture("initialize");

        if (seed == 0)
        {
            seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            GD.PushWarning(
                "[SimulationNode] No explicit seed provided — falling back to wall-clock time. This is non-deterministic and will cause desync in multiplayer."
            );
        }

        State = new MatchState
        {
            PrepTimeRemaining = prepDuration,
            WinCondition = winCondition,
            WinConditionTimeLimit =
                winConditionTimeLimit > 0 ? winConditionTimeLimit : matchDuration,
            WinConditionKillTarget = winConditionKillTarget,
            Phase = GamePhase.Preparation,
            Rng = new DeterministicRng(seed),
        };
        _matchSeed = seed;
        _reproCaptureEnabled = false;
        _reproCaptureFrameBuffer.Clear();
        _reproCaptureDroppedFrames = 0;
        _reproCaptureSessionId = "";
        _reproCaptureEndMatchTime = 0f;
        _reproCaptureLastSavedPath = "";
        State.TraitRuntimeState = UnifiedTraitRuntimeCompiler.CompileStub();

        Simulation.Log = msg => GD.Print(msg);
        _simulation = new Simulation(State);
        _commandRouter = new CommandRouter();

        SetSession(new LocalSession(_simulation, _commandRouter, State));
        _initialized = true;

        GD.Print(
            $"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, timeLimit={State.WinConditionTimeLimit}s, killTarget={winConditionKillTarget}, seed={seed})"
        );
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

        EndReproCapture("session_cleared");

        _session.SimEventsEmitted -= OnSessionSimEvents;
        if (_session is ClientSession clientSession)
            clientSession.FirstSnapshotApplied -= OnClientFirstSnapshotApplied;
        if (_session is IDisposable disposable)
            disposable.Dispose();
        _session = null;
    }

    private void OnSessionSimEvents(IReadOnlyList<SimEvent> events)
    {
        CaptureReproFrame(events);
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

    public void RegisterSummoner(
        int team,
        float hp,
        float maxHp,
        float mana,
        float maxMana,
        float castSpeed,
        string[] deckCatalogIds,
        int maxHandSize,
        Vector3 position
    )
    {
        RegisterSummoner(
            team,
            hp,
            maxHp,
            mana,
            maxMana,
            castSpeed,
            deckCatalogIds,
            maxHandSize,
            position,
            position
        );
    }

    public void RegisterBattleSide(
        ResolvedBattleSide side,
        Vector3 position,
        Vector3 targetPointPosition
    )
    {
        RegisterSummoner(
            side.Team,
            side.Summoner.Hp,
            side.Summoner.MaxHp,
            side.Summoner.Mana,
            side.Summoner.MaxMana,
            side.Summoner.CastSpeed,
            side.DeckCatalogIds(),
            side.MaxHandSize,
            position,
            targetPointPosition
        );
        SetSummonerCombatModifiers(
            side.Team,
            side.Summoner.DamageBonus,
            side.Summoner.DamageReduction,
            side.Summoner.SoulStrength,
            side.Summoner.ElementalDamageBonuses
        );
        SetSummonerHand(side.Team, side.HandCatalogIds());
        SetSummonerCardRefs(side.Team, side.DeckRefs(), side.HandRefs());
        ConfigureController(side);
    }

    public void RegisterSummoner(
        int team,
        float hp,
        float maxHp,
        float mana,
        float maxMana,
        float castSpeed,
        string[] deckCatalogIds,
        int maxHandSize,
        Vector3 position,
        Vector3 targetPointPosition
    )
    {
        int networkTeam = ToNetworkTeam(team);
        position = ToCanonical(position);
        targetPointPosition = ToCanonical(targetPointPosition);

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
        summoner.TargetPointPosition = new SimVector3(
            targetPointPosition.X,
            targetPointPosition.Y,
            targetPointPosition.Z
        );
        summoner.DamageBonus = 0f;
        summoner.DamageReduction = 0f;
        summoner.SoulStrength = 0f;
        summoner.ClearElementalDamageBonuses();

        summoner.Deck.Clear();
        foreach (var id in deckCatalogIds)
            summoner.Deck.Add(new SimCardCatalogId(id));
        summoner.DeckRefs.Clear();
        foreach (var id in deckCatalogIds)
        {
            summoner.DeckRefs.Add(
                new SimCardRuntimeRef
                {
                    CatalogId = new SimCardCatalogId(id),
                    InstanceId = SimCardInstanceId.Empty,
                }
            );
        }

        GD.Print(
            $"[SimulationNode] Registered summoner team={networkTeam} (local={team}): HP={maxHp}, Mana={maxMana}, CastSpeed={castSpeed}, Deck={deckCatalogIds.Length} cards, Position={position}, TargetPoint={targetPointPosition}"
        );
    }

    private void ConfigureController(ResolvedBattleSide side)
    {
        switch (side.Controller.Kind)
        {
            case BattleControllerKind.TrainerAi:
                ConfigureAi(
                    side.Team,
                    side.Controller.AiType,
                    side.Controller.AiPersonality,
                    side.Controller.AiDifficulty,
                    side.Controller.AiIntervalMin,
                    side.Controller.AiIntervalMax,
                    side.Controller.AiScript
                );
                ConfigureEncounterAi(EncounterAiConfig.DefaultTrainer(side.Team));
                break;
            case BattleControllerKind.EncounterAi:
                ConfigureAi(
                    side.Team,
                    side.Controller.AiType,
                    side.Controller.AiPersonality,
                    side.Controller.AiDifficulty,
                    side.Controller.AiIntervalMin,
                    side.Controller.AiIntervalMax,
                    side.Controller.AiScript
                );
                ConfigureEncounterAi(
                    side.Controller.EncounterAi ?? EncounterAiConfig.ScriptedEncounter(side.Team)
                );
                break;
            case BattleControllerKind.Player:
            case BattleControllerKind.Network:
            case BattleControllerKind.None:
                break;
        }
    }

    /// <summary>
    /// Set summoner combat modifiers loaded from profile-computed stats.
    /// Pass 2 wiring for damage pipeline completion.
    /// </summary>
    public void SetSummonerCombatModifiers(
        int team,
        float damageBonus,
        float damageReduction,
        float soulStrength = 0f,
        Dictionary<Element, float>? elementalDamageBonuses = null
    )
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1)
        {
            GD.PrintErr(
                $"[SimulationNode] Invalid team {networkTeam} for SetSummonerCombatModifiers"
            );
            return;
        }

        var summoner = State.Summoners[networkTeam];
        summoner.DamageBonus = damageBonus;
        summoner.DamageReduction = damageReduction;
        summoner.SoulStrength = soulStrength;
        summoner.ClearElementalDamageBonuses();

        if (elementalDamageBonuses != null)
        {
            foreach (var kvp in elementalDamageBonuses)
                summoner.SetElementalDamageBonus(kvp.Key, kvp.Value);
        }
    }

    public void SetSummonerHand(int team, string[] handCatalogIds)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.Hand.Clear();
        foreach (var id in handCatalogIds)
            summoner.Hand.Add(new SimCardCatalogId(id));
        summoner.HandRefs.Clear();
        foreach (var id in handCatalogIds)
        {
            summoner.HandRefs.Add(
                new SimCardRuntimeRef
                {
                    CatalogId = new SimCardCatalogId(id),
                    InstanceId = SimCardInstanceId.Empty,
                }
            );
        }
    }

    /// <summary>
    /// Pass 2 entry point: register deck/hand card runtime refs with instance identity.
    /// </summary>
    public void SetSummonerCardRefs(
        int team,
        SimCardRuntimeRef[] deckRefs,
        SimCardRuntimeRef[] handRefs
    )
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.DeckRefs.Clear();
        summoner.HandRefs.Clear();
        summoner.DiscardRefs.Clear();

        if (deckRefs != null && deckRefs.Length > 0)
            summoner.DeckRefs.AddRange(deckRefs);
        if (handRefs != null && handRefs.Length > 0)
            summoner.HandRefs.AddRange(handRefs);

        RebuildCardTraitRuntimeState();
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

    private void PopulateSingleCard(SimCardCatalogId catalogId, HashSet<string> processed)
    {
        if (!catalogId.HasValue || !processed.Add(catalogId.Value))
            return;

        var card = CardCatalog.GetCard(catalogId.Value);
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
                    simCard.UnitTemplates.Add(
                        UnitDefinitions.BuildSimTemplate(entry.UnitId, entry.Count, entry.Modifier)
                    );
            }
            else if (card.UnitId.HasValue)
            {
                simCard.UnitTemplates.Add(
                    UnitDefinitions.BuildSimTemplate(
                        card.UnitId,
                        card.SpawnCount,
                        card.UnitModifier
                    )
                );
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
    public void ConfigureAi(
        int team,
        AiType aiType,
        AiPersonality personality = AiPersonality.Balanced,
        int difficulty = DefaultAiDifficulty,
        float intervalMin = DefaultAiIntervalMin,
        float intervalMax = DefaultAiIntervalMax,
        Godot.Collections.Array? scriptSteps = null
    )
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
            PlayIntervalMax = intervalMax,
        };

        // Parse scripted steps
        if (aiType == AiType.Scripted && scriptSteps != null && scriptSteps.Count > 0)
        {
            var steps = new ScriptedAiStep[scriptSteps.Count];
            for (int i = 0; i < scriptSteps.Count; i++)
            {
                var stepDict = scriptSteps[i].AsGodotDictionary();
                float triggerTime = GetFloat(stepDict, "delay", 0.0f);
                string cardId = stepDict.GetValueOrDefault("card_name", "").ToString();

                SimVector3 spawnPos = SimVector3.Zero;
                var posVar = stepDict.GetValueOrDefault("position", default);
                if (posVar.VariantType == Variant.Type.Dictionary)
                {
                    var posDict = posVar.AsGodotDictionary();
                    float px = GetFloat(posDict, "x", 0.0f);
                    float pz = GetFloat(posDict, "y", 0.0f);
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

        GD.Print(
            $"[SimulationNode] Configured AI: team={networkTeam} type={aiType} personality={personality} difficulty={difficulty} interval=[{intervalMin},{intervalMax}]"
        );
    }

    private static float GetFloat(
        Godot.Collections.Dictionary dict,
        string key,
        float defaultValue
    )
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;

        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue,
        };
    }

    public void ConfigureEncounterAi(EncounterAiConfig? config)
    {
        State.EncounterAi = config;
        if (config == null)
            return;

        CanonicalizeEncounterAiPositions(config);

        foreach (var cardId in EnumerateEncounterAiCardIds(config))
            EnsureCardDataPopulated(cardId);

        GD.Print(
            $"[SimulationNode] Configured Encounter AI: preset={config.Preset} team={config.Team} rules={config.Rules.Count}"
        );
    }

    private void CanonicalizeEncounterAiPositions(EncounterAiConfig config)
    {
        if (config.PositionsAreCanonical)
            return;

        foreach (var rule in config.Rules)
        {
            foreach (var action in rule.Actions)
            {
                if (action.Position.HasValue)
                {
                    var position = action.Position.Value;
                    action.Position = ToSimCanonical(new Vector3(position.X, 0f, position.Z));
                }

                for (int i = 0; i < action.Positions.Count; i++)
                {
                    var position = action.Positions[i];
                    action.Positions[i] = ToSimCanonical(new Vector3(position.X, 0f, position.Z));
                }
            }
        }

        config.PositionsAreCanonical = true;
    }

    private static IEnumerable<string> EnumerateEncounterAiCardIds(EncounterAiConfig config)
    {
        foreach (var rule in config.Rules)
        {
            foreach (var cardId in rule.CardPool)
            {
                if (!string.IsNullOrWhiteSpace(cardId))
                    yield return cardId;
            }

            foreach (var action in rule.Actions)
            {
                if (!string.IsNullOrWhiteSpace(action.CardId))
                    yield return action.CardId;
                foreach (var cardId in action.CardIds)
                {
                    if (!string.IsNullOrWhiteSpace(cardId))
                        yield return cardId;
                }
            }
        }
    }

    // =========================================================================
    // GDSCRIPT-CALLABLE ACCESSORS (BattleScene polls these)
    // =========================================================================

    public int GetPhase() => (int)GetState().Phase;

    public float GetPrepTimeRemaining() => GetState().PrepTimeRemaining;

    public float GetMatchTime() => GetState().MatchTime;

    public int GetWinnerTeam() => GetState().WinnerTeam ?? -1;

    public Godot.Collections.Dictionary GetTraitRuntimeStatus()
    {
        var runtime = GetState().TraitRuntimeState;
        var diagnostics = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var diagnostic in runtime.Diagnostics)
        {
            diagnostics.Add(
                new Godot.Collections.Dictionary
                {
                    ["severity"] = diagnostic.Severity.ToString(),
                    ["code"] = diagnostic.Code,
                    ["message"] = diagnostic.Message,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["ruleset_version"] = runtime.RulesetVersion.Value,
            ["is_stub"] = runtime.RulesetVersion.Value == MatchTraitRuntimeState.StubRulesetVersion,
            ["diagnostic_count"] = runtime.Diagnostics.Count,
            ["diagnostics"] = diagnostics,
        };
    }

    /// <summary>
    /// Debug helper: returns current spawned unit stats from simulation state.
    /// </summary>
    /// <param name="team">
    /// Team filter: 0/1 for a specific team, or -1 for all teams.
    /// </param>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetUnitStatsSnapshot(int team = -1)
    {
        var units = new List<UnitData>(GetState().Units.Values);
        units.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var unit in units)
        {
            if (team >= 0 && (int)unit.Team != team)
                continue;

            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["unit_id"] = unit.UnitId,
                    ["network_id"] = unit.NetworkId,
                    ["team"] = (int)unit.Team,
                    ["catalog_id"] = unit.CatalogId.Value,
                    ["is_alive"] = unit.IsAlive,
                    ["activation_state"] = (int)unit.ActivationState,
                    ["current_hp"] = unit.CurrentHp,
                    ["max_hp"] = unit.MaxHp,
                    ["attack_damage"] = unit.AttackDamage,
                    ["attack_speed"] = unit.AttackSpeed,
                    ["move_speed"] = unit.MoveSpeed,
                    ["attack_range"] = unit.AttackRange,
                }
            );
        }

        return result;
    }

    // =========================================================================
    // DEBUG REPRO CAPTURE (dev-console driven)
    // =========================================================================

    public bool StartReproCapture(int windowSeconds = 12)
    {
        EndReproCapture("restart");

        int clampedSeconds = Math.Clamp(windowSeconds, 1, 120);
        _reproCaptureWindowFrames = Math.Max(1, (int)MathF.Round(clampedSeconds / FIXED_DELTA));
        _reproCaptureFrameBuffer.Clear();
        _reproCaptureDroppedFrames = 0;
        _reproCaptureSessionId =
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        var state = GetState();
        _reproCaptureEndMatchTime = state.MatchTime + clampedSeconds;
        _reproCaptureLastSavedPath = "";
        _reproCaptureEnabled = true;
        GD.Print(
            $"[SimulationNode] Repro capture started: window={clampedSeconds}s (~{_reproCaptureWindowFrames} frames), session={_reproCaptureSessionId}, auto_end_match_time={_reproCaptureEndMatchTime:0.00}"
        );
        return true;
    }

    public void StopReproCapture()
    {
        EndReproCapture("manual_stop");
    }

    public Godot.Collections.Dictionary GetReproCaptureStatus()
    {
        var state = GetState();
        float remainingSeconds = _reproCaptureEnabled
            ? MathF.Max(0f, _reproCaptureEndMatchTime - state.MatchTime)
            : 0f;
        return new Godot.Collections.Dictionary
        {
            ["enabled"] = _reproCaptureEnabled,
            ["session_id"] = _reproCaptureSessionId,
            ["window_frames"] = _reproCaptureWindowFrames,
            ["window_seconds"] = _reproCaptureWindowFrames * FIXED_DELTA,
            ["auto_end_match_time"] = _reproCaptureEndMatchTime,
            ["remaining_seconds"] = remainingSeconds,
            ["remaining_frames"] = (int)MathF.Ceiling(remainingSeconds / FIXED_DELTA),
            ["last_saved_path"] = _reproCaptureLastSavedPath,
            ["buffered_frames"] = _reproCaptureFrameBuffer.Count,
            ["dropped_frames"] = _reproCaptureDroppedFrames,
            ["seed"] = _matchSeed,
            ["local_player_index"] = LocalPlayerIndex,
            ["is_host"] = IsHost,
            ["match_time"] = state.MatchTime,
            ["frame_number"] = state.FrameNumber,
            ["phase"] = (int)state.Phase,
        };
    }

    public string MarkReproCapture(string label = "")
    {
        return SaveReproCaptureSnapshot(label);
    }

    private string SaveReproCaptureSnapshot(string label)
    {
        if (_reproCaptureFrameBuffer.Count == 0)
        {
            GD.Print("[SimulationNode] Repro capture mark rejected: no buffered frames");
            return string.Empty;
        }

        string safeLabel = SanitizeCaptureLabel(label);
        string stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var state = GetState();
        string suffix = safeLabel.Length > 0 ? $"_{safeLabel}" : "";
        string fileName = $"repro_{stamp}_f{state.FrameNumber}{suffix}.json";
        const string captureDirUser = "user://debug/repro_captures";

        var frames = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var frame in _reproCaptureFrameBuffer)
            frames.Add(frame);

        var payload = new Godot.Collections.Dictionary
        {
            ["schema_version"] = 1,
            ["created_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["label"] = label ?? string.Empty,
            ["session_id"] = _reproCaptureSessionId,
            ["seed"] = _matchSeed,
            ["local_player_index"] = LocalPlayerIndex,
            ["is_host"] = IsHost,
            ["window_frames"] = _reproCaptureWindowFrames,
            ["window_seconds"] = _reproCaptureWindowFrames * FIXED_DELTA,
            ["buffered_frames"] = _reproCaptureFrameBuffer.Count,
            ["dropped_frames"] = _reproCaptureDroppedFrames,
            ["state_overview"] = new Godot.Collections.Dictionary
            {
                ["frame_number"] = state.FrameNumber,
                ["match_time"] = state.MatchTime,
                ["phase"] = (int)state.Phase,
                ["winner_team"] = state.WinnerTeam ?? -1,
            },
            ["frames"] = frames,
        };

        try
        {
            string captureDirAbsolute = ProjectSettings.GlobalizePath(captureDirUser);
            Directory.CreateDirectory(captureDirAbsolute);
            string absolutePath = Path.Combine(captureDirAbsolute, fileName);
            File.WriteAllText(absolutePath, Json.Stringify(payload, "\t"));
            string userPath = $"{captureDirUser}/{fileName}";
            _reproCaptureLastSavedPath = userPath;
            GD.Print(
                $"[SimulationNode] Repro capture saved: {userPath} ({_reproCaptureFrameBuffer.Count} frames)"
            );
            return userPath;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SimulationNode] Failed to save repro capture: {ex.Message}");
            return string.Empty;
        }
    }

    private void CaptureReproFrame(IReadOnlyList<SimEvent> events)
    {
        if (!_reproCaptureEnabled)
            return;

        _reproCaptureFrameBuffer.Add(BuildReproFrameSnapshot(events));
        while (_reproCaptureFrameBuffer.Count > _reproCaptureWindowFrames)
        {
            _reproCaptureFrameBuffer.RemoveAt(0);
            _reproCaptureDroppedFrames++;
        }
    }

    private void MaybeAutoEndReproCapture()
    {
        if (!_reproCaptureEnabled)
            return;

        var state = GetState();
        if (state.MatchTime >= _reproCaptureEndMatchTime)
            EndReproCapture("window_elapsed");
    }

    private void EndReproCapture(string reason)
    {
        if (!_reproCaptureEnabled)
            return;

        var state = GetState();
        string autoSavedPath = string.Empty;
        if (reason == "window_elapsed")
            autoSavedPath = SaveReproCaptureSnapshot("auto");

        _reproCaptureEnabled = false;
        _reproCaptureEndMatchTime = state.MatchTime;
        string savedSuffix = autoSavedPath.Length > 0 ? $", autosaved={autoSavedPath}" : "";
        GD.Print(
            $"[SimulationNode] Repro capture ended ({reason}): session={_reproCaptureSessionId}, buffered={_reproCaptureFrameBuffer.Count}/{_reproCaptureWindowFrames}, dropped={_reproCaptureDroppedFrames}, frame={state.FrameNumber}, time={state.MatchTime:0.00}{savedSuffix}"
        );
    }

    private Godot.Collections.Dictionary BuildReproFrameSnapshot(IReadOnlyList<SimEvent> events)
    {
        var state = GetState();
        var frame = new Godot.Collections.Dictionary
        {
            ["frame_number"] = state.FrameNumber,
            ["match_time"] = state.MatchTime,
            ["phase"] = (int)state.Phase,
            ["winner_team"] = state.WinnerTeam ?? -1,
            ["pending_commands"] = state.PendingCommandBuffer.Count,
            ["summoners"] = BuildReproSummonerSnapshot(state),
            ["units"] = BuildReproUnitSnapshot(state),
            ["projectiles"] = BuildReproProjectileSnapshot(state),
            ["events"] = BuildReproEventSnapshot(events),
        };
        return frame;
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildReproSummonerSnapshot(
        MatchState state
    )
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        for (int i = 0; i < state.Summoners.Length; i++)
        {
            var summoner = state.Summoners[i];
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["team"] = i,
                    ["is_alive"] = summoner.IsAlive,
                    ["current_hp"] = summoner.CurrentHp,
                    ["max_hp"] = summoner.MaxHp,
                    ["mana"] = summoner.Mana,
                    ["max_mana"] = summoner.MaxMana,
                    ["cast_speed"] = summoner.CastSpeed,
                    ["position"] = ToVectorDict(summoner.Position),
                    ["target_point_position"] = ToVectorDict(summoner.TargetPointPosition),
                    ["hand_count"] = summoner.Hand.Count,
                    ["deck_count"] = summoner.Deck.Count,
                    ["discard_count"] = summoner.DiscardPile.Count,
                }
            );
        }
        return result;
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildReproUnitSnapshot(
        MatchState state
    )
    {
        var units = new List<UnitData>(state.Units.Values);
        units.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var unit in units)
        {
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["unit_id"] = unit.UnitId,
                    ["network_id"] = unit.NetworkId,
                    ["catalog_id"] = unit.CatalogId.Value,
                    ["team"] = (int)unit.Team,
                    ["is_alive"] = unit.IsAlive,
                    ["activation_state"] = (int)unit.ActivationState,
                    ["position"] = ToVectorDict(unit.Position),
                    ["velocity"] = ToVectorDict(unit.Velocity),
                    ["movement_layer"] = (int)unit.MovementLayer,
                    ["unit_type"] = (int)unit.UnitType,
                    ["behavior_state"] = (int)unit.BehaviorState,
                    ["combat_lifecycle_state"] = (int)unit.Engagement.LifecycleState,
                    ["attack_phase"] = (int)unit.Action.AttackPhase,
                    ["attack_cooldown"] = unit.AttackCooldown,
                    ["attack_range"] = unit.AttackRange,
                    ["move_speed"] = unit.MoveSpeed,
                    ["aggro_radius"] = unit.AggroRadius,
                    ["navigation_radius"] = unit.NavigationRadius,
                    ["hurtbox_radius"] = unit.HurtboxRadius,
                    ["engage_shape"] = (int)unit.EngageShape,
                    ["engage_rect_length"] = unit.EngageRectLength,
                    ["engage_rect_half_width"] = unit.EngageRectHalfWidth,
                    ["engage_rect_forward_offset"] = unit.EngageRectForwardOffset,
                    ["engage_close_radius"] = unit.EngageCloseRadius,
                    ["target_unit_id"] = NullableIntToVariant(unit.Engagement.TargetUnitId),
                    ["locked_target_unit_id"] = NullableIntToVariant(unit.Engagement.LockedTargetUnitId),
                    ["forced_target_unit_id"] = NullableIntToVariant(unit.Engagement.ForcedTargetUnitId),
                    ["navigation_blocked_time"] = unit.NavigationBlockedTime,
                    ["no_progress_timer"] = unit.Engagement.NoProgressTimer,
                    ["is_facing_right"] = unit.IsFacingRight,
                    ["attack_selection_mode"] = (int)unit.Attack.Selection.Mode,
                    ["attack_area_shape"] = (int)unit.Attack.Area.Shape,
                    ["attack_forward_offset"] = unit.Attack.Area.ForwardOffset,
                }
            );
        }
        return result;
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildReproProjectileSnapshot(
        MatchState state
    )
    {
        var projectiles = new List<SimProjectileData>(state.Projectiles.Values);
        projectiles.Sort((a, b) => a.ProjectileId.CompareTo(b.ProjectileId));

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var projectile in projectiles)
        {
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["projectile_id"] = projectile.ProjectileId,
                    ["catalog_id"] = projectile.ProjectileCatalogId.Value,
                    ["source_unit_id"] = projectile.SourceUnitId,
                    ["target_unit_id"] = projectile.TargetUnitId,
                    ["team"] = (int)projectile.Team,
                    ["current_position"] = ToVectorDict(projectile.CurrentPosition),
                    ["last_position"] = ToVectorDict(projectile.LastPosition),
                    ["target_position"] = ToVectorDict(projectile.TargetPosition),
                    ["direction"] = ToVectorDict(projectile.Direction),
                    ["speed"] = projectile.Speed,
                    ["time_alive"] = projectile.TimeAlive,
                    ["lifetime"] = projectile.Lifetime,
                    ["hit_radius"] = projectile.HitRadius,
                    ["aoe_radius"] = projectile.AoeRadius,
                    ["is_dead"] = projectile.IsDead,
                }
            );
        }
        return result;
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildReproEventSnapshot(
        IReadOnlyList<SimEvent> events
    )
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var simEvent in events)
            result.Add(SerializeSimEvent(simEvent));
        return result;
    }

    private static Godot.Collections.Dictionary SerializeSimEvent(SimEvent simEvent)
    {
        var payload = new Godot.Collections.Dictionary();
        var properties = simEvent.GetType().GetProperties(
            BindingFlags.Instance | BindingFlags.Public
        );
        Array.Sort(properties, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        foreach (var property in properties)
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            payload[property.Name] = SerializeEventValue(property.GetValue(simEvent));
        }

        return new Godot.Collections.Dictionary
        {
            ["type"] = simEvent.GetType().Name,
            ["payload"] = payload,
        };
    }

    private static Variant SerializeEventValue(object? value)
    {
        if (value == null)
            return default;

        return value switch
        {
            SimVector3 vec => Variant.From(ToVectorDict(vec)),
            SimCardCatalogId id => Variant.From(id.Value),
            SimProjectileCatalogId id => Variant.From(id.Value),
            SimUnitCatalogId id => Variant.From(id.Value),
            SimCardInstanceId id => Variant.From(id.Value),
            string s => Variant.From(s),
            bool b => Variant.From(b),
            byte b => Variant.From((int)b),
            sbyte b => Variant.From((int)b),
            short i => Variant.From((int)i),
            ushort i => Variant.From((int)i),
            int i => Variant.From(i),
            uint i => Variant.From((long)i),
            long i => Variant.From(i),
            ulong i => Variant.From(i.ToString()),
            float f => Variant.From(f),
            double d => Variant.From((float)d),
            Enum e => Variant.From(e.ToString()),
            _ => SerializeComplexEventValue(value),
        };
    }

    private static Variant SerializeComplexEventValue(object value)
    {
        if (value is string[])
        {
            var stringArray = new Godot.Collections.Array();
            foreach (string item in (string[])value)
                stringArray.Add(item);
            return Variant.From(stringArray);
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var array = new Godot.Collections.Array();
            foreach (object? item in enumerable)
                array.Add(SerializeEventValue(item));
            return Variant.From(array);
        }

        return Variant.From(value.ToString() ?? string.Empty);
    }

    private static Variant NullableIntToVariant(int? value)
    {
        return value.HasValue ? Variant.From(value.Value) : default;
    }

    private static Godot.Collections.Dictionary ToVectorDict(SimVector3 value)
    {
        return new Godot.Collections.Dictionary
        {
            ["x"] = value.X,
            ["y"] = value.Y,
            ["z"] = value.Z,
        };
    }

    private static string SanitizeCaptureLabel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var builder = new StringBuilder();
        foreach (char c in raw.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                builder.Append(c);
            else if (char.IsWhiteSpace(c))
                builder.Append('_');
        }

        string sanitized = builder.ToString().Trim('_');
        if (sanitized.Length > 48)
            sanitized = sanitized[..48];
        return sanitized;
    }

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
        var cmd = new PlayCardCommand(
            ToNetworkTeam(team),
            cardIndex,
            ToSimCanonical(spawnPosition),
            networkId
        );
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Queue a direct unit spawn (no mana, no casting, no hand management).
    /// Used by debug arena, event sequencer, scripted AI, tutorials.
    /// </summary>
    public void QueueSpawnUnit(
        string catalogId,
        int team,
        Vector3 position,
        bool activateImmediately = true,
        Godot.Collections.Dictionary? statOverrides = null
    )
    {
        var simCatalogId = new SimCardCatalogId(catalogId);
        EnsureCardDataPopulated(simCatalogId);

        var cmd = new SpawnUnitCommand(simCatalogId, ToNetworkTeam(team), ToSimCanonical(position))
        {
            ActivateImmediately = activateImmediately,
            StatOverrides = ConvertStatOverrides(statOverrides),
        };
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Queue a direct spell cast at position (no mana, no hand/casting state changes).
    /// Intended for debug/event-driven spell injection.
    /// </summary>
    public void QueueCastSpell(string catalogId, int team, Vector3 position)
    {
        var simCatalogId = new SimCardCatalogId(catalogId);
        EnsureCardDataPopulated(simCatalogId);

        if (
            !State.CardDataMap.TryGetValue(simCatalogId, out var cardData)
            || !cardData.IsSpell
        )
        {
            GD.PushWarning(
                $"[SimulationNode] QueueCastSpell rejected: '{catalogId}' is not a spell card"
            );
            return;
        }

        var cmd = new SpawnUnitCommand(simCatalogId, ToNetworkTeam(team), ToSimCanonical(position))
        {
            ActivateImmediately = true,
            StatOverrides = null,
        };
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Ensure a single card's data is in CardDataMap.
    /// Called by QueueSpawnUnit for cards that may not be in any summoner's deck.
    /// </summary>
    public void EnsureCardDataPopulated(string catalogId) =>
        EnsureCardDataPopulated(new SimCardCatalogId(catalogId));

    private void EnsureCardDataPopulated(SimCardCatalogId catalogId)
    {
        if (State.CardDataMap.ContainsKey(catalogId))
            return;

        var processed = new HashSet<string>();
        foreach (var key in State.CardDataMap.Keys)
            processed.Add(key.Value);
        PopulateSingleCard(catalogId, processed);
    }

    private static Dictionary<StatKey, float>? ConvertStatOverrides(
        Godot.Collections.Dictionary? gdDict
    )
    {
        if (gdDict == null || gdDict.Count == 0)
            return null;

        var result = new Dictionary<StatKey, float>();
        foreach (var key in gdDict.Keys)
        {
            var keyStr = key.AsString();
            var parsed = StatKeyExtensions.FromString(keyStr);
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

    private void RebuildCardTraitRuntimeState()
    {
        var runtime = State.TraitRuntimeState;
        runtime.ResetCardInstanceStatMultipliers();

        var cardService = CardService.Instance;
        if (cardService == null)
            return;

        var processedInstanceIds = new HashSet<string>();
        foreach (var summoner in State.Summoners)
        {
            RegisterTraitRuntimeModifiersForRefs(
                summoner.DeckRefs,
                cardService,
                runtime,
                processedInstanceIds
            );
            RegisterTraitRuntimeModifiersForRefs(
                summoner.HandRefs,
                cardService,
                runtime,
                processedInstanceIds
            );
            RegisterTraitRuntimeModifiersForRefs(
                summoner.DiscardRefs,
                cardService,
                runtime,
                processedInstanceIds
            );
        }
    }

    private static void RegisterTraitRuntimeModifiersForRefs(
        IEnumerable<SimCardRuntimeRef> refs,
        CardService cardService,
        MatchTraitRuntimeState runtime,
        ISet<string> processedInstanceIds
    )
    {
        foreach (var cardRef in refs)
        {
            if (!cardRef.InstanceId.HasValue)
                continue;
            if (!processedInstanceIds.Add(cardRef.InstanceId.Value))
                continue;

            var rawModifiers = cardService.GetTraitStatModifiersTyped(cardRef.InstanceId.Value);
            var rawAdds = cardService.GetTraitStatAddModifiersTyped(cardRef.InstanceId.Value);
            var spawnCountAdd = cardService.GetTraitSpawnCountBonus(cardRef.InstanceId.Value);
            if (rawModifiers.Count == 0 && rawAdds.Count == 0)
            {
                if (spawnCountAdd != 0)
                {
                    runtime.SetCardInstanceSpawnCountAdd(
                        new TraitRuntimeCardInstanceId(cardRef.InstanceId.Value),
                        spawnCountAdd
                    );
                }
                continue;
            }

            var typedModifiers = new Dictionary<StatKey, float>();
            foreach (var (statKey, multiplier) in rawModifiers)
            {
                if (multiplier <= 0f)
                    continue;

                var parsedStatKey = StatKeyExtensions.FromString(statKey);
                if (!parsedStatKey.HasValue)
                    continue;
                typedModifiers[parsedStatKey.Value] = multiplier;
            }

            if (typedModifiers.Count == 0)
            {
                if (spawnCountAdd != 0)
                {
                    runtime.SetCardInstanceSpawnCountAdd(
                        new TraitRuntimeCardInstanceId(cardRef.InstanceId.Value),
                        spawnCountAdd
                    );
                }
            }

            var typedAdds = new Dictionary<StatKey, float>();
            foreach (var (statKey, addValue) in rawAdds)
            {
                if (addValue == 0f)
                    continue;

                var parsedStatKey = StatKeyExtensions.FromString(statKey);
                if (!parsedStatKey.HasValue)
                    continue;
                typedAdds[parsedStatKey.Value] = addValue;
            }

            var traitCardInstanceId = new TraitRuntimeCardInstanceId(cardRef.InstanceId.Value);
            if (typedModifiers.Count > 0)
            {
                runtime.SetCardInstanceStatMultipliers(traitCardInstanceId, typedModifiers);
            }

            if (typedAdds.Count > 0)
            {
                runtime.SetCardInstanceStatAdds(traitCardInstanceId, typedAdds);
            }

            if (spawnCountAdd != 0)
            {
                runtime.SetCardInstanceSpawnCountAdd(traitCardInstanceId, spawnCountAdd);
            }
        }
    }
}
