using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Movement;

namespace Fateforged.Simulation;

/// <summary>
/// Pure deterministic simulation. All state mutations go through Tick().
/// No Godot node dependencies — operates on MatchState data only.
///
/// Tick Order Contract (11 steps):
///   1. Increment frame, advance match time (Battle only)
///   2. Drain and execute due commands
///   3. Phase timers / transitions (prep→battle: activate units, refresh hands)
///   4. Tick casting (decrement timers, handle completions, replacement draws)
///   5. Tick units (cooldowns → targeting → behavior → movement → pending melee damage)
///   6. Tick projectiles
///   7. Tick effects (buffs: decrement, periodic, remove expired) — placeholder
///   8. Tick delayed effects (death explosions, etc.) — placeholder
///   9. Death cleanup (DeathCleanupTimer countdown, remove expired, emit UnitRemovedEvent)
///  10. Evaluate win conditions
///  11. Return events
/// </summary>
public class Simulation
{
    private readonly MatchState _state;

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

        // Step 2: Drain and execute due commands
        DrainCommands(events);

        // Step 3: Phase timers / transitions
        TickPhaseTransitions(fixedDelta, events);

        // Step 4: Tick casting
        TickCasting(fixedDelta, events);

        // Step 5: Tick units (only during Battle)
        if (_state.Phase == GamePhase.Battle)
        {
            TickUnits(fixedDelta, events);
        }

        // Step 6: Tick projectiles (only during Battle)
        if (_state.Phase == GamePhase.Battle)
        {
            SimProjectile.TickAll(_state, fixedDelta, events);
        }

        // Step 7: Tick effects — placeholder for Phase 5
        // SimEffects.TickBuffs(_state, fixedDelta, events);

        // Step 8: Tick delayed effects — placeholder for Phase 5
        // SimEffects.TickDelayedEffects(_state, fixedDelta, events);

        // Step 9: Death cleanup
        TickDeathCleanup(fixedDelta, events);

        // Step 10: Evaluate win conditions
        // Will be implemented in Phase 4 with IWinCondition system

