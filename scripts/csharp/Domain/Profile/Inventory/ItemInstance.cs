using System.Text.Json.Serialization;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Summoners;

namespace ProjectSummoner.Domain.Profile.Inventory;

/// <summary>
/// Represents an item instance owned by the player.
/// Each instance has a unique ID and tracks which summoner has it equipped (if any).
/// </summary>
public class ItemInstance
{
    /// <summary>Unique instance ID (UUID).</summary>
    [JsonPropertyName("id")]
    public required ItemId Id { get; set; }

    /// <summary>Catalog ID referencing the ItemDefinition.</summary>
    [JsonPropertyName("catalog_id")]
    public required ItemId CatalogId { get; set; }

    /// <summary>
    /// Summoner ID that has this item equipped, or null if unequipped.
    /// For SummonerBound items, this is also the owner.
    /// </summary>
    [JsonPropertyName("equipped_by")]
    public SummonerId? EquippedBySummonerId { get; set; }

    /// <summary>
    /// For SummonerBound items, the summoner ID that owns this item.
    /// AccountWide items have this set to null.
    /// </summary>
    [JsonPropertyName("bound_to")]
    public SummonerId? BoundToSummonerId { get; set; }

    /// <summary>
    /// Equipment slot this item is currently in, or null if not equipped.
    /// Serialization handled by DtoConverters.
    /// </summary>
    [JsonIgnore]
    public ItemSlot? EquippedSlot { get; set; }
}
