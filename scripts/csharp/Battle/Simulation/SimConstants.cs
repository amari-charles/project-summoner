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
    /// Default is immediate cleanup; view layer owns death presentation timing.
    /// </summary>
    public const float DeathCleanupSeconds = 0f;

    // =========================================================================
    // OBJECTIVE ADVANCE
    // =========================================================================

    /// <summary>
    /// Progress fraction (0..1) along own-summoner -> enemy-summoner axis where
    /// objective-advance steering begins curving toward the enemy summoner.
    /// </summary>
    public const float ObjectiveAdvanceBandStartProgress = 0.70f;

    /// <summary>
    /// Exponent for objective-advance blend ramp after band start.
    /// 2.0 = gentle progressive curve.
    /// </summary>
    public const float ObjectiveAdvanceCurveExponent = 2.0f;
}
