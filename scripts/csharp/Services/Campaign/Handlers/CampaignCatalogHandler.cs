using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Events;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Handles campaign and battle catalog queries.
/// Now backed by EventCatalog and CampaignCatalog instead of GDScript data.
/// </summary>
public class CampaignCatalogHandler
{
    private readonly CampaignDataStore _store;
    private readonly CampaignProgressHandler _progress;

    public CampaignCatalogHandler(CampaignDataStore store, CampaignProgressHandler progress)
    {
        _store = store;
        _progress = progress;
    }

    // =========================================================================
    // DATA LOADING
    // =========================================================================

    /// <summary>
    /// Initialize catalog data from C# EventCatalog and CampaignCatalog.
    /// Called once at startup. No longer loads from GDScript.
    /// </summary>
    public void Initialize()
    {
        _store.Campaigns.Clear();
        _store.Battles.Clear();

        // Load all campaigns from CampaignCatalog
        foreach (var campaign in CampaignCatalog.GetAllCampaigns())
        {
            var campaignDict = CampaignCatalog.ToDictionary(campaign);
            _store.Campaigns[campaign.Id] = campaignDict;

            // Load events/battles for this campaign
            foreach (var eventId in campaign.EventIds)
            {
                var evt = EventCatalog.GetEvent(eventId);
                if (evt == null) continue;

                var eventDict = EventCatalog.ToDictionary(evt);
                // Add event_type for UI type checking
                eventDict["event_type"] = GetEventTypeForUI(evt.Type);
                _store.Battles[eventId] = eventDict;
            }
        }

        GD.Print($"CampaignCatalogHandler: Initialized {_store.Campaigns.Count} campaigns with {_store.Battles.Count} total events from C# catalogs");
    }

    private static string GetEventTypeForUI(EventType type) => type switch
    {
        EventType.Battle or EventType.Elite or EventType.Boss => "battle",
        EventType.Caravan => "caravan",
        EventType.Choice => "choice",
        EventType.Rest => "rest",
        EventType.Story => "story",
        _ => "battle"
    };

    // =========================================================================
    // CAMPAIGN QUERIES
    // =========================================================================

    /// <summary>Get all campaigns with unlock status.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCampaigns()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var campaign in CampaignCatalog.GetAllCampaignsSorted())
        {
            var dict = CampaignCatalog.ToDictionary(campaign);
            dict["is_unlocked"] = IsCampaignUnlocked(campaign.Id);
            result.Add(dict);
        }

        return result;
    }

    /// <summary>Get a specific campaign's metadata.</summary>
    public Godot.Collections.Dictionary GetCampaign(string campaignId)
    {
        var campaign = CampaignCatalog.GetCampaign(new CampaignId(campaignId));
        if (campaign == null) return new Godot.Collections.Dictionary();
        return CampaignCatalog.ToDictionary(campaign);
    }

    /// <summary>Check if a campaign is unlocked.</summary>
    public bool IsCampaignUnlocked(string campaignId)
    {
        var campaign = CampaignCatalog.GetCampaign(new CampaignId(campaignId));
        if (campaign == null) return false;

        // No requirements = unlocked
        if (campaign.UnlockRequirements.Count == 0) return true;

        // Check all requirements are met
        foreach (var req in campaign.UnlockRequirements)
        {
            if (!_progress.IsCampaignComplete(req))
                return false;
        }

        return true;
    }

    // =========================================================================
    // BATTLE/EVENT QUERIES
    // =========================================================================

    /// <summary>Get all events for the current campaign.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        var campaign = CampaignCatalog.GetCampaign(new CampaignId(_store.CurrentCampaignId));
        if (campaign == null) return result;

        foreach (var eventId in campaign.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null) continue;

            var dict = EventCatalog.ToDictionary(evt);
            dict["event_type"] = GetEventTypeForUI(evt.Type);
            result.Add(dict);
        }

        return result;
    }

    /// <summary>Get a specific event by ID.</summary>
    public Godot.Collections.Dictionary GetBattle(string battleId)
    {
        var evt = EventCatalog.GetEvent(new EventId(battleId));
        if (evt == null) return new Godot.Collections.Dictionary();

        var dict = EventCatalog.ToDictionary(evt);
        dict["event_type"] = GetEventTypeForUI(evt.Type);
        return dict;
    }

    /// <summary>Get all completed events.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCompletedBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var battleId in _store.CompletedBattles)
        {
            var evt = EventCatalog.GetEvent(new EventId(battleId));
            if (evt == null) continue;

            var dict = EventCatalog.ToDictionary(evt);
            dict["event_type"] = GetEventTypeForUI(evt.Type);
            result.Add(dict);
        }

        return result;
    }

    // =========================================================================
    // NEW TYPED ACCESS (for C# consumers)
    // =========================================================================

    /// <summary>Get typed event definition by ID.</summary>
    public EventDefinition? GetEventDefinition(string eventId)
    {
        return EventCatalog.GetEvent(new EventId(eventId));
    }

    /// <summary>Get typed event definition by ID with specific type.</summary>
    public T? GetEventDefinition<T>(string eventId) where T : EventDefinition
    {
        return EventCatalog.GetEvent<T>(new EventId(eventId));
    }

    /// <summary>Get typed campaign definition by ID.</summary>
    public CampaignDefinition? GetCampaignDefinition(string campaignId)
    {
        return CampaignCatalog.GetCampaign(new CampaignId(campaignId));
    }

    /// <summary>Get all battle events for current campaign.</summary>
    public BattleEventDefinition[] GetCurrentCampaignBattles()
    {
        var campaign = CampaignCatalog.GetCampaign(new CampaignId(_store.CurrentCampaignId));
        if (campaign == null) return System.Array.Empty<BattleEventDefinition>();

        return campaign.EventIds
            .Select(id => EventCatalog.GetEvent<BattleEventDefinition>(id))
            .Where(e => e != null)
            .Cast<BattleEventDefinition>()
            .ToArray();
    }
}
