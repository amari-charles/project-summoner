using Godot;
using System.Collections.Generic;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Units;

namespace ProjectSummoner.Systems;

/// <summary>
/// Spatial hash grid for efficient proximity queries.
/// Replaces O(n²) unit iteration with O(k) where k = nearby units.
///
/// Grid covers the battlefield (100x80 units) with 10x10 cells = 80 cells total.
/// Units register on spawn, unregister on death, and update position when moving.
/// </summary>
public partial class SpatialGrid : Node
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static SpatialGrid? Instance { get; private set; }

    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    private const float CellSize = 10.0f;
    private const float GridMinX = -50.0f;
    private const float GridMaxX = 50.0f;
    private const float GridMinZ = -40.0f;
    private const float GridMaxZ = 40.0f;
    private const int GridWidth = 10;   // (50 - -50) / 10 = 10 cells
    private const int GridHeight = 8;   // (40 - -40) / 10 = 8 cells
    private const float PositionUpdateThresholdSq = 4.0f;  // 2.0 squared

    // =========================================================================
    // DATA STRUCTURES
    // =========================================================================

    /// <summary>
    /// The grid: List of lists, each cell contains unit references.
    /// Access: _grid[cellIndex] where cellIndex = zIndex * GridWidth + xIndex
    /// </summary>
    private readonly List<List<Node3D>> _grid = new();

    /// <summary>
    /// Reverse lookup: Unit instance ID -> registration data
    /// </summary>
    private readonly Dictionary<ulong, UnitRegistration> _unitRegistry = new();

    private struct UnitRegistration
    {
        public int Cell;
        public Vector3 Position;
    }

    // Debug statistics
    private int _statsQueriesThisFrame;
    private int _statsCellUpdatesThisFrame;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;

        // Initialize empty grid
        _grid.Clear();
        for (int i = 0; i < GridWidth * GridHeight; i++)
        {
            _grid.Add(new List<Node3D>());
        }

        GD.Print($"[SpatialGrid] Initialized {GridWidth}x{GridHeight} grid ({_grid.Count} cells)");
    }

    public override void _Process(double delta)
    {
        // Reset frame stats
        _statsQueriesThisFrame = 0;
        _statsCellUpdatesThisFrame = 0;
    }

    // =========================================================================
    // GDSCRIPT-COMPATIBLE API (snake_case wrappers)
    // =========================================================================

    // These wrappers allow GDScript to call methods using snake_case naming
    public void register_unit(Node3D unit) => RegisterUnit(unit);
    public void unregister_unit(Node3D unit) => UnregisterUnit(unit);
    public bool update_unit_position(Node3D unit) => UpdateUnitPosition(unit);
    public Godot.Collections.Array<Node3D> get_units_in_radius(Vector3 position, float radius)
        => GetUnitsInRadius(position, radius);
    public Godot.Collections.Array<Node3D> get_units_in_radius(Vector3 position, float radius, int teamFilter)
        => GetUnitsInRadius(position, radius, teamFilter);
    public Godot.Collections.Array<Node3D> get_units_in_radius(Vector3 position, float radius, int teamFilter, Node3D? excludeUnit)
        => GetUnitsInRadius(position, radius, teamFilter, excludeUnit);
    public void clear() => Clear();
    public Godot.Collections.Dictionary get_stats() => GetStats();

    // =========================================================================
    // REGISTRATION API
    // =========================================================================

    /// <summary>
    /// Register a unit with the spatial grid.
    /// Called from Unit3D._Ready() after adding to groups.
    /// </summary>
    public void RegisterUnit(Node3D unit)
    {
        if (!IsInstanceValid(unit))
            return;

        ulong instanceId = unit.GetInstanceId();

        // Already registered?
        if (_unitRegistry.ContainsKey(instanceId))
            return;

        Vector3 pos = unit.GlobalPosition;
        int cellIdx = GetCellIndex(pos);

        // Add to grid cell
        if (cellIdx >= 0 && cellIdx < _grid.Count)
        {
            _grid[cellIdx].Add(unit);
        }

        // Add to registry
        _unitRegistry[instanceId] = new UnitRegistration
        {
            Cell = cellIdx,
            Position = pos
        };
    }

    /// <summary>
    /// Unregister a unit from the spatial grid.
    /// Called from Unit3D.Die() or QueueFree().
    /// </summary>
    public void UnregisterUnit(Node3D unit)
    {
        if (!IsInstanceValid(unit))
            return;

        ulong instanceId = unit.GetInstanceId();
        if (!_unitRegistry.TryGetValue(instanceId, out var data))
            return;

        int cellIdx = data.Cell;

        // Remove from grid cell
        if (cellIdx >= 0 && cellIdx < _grid.Count)
        {
            _grid[cellIdx].Remove(unit);
        }

        // Remove from registry
        _unitRegistry.Remove(instanceId);
    }

    /// <summary>
    /// Update a unit's position in the grid if it moved significantly.
    /// Returns true if cell changed, false otherwise.
    /// </summary>
    public bool UpdateUnitPosition(Node3D unit)
    {
        if (!IsInstanceValid(unit))
            return false;

        ulong instanceId = unit.GetInstanceId();
        if (!_unitRegistry.TryGetValue(instanceId, out var data))
        {
            // Unit not registered, register it now
            RegisterUnit(unit);
            return true;
        }

        Vector3 oldPos = data.Position;
        Vector3 newPos = unit.GlobalPosition;

        // Check if moved enough to warrant update (squared distance on XZ plane)
        Vector3 delta = newPos - oldPos;
        float distSq = delta.X * delta.X + delta.Z * delta.Z;
        if (distSq < PositionUpdateThresholdSq)
            return false;

        // Calculate new cell
        int oldCell = data.Cell;
        int newCell = GetCellIndex(newPos);

        // Update registry position
        _unitRegistry[instanceId] = new UnitRegistration
        {
            Cell = newCell,
            Position = newPos
        };

        // If cell changed, move unit between cells
        if (oldCell != newCell)
        {
            if (oldCell >= 0 && oldCell < _grid.Count)
            {
                _grid[oldCell].Remove(unit);
            }
            if (newCell >= 0 && newCell < _grid.Count)
            {
                _grid[newCell].Add(unit);
            }
            _statsCellUpdatesThisFrame++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clear all units from the grid (for battle end/scene transitions).
    /// </summary>
    public void Clear()
    {
        foreach (var cell in _grid)
        {
            cell.Clear();
        }
        _unitRegistry.Clear();
    }

    // =========================================================================
    // QUERY API
    // =========================================================================

    /// <summary>
    /// Get all units within a radius (GDScript-compatible overload).
    /// </summary>
    public Godot.Collections.Array<Node3D> GetUnitsInRadius(Vector3 position, float radius)
    {
        return GetUnitsInRadius(position, radius, -1, null);
    }

    /// <summary>
    /// Get all units within a radius (GDScript-compatible overload).
    /// </summary>
    public Godot.Collections.Array<Node3D> GetUnitsInRadius(Vector3 position, float radius, int teamFilter)
    {
        return GetUnitsInRadius(position, radius, teamFilter, null);
    }

    /// <summary>
    /// Get all units within a radius of a position.
    /// </summary>
    /// <param name="position">Center of the query circle</param>
    /// <param name="radius">Query radius in world units</param>
    /// <param name="teamFilter">Team to filter by (-1 = all teams, 0 = player, 1 = enemy)</param>
    /// <param name="excludeUnit">Optional unit to exclude from results (typically self)</param>
    /// <returns>Array of Node3D references</returns>
    public Godot.Collections.Array<Node3D> GetUnitsInRadius(
        Vector3 position,
        float radius,
        int teamFilter,
        Node3D? excludeUnit)
    {
        _statsQueriesThisFrame++;
        var result = new Godot.Collections.Array<Node3D>();
        float radiusSq = radius * radius;

        // Get all cells that could contain units within radius
        var cells = GetCellsInRadius(position, radius);

        foreach (int cellIdx in cells)
        {
            if (cellIdx < 0 || cellIdx >= _grid.Count)
                continue;

            var cell = _grid[cellIdx];
            foreach (var unit in cell)
            {
                // Skip invalid/freed units
                if (!IsInstanceValid(unit))
                    continue;

                // Skip excluded unit
                if (unit == excludeUnit)
                    continue;

                // Check if unit is alive
                if (unit is IDamageable damageable)
                {
                    if (!damageable.IsAlive)
                        continue;
                }
                else
                {
                    // GDScript interop: check IsAlive (PascalCase) or is_alive (snake_case)
                    var aliveVar = unit.Get("IsAlive");
                    if (aliveVar.VariantType == Variant.Type.Nil)
                        aliveVar = unit.Get("is_alive");
                    if (aliveVar.VariantType != Variant.Type.Nil && !aliveVar.AsBool())
                        continue;
                }

                // Team filter
                if (teamFilter >= 0)
                {
                    int unitTeam = -1;
                    if (unit is Unit3D unit3D)
                    {
                        unitTeam = unit3D.Team;
                    }
                    else
                    {
                        // GDScript interop: check Team (PascalCase) or team (snake_case)
                        var teamVar = unit.Get("Team");
                        if (teamVar.VariantType == Variant.Type.Nil)
                            teamVar = unit.Get("team");
                        if (teamVar.VariantType != Variant.Type.Nil)
                        {
                            unitTeam = teamVar.AsInt32();
                        }
                    }

                    if (unitTeam != teamFilter)
                        continue;
                }

                // Distance check (2D on XZ plane)
                Vector3 delta = unit.GlobalPosition - position;
                float distSq = delta.X * delta.X + delta.Z * delta.Z;
                if (distSq <= radiusSq)
                {
                    result.Add(unit);
                }
            }
        }

        return result;
    }

    // =========================================================================
    // COORDINATE HELPERS
    // =========================================================================

    private static int WorldToCellX(float worldX)
    {
        return (int)((worldX - GridMinX) / CellSize);
    }

    private static int WorldToCellZ(float worldZ)
    {
        return (int)((worldZ - GridMinZ) / CellSize);
    }

    private static int GetCellIndex(Vector3 position)
    {
        int cellX = WorldToCellX(position.X);
        int cellZ = WorldToCellZ(position.Z);

        // Clamp to grid bounds
        cellX = Mathf.Clamp(cellX, 0, GridWidth - 1);
        cellZ = Mathf.Clamp(cellZ, 0, GridHeight - 1);

        return cellZ * GridWidth + cellX;
    }

    private static List<int> GetCellsInRadius(Vector3 position, float radius)
    {
        var cells = new List<int>();

        // Calculate bounding box of query circle
        float minX = position.X - radius;
        float maxX = position.X + radius;
        float minZ = position.Z - radius;
        float maxZ = position.Z + radius;

        // Convert to cell indices
        int startX = WorldToCellX(minX);
        int endX = WorldToCellX(maxX);
        int startZ = WorldToCellZ(minZ);
        int endZ = WorldToCellZ(maxZ);

        // Clamp to grid bounds
        startX = Mathf.Clamp(startX, 0, GridWidth - 1);
        endX = Mathf.Clamp(endX, 0, GridWidth - 1);
        startZ = Mathf.Clamp(startZ, 0, GridHeight - 1);
        endZ = Mathf.Clamp(endZ, 0, GridHeight - 1);

        // Collect all cells in range
        for (int z = startZ; z <= endZ; z++)
        {
            for (int x = startX; x <= endX; x++)
            {
                cells.Add(z * GridWidth + x);
            }
        }

        return cells;
    }

    // =========================================================================
    // STATISTICS (for profiling)
    // =========================================================================

    /// <summary>
    /// Get debug statistics dictionary.
    /// </summary>
    public Godot.Collections.Dictionary GetStats()
    {
        int totalUnits = 0;
        int maxCellPopulation = 0;
        int nonEmptyCells = 0;

        foreach (var cell in _grid)
        {
            int size = cell.Count;
            totalUnits += size;
            if (size > 0)
                nonEmptyCells++;
            if (size > maxCellPopulation)
                maxCellPopulation = size;
        }

        return new Godot.Collections.Dictionary
        {
            { "total_units", totalUnits },
            { "registered_units", _unitRegistry.Count },
            { "non_empty_cells", nonEmptyCells },
            { "max_cell_population", maxCellPopulation },
            { "queries_this_frame", _statsQueriesThisFrame },
            { "cell_updates_this_frame", _statsCellUpdatesThisFrame }
        };
    }

    // =========================================================================
    // DEBUG API (for FPS Test Tool)
    // =========================================================================

    private bool _debugEnabled;

    public bool IsDebugEnabled() => _debugEnabled;

    public void ToggleDebug()
    {
        _debugEnabled = !_debugEnabled;
        GD.Print($"[SpatialGrid] Debug visualization {(_debugEnabled ? "enabled" : "disabled")}");
        // Note: Full debug visualization can be added later if needed
    }
}
