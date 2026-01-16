using Godot;
using ProjectSummoner.Visual;

namespace ProjectSummoner.SpawnPreview;

/// <summary>
/// Transparent preview of a unit during card drag.
/// Shows what the spawned unit will look like with ghostly transparency.
/// Supports both C# visual components and GDScript interops.
/// </summary>
[GlobalClass]
public partial class GhostUnit3D : Node3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private static readonly Color ValidTint = new(0.7f, 0.85f, 1.0f, 0.5f);   // Light blue, 50% alpha
    private static readonly Color InvalidTint = new(1.0f, 0.5f, 0.5f, 0.5f);  // Red, 50% alpha

    // =========================================================================
    // STATE
    // =========================================================================

    private Node? _visualRoot;
    private bool _isValid = true;
    private float _flightAltitude;
    private bool _facingRight = true;  // Default to player (facing right)

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Check if this ghost has a valid visual (useful for fallback detection).
    /// </summary>
    public bool HasVisual() => _visualRoot != null;

    /// <summary>
    /// Initialize ghost with unit scene data.
    /// </summary>
    /// <param name="unitScene">The unit scene to preview</param>
    /// <param name="team">Team (0=player faces right, 1=enemy faces left)</param>
    public void Setup(PackedScene unitScene, int team = 0)
    {
        if (unitScene == null)
            return;

        // Player team faces right (flip=true), enemy faces left (flip=false)
        _facingRight = team == 0;

        // Instantiate unit to find its Visual child and flight altitude
        var tempUnit = unitScene.Instantiate();
        if (tempUnit == null)
            return;

        // Get spawn altitude from Unit3D (single source of truth)
        if (tempUnit is ProjectSummoner.Units.Unit3D unit)
        {
            _flightAltitude = unit.GetSpawnAltitude();
        }

        // Find the Visual child node - it already has all property overrides from the unit scene
        var visualNode = tempUnit.GetNodeOrNull("Visual");
        if (visualNode == null)
        {
            tempUnit.Free();  // Not in tree, use Free() not QueueFree()
            return;
        }

        // Reparent the Visual from the unit to the ghost (truly DRY - no property copying needed)
        // This preserves all property overrides set in the unit scene file
        visualNode.Owner = null;  // Unset owner to avoid "inconsistent owner" warning
        tempUnit.RemoveChild(visualNode);
        AddChild(visualNode);
        _visualRoot = visualNode;

        // Clean up the unit shell (Visual has been moved out)
        tempUnit.Free();  // Not in tree, use Free() not QueueFree()

        // Position at flight altitude if flying
        if (_flightAltitude > 0)
        {
            Position = new Vector3(0, _flightAltitude, 0);
        }

        // Apply ghost transparency AFTER the component fully initializes
        // Visual components may use await in _Ready(), so we need to wait
        CallDeferred(MethodName.ApplyGhostAppearanceDeferred);
    }

    /// <summary>
    /// Set whether the spawn position is valid (changes tint color).
    /// </summary>
    public void SetValid(bool isValid)
    {
        if (_isValid == isValid)
            return;

        _isValid = isValid;
        ApplyGhostAppearance();
    }

    /// <summary>
    /// Clean up resources.
    /// </summary>
    public void Cleanup()
    {
        if (_visualRoot != null && IsInstanceValid(_visualRoot))
        {
            _visualRoot.QueueFree();
        }
        _visualRoot = null;

        QueueFree();
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Apply ghost transparency after waiting for component to initialize.
    /// Visual components may use await in _Ready(), so children don't exist immediately.
    /// </summary>
    private async void ApplyGhostAppearanceDeferred()
    {
        // Wait 2 frames for visual components to fully initialize
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (_visualRoot == null || !IsInstanceValid(_visualRoot))
            return;

        // Set facing direction based on team
        if (_visualRoot.HasMethod("set_flip_h"))
        {
            _visualRoot.Call("set_flip_h", _facingRight);
        }
        else if (_visualRoot.HasMethod("SetFlipH"))
        {
            _visualRoot.Call("SetFlipH", _facingRight);
        }

        ApplyGhostAppearance();
    }

    /// <summary>
    /// Apply ghost transparency and tint to the visual.
    /// </summary>
    private void ApplyGhostAppearance()
    {
        if (_visualRoot == null)
            return;

        ApplyGhostAppearanceToNode(_visualRoot);
    }

    /// <summary>
    /// Apply ghost transparency to a node and its children.
    /// </summary>
    private void ApplyGhostAppearanceToNode(Node node)
    {
        var tint = _isValid ? ValidTint : InvalidTint;

        // Check for C# IVisualComponent
        if (node is IVisualComponent visualComponent)
        {
            visualComponent.ApplyGhostTint(tint);
            return;
        }

        // Check for SkeletalVisualComponent (C#)
        if (node is SkeletalVisualComponent skeletalComp)
        {
            // Access internal skeletal instance via reflection-like approach
            var skeletalInstance = node.Get("_skeletalInstance");
            if (skeletalInstance.VariantType != Variant.Type.Nil && skeletalInstance.AsGodotObject() is Node2D skel2D)
            {
                skel2D.Modulate = tint;
            }
            return;
        }

        // Check for SpriteVisualComponent (C#)
        if (node is SpriteVisualComponent)
        {
            // Try to find the character sprite
            var charSprite = node.GetNodeOrNull<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");
            if (charSprite != null)
            {
                charSprite.Modulate = tint;
            }
            return;
        }

        // GDScript: SkeletalCharacter2D5Component
        if (node.GetClass() == "SkeletalCharacter2D5Component" ||
            node.SceneFilePath.Contains("skeletal"))
        {
            var skeletalInstance = node.Get("skeletal_instance");
            if (skeletalInstance.VariantType != Variant.Type.Nil && skeletalInstance.AsGodotObject() is Node2D skel)
            {
                skel.Modulate = tint;
            }
            return;
        }

        // GDScript: SpriteCharacter2D5Component
        if (node.GetClass() == "SpriteCharacter2D5Component" ||
            node.SceneFilePath.Contains("sprite"))
        {
            var animSprite = node.Get("animated_sprite");
            if (animSprite.VariantType != Variant.Type.Nil && animSprite.AsGodotObject() is AnimatedSprite2D sprite)
            {
                sprite.Modulate = tint;
            }
            return;
        }

        // Apply to Sprite3D (fallback for other cases)
        if (node is Sprite3D sprite3D)
        {
            sprite3D.Modulate = tint;
        }

        // Apply to CanvasItem children inside SubViewport
        if (node is CanvasItem canvasItem)
        {
            canvasItem.Modulate = tint;
        }

        // Recursively apply to children
        foreach (var child in node.GetChildren())
        {
            ApplyGhostAppearanceToNode(child);
        }
    }

}
