using System.Collections.Generic;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Shared data store for campaign and battle data loaded from GDScript.
/// All campaign handlers reference this shared state.
/// </summary>
public class CampaignDataStore
{
    /// <summary>Campaign data indexed by campaign_id.</summary>
    public Dictionary<string, Godot.Collections.Dictionary> Campaigns { get; } = [];

    /// <summary>Battle data indexed by battle_id (across all campaigns).</summary>
    public Dictionary<string, Godot.Collections.Dictionary> Battles { get; } = [];

    /// <summary>Current campaign ID.</summary>
    public string CurrentCampaignId { get; set; } = "";

    /// <summary>List of completed battle IDs for the current campaign context.</summary>
    public List<string> CompletedBattles { get; } = [];

    /// <summary>Clear all data.</summary>
    public void Clear()
    {
        Campaigns.Clear();
        Battles.Clear();
        CompletedBattles.Clear();
        CurrentCampaignId = "";
    }
}
