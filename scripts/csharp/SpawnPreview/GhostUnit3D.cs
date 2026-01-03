using Godot;
using ProjectSummoner.Visual;

namespace ProjectSummoner.SpawnPreview;

/// <summary>
/// Transparent preview of a unit during card drag.
/// Shows what the spawned unit will look like with ghostly transparency.
/// Supports both C# visual components and GDScript fallbacks.
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
    private ShadowComponent? _groundShadow;

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
    public void Setup(PackedScene unitScene)
    {
        if (unitScene == null)
            return;

        // Instantiate unit to find its Visual child and flight altitude
        var tempUnit = unitScene.Instantiate();
        if (tempUnit == null)
            return;

        // Check for flight altitude (C# units use PascalCase)
        if (tempUnit is ProjectSummoner.Units.Unit3D unit)
        {
            _flightAltitude = unit.FlightAltitude;
        }

        // Find the Visual child node
        var visualNode = tempUnit.GetNodeOrNull("Visual");
        if (visualNode == null)
        {
            tempUnit.QueueFree();
            return;
        }

        // Get the scene file path to instantiate fresh (avoids ViewportTexture reference issues)
        string scenePath = visualNode.SceneFilePath;
        Node? newVisual = null;

        if (!string.IsNullOrEmpty(scenePath))
        {
            // Instantiate fresh from scene file
            var visualScene = GD.Load<PackedScene>(scenePath);
            if (visualScene != null)
            {
                newVisual = visualScene.Instantiate();

                // Copy relevant properties BEFORE adding to tree (so _Ready() sees them)
                CopyVisualProperties(visualNode, newVisual);
            }
        }

        // Fallback to duplicate if scene instantiation failed
        if (newVisual == null)
        {
            newVisual = visualNode.Duplicate((int)(
                Node.DuplicateFlags.UseInstantiation |
                Node.DuplicateFlags.Scripts |
                Node.DuplicateFlags.Signals));
        }

        if (newVisual == null)
        {
            tempUnit.QueueFree();
            return;
        }

        // Add to ghost (this triggers _Ready() which needs properties to be set)
        AddChild(newVisual);
        _visualRoot = newVisual;

        // Clean up temp unit
        tempUnit.QueueFree();

        // Position at flight altitude if flying
        if (_flightAltitude > 0)
        {
            Position = new Vector3(0, _flightAltitude, 0);
            CreateGroundShadow();
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

        if (_groundShadow != null && IsInstanceValid(_groundShadow))
        {
            _groundShadow.QueueFree();
        }
        _groundShadow = null;

        QueueFree();
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Copy visual properties from source to destination node.
    /// </summary>
    private static void CopyVisualProperties(Node source, Node dest)
    {
        // C# visual component properties
        CopyPropertyIfExists(source, dest, "SkeletalScene");
        CopyPropertyIfExists(source, dest, "ScaleFactor");
        CopyPropertyIfExists(source, dest, "SpriteScale");
        CopyPropertyIfExists(source, dest, "SpriteFramesResource");
        CopyPropertyIfExists(source, dest, "ViewportScale");
        CopyPropertyIfExists(source, dest, "FeetOffsetPixels");
        CopyPropertyIfExists(source, dest, "HeadOffsetPixels");

        // GDScript visual component properties (snake_case)
        CopyPropertyIfExists(source, dest, "skeletal_scene");
        CopyPropertyIfExists(source, dest, "scale_factor");
        CopyPropertyIfExists(source, dest, "sprite_frames");
        CopyPropertyIfExists(source, dest, "sprite_scale");
        CopyPropertyIfExists(source, dest, "viewport_scale");
    }

    private static void CopyPropertyIfExists(Node source, Node dest, string property)
    {
        var value = source.Get(property);
        if (value.VariantType != Variant.Type.Nil)
        {
            dest.Set(property, value);
        }
    }

    /// <summary>
    /// Create a shadow on the ground for flying units.
    /// </summary>
    private void CreateGroundShadow()
    {
        _groundShadow = new ShadowComponent();
        GetParent()?.AddChild(_groundShadow);
        _groundShadow.Initialize(1.0f, 0.4f);
        _groundShadow.UpdateForAltitude(_flightAltitude);
    }

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

        // Ensure ghost faces correct direction (player units face left in this game layout)
        if (_visualRoot.HasMethod("set_flip_h"))
        {
            _visualRoot.Call("set_flip_h", true);
        }
        else if (_visualRoot.HasMethod("SetFlipH"))
        {
            _visualRoot.Call("SetFlipH", true);
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

    /// <summary>
    /// Try to find and tint internal sprites in visual components.
    /// </summary>
    private static void ApplyTintToInternalSprites(Node node, Color tint)
    {
        // Look for SubViewport with sprites inside
        var viewport = node.GetNodeOrNull<SubViewport>("Sprite3D/SubViewport");
        if (viewport == null)
            return;

        // Find all CanvasItems and modulate them
        ApplyTintToCanvasItems(viewport, tint);
    }

    /// <summary>
    /// Recursively apply tint to all CanvasItem nodes.
    /// </summary>
    private static void ApplyTintToCanvasItems(Node node, Color tint)
    {
        if (node is CanvasItem canvasItem)
        {
            canvasItem.Modulate = tint;
        }

        foreach (var child in node.GetChildren())
        {
            ApplyTintToCanvasItems(child, tint);
        }
    }
}
