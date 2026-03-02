namespace Fateforged.Simulation;

/// <summary>
/// Shared constants for the deterministic simulation layer.
/// Centralizes values used across multiple sim files to prevent drift.
/// </summary>
public static class SimConstants
{
    // =========================================================================
    // DEATH CLEANUP
    // =========================================================================

    /// <summary>
    /// Time in seconds between a unit dying and being removed from MatchState.
    /// Allows death animations to play before cleanup.
    /// </summary>
    public const float DeathCleanupSeconds = 2.0f;
}
