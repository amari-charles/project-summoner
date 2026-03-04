using System.Collections.Generic;
using Godot;
using Fateforged.Meta.Campaign;

namespace Fateforged.Data.Events;

/// <summary>
/// Defines a campaign's structure and metadata.
/// </summary>
public class CampaignDefinition
{
    /// <summary>Unique campaign identifier</summary>
    public CampaignId Id { get; set; } = CampaignId.None;

    /// <summary>Localization key for campaign name</summary>
    public string NameKey { get; set; } = "";

    /// <summary>Localization key for campaign description</summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>Sort order in campaign selection UI</summary>
    public int SortOrder { get; set; }

    /// <summary>Starting event ID</summary>
    public EventId StartEventId { get; set; } = EventId.None;

    /// <summary>Campaign IDs that must be completed to unlock this campaign</summary>
    public List<CampaignId> UnlockRequirements { get; set; } = new();

    /// <summary>Event IDs in this campaign (references EventCatalog)</summary>
    public List<EventId> EventIds { get; set; } = new();

    /// <summary>Edges connecting events in the campaign graph</summary>
    public List<CampaignEdge> Edges { get; set; } = new();
}

/// <summary>
/// Represents an edge connecting two events in the campaign graph.
/// </summary>
public class CampaignEdge
{
    /// <summary>Source event ID</summary>
    public EventId FromEventId { get; set; } = EventId.None;

    /// <summary>Target event ID</summary>
    public EventId ToEventId { get; set; } = EventId.None;

    /// <summary>Condition for this edge (optional)</summary>
    public EdgeCondition? Condition { get; set; }

    public CampaignEdge() { }

    public CampaignEdge(EventId from, EventId to, EdgeCondition? condition = null)
    {
        FromEventId = from;
        ToEventId = to;
        Condition = condition;
    }
}

/// <summary>
/// Condition for a campaign edge (e.g., requires specific choice).
/// </summary>
public class EdgeCondition
{
    /// <summary>Required choice ID at the source node</summary>
    public ChoiceId? ChoiceId { get; set; }

    public EdgeCondition() { }

    public EdgeCondition(ChoiceId choiceId)
    {
        ChoiceId = choiceId;
    }
}
