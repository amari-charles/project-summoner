using System.Collections.Generic;
using System.Linq;

namespace Fateforged.Simulation;

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
    public string WinCondition { get; set; } = "";
    public int KillCount { get; set; }
    public float WinConditionTimeLimit { get; set; }
    public int WinConditionKillTarget { get; set; }

    // Summoners (index 0 = player, index 1 = enemy)
    public SummonerData[] Summoners { get; } = new SummonerData[2]
    {
        new() { Team = 0 },
        new() { Team = 1 }
    };

    // Units (keyed by MatchState-local unit ID)
    public Dictionary<int, UnitData> Units { get; } = new();

    // Projectiles (keyed by MatchState-local projectile ID)
    public Dictionary<int, SimProjectileData> Projectiles { get; } = new();

    // Next unit ID counter
    private int _nextUnitId;

    // Next projectile ID counter
    private int _nextProjectileId;

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
    /// Get the next unique network ID for this match.
    /// Used by Simulation to assign NetworkIds to spawned units.
    /// Single source of truth — replaces NetworkIdRegistry.NextIdWithoutRegistering.
    /// </summary>
    public int NextNetworkId() => _nextNetworkId++;

    // Delayed effects (death explosions, timed AoE, etc.)
    public List<DelayedEffect> DelayedEffects { get; } = new();

    // Card data map — sim-local card data populated at match start
    public Dictionary<string, SimCardData> CardDataMap { get; } = new();

    // Pending commands — commands with ExecuteFrame <= FrameNumber are drained each tick
    public List<ICommand> PendingCommandBuffer { get; } = new();

    // Deterministic RNG — same seed on both host and client
    public DeterministicRng? Rng { get; set; }

    // =========================================================================
    // UNIT QUERY HELPERS (used by simulation for targeting/combat)
    // =========================================================================

    /// <summary>
    /// Get all alive, active units sorted by UnitId for deterministic iteration.
    /// </summary>
    public List<UnitData> GetAliveActiveUnits()
    {
        return Units.Values
            .Where(u => u.IsAlive && u.ActivationState == (int)ProjectSummoner.Units.ActivationState.Active)
            .OrderBy(u => u.UnitId)
            .ToList();
    }

    /// <summary>
    /// Get alive active units for a specific team.
    /// </summary>
    public List<UnitData> GetAliveActiveUnitsForTeam(int team)
    {
        return Units.Values
            .Where(u => u.IsAlive && u.ActivationState == (int)ProjectSummoner.Units.ActivationState.Active && u.Team == team)
            .OrderBy(u => u.UnitId)
            .ToList();
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
    /// Get the alive enemy summoner for a given team.
    /// Returns null if the enemy summoner is dead.
    /// </summary>
    public SummonerData? GetAliveEnemySummoner(int team)
    {
        int enemyTeam = team == 0 ? 1 : 0;
        var summoner = Summoners[enemyTeam];
        return summoner.IsAlive ? summoner : null;
    }
}
