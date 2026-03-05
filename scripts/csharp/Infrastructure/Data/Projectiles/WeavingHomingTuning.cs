namespace Fateforged.Projectiles;

/// <summary>
/// Shared tuning constants for weaving-homing projectile movement.
/// Keep client visual simulation and deterministic simulation in sync.
/// </summary>
public static class WeavingHomingTuning
{
    public const float VeerReferenceDistance = 10f;
    public const float VeerMinDistance = 3f;
    public const float VeerPitchRatio = 0.45f;
    public const float VeerCounterYawRatio = 0.85f;
    public const float VeerCounterPitchRatio = 0.6f;
    public const float CounterVeerDurationRatio = 0.85f;

    public const float BlendOutTargetWeight = 0.06f;
    public const float BlendBackTargetWeight = 0.12f;

    public const float HomingWeaveSettleDistance = 10f;
    public const float HomingWeaveYawRatio = 0.55f;
    public const float HomingWeavePitchRatio = 0.28f;
    public const float HomingWeaveFrequency = 8.5f;
    public const float HomingWeavePitchFrequency = 6.2f;
    public const float HomingFarSteerScale = 0.38f;
    public const float HomingFinalLockDistance = 6f;
    public const float HomingFinalLockTime = 0.55f;

    public const float HomingYawFromDurationScale = 220f;
    public const float HomingYawFallbackMinDegrees = 22f;
    public const float HomingYawFallbackArcMultiplier = 16f;
}
