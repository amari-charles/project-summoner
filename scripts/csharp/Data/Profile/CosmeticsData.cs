using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player's cosmetic items (skins, card backs, UI themes).
/// </summary>
public class CosmeticsData
{
    /// <summary>Array of owned cosmetic IDs.</summary>
    public List<string> Owned { get; set; } = [];

    /// <summary>Currently equipped cosmetics by slot.</summary>
    public EquippedCosmetics Equipped { get; set; } = new();

    /// <summary>Summoner-specific skin mappings (summoner_id -> skin_id).</summary>
    public Dictionary<string, string> SummonerSkins { get; set; } = [];
}

/// <summary>
/// Currently equipped cosmetics.
/// </summary>
public class EquippedCosmetics
{
    /// <summary>Equipped card back cosmetic ID.</summary>
    public string CardBack { get; set; } = "";

    /// <summary>Equipped UI theme cosmetic ID.</summary>
    public string UiTheme { get; set; } = "";
}
