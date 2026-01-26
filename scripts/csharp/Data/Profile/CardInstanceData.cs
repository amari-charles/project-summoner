using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Represents a card instance in the player's collection.
/// </summary>
public class CardInstanceData
{
    /// <summary>Unique instance ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = "";

    /// <summary>Card catalog ID (e.g., "fire_elemental").</summary>
    [JsonPropertyName("catalog_id")]
    public required string CatalogId { get; set; }

    /// <summary>Card rarity (common, rare, epic, legendary).</summary>
    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "common";

    /// <summary>Card progression level (1-10).</summary>
    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    /// <summary>XP towards next level.</summary>
    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    /// <summary>Array of chosen upgrade IDs.</summary>
    [JsonPropertyName("upgrades")]
    public List<string> Upgrades { get; set; } = [];

    /// <summary>Roll JSON for card variants (nullable).</summary>
    [JsonPropertyName("roll_json")]
    public string? RollJson { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// Ownership binding type.
    /// AccountWide = any summoner can use, SummonerBound = only bound summoner.
    /// Note: Serialized as int via EnumSerializers in DtoConverters for GDScript interop.
    /// </summary>
    [JsonPropertyName("binding")]
    public ContentBinding Binding { get; set; } = ContentBinding.AccountWide;

    /// <summary>
    /// For SummonerBound cards, the summoner ID that owns this card.
    /// Null for AccountWide cards.
    /// </summary>
    [JsonPropertyName("bound_to")]
    public string? BoundToSummonerId { get; set; }
}
