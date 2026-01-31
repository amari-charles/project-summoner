using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Events;
using ProjectSummoner.Services.Campaign.Models;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Shared data store for campaign graph data.
/// Manages campaign graphs and tracks node completion state.
/// Now loads from C# CampaignCatalog instead of GDScript.
/// </summary>
public class CampaignGraphStore
{
    /// <summary>Campaign graphs indexed by campaign_id.</summary>
    public Dictionary<string, CampaignGraph> Graphs { get; } = [];

    /// <summary>Current campaign ID.</summary>
    public string CurrentCampaignId { get; set; } = "";

    /// <summary>Current campaign graph (convenience accessor).</summary>
    public CampaignGraph? CurrentGraph =>
        !string.IsNullOrEmpty(CurrentCampaignId) && Graphs.TryGetValue(CurrentCampaignId, out var graph)
            ? graph
            : null;

    /// <summary>List of completed node IDs for the current campaign context.</summary>
    public List<string> CompletedNodes { get; } = [];

    // =========================================================================
    // DATA LOADING
    // =========================================================================

    /// <summary>
    /// Initialize campaign graphs from C# CampaignCatalog.
    /// This is the new preferred initialization method.
    /// </summary>
    public void InitializeFromCatalog()
    {
        Graphs.Clear();

        foreach (var campaign in CampaignCatalog.GetAllCampaigns())
        {
            var graph = CampaignGraph.FromCampaignDefinition(campaign);
            if (!string.IsNullOrEmpty(graph.CampaignId))
            {
                Graphs[graph.CampaignId] = graph;
                GD.Print($"CampaignGraphStore: Loaded graph '{graph.CampaignId}' with {graph.Nodes.Count} nodes and {graph.Edges.Count} edges");
            }
        }

        GD.Print($"CampaignGraphStore: Initialized {Graphs.Count} campaign graphs from C# catalogs");
    }

    /// <summary>
    /// Load campaign graphs from GDScript data (legacy, now uses C# catalogs).
    /// </summary>
    [System.Obsolete("Use InitializeFromCatalog() instead. GDScript campaign data is no longer used.")]
    public void LoadGraphsFromGDScript(Godot.Collections.Array<Godot.Collections.Dictionary> campaigns)
    {
        // Ignore GDScript data - use C# catalogs
        InitializeFromCatalog();
    }

    /// <summary>
    /// Set the current campaign and load its progress.
    /// </summary>
    public void SetCurrentCampaign(string campaignId)
    {
        if (!Graphs.ContainsKey(campaignId))
        {
            GD.PushWarning($"CampaignGraphStore: Campaign '{campaignId}' not found");
            return;
        }

        CurrentCampaignId = campaignId;
        GD.Print($"CampaignGraphStore: Set current campaign to '{campaignId}'");
    }

    // =========================================================================
    // NODE COMPLETION
    // =========================================================================

    /// <summary>Check if a node is completed.</summary>
    public bool IsNodeCompleted(string nodeId)
    {
        return CompletedNodes.Contains(nodeId);
    }

    /// <summary>Mark a node as completed.</summary>
    public void CompleteNode(string nodeId)
    {
        if (!CompletedNodes.Contains(nodeId))
        {
            CompletedNodes.Add(nodeId);
        }
    }

    /// <summary>
    /// Load completed nodes from a list (for save/load).
    /// </summary>
    public void LoadCompletedNodes(List<string> nodeIds)
    {
        CompletedNodes.Clear();
        CompletedNodes.AddRange(nodeIds);
    }

    /// <summary>
    /// Load completed nodes from Godot Array (for GDScript interop).
    /// </summary>
    public void LoadCompletedNodesFromGodot(Godot.Collections.Array nodeIds)
    {
        CompletedNodes.Clear();
        foreach (var nodeId in nodeIds)
        {
            var id = nodeId.AsString();
            if (!string.IsNullOrEmpty(id))
            {
                CompletedNodes.Add(id);
            }
        }
    }

    // =========================================================================
    // NODE QUERIES
    // =========================================================================

    /// <summary>Get a node by ID from the current campaign.</summary>
    public CampaignNode? GetNode(string nodeId)
    {
        return CurrentGraph?.GetNode(nodeId);
    }

    /// <summary>Get all nodes from the current campaign.</summary>
    public List<CampaignNode> GetAllNodes()
    {
        return CurrentGraph?.GetAllNodes() ?? [];
    }

    /// <summary>Get all nodes of a specific type from the current campaign.</summary>
    public List<CampaignNode> GetNodesByType(string type)
    {
        var result = new List<CampaignNode>();
        if (CurrentGraph != null)
        {
            result.AddRange(CurrentGraph.GetNodesByType(type));
        }
        return result;
    }

    /// <summary>Get completed nodes as a list.</summary>
    public List<CampaignNode> GetCompletedNodes()
    {
        var result = new List<CampaignNode>();
        var graph = CurrentGraph;
        if (graph == null) return result;

        foreach (var nodeId in CompletedNodes)
        {
            var node = graph.GetNode(nodeId);
            if (node != null)
            {
                result.Add(node);
            }
        }
        return result;
    }

    // =========================================================================
    // GRAPH QUERIES
    // =========================================================================

    /// <summary>Get a campaign graph by ID.</summary>
    public CampaignGraph? GetGraph(string campaignId)
    {
        return Graphs.TryGetValue(campaignId, out var graph) ? graph : null;
    }

    /// <summary>Get all campaign graphs.</summary>
    public List<CampaignGraph> GetAllGraphs()
    {
        return [.. Graphs.Values];
    }

    // =========================================================================
    // CLEAR / RESET
    // =========================================================================

    /// <summary>Clear all data.</summary>
    public void Clear()
    {
        Graphs.Clear();
        CompletedNodes.Clear();
        CurrentCampaignId = "";
    }

    /// <summary>Clear only progress data (keeps graphs loaded).</summary>
    public void ClearProgress()
    {
        CompletedNodes.Clear();
    }
}
