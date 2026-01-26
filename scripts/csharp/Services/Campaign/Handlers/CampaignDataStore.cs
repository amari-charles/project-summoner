using System.Collections.Generic;

namespace ProjectSummoner.Services.Campaign.Handlers;

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

    /// <summary>Set of campaign IDs that use shared (account-wide) progress.</summary>
    public HashSet<string> SharedCampaigns { get; } = [];

    /// <summary>Current campaign ID.</summary>
    public string CurrentCampaignId { get; set; } = "";

    /// <summary>List of completed battle IDs for the current campaign context.</summary>
    public List<string> CompletedBattles { get; } = [];

    /// <summary>Clear all data.</summary>
    public void Clear()
    {
        Campaigns.Clear();
        Battles.Clear();
        SharedCampaigns.Clear();
        CompletedBattles.Clear();
        CurrentCampaignId = "";
    }

    /// <summary>Check if a campaign uses shared (account-wide) progress.</summary>
    public bool IsSharedCampaign(string campaignId) => SharedCampaigns.Contains(campaignId);
}
