using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Projectiles;
using Fateforged.Projectiles;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Fateforged.Simulation.Movement;
using Fateforged.Simulation.Spatial;
using Fateforged.Simulation.Subsystems;
using Fateforged.Stats;
using Fateforged.Units;

namespace Fateforged.Simulation;

/// <summary>
/// Pure deterministic simulation. All state mutations go through Tick().
/// No Godot node dependencies — operates on MatchState data only.
///
/// Tick Order Contract (12 steps):
///   1. Increment frame, advance match time (Battle only)
///   1.5. Tick AI (produces PlayCardCommands into PendingCommandBuffer)
///   2. Drain and execute due commands
///   3. Phase timers / transitions (prep→battle: activate units, refresh hands)
///   4. Tick casting (decrement timers, handle completions, replacement draws)
///   5. Tick units (cooldowns → targeting → behavior → movement → pending melee damage)
///   6. Tick projectiles
///   7. Tick effects (buffs: decrement, periodic, remove expired)
///   8. Tick delayed effects (death explosions, timed AoE)
///   9. Death cleanup (DeathCleanupTimer countdown, remove expired, emit UnitRemovedEvent)
///  10. Evaluate win conditions
///  11. Return events
/// </summary>
public class Simulation
{
    public const float FixedDeltaSeconds = 1.0f / 60.0f;

    private readonly MatchState _state;
    private IWinCondition? _winCondition;

    /// <summary>
    /// Optional logging delegate. Set by SimulationNode to route logs to Godot.GD.Print().
    /// Keeps simulation files free of Godot dependencies.
    /// </summary>
    public static Action<string>? Log { get; set; }

    public Simulation(MatchState state)
    {
        _state = state;
    }

    /// <summary>
    /// Advance the simulation by one fixed timestep.
    /// Returns a list of events that occurred during this tick for SimulationNode to emit as signals.
    /// </summary>
    public List<SimEvent> Tick(float fixedDelta)
    {
        var events = new List<SimEvent>();

        if (_state.Phase == GamePhase.GameOver)
            return events;

        // Step 1: Increment frame, advance match time (Battle only)
        _state.FrameNumber++;
        if (_state.Phase == GamePhase.Battle)
        {
            _state.MatchTime += fixedDelta;
            events.Add(new MatchTimeUpdatedEvent(_state.MatchTime));
        }

        // Step 1.5: Tick AI (produces PlayCardCommands into PendingCommandBuffer)
        SimAi.Tick(_state, fixedDelta);

        // Step 2: Drain and execute due commands
        DrainCommands(events);

        // Step 3: Phase timers / transitions
        TickPhaseTransitions(fixedDelta, events);

        // Step 4: Tick casting
        TickCasting(fixedDelta, events);

        // Step 5: Tick spawn timers — activate units whose spawn timer expired
        if (_state.Phase == GamePhase.Battle)
        {
            TickSpawnTimers(fixedDelta);
        }

        // Step 6: Tick units (only during Battle)
        if (_state.Phase == GamePhase.Battle)
        {
            TickUnits(fixedDelta, events);
        }

        // Step 6.5: Tick simulation-owned unit abilities (only during Battle)
        if (_state.Phase == GamePhase.Battle)
        {
            SimAbilityOrchestrator.Tick(_state, fixedDelta, events);
        }

        // Step 7: Tick projectiles (only during Battle)
        if (_state.Phase == GamePhase.Battle)
        {
            SimProjectile.TickAll(_state, fixedDelta, events);
        }

        // Step 8: Tick effects (buffs, periodic triggers, HP threshold triggers)
        if (_state.Phase == GamePhase.Battle)
        {
            SimEffects.TickBuffs(_state, fixedDelta, events);
        }

        // Step 9: Tick delayed effects (death explosions, timed AoE)
        if (_state.Phase == GamePhase.Battle)
        {
            SimEffects.TickDelayedEffects(_state, fixedDelta, events);
        }

        // Step 10: Death cleanup
        TickDeathCleanup(fixedDelta, events);

        // Step 11: Evaluate win conditions (Battle phase only)
        if (_state.Phase == GamePhase.Battle)
        {
            EvaluateWinConditions(events);
        }

        // Step 12: Return events
        return events;
    }

    /// <summary>
    /// Drain PendingCommandBuffer: execute commands where ExecuteFrame <= FrameNumber.
    /// Sort by (ExecuteFrame, Team, Sequence) for deterministic ordering.
    /// </summary>
    private void DrainCommands(List<SimEvent> events)
    {
        if (_state.PendingCommandBuffer.Count == 0)
            return;

        // Partition: due commands vs future commands
        var dueCommands = new List<ICommand>();
        var futureCommands = new List<ICommand>();

        foreach (var cmd in _state.PendingCommandBuffer)
        {
            if (cmd.ExecuteFrame <= _state.FrameNumber)
                dueCommands.Add(cmd);
            else
                futureCommands.Add(cmd);
        }

        _state.PendingCommandBuffer.Clear();
        _state.PendingCommandBuffer.AddRange(futureCommands);

        if (dueCommands.Count == 0)
            return;

        // Sort due commands by (ExecuteFrame, Team, Sequence)
        dueCommands.Sort(
            (a, b) =>
            {
                int cmp = a.ExecuteFrame.CompareTo(b.ExecuteFrame);
                if (cmp != 0)
                    return cmp;

                // Extract team for ordering
                int teamA =
                    a is PlayCardCommand pcA ? pcA.Team
                    : a is SpawnUnitCommand suA ? suA.Team
                    : (a is ForfeitCommand fcA ? fcA.Team : 0);
                int teamB =
                    b is PlayCardCommand pcB ? pcB.Team
                    : b is SpawnUnitCommand suB ? suB.Team
                    : (b is ForfeitCommand fcB ? fcB.Team : 0);
                cmp = teamA.CompareTo(teamB);
                if (cmp != 0)
                    return cmp;

                // Extract sequence for ordering
                int seqA = a is PlayCardCommand pcA2 ? pcA2.Sequence : 0;
                int seqB = b is PlayCardCommand pcB2 ? pcB2.Sequence : 0;
                return seqA.CompareTo(seqB);
            }
        );

        // Execute each due command
        foreach (var cmd in dueCommands)
        {
            ExecuteCommand(cmd, events);
        }
    }

    /// <summary>
    /// Execute a single command. Validates and applies state changes.
    /// </summary>
    private void ExecuteCommand(ICommand cmd, List<SimEvent> events)
    {
        switch (cmd)
        {
            case PlayCardCommand playCard:
                ExecutePlayCard(playCard, events);
                break;

            case SpawnUnitCommand spawn:
                ExecuteSpawnUnit(spawn, events);
                break;

            case ForfeitCommand forfeit:
                int winnerTeam = MatchState.GetEnemyTeam(forfeit.Team);
                _state.WinnerTeam = winnerTeam;
                _state.Phase = GamePhase.GameOver;
                events.Add(new GameOverEvent(winnerTeam, "Forfeit"));
                break;

            default:
                Log?.Invoke($"[Simulation] Unknown command type: {cmd.GetType().Name}");
                break;
        }
    }

