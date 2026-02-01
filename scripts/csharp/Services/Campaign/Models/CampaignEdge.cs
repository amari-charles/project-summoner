using System;
using Godot;
using ProjectSummoner.Data.Events;

namespace ProjectSummoner.Services.Campaign.Models;

/// <summary>
/// Represents an edge (connection) between two nodes in the campaign graph.
/// Edges define progression paths and can have unlock conditions.
/// </summary>
public class CampaignEdge
{
    /// <summary>ID of the source node (event).</summary>
    public EventId FromId { get; set; } = EventId.None;

    /// <summary>ID of the target node (event).</summary>
    public EventId ToId { get; set; } = EventId.None;

    /// <summary>Optional condition that must be met for this edge to be traversable.</summary>
    public EdgeCondition? Condition { get; set; }

    /// <summary>
    /// Create a CampaignEdge from GDScript Dictionary.
    /// </summary>
    public static CampaignEdge FromDictionary(Godot.Collections.Dictionary dict)
    {
        var edge = new CampaignEdge
        {
            FromId = new EventId(dict.GetValueOrDefault("from", "").AsString()),
            ToId = new EventId(dict.GetValueOrDefault("to", "").AsString())
        };

        // Parse condition if present
        var conditionVariant = dict.GetValueOrDefault("condition", new Godot.Collections.Dictionary());
        if (conditionVariant.Obj is Godot.Collections.Dictionary condDict && condDict.Count > 0)
        {
            edge.Condition = EdgeCondition.FromDictionary(condDict);
        }

        return edge;
    }

    /// <summary>
    /// Convert back to GDScript Dictionary for interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["from"] = (string)FromId,
            ["to"] = (string)ToId
        };

        if (Condition != null)
        {
            dict["condition"] = Condition.ToDictionary();
        }

        return dict;
    }

    /// <summary>Check if this edge has a condition.</summary>
    public bool HasCondition => Condition != null;
}

/// <summary>
/// Represents a condition that must be met for an edge to be traversable.
/// Supports various condition types: choice outcomes, node completion, items, etc.
/// </summary>
public class EdgeCondition
{
    /// <summary>Condition type: "choice", "completed", "item", etc.</summary>
    public string Type { get; set; } = "";

    /// <summary>The node ID this condition references (for choice/completed conditions).</summary>
    public EventId NodeId { get; set; } = EventId.None;

    /// <summary>The required value (e.g., choice option ID, item ID).</summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Create an EdgeCondition from GDScript Dictionary.
    /// Supports shorthand format: {"choice": "elite"} means type=choice, value=elite
    /// Full format: {"type": "choice", "node_id": "fork_01", "value": "elite"}
    /// </summary>
    public static EdgeCondition FromDictionary(Godot.Collections.Dictionary dict)
    {
        var condition = new EdgeCondition();

        // Check for shorthand format first
        if (dict.ContainsKey("choice"))
        {
            condition.Type = "choice";
            condition.Value = dict.GetValueOrDefault("choice", "").AsString();
            // NodeId will be inferred from the "from" node in the edge
            return condition;
        }

        if (dict.ContainsKey("completed"))
        {
            condition.Type = "completed";
            condition.NodeId = new EventId(dict.GetValueOrDefault("completed", "").AsString());
            return condition;
        }

        if (dict.ContainsKey("item"))
        {
            condition.Type = "item";
            condition.Value = dict.GetValueOrDefault("item", "").AsString();
            return condition;
        }

        // Full format
        condition.Type = dict.GetValueOrDefault("type", "").AsString();
        condition.NodeId = new EventId(dict.GetValueOrDefault("node_id", "").AsString());
        condition.Value = dict.GetValueOrDefault("value", "").AsString();

        return condition;
    }

    /// <summary>
    /// Convert back to GDScript Dictionary for interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            ["type"] = Type,
            ["node_id"] = (string)NodeId,
            ["value"] = Value
        };
    }
}
