using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Central state container for the entire match.
/// All logical gameplay state lives here. Godot nodes are the presentation layer.
///
/// Single mutation path: Simulation.Tick() owns ALL state mutations.
/// Unit3D is a visual puppet that reads from MatchState.
/// </summary>
public class MatchState
{
    // Frame tracking
    public long FrameNumber { get; set; }
    public float MatchTime { get; set; }

    // Phase
    public GamePhase Phase { get; set; } = GamePhase.Preparation;
    public float PrepTimeRemaining { get; set; }

    // Overtime
    public bool IsOvertime { get; set; }

    // Win condition
    public int? WinnerTeam { get; set; }
    public WinConditionType WinCondition { get; set; } = WinConditionType.DestroySummoner;
    public int KillCount { get; set; }
    public float WinConditionTimeLimit { get; set; }
    public int WinConditionKillTarget { get; set; }

    // PASS 3 combat telemetry counters.
    public int CombatTargetSwitchCount { get; set; }
    public int CombatBlockedTimeoutRetargetCount { get; set; }
    public int CombatWindupsStarted { get; set; }
    public int CombatWindupsCancelled { get; set; }

    // Summoners (index 0 = player, index 1 = enemy)
    public SummonerData[] Summoners { get; } =
        new SummonerData[2]
        {
            new() { Team = Team.Player },
            new() { Team = Team.Enemy },
        };

    // Units (keyed by MatchState-local unit ID)
    public Dictionary<int, UnitData> Units { get; } = new();

    // Projectiles (keyed by MatchState-local projectile ID)
    public Dictionary<int, SimProjectileData> Projectiles { get; } = new();

    // Target-owned melee slot containers (keyed by target id, including summoner target ids).
    public Dictionary<int, TargetSlotState> TargetSlotStates { get; } = new();

    // Next unit ID counter
    private int _nextUnitId;

    // Next projectile ID counter
    private int _nextProjectileId;

    // Next buff ID counter (instance-scoped for determinism across matches)
    private int _nextBuffId;

    // Next network ID counter — simulation owns NetworkId assignment
    private int _nextNetworkId = 1;

    /// <summary>
    /// Get the next unique unit ID for this match.
    /// </summary>
    public int NextUnitId() => _nextUnitId++;

    /// <summary>
    /// Get the next unique projectile ID for this match.
    /// </summary>
    public int NextProjectileId() => _nextProjectileId++;

    /// <summary>
    /// Get the next unique buff ID for this match.
    /// Instance-scoped (not static) to ensure determinism across matches.
    /// </summary>
    public int NextBuffId() => _nextBuffId++;

    /// <summary>
    /// Get the next unique network ID for this match.
    /// Used by Simulation to assign NetworkIds to spawned units.
    /// Single source of truth — replaces NetworkIdRegistry.NextIdWithoutRegistering.
    /// </summary>
    public int NextNetworkId() => _nextNetworkId++;

    // Delayed effects (death explosions, timed AoE, etc.)
    public List<DelayedEffect> DelayedEffects { get; } = new();

    // Card data map — sim-local card data populated at match start
    public Dictionary<SimCardCatalogId, SimCardData> CardDataMap { get; } = new();

    // Pending commands — commands with ExecuteFrame <= FrameNumber are drained each tick
    public List<ICommand> PendingCommandBuffer { get; } = new();

    // Deterministic RNG — same seed on both host and client
    public DeterministicRng? Rng { get; set; }

    // Unified trait runtime state (Pass 2 stub; Pass 3 full implementation)
    public MatchTraitRuntimeState TraitRuntimeState { get; set; } = MatchTraitRuntimeState.Empty();

    // =========================================================================
    // UNIT QUERY HELPERS (used by simulation for targeting/combat)
    // Reusable lists to avoid per-tick LINQ allocations in hot paths.
    // =========================================================================

    private readonly List<UnitData> _aliveActiveCache = new();
    private readonly List<UnitData> _aliveActiveTeamCache = new();