    /// <summary>
    /// Execute a PlayCardCommand: validate, deduct mana, spawn units, manage hand, start casting.
    /// Units spawn immediately (matching current game behavior where units appear with reveal effect).
    /// Hand management (discard + draw) also happens immediately.
    /// Casting timer just locks the player from playing another card.
    /// </summary>
    private void ExecutePlayCard(PlayCardCommand cmd, List<SimEvent> events)
    {
        var summoner = _state.Summoners[cmd.Team];

        // Validate: summoner alive and not already casting
        if (!summoner.IsAlive || summoner.IsCasting)
        {
            Log?.Invoke(
                $"[Simulation] PlayCard rejected: team={cmd.Team} alive={summoner.IsAlive} casting={summoner.IsCasting}"
            );
            return;
        }

        // Validate: card index in bounds
        if (cmd.CardIndex < 0 || cmd.CardIndex >= summoner.Hand.Count)
        {
            Log?.Invoke(
                $"[Simulation] PlayCard rejected: team={cmd.Team} cardIndex={cmd.CardIndex} out of bounds (hand size={summoner.Hand.Count})"
            );
            return;
        }

        // Look up card data
        var catalogId = summoner.Hand[cmd.CardIndex];
        if (!_state.CardDataMap.TryGetValue(catalogId, out var cardData))
        {
            Log?.Invoke(
                $"[Simulation] PlayCard rejected: team={cmd.Team} catalogId={catalogId} not found in CardDataMap"
            );
            return;
        }

        // Validate: spells only during Battle phase
        if (cardData.IsSpell && _state.Phase != GamePhase.Battle)
        {
            Log?.Invoke(
                $"[Simulation] PlayCard rejected: team={cmd.Team} spell={catalogId} not allowed during {_state.Phase}"
            );
            return;
        }

        // Validate: enough mana
        if (summoner.Mana < cardData.ManaCost)
        {
            Log?.Invoke(
                $"[Simulation] PlayCard rejected: team={cmd.Team} mana={summoner.Mana} < cost={cardData.ManaCost} for {catalogId}"
            );
            return;
        }

        // Deduct mana
        summoner.Mana -= cardData.ManaCost;
        events.Add(new SummonerManaChangedEvent(cmd.Team, summoner.Mana, summoner.MaxMana));

        // Calculate effective summon time (apply cast speed)
        float effectiveSummonTime =
            summoner.CastSpeed > 0 ? cardData.SummonTime / summoner.CastSpeed : cardData.SummonTime;

        var playedRuntimeRef = GetHandCardRef(summoner, cmd.CardIndex, catalogId);

        // Start casting (locks player from playing another card)
        summoner.IsCasting = true;
        summoner.CastingTimeRemaining = effectiveSummonTime;
        summoner.CastingTimeTotal = effectiveSummonTime;
        summoner.CastingCardIndex = cmd.CardIndex;
        summoner.CastingCatalogId = catalogId;
        summoner.CastingCardInstanceId = playedRuntimeRef.InstanceId;
        summoner.CastingSpawnPosition = cmd.SpawnPosition;
        summoner.CastingNetworkId = cmd.NetworkId;

        events.Add(
            new CastingStartedEvent(
                cmd.Team,
                cmd.CardIndex,
                effectiveSummonTime,
                cmd.SpawnPosition,
                catalogId
            )
        );

        // Execute card effect
        if (cardData.IsSpell)
        {
            ExecuteSpellEffects(cardData, cmd.Team, cmd.SpawnPosition, cmd.TargetUnitId, events);
        }
        else
        {
            SpawnUnitsFromCard(
                cardData,
                cmd.Team,
                cmd.SpawnPosition,
                effectiveSummonTime,
                events,
                statOverrides: null,
                castingCardInstanceId: summoner.CastingCardInstanceId
            );
        }

        // Hand management: remove played card, discard, draw replacement
        var playedCatalogId = summoner.Hand[cmd.CardIndex];
        summoner.Hand.RemoveAt(cmd.CardIndex);
        if (cmd.CardIndex >= 0 && cmd.CardIndex < summoner.HandRefs.Count)
            summoner.HandRefs.RemoveAt(cmd.CardIndex);
        summoner.DiscardPile.Add(playedCatalogId);
        summoner.DiscardRefs.Add(playedRuntimeRef);
        DrawReplacementCard(summoner, cmd.CardIndex, events);

        // If hand and deck are both empty, recycle discard pile
        if (summoner.Hand.Count == 0 && summoner.Deck.Count == 0 && summoner.DiscardPile.Count > 0)
        {
            RecycleDeck(summoner);
            events.Add(new DeckRecycledEvent((int)summoner.Team));

            for (int j = 0; j < summoner.MaxHandSize && summoner.Deck.Count > 0; j++)
            {
                DrawTopDeckCardIntoHand(summoner, summoner.Hand.Count, events: null);
            }
        }

        events.Add(new HandChangedEvent((int)summoner.Team, ToCatalogIdStrings(summoner.Hand)));
    }

    /// <summary>
    /// Execute a SpawnUnitCommand: look up card data, spawn units directly.
    /// No mana cost, no casting, no hand management.
    /// </summary>
    private void ExecuteSpawnUnit(SpawnUnitCommand cmd, List<SimEvent> events)
    {
        if (!_state.CardDataMap.TryGetValue(cmd.CatalogId, out var cardData))
        {
            Log?.Invoke(
                $"[Simulation] SpawnUnit rejected: catalogId={cmd.CatalogId} not found in CardDataMap"
            );
            return;
        }

        if (cmd.Team < 0 || cmd.Team > 1)
        {
            Log?.Invoke($"[Simulation] SpawnUnit rejected: invalid team={cmd.Team}");
            return;
        }

        float spawnTimer = cmd.ActivateImmediately ? 0f : cardData.SummonTime;
        SpawnUnitsFromCard(
            cardData,
            cmd.Team,
            cmd.SpawnPosition,
            spawnTimer,
            events,
            cmd.StatOverrides
        );
    }

    /// <summary>
    /// Handle phase timers and transitions.
    /// </summary>
    private void TickPhaseTransitions(float fixedDelta, List<SimEvent> events)
    {
        if (_state.Phase == GamePhase.Preparation)
        {
            _state.PrepTimeRemaining -= fixedDelta;
            events.Add(new PrepTimerUpdatedEvent(_state.PrepTimeRemaining));

            if (_state.PrepTimeRemaining <= 0f)
            {
                _state.PrepTimeRemaining = 0f;
                _state.Phase = GamePhase.Battle;
                events.Add(new PhaseChangedEvent(GamePhase.Battle));

                // Activate all units on phase transition
                ActivateAllUnits(events);

                // Refresh hands on phase transition
                RefreshHands(events);
            }
        }
    }

