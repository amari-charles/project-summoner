using System.Collections.Generic;
using Godot;
using Fateforged.Meta.Campaign;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Tracks player choices made at choice nodes in the campaign graph.
/// Choices affect which edges are traversable and which paths unlock.
/// </summary>
public class ChoiceTracker
{
    /// <summary>Choices made by node ID. Key = nodeId, Value = choiceId.</summary>
    private readonly Dictionary<NodeId, ChoiceId> _choices = [];

    // =========================================================================
    // CHOICE MANAGEMENT (Typed API)
    // =========================================================================

    /// <summary>
    /// Record a choice made at a specific node.
    /// </summary>
    public void RecordChoice(NodeId nodeId, ChoiceId choiceId)
    {
        if (!nodeId.HasValue)
        {
            GD.PushWarning("ChoiceTracker: Cannot record choice with empty node ID");
            return;
        }

        _choices[nodeId] = choiceId;
        GD.Print($"ChoiceTracker: Recorded choice '{choiceId}' at node '{nodeId}'");
    }

    /// <summary>
    /// Get the choice made at a specific node.
    /// Returns ChoiceId.None if no choice was made at that node.
    /// </summary>
    public ChoiceId GetChoice(NodeId nodeId)
    {
        return _choices.TryGetValue(nodeId, out var choice) ? choice : ChoiceId.None;
    }

    /// <summary>
    /// Check if a choice has been made at a specific node.
    /// </summary>
    public bool HasChoice(NodeId nodeId)
    {
        return _choices.ContainsKey(nodeId);
    }

    /// <summary>
    /// Clear a specific choice (for testing or undo functionality).
    /// </summary>
    public void ClearChoice(NodeId nodeId)
    {
        _choices.Remove(nodeId);
    }

    /// <summary>
    /// Clear all recorded choices.
    /// </summary>
    public void ClearAll()
    {
        _choices.Clear();
    }

    // =========================================================================
    // GDSCRIPT INTEROP (String-based API)
    // =========================================================================

    /// <summary>
    /// Record a choice from string IDs (for GDScript interop).
    /// </summary>
    public void RecordChoiceFromString(string nodeId, string choiceId)
    {
        RecordChoice(new NodeId(nodeId), new ChoiceId(choiceId));
    }

    /// <summary>
    /// Get the choice as a string (for GDScript interop).
    /// Returns empty string if no choice was made.
    /// </summary>
    public string GetChoiceAsString(string nodeId)
    {
        return (string)GetChoice(new NodeId(nodeId));
    }

    /// <summary>
    /// Check if a choice has been made (string version for GDScript interop).
    /// </summary>
    public bool HasChoiceFromString(string nodeId)
    {
        return HasChoice(new NodeId(nodeId));
    }

    // =========================================================================
    // SERIALIZATION (for save/load)
    // =========================================================================

    /// <summary>
    /// Get all choices as a typed dictionary for serialization.
    /// </summary>
    public Dictionary<NodeId, ChoiceId> GetAllChoices()
    {
        return new Dictionary<NodeId, ChoiceId>(_choices);
    }

    /// <summary>
    /// Load choices from a typed dictionary (for deserialization).
    /// </summary>
    public void LoadChoices(Dictionary<NodeId, ChoiceId> choices)
    {
        _choices.Clear();
        foreach (var kvp in choices)
        {
            _choices[kvp.Key] = kvp.Value;
        }
        GD.Print($"ChoiceTracker: Loaded {_choices.Count} choices");
    }

    /// <summary>
    /// Convert choices to Godot Dictionary for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary ToGodotDictionary()
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var kvp in _choices)
        {
            dict[(string)kvp.Key] = (string)kvp.Value;
        }
        return dict;
    }

    /// <summary>
    /// Load choices from Godot Dictionary (for GDScript interop).
    /// </summary>
    public void FromGodotDictionary(Godot.Collections.Dictionary dict)
    {
        _choices.Clear();
        foreach (var key in dict.Keys)
        {
            var nodeIdStr = key.AsString();
            var choiceIdStr = dict[key].AsString();
            if (!string.IsNullOrEmpty(nodeIdStr))
            {
                _choices[new NodeId(nodeIdStr)] = new ChoiceId(choiceIdStr);
            }
        }
    }
}
