namespace Fateforged.Data.Events;

/// <summary>
/// Strongly-typed identifier for campaign types.
/// Prevents typos and enables IDE autocomplete via CampaignIds static class.
/// </summary>
public readonly record struct CampaignId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(CampaignId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator CampaignId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset campaign ID.</summary>
    public static readonly CampaignId None = new("");
}

/// <summary>
/// All known campaign IDs. Use these instead of raw strings.
/// Example: CampaignIds.SummonersPath instead of "summoners_path"
/// </summary>
public static class CampaignIds
{
    /// <summary>Summoner's Path - The main campaign for all summoners</summary>
    public static readonly CampaignId SummonersPath = new("summoners_path");

    /// <summary>Test Arena - Debug campaign for testing core combat mechanics</summary>
    public static readonly CampaignId TestArena = new("test_arena");

    /// <summary>Default campaign for new players</summary>
    public static readonly CampaignId Default = SummonersPath;
}