    /// <summary>
    /// Activate all inactive units when transitioning to Battle phase.
    /// </summary>
    private void ActivateAllUnits(List<SimEvent> events)
    {
        foreach (var unit in _state.Units.Values)
        {
            if (unit.IsAlive && unit.ActivationState != ActivationState.Active)
            {
                unit.ActivationState = ActivationState.Active;
            }
        }
    }

    /// <summary>
    /// Refresh hands for all summoners: discard current hand, draw MaxHandSize cards.
    /// </summary>
    private void RefreshHands(List<SimEvent> events)
    {
        for (int i = 0; i < _state.Summoners.Length; i++)
        {
            var summoner = _state.Summoners[i];

            // Discard current hand
            for (int cardIndex = 0; cardIndex < summoner.Hand.Count; cardIndex++)
            {
                var catalogId = summoner.Hand[cardIndex];
                var cardRef =
                    cardIndex < summoner.HandRefs.Count
                        ? summoner.HandRefs[cardIndex]
                        : BuildRuntimeRef(catalogId);
                summoner.DiscardPile.Add(catalogId);
                summoner.DiscardRefs.Add(cardRef);
            }
            summoner.Hand.Clear();
            summoner.HandRefs.Clear();

            // Draw new hand
            for (int j = 0; j < summoner.MaxHandSize; j++)
            {
                if (summoner.Deck.Count == 0 && summoner.DiscardPile.Count > 0)
                {
                    // Recycle discard pile into deck (seeded shuffle)
                    RecycleDeck(summoner);
                    events.Add(new DeckRecycledEvent((int)summoner.Team));
                }

                if (summoner.Deck.Count > 0)
                    DrawTopDeckCardIntoHand(summoner, summoner.Hand.Count, events: null);
            }

            events.Add(new HandChangedEvent((int)summoner.Team, ToCatalogIdStrings(summoner.Hand)));
        }
    }

    /// <summary>
    /// Shuffle discard pile back into deck using deterministic RNG.
    /// </summary>
    private void RecycleDeck(SummonerData summoner)
    {
        summoner.Deck.AddRange(summoner.DiscardPile);
        foreach (var cardRef in summoner.DiscardRefs)
            summoner.DeckRefs.Add(cardRef);

        // If refs were not tracked for discarded cards yet, synthesize refs from catalog IDs.
        if (summoner.DeckRefs.Count > summoner.Deck.Count)
            summoner.DeckRefs.RemoveRange(
                summoner.Deck.Count,
                summoner.DeckRefs.Count - summoner.Deck.Count
            );
        for (int i = summoner.DeckRefs.Count; i < summoner.Deck.Count; i++)
            summoner.DeckRefs.Add(BuildRuntimeRef(summoner.Deck[i]));

        summoner.DiscardPile.Clear();
        summoner.DiscardRefs.Clear();

        // Fisher-Yates shuffle with deterministic RNG
        if (_state.Rng != null)
        {
            for (int i = summoner.Deck.Count - 1; i > 0; i--)
            {
                int j = _state.Rng.Range(0, i);
                (summoner.Deck[i], summoner.Deck[j]) = (summoner.Deck[j], summoner.Deck[i]);
                (summoner.DeckRefs[i], summoner.DeckRefs[j]) = (
                    summoner.DeckRefs[j],
                    summoner.DeckRefs[i]
                );
            }
        }
    }

    /// <summary>
    /// Tick all alive active units: cooldowns, targeting, behavior, movement, pending damage.
    /// Units are processed in deterministic order (sorted by UnitId).
    /// </summary>
    private void TickUnits(float fixedDelta, List<SimEvent> events)
    {
        // Commit-slot ordering: clear dead/invalid slot bindings before target reacquire.
        _state.ReleaseInvalidSlotReferences();

        var units = _state.GetAliveActiveUnits();

        foreach (var unit in units)
        {
            // Cooldowns
            SimBehavior.TickCooldowns(unit, fixedDelta);

            // Combat orchestration (commit-slot flow is authoritative).
            var result = SimCombatStateMachine.Tick(unit, _state, fixedDelta, events);

            // Movement
            SimMovement.Tick(unit, result, _state, fixedDelta);

            // Pending ranged damage
            SimBehavior.TickPendingDamage(unit, _state, fixedDelta, events);
        }
    }

    /// <summary>
    /// Tick casting timers for all summoners (runs in both Preparation and Battle).
    /// On completion: create units, manage hand (remove played card, draw replacement).
    /// </summary>
    private void TickCasting(float fixedDelta, List<SimEvent> events)
    {
        for (int i = 0; i < _state.Summoners.Length; i++)
        {
            var summoner = _state.Summoners[i];
            if (!summoner.IsCasting)
                continue;

            summoner.CastingTimeRemaining -= fixedDelta;

            if (summoner.CastingTimeRemaining <= 0f)
            {
                CompleteCasting(summoner, events);
            }
        }
    }

    /// <summary>
    /// Complete a casting: clear casting state and emit event.
    /// Units and hand management already happened in ExecutePlayCard (units spawn immediately).
    /// Casting timer just locks the player from playing another card.
    /// </summary>
    private void CompleteCasting(SummonerData summoner, List<SimEvent> events)
    {
        var spawnPosition = summoner.CastingSpawnPosition;
        var cardIndex = summoner.CastingCardIndex;
        var networkId = summoner.CastingNetworkId;

        events.Add(
            new CastingCompletedEvent((int)summoner.Team, cardIndex, spawnPosition, networkId)
        );

        // Clear casting state
        summoner.IsCasting = false;
        summoner.CastingTimeRemaining = 0f;
        summoner.CastingCardIndex = -1;
        summoner.CastingCatalogId = SimCardCatalogId.Empty;
        summoner.CastingCardInstanceId = SimCardInstanceId.Empty;
        summoner.CastingSpawnPosition = SimVector3.Zero;
        summoner.CastingNetworkId = -1;
    }

    /// <summary>
    /// Tick spawn timers for inactive units. When a unit's SpawnTimer reaches 0, activate it.
    /// Runs during Battle phase only — prep-spawned units are activated by ActivateAllUnits instead.
    /// </summary>
    private void TickSpawnTimers(float fixedDelta)
    {
        foreach (var unit in _state.Units.Values)
        {
            if (!unit.IsAlive)
                continue;
            if (unit.ActivationState == ActivationState.Active)
                continue;

            // Defensive: units with no remaining spawn timer should become active immediately.
            if (unit.SpawnTimer <= 0f)
            {
                unit.SpawnTimer = 0f;
                unit.ActivationState = ActivationState.Active;
                continue;
            }

            unit.SpawnTimer -= fixedDelta;
            if (unit.SpawnTimer <= 0f)
            {
                unit.SpawnTimer = 0f;
                unit.ActivationState = ActivationState.Active;
            }
        }
    }

