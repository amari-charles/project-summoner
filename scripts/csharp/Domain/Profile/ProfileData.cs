using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Shop;
using Fateforged.Domain.Profile.Summoners;

namespace Fateforged.Domain.Profile;

/// <summary>
/// Complete player profile data.
/// This is the top-level aggregate root for all profile persistence.
/// </summary>
public class ProfileData
{
    /// <summary>Current save version for migrations.</summary>
    public const int CurrentVersion = 6;

    /// <summary>Save data version.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Unique profile identifier.</summary>
    [JsonPropertyName("profile_id")]
    public ProfileId ProfileId { get; set; } = Profile.ProfileId.None;

    /// <summary>Last update timestamp (Unix time).</summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    /// <summary>Catalog version for compatibility checks.</summary>
    [JsonPropertyName("catalog_version")]
    public string CatalogVersion { get; set; } = "1.0.0";

    /// <summary>Player resources (gold, gems, etc.).</summary>
    [JsonPropertyName("resources")]
    public Resources Resources { get; set; } = new();

    /// <summary>Player's card collection.</summary>
    [JsonPropertyName("collection")]
    public List<CardInstance> Collection { get; set; } = [];

    /// <summary>Array of unlocked summoner IDs.</summary>
    [JsonPropertyName("unlocked_summoners")]
    public List<SummonerId> UnlockedSummoners { get; set; } = [];

    /// <summary>Summoner instance data (level, XP, equipped items).</summary>
    [JsonPropertyName("summoner_instances")]
    public List<SummonerInstance> SummonerInstances { get; set; } = [];

    /// <summary>Player's item inventory.</summary>
    [JsonPropertyName("items")]
    public List<ItemInstance> Items { get; set; } = [];

    /// <summary>Player's decks.</summary>
    [JsonPropertyName("decks")]
    public List<Deck> Decks { get; set; } = [];

    /// <summary>Per-summoner campaign progress.</summary>
    [JsonPropertyName("campaign_progress")]
    public Dictionary<string, CampaignProgress> CampaignProgressMap { get; set; } = [];

    /// <summary>Shared (account-wide) campaign progress.</summary>
    [JsonPropertyName("shared_campaign_progress")]
    public CampaignProgress SharedCampaignProgress { get; set; } = new();

    /// <summary>Shop purchase tracking ("shop_id::offering_id::refresh_epoch" -> count).</summary>
    [JsonPropertyName("shop_purchases")]
    public Dictionary<string, int> ShopPurchases { get; set; } = [];

    /// <summary>Per-shop refresh state tracking.</summary>
    [JsonPropertyName("shop_refresh_state")]
    public Dictionary<string, ShopRefreshState> ShopRefreshStateMap { get; set; } = [];

    /// <summary>Cosmetic items (skins, card backs, themes).</summary>
    [JsonPropertyName("cosmetics")]
    public Cosmetics Cosmetics { get; set; } = new();

    /// <summary>Battle emotes.</summary>
    [JsonPropertyName("emotes")]
    public Emotes Emotes { get; set; } = new();

    /// <summary>Miscellaneous metadata.</summary>
    [JsonPropertyName("meta")]
    public AccountMeta Meta { get; set; } = new();

    /// <summary>Last match data.</summary>
    [JsonPropertyName("last_match")]
    public LastMatch LastMatch { get; set; } = new();

    /// <summary>User settings.</summary>
    [JsonPropertyName("settings")]
    public Settings Settings { get; set; } = new();

    /// <summary>Write-ahead log for sync support.</summary>
    [JsonPropertyName("wal")]
    public List<Dictionary<string, object>> Wal { get; set; } = [];
}
