using Godot;
using System.Collections.Generic;
using ProjectSummoner.Constants;
using ProjectSummoner.Units;
using ProjectSummoner.Visual;

namespace Fateforged.Input;

/// <summary>
/// Visual preview showing where units will spawn during card drag.
/// Uses ghost units to show exactly what will spawn.
/// </summary>
[GlobalClass]
public partial class SummonPreview : Node3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    private static readonly Color ValidColor = new(0.3f, 0.7f, 1.0f, 0.5f);    // Light blue (valid spawn)
    private static readonly Color InvalidColor = new(1.0f, 0.3f, 0.3f, 0.5f);  // Red (invalid spawn)
    private const float CircleHeight = 0.05f;
    private const float GroundOverlayOffset = 0.02f;  // Same as BattlefieldConstants.GROUND_OVERLAY_OFFSET

    // =========================================================================
    // STATE
    // =========================================================================

    private List<UnitGhost> _ghostUnits = new();
    private MeshInstance3D? _circleMarker;  // Fallback if ghost creation fails
    private float _separationRadius = 0.5f;
    private float _flightAltitude;
    private bool _isValid = true;
    private int _spawnCount = 1;
    private PackedScene? _unitScene;
    private int _team = 0;  // 0=player, 1=enemy

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// One-shot init. Resolves unit-specific data (flight altitude,
    /// separation radius) from UnitDefinitions internally.
    /// </summary>
    /// <param name="unitScene">The unit scene to preview</param>
    /// <param name="spawnCount">Number of units to spawn</param>
    /// <param name="team">Team (0=player, 1=enemy) for facing direction</param>
    /// <param name="catalogId">Catalog ID for unit definition lookup</param>
    public void Initialize(PackedScene unitScene, int spawnCount, int team, string catalogId)
    {
        _unitScene = unitScene;
        _spawnCount = spawnCount;
        _team = team;

        // Resolve unit-specific data from catalog
        if (UnitDefinitions.TryGet(new UnitId(catalogId), out var unitDef) && unitDef != null)
        {
            _separationRadius = unitDef.Visual.SeparationRadius;
            if (unitDef.Flying != null)
            {
                _flightAltitude = unitDef.Flying.Altitude;
            }
        }

        // Create ghost units for preview
        CreateGhostUnits();
    }

    /// <summary>
    /// Update positions for each ghost unit (matches actual spawn positions).
    /// </summary>
    public void UpdatePositions(Godot.Collections.Array<Vector3> positions)
    {
        // Set SummonPreview to origin since ghost units will use global positions
        GlobalPosition = Vector3.Zero;

        for (int i = 0; i < _ghostUnits.Count && i < positions.Count; i++)
        {
            if (IsInstanceValid(_ghostUnits[i]))
            {
                // Preserve Y (includes flight altitude) while updating X/Z from positions
                var ghost = _ghostUnits[i];
                ghost.GlobalPosition = new Vector3(positions[i].X, ghost.GlobalPosition.Y, positions[i].Z);
            }
        }
    }

    /// <summary>
    /// Set whether the spawn position is valid (changes color).
    /// </summary>
    public void SetValid(bool isValid)
    {
        if (_isValid == isValid)
            return;

        _isValid = isValid;

        // Update all ghost units
        foreach (var ghost in _ghostUnits)
        {
            if (IsInstanceValid(ghost))
            {
                ghost.SetValid(isValid);
            }
        }

        // Update fallback circle if used
        UpdateMaterialColor();
    }

    /// <summary>
    /// Clean up resources.
    /// </summary>
    public void Cleanup()
    {
        foreach (var ghost in _ghostUnits)
        {
            if (IsInstanceValid(ghost))
            {
                ghost.Cleanup();
            }
        }
        _ghostUnits.Clear();

        if (_circleMarker != null)
        {
            _circleMarker.QueueFree();
            _circleMarker = null;
        }

        QueueFree();
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Create ghost unit previews in formation.
    /// </summary>
    private void CreateGhostUnits()
    {
        // Clear existing ghosts
        foreach (var ghost in _ghostUnits)
        {
            if (IsInstanceValid(ghost))
            {
                ghost.QueueFree();
            }
        }
        _ghostUnits.Clear();

        if (_unitScene == null)
        {
            // Fallback to circle marker if no unit scene
            CreateCircleMarker();
            return;
        }

        // Create ghost for each unit in formation
        bool anyGhostValid = false;
        for (int i = 0; i < _spawnCount; i++)
        {
            var ghost = new UnitGhost();
            AddChild(ghost);
            ghost.Initialize(_unitScene, _team, _flightAltitude);
            ghost.SetValid(_isValid);
            _ghostUnits.Add(ghost);

            if (ghost.HasVisual)
            {
                anyGhostValid = true;
            }
        }

        // If no ghosts have visuals, fallback to circle marker
        if (!anyGhostValid)
        {
            // Clean up empty ghosts
            foreach (var ghost in _ghostUnits)
            {
                ghost.QueueFree();
            }
            _ghostUnits.Clear();
            CreateCircleMarker();
            return;
        }

        // Position in formation
        UpdateFormationPositions();
    }

    /// <summary>
    /// Update formation positions based on spawn count.
    /// Uses default grid formation for initial placement.
    /// Actual positions are set by GDScript calling UpdatePositions().
    /// </summary>
    private void UpdateFormationPositions()
    {
        for (int i = 0; i < _ghostUnits.Count; i++)
        {
            var offset = GetDefaultGridOffset(i, _spawnCount);
            var ghost = _ghostUnits[i];
            // Preserve Y (flight altitude) while updating X/Z for formation
            ghost.Position = new Vector3(offset.X, ghost.Position.Y, offset.Z);
        }
    }

    /// <summary>
    /// Default grid formation offset for initial ghost positioning.
    /// This is a simplified calculation - actual positions come from Card.get_formation_offset().
    /// </summary>
    private static Vector3 GetDefaultGridOffset(int unitIndex, int unitCount)
    {
        if (unitCount <= 1)
            return Vector3.Zero;

        const float spacing = 1.8f;
        const float rowOffset = 0.5f;
        const int twoRowMax = 20;
        const float largeRowDensity = 3.0f;

        int rows = unitCount <= twoRowMax ? 2 : Mathf.CeilToInt(Mathf.Sqrt(unitCount / largeRowDensity));
        int cols = Mathf.CeilToInt((float)unitCount / rows);

        int row = unitIndex / cols;
        int col = unitIndex % cols;
        int unitsInRow = Mathf.Min(cols, unitCount - row * cols);

        float stagger = row % 2 == 1 ? rowOffset * spacing : 0.0f;
        float formationDepth = (rows - 1) * spacing;
        float xOff = row * spacing - formationDepth / 2.0f;
        float rowWidth = (unitsInRow - 1) * spacing;
        float zOff = col * spacing - rowWidth / 2.0f + stagger;

        return new Vector3(xOff, 0, zOff);
    }

    /// <summary>
    /// Update material color based on validity (for fallback circle).
    /// </summary>
    private void UpdateMaterialColor()
    {
        if (_circleMarker?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = _isValid ? ValidColor : InvalidColor;
        }
    }

    /// <summary>
    /// Create the circle marker mesh (fallback).
    /// </summary>
    private void CreateCircleMarker()
    {
        if (_circleMarker != null)
            return;  // Already exists

        _circleMarker = new MeshInstance3D();
        UpdateCircleSize();
        AddChild(_circleMarker);
    }

    /// <summary>
    /// Update circle mesh size based on separation_radius.
    /// </summary>
    private void UpdateCircleSize()
    {
        if (_circleMarker == null)
            return;

        float radius = _separationRadius;

        // Create a flat cylinder for the circle
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = radius;
        cylinder.BottomRadius = radius;
        cylinder.Height = CircleHeight;
        _circleMarker.Mesh = cylinder;

        // Create semi-transparent material
        var material = new StandardMaterial3D();
        material.AlbedoColor = _isValid ? ValidColor : InvalidColor;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _circleMarker.MaterialOverride = material;

        // Position slightly above ground to avoid z-fighting
        _circleMarker.Position = new Vector3(0, CircleHeight / 2 + GroundOverlayOffset, 0);
    }
}

