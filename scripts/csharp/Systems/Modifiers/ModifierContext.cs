using System.Collections.Generic;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Context for modifier queries.
/// Contains information needed to filter and apply modifiers.
/// </summary>
public class ModifierContext
{
    /// <summary>
    /// Type of target being modified (e.g., "unit", "card", "summoner").
    /// </summary>
    public string TargetType { get; set; } = "unit";

    /// <summary>
    /// Card instance ID for instance-scoped modifiers.
    /// </summary>
    public string? CardInstanceId { get; set; }

    /// <summary>
    /// Card name for logging/debugging.
    /// </summary>
    public string? CardName { get; set; }

    /// <summary>
    /// Team of the unit being spawned.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Categories for condition matching.
    /// Key: category name (e.g., "elemental_affinity", "tags", "unit_type")
    /// Value: category value(s)
    /// </summary>
    public Dictionary<string, object> Categories { get; set; } = new();

    /// <summary>
    /// Create a ModifierContext from Godot Dictionaries (for interop with GDScript).
    /// </summary>
    public static ModifierContext FromDictionaries(
        Godot.Collections.Dictionary categories,
        Godot.Collections.Dictionary context)
    {
        var result = new ModifierContext();

        // Parse categories
        foreach (var key in categories.Keys)
        {
            result.Categories[key.AsString()] = categories[key].Obj!;
        }

        // Parse context
        if (context.TryGetValue("card_instance_id", out var instanceId) &&
            instanceId.VariantType != Godot.Variant.Type.Nil)
        {
            result.CardInstanceId = instanceId.AsString();
        }

        if (context.TryGetValue("card_name", out var cardName) &&
            cardName.VariantType != Godot.Variant.Type.Nil)
        {
            result.CardName = cardName.AsString();
        }

        if (context.TryGetValue("team", out var team))
        {
            result.Team = team.AsInt32();
        }

        return result;
    }

    /// <summary>
    /// Convert to Godot Dictionaries (for interop with GDScript).
    /// </summary>
    public (Godot.Collections.Dictionary categories, Godot.Collections.Dictionary context) ToDictionaries()
    {
        var categories = new Godot.Collections.Dictionary();
        foreach (var kvp in Categories)
        {
            categories[kvp.Key] = ObjectToVariant(kvp.Value);
        }

        var context = new Godot.Collections.Dictionary
        {
            ["team"] = Team
        };

        if (!string.IsNullOrEmpty(CardInstanceId))
        {
            context["card_instance_id"] = CardInstanceId;
        }

        if (!string.IsNullOrEmpty(CardName))
        {
            context["card_name"] = CardName;
        }

        return (categories, context);
    }

    /// <summary>
    /// Convert a C# object to a Godot Variant safely.
    /// Handles common types that Godot supports.
    /// </summary>
    private static Godot.Variant ObjectToVariant(object? value)
    {
        return value switch
        {
            null => default,
            string s => s,
            int i => i,
            float f => f,
            double d => (float)d,
            bool b => b,
            long l => l,
            Godot.GodotObject obj => obj,
            Godot.Variant v => v,
            Godot.Collections.Array arr => arr,
            Godot.Collections.Dictionary dict => dict,
            _ => value.ToString() ?? ""
        };
    }
}
