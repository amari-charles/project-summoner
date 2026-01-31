namespace ProjectSummoner.Data.Events;

/// <summary>
/// Campaign ID constants - type-safe campaign references.
/// Mirrors GDScript CampaignIDs for C# code.
/// </summary>
public static class CampaignId
{
    /// <summary>Summoner's Path - The main campaign for all summoners</summary>
    public const string SummonersPath = "summoners_path";

    /// <summary>Test Arena - Debug campaign for testing core combat mechanics</summary>
    public const string TestArena = "test_arena";

    /// <summary>Default campaign for new players</summary>
    public const string Default = SummonersPath;
}
