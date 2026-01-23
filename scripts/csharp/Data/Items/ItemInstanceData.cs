using System.Text.Json.Serialization;

namespace ProjectSummoner.Data.Items;

/// <summary>
/// Represents an item instance owned by the player.
/// Each instance has a unique ID and tracks which summoner has it equipped (if any).
/// </summary>
public class ItemInstanceData
{
    /// <summary>Unique instance ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>Catalog ID referencing the ItemDefinition.</summary>
    [JsonPropertyName("catalog_id")]
    public required string CatalogId { get; set; }

    /// <summary>
    /// Summoner ID that has this item equipped, or null if unequipped.
    /// For SummonerBound items, this is also the owner.
    /// </summary>
    [JsonPropertyName("equipped_by")]
    public string? EquippedBySummonerId { get; set; }

    /// <summary>
    /// For SummonerBound items, the summoner ID that owns this item.
    /// AccountWide items have this set to null.
    /// </summary>
    [JsonPropertyName("bound_to")]
    public string? BoundToSummonerId { get; set; }

    /// <summary>
    /// Equipment slot this item is currently in, or null if not equipped.
    /// </summary>
    [JsonPropertyName("slot")]
    public string? EquippedSlot { get; set; }
}