    /// <summary>
    /// Get all alive, active units sorted by UnitId for deterministic iteration.
    /// Returns a shared list — do not hold references across ticks.
    /// </summary>
    public List<UnitData> GetAliveActiveUnits()
    {
        _aliveActiveCache.Clear();
        foreach (var unit in Units.Values)
        {
            if (unit.IsAlive && unit.ActivationState == ActivationState.Active)
                _aliveActiveCache.Add(unit);
        }
        _aliveActiveCache.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));
        return _aliveActiveCache;
    }

    /// <summary>
    /// Get alive active units for a specific team.
    /// Returns a shared list — do not hold references across ticks.
    /// </summary>
    public List<UnitData> GetAliveActiveUnitsForTeam(int team)
    {
        _aliveActiveTeamCache.Clear();
        var targetTeam = (Team)team;
        foreach (var unit in Units.Values)
        {
            if (
                unit.IsAlive
                && unit.ActivationState == ActivationState.Active
                && unit.Team == targetTeam
            )
                _aliveActiveTeamCache.Add(unit);
        }
        _aliveActiveTeamCache.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));
        return _aliveActiveTeamCache;
    }

    /// <summary>
    /// Find a unit by its UnitId. Returns null if not found or dead.
    /// </summary>
    public UnitData? GetAliveUnit(int unitId)
    {
        return Units.TryGetValue(unitId, out var unit) && unit.IsAlive ? unit : null;
    }

    // =========================================================================
    // SUMMONER TARGET ID HELPERS
    // Convention: summoner target IDs are negative (-1 = team 0, -2 = team 1)
    // =========================================================================

    /// <summary>
    /// Check if a target ID refers to a summoner (negative ID).
    /// </summary>
    public static bool IsSummonerTarget(int? targetId)
    {
        return targetId.HasValue && targetId.Value < 0;
    }

    /// <summary>
    /// Get the team index from a summoner target ID.
    /// -1 → team 0, -2 → team 1.
    /// </summary>
    public static int GetSummonerTeamFromTargetId(int targetId)
    {
        return (-targetId) - 1;
    }

    /// <summary>
    /// Get the summoner target ID for a given team.
    /// team 0 → -1, team 1 → -2.
    /// </summary>
    public static int GetSummonerTargetId(int team)
    {
        return -(team + 1);
    }

    /// <summary>
    /// Get the opposing team index. 0 → 1, 1 → 0.
    /// </summary>
    public static int GetEnemyTeam(int team) => team == 0 ? 1 : 0;

    /// <summary>
    /// Get the alive enemy summoner for a given team.
    /// Returns null if the enemy summoner is dead.
    /// </summary>
    public SummonerData? GetAliveEnemySummoner(int team)
    {
        int enemyTeam = GetEnemyTeam(team);
        var summoner = Summoners[enemyTeam];
        return summoner.IsAlive ? summoner : null;
    }

    /// <summary>
    /// Release slot reservations/occupancy for dead entities and remove slot containers
    /// for invalid targets. Intended to run before target reacquire in commit-slot flow.
    /// </summary>
    public void ReleaseInvalidSlotReferences()
    {
        if (TargetSlotStates.Count == 0)
            return;

        var invalidTargets = new List<int>();
        foreach (var (targetId, slotState) in TargetSlotStates)
        {
            bool targetAlive;
            if (IsSummonerTarget(targetId))
            {
                int team = GetSummonerTeamFromTargetId(targetId);
                targetAlive = team >= 0 && team < Summoners.Length && Summoners[team].IsAlive;
            }
            else
            {
                targetAlive = GetAliveUnit(targetId) != null;
            }

            if (!targetAlive)
            {
                invalidTargets.Add(targetId);
                continue;
            }

            foreach (var slot in slotState.Slots)
            {
                bool reservedAlive =
                    slot.ReservedUnitId.HasValue && GetAliveUnit(slot.ReservedUnitId.Value) != null;
                bool occupiedAlive =
                    slot.OccupiedUnitId.HasValue && GetAliveUnit(slot.OccupiedUnitId.Value) != null;

                if (!reservedAlive)
                    slot.ReservedUnitId = null;
                if (!occupiedAlive)
                    slot.OccupiedUnitId = null;

                if (slot.OccupiedUnitId.HasValue)
                {
                    slot.OccupancyState = SlotOccupancyState.Occupied;
                }
                else if (slot.ReservedUnitId.HasValue)
                {
                    slot.OccupancyState = SlotOccupancyState.Reserved;
                }
                else
                {
                    slot.OccupancyState = SlotOccupancyState.Free;
                    slot.ReservationDistanceSq = float.MaxValue;
                    slot.ReservationUnitId = int.MaxValue;
                }
            }
        }

        foreach (int targetId in invalidTargets)
            TargetSlotStates.Remove(targetId);
    }
}
