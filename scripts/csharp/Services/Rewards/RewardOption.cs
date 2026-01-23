namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Represents a single reward option that can be presented to the player.
/// Used for flexible reward screens where players choose from multiple options.
/// </summary>
public class RewardOption
{
    /// <summary>Type of reward (card, gold, item, etc.).</summary>
    public required RewardType Type { get; init; }

    /// <summary>
    /// ID of the reward (catalog_id for cards, item_id for items, etc.).
    /// Empty for gold/essence rewards.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>Amount for stackable rewards (gold, essence, card quantity).</summary>
    public int Amount { get; init; } = 1;

    /// <summary>Rarity for card rewards.</summary>
    public string Rarity { get; init; } = "common";

    /// <summary>
    /// Whether this is a guaranteed option (summoner-themed) vs pool-drawn.
    /// Guaranteed options are marked with a special indicator in UI.
    /// </summary>
    public bool IsGuaranteed { get; init; }

    /// <summary>Display name (localized).</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Description (localized).</summary>
    public string Description { get; init; } = "";

    /// <summary>Icon path for UI display.</summary>
    public string IconPath { get; init; } = "";

    /// <summary>Element of the reward (for cards).</summary>
    public string Element { get; init; } = "";
}

/// <summary>
/// Types of rewards that can be offered.
/// </summary>
public enum RewardType
{
    /// <summary>A card to add to the collection.</summary>
    Card,

    /// <summary>Campaign-scoped gold.</summary>
    CampaignGold,

    /// <summary>Account-wide gold (deprecated, use CampaignGold).</summary>
    Gold,

    /// <summary>Gems (premium currency).</summary>
    Gems,

    /// <summary>Essence for crafting/upgrades.</summary>
    Essence,

    /// <summary>Card fragments.</summary>
    Fragments,

    /// <summary>An equippable item (Phase 4).</summary>
    Item,

    /// <summary>A summoner unlock.</summary>
    Summoner,

    /// <summary>A cosmetic unlock.</summary>
    Cosmetic,

    /// <summary>An emote unlock.</summary>
    Emote
}

/// <summary>
/// Modes for filtering rewards based on player's collection.
/// </summary>
public enum CollectionFilterMode
{
    /// <summary>No filtering - show all eligible rewards.</summary>
    None,

    /// <summary>
    /// Exclude cards the player already owns.
    /// Use for new card acquisition rewards.
    /// </summary>
    ExcludeOwned,

    /// <summary>
    /// Allow owned cards but not exact duplicates.
    /// Use for rewards where duplicates provide XP.
    /// </summary>
    ExcludeDuplicates
}
