using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Shop;

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
