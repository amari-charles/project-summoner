using System.Collections.Generic;
using ProjectSummoner.Data.Items;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Serialized data for a summoner instance in a player's profile.
/// </summary>
public class SummonerInstanceData
{
    /// <summary>Summoner catalog ID (e.g., "summoner_fire").</summary>
    public required string SummonerId { get; set; }

    /// <summary>Current level (1-10).</summary>
    public int Level { get; set; } = 1;

    /// <summary>XP towards next level.</summary>
    public int Xp { get; set; }

    /// <summary>
    /// [DEPRECATED] IDs of acquired boons from TraitCatalog.
    /// Kept for migration from v4 to v5. New profiles use EquippedItems instead.
    /// </summary>
    public List<string> AcquiredBoonIds { get; set; } = [];

    /// <summary>
    /// Equipped item instance IDs by slot.
    /// Keys: "grimoire", "weapon", "ring", "vestments"
    /// Values: Item instance ID or null if slot is empty.
    /// </summary>
    public Dictionary<string, string?> EquippedItems { get; set; } = new()
    {
        ["grimoire"] = null,
        ["weapon"] = null,
        ["ring"] = null,
        ["vestments"] = null
    };
}
