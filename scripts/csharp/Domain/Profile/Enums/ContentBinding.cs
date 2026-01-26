namespace ProjectSummoner.Domain.Profile.Enums;

/// <summary>
/// Defines how content (items, cards, etc.) is bound in terms of ownership.
/// This shared enum enables unified query patterns across different content types.
/// </summary>
public enum ContentBinding
{
    /// <summary>
    /// Content can be accessed and used by any summoner on the account.
    /// Examples: cosmetics, premium items, shared equipment.
    /// </summary>
    AccountWide,

    /// <summary>
    /// Content is bound to a specific summoner and only they can access it.
    /// Examples: progression rewards, earned equipment, summoner-specific unlocks.
    /// </summary>
    SummonerBound
}
