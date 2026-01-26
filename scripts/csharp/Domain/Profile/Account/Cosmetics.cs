using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Domain.Profile.Account;

/// <summary>
/// Player's cosmetic items (skins, card backs, UI themes).
/// </summary>
public class Cosmetics
{
    /// <summary>Array of owned cosmetic IDs.</summary>
    [JsonPropertyName("owned")]
    public List<string> Owned { get; set; } = [];

    /// <summary>Currently equipped cosmetics by slot.</summary>
    [JsonPropertyName("equipped")]
    public EquippedCosmetics Equipped { get; set; } = new();

    /// <summary>Summoner-specific skin mappings (summoner_id -> skin_id).</summary>
    [JsonPropertyName("summoner_skins")]
    public Dictionary<string, string> SummonerSkins { get; set; } = [];
}

/// <summary>
/// Currently equipped cosmetics.
/// </summary>
public class EquippedCosmetics
{
    /// <summary>Equipped card back cosmetic ID.</summary>
    [JsonPropertyName("card_back")]
    public string CardBack { get; set; } = "";

    /// <summary>Equipped UI theme cosmetic ID.</summary>
    [JsonPropertyName("ui_theme")]
    public string UiTheme { get; set; } = "";
}
