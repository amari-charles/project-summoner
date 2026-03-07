namespace Fateforged.Data.Summoners;

/// <summary>
/// Strongly-typed identifier for summoner types.
/// Prevents typos and enables IDE autocomplete via SummonerIds static class.
/// </summary>
public readonly record struct SummonerId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(SummonerId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator SummonerId(string value) => new(value);

    /// <summary>Create a SummonerId from a string. Standardized factory for facade boundaries.</summary>
    public static SummonerId FromString(string id) => new(id);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset summoner ID.</summary>
    public static readonly SummonerId None = new("");
}

/// <summary>
/// All known summoner IDs. Use these instead of raw strings.
/// Example: SummonerIds.Cole instead of "summoner_cole"
/// </summary>
public static class SummonerIds
{
    // =========================================================================
    // FATEFORGERS (Core starting summoners)
    // =========================================================================

    /// <summary>Cole - Fire Fateforger. Arrogant, competitive, cocky.</summary>
    public static readonly SummonerId Cole = new("summoner_cole");

    /// <summary>Selene - Water Fateforger. Gentle, caring, emotionally intelligent.</summary>
    public static readonly SummonerId Selene = new("summoner_selene");

    /// <summary>Mei - Wind Fateforger. Elusive, self-interested, loner.</summary>
    public static readonly SummonerId Mei = new("summoner_mei");

    /// <summary>Teo - Earth Fateforger. Gym rat, straightforward, reliable.</summary>
    public static readonly SummonerId Teo = new("summoner_teo");

    // =========================================================================
    // DEV/TEST SUMMONERS
    // =========================================================================

    /// <summary>Mana Test Summoner - High mana pool for testing</summary>
    public static readonly SummonerId ManaTest = new("summoner_mana_test");

    // =========================================================================
    // UTILITY ARRAYS
    // =========================================================================

    /// <summary>Core starting summoners (always available in selection).</summary>
    public static readonly SummonerId[] AllStarting = [Cole, Selene, Mei, Teo];

    /// <summary>Dev/test summoners (not available to players).</summary>
    public static readonly SummonerId[] AllDev = [ManaTest];

    /// <summary>Default summoner ID (used for fallbacks).</summary>
    public static readonly SummonerId Default = Cole;
}
