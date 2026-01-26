using System.Collections.Generic;
using Godot;
using ProjectSummoner.Services.Campaign.Models;

namespace ProjectSummoner.Services.Campaign.Handlers;

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

        // Check if node exists
        var node = graph.GetNode(nodeId);
        if (node == null)
        {
            return false;
        }

        // Start node is always unlocked
        if (graph.IsStartNode(nodeId))
        {
            return true;
        }

        // Get incoming edges
        var incomingEdges = graph.GetIncomingEdges(nodeId);
        if (incomingEdges.Count == 0)
        {
            // No incoming edges = start node, always unlocked
            return true;
        }

        // Node is unlocked if ANY incoming edge is satisfied (OR logic)
        foreach (var edge in incomingEdges)
        {
            if (IsEdgeSatisfied(edge))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if an edge's conditions are satisfied.
    /// Source node must be completed AND any edge conditions must be met.
    /// </summary>
    public bool IsEdgeSatisfied(CampaignEdge edge)
    {
        // Source node must be completed
        if (!_graphStore.IsNodeCompleted(edge.FromId))
        {
            return false;
        }

        // Check edge condition (if any)
        if (edge.Condition != null)
        {
            return EvaluateCondition(edge.Condition, edge.FromId);
        }

        // No condition, just requires source completion
        return true;
    }

    /// <summary>
    /// Evaluate an edge condition.
    /// Supports: choice, completed, item (future)
    /// </summary>
    public bool EvaluateCondition(EdgeCondition condition, string sourceNodeId)
    {
        return condition.Type switch
        {
            "choice" => EvaluateChoiceCondition(condition, sourceNodeId),
            "completed" => EvaluateCompletedCondition(condition),
            "item" => EvaluateItemCondition(condition),
            _ => true  // Unknown condition type = pass
        };
    }

    private bool EvaluateChoiceCondition(EdgeCondition condition, string sourceNodeId)
    {
        // Get the node ID where the choice was made
        // If not specified, use the source node of this edge
        var choiceNodeId = string.IsNullOrEmpty(condition.NodeId)
            ? sourceNodeId
            : condition.NodeId;

        // Check if the choice made at that node matches the required value
        var choiceMade = _choices.GetChoice(choiceNodeId);
        return choiceMade == condition.Value;
    }

    private bool EvaluateCompletedCondition(EdgeCondition condition)
    {
        // Check if a specific node is completed
        return _graphStore.IsNodeCompleted(condition.NodeId);
    }

    private bool EvaluateItemCondition(EdgeCondition condition)
    {
        // Item conditions (for future inventory integration)
        // For now, always pass
        GD.PrintRich($"[color=yellow]NodeUnlockHandler: Item conditions not yet implemented (item: {condition.Value})[/color]");
        return true;
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
        if (graph == null) return result;

        foreach (var node in graph.GetAllNodes())
        {
            if (IsNodeUnlocked(node.Id))
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
        if (graph == null) return result;

        foreach (var node in graph.GetAllNodes())
        {
            if (IsNodeUnlocked(node.Id) && !_graphStore.IsNodeCompleted(node.Id))
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
        if (graph == null) return result;

        // Get all nodes that have edges from the completed node
        var outgoingEdges = graph.GetOutgoingEdges(completedNodeId);
        foreach (var edge in outgoingEdges)
        {
            // Check if this node is now unlocked (wasn't before, is now)
            if (IsNodeUnlocked(edge.ToId) && !_graphStore.IsNodeCompleted(edge.ToId))
            {
                result.Add(edge.ToId);
            }
        }

        return result;
    }
}