// =============================================================================
// UnitGhost — internal to SummonPreview
// =============================================================================

/// <summary>
/// Transparent preview of a single unit during card drag.
/// Shows what the spawned unit will look like with ghostly transparency.
/// Internal to SummonPreview — not used by any other component.
/// </summary>
public partial class UnitGhost : Node3D
{
    private static readonly Color ValidTint = new(0.7f, 0.85f, 1.0f, 0.5f);   // Light blue, 50% alpha
    private static readonly Color InvalidTint = new(1.0f, 0.5f, 0.5f, 0.5f);  // Red, 50% alpha

    private Node? _visualRoot;
    private bool _isValid = true;
    private bool _facingRight = true;  // Default to player (facing right)

    /// <summary>
    /// True if this ghost has a valid visual (false = fallback needed).
    /// </summary>
    public bool HasVisual => _visualRoot != null;

    /// <summary>
    /// Create ghost visual from unit scene.
    /// flightAltitude > 0 positions the ghost in the air.
    /// </summary>
    public void Initialize(PackedScene unitScene, int team, float flightAltitude)
    {
        if (unitScene == null)
            return;

        // Player team faces right (flip=true), enemy faces left (flip=false)
        _facingRight = team == 0;

        // Position at flight altitude if flying
        if (flightAltitude > 0)
        {
            Position = new Vector3(0, flightAltitude, 0);
        }

        // Instantiate unit to find its Visual child and flight altitude
        var tempUnit = unitScene.Instantiate();
        if (tempUnit == null)
            return;

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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

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

    private void ApplyGhostAppearance()
    {
        if (_visualRoot == null)
            return;

        ApplyGhostAppearanceToNode(_visualRoot);
    }

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
