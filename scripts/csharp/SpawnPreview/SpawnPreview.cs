using Godot;
using System.Collections.Generic;

namespace ProjectSummoner.SpawnPreview;

/// <summary>
/// Visual preview showing where units will spawn during card drag.
/// Uses ghost units to show exactly what will spawn.
/// </summary>
[GlobalClass]
public partial class SpawnPreview : Node3D
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

    private List<GhostUnit3D> _ghostUnits = new();
    private MeshInstance3D? _circleMarker;  // Fallback if ghost creation fails
    private float _collisionRadius = 0.5f;
    private bool _isValid = true;
    private int _spawnCount = 1;
    private PackedScene? _unitScene;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Initialize preview with unit scene and spawn count.
    /// </summary>
    public void Setup(PackedScene unitScene, int spawnCount = 1)
    {
        _unitScene = unitScene;
        _spawnCount = spawnCount;

        if (unitScene != null)
        {
            // Extract collision_radius for spacing/fallback
            var tempUnit = unitScene.Instantiate();
            if (tempUnit != null)
            {
                var radiusVar = tempUnit.Get("collision_radius");
                if (radiusVar.VariantType != Variant.Type.Nil)
                {
                    _collisionRadius = radiusVar.AsSingle();
                }
                // Also try C# property name
                radiusVar = tempUnit.Get("CollisionRadius");
                if (radiusVar.VariantType != Variant.Type.Nil)
                {
                    _collisionRadius = radiusVar.AsSingle();
                }
                tempUnit.QueueFree();
            }
        }

        // Create ghost units for preview
        CreateGhostUnits();
    }

    /// <summary>
    /// Update the preview position.
    /// </summary>
    public void UpdatePosition(Vector3 pos)
    {
        GlobalPosition = pos;
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
            var ghost = new GhostUnit3D();
            AddChild(ghost);
            ghost.Setup(_unitScene);
            ghost.SetValid(_isValid);
            _ghostUnits.Add(ghost);

            if (ghost.HasVisual())
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
    /// </summary>
    private void UpdateFormationPositions()
    {
        for (int i = 0; i < _ghostUnits.Count; i++)
        {
            var offset = FormationHelper.GenerateFormationOffset(i, _spawnCount);
            _ghostUnits[i].Position = offset;
        }
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
    /// Update circle mesh size based on collision_radius.
    /// </summary>
    private void UpdateCircleSize()
    {
        if (_circleMarker == null)
            return;

        float radius = _collisionRadius;

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