    /// <summary>
    /// Create UnitData entries from a card's unit templates.
    /// Units are spread around the spawn position.
    /// Optional statOverrides are applied after template defaults (used by SpawnUnitCommand).
    /// </summary>
    private void SpawnUnitsFromCard(
        SimCardData cardData,
        int team,
        SimVector3 spawnPosition,
        float spawnTimer,
        List<SimEvent> events,
        Dictionary<StatKey, float>? statOverrides = null,
        SimCardInstanceId castingCardInstanceId = default
    )
    {
        var spawningCardRef = BuildRuntimeRef(cardData.CatalogId, castingCardInstanceId);
        var spawnCountAdd = _state.TraitRuntimeState.GetCardInstanceSpawnCountAdd(
            new TraitRuntimeCardInstanceId(castingCardInstanceId.Value)
        );
        var effectiveTemplateCounts = BuildEffectiveTemplateCounts(
            cardData.UnitTemplates,
            spawnCountAdd
        );
        int unitIndex = 0;
        int totalUnits = 0;
        int firstNetworkId = -1;
        foreach (var count in effectiveTemplateCounts)
            totalUnits += count;
        if (totalUnits <= 0)
            return;

        for (int templateIndex = 0; templateIndex < cardData.UnitTemplates.Count; templateIndex++)
        {
            var template = cardData.UnitTemplates[templateIndex];
            var effectiveCount = effectiveTemplateCounts[templateIndex];
            for (int i = 0; i < effectiveCount; i++)
            {
                var unitId = _state.NextUnitId();
                var networkId = _state.NextNetworkId();
                if (firstNetworkId < 0)
                    firstNetworkId = networkId;
                float spawnRadius =
                    template.NavigationRadius > 0f ? template.NavigationRadius : 0.5f;
                var position = CalculateSpawnOffset(
                    spawnPosition,
                    unitIndex,
                    totalUnits,
                    spawnRadius
                );
                if (template.MovementLayer == MovementLayer.Air)
                    position = new SimVector3(position.X, template.FlightAltitude, position.Z);

                var unitData = new UnitData
                {
                    UnitId = unitId,
                    NetworkId = networkId,
                    CatalogId = template.UnitTypeId,
                    Team = (Team)team,
                    CurrentHp = template.MaxHp,
                    MaxHp = template.MaxHp,
                    IsAlive = true,
                    Position = position,
                    AttackDamage = template.AttackDamage,
                    AttackSpeed = template.AttackSpeed,
                    MoveSpeed = template.MoveSpeed,
                    AttackRange = template.AttackRange,
                    AggroRadius = template.AggroRadius,
                    SoulStrength = template.SoulStrength,
                    SeparationRadius = template.SeparationRadius,
                    NavigationRadius = template.NavigationRadius,
                    HurtboxRadius = template.HurtboxRadius,
                    HurtboxHeight = template.HurtboxHeight,
                    HurtboxHorizontal = template.HurtboxHorizontal,
                    HurtboxOffset = template.HurtboxOffset,
                    CritChance = template.CritChance,
                    CritDamage = template.CritDamage,
                    UnitType = template.UnitType,
                    TacticalRole = template.TacticalRole,
                    MovementLayer = template.MovementLayer,
                    AssignedLane = VirtualLanes.GetLaneIndex(position.Z),
                    ElementId = template.ElementId,
                    FallbackMovement = template.FallbackMovement,
                    EngageShape = template.EngageShape,
                    EngageRectLength = template.EngageRectLength,
                    EngageRectHalfWidth = template.EngageRectHalfWidth,
                    EngageRectForwardOffset = template.EngageRectForwardOffset,
                    EngageCloseRadius = template.EngageCloseRadius,
                    HasConeConstraint = template.HasConeConstraint,
                    ConeHalfAngle = template.ConeHalfAngle,
                    ConeCenterOffsetDegrees = template.ConeCenterOffsetDegrees,
                    CloseRangeThreshold = template.CloseRangeThreshold,
                    TargetLayerFilter = template.TargetLayerFilter,
                    DistanceScorerWeight = template.DistanceScorerWeight,
                    HealthScorerWeight = template.HealthScorerWeight,
                    TargetPolicyId = template.TargetPolicyId,
                    MovementIntentStrategy = template.MovementIntentStrategy,
                    FlightAltitude = template.FlightAltitude,
                    ProjectileCatalogId = template.ProjectileCatalogId,
                    ProjectileDelay = template.ProjectileDelay,
                    ProjectileTargetAffinity = template.ProjectileTargetAffinity,
                    ProjectileImpactKind = template.ProjectileImpactKind,
                    ProjectileStatusKind = template.ProjectileStatusKind,
                    ProjectileStatusDuration = template.ProjectileStatusDuration,
                    ProjectileStatusTickInterval = template.ProjectileStatusTickInterval,
                    ProjectileStatusPotencyPerStack = template.ProjectileStatusPotencyPerStack,
                    ProjectileStatusMaxStacks = template.ProjectileStatusMaxStacks,
                    AttackType = template.AttackType,
                    PhysicalDamageRatio = template.PhysicalDamageRatio,
                    ElementalDamageRatio = template.ElementalDamageRatio,
                    PhysicalDefense = template.PhysicalDefense,
                    MagicDefense = template.MagicDefense,
                    Evasion = template.Evasion,
                    Attack = template.Attack.DeepClone(),
                    Abilities = BuildAbilityRuntimeState(template.Abilities),
                    IsFacingRight = UnitData.DefaultFacingForTeam((Team)team),
                    // Spawn inactive when there is a reveal/cast delay; otherwise active immediately.
                    ActivationState =
                        spawnTimer > 0f ? ActivationState.Inactive : ActivationState.Active,
                    SpawnTimer = spawnTimer,
                };

                // Apply stat overrides (SpawnUnitCommand path)
                if (statOverrides != null)
                    ApplyStatOverrides(unitData, statOverrides);

                ApplyUnifiedTraitSpawnEffects(unitData, team, spawningCardRef);
                _state.Units[unitId] = unitData;

                events.Add(
                    new UnitRegisteredEvent(
                        unitId,
                        unitData.NetworkId,
                        cardData.CatalogId,
                        team,
                        position
                    )
                );

                unitIndex++;
            }
        }

        // Update CastingNetworkId to match the first spawned unit's actual NetworkId
        if (firstNetworkId >= 0)
            _state.Summoners[team].CastingNetworkId = firstNetworkId;
    }

    private static int[] BuildEffectiveTemplateCounts(
        List<SimUnitTemplate> templates,
        int spawnCountAdd
    )
    {
        var counts = new int[templates.Count];
        for (int i = 0; i < templates.Count; i++)
            counts[i] = templates[i].Count < 0 ? 0 : templates[i].Count;

        if (spawnCountAdd == 0 || templates.Count == 0)
            return counts;

        // Bias additional/reduced units toward templates with larger existing counts
        // to preserve the original composition profile.
        var rankedTemplateIndices = templates
            .Select((template, index) => new { template.Count, index })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.index)
            .Select(x => x.index)
            .ToArray();

        if (spawnCountAdd > 0)
        {
            for (int i = 0; i < spawnCountAdd; i++)
            {
                var targetIndex = rankedTemplateIndices[i % rankedTemplateIndices.Length];
                counts[targetIndex] += 1;
            }

            return counts;
        }

