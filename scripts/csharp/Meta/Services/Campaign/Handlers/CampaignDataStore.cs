using System.Collections.Generic;
using Fateforged.Data.Events;
using Fateforged.Meta.Campaign;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Shared data store for campaign and battle data.
/// All campaign handlers reference this shared state.
/// Fully typed — no raw string keys or Godot.Collections.Dictionary values.
/// </summary>
public class CampaignDataStore
{
    /// <summary>Campaign definitions indexed by campaign ID.</summary>
    public Dictionary<CampaignId, CampaignDefinition> Campaigns { get; } = [];

    /// <summary>Event definitions indexed by event ID (across all campaigns).</summary>
    public Dictionary<EventId, EventDefinition> Events { get; } = [];

    /// <summary>Current campaign ID.</summary>
    public CampaignId CurrentCampaignId { get; set; } = CampaignId.None;

    /// <summary>List of completed battle IDs for the current campaign context.</summary>
    public List<BattleId> CompletedBattles { get; } = [];

    /// <summary>Clear all data.</summary>
    public void Clear()
    {
        Campaigns.Clear();
        Events.Clear();
        CompletedBattles.Clear();
        CurrentCampaignId = CampaignId.None;
    }
}