        // Step 11: Return events
        return events;
    }

    /// <summary>
    /// Drain PendingCommandBuffer: execute commands where ExecuteFrame <= FrameNumber.
    /// Sort by (ExecuteFrame, Team, Sequence) for deterministic ordering.
    /// </summary>
    private void DrainCommands(List<SimEvent> events)
    {
        if (_state.PendingCommandBuffer.Count == 0) return;

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

        if (dueCommands.Count == 0) return;

        // Sort due commands by (ExecuteFrame, Team, Sequence)
        dueCommands.Sort((a, b) =>
        {
            int cmp = a.ExecuteFrame.CompareTo(b.ExecuteFrame);
            if (cmp != 0) return cmp;

            // Extract team for ordering
            int teamA = a is PlayCardCommand pcA ? pcA.Team : (a is ForfeitCommand fcA ? fcA.Team : 0);
            int teamB = b is PlayCardCommand pcB ? pcB.Team : (b is ForfeitCommand fcB ? fcB.Team : 0);
            cmp = teamA.CompareTo(teamB);
            if (cmp != 0) return cmp;

            // Extract sequence for ordering
            int seqA = a is PlayCardCommand pcA2 ? pcA2.Sequence : 0;
            int seqB = b is PlayCardCommand pcB2 ? pcB2.Sequence : 0;
            return seqA.CompareTo(seqB);
        });

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
                // Validation and execution will be fully implemented in Phase 2
                break;

            case ForfeitCommand forfeit:
                int winnerTeam = forfeit.Team == 0 ? 1 : 0;
                _state.WinnerTeam = winnerTeam;
                _state.Phase = GamePhase.GameOver;
                events.Add(new GameOverEvent(winnerTeam, "Forfeit"));
                break;
        }
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
            if (unit.IsAlive && unit.ActivationState != (int)ProjectSummoner.Units.ActivationState.Active)
            {
                unit.ActivationState = (int)ProjectSummoner.Units.ActivationState.Active;
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
            summoner.DiscardPile.AddRange(summoner.Hand);
            summoner.Hand.Clear();

            // Draw new hand
            for (int j = 0; j < summoner.MaxHandSize; j++)
            {
                if (summoner.Deck.Count == 0 && summoner.DiscardPile.Count > 0)
                {
                    // Recycle discard pile into deck (seeded shuffle)
                    RecycleDeck(summoner);
                    events.Add(new DeckRecycledEvent(summoner.Team));
                }

                if (summoner.Deck.Count > 0)
                {
                    var card = summoner.Deck[0];
                    summoner.Deck.RemoveAt(0);
                    summoner.Hand.Add(card);
                }
            }

            events.Add(new HandChangedEvent(summoner.Team, summoner.Hand.ToArray()));
        }
    }

    /// <summary>
    /// Shuffle discard pile back into deck using deterministic RNG.
    /// </summary>
    private void RecycleDeck(SummonerData summoner)
    {
        summoner.Deck.AddRange(summoner.DiscardPile);
        summoner.DiscardPile.Clear();

        // Fisher-Yates shuffle with deterministic RNG
        if (_state.Rng != null)
        {
            for (int i = summoner.Deck.Count - 1; i > 0; i--)
            {
                int j = _state.Rng.Range(0, i);
                (summoner.Deck[i], summoner.Deck[j]) = (summoner.Deck[j], summoner.Deck[i]);
            }
        }
    }

    /// <summary>
    /// Tick all alive active units: cooldowns, targeting, behavior, movement, pending damage.
    /// Units are processed in deterministic order (sorted by UnitId).
    /// </summary>
    private void TickUnits(float fixedDelta, List<SimEvent> events)
    {
        var units = _state.GetAliveActiveUnits();

        foreach (var unit in units)
        {
            // Cooldowns
            SimBehavior.TickCooldowns(unit, fixedDelta);

            // Targeting
            SimBehavior.TickTargeting(unit, _state);

            // Behavior (returns movement instruction)
            var result = SimBehavior.TickBehavior(unit, _state, fixedDelta, events);

            // Movement
            SimMovement.Tick(unit, result, _state, fixedDelta);

            // Pending ranged damage
            SimBehavior.TickPendingDamage(unit, _state, fixedDelta, events);
        }
    }

    /// <summary>
    /// Tick casting timers for all summoners (runs in both Preparation and Battle).
    /// </summary>
    private void TickCasting(float fixedDelta, List<SimEvent> events)
    {
        for (int i = 0; i < _state.Summoners.Length; i++)
        {
            var summoner = _state.Summoners[i];
            if (!summoner.IsCasting) continue;

            summoner.CastingTimeRemaining -= fixedDelta;

            if (summoner.CastingTimeRemaining <= 0f)
            {
                summoner.CastingTimeRemaining = 0f;
                summoner.IsCasting = false;

                events.Add(new CastingCompletedEvent(
                    summoner.Team,
                    summoner.CastingCardIndex,
                    summoner.CastingSpawnPosition,
                    summoner.CastingNetworkId
                ));

                summoner.CastingCardIndex = -1;
                summoner.CastingSpawnPosition = SimVector3.Zero;
                summoner.CastingNetworkId = -1;
            }
        }
    }

    /// <summary>
    /// Death cleanup: decrement timers on dead units, remove expired, emit UnitRemovedEvent.
    /// </summary>
    private void TickDeathCleanup(float fixedDelta, List<SimEvent> events)
    {
        var toRemove = new List<int>();

        foreach (var (unitId, unit) in _state.Units)
        {
            if (!unit.IsAlive && unit.DeathCleanupTimer > 0)
            {
                unit.DeathCleanupTimer -= fixedDelta;
                if (unit.DeathCleanupTimer <= 0)
                {
                    toRemove.Add(unitId);
                }
            }
        }

        foreach (var unitId in toRemove)
        {
            _state.Units.Remove(unitId);
            events.Add(new UnitRemovedEvent(unitId));
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
public abstract class SimEvent { }

public class PhaseChangedEvent : SimEvent
{
    public GamePhase NewPhase { get; }
    public PhaseChangedEvent(GamePhase newPhase) => NewPhase = newPhase;
}

public class PrepTimerUpdatedEvent : SimEvent
{
    public float Remaining { get; }
    public PrepTimerUpdatedEvent(float remaining) => Remaining = remaining;
}

public class MatchTimeUpdatedEvent : SimEvent
{
    public float MatchTime { get; }
    public MatchTimeUpdatedEvent(float matchTime) => MatchTime = matchTime;
}

public class SummonerHpChangedEvent : SimEvent
{
    public int Team { get; }
    public float Hp { get; }
    public float MaxHp { get; }
    public SummonerHpChangedEvent(int team, float hp, float maxHp) { Team = team; Hp = hp; MaxHp = maxHp; }
}

public class SummonerManaChangedEvent : SimEvent
{
    public int Team { get; }
    public float Mana { get; }
    public float MaxMana { get; }
    public SummonerManaChangedEvent(int team, float mana, float maxMana) { Team = team; Mana = mana; MaxMana = maxMana; }
}

public class CastingStartedEvent : SimEvent
{
    public int Team { get; }
    public int CardIndex { get; }
    public float Duration { get; }
    public SimVector3 SpawnPosition { get; }
    public CastingStartedEvent(int team, int cardIndex, float duration, SimVector3 spawnPosition)
    { Team = team; CardIndex = cardIndex; Duration = duration; SpawnPosition = spawnPosition; }
}

public class CastingCompletedEvent : SimEvent
{
    public int Team { get; }
    public int CardIndex { get; }
    public SimVector3 SpawnPosition { get; }
    public int NetworkId { get; }
    public CastingCompletedEvent(int team, int cardIndex, SimVector3 spawnPosition, int networkId)
    { Team = team; CardIndex = cardIndex; SpawnPosition = spawnPosition; NetworkId = networkId; }
}

public class CardDrawnEvent : SimEvent
{
    public int Team { get; }
    public int HandIndex { get; }
    public string CatalogId { get; }
    public CardDrawnEvent(int team, int handIndex, string catalogId) { Team = team; HandIndex = handIndex; CatalogId = catalogId; }
}

public class HandChangedEvent : SimEvent
{
    public int Team { get; }
    public string[] Hand { get; }
    public HandChangedEvent(int team, string[] hand) { Team = team; Hand = hand; }
}

public class DeckRecycledEvent : SimEvent
{
    public int Team { get; }
    public DeckRecycledEvent(int team) => Team = team;
}

public class UnitRegisteredEvent : SimEvent
{
    public int UnitId { get; }
    public int NetworkId { get; }
    public string CatalogId { get; }
    public int Team { get; }
    public SimVector3 Position { get; }
    public UnitRegisteredEvent(int unitId, int networkId, string catalogId, int team, SimVector3 position)
    { UnitId = unitId; NetworkId = networkId; CatalogId = catalogId; Team = team; Position = position; }
}

public class UnitRemovedEvent : SimEvent
{
    public int UnitId { get; }
    public UnitRemovedEvent(int unitId) => UnitId = unitId;
}

public class GameOverEvent : SimEvent
{
    public int WinnerTeam { get; }
    public string Reason { get; }
    public GameOverEvent(int winnerTeam, string reason) { WinnerTeam = winnerTeam; Reason = reason; }
}

/// <summary>
/// A unit attacked another unit (for visual/audio feedback).
/// </summary>
public class UnitAttackedEvent : SimEvent
{
    public int AttackerUnitId { get; }
    public int TargetUnitId { get; }
    public UnitAttackedEvent(int attackerUnitId, int targetUnitId)
    { AttackerUnitId = attackerUnitId; TargetUnitId = targetUnitId; }
}

/// <summary>
/// A unit took damage (for visual feedback — flash, HP bar update).
/// </summary>
public class UnitDamagedEvent : SimEvent
{
    public int TargetUnitId { get; }
    public int AttackerUnitId { get; }
    public float Damage { get; }
    public bool IsCrit { get; }
    public UnitDamagedEvent(int targetUnitId, int attackerUnitId, float damage, bool isCrit)
    { TargetUnitId = targetUnitId; AttackerUnitId = attackerUnitId; Damage = damage; IsCrit = isCrit; }
}

/// <summary>
/// A unit died (for death animation, cleanup, kill tracking).
/// </summary>
public class UnitDiedSimEvent : SimEvent
{
    public int UnitId { get; }
    public int KillerUnitId { get; }
    public UnitDiedSimEvent(int unitId, int killerUnitId)
    { UnitId = unitId; KillerUnitId = killerUnitId; }
}

/// <summary>
/// A projectile hit a unit (for visual feedback — impact VFX, pierce tracking).
/// </summary>
public class ProjectileHitSimEvent : SimEvent
{
    public int ProjectileId { get; }
    public int TargetUnitId { get; }
    public ProjectileHitSimEvent(int projectileId, int targetUnitId)
    { ProjectileId = projectileId; TargetUnitId = targetUnitId; }
}

/// <summary>
/// A unit's activation state changed (for visual feedback).
/// </summary>
public class UnitActivationChangedEvent : SimEvent
{
    public int UnitId { get; }
    public int NewState { get; }
    public UnitActivationChangedEvent(int unitId, int newState)
    { UnitId = unitId; NewState = newState; }
}
