using Godot;
using System;
using System.Collections.Generic;
using ProjectSummoner.Units;
using ProjectSummoner.Visual;

namespace Fateforged.View;

/// <summary>
/// Handles the spawn reveal animation (ghost materialize effect).
/// Used by UnitVisual to animate newly spawned units.
/// </summary>
public class SpawnRevealComponent
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private const string ShaderPath = "res://shaders/vfx/spawn_reveal.gdshader";
    private static readonly Color GlowColorPlayer = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Color GlowColorEnemy = new(1.0f, 0.4f, 0.4f, 1.0f);

    // Static shader cache (shared across all instances)
    private static Shader? _shaderCache;

    // =========================================================================
    // STATE
    // =========================================================================

    private bool _isRevealing;
    private ShaderMaterial? _material;
    private Tween? _tween;
    private readonly Dictionary<CanvasItem, Material?> _originalMaterials = new();

    // =========================================================================
    // DEPENDENCIES
    // =========================================================================

    private readonly Node3D _owner;
    private readonly Func<IVisualComponent?> _getVisual;
    private readonly Func<Node3D?> _getShadow;
    private readonly Func<Team> _getTeam;

    /// <summary>
    /// Called when spawn reveal animation completes.
    /// </summary>
    public event Action? OnRevealComplete;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Whether a spawn reveal is currently in progress.
    /// </summary>
    public bool IsRevealing => _isRevealing;

    /// <summary>
    /// Create a new spawn reveal component.
    /// </summary>
    /// <param name="owner">The owning Node3D (used for CreateTween, CallDeferred)</param>
    /// <param name="getVisual">Function to get the visual component</param>
    /// <param name="getShadow">Function to get the shadow component</param>
    /// <param name="getTeam">Function to get the unit's team</param>
    public SpawnRevealComponent(
        Node3D owner,
        Func<IVisualComponent?> getVisual,
        Func<Node3D?> getShadow,
        Func<Team> getTeam)
    {
        _owner = owner;
        _getVisual = getVisual;
        _getShadow = getShadow;
        _getTeam = getTeam;
    }

    /// <summary>
    /// Start the spawn reveal animation.
    /// </summary>
    /// <param name="duration">Animation duration in seconds</param>
    /// <returns>True if animation started, false if already revealing</returns>
    public bool StartReveal(float duration)
    {
        if (_isRevealing)
            return false;

        _isRevealing = true;

        // Start shadow at scale 0 (will grow during reveal)
        var shadow = _getShadow();
        if (shadow != null)
        {
            shadow.Scale = Vector3.Zero;
        }

        // Load shader if not cached
        _shaderCache ??= GD.Load<Shader>(ShaderPath);

        if (_shaderCache == null)
        {
            GD.PushError("SpawnRevealComponent: Failed to load spawn_reveal shader!");
            Complete();
            return false;
        }

        // Create shader material
        _material = new ShaderMaterial();
        _material.Shader = _shaderCache;
        _material.SetShaderParameter("progress", 0.0f);

        // Set glow color based on team
        var team = _getTeam();
        var glowColor = team == Team.Player ? GlowColorPlayer : GlowColorEnemy;
        _material.SetShaderParameter("glow_color", glowColor);

        // Apply shader and start animation (deferred to allow visual component initialization)
        Callable.From(() => ApplyRevealDeferred(duration)).CallDeferred();
        return true;
    }

    // =========================================================================
    // INTERNAL
    // =========================================================================

    private void ApplyRevealDeferred(float duration)
    {
        if (!GodotObject.IsInstanceValid(_owner) || !_isRevealing)
            return;

        ApplyShaderToVisual();

        // Animate progress from 0 to 1
        _tween = _owner.CreateTween();
        _tween.TweenMethod(Callable.From<float>(UpdateProgress), 0.0f, 1.0f, duration);

        // Animate shadow growing alongside
        var shadow = _getShadow();
        if (shadow != null)
        {
            _tween.Parallel().TweenProperty(shadow, "scale", Vector3.One, duration);
        }

        // Complete when done
        _tween.TweenCallback(Callable.From(Complete));
    }

    private void ApplyShaderToVisual()
    {
        var visual = _getVisual();
        if (visual == null || _material == null)
            return;

        _originalMaterials.Clear();

        // Find all CanvasItems in the visual component and apply shader
        if (visual is Node visualNode)
        {
            var sprites = FindAllCanvasItems(visualNode);
            foreach (var sprite in sprites)
            {
                _originalMaterials[sprite] = sprite.Material;
                sprite.Material = _material;
            }
        }
    }

    private static List<CanvasItem> FindAllCanvasItems(Node root)
    {
        var result = new List<CanvasItem>();
        FindCanvasItemsRecursive(root, result);
        return result;
    }

    private static void FindCanvasItemsRecursive(Node node, List<CanvasItem> result)
    {
        if (node is CanvasItem canvasItem)
        {
            result.Add(canvasItem);
        }

        foreach (var child in node.GetChildren())
        {
            FindCanvasItemsRecursive(child, result);
        }
    }

    private void UpdateProgress(float progress)
    {
        _material?.SetShaderParameter("progress", progress);
    }

    private void Complete()
    {
        _isRevealing = false;

        // Restore original materials
        foreach (var (sprite, originalMaterial) in _originalMaterials)
        {
            if (GodotObject.IsInstanceValid(sprite))
            {
                sprite.Material = originalMaterial;
            }
        }
        _originalMaterials.Clear();

        // Clean up shader material
        _material?.Dispose();
        _material = null;

        // Notify owner
        OnRevealComplete?.Invoke();
    }

    /// <summary>
    /// Cancel the reveal animation early (e.g., if unit is destroyed).
    /// </summary>
    public void Cancel()
    {
        if (!_isRevealing)
            return;

        _tween?.Kill();
        _tween = null;

        // Restore original materials without firing completion event
        foreach (var (sprite, originalMaterial) in _originalMaterials)
        {
            if (GodotObject.IsInstanceValid(sprite))
            {
                sprite.Material = originalMaterial;
            }
        }
        _originalMaterials.Clear();

        _material?.Dispose();
        _material = null;
        _isRevealing = false;
    }
}