        var removals = -spawnCountAdd;
        var remaining = removals;
        while (remaining > 0)
        {
            var removedThisPass = false;
            foreach (var targetIndex in rankedTemplateIndices)
            {
                if (counts[targetIndex] <= 0)
                    continue;

                counts[targetIndex] -= 1;
                remaining -= 1;
                removedThisPass = true;
                if (remaining == 0)
                    break;
            }

            if (!removedThisPass)
                break;
        }

        return counts;
    }

    private static List<UnitAbilityState> BuildAbilityRuntimeState(List<UnitAbilityState> source)
    {
        if (source.Count == 0)
            return new List<UnitAbilityState>();

        var result = new List<UnitAbilityState>(source.Count);
        foreach (var ability in source)
            result.Add(ability.DeepClone());
        return result;
    }

    /// <summary>
    /// Calculate spawn offset for a unit in a group.
    /// Simple line formation spread along Z axis, centered on spawn position.
    /// </summary>
    private static SimVector3 CalculateSpawnOffset(
        SimVector3 center,
        int index,
        int total,
        float spacing
    )
    {
        if (total <= 1)
            return center;

        float totalWidth = (total - 1) * spacing * 2f;
        float startZ = center.Z - totalWidth / 2f;
        return new SimVector3(center.X, center.Y, startZ + index * spacing * 2f);
    }

    /// <summary>
    /// Apply runtime stat overrides to a newly created UnitData.
    /// Used by SpawnUnitCommand for debug/event/tutorial spawns with custom stats.
    /// </summary>
    private static void ApplyStatOverrides(UnitData unit, Dictionary<StatKey, float> overrides)
    {
        foreach (var (key, value) in overrides)
        {
            switch (key)
            {
                case StatKey.MaxHp:
                    unit.MaxHp = value;
                    unit.CurrentHp = value;
                    break;
                case StatKey.MoveSpeed:
                    unit.MoveSpeed = value;
                    break;
                case StatKey.AttackDamage:
                    unit.AttackDamage = value;
                    break;
                case StatKey.AttackSpeed:
                    unit.AttackSpeed = value;
                    break;
                case StatKey.AttackRange:
                    unit.AttackRange = value;
                    break;
                case StatKey.SoulStrength:
                    unit.SoulStrength = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Execute spell effects: resolve targets, apply each effect via SimEffects.
    /// </summary>
    private void ExecuteSpellEffects(
        SimCardData cardData,
        int team,
        SimVector3 position,
        int? targetUnitId,
        List<SimEvent> events
    )
    {
        int summonerSourceId = MatchState.GetSummonerTargetId(team);
        events.Add(new SpellCastEvent(team, cardData.CatalogId, position));

        foreach (var effect in cardData.SpellEffects)
        {
            if (TrySpawnSpellProjectile(cardData, effect, team, position, targetUnitId))
                continue;

            var targets = ResolveSpellTargets(cardData, effect, team, position, targetUnitId);
            foreach (var target in targets)
            {
                SimEffects.ApplyEffect(
                    _state,
                    effect.EffectType,
                    effect.Value,
                    effect.Duration,
                    effect.DamageType,
                    target,
                    summonerSourceId,
                    (Team)team,
                    events
                );
            }
        }
    }

    /// <summary>
    /// Spawn a simulated projectile for damage spells that define a projectile ID.
    /// Returns true when projectile path was used (caller should skip immediate effect application).
    /// </summary>
    private bool TrySpawnSpellProjectile(
        SimCardData cardData,
        SimSpellEffect effect,
        int team,
        SimVector3 castPosition,
        int? targetUnitId
    )
    {
        if (effect.EffectType != EffectType.Damage)
            return false;
        if (string.IsNullOrEmpty(cardData.SpellProjectileId))
            return false;

        var projectileData = ProjectileDefinitions.Get(cardData.SpellProjectileId);
        if (projectileData == null)
        {
            Log?.Invoke(
                $"[Simulation] ERROR: Spell '{cardData.CatalogId}' references unknown projectile '{cardData.SpellProjectileId}'."
            );
            return true;
        }

        float spawnSpeed = projectileData.Speed;
        var summoner = _state.Summoners[team];
        var startPos = summoner.Position;
        var targetPos = castPosition;
        // No explicit target unit (position-targeted projectile spell).
        // Use a non-negative invalid sentinel so it is never interpreted as a summoner target ID.
        int resolvedTargetUnitId = int.MaxValue;

        switch (cardData.SpellTargetingMode)
        {
            case SpellTargetingMode.Position:
                // AoE spells travel to the selected cast position and explode there.
                break;

            case SpellTargetingMode.NearestEnemy:
            {
                var targets = ResolveSpellTargets(
                    cardData,
                    effect,
                    team,
                    castPosition,
                    targetUnitId
                );
                if (targets.Count == 0)
                    return true;

                var target = targets[0];
                resolvedTargetUnitId = target.UnitId;
                targetPos = target.Position;
                break;
            }

            default:
                // Only Position and NearestEnemy projectile spells are supported right now.
                Log?.Invoke(
                    $"[Simulation] ERROR: Projectile spell '{cardData.CatalogId}' uses unsupported targeting mode '{cardData.SpellTargetingMode}'."
                );
                return true;
        }

        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, targetPos.Y, startPos.Z);

        float aoeRadius = effect.AoeRadius > 0f ? effect.AoeRadius : projectileData.AoeRadius;
        SimProjectile.Spawn(
            _state,
            sourceUnitId: MatchState.GetSummonerTargetId(team),
            targetUnitId: resolvedTargetUnitId,
            team: (Team)team,
            damage: effect.Value,
            sourceElementId: cardData.ElementId,
            movementType: projectileData.MovementType,
            speed: spawnSpeed,
            lifetime: projectileData.Lifetime,
            startPos: startPos,
            targetPos: targetPos,
            arcHeight: projectileData.ArcHeight,
            pierceCount: projectileData.PierceCount,
            aoeRadius: aoeRadius,
            hitRadius: projectileData.HitRadius,
            hitSpace: projectileData.HitSpace,
            steerStrength: projectileData.SteerStrength,
            veerDelay: projectileData.VeerDelay,
            veerAngle: projectileData.VeerAngle,
            veerDuration: projectileData.VeerDuration,
            projectileCatalogId: cardData.SpellProjectileId,
            acceleration: projectileData.Acceleration,
            minSpeed: projectileData.MinSpeed,
            speedStart: projectileData.SpeedStart,
            speedEnd: projectileData.SpeedEnd,
            speedTransitionDuration: projectileData.SpeedTransitionDuration,
            speedEasing: projectileData.SpeedEasing,
            speedEaseExponent: projectileData.SpeedEaseExponent,
            tracking: projectileData.Tracking
        );

        return true;
    }

    /// <summary>
    /// Resolve targets for a spell effect based on targeting mode and affinity.
    /// </summary>
    private List<UnitData> ResolveSpellTargets(
        SimCardData cardData,
        SimSpellEffect effect,
        int team,
        SimVector3 position,
        int? targetUnitId
    )
    {
        var targets = new List<UnitData>();

        // Determine target team filter based on affinity
        int? teamFilter = effect.Affinity switch
        {
            SpellAffinity.Enemies => MatchState.GetEnemyTeam(team),
            SpellAffinity.Allies => team,
            _ => null, // Both — no filter
        };

        switch (cardData.SpellTargetingMode)
        {
            case SpellTargetingMode.Position:
            {
                // AoE at position — find all matching units in radius
                float radius = effect.AoeRadius > 0 ? effect.AoeRadius : cardData.SpellRadius;
                float radiusSq = radius * radius;
                foreach (var unit in _state.GetAliveActiveUnits())
                {
                    if (teamFilter.HasValue && (int)unit.Team != teamFilter.Value)
                        continue;
                    if (unit.Position.DistanceSquaredTo(position) <= radiusSq)
                        targets.Add(unit);
                }
                break;
            }

            case SpellTargetingMode.NearestEnemy:
            {
                // Single nearest enemy to caster
                int enemyTeam = MatchState.GetEnemyTeam(team);
                float bestDistSq = float.MaxValue;
                UnitData? best = null;

                // Single-target spells (Mana Bolt, Weaving Bolt) are auto-targeted.
                // Use the caster summoner position as origin so cursor position does
                // not change which enemy is selected.
                var searchOrigin = _state.Summoners[team].Position;

                // If a specific target was provided, use it directly
                if (targetUnitId.HasValue)
                {
                    var specified = _state.GetAliveUnit(targetUnitId.Value);
                    if (specified != null)
                    {
                        targets.Add(specified);
                        break;
                    }
                }

                foreach (var unit in _state.GetAliveActiveUnits())
                {
                    if ((int)unit.Team != enemyTeam)
                        continue;
                    float distSq = unit.Position.DistanceSquaredTo(searchOrigin);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = unit;
                    }
                }
                if (best != null)
                    targets.Add(best);
                break;
            }

            case SpellTargetingMode.AlliesInRadius:
            {
                // Allied units within selection radius
                float radiusSq = cardData.SpellRadius * cardData.SpellRadius;
                foreach (var unit in _state.GetAliveActiveUnits())
                {
                    if ((int)unit.Team != team)
                        continue;
                    if (unit.Position.DistanceSquaredTo(position) <= radiusSq)
                        targets.Add(unit);
                }
                break;
            }
        }

        return targets;
    }

    /// <summary>
    /// Draw a replacement card into the given hand slot.
    /// If deck is empty and discard has cards, recycle first.
    /// </summary>
    private void DrawReplacementCard(SummonerData summoner, int targetIndex, List<SimEvent> events)
    {
        if (_state.Phase == GamePhase.Preparation)
        {
            DrawReplacementCardDuringPreparation(summoner, targetIndex, events);
            return;
        }

        if (summoner.Deck.Count == 0 && summoner.DiscardPile.Count > 0)
        {
            RecycleDeck(summoner);
            events.Add(new DeckRecycledEvent((int)summoner.Team));
        }

        DrawTopDeckCardIntoHand(summoner, targetIndex, events, eventHandIndex: targetIndex);
    }

    private void DrawReplacementCardDuringPreparation(
        SummonerData summoner,
        int targetIndex,
        List<SimEvent> events
    )
    {
        if (TryDrawFirstSummonCardFromDeckIntoHand(summoner, targetIndex, events, targetIndex))
            return;

        bool deckHasSummons = HasMatchingCard(summoner.Deck, IsSummonCard);
        bool discardHasSummons = HasMatchingCard(summoner.DiscardPile, IsSummonCard);
        if (!deckHasSummons && discardHasSummons)
        {
            RecycleDeck(summoner);
            events.Add(new DeckRecycledEvent((int)summoner.Team));
            TryDrawFirstSummonCardFromDeckIntoHand(summoner, targetIndex, events, targetIndex);
        }
    }

    private bool TryDrawFirstSummonCardFromDeckIntoHand(
        SummonerData summoner,
        int insertIndex,
        List<SimEvent> events,
        int eventHandIndex
    )
    {
        for (int i = 0; i < summoner.Deck.Count; i++)
        {
            if (!IsSummonCard(summoner.Deck[i]))
                continue;

            return DrawDeckCardIntoHand(
                summoner,
                i,
                insertIndex,
                events,
                eventHandIndex: eventHandIndex
            );
        }

        return false;
    }

    private bool IsSummonCard(SimCardCatalogId catalogId)
    {
        if (_state.CardDataMap.TryGetValue(catalogId, out var cardData))
            return !cardData.IsSpell;

        Log?.Invoke(
            $"[Simulation] Missing card data while evaluating prep draw filtering: catalogId={catalogId}"
        );
        return false;
    }

    private static bool HasMatchingCard(
        List<SimCardCatalogId> cards,
        Func<SimCardCatalogId, bool> predicate
    )
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (predicate(cards[i]))
                return true;
        }

        return false;
    }

    private static SimCardRuntimeRef BuildRuntimeRef(
        SimCardCatalogId catalogId,
        SimCardInstanceId instanceId = default
    )
    {
        return new SimCardRuntimeRef { CatalogId = catalogId, InstanceId = instanceId };
    }

    private static SimCardRuntimeRef GetHandCardRef(
        SummonerData summoner,
        int handIndex,
        SimCardCatalogId fallbackCatalogId
    )
    {
        if (handIndex >= 0 && handIndex < summoner.HandRefs.Count)
            return summoner.HandRefs[handIndex];

        return BuildRuntimeRef(fallbackCatalogId);
    }

    private static bool DrawTopDeckCardIntoHand(
        SummonerData summoner,
        int insertIndex,
        List<SimEvent>? events,
        int? eventHandIndex = null
    )
    {
        return DrawDeckCardIntoHand(summoner, 0, insertIndex, events, eventHandIndex);
    }

    private static bool DrawDeckCardIntoHand(
        SummonerData summoner,
        int deckIndex,
        int insertIndex,
        List<SimEvent>? events,
        int? eventHandIndex = null
    )
    {
        if (deckIndex < 0 || deckIndex >= summoner.Deck.Count)
            return false;

        int originalDeckCount = summoner.Deck.Count;
        if (summoner.DeckRefs.Count > originalDeckCount)
        {
            summoner.DeckRefs.RemoveRange(
                originalDeckCount,
                summoner.DeckRefs.Count - originalDeckCount
            );
        }

        var card = summoner.Deck[deckIndex];
        summoner.Deck.RemoveAt(deckIndex);

        SimCardRuntimeRef cardRef;
        if (deckIndex >= 0 && deckIndex < summoner.DeckRefs.Count)
        {
            cardRef = summoner.DeckRefs[deckIndex];
            summoner.DeckRefs.RemoveAt(deckIndex);
            if (!cardRef.CatalogId.HasValue)
                cardRef.CatalogId = card;
        }
        else
        {
            cardRef = BuildRuntimeRef(card);
        }

        // Repair ref list shape if prior data was serialized without refs.
        if (summoner.HandRefs.Count > summoner.Hand.Count)
            summoner.HandRefs.RemoveRange(
                summoner.Hand.Count,
                summoner.HandRefs.Count - summoner.Hand.Count
            );
        for (int i = summoner.HandRefs.Count; i < summoner.Hand.Count; i++)
            summoner.HandRefs.Add(BuildRuntimeRef(summoner.Hand[i]));

        int resolvedInsertIndex;
        if (insertIndex >= 0 && insertIndex <= summoner.Hand.Count)
        {
            summoner.Hand.Insert(insertIndex, card);
            summoner.HandRefs.Insert(insertIndex, cardRef);
            resolvedInsertIndex = insertIndex;
        }
        else
        {
            summoner.Hand.Add(card);
            summoner.HandRefs.Add(cardRef);
            resolvedInsertIndex = summoner.Hand.Count - 1;
        }

        if (events != null)
            events.Add(
                new CardDrawnEvent((int)summoner.Team, eventHandIndex ?? resolvedInsertIndex, card)
            );

        return true;
    }

    private void ApplyUnifiedTraitSpawnEffects(
        UnitData unitData,
        int team,
        SimCardRuntimeRef cardRef
    )
    {
        var spawnContext = new TraitRuntimeSpawnContext
        {
            TeamId = team,
            CardCatalogId = new TraitRuntimeCardCatalogId(cardRef.CatalogId.Value),
            CardInstanceId = new TraitRuntimeCardInstanceId(cardRef.InstanceId.Value),
        };
        _state.TraitRuntimeState.ApplySpawnModifiers(unitData, spawnContext);
    }

    private static string[] ToCatalogIdStrings(List<SimCardCatalogId> ids)
    {
        var result = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++)
            result[i] = ids[i].Value;
        return result;
    }

    /// <summary>
    /// Death cleanup: decrement timers on dead units, remove expired, emit UnitRemovedEvent.
    /// Dead units with timer <= 0 are removed immediately in the same tick.
    /// </summary>
    private void TickDeathCleanup(float fixedDelta, List<SimEvent> events)
    {
        var toRemove = new List<int>();

        foreach (var (unitId, unit) in _state.Units)
        {
            if (!unit.IsAlive)
            {
                if (unit.DeathCleanupTimer > 0)
                    unit.DeathCleanupTimer -= fixedDelta;

                if (unit.DeathCleanupTimer <= 0)
                    toRemove.Add(unitId);
            }
        }

        foreach (var unitId in toRemove)
        {
            _state.Units.Remove(unitId);
            events.Add(new UnitRemovedEvent(unitId));
        }
    }

    /// <summary>
    /// Evaluate win conditions. Creates the IWinCondition lazily on first call.
    /// If a win condition is met, transitions to GameOver and emits GameOverEvent.
    /// This is the single authoritative source of GameOverEvent (step 10).
    /// </summary>
    private void EvaluateWinConditions(List<SimEvent> events)
    {
        // Already game over (e.g., SimBehavior emitted GameOverEvent this tick)
        if (_state.Phase == GamePhase.GameOver)
            return;

        _winCondition ??= WinConditionFactory.Create(_state);

        var result = _winCondition.Evaluate(_state);
        if (result != null)
        {
            _state.WinnerTeam = result.WinnerTeam;
            _state.Phase = GamePhase.GameOver;
            events.Add(new GameOverEvent(result.WinnerTeam, result.Reason));
        }
    }
}

