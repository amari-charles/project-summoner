using System;
using Fateforged.Data.Events;
using Godot;

namespace Fateforged.Meta.Campaign.Models;

/// <summary>
/// Represents a node in the campaign graph.
/// Nodes can be battles, events, choices, etc.
/// </summary>
public class CampaignNode
{
    /// <summary>Unique identifier for this node (references an event).</summary>
    public EventId Id { get; set; } = EventId.None;

    /// <summary>Node type (battle, elite, boss, choice, caravan).</summary>
    public string Type { get; set; } = "";

    /// <summary>Position on the campaign map for rendering.</summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>Type-specific data (enemy deck, reward options, etc.).</summary>
    public Godot.Collections.Dictionary Data { get; set; } = [];

    /// <summary>The campaign this node belongs to.</summary>
    public CampaignId CampaignId { get; set; } = CampaignId.None;

    /// <summary>
    /// Create a CampaignNode from GDScript Dictionary.
    /// </summary>
    public static CampaignNode FromDictionary(
        Godot.Collections.Dictionary dict,
        CampaignId campaignId
    )
    {
        var node = new CampaignNode
        {
            Id = new EventId(dict.GetValueOrDefault("id", "").AsString()),
            Type = dict.GetValueOrDefault("type", "battle").AsString(),
            CampaignId = campaignId,
        };

        // Parse position
        var posVariant = dict.GetValueOrDefault("position", Vector2.Zero);
        if (posVariant.Obj is Vector2 pos)
        {
            node.Position = pos;
        }

        // Store the full data dictionary for type-specific properties
        var dataVariant = dict.GetValueOrDefault("data", new Godot.Collections.Dictionary());
        if (dataVariant.Obj is Godot.Collections.Dictionary data)
        {
            node.Data = data;
        }

        return node;
    }

    /// <summary>
    /// Create a CampaignNode from a typed EventDefinition.
    /// This is the new preferred construction method.
    /// </summary>
    public static CampaignNode FromEventDefinition(EventDefinition evt, CampaignId campaignId)
    {
        var node = new CampaignNode
        {
            Id = evt.Id,
            Type = evt.Type.ToStringId(),
            Position = evt.Position,
            CampaignId = campaignId,
            Data = EventCatalog.ToDictionary(evt),
        };

        return node;
    }

    /// <summary>
    /// Convert back to GDScript Dictionary for interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)Id,
            ["type"] = Type,
            ["position"] = Position,
            ["data"] = Data,
            ["campaign_id"] = (string)CampaignId,
        };
    }

    // =========================================================================
    // CONVENIENCE ACCESSORS FOR COMMON DATA FIELDS
    // =========================================================================

    /// <summary>Check if this is a tutorial battle.</summary>
    public bool IsTutorial => Data.GetValueOrDefault("is_tutorial", false).AsBool();

    /// <summary>Get level cap from data (for battle nodes).</summary>
    public int GetLevelCap() => Data.GetValueOrDefault("level_cap", 0).AsInt32();

    /// <summary>Get choice options (for choice nodes).</summary>
    public Godot.Collections.Array GetChoiceOptions()
    {
        var variant = Data.GetValueOrDefault("options", new Godot.Collections.Array());
        return variant.Obj as Godot.Collections.Array ?? [];
    }
}
