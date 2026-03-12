using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Events;
using Godot;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Handles campaign and battle catalog queries.
/// Backed by EventCatalog and CampaignCatalog.
/// Store holds typed definitions; dict serialization happens only in GDScript-facing query methods.
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
    /// Stores typed definitions directly — no dict serialization at load time.
    /// </summary>
    public void Initialize()
    {
        _store.Campaigns.Clear();
        _store.Events.Clear();

        foreach (var campaign in CampaignCatalog.GetAllCampaigns())
        {
            _store.Campaigns[campaign.Id] = campaign;

            foreach (var eventId in campaign.EventIds)
            {
                var evt = EventCatalog.GetEvent(eventId);
                if (evt == null)
                    continue;

                _store.Events[eventId] = evt;
            }
        }

        GD.Print(
            $"CampaignCatalogHandler: Initialized {_store.Campaigns.Count} campaigns with {_store.Events.Count} total events from C# catalogs"
        );
    }

    private static string GetEventTypeForUI(EventType type) =>
        type switch
        {
            EventType.Battle or EventType.Elite or EventType.Boss => "battle",
            EventType.Caravan => "caravan",
            EventType.Choice => "choice",
            _ => "battle",
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
    public Godot.Collections.Dictionary GetCampaign(CampaignId campaignId)
    {
        var campaign = CampaignCatalog.GetCampaign(campaignId);
        if (campaign == null)
            return new Godot.Collections.Dictionary();
        return CampaignCatalog.ToDictionary(campaign);
    }

    /// <summary>Check if a campaign is unlocked.</summary>
    public bool IsCampaignUnlocked(CampaignId campaignId)
    {
        var campaign = CampaignCatalog.GetCampaign(campaignId);
        if (campaign == null)
            return false;

        if (campaign.UnlockRequirements.Count == 0)
            return true;

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

        var campaign = CampaignCatalog.GetCampaign(_store.CurrentCampaignId);
        if (campaign == null)
            return result;

        foreach (var eventId in campaign.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null)
                continue;

            var dict = EventCatalog.ToDictionary(evt);
            dict["event_type"] = GetEventTypeForUI(evt.Type);
            result.Add(dict);
        }

        return result;
    }

    /// <summary>Get a specific event by ID.</summary>
    public Godot.Collections.Dictionary GetBattle(EventId eventId)
    {
        var evt = EventCatalog.GetEvent(eventId);
        if (evt == null)
            return new Godot.Collections.Dictionary();

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
            var evt = EventCatalog.GetEvent(new EventId(battleId.Value));
            if (evt == null)
                continue;

            var dict = EventCatalog.ToDictionary(evt);
            dict["event_type"] = GetEventTypeForUI(evt.Type);
            result.Add(dict);
        }

        return result;
    }

    // =========================================================================
    // TYPED ACCESS (for C# consumers)
    // =========================================================================

    /// <summary>Get typed event definition by ID.</summary>
    public EventDefinition? GetEventDefinition(EventId eventId)
    {
        return EventCatalog.GetEvent(eventId);
    }

    /// <summary>Get typed event definition by ID with specific type.</summary>
    public T? GetEventDefinition<T>(EventId eventId)
        where T : EventDefinition
    {
        return EventCatalog.GetEvent<T>(eventId);
    }

    /// <summary>Get typed campaign definition by ID.</summary>
    public CampaignDefinition? GetCampaignDefinition(CampaignId campaignId)
    {
        return CampaignCatalog.GetCampaign(campaignId);
    }

    /// <summary>Get all battle events for current campaign.</summary>
    public BattleEventDefinition[] GetCurrentCampaignBattles()
    {
        var campaign = CampaignCatalog.GetCampaign(_store.CurrentCampaignId);
        if (campaign == null)
            return System.Array.Empty<BattleEventDefinition>();

        return campaign
            .EventIds.Select(id => EventCatalog.GetEvent<BattleEventDefinition>(id))
            .Where(e => e != null)
            .Cast<BattleEventDefinition>()
            .ToArray();
    }
}
