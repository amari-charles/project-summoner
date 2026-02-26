using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;

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
    /// Convert a Godot.Vector3 to SimVector3 in canonical coordinates.
    /// </summary>
    private SimVector3 ToSimCanonical(Vector3 localPos)
    {
        var c = CoordinateTransform.LocalToCanonical(localPos);
        return new SimVector3(c.X, c.Y, c.Z);
    }

    // =========================================================================
    // SIGNALS (emitted after Tick, consumed by presentation layer)
    // =========================================================================

    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);
    [Signal] public delegate void PrepTimerUpdatedEventHandler(float remaining);
    [Signal] public delegate void MatchTimeUpdatedEventHandler(float matchTime);
    [Signal] public delegate void SummonerHpChangedEventHandler(int team, float hp, float maxHp);
    [Signal] public delegate void SummonerManaChangedEventHandler(int team, float mana, float maxMana);
    [Signal] public delegate void CastingStartedEventHandler(int team, int cardIndex, float duration, Vector3 spawnPosition);
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
        }
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    /// <summary>
    /// Initialize the simulation with match configuration.
    /// Called by GameController3D during _ready().
    /// </summary>
    public void Initialize(float prepDuration, float matchDuration, string winCondition)
    {
        // Use match seed from MatchSession if in multiplayer, otherwise use a time-based seed
        long seed = MatchSession.Current?.Seed ?? System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        State = new MatchState
        {
            PrepTimeRemaining = prepDuration,
            WinCondition = winCondition,
            Phase = GamePhase.Preparation,
            Rng = new DeterministicRng(seed)
        };

        _simulation = new Simulation(State);
        _initialized = true;

        GD.Print($"[SimulationNode] Initialized (prep={prepDuration}s, winCondition={winCondition}, seed={seed})");
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

    /// <summary>
    /// Sync the MatchState hand after a card play (remove played card, add drawn card).
    /// Called by Summoner.gd after _complete_card_play() manages the Card resources.
    /// </summary>
    public void SyncHandAfterCardPlay(int team, string[] newHand, string discardedCatalogId)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.Hand.Clear();
        summoner.Hand.AddRange(newHand);
        summoner.DiscardPile.Add(discardedCatalogId);
    }

    /// <summary>
    /// Sync deck recycle to MatchState.
    /// Called by Summoner.gd after _recycle_discard_pile().
    /// </summary>
    public void SyncDeckRecycle(int team, string[] newDeck)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.Deck.Clear();
        summoner.Deck.AddRange(newDeck);
        summoner.DiscardPile.Clear();
    }

    /// <summary>
    /// Sync a single card draw to MatchState (remove from deck front, add to hand).
    /// Called by Summoner.gd after draw_card().
    /// </summary>
    public void SyncCardDraw(int team, string catalogId)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        if (summoner.Deck.Count > 0)
            summoner.Deck.RemoveAt(0);
        summoner.Hand.Add(catalogId);
    }

    /// <summary>
    /// Set summoner stats (maxHp, maxMana, castSpeed) in MatchState.
    /// Called by GDScript (multiplayer_game_bridge) when a SummonerExchange is received.
    /// </summary>
    public void SetSummonerStats(int team, float maxHp, float maxMana, float castSpeed)
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1) return;
        var summoner = State.Summoners[networkTeam];

        summoner.MaxHp = maxHp;
        summoner.CurrentHp = maxHp;
        summoner.MaxMana = maxMana;
        summoner.Mana = maxMana;
        summoner.CastSpeed = castSpeed;
        summoner.IsAlive = true;

        EmitSignal(SignalName.SummonerHpChanged, team, maxHp, maxHp);
        EmitSignal(SignalName.SummonerManaChanged, team, maxMana, maxMana);

        GD.Print($"[SimulationNode] SetSummonerStats team={networkTeam} (local={team}): HP={maxHp}, Mana={maxMana}, CastSpeed={castSpeed}");
    }

    /// <summary>
    /// Set summoner damage bonuses in MatchState.
    /// Called by GDScript to sync summoner trait bonuses for SimDamage calculations.
    /// </summary>
    public void SetSummonerBonuses(int team, float damageBonus, float damageReduction, int elementId)
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1) return;
        var summoner = State.Summoners[networkTeam];
        summoner.DamageBonus = damageBonus;
        summoner.DamageReduction = damageReduction;
        summoner.ElementId = elementId;
    }

    // =========================================================================
    // CASTING STATE
    // =========================================================================

    /// <summary>
    /// Start casting for a summoner. Writes to MatchState so Simulation.Tick() can decrement the timer.
    /// Called by Summoner.gd when a card play begins casting.
    /// </summary>
    public void StartCasting(int team, int cardIndex, float duration, Vector3 spawnPosition, int networkId = -1)
    {
        var summoner = State.Summoners[ToNetworkTeam(team)];
        summoner.IsCasting = true;
        summoner.CastingTimeRemaining = duration;
        summoner.CastingTimeTotal = duration;
        summoner.CastingCardIndex = cardIndex;
        summoner.CastingSpawnPosition = ToSimCanonical(spawnPosition);
        summoner.CastingNetworkId = networkId;
    }

    // =========================================================================
    // MUTATION METHODS (write to MatchState, emit signals)
    // These are called by external systems (DamageSystem, Unit3D) that mutate state
    // outside of Tick() — the intentional "second mutation path" for physics/combat.
    // =========================================================================

    /// <summary>
    /// Apply damage to a summoner. Called by DamageSystem when a summoner takes damage.
    /// </summary>
    public void ApplySummonerDamage(int team, float amount)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];
        if (!summoner.IsAlive) return;

        summoner.CurrentHp -= amount;
        if (summoner.CurrentHp < 0) summoner.CurrentHp = 0;

        EmitSignal(SignalName.SummonerHpChanged, team, summoner.CurrentHp, summoner.MaxHp);

        if (summoner.CurrentHp <= 0)
        {
            summoner.IsAlive = false;
        }
    }

    /// <summary>
    /// Deduct mana from a summoner. Returns true if sufficient mana was available.
    /// </summary>
    public bool DeductMana(int team, float amount)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];
        if (summoner.Mana < amount) return false;

        summoner.Mana -= amount;
        EmitSignal(SignalName.SummonerManaChanged, team, summoner.Mana, summoner.MaxMana);
        return true;
    }

    /// <summary>
    /// Set summoner HP directly (for desync corrections).
    /// </summary>
    public void ForceSetSummonerHp(int team, float hp)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];
        summoner.CurrentHp = hp;
        summoner.IsAlive = hp > 0;
        EmitSignal(SignalName.SummonerHpChanged, team, summoner.CurrentHp, summoner.MaxHp);
    }

    /// <summary>
    /// Set summoner mana directly (for desync corrections).
    /// </summary>
    public void ForceSetSummonerMana(int team, float mana)
    {
        int networkTeam = ToNetworkTeam(team);
        var summoner = State.Summoners[networkTeam];
        summoner.Mana = mana;
        EmitSignal(SignalName.SummonerManaChanged, team, summoner.Mana, summoner.MaxMana);
    }

    /// <summary>
    /// Set summoner max HP directly. Called by Summoner.gd setter.
    /// </summary>
    public void ForceSetSummonerMaxHp(int team, float maxHp)
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1) return;
        State.Summoners[networkTeam].MaxHp = maxHp;
    }

    /// <summary>
    /// Set summoner max mana directly. Called by Summoner.gd setter.
    /// </summary>
    public void ForceSetSummonerMaxMana(int team, float maxMana)
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1) return;
        State.Summoners[networkTeam].MaxMana = maxMana;
    }

    // =========================================================================
    // UNIT STATE (Phase 5 will populate these)
    // =========================================================================

    /// <summary>
    /// Register a unit in MatchState. Called by Unit3D._Ready().
    /// Returns the MatchState-local unit ID.
    /// </summary>
    public int RegisterUnit(int networkId, string catalogId, int team, float hp, float maxHp,
        float attackDamage, float attackSpeed, float moveSpeed, float attackRange, Vector3 position,
        int elementId = 0)
    {
        int networkTeam = ToNetworkTeam(team);
        var canonicalPos = ToSimCanonical(position);

        int unitId = State.NextUnitId();
        State.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            NetworkId = networkId,
            CatalogId = catalogId,
            Team = networkTeam,
            CurrentHp = hp,
            MaxHp = maxHp,
            IsAlive = true,
            Position = canonicalPos,
            AttackDamage = attackDamage,
            AttackSpeed = attackSpeed,
            MoveSpeed = moveSpeed,
            AttackRange = attackRange,
            ElementId = elementId
        };

        EmitSignal(SignalName.UnitStateRegistered, unitId, networkId, team);
        return unitId;
    }

    /// <summary>
    /// Sync unit position from Godot physics. Called by Unit3D after MoveAndSlide().
    /// </summary>
    public void SyncUnitPosition(int unitId, Vector3 position)
    {
        if (State.Units.TryGetValue(unitId, out var unit))
        {
            unit.Position = ToSimCanonical(position);
        }
    }

    /// <summary>
    /// Apply damage to a unit in MatchState. Called by DamageSystem.
    /// Returns true if the unit was killed.
    /// </summary>
    public bool ApplyUnitDamage(int unitId, float amount)
    {
        if (!State.Units.TryGetValue(unitId, out var unit)) return false;
        if (!unit.IsAlive) return false;

        unit.CurrentHp -= amount;
        if (unit.CurrentHp <= 0)
        {
            unit.CurrentHp = 0;
            unit.IsAlive = false;
            return true;
        }
        return false;
    }

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
    /// Force-set a unit's HP in MatchState. Used for desync corrections.
    /// </summary>
    public void ForceSetUnitHp(int unitId, float hp)
    {
        if (State.Units.TryGetValue(unitId, out var unit))
        {
            unit.CurrentHp = hp;
            unit.IsAlive = hp > 0;
        }
    }

    /// <summary>
    /// Overwrite card state in MatchState for a summoner. Used for DeckSync desync correction.
    /// </summary>
    public void ApplyDeckSync(int team, string[] deck, string[] hand, string[] discard)
    {
        int networkTeam = ToNetworkTeam(team);
        if (networkTeam < 0 || networkTeam > 1) return;
        var summoner = State.Summoners[networkTeam];
        summoner.Deck.Clear();
        summoner.Deck.AddRange(deck);
        summoner.Hand.Clear();
        summoner.Hand.AddRange(hand);
        summoner.DiscardPile.Clear();
        summoner.DiscardPile.AddRange(discard);
        GD.Print($"[SimulationNode] Applied DeckSync for team {networkTeam} (local={team}): Deck={deck.Length}, Hand={hand.Length}, Discard={discard.Length}");
    }

    /// <summary>
    /// Sync a unit's current target to MatchState. Called by Unit3D when targeting changes.
    /// </summary>
    public void SyncUnitTarget(int unitId, int? targetNetworkId)
    {
        if (State.Units.TryGetValue(unitId, out var unit))
        {
            unit.TargetNetworkId = targetNetworkId;
        }
    }

    /// <summary>
    /// Remove a unit from MatchState. Called when unit is freed.
    /// </summary>
    public void RemoveUnit(int unitId)
    {
        if (State.Units.Remove(unitId))
        {
            EmitSignal(SignalName.UnitStateRemoved, unitId);
        }
    }

    /// <summary>
    /// Increment the kill count in MatchState. Called by game controller on enemy kill.
    /// </summary>
    public void IncrementKillCount()
    {
        State.KillCount++;
    }

    /// <summary>
    /// Get the current kill count from MatchState.
    /// </summary>
    public int GetKillCount()
    {
        return State.KillCount;
    }

    /// <summary>
    /// Set the winner team in MatchState. Called by game controller on game end.
    /// </summary>
    public void SetWinnerTeam(int team)
    {
        State.WinnerTeam = ToNetworkTeam(team);
        State.Phase = GamePhase.GameOver;
    }

    /// <summary>
    /// Set the overtime flag in MatchState.
    /// </summary>
    public void SetOvertime(bool isOvertime)
    {
        if (State.IsOvertime != isOvertime)
        {
            State.IsOvertime = isOvertime;
            EmitSignal(SignalName.OvertimeChanged, isOvertime);
        }
    }

    /// <summary>
    /// Get the overtime flag from MatchState.
    /// </summary>
    public bool GetIsOvertime()
    {
        return State.IsOvertime;
    }

    /// <summary>
    /// Sync a unit's activation state to MatchState. Called by Unit3D on Activate/Deactivate.
    /// </summary>
    public void SyncUnitActivationState(int unitId, int activationState)
    {
        if (State.Units.TryGetValue(unitId, out var unit))
        {
            unit.ActivationState = activationState;
        }
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
    /// Queue a forfeit command.
    /// </summary>
    public void QueueForfeit(int team)
    {
        var cmd = new ForfeitCommand(ToNetworkTeam(team));
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
                    EmitSignal(SignalName.CastingStarted, ToLocalTeam(e.Team), e.CardIndex, e.Duration, ToLocal(e.SpawnPosition));
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
