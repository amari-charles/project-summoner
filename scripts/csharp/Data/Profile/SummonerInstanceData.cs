using System.Collections.Generic;
using System.Text.Json.Serialization;
using ProjectSummoner.Data.Items;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Serialized data for a summoner instance in a player's profile.
/// </summary>
public class SummonerInstanceData
{
    /// <summary>Summoner catalog ID (e.g., "summoner_fire").</summary>
    [JsonPropertyName("summoner_id")]
    public required string SummonerId { get; set; }

    /// <summary>Current level (1-10).</summary>
    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    /// <summary>XP towards next level.</summary>
    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    /// <summary>
    /// [DEPRECATED] IDs of acquired boons from TraitCatalog.
    /// Kept for migration from v4 to v5. New profiles use EquippedItems instead.
    /// </summary>
    [JsonPropertyName("acquired_boon_ids")]
    public List<string> AcquiredBoonIds { get; set; } = [];

    /// <summary>
    /// Equipped item instance IDs by slot.
    /// Values: Item instance ID or null if slot is empty.
    /// Note: JsonIgnore because Dictionary&lt;ItemSlot, string?&gt; requires custom handling.
    /// Serialized to {"weapon": "id", ...} via DtoConverters for GDScript interop.
    /// </summary>
    [JsonIgnore]
    public Dictionary<ItemSlot, string?> EquippedItems { get; set; } = new()
    {
        [ItemSlot.Weapon] = null,
        [ItemSlot.Ring1] = null,
        [ItemSlot.Ring2] = null,
        [ItemSlot.Vestments] = null
    };
}
