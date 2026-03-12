namespace Fateforged.Constants;

using System;
using Fateforged.Simulation;
using Godot;

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
    public const float HalfDepth = 25.0f;

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
    // DEBUG BYPASS (formerly in SpatialGrid)
    // =========================================================================

    private static bool _debugBypassSpawnBoundary;

    public static bool IsDebugBypassSpawnBoundaryEnabled() => _debugBypassSpawnBoundary;

    public static void SetDebugBypassSpawnBoundary(bool enabled) =>
        _debugBypassSpawnBoundary = enabled;

    public static void ToggleDebugBypassSpawnBoundary() =>
        _debugBypassSpawnBoundary = !_debugBypassSpawnBoundary;

    // =========================================================================
    // BOUNDARY UTILITY METHODS
    // =========================================================================

    /// <summary>
    /// Clamp a position to battlefield boundaries (XZ plane only).
    /// Y coordinate is preserved unchanged.
    /// </summary>
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
    public static bool IsInBounds(Vector3 position)
    {
        return position.X >= MinX && position.X <= MaxX && position.Z >= MinZ && position.Z <= MaxZ;
    }

    /// <summary>
    /// Simulation-layer overload for bounds check (no Godot Vector dependency).
    /// </summary>
    public static bool IsInBounds(SimVector3 position)
    {
        return position.X >= MinX && position.X <= MaxX && position.Z >= MinZ && position.Z <= MaxZ;
    }

    // =========================================================================
    // SPAWN ZONE UTILITY METHODS
    // =========================================================================

    /// <summary>
    /// Check if a position is valid for spawning for the given team.
    /// Player (team 0): X &lt;= 0, Enemy (team 1): X &gt; 0
    /// </summary>
    public static bool IsValidSpawnPositionForTeam(Vector3 position, int team)
    {
        if (_debugBypassSpawnBoundary)
            return true;

        if (team == 0) // Player
            return position.X <= SpawnBoundaryX;
        else // Enemy
            return position.X > SpawnBoundaryX;
    }

    /// <summary>
    /// Simulation-layer overload for spawn-side validation.
    /// Player (team 0): X &lt;= 0, Enemy (team 1): X &gt; 0
    /// </summary>
    public static bool IsValidSpawnPositionForTeam(SimVector3 position, int team)
    {
        if (_debugBypassSpawnBoundary)
            return true;

        if (team == 0) // Player
            return position.X <= SpawnBoundaryX;
        else // Enemy
            return position.X > SpawnBoundaryX;
    }

    /// <summary>
    /// Clamp a position to a fully valid spawn zone for the given team.
    /// Applies both outer battlefield bounds AND team spawn boundary.
    /// </summary>
    public static Vector3 ClampToValidSpawnZone(Vector3 position, int team)
    {
        // Apply outer battlefield bounds first
        var clamped = ClampToBounds(position);

        if (_debugBypassSpawnBoundary)
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
