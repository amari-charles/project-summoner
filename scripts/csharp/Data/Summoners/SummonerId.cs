namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Constants for all summoner IDs in the game.
/// Provides compile-time safety for summoner references.
/// </summary>
public static class SummonerId
{
    // =========================================================================
    // STARTING SUMMONERS (Core 4 - Always available at start)
    // =========================================================================

    /// <summary>Fire Summoner - Pyralis</summary>
    public const string Fire = "summoner_fire";

    /// <summary>Water Summoner - Aquira</summary>
    public const string Water = "summoner_water";

    /// <summary>Wind Summoner - Zephyrion</summary>
    public const string Wind = "summoner_wind";

    /// <summary>Earth Summoner - Terravorn</summary>
    public const string Earth = "summoner_earth";

    // =========================================================================
    // RANDOM POOL SUMMONERS (Starter-only, available via Random option)
    // =========================================================================

    /// <summary>Shadow Initiate (starter-only)</summary>
    public const string ShadowInitiate = "summoner_shadow_initiate";

    // =========================================================================
    // PURCHASABLE SUMMONERS (Available via Premium Store)
    // =========================================================================

    /// <summary>Lightning Adept - Fast, high-risk/reward glass cannon</summary>
    public const string LightningAdept = "summoner_lightning_adept";

    /// <summary>Verdant Sage - Life element healer/support summoner</summary>
    public const string VerdantSage = "summoner_verdant_sage";

    /// <summary>Void Walker - Death element with draining abilities</summary>
    public const string VoidWalker = "summoner_void_walker";

    // =========================================================================
    // DEV/TEST SUMMONERS
    // =========================================================================

    /// <summary>Mana Test Summoner - High mana pool for testing</summary>
    public const string ManaTest = "summoner_mana_test";

    // =========================================================================
    // UTILITY ARRAYS
    // =========================================================================

    /// <summary>Core starting summoners (always available in selection).</summary>
    public static readonly string[] AllStarting = [Fire, Water, Wind, Earth];

    /// <summary>Random pool summoners (starter-only, included in random selection).</summary>
    public static readonly string[] AllRandomPool = [ShadowInitiate];

    /// <summary>Purchasable summoners (available via Premium Store).</summary>
    public static readonly string[] AllPurchasable = [LightningAdept, VerdantSage, VoidWalker];

    /// <summary>Dev/test summoners (not available to players).</summary>
    public static readonly string[] AllDev = [ManaTest];

    /// <summary>Default summoner ID (used for fallbacks).</summary>
    public const string Default = Fire;
}
