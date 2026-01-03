namespace ProjectSummoner.Constants;

/// <summary>
/// Group ID constants matching GDScript GroupIDs.
/// Use these instead of magic strings when querying groups.
///
/// Usage:
///   GetTree().GetNodesInGroup(GroupIDs.EnemySummoners)
/// </summary>
public static class GroupIDs
{
    // =========================================================================
    // UNIT GROUPS
    // =========================================================================

    /// <summary>All units (both teams)</summary>
    public const string Units = "units";

    /// <summary>Player team units only</summary>
    public const string PlayerUnits = "player_units";

    /// <summary>Enemy team units only</summary>
    public const string EnemyUnits = "enemy_units";

    // =========================================================================
    // BASE/STRUCTURE GROUPS
    // =========================================================================

    /// <summary>All bases (both teams)</summary>
    public const string Bases = "bases";

    /// <summary>Player team bases only</summary>
    public const string PlayerBases = "player_bases";

    /// <summary>Enemy team bases only</summary>
    public const string EnemyBases = "enemy_bases";

    // =========================================================================
    // CHARACTER GROUPS
    // =========================================================================

    /// <summary>All summoners (both teams)</summary>
    public const string Summoners = "summoners";

    /// <summary>Player summoners only</summary>
    public const string PlayerSummoners = "player_summoners";

    /// <summary>Enemy summoners only</summary>
    public const string EnemySummoners = "enemy_summoners";

    // =========================================================================
    // PROJECTILE GROUPS
    // =========================================================================

    /// <summary>All projectiles</summary>
    public const string Projectiles = "projectiles";

    // =========================================================================
    // STRUCTURE GROUPS
    // =========================================================================

    /// <summary>All structures (towers, walls, etc.)</summary>
    public const string Structures = "structures";

    // =========================================================================
    // FLYING UNIT GROUPS
    // =========================================================================

    /// <summary>All flying units (both teams)</summary>
    public const string FlyingUnits = "flying_units";

    // =========================================================================
    // UI GROUPS
    // =========================================================================

    /// <summary>Hand UI elements</summary>
    public const string HandUI = "hand_ui";

    /// <summary>Game UI layer</summary>
    public const string GameUI = "game_ui";

    /// <summary>Game controller references</summary>
    public const string GameController = "game_controller";

    // =========================================================================
    // UTILITY METHODS
    // =========================================================================

    /// <summary>
    /// Get the enemy units group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for enemy units</returns>
    public static string EnemyUnitsFor(int team)
    {
        return team == 0 ? EnemyUnits : PlayerUnits;
    }

    /// <summary>
    /// Get the ally units group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for ally units</returns>
    public static string AllyUnitsFor(int team)
    {
        return team == 0 ? PlayerUnits : EnemyUnits;
    }

    /// <summary>
    /// Get the enemy summoners group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for enemy summoners</returns>
    public static string EnemySummonersFor(int team)
    {
        return team == 0 ? EnemySummoners : PlayerSummoners;
    }

    /// <summary>
    /// Get the ally summoners group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for ally summoners</returns>
    public static string AllySummonersFor(int team)
    {
        return team == 0 ? PlayerSummoners : EnemySummoners;
    }

    /// <summary>
    /// Get the enemy bases group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for enemy bases</returns>
    public static string EnemyBasesFor(int team)
    {
        return team == 0 ? EnemyBases : PlayerBases;
    }

    /// <summary>
    /// Get the ally bases group for a given team.
    /// </summary>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Group name for ally bases</returns>
    public static string AllyBasesFor(int team)
    {
        return team == 0 ? PlayerBases : EnemyBases;
    }
}
