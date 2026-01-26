using System.Collections.Generic;
using Godot;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Tracks player choices made at choice nodes in the campaign graph.
/// Choices affect which edges are traversable and which paths unlock.
/// </summary>
public class ChoiceTracker
{
    /// <summary>Choices made by node ID. Key = nodeId, Value = choiceId.</summary>
    private readonly Dictionary<string, string> _choices = [];

    // =========================================================================
    // CHOICE MANAGEMENT
    // =========================================================================

    /// <summary>
    /// Record a choice made at a specific node.
    /// </summary>
    public void RecordChoice(string nodeId, string choiceId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            GD.PushWarning("ChoiceTracker: Cannot record choice with empty node ID");
            return;
        }

        _choices[nodeId] = choiceId;
        GD.Print($"ChoiceTracker: Recorded choice '{choiceId}' at node '{nodeId}'");
    }

    /// <summary>
    /// Get the choice made at a specific node.
    /// Returns empty string if no choice was made at that node.
    /// </summary>
    public string GetChoice(string nodeId)
    {
        return _choices.TryGetValue(nodeId, out var choice) ? choice : "";
    }

    /// <summary>
    /// Check if a choice has been made at a specific node.
    /// </summary>
    public bool HasChoice(string nodeId)
    {
        return _choices.ContainsKey(nodeId);
    }

    /// <summary>
    /// Clear a specific choice (for testing or undo functionality).
    /// </summary>
    public void ClearChoice(string nodeId)
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
    // SERIALIZATION (for save/load)
    // =========================================================================

    /// <summary>
    /// Get all choices as a dictionary for serialization.
    /// </summary>
    public Dictionary<string, string> GetAllChoices()
    {
        return new Dictionary<string, string>(_choices);
    }

    /// <summary>
    /// Load choices from a dictionary (for deserialization).
    /// </summary>
    public void LoadChoices(Dictionary<string, string> choices)
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
            dict[kvp.Key] = kvp.Value;
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
            var nodeId = key.AsString();
            var choiceId = dict[key].AsString();
            if (!string.IsNullOrEmpty(nodeId))
            {
                _choices[nodeId] = choiceId;
            }
        }
    }
}
