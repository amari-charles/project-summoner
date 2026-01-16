using Godot;
using System.Collections.Generic;
using ProjectSummoner.Summons;

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
    private float _separationRadius = 0.5f;
    private bool _isValid = true;
    private int _spawnCount = 1;
    private PackedScene? _unitScene;
    private int _team = 0;  // 0=player, 1=enemy

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Initialize preview with unit scene and spawn count.
    /// </summary>
    /// <param name="unitScene">The unit scene to preview</param>
    /// <param name="spawnCount">Number of units to spawn</param>
    /// <param name="team">Team (0=player, 1=enemy) for facing direction</param>
    public void Setup(PackedScene unitScene, int spawnCount = 1, int team = 0)
    {
        _unitScene = unitScene;
        _spawnCount = spawnCount;
        _team = team;

        if (unitScene != null)
        {
            // Get separation radius from UnitSpawner (single source of truth)
            _separationRadius = UnitSpawner.GetSeparationRadius(unitScene);
        }

        // Create ghost units for preview
        CreateGhostUnits();
    }

    /// <summary>
    /// Update the preview position (legacy - single position for center).
    /// </summary>
    public void UpdatePosition(Vector3 pos)
    {
        GlobalPosition = pos;
    }

    /// <summary>
    /// Update positions for each ghost unit (matches actual spawn positions).
    /// </summary>
    public void UpdatePositions(Godot.Collections.Array<Vector3> positions)
    {
        // Set SpawnPreview to origin since ghost units will use global positions
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
            var ghost = new GhostUnit3D();
            AddChild(ghost);
            ghost.Setup(_unitScene, _team);
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
