using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Complete player profile data.
/// This is the top-level structure persisted to JSON.
/// </summary>
public class ProfileData
{
    /// <summary>Current save version for migrations.</summary>
    public const int CurrentVersion = 4;

    /// <summary>Save data version.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Unique profile identifier.</summary>
    public string ProfileId { get; set; } = "";

    /// <summary>Last update timestamp (Unix time).</summary>
    public long UpdatedAt { get; set; }

    /// <summary>Catalog version for compatibility checks.</summary>
    public string CatalogVersion { get; set; } = "1.0.0";

    /// <summary>Player resources (gold, gems, etc.).</summary>
    public ResourceData Resources { get; set; } = new();

    /// <summary>Player's card collection.</summary>
    public List<CardInstanceData> Collection { get; set; } = [];

    /// <summary>Array of unlocked summoner IDs.</summary>
    public List<string> UnlockedSummoners { get; set; } = [];

    /// <summary>Summoner instance data (level, XP, boons).</summary>
    public List<SummonerInstanceData> SummonerInstances { get; set; } = [];

    /// <summary>Player's decks.</summary>
    public List<DeckData> Decks { get; set; } = [];

    /// <summary>Per-summoner campaign progress.</summary>
    public Dictionary<string, CampaignProgressData> CampaignProgress { get; set; } = [];

    /// <summary>Shared (account-wide) campaign progress.</summary>
    public CampaignProgressData SharedCampaignProgress { get; set; } = new();

    /// <summary>Shop purchase tracking ("shop_id::offering_id::refresh_epoch" -> count).</summary>
    public Dictionary<string, int> ShopPurchases { get; set; } = [];

    /// <summary>Per-shop refresh state tracking.</summary>
    public Dictionary<string, ShopRefreshState> ShopRefreshState { get; set; } = [];

    /// <summary>Cosmetic items (skins, card backs, themes).</summary>
    public CosmeticsData Cosmetics { get; set; } = new();

    /// <summary>Battle emotes.</summary>
    public EmotesData Emotes { get; set; } = new();

    /// <summary>Miscellaneous metadata.</summary>
    public MetaData Meta { get; set; } = new();

    /// <summary>Last match data.</summary>
    public LastMatchData LastMatch { get; set; } = new();

    /// <summary>User settings.</summary>
    public SettingsData Settings { get; set; } = new();

    /// <summary>Write-ahead log for sync support.</summary>
    public List<Dictionary<string, object>> Wal { get; set; } = [];
}

/// <summary>
/// Shop refresh state for a specific shop.
/// </summary>
public class ShopRefreshState
{
    /// <summary>Current refresh epoch.</summary>
    public int RefreshEpoch { get; set; }

    /// <summary>ISO timestamp of last refresh.</summary>
    public string LastRefreshAt { get; set; } = "";
}
