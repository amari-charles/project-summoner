using System.Collections.Generic;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Represents a card instance in the player's collection.
/// </summary>
public class CardInstanceData
{
    /// <summary>Unique instance ID (UUID).</summary>
    public required string Id { get; set; }

    /// <summary>Profile ID reference.</summary>
    public string ProfileId { get; set; } = "";

    /// <summary>Card catalog ID (e.g., "fire_elemental").</summary>
    public required string CatalogId { get; set; }

    /// <summary>Card rarity (common, rare, epic, legendary).</summary>
    public string Rarity { get; set; } = "common";

    /// <summary>Card progression level (1-10).</summary>
    public int Level { get; set; } = 1;

    /// <summary>XP towards next level.</summary>
    public int Xp { get; set; }

    /// <summary>Array of chosen upgrade IDs.</summary>
    public List<string> Upgrades { get; set; } = [];

    /// <summary>Roll JSON for card variants (nullable).</summary>
    public string? RollJson { get; set; }

    /// <summary>Creation timestamp.</summary>
    public long CreatedAt { get; set; }
}
