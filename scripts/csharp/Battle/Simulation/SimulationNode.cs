using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
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
/// Scene-tree bridge for the simulation layer.
/// Owns MatchState and Simulation. Implements IGameSession.
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
    private LocalSession? _localSession;
    private CommandRouter _commandRouter = new();
    private bool _initialized;

    public const float FIXED_DELTA = 1.0f / 60.0f;
    private const int SimulationProcessPriority = -100;
    private const int DefaultAiDifficulty = 3;
    private const float DefaultAiIntervalMin = 3.0f;
    private const float DefaultAiIntervalMax = 6.0f;

    private float _accumulator;

    public bool IsHost { get; set; } = true;
    public int LocalPlayerIndex { get; set; } = 0;

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

    public MatchState GetState() => State;

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public void SubmitCommand(ICommand cmd)
    {
        if (_localSession != null)
        {
            _localSession.SubmitCommand(cmd);
        }
        else
        {
            GD.PrintErr("[SimulationNode] SubmitCommand called before initialization — command rejected");
        }
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
        if (Current == this)
            Current = null;
    }

    private bool _invariantsChecked;

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized || _localSession == null)
            return;

#if DEBUG
        if (!_invariantsChecked)
        {
            _invariantsChecked = true;
            var violations = MatchStateInvariants.ValidatePostInit(State);
            foreach (var v in violations)
                GD.PrintErr($"[SimulationNode] Post-init invariant violation: {v}");
        }
#endif

        if (!IsHost)
            return;

        _accumulator += (float)delta;
        while (_accumulator >= FIXED_DELTA)
        {
            _localSession.Tick(FIXED_DELTA);
            _accumulator -= FIXED_DELTA;
        }
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
        _localSession = new LocalSession(_simulation, _commandRouter, State);
        _localSession.SimEventsEmitted += events => SimEventsEmitted?.Invoke(events);
        _initialized = true;

        GD.Print($"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, timeLimit={State.WinConditionTimeLimit}s, killTarget={winConditionKillTarget}, seed={seed})");
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
    public void ConfigureAi(int team, string aiType, AiPersonality personality = AiPersonality.Balanced,
        int difficulty = DefaultAiDifficulty, float intervalMin = DefaultAiIntervalMin, float intervalMax = DefaultAiIntervalMax,
        Godot.Collections.Array? scriptSteps = null)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];

        var aiTypeLower = aiType.ToLowerInvariant();
        var type = aiTypeLower switch
        {
            "simple" => AiType.Simple,
            "heuristic" => AiType.Heuristic,
            "scripted" => AiType.Scripted,
            "passive" or "none" => AiType.None,
            _ => AiType.Heuristic
        };

        if (aiTypeLower is not ("simple" or "heuristic" or "scripted" or "passive" or "none"))
            GD.PushWarning($"[SimulationNode] Unknown AI type '{aiType}', defaulting to Heuristic");

        if (type == AiType.None)
        {
            summoner.Ai = null;
            return;
        }

        var config = new AiConfig
        {
            Type = type,
            Personality = personality,
            Difficulty = difficulty,
            PlayIntervalMin = intervalMin,
            PlayIntervalMax = intervalMax
        };

        // Parse scripted steps
        if (type == AiType.Scripted && scriptSteps != null && scriptSteps.Count > 0)
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

        GD.Print($"[SimulationNode] Configured AI: team={networkTeam} type={type} personality={personality} difficulty={difficulty} interval=[{intervalMin},{intervalMax}]");
    }

    // =========================================================================
    // GDSCRIPT-CALLABLE ACCESSORS (BattleScene polls these)
    // =========================================================================

    public int GetPhase() => (int)State.Phase;
    public float GetPrepTimeRemaining() => State.PrepTimeRemaining;
    public float GetMatchTime() => State.MatchTime;

    public void SkipPreparation()
    {
        if (State.Phase == GamePhase.Preparation)
            State.PrepTimeRemaining = 0f;
    }

    // =========================================================================
    // COMMAND QUEUE
    // =========================================================================

    public void QueuePlayCard(int team, int cardIndex, Vector3 spawnPosition, int networkId = -1)
    {
        // MP client: route through authority provider instead of local submission
        if (!IsHost)
        {
            var battleContext = GetNodeOrNull("/root/BattleContext");
            if (battleContext != null)
            {
                var authProvider = battleContext.Get("authority_provider");
                if (authProvider.VariantType != Variant.Type.Nil)
                {
                    var provider = authProvider.AsGodotObject() as Node;
                    if (provider != null && provider.HasMethod("request_card_play"))
                    {
                        provider.Call("request_card_play", cardIndex, spawnPosition);
                        return;
                    }
                }
            }
            GD.PrintErr("[SimulationNode] MP client has no authority provider for card play!");
            return;
        }

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

    private static System.Collections.Generic.Dictionary<string, float>? ConvertStatOverrides(
        Godot.Collections.Dictionary? gdDict)
    {
        if (gdDict == null || gdDict.Count == 0)
            return null;

        var result = new System.Collections.Generic.Dictionary<string, float>();
        foreach (var key in gdDict.Keys)
        {
            var keyStr = key.AsString();
            var val = gdDict[key];
            if (val.VariantType == Variant.Type.Float || val.VariantType == Variant.Type.Int)
                result[keyStr] = val.AsSingle();
        }
        return result.Count > 0 ? result : null;
    }
}
