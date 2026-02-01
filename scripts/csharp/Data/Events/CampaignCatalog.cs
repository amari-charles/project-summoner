using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ProjectSummoner.Data.Events;

/// <summary>
/// Central registry of all campaign definitions.
/// Provides type-safe campaign lookup and query methods.
/// </summary>
public static class CampaignCatalog
{
    // =========================================================================
    // CAMPAIGN DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, CampaignDefinition> _campaigns = new()
    {
        // =====================================================================
        // SUMMONER'S PATH - Main campaign for all summoners
        // =====================================================================

        [CampaignIds.SummonersPath] = new CampaignDefinition
        {
            Id = CampaignIds.SummonersPath,
            NameKey = "campaign.summoners_path.name",
            DescriptionKey = "campaign.summoners_path.description",
            SortOrder = 0,
            StartEventId = EventIds.FirstTrial,
            EventIds = new List<string>
            {
                EventIds.FirstTrial,
                EventIds.SecondChallenge,
                EventIds.Caravan01,
                EventIds.ThirdTrial,
                EventIds.PathFork,
                EventIds.EliteBattle01,
                EventIds.StandardBattle01,
                EventIds.Act1Boss
            },
            Edges = new List<CampaignEdge>
            {
                // Linear progression through early game
                new(EventIds.FirstTrial, EventIds.SecondChallenge),
                new(EventIds.SecondChallenge, EventIds.Caravan01),
                new(EventIds.Caravan01, EventIds.ThirdTrial),
                new(EventIds.ThirdTrial, EventIds.PathFork),

                // Branching paths based on player choice
                new(EventIds.PathFork, EventIds.EliteBattle01, new EdgeCondition("elite")),
                new(EventIds.PathFork, EventIds.StandardBattle01, new EdgeCondition("standard")),

                // Both paths lead to Act 1 Boss
                new(EventIds.EliteBattle01, EventIds.Act1Boss),
                new(EventIds.StandardBattle01, EventIds.Act1Boss)
            }
        },

        // =====================================================================
        // TEST ARENA - Debug campaign for testing
        // =====================================================================

        [CampaignIds.TestArena] = new CampaignDefinition
        {
            Id = CampaignIds.TestArena,
            NameKey = "campaign.test_arena.name",
            DescriptionKey = "campaign.test_arena.description",
            SortOrder = 99,
            StartEventId = EventIds.ArenaEarthSprite,
            EventIds = new List<string>
            {
                EventIds.ArenaEarthSprite,
                EventIds.ArenaPuff,
                EventIds.ArenaFireWisp,
                EventIds.ArenaCloudSwarm,
                EventIds.ArenaManaBolt,
                EventIds.DebugArena
            },
            Edges = new List<CampaignEdge>() // No edges - all nodes independently accessible
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a campaign by ID.</summary>
    public static CampaignDefinition? GetCampaign(string id)
    {
        return _campaigns.GetValueOrDefault(id);
    }

    /// <summary>Check if a campaign exists.</summary>
    public static bool HasCampaign(string id) => _campaigns.ContainsKey(id);

    /// <summary>Get all campaign IDs.</summary>
    public static string[] GetAllCampaignIds() => _campaigns.Keys.ToArray();

    /// <summary>Get all campaigns.</summary>
    public static CampaignDefinition[] GetAllCampaigns() => _campaigns.Values.ToArray();

    /// <summary>Get all campaigns sorted by sort order.</summary>
    public static CampaignDefinition[] GetAllCampaignsSorted()
    {
        return _campaigns.Values.OrderBy(c => c.SortOrder).ToArray();
    }

    /// <summary>Total campaign count.</summary>
    public static int Count => _campaigns.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get events for a campaign.</summary>
    public static EventDefinition[] GetCampaignEvents(string campaignId)
    {
        var campaign = GetCampaign(campaignId);
        if (campaign == null) return System.Array.Empty<EventDefinition>();

        return campaign.EventIds
            .Select(id => EventCatalog.GetEvent(id))
            .Where(e => e != null)
            .Cast<EventDefinition>()
            .ToArray();
    }

    /// <summary>Get edges for a campaign.</summary>
    public static CampaignEdge[] GetCampaignEdges(string campaignId)
    {
        var campaign = GetCampaign(campaignId);
        return campaign?.Edges.ToArray() ?? System.Array.Empty<CampaignEdge>();
    }

    /// <summary>Get the start event for a campaign.</summary>
    public static EventDefinition? GetStartEvent(string campaignId)
    {
        var campaign = GetCampaign(campaignId);
        if (campaign == null) return null;
        return EventCatalog.GetEvent(campaign.StartEventId);
    }

    // =========================================================================
    // GDSCRIPT BRIDGE
    // =========================================================================

    /// <summary>Get campaign as Godot Dictionary for GDScript interop.</summary>
    public static Godot.Collections.Dictionary GetCampaignAsDict(string id)
    {
        var campaign = GetCampaign(id);
        if (campaign == null) return new Godot.Collections.Dictionary();
        return ToDictionary(campaign);
    }

    /// <summary>Get all campaigns as Godot Array for GDScript interop.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCampaignsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var campaign in GetAllCampaignsSorted())
        {
            result.Add(ToDictionary(campaign));
        }
        return result;
    }

    /// <summary>Convert CampaignDefinition to Godot Dictionary.</summary>
    public static Godot.Collections.Dictionary ToDictionary(CampaignDefinition campaign)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["campaign_id"] = campaign.Id,
            ["name_key"] = campaign.NameKey,
            ["description_key"] = campaign.DescriptionKey,
            ["sort_order"] = campaign.SortOrder,
            ["start_node"] = campaign.StartEventId,
            ["icon"] = ""
        };

        // Unlock requirements
        var unlockReqs = new Godot.Collections.Array();
        foreach (var req in campaign.UnlockRequirements)
        {
            unlockReqs.Add(req);
        }
        dict["unlock_requirements"] = unlockReqs;

        // Nodes (events with position data)
        var nodes = new Godot.Collections.Array();
        foreach (var eventId in campaign.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null) continue;

            var nodeDict = new Godot.Collections.Dictionary
            {
                ["id"] = evt.Id,
                ["type"] = evt.Type.ToStringId(),
                ["position"] = evt.Position,
                ["data"] = EventCatalog.ToDictionary(evt)
            };
            nodes.Add(nodeDict);
        }
        dict["nodes"] = nodes;

        // Edges
        var edges = new Godot.Collections.Array();
        foreach (var edge in campaign.Edges)
        {
            var edgeDict = new Godot.Collections.Dictionary
            {
                ["from"] = edge.FromEventId,
                ["to"] = edge.ToEventId
            };
            if (edge.Condition?.ChoiceId != null)
            {
                edgeDict["condition"] = new Godot.Collections.Dictionary
                {
                    ["choice"] = edge.Condition.ChoiceId
                };
            }
            edges.Add(edgeDict);
        }
        dict["edges"] = edges;

        // Battles array (flattened for backwards compatibility)
        var battles = new Godot.Collections.Array();
        foreach (var eventId in campaign.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null) continue;

            var battleDict = EventCatalog.ToDictionary(evt);
            // Add event_type for UI type checking
            battleDict["event_type"] = GetEventTypeForUI(evt.Type);
            battles.Add(battleDict);
        }
        dict["battles"] = battles;

        return dict;
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
}
