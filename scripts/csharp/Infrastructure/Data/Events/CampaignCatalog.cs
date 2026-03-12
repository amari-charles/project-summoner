using System.Collections.Generic;
using System.Linq;
using Fateforged.Meta.Campaign;
using Godot;

namespace Fateforged.Data.Events;

/// <summary>
/// Central registry of all campaign definitions.
/// Provides type-safe campaign lookup and query methods.
/// </summary>
public static class CampaignCatalog
{
    // =========================================================================
    // CAMPAIGN DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<CampaignId, CampaignDefinition> _campaigns = new()
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
            EventIds = new List<EventId>
            {
                EventIds.FirstTrial,
                EventIds.SecondChallenge,
                EventIds.OpeningDoctrine,
                EventIds.AggressivePush,
                EventIds.ScoutSkirmish,
                EventIds.Caravan01,
                EventIds.StabilityLine,
                EventIds.ThirdTrial,
                EventIds.MidlineTrial,
                EventIds.RouteChoice,
                EventIds.RidgeAssault,
                EventIds.RiverHold,
                EventIds.GrovePatrol,
                EventIds.Caravan02,
                EventIds.Chokepoint,
                EventIds.Gatekeeper,
                EventIds.PathFork,
                EventIds.EliteBattle01,
                EventIds.EliteBattle02,
                EventIds.EliteBattle03,
                EventIds.EliteBattle04,
                EventIds.StandardBattle01,
                EventIds.Caravan03,
                EventIds.StandardBattle02,
                EventIds.StandardBattle03,
                EventIds.StandardBattle04,
                EventIds.GambitBattle01,
                EventIds.GambitBattle02,
                EventIds.GambitBattle03,
                EventIds.GambitBattle04,
                EventIds.RejoinTrial,
                EventIds.FinalAnte,
                EventIds.StormBreaker,
                EventIds.Act1Boss,
            },
            Edges = new List<CampaignEdge>
            {
                new(EventIds.FirstTrial, EventIds.SecondChallenge),
                new(EventIds.SecondChallenge, EventIds.OpeningDoctrine),
                new(
                    EventIds.OpeningDoctrine,
                    EventIds.AggressivePush,
                    new EdgeCondition(ChoiceIds.Aggressive)
                ),
                new(
                    EventIds.OpeningDoctrine,
                    EventIds.Caravan01,
                    new EdgeCondition(ChoiceIds.Prepared)
                ),
                new(
                    EventIds.OpeningDoctrine,
                    EventIds.ScoutSkirmish,
                    new EdgeCondition(ChoiceIds.Insight)
                ),
                new(EventIds.AggressivePush, EventIds.Caravan01),
                new(EventIds.ScoutSkirmish, EventIds.Caravan01),
                new(EventIds.Caravan01, EventIds.StabilityLine),
                new(EventIds.StabilityLine, EventIds.ThirdTrial),
                new(EventIds.ThirdTrial, EventIds.MidlineTrial),
                new(EventIds.MidlineTrial, EventIds.RouteChoice),
                new(
                    EventIds.RouteChoice,
                    EventIds.RidgeAssault,
                    new EdgeCondition(ChoiceIds.Ridge)
                ),
                new(EventIds.RouteChoice, EventIds.RiverHold, new EdgeCondition(ChoiceIds.River)),
                new(EventIds.RouteChoice, EventIds.GrovePatrol, new EdgeCondition(ChoiceIds.Grove)),
                new(EventIds.RidgeAssault, EventIds.Caravan02),
                new(EventIds.RiverHold, EventIds.Caravan02),
                new(EventIds.GrovePatrol, EventIds.Caravan02),
                new(EventIds.Caravan02, EventIds.Chokepoint),
                new(EventIds.Chokepoint, EventIds.Gatekeeper),
                new(EventIds.Gatekeeper, EventIds.PathFork),
                new(EventIds.PathFork, EventIds.EliteBattle01, new EdgeCondition(ChoiceIds.Elite)),
                new(
                    EventIds.PathFork,
                    EventIds.StandardBattle01,
                    new EdgeCondition(ChoiceIds.Standard)
                ),
                new(
                    EventIds.PathFork,
                    EventIds.GambitBattle01,
                    new EdgeCondition(ChoiceIds.Gambit)
                ),
                new(EventIds.EliteBattle01, EventIds.EliteBattle02),
                new(EventIds.EliteBattle02, EventIds.EliteBattle03),
                new(EventIds.EliteBattle03, EventIds.EliteBattle04),
                new(EventIds.EliteBattle04, EventIds.RejoinTrial),
                new(EventIds.StandardBattle01, EventIds.Caravan03),
                new(EventIds.Caravan03, EventIds.StandardBattle02),
                new(EventIds.StandardBattle02, EventIds.StandardBattle03),
                new(EventIds.StandardBattle03, EventIds.StandardBattle04),
                new(EventIds.StandardBattle04, EventIds.RejoinTrial),
                new(EventIds.GambitBattle01, EventIds.GambitBattle02),
                new(EventIds.GambitBattle02, EventIds.GambitBattle03),
                new(EventIds.GambitBattle03, EventIds.GambitBattle04),
                new(EventIds.GambitBattle04, EventIds.RejoinTrial),
                new(EventIds.RejoinTrial, EventIds.FinalAnte),
                new(EventIds.FinalAnte, EventIds.StormBreaker),
                new(EventIds.StormBreaker, EventIds.Act1Boss),
            },
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
            EventIds = new List<EventId>
            {
                EventIds.ArenaEarthSprite,
                EventIds.ArenaPuff,
                EventIds.ArenaFireWisp,
                EventIds.ArenaCloudSwarm,
                EventIds.ArenaManaBolt,
                EventIds.DebugArena,
            },
            Edges = new List<CampaignEdge>(), // No edges - all nodes independently accessible
        },
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a campaign by ID.</summary>
    public static CampaignDefinition? GetCampaign(CampaignId id)
    {
        return _campaigns.GetValueOrDefault(id);
    }

    /// <summary>Check if a campaign exists.</summary>
    public static bool HasCampaign(CampaignId id) => _campaigns.ContainsKey(id);

    /// <summary>Get all campaign IDs.</summary>
    public static CampaignId[] GetAllCampaignIds() => _campaigns.Keys.ToArray();

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
    public static EventDefinition[] GetCampaignEvents(CampaignId campaignId)
    {
        var campaign = GetCampaign(campaignId);
        if (campaign == null)
            return System.Array.Empty<EventDefinition>();

        return campaign
            .EventIds.Select(id => EventCatalog.GetEvent(id))
            .Where(e => e != null)
            .Cast<EventDefinition>()
            .ToArray();
    }

    /// <summary>Get edges for a campaign.</summary>
    public static CampaignEdge[] GetCampaignEdges(CampaignId campaignId)
    {
        var campaign = GetCampaign(campaignId);
        return campaign?.Edges.ToArray() ?? System.Array.Empty<CampaignEdge>();
    }

    /// <summary>Get the start event for a campaign.</summary>
    public static EventDefinition? GetStartEvent(CampaignId campaignId)
    {
        var campaign = GetCampaign(campaignId);
        if (campaign == null)
            return null;
        return EventCatalog.GetEvent(campaign.StartEventId);
    }

    // =========================================================================
    // GDSCRIPT BRIDGE
    // =========================================================================

    /// <summary>Get campaign as Godot Dictionary for GDScript interop.</summary>
    public static Godot.Collections.Dictionary GetCampaignAsDict(CampaignId id)
    {
        var campaign = GetCampaign(id);
        if (campaign == null)
            return new Godot.Collections.Dictionary();
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
            ["campaign_id"] = (string)campaign.Id,
            ["name_key"] = campaign.NameKey,
            ["description_key"] = campaign.DescriptionKey,
            ["sort_order"] = campaign.SortOrder,
            ["start_node"] = (string)campaign.StartEventId,
            ["icon"] = "",
        };

        // Unlock requirements
        var unlockReqs = new Godot.Collections.Array();
        foreach (var req in campaign.UnlockRequirements)
        {
            unlockReqs.Add((string)req);
        }
        dict["unlock_requirements"] = unlockReqs;

        // Nodes (events with position data)
        var nodes = new Godot.Collections.Array();
        foreach (var eventId in campaign.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null)
                continue;

            var nodeDict = new Godot.Collections.Dictionary
            {
                ["id"] = (string)evt.Id,
                ["type"] = evt.Type.ToStringId(),
                ["position"] = evt.Position,
                ["data"] = EventCatalog.ToDictionary(evt),
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
                ["from"] = (string)edge.FromEventId,
                ["to"] = (string)edge.ToEventId,
            };
            if (edge.Condition?.ChoiceId != null)
            {
                edgeDict["condition"] = new Godot.Collections.Dictionary
                {
                    ["choice"] = (string)edge.Condition.ChoiceId.Value,
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
            if (evt == null)
                continue;

            var battleDict = EventCatalog.ToDictionary(evt);
            // Add event_type for UI type checking
            battleDict["event_type"] = GetEventTypeForUI(evt.Type);
            battles.Add(battleDict);
        }
        dict["battles"] = battles;

        return dict;
    }

    private static string GetEventTypeForUI(EventType type) =>
        type switch
        {
            EventType.Battle or EventType.Elite or EventType.Boss => "battle",
            EventType.Caravan => "caravan",
            EventType.Choice => "choice",
            _ => "battle",
        };
}
