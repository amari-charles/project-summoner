using System.Collections.Generic;
using Godot;
using ProjectSummoner.Constants;

namespace ProjectSummoner.Summons;

/// <summary>
/// Calculates safe spawn positions for units.
/// Uses ring search algorithm to find positions that don't overlap with existing units.
/// </summary>
public static class SpawnPositionCalculator
{
    // Ring search constants for finding non-overlapping spawn positions.
    // MinUnitSpacing: Distance between ring search circles. 1.5 units provides enough
    // separation for most unit sizes while keeping search area compact.
    private const float MinUnitSpacing = 1.5f;

    // SearchAttemptsPerRing: Positions tested per ring (8 = every 45 degrees).
    // Provides good coverage without excessive checks.
    private const int SearchAttemptsPerRing = 8;

    // SearchRings: Number of expanding rings to search (3 rings = up to 4.5 units from desired).
    // Balances finding valid positions vs performance.
    private const int SearchRings = 3;

    // Default collision radius when none specified.
    // 0.5 units is a reasonable default for standard-sized units.
    private const float DefaultCollisionRadius = 0.5f;

    /// <summary>
    /// Calculate safe spawn positions for all units in a formation.
    /// Positions are calculated against current scene state and each other
    /// to ensure preview matches actual spawn positions.
    /// </summary>
    /// <param name="formation">Formation to use for offsets</param>
    /// <param name="centerPosition">Center position for the formation</param>
    /// <param name="spawnCount">Number of units to spawn</param>
    /// <param name="tree">Scene tree for finding existing units</param>
    /// <param name="collisionRadius">Collision radius of units being spawned</param>
    /// <returns>List of safe spawn positions for each unit</returns>
    public static List<Vector3> CalculateFormationPositions(
        Cards.Formations.IFormationStrategy formation,
        Vector3 centerPosition,
        int spawnCount,
        SceneTree? tree,
        float collisionRadius)
    {
        var positions = new List<Vector3>();

        if (collisionRadius <= 0)
            collisionRadius = DefaultCollisionRadius;

        for (int i = 0; i < spawnCount; i++)
        {
            var offset = formation.GetOffset(i, spawnCount);
            var desiredPos = centerPosition + offset;
            var safePos = FindSafeSpawnPosition(desiredPos, tree, collisionRadius, null, positions);
            positions.Add(safePos);
        }

        return positions;
    }

    /// <summary>
    /// Find a safe spawn position that doesn't overlap with existing units
    /// or other positions in the same spawn batch.
    /// Uses ring search algorithm: checks outward in expanding circles.
    /// </summary>
    /// <param name="desiredPos">The desired position to spawn at</param>
    /// <param name="tree">Scene tree for finding existing units</param>
    /// <param name="collisionRadius">Collision radius of unit being spawned</param>
    /// <param name="excludeUnit">Unit to exclude from collision checks (for repositioning)</param>
    /// <param name="batchPositions">Other positions already calculated in this batch</param>
    /// <returns>A safe spawn position (may be the original if no conflicts)</returns>
    public static Vector3 FindSafeSpawnPosition(
        Vector3 desiredPos,
        SceneTree? tree,
        float collisionRadius,
        Node3D? excludeUnit = null,
        List<Vector3>? batchPositions = null)
    {
        // Get all existing units
        var units = tree?.GetNodesInGroup(GroupIDs.Units);

        // Check if desired position is safe first
        if (IsSpawnPositionSafe(desiredPos, units, collisionRadius, excludeUnit, batchPositions))
            return desiredPos;

        // Search in expanding rings around desired position
        for (int ring = 1; ring <= SearchRings; ring++)
        {
            float radius = MinUnitSpacing * ring;

            for (int attempt = 0; attempt < SearchAttemptsPerRing; attempt++)
            {
                float angle = (float)attempt / SearchAttemptsPerRing * Mathf.Tau;
                var offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                var testPos = desiredPos + offset;

                if (IsSpawnPositionSafe(testPos, units, collisionRadius, excludeUnit, batchPositions))
                    return testPos;
            }
        }

        // Fallback: no safe position found, use desired (units will overlap but game continues)
        return desiredPos;
    }

    /// <summary>
    /// Check if a spawn position is safe (no overlap with existing units or batch positions).
    /// </summary>
    /// <param name="checkPos">Position to check</param>
    /// <param name="units">Existing units in the scene</param>
    /// <param name="collisionRadius">Collision radius of unit being spawned</param>
    /// <param name="excludeUnit">Unit to exclude from collision checks</param>
    /// <param name="batchPositions">Other positions already calculated in this batch</param>
    /// <returns>True if position is safe, false if it overlaps</returns>
    public static bool IsSpawnPositionSafe(
        Vector3 checkPos,
        Godot.Collections.Array<Node>? units,
        float collisionRadius,
        Node3D? excludeUnit = null,
        List<Vector3>? batchPositions = null)
    {
        // Check against existing units in scene
        if (units != null)
        {
            foreach (var node in units)
            {
                if (node == excludeUnit)
                    continue;

                if (node is not Node3D otherUnit)
                    continue;

                // Check if unit is alive (duck typing)
                var isAliveVar = otherUnit.Get("is_alive");
                if (isAliveVar.VariantType == Variant.Type.Bool && !isAliveVar.AsBool())
                    continue;

                // Get collision radius
                var otherRadiusVar = otherUnit.Get("collision_radius");
                float otherRadius = otherRadiusVar.VariantType != Variant.Type.Nil ? otherRadiusVar.AsSingle() : DefaultCollisionRadius;

                // Calculate 2D distance (ignore Y)
                float dx = checkPos.X - otherUnit.GlobalPosition.X;
                float dz = checkPos.Z - otherUnit.GlobalPosition.Z;
                float distSq = dx * dx + dz * dz;

                // Minimum spacing is sum of both collision radii
                float minSpacing = collisionRadius + otherRadius;
                if (distSq < minSpacing * minSpacing)
                    return false;
            }
        }

        // Check against other positions in the same spawn batch
        if (batchPositions != null)
        {
            foreach (var otherPos in batchPositions)
            {
                float dx = checkPos.X - otherPos.X;
                float dz = checkPos.Z - otherPos.Z;
                float distSq = dx * dx + dz * dz;

                // Same collision radius for units in same batch
                float minSpacing = collisionRadius * 2;
                if (distSq < minSpacing * minSpacing)
                    return false;
            }
        }

        return true;
    }
}
