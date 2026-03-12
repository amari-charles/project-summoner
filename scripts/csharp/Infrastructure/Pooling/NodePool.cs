using System.Collections.Generic;
using Godot;

namespace Fateforged.Infrastructure.Pooling;

/// <summary>
/// Contract for objects managed by NodePool.
/// Ensures proper state reset on reuse to prevent stale data leaks.
/// </summary>
public interface IPoolable
{
    /// <summary>Called when taken from pool and put into active use.</summary>
    void OnAcquired();

    /// <summary>Called when returned to pool. Hide visuals, stop processing.</summary>
    void OnReleased();

    /// <summary>Reset all mutable state for safe reuse.</summary>
    void ResetState();
}

/// <summary>
/// Generic object pool for Godot Node3D types.
/// Pooled nodes stay in the scene tree under a hidden container to avoid orphan warnings.
/// </summary>
public class NodePool<T>
    where T : Node3D, IPoolable, new()
{
    private readonly Node _container;
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _active = new();
    private readonly int _maxSize;

    public int ActiveCount => _active.Count;
    public int AvailableCount => _available.Count;

    /// <param name="parent">Parent node in the scene tree (pool container will be added as child).</param>
    /// <param name="maxSize">Maximum total pooled instances. Once exceeded, oldest released items are freed.</param>
    public NodePool(Node parent, int maxSize = 64)
    {
        _maxSize = maxSize;
        _container = new Node3D { Name = $"Pool_{typeof(T).Name}" };
        ((Node3D)_container).Visible = false;
        parent.AddChild(_container);
    }

    /// <summary>
    /// Get an instance from the pool, or create a new one.
    /// The returned node is reparented out of the pool container — caller must AddChild to desired parent.
    /// </summary>
    public T Acquire()
    {
        T item;
        if (_available.Count > 0)
        {
            item = _available.Pop();
            _container.RemoveChild(item);
        }
        else
        {
            item = new T();
        }

        _active.Add(item);
        item.ResetState();
        item.OnAcquired();
        return item;
    }

    /// <summary>
    /// Return an instance to the pool. The node is reparented into the hidden container.
    /// </summary>
    public void Release(T item)
    {
        if (!_active.Remove(item))
            return;

        item.OnReleased();

        // Enforce max pool size — free excess instead of hoarding
        if (_available.Count >= _maxSize)
        {
            item.QueueFree();
            return;
        }

        // Reparent into pool container
        var currentParent = item.GetParent();
        currentParent?.RemoveChild(item);
        _container.AddChild(item);

        _available.Push(item);
    }

    /// <summary>
    /// Free all pooled instances (both active and available).
    /// </summary>
    public void Clear()
    {
        foreach (var item in _active)
        {
            if (Node.IsInstanceValid(item))
            {
                item.OnReleased();
                item.QueueFree();
            }
        }
        _active.Clear();

        while (_available.Count > 0)
        {
            var item = _available.Pop();
            if (Node.IsInstanceValid(item))
                item.QueueFree();
        }
    }
}
