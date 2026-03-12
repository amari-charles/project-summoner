using System;
using System.Collections.Generic;
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

    public void Initialize(
        float prepDuration,
        float matchDuration,
        WinConditionType winCondition,
        float winConditionTimeLimit = 0f,
        int winConditionKillTarget = 0,
        long seed = 0
    )
    {
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
            $"[SimulationNode] Registered summoner team={networkTeam} (local={team}): HP={maxHp}, Mana={maxMana}, CastSpeed={castSpeed}, Deck={deckCatalogIds.Length} cards, Position={position}"
        );
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

        GD.Print(
            $"[SimulationNode] Configured AI: team={networkTeam} type={aiType} personality={personality} difficulty={difficulty} interval=[{intervalMin},{intervalMax}]"
        );
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
