using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Session;
using ProjectSummoner.Cards;
using ProjectSummoner.Units;
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
    private bool _initialized;

    public const float FIXED_DELTA = 1.0f / 60.0f;
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

    void IGameSession.Tick(float delta) { }

    public void SubmitCommand(ICommand cmd)
    {
        cmd.ExecuteFrame = State.FrameNumber + 1;
        State.PendingCommandBuffer.Add(cmd);
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
        ProcessPriority = -100;
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
        if (!_initialized || _simulation == null)
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
            var events = _simulation.Tick(FIXED_DELTA);
            _accumulator -= FIXED_DELTA;
            SimEventsEmitted?.Invoke(events);
        }
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public void Initialize(float prepDuration, float matchDuration, string winCondition,
        float winConditionTimeLimit = 0f, int winConditionKillTarget = 0, long seed = 0)
    {
        if (seed == 0)
            seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
        _initialized = true;

        GD.Print($"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, timeLimit={State.WinConditionTimeLimit}s, killTarget={winConditionKillTarget}, seed={seed})");
    }

    // =========================================================================
    // SUMMONER REGISTRATION (summoner.gd calls these)
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
    // CARD DATA (summoner.gd triggers via BattleScene)
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
    // GDSCRIPT-CALLABLE ACCESSORS (summoner.gd + BattleScene poll these)
    // =========================================================================

    public int GetPhase() => (int)State.Phase;
    public float GetPrepTimeRemaining() => State.PrepTimeRemaining;
    public float GetMatchTime() => State.MatchTime;

    // Summoner accessors (callers pass local team, we convert to network)
    public float GetPlayerHp(int team) => State.Summoners[ToNetworkTeam(team)].CurrentHp;
    public float GetPlayerMaxHp(int team) => State.Summoners[ToNetworkTeam(team)].MaxHp;
    public float GetPlayerMana(int team) => State.Summoners[ToNetworkTeam(team)].Mana;
    public float GetPlayerMaxMana(int team) => State.Summoners[ToNetworkTeam(team)].MaxMana;
    public bool IsPlayerCasting(int team) => State.Summoners[ToNetworkTeam(team)].IsCasting;
    public float GetCastingTimeRemaining(int team) => State.Summoners[ToNetworkTeam(team)].CastingTimeRemaining;
    public float GetCastingTimeTotal(int team) => State.Summoners[ToNetworkTeam(team)].CastingTimeTotal;
    public string[] GetPlayerHand(int team) => State.Summoners[ToNetworkTeam(team)].Hand.ToArray();

    public void SkipPreparation()
    {
        if (State.Phase == GamePhase.Preparation)
            State.PrepTimeRemaining = 0f;
    }

    // =========================================================================
    // COMMAND QUEUE (summoner.gd calls QueuePlayCard)
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
