using System.Collections.Generic;
using Godot;

namespace ProjectSummoner.Data.Events;

/// <summary>
/// Defines a campaign's structure and metadata.
/// </summary>
public class CampaignDefinition
{
    /// <summary>Unique campaign identifier</summary>
    public string Id { get; set; } = "";

    /// <summary>Localization key for campaign name</summary>
    public string NameKey { get; set; } = "";

    /// <summary>Localization key for campaign description</summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>Sort order in campaign selection UI</summary>
    public int SortOrder { get; set; }

    /// <summary>Starting event ID</summary>
    public string StartEventId { get; set; } = "";

    /// <summary>Campaign IDs that must be completed to unlock this campaign</summary>
    public List<string> UnlockRequirements { get; set; } = new();

    /// <summary>Event IDs in this campaign (references EventCatalog)</summary>
    public List<string> EventIds { get; set; } = new();

    /// <summary>Edges connecting events in the campaign graph</summary>
    public List<CampaignEdge> Edges { get; set; } = new();
}

/// <summary>
/// Represents an edge connecting two events in the campaign graph.
/// </summary>
public class CampaignEdge
{
    /// <summary>Source event ID</summary>
    public string FromEventId { get; set; } = "";

    /// <summary>Target event ID</summary>
    public string ToEventId { get; set; } = "";

    /// <summary>Condition for this edge (optional)</summary>
    public EdgeCondition? Condition { get; set; }

    public CampaignEdge() { }

    public CampaignEdge(string from, string to, EdgeCondition? condition = null)
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
    public string? ChoiceId { get; set; }

    public EdgeCondition() { }

    public EdgeCondition(string choiceId)
    {
        ChoiceId = choiceId;
    }
}
