using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;
using ProjectSummoner.Stats;
using ProjectSummoner.Units;
using ProjectSummoner.Targeting;

namespace Fateforged.Simulation;

/// <summary>
/// Scene-tree bridge for the simulation layer.
/// Owns MatchState and Simulation. Exposes GDScript-callable accessors and emits Godot signals.
///
/// Added as a child of GameController3D. Runs Tick() in _PhysicsProcess() with ProcessPriority = -100
/// so it runs before Unit3D._PhysicsProcess() (which writes positions back to MatchState).
///
/// Singleton via SimulationNode.Current (same pattern as MatchSession.Current, DamageSystem.Instance).
/// </summary>
[GlobalClass]
public partial class SimulationNode : Node
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

    /// <summary>
    /// Tracks which sim unit IDs have been claimed by visual Unit3D nodes.
    /// Used by ClaimNextSimUnitId to prevent double-linking.
    /// </summary>
    private readonly System.Collections.Generic.HashSet<int> _claimedSimUnitIds = new();

    /// <summary>
    /// Fixed timestep for simulation ticks (60 Hz).
    /// </summary>
    public const float FIXED_DELTA = 1.0f / 60.0f;

    /// <summary>
    /// Accumulator for fixed timestep. Excess delta from _PhysicsProcess is carried over.
    /// </summary>
    private float _accumulator;

    /// <summary>
    /// Whether this node acts as the host (runs Tick). Default true for single-player.
    /// Set to false by ClientRunner for multiplayer clients.
    /// </summary>
    public bool IsHost { get; set; } = true;

    /// <summary>
    /// Local player's network index. Used to remap snapshot teams to local teams.
    /// 0 = host/single-player (no remap), 1 = client (swap).
    /// Set by MultiplayerGameBridge before any snapshots arrive.
    /// </summary>
    public int LocalPlayerIndex { get; set; } = 0;

    /// <summary>
    /// Swap team index. For a 2-player game, swapping is its own inverse.
    /// Host/single-player: identity (no swap). Client: 0↔1.
    /// </summary>
    public int RemapTeam(int team)
    {
        if (LocalPlayerIndex == 0) return team;
        return team == 0 ? 1 : 0;
    }

    /// <summary>
    /// Convert local team (caller's perspective) to network team (MatchState storage).
    /// GDScript always uses local teams (PLAYER=0, ENEMY=1). MatchState uses network teams.
    /// </summary>
    private int ToNetworkTeam(int localTeam) => RemapTeam(localTeam);

    /// <summary>
    /// Convert network team (MatchState) to local team (for signal emission to GDScript).
    /// </summary>
    private int ToLocalTeam(int networkTeam) => RemapTeam(networkTeam);

    /// <summary>
    /// Convert local-space position to canonical (MatchState) coordinates.
    /// </summary>
    private Vector3 ToCanonical(Vector3 localPos) => CoordinateTransform.LocalToCanonical(localPos);

    /// <summary>
    /// Convert canonical (MatchState) position to local-space coordinates.
    /// </summary>
    private Vector3 ToLocal(Vector3 canonicalPos) => CoordinateTransform.CanonicalToLocal(canonicalPos);

    /// <summary>
    /// Convert a SimVector3 to local Godot.Vector3 (for signal emission to GDScript).
    /// </summary>
    private Vector3 ToLocal(SimVector3 simPos) => CoordinateTransform.CanonicalToLocal(new Vector3(simPos.X, simPos.Y, simPos.Z));

    /// <summary>
    /// Convert a SimVector3 to local Godot.Vector3 (public, for Unit3D to read positions).
    /// </summary>
    public Vector3 SimToLocal(SimVector3 simPos) => ToLocal(simPos);

    /// <summary>
    /// Convert a Godot.Vector3 to SimVector3 in canonical coordinates.
    /// </summary>
    private SimVector3 ToSimCanonical(Vector3 localPos)
    {
        var c = CoordinateTransform.LocalToCanonical(localPos);
        return new SimVector3(c.X, c.Y, c.Z);
    }

    // =========================================================================
    // MULTIPLAYER HOOKS
    // =========================================================================

    /// <summary>
    /// Invoked after each Tick + EmitEvents. HostRunner subscribes to convert
    /// key SimEvents into protocol messages for broadcast to clients.
    /// Null when no MatchSession is active (single-player).
    /// </summary>
    public event Action<List<SimEvent>>? OnTickCompleted;

    /// <summary>
    /// Expose the Simulation instance for snapshot hash computation.
    /// </summary>
    public Simulation? GetSimulation() => _simulation;

    // =========================================================================
    // SIGNALS (emitted after Tick, consumed by presentation layer)
    // =========================================================================

    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);
    [Signal] public delegate void PrepTimerUpdatedEventHandler(float remaining);
    [Signal] public delegate void MatchTimeUpdatedEventHandler(float matchTime);
    [Signal] public delegate void SummonerHpChangedEventHandler(int team, float hp, float maxHp);
    [Signal] public delegate void SummonerManaChangedEventHandler(int team, float mana, float maxMana);
    [Signal] public delegate void CastingStartedEventHandler(int team, int cardIndex, float duration, Vector3 spawnPosition, string catalogId);
    [Signal] public delegate void CastingCompletedEventHandler(int team, int cardIndex, Vector3 spawnPosition, int networkId);
    [Signal] public delegate void CardDrawnEventHandler(int team, int handIndex, string catalogId);
    [Signal] public delegate void HandChangedEventHandler(int team, string[] hand);
    [Signal] public delegate void DeckRecycledEventHandler(int team);
    [Signal] public delegate void UnitStateRegisteredEventHandler(int unitId, int networkId, int team);
    [Signal] public delegate void UnitStateRemovedEventHandler(int unitId);
    [Signal] public delegate void GameOverEventHandler(int winnerTeam, string reason);
    [Signal] public delegate void OvertimeChangedEventHandler(bool isOvertime);

    // New simulation-driven unit events
    [Signal] public delegate void UnitAttackedEventHandler(int attackerUnitId, int targetUnitId);
    [Signal] public delegate void UnitDamagedEventHandler(int targetUnitId, int attackerUnitId, float damage, bool isCrit);
    [Signal] public delegate void UnitDiedSimEventHandler(int unitId, int killerUnitId);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        // Run before Unit3D._PhysicsProcess() so Tick() executes first each frame
        ProcessPriority = -100;
        // Keep ticking during pause so the host generates snapshots for client recovery
        ProcessMode = ProcessModeEnum.Always;
        Current = this;
        AddToGroup("simulation_node");
    }

    public override void _ExitTree()
    {
        if (Current == this)
            Current = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized || _simulation == null)
            return;

        // Only the host runs Tick(). Clients receive events/snapshots from the host.
        if (!IsHost)
            return;

        // Fixed timestep accumulator: run Tick() at exactly FIXED_DELTA intervals
        // regardless of Godot's physics frame rate.
        _accumulator += (float)delta;
        while (_accumulator >= FIXED_DELTA)
        {
            var events = _simulation.Tick(FIXED_DELTA);
            _accumulator -= FIXED_DELTA;
            EmitEvents(events);
            OnTickCompleted?.Invoke(events);
        }
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    /// <summary>
    /// Initialize the simulation with match configuration.
    /// Called by GameController3D during _ready().
    /// </summary>
    public void Initialize(float prepDuration, float matchDuration, string winCondition,
        float winConditionTimeLimit = 0f, int winConditionKillTarget = 0)
    {
        // Use match seed from MatchSession if in multiplayer, otherwise use a time-based seed
        long seed = MatchSession.Current?.Seed ?? System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        State = new MatchState
        {
            PrepTimeRemaining = prepDuration,
            WinCondition = winCondition,
            WinConditionTimeLimit = winConditionTimeLimit > 0 ? winConditionTimeLimit : matchDuration,
            WinConditionKillTarget = winConditionKillTarget,
            Phase = GamePhase.Preparation,
            Rng = new DeterministicRng(seed)
        };

        _simulation = new Simulation(State);
        _claimedSimUnitIds.Clear();
        _initialized = true;

        GD.Print($"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, timeLimit={State.WinConditionTimeLimit}s, killTarget={winConditionKillTarget}, seed={seed})");
    }

    /// <summary>
    /// Populate MatchState.CardDataMap with sim-local card data for all cards in both decks.
    /// Called after RegisterSummoner for both teams, before the first Tick.
    /// </summary>
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
        if (string.IsNullOrEmpty(catalogId) || processed.Contains(catalogId))
            return;
        processed.Add(catalogId);

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PrintErr($"[SimulationNode] Card not found in catalog: {catalogId}");
            return;
        }

        var simCard = new SimCardData
        {
            CatalogId = catalogId,
            ManaCost = card.ManaCost,
            SummonTime = card.Summon?.SummonTime ?? card.SummonTime,
            IsSpell = card.Type == CardType.Spell,
            ElementId = (int)card.ElementalAffinity
        };

        // Build spell effects for spell cards
        if (card.Type == CardType.Spell)
        {
            simCard.SpellTargetingMode = card.SpellTargeting switch
            {
                SpellTargeting.SingleTarget => SpellTargetingMode.NearestEnemy,
                SpellTargeting.AreaOfEffect => SpellTargetingMode.Position,
                SpellTargeting.SelectionRadius => SpellTargetingMode.AlliesInRadius,
                _ => SpellTargetingMode.Position
            };
            simCard.SpellRadius = card.SpellTargeting == SpellTargeting.SelectionRadius
                ? card.SelectionRadius
                : card.SpellRadius;

            if (card.SpellCategory == SpellCategory.Damage && card.SpellDamage > 0)
            {
                simCard.SpellEffects.Add(new SimSpellEffect
                {
                    EffectType = EffectType.Damage,
                    Value = card.SpellDamage,
                    DamageType = MapElementToDamageType(card.ElementalAffinity),
                    AoeRadius = card.SpellRadius,
                    Affinity = SpellAffinity.Enemies
                });
            }
        }

        // Build unit templates for summon cards
        if (card.Type == CardType.Summon)
        {
            if (card.Summon != null)
            {
                // New SummonSpec path (multi-unit cards)
                foreach (var entry in card.Summon.Units)
                {
                    var template = BuildUnitTemplate(entry.UnitId, entry.Count, entry.Modifier);
                    simCard.UnitTemplates.Add(template);
                }
            }
            else if (card.UnitId.HasValue)
            {
                // Single UnitId path
                var template = BuildUnitTemplate(card.UnitId, card.SpawnCount, card.UnitModifier);
                simCard.UnitTemplates.Add(template);
            }
            else
            {
                // Legacy: build from CardDefinition stats directly
                var stats = UnitStatCalculator.FromCardDefinition(card);
                var template = new SimUnitTemplate
                {
                    Count = card.SpawnCount,
                    MaxHp = stats.MaxHp,
                    AttackDamage = stats.AttackDamage,
                    AttackSpeed = stats.AttackSpeed,
                    MoveSpeed = stats.MoveSpeed,
                    AttackRange = stats.AttackRange,
                    AggroRadius = stats.AggroRadius,
                    CritChance = stats.CritChance,
                    CritDamage = stats.CritDamage,
                    UnitType = card.IsRanged ? 1 : 0,
                    ElementId = (int)card.ElementalAffinity,
                    PhysicalDefense = stats.Armor,
                    MagicDefense = stats.MagicResist
                };
                simCard.UnitTemplates.Add(template);
            }
        }

        State.CardDataMap[catalogId] = simCard;
    }

    private SimUnitTemplate BuildUnitTemplate(UnitId unitId, int count, ProjectSummoner.Systems.Modifiers.StatModifier? modifier)
    {
        var template = new SimUnitTemplate { Count = count };

        if (UnitDefinitions.TryGet(unitId, out var def) && def != null)
        {
            var stats = def.Stats;
            if (modifier != null)
                stats = stats.WithModifier(modifier);

            template.MaxHp = stats.MaxHp;
            template.AttackDamage = stats.AttackDamage;
            template.AttackSpeed = stats.AttackSpeed;
            template.MoveSpeed = stats.MoveSpeed;
            template.AttackRange = stats.AttackRange;
            template.AggroRadius = stats.AggroRadius;
            template.CritChance = stats.CritChance;
            template.CritDamage = stats.CritDamage;
            template.UnitType = def.UnitType == ProjectSummoner.Units.UnitType.Ranged ? 1 : 0;
            template.MovementLayer = (int)def.MovementLayer;
            template.ElementId = (int)(def.DamageProfile.Element ?? Element.Neutral);
            template.SeparationRadius = def.Visual.SeparationRadius;
            template.PhysicalDefense = stats.Armor;
            template.MagicDefense = stats.MagicResist;

            // Ranged config
            if (def.Ranged != null)
            {
                template.ProjectileDelay = def.Ranged.ProjectileDelay;
            }

            // Flying config
            if (def.Flying != null)
            {
                template.FlightAltitude = def.Flying.Altitude;
            }

            // Extract targeting config for sim
            var targetingConfig = def.Targeting.BuildConfig();
            template.FallbackMovement = (int)targetingConfig.FallbackMovement;

            // Extract scorer weights if available
            if (targetingConfig.Scorer is ProjectSummoner.Targeting.Scorers.CompositeScorer composite)
            {
                foreach (var scorer in composite.Scorers)
                {
                    if (scorer is ProjectSummoner.Targeting.Scorers.DistanceScorer ds)
                        template.DistanceScorerWeight = ds.Weight;
                    else if (scorer is ProjectSummoner.Targeting.Scorers.HealthScorer hs)
                        template.HealthScorerWeight = hs.Weight;
                }
            }

            // Extract cone constraint if available (may be direct or inside CompositeConstraint)
            ExtractConeConstraint(targetingConfig, template);

            // Extract layer filter (may be direct or inside CompositeTargetFilter)
            ExtractLayerFilter(targetingConfig, template);
        }

        return template;
    }

    private static void ExtractConeConstraint(TargetingConfig config, SimUnitTemplate template)
    {
        if (config.AttackConstraint is ProjectSummoner.Targeting.Constraints.ConeConstraint3D cone)
        {
            template.HasConeConstraint = true;
            template.ConeHalfAngle = cone.ConeHalfAngle;
            template.CloseRangeThreshold = cone.CloseRangeThreshold;
        }
        else if (config.AttackConstraint is ProjectSummoner.Targeting.Constraints.CompositeConstraint composite)
        {
            foreach (var c in composite.Constraints)
            {
                if (c is ProjectSummoner.Targeting.Constraints.ConeConstraint3D innerCone)
                {
                    template.HasConeConstraint = true;
                    template.ConeHalfAngle = innerCone.ConeHalfAngle;
                    template.CloseRangeThreshold = innerCone.CloseRangeThreshold;
                    break;
                }
            }
        }
    }

    private static void ExtractLayerFilter(TargetingConfig config, SimUnitTemplate template)
    {
        if (config.Filter is ProjectSummoner.Targeting.Filters.LayerTargetFilter layerFilter)
        {
            template.TargetLayerFilter = (int)layerFilter.CanTarget;
        }
        else if (config.Filter is ProjectSummoner.Targeting.Filters.CompositeTargetFilter composite)
        {
            foreach (var f in composite.Filters)
            {
                if (f is ProjectSummoner.Targeting.Filters.LayerTargetFilter innerLayer)
                {
                    template.TargetLayerFilter = (int)innerLayer.CanTarget;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Map an Element enum to the sim DamageType.
    /// Fire/Ice/Lightning etc. → Magic; Neutral → Physical.
    /// </summary>
    private static DamageType MapElementToDamageType(Element element)
    {
        return element == Element.Neutral ? DamageType.Physical : DamageType.Magic;
    }

    // =========================================================================
    // SUMMONER REGISTRATION (Phase 1)
    // =========================================================================

    /// <summary>
    /// Register a summoner's initial state in MatchState.
    /// Called by Summoner.gd during init().
    /// </summary>
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
        summoner.Team = networkTeam;
        summoner.CurrentHp = hp;
        summoner.MaxHp = maxHp;
        summoner.Mana = mana;
        summoner.MaxMana = maxMana;
        summoner.CastSpeed = castSpeed;
        summoner.IsAlive = true;
        summoner.MaxHandSize = maxHandSize;
        summoner.Position = new SimVector3(position.X, position.Y, position.Z);

        // Populate deck
        summoner.Deck.Clear();
        summoner.Deck.AddRange(deckCatalogIds);

        GD.Print($"[SimulationNode] Registered summoner team={networkTeam} (local={team}): HP={maxHp}, Mana={maxMana}, CastSpeed={castSpeed}, Deck={deckCatalogIds.Length} cards, Position={position}");
    }

    // =========================================================================
    // GDSCRIPT-CALLABLE ACCESSORS (read from MatchState)
    // =========================================================================

    public int GetPhase() => (int)State.Phase;
    public float GetPrepTimeRemaining() => State.PrepTimeRemaining;
    public float GetMatchTime() => State.MatchTime;
    public long GetFrameNumber() => State.FrameNumber;

    // Summoner accessors (callers pass local team, we convert to network)
    public float GetPlayerHp(int team) => State.Summoners[ToNetworkTeam(team)].CurrentHp;
    public float GetPlayerMaxHp(int team) => State.Summoners[ToNetworkTeam(team)].MaxHp;
    public float GetPlayerMana(int team) => State.Summoners[ToNetworkTeam(team)].Mana;
    public float GetPlayerMaxMana(int team) => State.Summoners[ToNetworkTeam(team)].MaxMana;
    public bool IsPlayerAlive(int team) => State.Summoners[ToNetworkTeam(team)].IsAlive;
    public bool IsPlayerCasting(int team) => State.Summoners[ToNetworkTeam(team)].IsCasting;
    public float GetPlayerCastSpeed(int team) => State.Summoners[ToNetworkTeam(team)].CastSpeed;
    public float GetCastingTimeRemaining(int team) => State.Summoners[ToNetworkTeam(team)].CastingTimeRemaining;
    public float GetCastingTimeTotal(int team) => State.Summoners[ToNetworkTeam(team)].CastingTimeTotal;

    /// <summary>
    /// Skip the preparation phase (debug tool). Sets prep time to 0 so next Tick() triggers transition.
    /// </summary>
    public void SkipPreparation()
    {
        if (State.Phase == GamePhase.Preparation)
        {
            State.PrepTimeRemaining = 0f;
        }
    }

    // Hand/Deck accessors (callers pass local team, we convert to network)
    public string[] GetPlayerHand(int team) => State.Summoners[ToNetworkTeam(team)].Hand.ToArray();
    public int GetPlayerHandSize(int team) => State.Summoners[ToNetworkTeam(team)].Hand.Count;
    public int GetPlayerDeckSize(int team) => State.Summoners[ToNetworkTeam(team)].Deck.Count;
    public int GetPlayerDiscardSize(int team) => State.Summoners[ToNetworkTeam(team)].DiscardPile.Count;

    // =========================================================================
    // HAND / DECK MANAGEMENT
    // =========================================================================

    /// <summary>
    /// Set the initial hand for a summoner in MatchState.
    /// Called after RegisterSummoner once the hand has been drawn.
    /// </summary>
    public void SetSummonerHand(int team, string[] handCatalogIds)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.Hand.Clear();
        summoner.Hand.AddRange(handCatalogIds);
    }

    // =========================================================================
    // VISUAL UNIT LINKING (connects Unit3D to sim UnitData)
    // =========================================================================

    /// <summary>
    /// Claim the next unclaimed sim unit ID for a given team.
    /// Called by Unit3D._Ready() to link itself to its simulation data.
    /// Units are claimed in UnitId order (deterministic, matches spawn order).
    /// </summary>
    public int ClaimNextSimUnitId(int localTeam)
    {
        int networkTeam = ToNetworkTeam(localTeam);

        foreach (var kvp in State.Units.OrderBy(kv => kv.Key))
        {
            var unit = kvp.Value;
            if (unit.Team == networkTeam && !_claimedSimUnitIds.Contains(unit.UnitId))
            {
                _claimedSimUnitIds.Add(unit.UnitId);
                return unit.UnitId;
            }
        }

        return -1;
    }

    // =========================================================================
    // UNIT STATE (read-only accessors)
    // =========================================================================

    /// <summary>
    /// Get a unit's HP from MatchState.
    /// Returns -1 if unitId is not found.
    /// </summary>
    public float GetUnitHp(int unitId)
    {
        return State.Units.TryGetValue(unitId, out var unit) ? unit.CurrentHp : -1f;
    }

    /// <summary>
    /// Get whether a unit is alive from MatchState.
    /// Returns false if unitId is not found.
    /// </summary>
    public bool GetUnitIsAlive(int unitId)
    {
        return State.Units.TryGetValue(unitId, out var unit) && unit.IsAlive;
    }

    /// <summary>
    /// Get the UnitData for a unit. Used by Unit3D to read simulation state.
    /// Returns null if unitId is not found.
    /// </summary>
    public UnitData? GetUnitData(int unitId)
    {
        return State.Units.TryGetValue(unitId, out var unit) ? unit : null;
    }

    /// <summary>
    /// Get the current kill count from MatchState.
    /// </summary>
    public int GetKillCount()
    {
        return State.KillCount;
    }

    // =========================================================================
    // COMMAND QUEUE
    // =========================================================================

    /// <summary>
    /// Queue a card play command. Called by Summoner.gd or AuthorityBridge.
    /// </summary>
    public void QueuePlayCard(int team, int cardIndex, Vector3 spawnPosition, int networkId = -1)
    {
        var cmd = new PlayCardCommand(ToNetworkTeam(team), cardIndex, ToSimCanonical(spawnPosition), networkId);
        SubmitCommand(cmd);
    }

    /// <summary>
    /// Submit a command to the simulation. Stamps ExecuteFrame and adds to PendingCommandBuffer.
    /// This is the ONLY entry point for external code to enqueue commands.
    /// </summary>
    public void SubmitCommand(ICommand cmd)
    {
        cmd.ExecuteFrame = State.FrameNumber + 1;
        State.PendingCommandBuffer.Add(cmd);
    }

    // =========================================================================
    // SNAPSHOT APPLICATION (client-side, driven by host snapshots)
    // =========================================================================

    /// <summary>
    /// Apply a state snapshot from the host to the client's MatchState.
    /// Updates frame/time, phase, summoner state, and unit corrections.
    /// Emits signals so the presentation layer stays in sync.
    /// </summary>
    public void ApplySnapshot(StateSnapshot snapshot)
    {
        // Update frame/time
        State.FrameNumber = snapshot.Frame;
        State.MatchTime = snapshot.MatchTime;

        // Phase transition
        var newPhase = (GamePhase)snapshot.Phase;
        if (State.Phase != newPhase)
        {
            State.Phase = newPhase;
            EmitSignal(SignalName.PhaseChanged, snapshot.Phase);
        }

        // Prep timer
        State.PrepTimeRemaining = snapshot.PrepTimeRemaining;
        EmitSignal(SignalName.PrepTimerUpdated, snapshot.PrepTimeRemaining);
        EmitSignal(SignalName.MatchTimeUpdated, snapshot.MatchTime);

        // Summoner states — snapshot uses network team, MatchState uses network team
        foreach (var ss in snapshot.Summoners)
        {
            int networkTeam = ss.Team;
            if (networkTeam < 0 || networkTeam > 1) continue;
            var summoner = State.Summoners[networkTeam];
            int localTeam = ToLocalTeam(networkTeam);

            // HP — emit with localTeam so GDScript signal handlers match
            if (Mathf.Abs(summoner.CurrentHp - ss.Hp) > 0.1f)
            {
                summoner.CurrentHp = ss.Hp;
                summoner.MaxHp = ss.MaxHp;
                summoner.IsAlive = ss.Hp > 0;
                EmitSignal(SignalName.SummonerHpChanged, localTeam, ss.Hp, ss.MaxHp);
            }

            // Mana — emit with localTeam
            if (Mathf.Abs(summoner.Mana - ss.Mana) > 0.1f)
            {
                summoner.Mana = ss.Mana;
                summoner.MaxMana = ss.MaxMana;
                EmitSignal(SignalName.SummonerManaChanged, localTeam, ss.Mana, ss.MaxMana);
            }

            // Casting state
            summoner.IsCasting = ss.IsCasting;
            summoner.CastingTimeRemaining = ss.CastingTimeRemaining;
            summoner.CastingTimeTotal = ss.CastingTimeTotal;
            summoner.CastingCardIndex = ss.CastingCardIndex;
            summoner.CastingSpawnPosition = new SimVector3(ss.CastingSpawnPosition.X, ss.CastingSpawnPosition.Y, ss.CastingSpawnPosition.Z);
            summoner.CastingNetworkId = ss.CastingNetworkId;
        }

        // Overtime
        if (State.IsOvertime != snapshot.IsOvertime)
        {
            State.IsOvertime = snapshot.IsOvertime;
            EmitSignal(SignalName.OvertimeChanged, snapshot.IsOvertime);
        }

        // Unit state corrections (position, HP, targeting, activation)
        var snapshotNetworkIds = new System.Collections.Generic.HashSet<int>(snapshot.Units.Length);
        foreach (var unitState in snapshot.Units)
        {
            snapshotNetworkIds.Add(unitState.NetworkId);

            foreach (var kvp in State.Units)
            {
                if (kvp.Value.NetworkId != unitState.NetworkId) continue;
                var unit = kvp.Value;

                var snapshotSimPos = new SimVector3(unitState.Position.X, unitState.Position.Y, unitState.Position.Z);
                if (unit.Position.DistanceTo(snapshotSimPos) > 0.5f)
                    unit.Position = snapshotSimPos;
                // Always write HP/alive from host snapshot to MatchState.
                // Combat runs independently on both sides, so any tolerance
                // creates hash mismatches (quantized at 0.1 HP precision).
                unit.CurrentHp = unitState.Hp;
                unit.IsAlive = unitState.IsAlive;
                unit.TargetNetworkId = unitState.TargetNetworkId;
                unit.ActivationState = unitState.ActivationState;
                break;
            }
        }

        // Mark any local alive units NOT in the snapshot as dead.
        // Safety net for lost/delayed UnitDied messages — the host's snapshot
        // omits dead units, so missing entries mean the unit died on the host.
        foreach (var kvp in State.Units)
        {
            if (kvp.Value.IsAlive && !snapshotNetworkIds.Contains(kvp.Value.NetworkId))
            {
                kvp.Value.IsAlive = false;
                kvp.Value.CurrentHp = 0;
            }
        }
    }

    // =========================================================================
    // EVENT EMISSION
    // =========================================================================

    private void EmitEvents(System.Collections.Generic.List<SimEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case PhaseChangedEvent e:
                    EmitSignal(SignalName.PhaseChanged, (int)e.NewPhase);
                    break;
                case PrepTimerUpdatedEvent e:
                    EmitSignal(SignalName.PrepTimerUpdated, e.Remaining);
                    break;
                case MatchTimeUpdatedEvent e:
                    EmitSignal(SignalName.MatchTimeUpdated, e.MatchTime);
                    break;
                case SummonerHpChangedEvent e:
                    EmitSignal(SignalName.SummonerHpChanged, ToLocalTeam(e.Team), e.Hp, e.MaxHp);
                    break;
                case SummonerManaChangedEvent e:
                    EmitSignal(SignalName.SummonerManaChanged, ToLocalTeam(e.Team), e.Mana, e.MaxMana);
                    break;
                case CastingStartedEvent e:
                    EmitSignal(SignalName.CastingStarted, ToLocalTeam(e.Team), e.CardIndex, e.Duration, ToLocal(e.SpawnPosition), e.CatalogId);
                    break;
                case CastingCompletedEvent e:
                    EmitSignal(SignalName.CastingCompleted, ToLocalTeam(e.Team), e.CardIndex, ToLocal(e.SpawnPosition), e.NetworkId);
                    break;
                case UnitActivationChangedEvent:
                    // No signal for this yet — will be added if needed
                    break;
                case CardDrawnEvent e:
                    EmitSignal(SignalName.CardDrawn, ToLocalTeam(e.Team), e.HandIndex, e.CatalogId);
                    break;
                case HandChangedEvent e:
                    EmitSignal(SignalName.HandChanged, ToLocalTeam(e.Team), e.Hand);
                    break;
                case DeckRecycledEvent e:
                    EmitSignal(SignalName.DeckRecycled, ToLocalTeam(e.Team));
                    break;
                case UnitRegisteredEvent e:
                    EmitSignal(SignalName.UnitStateRegistered, e.UnitId, e.NetworkId, ToLocalTeam(e.Team));
                    break;
                case UnitRemovedEvent e:
                    EmitSignal(SignalName.UnitStateRemoved, e.UnitId);
                    break;
                case GameOverEvent e:
                    EmitSignal(SignalName.GameOver, ToLocalTeam(e.WinnerTeam), e.Reason);
                    break;
                case UnitAttackedEvent e:
                    EmitSignal(SignalName.UnitAttacked, e.AttackerUnitId, e.TargetUnitId);
                    break;
                case UnitDamagedEvent e:
                    EmitSignal(SignalName.UnitDamaged, e.TargetUnitId, e.AttackerUnitId, e.Damage, e.IsCrit);
                    break;
                case UnitDiedSimEvent e:
                    EmitSignal(SignalName.UnitDiedSim, e.UnitId, e.KillerUnitId);
                    break;
            }
        }
    }
}
