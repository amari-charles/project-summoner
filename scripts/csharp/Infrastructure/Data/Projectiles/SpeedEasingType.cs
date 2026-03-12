namespace Fateforged.Projectiles;

/// <summary>
/// Defines easing curves for projectile speed transitions.
/// </summary>
public enum SpeedEasingType
{
    /// <summary>
    /// Linear interpolation: t
    /// </summary>
    Linear,

    /// <summary>
    /// Ease in (slow start, fast end): t^exponent
    /// </summary>
    EaseIn,

    /// <summary>
    /// Ease out (fast start, slow end): 1 - (1-t)^exponent
    /// </summary>
    EaseOut,

    /// <summary>
    /// Smooth S-curve using sine: (1 - cos(t * PI)) / 2
    /// </summary>
    EaseInOut,
}
