using System.Collections.Generic;
using System.Text.Json.Serialization;
using ProjectSummoner.Domain.Profile.Enums;

namespace ProjectSummoner.Domain.Profile.Collection;

/// <summary>
/// A card instance in the player's collection.
/// This is the single source of truth for card ownership data.
/// Replaces both CardInstanceData and PlayerCardInstance.
/// </summary>
public class CardInstance
{
    /// <summary>Unique instance ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>Card catalog ID (e.g., "fire_elemental").</summary>
    [JsonPropertyName("catalog_id")]
    public required string CatalogId { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = "";

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
    /// Serialized as int via DtoConverters for GDScript interop.
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
