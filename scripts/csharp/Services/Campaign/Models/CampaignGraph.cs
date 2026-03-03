using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Data.Events;

namespace Fateforged.Meta.Campaign.Models;

/// <summary>
/// Represents a campaign as a directed graph of nodes connected by edges.
/// Provides graph traversal and query operations.
/// </summary>
public class CampaignGraph
{
    /// <summary>Unique identifier for this campaign.</summary>
    public CampaignId CampaignId { get; set; } = Data.Events.CampaignId.None;

    /// <summary>Display name localization key.</summary>
    public string NameKey { get; set; } = "";

    /// <summary>Description localization key.</summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>ID of the starting node.</summary>
    public EventId StartNodeId { get; set; } = EventId.None;

    /// <summary>Sort order for campaign selection UI.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>All nodes in the graph, indexed by event ID.</summary>
    public Dictionary<EventId, CampaignNode> Nodes { get; } = [];

    /// <summary>All edges in the graph.</summary>
    public List<CampaignEdge> Edges { get; } = [];

    /// <summary>Pre-computed incoming edges for each node (for unlock checking).</summary>
    private Dictionary<EventId, List<CampaignEdge>> _incomingEdges = [];

    /// <summary>Pre-computed outgoing edges for each node (for traversal).</summary>
    private Dictionary<EventId, List<CampaignEdge>> _outgoingEdges = [];

    // =========================================================================
    // CONSTRUCTION
    // =========================================================================

    /// <summary>
    /// Create a CampaignGraph from GDScript Dictionary.
    /// Expected format:
    /// {
    ///   "campaign_id": "summoners_path",
    ///   "name_key": "campaign.summoners_path.name",
    ///   "description_key": "campaign.summoners_path.description",
    ///   "start_node": "battle_01",
    ///   "sort_order": 1,
    ///   "nodes": [...],
    ///   "edges": [...]
    /// }
    /// </summary>
    public static CampaignGraph FromDictionary(Godot.Collections.Dictionary dict)
    {
        var graph = new CampaignGraph
        {
            CampaignId = new CampaignId(dict.GetValueOrDefault("campaign_id", "").AsString()),
            NameKey = dict.GetValueOrDefault("name_key", "").AsString(),
            DescriptionKey = dict.GetValueOrDefault("description_key", "").AsString(),
            StartNodeId = new EventId(dict.GetValueOrDefault("start_node", "").AsString()),
            SortOrder = dict.GetValueOrDefault("sort_order", 0).AsInt32()
        };

        // Parse nodes
        var nodesVariant = dict.GetValueOrDefault("nodes", new Godot.Collections.Array());
        if (nodesVariant.Obj is Godot.Collections.Array nodesArray)
        {
            foreach (var nodeVariant in nodesArray)
            {
                if (nodeVariant.Obj is Godot.Collections.Dictionary nodeDict)
                {
                    var node = CampaignNode.FromDictionary(nodeDict, graph.CampaignId);
                    if (node.Id.HasValue)
                    {
                        graph.Nodes[node.Id] = node;
                    }
                }
            }
        }

        // Parse edges
        var edgesVariant = dict.GetValueOrDefault("edges", new Godot.Collections.Array());
        if (edgesVariant.Obj is Godot.Collections.Array edgesArray)
        {
            foreach (var edgeVariant in edgesArray)
            {
                if (edgeVariant.Obj is Godot.Collections.Dictionary edgeDict)
                {
                    var edge = CampaignEdge.FromDictionary(edgeDict);
                    if (edge.FromId.HasValue && edge.ToId.HasValue)
                    {
                        graph.Edges.Add(edge);
                    }
                }
            }
        }

        // Build edge indices
        graph.RebuildEdgeIndices();

        return graph;
    }

    /// <summary>
    /// Create a CampaignGraph from a typed CampaignDefinition.
    /// This is the new preferred construction method.
    /// </summary>
    public static CampaignGraph FromCampaignDefinition(CampaignDefinition definition)
    {
        var graph = new CampaignGraph
        {
            CampaignId = definition.Id,
            NameKey = definition.NameKey,
            DescriptionKey = definition.DescriptionKey,
            StartNodeId = definition.StartEventId,
            SortOrder = definition.SortOrder
        };

        // Add nodes from EventCatalog
        foreach (var eventId in definition.EventIds)
        {
            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null) continue;

            var node = CampaignNode.FromEventDefinition(evt, definition.Id);
            if (node.Id.HasValue)
            {
                graph.Nodes[node.Id] = node;
            }
        }

        // Add edges (convert from Data.Events.CampaignEdge to Models.CampaignEdge)
        foreach (var edge in definition.Edges)
        {
            var campaignEdge = new CampaignEdge
            {
                FromId = edge.FromEventId,
                ToId = edge.ToEventId
            };

            // Convert edge condition if present
            if (edge.Condition?.ChoiceId != null)
            {
                campaignEdge.Condition = new EdgeCondition
                {
                    Type = "choice",
                    Value = edge.Condition.ChoiceId.Value.Value
                };
            }

            if (campaignEdge.FromId.HasValue && campaignEdge.ToId.HasValue)
            {
                graph.Edges.Add(campaignEdge);
            }
        }

