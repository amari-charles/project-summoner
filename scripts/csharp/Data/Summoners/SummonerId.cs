namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Constants for all summoner IDs in the game.
/// Provides compile-time safety for summoner references.
/// </summary>
public static class SummonerId
{
    // =========================================================================
    // FATEFORGERS (Core starting summoners)
    // =========================================================================

    /// <summary>Cole - Fire Fateforger. Arrogant, competitive, cocky.</summary>
    public const string Cole = "summoner_cole";

    /// <summary>Selene - Water Fateforger. Gentle, caring, emotionally intelligent.</summary>
    public const string Selene = "summoner_selene";

    /// <summary>Mei - Wind Fateforger. Elusive, self-interested, loner.</summary>
    public const string Mei = "summoner_mei";

    /// <summary>Teo - Earth Fateforger. Gym rat, straightforward, reliable.</summary>
    public const string Teo = "summoner_teo";

    // =========================================================================
    // DEV/TEST SUMMONERS
    // =========================================================================

    /// <summary>Mana Test Summoner - High mana pool for testing</summary>
    public const string ManaTest = "summoner_mana_test";

    // =========================================================================
    // UTILITY ARRAYS
    // =========================================================================

    /// <summary>Core starting summoners (always available in selection).</summary>
    public static readonly string[] AllStarting = [Cole, Selene, Mei, Teo];

    /// <summary>Dev/test summoners (not available to players).</summary>
    public static readonly string[] AllDev = [ManaTest];

    /// <summary>Default summoner ID (used for fallbacks).</summary>
    public const string Default = Cole;
}