// =========================================================================
// SIM EVENTS
// =========================================================================

/// <summary>
/// Events produced by Simulation.Tick() for SimulationNode to emit as Godot signals.
/// No Godot types — positions use SimVector3.
/// </summary>
public abstract class SimEvent
{
    public abstract void Accept(ISimEventVisitor visitor);
}

[EventCategory(EventCategory.Snapshot)]
public class PhaseChangedEvent : SimEvent
{
    public GamePhase NewPhase { get; }

    public PhaseChangedEvent(GamePhase newPhase) => NewPhase = newPhase;

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class PrepTimerUpdatedEvent : SimEvent
{
    public float Remaining { get; }

    public PrepTimerUpdatedEvent(float remaining) => Remaining = remaining;

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class MatchTimeUpdatedEvent : SimEvent
{
    public float MatchTime { get; }

    public MatchTimeUpdatedEvent(float matchTime) => MatchTime = matchTime;

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Broadcast)]
public class SummonerHpChangedEvent : SimEvent
{
    public int Team { get; }
    public float Hp { get; }
    public float MaxHp { get; }

    public SummonerHpChangedEvent(int team, float hp, float maxHp)
    {
        Team = team;
        Hp = hp;
        MaxHp = maxHp;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class SummonerManaChangedEvent : SimEvent
{
    public int Team { get; }
    public float Mana { get; }
    public float MaxMana { get; }

    public SummonerManaChangedEvent(int team, float mana, float maxMana)
    {
        Team = team;
        Mana = mana;
        MaxMana = maxMana;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class CastingStartedEvent : SimEvent
{
    public int Team { get; }
    public int CardIndex { get; }
    public float Duration { get; }
    public SimVector3 SpawnPosition { get; }
    public SimCardCatalogId CatalogId { get; }

    public CastingStartedEvent(
        int team,
        int cardIndex,
        float duration,
        SimVector3 spawnPosition,
        SimCardCatalogId catalogId = default
    )
    {
        Team = team;
        CardIndex = cardIndex;
        Duration = duration;
        SpawnPosition = spawnPosition;
        CatalogId = catalogId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class CastingCompletedEvent : SimEvent
{
    public int Team { get; }
    public int CardIndex { get; }
    public SimVector3 SpawnPosition { get; }
    public int NetworkId { get; }

    public CastingCompletedEvent(int team, int cardIndex, SimVector3 spawnPosition, int networkId)
    {
        Team = team;
        CardIndex = cardIndex;
        SpawnPosition = spawnPosition;
        NetworkId = networkId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class CardDrawnEvent : SimEvent
{
    public int Team { get; }
    public int HandIndex { get; }
    public SimCardCatalogId CatalogId { get; }

    public CardDrawnEvent(int team, int handIndex, SimCardCatalogId catalogId)
    {
        Team = team;
        HandIndex = handIndex;
        CatalogId = catalogId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class HandChangedEvent : SimEvent
{
    public int Team { get; }
    public string[] Hand { get; }

    public HandChangedEvent(int team, string[] hand)
    {
        Team = team;
        Hand = hand;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Snapshot)]
public class DeckRecycledEvent : SimEvent
{
    public int Team { get; }

    public DeckRecycledEvent(int team) => Team = team;

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Broadcast)]
public class UnitRegisteredEvent : SimEvent
{
    public int UnitId { get; }
    public int NetworkId { get; }
    public SimCardCatalogId CatalogId { get; }
    public int Team { get; }
    public SimVector3 Position { get; }

    public UnitRegisteredEvent(
        int unitId,
        int networkId,
        SimCardCatalogId catalogId,
        int team,
        SimVector3 position
    )
    {
        UnitId = unitId;
        NetworkId = networkId;
        CatalogId = catalogId;
        Team = team;
        Position = position;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.HostOnly)]
public class UnitRemovedEvent : SimEvent
{
    public int UnitId { get; }

    public UnitRemovedEvent(int unitId) => UnitId = unitId;

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

[EventCategory(EventCategory.Broadcast)]
public class GameOverEvent : SimEvent
{
    public int WinnerTeam { get; }
    public string Reason { get; }

    public GameOverEvent(int winnerTeam, string reason)
    {
        WinnerTeam = winnerTeam;
        Reason = reason;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A summoner took damage (for visual feedback — flash, screen shake).
/// </summary>
[EventCategory(EventCategory.Broadcast)]
public class SummonerDamagedEvent : SimEvent
{
    public int Team { get; }
    public float Damage { get; }
    public int AttackerUnitId { get; }

    public SummonerDamagedEvent(int team, float damage, int attackerUnitId)
    {
        Team = team;
        Damage = damage;
        AttackerUnitId = attackerUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A summoner was destroyed (for death animation, game-over trigger).
/// </summary>
[EventCategory(EventCategory.Broadcast)]
public class SummonerDestroyedEvent : SimEvent
{
    public int Team { get; }
    public int KillerUnitId { get; }

    public SummonerDestroyedEvent(int team, int killerUnitId)
    {
        Team = team;
        KillerUnitId = killerUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A spell card was cast (for visual feedback — VFX, projectiles).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class SpellCastEvent : SimEvent
{
    public int Team { get; }
    public SimCardCatalogId CatalogId { get; }
    public SimVector3 Position { get; }

    public SpellCastEvent(int team, SimCardCatalogId catalogId, SimVector3 position)
    {
        Team = team;
        CatalogId = catalogId;
        Position = position;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A unit attacked another unit (for visual/audio feedback).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class UnitAttackedEvent : SimEvent
{
    public int AttackerUnitId { get; }
    public int TargetUnitId { get; }

    public UnitAttackedEvent(int attackerUnitId, int targetUnitId)
    {
        AttackerUnitId = attackerUnitId;
        TargetUnitId = targetUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A unit took damage (for visual feedback — flash, HP bar update).
/// </summary>
[EventCategory(EventCategory.Broadcast)]
public class UnitDamagedEvent : SimEvent
{
    public int TargetUnitId { get; }
    public int AttackerUnitId { get; }
    public float Damage { get; }
    public bool IsCrit { get; }

    public UnitDamagedEvent(int targetUnitId, int attackerUnitId, float damage, bool isCrit)
    {
        TargetUnitId = targetUnitId;
        AttackerUnitId = attackerUnitId;
        Damage = damage;
        IsCrit = isCrit;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A unit died (for death animation, cleanup, kill tracking).
/// </summary>
[EventCategory(EventCategory.Broadcast)]
public class UnitDiedEvent : SimEvent
{
    public int UnitId { get; }
    public int KillerUnitId { get; }

    public UnitDiedEvent(int unitId, int killerUnitId)
    {
        UnitId = unitId;
        KillerUnitId = killerUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A projectile hit a unit (for visual feedback — impact VFX, pierce tracking).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class ProjectileHitEvent : SimEvent
{
    public int ProjectileId { get; }
    public int TargetUnitId { get; }

    public ProjectileHitEvent(int projectileId, int targetUnitId)
    {
        ProjectileId = projectileId;
        TargetUnitId = targetUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A unit's activation state changed (for visual feedback).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class UnitActivationChangedEvent : SimEvent
{
    public int UnitId { get; }
    public int NewState { get; }

    public UnitActivationChangedEvent(int unitId, int newState)
    {
        UnitId = unitId;
        NewState = newState;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A unit evaded an attack (for visual feedback — dodge text, animation).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class AttackEvadedEvent : SimEvent
{
    public int TargetUnitId { get; }
    public int AttackerUnitId { get; }

    public AttackEvadedEvent(int targetUnitId, int attackerUnitId)
    {
        TargetUnitId = targetUnitId;
        AttackerUnitId = attackerUnitId;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A buff/debuff was applied to a unit (for visual feedback — VFX, status icons).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class BuffAppliedEvent : SimEvent
{
    public int TargetUnitId { get; }
    public EffectType EffectType { get; }
    public float Value { get; }
    public float Duration { get; }

    public BuffAppliedEvent(int targetUnitId, EffectType effectType, float value, float duration)
    {
        TargetUnitId = targetUnitId;
        EffectType = effectType;
        Value = value;
        Duration = duration;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A buff/debuff expired on a unit (for visual cleanup).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class BuffExpiredEvent : SimEvent
{
    public int TargetUnitId { get; }
    public int BuffId { get; }
    public EffectType EffectType { get; }

    public BuffExpiredEvent(int targetUnitId, int buffId, EffectType effectType)
    {
        TargetUnitId = targetUnitId;
        BuffId = buffId;
        EffectType = effectType;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A simulation-owned unit ability activated (for visual/audio/debug feedback).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class AbilityActivatedEvent : SimEvent
{
    public int SourceUnitId { get; }
    public string AbilityId { get; }
    public int? TargetUnitId { get; }
    public SimVector3 Position { get; }

    public AbilityActivatedEvent(
        int sourceUnitId,
        string abilityId,
        int? targetUnitId,
        SimVector3 position
    )
    {
        SourceUnitId = sourceUnitId;
        AbilityId = abilityId;
        TargetUnitId = targetUnitId;
        Position = position;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A status payload was applied/stacked on a unit.
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class StatusAppliedEvent : SimEvent
{
    public int SourceUnitId { get; }
    public int TargetUnitId { get; }
    public StatusEffectKind StatusKind { get; }
    public int StackCount { get; }
    public float DurationSeconds { get; }

    public StatusAppliedEvent(
        int sourceUnitId,
        int targetUnitId,
        StatusEffectKind statusKind,
        int stackCount,
        float durationSeconds
    )
    {
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
        StatusKind = statusKind;
        StackCount = stackCount;
        DurationSeconds = durationSeconds;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}

/// <summary>
/// A delayed effect fired (death explosion, timed AoE — for visual feedback).
/// </summary>
[EventCategory(EventCategory.HostOnly)]
public class DelayedEffectFiredEvent : SimEvent
{
    public SimVector3 Position { get; }
    public EffectType EffectType { get; }
    public float AoeRadius { get; }

    public DelayedEffectFiredEvent(SimVector3 position, EffectType effectType, float aoeRadius)
    {
        Position = position;
        EffectType = effectType;
        AoeRadius = aoeRadius;
    }

    public override void Accept(ISimEventVisitor visitor) => visitor.Visit(this);
}
