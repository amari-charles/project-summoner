using System.Collections.Generic;
using Godot;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Maps network IDs to node instances for synchronized entities.
/// Only the host/authority assigns new IDs; clients receive assignments.
/// </summary>
public class NetworkIdRegistry
{
    private int _nextId = 1;
    private readonly Dictionary<int, Node> _idToNode = new();
    private readonly Dictionary<Node, int> _nodeToId = new();

    /// <summary>
    /// Register a node and assign it a network ID.
    /// Should only be called by the host/authority.
    /// </summary>
    /// <returns>The assigned network ID</returns>
    public int Register(Node node)
    {
        if (_nodeToId.TryGetValue(node, out var existingId))
        {
            return existingId;
        }

        var id = _nextId++;
        _idToNode[id] = node;
        _nodeToId[node] = id;
        return id;
    }

    /// <summary>
    /// Reserve the next network ID without registering a node.
    /// Used when broadcasting a spawn message before the unit is actually created.
    /// The actual node should be registered later using RegisterWithId().
    /// </summary>
    /// <returns>The reserved network ID</returns>
    public int NextIdWithoutRegistering()
    {
        return _nextId++;
    }

    /// <summary>
    /// Register a node with a specific network ID.
    /// Used by clients when receiving ID assignments from host.
    /// </summary>
    public void RegisterWithId(Node node, int networkId)
    {
        // Update next ID to avoid collisions
        if (networkId >= _nextId)
        {
            _nextId = networkId + 1;
        }

        _idToNode[networkId] = node;
        _nodeToId[node] = networkId;
    }

    /// <summary>
    /// Get the node for a network ID.
    /// </summary>
    /// <returns>The node, or null if not found</returns>
    public Node? GetNode(int networkId)
    {
        return _idToNode.TryGetValue(networkId, out var node) ? node : null;
    }

    /// <summary>
    /// Get the node for a network ID, cast to a specific type.
    /// </summary>
    public T? GetNode<T>(int networkId) where T : Node
    {
        return GetNode(networkId) as T;
    }

    /// <summary>
    /// Get the network ID for a node.
    /// </summary>
    /// <returns>The network ID, or -1 if not registered</returns>
    public int GetId(Node node)
    {
        return _nodeToId.TryGetValue(node, out var id) ? id : -1;
    }

    /// <summary>
    /// Check if a node is registered.
    /// </summary>
    public bool IsRegistered(Node node)
    {
        return _nodeToId.ContainsKey(node);
    }

    /// <summary>
    /// Check if a network ID is registered.
    /// </summary>
    public bool HasId(int networkId)
    {
        return _idToNode.ContainsKey(networkId);
    }

    /// <summary>
    /// Unregister a node (e.g., when it dies or is removed).
    /// </summary>
    public void Unregister(Node node)
    {
        if (_nodeToId.TryGetValue(node, out var id))
        {
            _idToNode.Remove(id);
            _nodeToId.Remove(node);
        }
    }

    /// <summary>
    /// Unregister by network ID.
    /// </summary>
    public void UnregisterById(int networkId)
    {
        if (_idToNode.TryGetValue(networkId, out var node))
        {
            _idToNode.Remove(networkId);
            _nodeToId.Remove(node);
        }
    }

    /// <summary>
    /// Clear all registrations. Called when match ends.
    /// </summary>
    public void Clear()
    {
        _idToNode.Clear();
        _nodeToId.Clear();
        _nextId = 1;
    }

    /// <summary>
    /// Get all registered network IDs.
    /// </summary>
    public IEnumerable<int> GetAllIds()
    {
        return _idToNode.Keys;
    }

    /// <summary>
    /// Get the count of registered entities.
    /// </summary>
    public int Count => _idToNode.Count;
}