        // Build edge indices
        graph.RebuildEdgeIndices();

        return graph;
    }

    /// <summary>
    /// Rebuild the edge indices after modifying nodes or edges.
    /// </summary>
    public void RebuildEdgeIndices()
    {
        _incomingEdges.Clear();
        _outgoingEdges.Clear();

        foreach (var edge in Edges)
        {
            // Add to outgoing edges of source
            if (!_outgoingEdges.TryGetValue(edge.FromId, out var outgoing))
            {
                outgoing = [];
                _outgoingEdges[edge.FromId] = outgoing;
            }
            outgoing.Add(edge);

            // Add to incoming edges of target
            if (!_incomingEdges.TryGetValue(edge.ToId, out var incoming))
            {
                incoming = [];
                _incomingEdges[edge.ToId] = incoming;
            }
            incoming.Add(edge);
        }
    }

    /// <summary>
    /// Convert back to GDScript Dictionary for interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        var nodesArray = new Godot.Collections.Array();
        foreach (var node in Nodes.Values)
        {
            nodesArray.Add(node.ToDictionary());
        }

        var edgesArray = new Godot.Collections.Array();
        foreach (var edge in Edges)
        {
            edgesArray.Add(edge.ToDictionary());
        }

        return new Godot.Collections.Dictionary
        {
            ["campaign_id"] = (string)CampaignId,
            ["name_key"] = NameKey,
            ["description_key"] = DescriptionKey,
            ["start_node"] = (string)StartNodeId,
            ["sort_order"] = SortOrder,
            ["nodes"] = nodesArray,
            ["edges"] = edgesArray
        };
    }

    // =========================================================================
    // NODE QUERIES
    // =========================================================================

    /// <summary>Get a node by ID, or null if not found.</summary>
    public CampaignNode? GetNode(EventId nodeId)
    {
        return Nodes.TryGetValue(nodeId, out var node) ? node : null;
    }

    /// <summary>Get all nodes of a specific type.</summary>
    public IEnumerable<CampaignNode> GetNodesByType(string type)
    {
        return Nodes.Values.Where(n => n.Type == type);
    }

    /// <summary>Get the start node.</summary>
    public CampaignNode? GetStartNode()
    {
        return GetNode(StartNodeId);
    }

    /// <summary>Get all nodes as a list (preserves insertion order from data).</summary>
    public List<CampaignNode> GetAllNodes()
    {
        return Nodes.Values.ToList();
    }

    // =========================================================================
    // EDGE QUERIES
    // =========================================================================

    /// <summary>Get all edges going INTO a node.</summary>
    public List<CampaignEdge> GetIncomingEdges(EventId nodeId)
    {
        return _incomingEdges.TryGetValue(nodeId, out var edges) ? edges : [];
    }

    /// <summary>Get all edges going OUT OF a node.</summary>
    public List<CampaignEdge> GetOutgoingEdges(EventId nodeId)
    {
        return _outgoingEdges.TryGetValue(nodeId, out var edges) ? edges : [];
    }

    /// <summary>Get all node IDs that this node can lead to.</summary>
    public List<EventId> GetSuccessors(EventId nodeId)
    {
        return GetOutgoingEdges(nodeId).Select(e => e.ToId).ToList();
    }

    /// <summary>Get all node IDs that can lead to this node.</summary>
    public List<EventId> GetPredecessors(EventId nodeId)
    {
        return GetIncomingEdges(nodeId).Select(e => e.FromId).ToList();
    }

    // =========================================================================
    // GRAPH PROPERTIES
    // =========================================================================

    /// <summary>Check if a node has any incoming edges (is it reachable?).</summary>
    public bool HasIncomingEdges(EventId nodeId)
    {
        return _incomingEdges.ContainsKey(nodeId) && _incomingEdges[nodeId].Count > 0;
    }

    /// <summary>Check if a node has any outgoing edges (does it lead somewhere?).</summary>
    public bool HasOutgoingEdges(EventId nodeId)
    {
        return _outgoingEdges.ContainsKey(nodeId) && _outgoingEdges[nodeId].Count > 0;
    }

    /// <summary>Check if a node is a start node (no incoming edges or is designated start).</summary>
    public bool IsStartNode(EventId nodeId)
    {
        return nodeId == StartNodeId || !HasIncomingEdges(nodeId);
    }

    /// <summary>Check if a node is an end node (no outgoing edges).</summary>
    public bool IsEndNode(EventId nodeId)
    {
        return !HasOutgoingEdges(nodeId);
    }

    /// <summary>Get all end nodes (nodes with no outgoing edges).</summary>
    public List<CampaignNode> GetEndNodes()
    {
        return Nodes.Values.Where(n => IsEndNode(n.Id)).ToList();
    }
}
