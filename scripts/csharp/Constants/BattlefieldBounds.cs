namespace ProjectSummoner.Constants;

using System;
using Godot;
using Fateforged.Simulation;
using ProjectSummoner.Systems;

/// <summary>
/// Battlefield boundary constants and utility methods.
/// C# equivalent of battlefield_constants.gd.
///
/// Usage:
///   var clamped = BattlefieldBounds.ClampToBounds(position);
///   if (BattlefieldBounds.IsValidSpawnPositionForTeam(pos, team)) { ... }
/// </summary>
public static class BattlefieldBounds
{
    // =========================================================================
    // BATTLEFIELD EXTENTS
    // =========================================================================

    /// <summary>Half the X-axis extent. Battlefield spans -HalfWidth to +HalfWidth.</summary>
    public const float HalfWidth = 50.0f;

    /// <summary>Half the Z-axis extent. Battlefield spans -HalfDepth to +HalfDepth.</summary>
    public const float HalfDepth = 40.0f;

    /// <summary>Minimum X coordinate (left edge)</summary>
    public const float MinX = -HalfWidth;

    /// <summary>Maximum X coordinate (right edge)</summary>
    public const float MaxX = HalfWidth;

    /// <summary>Minimum Z coordinate (front edge)</summary>
    public const float MinZ = -HalfDepth;

    /// <summary>Maximum Z coordinate (back edge)</summary>
    public const float MaxZ = HalfDepth;

    // =========================================================================
    // SPAWN ZONE BOUNDARIES
    // =========================================================================

    /// <summary>X boundary between player and enemy spawn zones. Player: X &lt;= 0, Enemy: X &gt; 0</summary>
    public const float SpawnBoundaryX = 0.0f;

    /// <summary>Small offset for enemy spawn boundary (X must be strictly greater than 0)</summary>
    public const float SpawnBoundaryEpsilon = 0.001f;

    // =========================================================================
    // BOUNDARY UTILITY METHODS
    // =========================================================================

    /// <summary>
    /// Clamp a position to battlefield boundaries (XZ plane only).
    /// Y coordinate is preserved unchanged.
    /// </summary>
    /// <param name="position">Position to clamp</param>
    /// <returns>Position clamped to battlefield bounds</returns>
    public static Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.X, MinX, MaxX),
            position.Y,
            Mathf.Clamp(position.Z, MinZ, MaxZ)
        );
    }

    /// <summary>
    /// Clamp a SimVector3 position to battlefield boundaries (XZ plane only).
    /// Simulation-layer overload — no Godot dependency at the call site.
    /// </summary>
    public static SimVector3 ClampToBounds(SimVector3 position)
    {
        return new SimVector3(
            Math.Clamp(position.X, MinX, MaxX),
            position.Y,
            Math.Clamp(position.Z, MinZ, MaxZ)
        );
    }

    /// <summary>
    /// Check if a position is within battlefield boundaries.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if position is within bounds</returns>
    public static bool IsInBounds(Vector3 position)
    {
        return position.X >= MinX && position.X <= MaxX
            && position.Z >= MinZ && position.Z <= MaxZ;
    }

    // =========================================================================
    // SPAWN ZONE UTILITY METHODS
    // =========================================================================

    /// <summary>
    /// Check if a position is valid for spawning for the given team.
    /// Player (team 0): X &lt;= 0, Enemy (team 1): X &gt; 0
    /// Respects debug bypass flag from SpatialGrid.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>True if position is in valid spawn zone for team</returns>
    public static bool IsValidSpawnPositionForTeam(Vector3 position, int team)
    {
        // Debug bypass - all positions are valid (flag stored in SpatialGrid for GDScript access)
        if (SpatialGrid.IsDebugBypassSpawnBoundaryEnabled())
            return true;

        if (team == 0) // Player
            return position.X <= SpawnBoundaryX;
        else // Enemy
            return position.X > SpawnBoundaryX;
    }

    /// <summary>
    /// Clamp a position to a fully valid spawn zone for the given team.
    /// Applies both outer battlefield bounds AND team spawn boundary.
    /// Use this when you need a guaranteed valid spawn position.
    /// Respects debug bypass flag (only applies outer bounds when bypassed).
    /// </summary>
    /// <param name="position">Position to clamp</param>
    /// <param name="team">0 = Player, 1 = Enemy</param>
    /// <returns>Position clamped to valid spawn zone (within bounds and on correct team side)</returns>
    public static Vector3 ClampToValidSpawnZone(Vector3 position, int team)
    {
        // Apply outer battlefield bounds first
        var clamped = ClampToBounds(position);

        // Debug bypass - skip team boundary enforcement (flag stored in SpatialGrid for GDScript access)
        if (SpatialGrid.IsDebugBypassSpawnBoundaryEnabled())
            return clamped;

        // Then apply team spawn boundary
        if (team == 0 && clamped.X > SpawnBoundaryX) // Player on wrong side
        {
            clamped.X = SpawnBoundaryX;
        }
        else if (team != 0 && clamped.X <= SpawnBoundaryX) // Enemy on wrong side
        {
            clamped.X = SpawnBoundaryX + SpawnBoundaryEpsilon;
        }

        return clamped;
    }
}
