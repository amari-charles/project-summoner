using System.Collections.Generic;
using Fateforged.Meta.Campaign.Models;
using Godot;
using EventId = Fateforged.Data.Events.EventId;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Handles node unlock condition evaluation for the campaign graph.
/// Determines if a node is unlocked based on completed nodes and edge conditions.
/// </summary>
public class NodeUnlockHandler
{
    private readonly CampaignGraphStore _graphStore;
    private readonly ChoiceTracker _choices;

    public NodeUnlockHandler(CampaignGraphStore graphStore, ChoiceTracker choices)
    {
        _graphStore = graphStore;
        _choices = choices;
    }

    // =========================================================================
    // NODE UNLOCK EVALUATION
    // =========================================================================

    /// <summary>
    /// Check if a node is unlocked.
    /// A node is unlocked if ANY incoming edge is satisfied (OR logic).
    /// Start nodes (no incoming edges) are always unlocked.
    /// </summary>
    public bool IsNodeUnlocked(string nodeId)
    {
        var graph = _graphStore.CurrentGraph;
        if (graph == null)
        {
            GD.PushWarning($"NodeUnlockHandler: No current graph loaded");
            return false;
        }

        var campaign = Fateforged.Data.Events.CampaignCatalog.GetCampaign(
            new Fateforged.Data.Events.CampaignId(graph.CampaignId)
        );
        return campaign != null
            && CampaignUnlockPolicy.IsUnlocked(
                campaign,
                new EventId(nodeId),
                _graphStore.CompletedNodes,
                _choices.GetAllChoices()
            );
    }

    // =========================================================================
    // BATCH QUERIES
    // =========================================================================

    /// <summary>
    /// Get all unlocked nodes in the current graph.
    /// </summary>
    public List<CampaignNode> GetUnlockedNodes()
    {
        var result = new List<CampaignNode>();
        var graph = _graphStore.CurrentGraph;
        if (graph == null)
            return result;

        foreach (var node in graph.GetAllNodes())
        {
            if (IsNodeUnlocked((string)node.Id))
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all available nodes (unlocked but not completed).
    /// </summary>
    public List<CampaignNode> GetAvailableNodes()
    {
        var result = new List<CampaignNode>();
        var graph = _graphStore.CurrentGraph;
        if (graph == null)
            return result;

        foreach (var node in graph.GetAllNodes())
        {
            if (IsNodeUnlocked((string)node.Id) && !_graphStore.IsNodeCompleted(node.Id))
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// Get nodes that were unlocked by completing a specific node.
    /// Used to emit BattleUnlocked signals.
    /// </summary>
    public List<string> GetNewlyUnlockedNodes(string completedNodeId)
    {
        var result = new List<string>();
        var graph = _graphStore.CurrentGraph;
        if (graph == null)
            return result;

        var typedNodeId = new EventId(completedNodeId);

        // Get all nodes that have edges from the completed node
        var outgoingEdges = graph.GetOutgoingEdges(typedNodeId);
        foreach (var edge in outgoingEdges)
        {
            // Check if this node is now unlocked (wasn't before, is now)
            if (IsNodeUnlocked((string)edge.ToId) && !_graphStore.IsNodeCompleted(edge.ToId))
            {
                result.Add((string)edge.ToId);
            }
        }

        return result;
    }
}
