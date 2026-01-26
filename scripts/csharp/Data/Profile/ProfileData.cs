using System.Collections.Generic;
using System.Text.Json.Serialization;
using ProjectSummoner.Data.Items;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Complete player profile data.
/// This is the top-level structure persisted to JSON.
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
    public string ProfileId { get; set; } = "";

    /// <summary>Last update timestamp (Unix time).</summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    /// <summary>Catalog version for compatibility checks.</summary>
    [JsonPropertyName("catalog_version")]
    public string CatalogVersion { get; set; } = "1.0.0";

    /// <summary>Player resources (gold, gems, etc.).</summary>
    [JsonPropertyName("resources")]
    public ResourceData Resources { get; set; } = new();

    /// <summary>Player's card collection.</summary>
    [JsonPropertyName("collection")]
    public List<CardInstanceData> Collection { get; set; } = [];

    /// <summary>Array of unlocked summoner IDs.</summary>
    [JsonPropertyName("unlocked_summoners")]
    public List<string> UnlockedSummoners { get; set; } = [];

    /// <summary>Summoner instance data (level, XP, equipped items).</summary>
    [JsonPropertyName("summoner_instances")]
    public List<SummonerInstanceData> SummonerInstances { get; set; } = [];

    /// <summary>Player's item inventory.</summary>
    [JsonPropertyName("items")]
    public List<ItemInstanceData> Items { get; set; } = [];

    /// <summary>Player's decks.</summary>
    [JsonPropertyName("decks")]
    public List<DeckData> Decks { get; set; } = [];

    /// <summary>Per-summoner campaign progress.</summary>
    [JsonPropertyName("campaign_progress")]
    public Dictionary<string, CampaignProgressData> CampaignProgress { get; set; } = [];

    /// <summary>Shared (account-wide) campaign progress.</summary>
    [JsonPropertyName("shared_campaign_progress")]
    public CampaignProgressData SharedCampaignProgress { get; set; } = new();

    /// <summary>Shop purchase tracking ("shop_id::offering_id::refresh_epoch" -> count).</summary>
    [JsonPropertyName("shop_purchases")]
    public Dictionary<string, int> ShopPurchases { get; set; } = [];

    /// <summary>Per-shop refresh state tracking.</summary>
    [JsonPropertyName("shop_refresh_state")]
    public Dictionary<string, ShopRefreshState> ShopRefreshState { get; set; } = [];

    /// <summary>Cosmetic items (skins, card backs, themes).</summary>
    [JsonPropertyName("cosmetics")]
    public CosmeticsData Cosmetics { get; set; } = new();

    /// <summary>Battle emotes.</summary>
    [JsonPropertyName("emotes")]
    public EmotesData Emotes { get; set; } = new();

    /// <summary>Miscellaneous metadata.</summary>
    [JsonPropertyName("meta")]
    public MetaData Meta { get; set; } = new();

    /// <summary>Last match data.</summary>
    [JsonPropertyName("last_match")]
    public LastMatchData LastMatch { get; set; } = new();

    /// <summary>User settings.</summary>
    [JsonPropertyName("settings")]
    public SettingsData Settings { get; set; } = new();

    /// <summary>Write-ahead log for sync support.</summary>
    [JsonPropertyName("wal")]
    public List<Dictionary<string, object>> Wal { get; set; } = [];
}

/// <summary>
/// Shop refresh state for a specific shop.
/// </summary>
public class ShopRefreshState
{
    /// <summary>Current refresh epoch.</summary>
    [JsonPropertyName("refresh_epoch")]
    public int RefreshEpoch { get; set; }

    /// <summary>ISO timestamp of last refresh.</summary>
    [JsonPropertyName("last_refresh_at")]
    public string LastRefreshAt { get; set; } = "";
}
