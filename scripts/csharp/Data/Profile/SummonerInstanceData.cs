using System.Collections.Generic;

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

    /// <summary>IDs of acquired boons from TraitCatalog.</summary>
    public List<string> AcquiredBoonIds { get; set; } = [];
}
