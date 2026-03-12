using Godot;

namespace Fateforged.Meta;

/// <summary>
/// Settings for HP bar appearance and behavior.
/// Immutable struct - create new instances via With* methods or factory methods.
/// </summary>
public readonly struct HPBarSettings
{
    // Default dimensions in world units (sized for typical unit sprites)
    public const float DefaultBarWidth = 0.8f;
    public const float DefaultBarHeight = 0.08f;

    // Vertical offset above unit origin (accounts for average unit sprite height + padding)
    public const float DefaultOffsetY = 3.2f;

    // Z offset toward camera (negative Z = closer to camera)
    public const float DefaultOffsetZ = -0.5f;

    // Timing for fade behavior (in seconds)
    public const float DefaultFadeDelay = 3.0f;
    public const float DefaultFadeDuration = 0.5f;

    // Default color thresholds
    public const float DefaultThresholdMid = 0.5f;
    public const float DefaultThresholdLow = 0.25f;

    // HP drain animation speed (percent per second)
    public const float DefaultAnimationSpeed = 2.0f;

    // Dimensions
    public float BarWidth { get; init; }
    public float BarHeight { get; init; }
    public float OffsetY { get; init; }
    public float OffsetZ { get; init; }

    // Behavior
    public bool ShowOnDamageOnly { get; init; }
    public float FadeDelay { get; init; }
    public float FadeDuration { get; init; }

    // Color thresholds (configurable per bar type)
    public float ThresholdMid { get; init; }
    public float ThresholdLow { get; init; }

    // Animation
    public float AnimationSpeed { get; init; }

    // Colors (optional overrides - null uses shader defaults)
    public Color? ColorFull { get; init; }
    public Color? ColorMid { get; init; }
    public Color? ColorLow { get; init; }
    public Color? ColorBackground { get; init; }

    public static HPBarSettings Default =>
        new()
        {
            BarWidth = DefaultBarWidth,
            BarHeight = DefaultBarHeight,
            OffsetY = DefaultOffsetY,
            OffsetZ = DefaultOffsetZ,
            ShowOnDamageOnly = true,
            FadeDelay = DefaultFadeDelay,
            FadeDuration = DefaultFadeDuration,
            ThresholdMid = DefaultThresholdMid,
            ThresholdLow = DefaultThresholdLow,
            AnimationSpeed = DefaultAnimationSpeed,
            ColorFull = null,
            ColorMid = null,
            ColorLow = null,
            ColorBackground = null,
        };

    public static HPBarSettings AlwaysVisible =>
        new()
        {
            BarWidth = DefaultBarWidth,
            BarHeight = DefaultBarHeight,
            OffsetY = DefaultOffsetY,
            OffsetZ = DefaultOffsetZ,
            ShowOnDamageOnly = false,
            FadeDelay = DefaultFadeDelay,
            FadeDuration = DefaultFadeDuration,
            ThresholdMid = DefaultThresholdMid,
            ThresholdLow = DefaultThresholdLow,
            AnimationSpeed = DefaultAnimationSpeed,
            ColorFull = null,
            ColorMid = null,
            ColorLow = null,
            ColorBackground = null,
        };

    /// <summary>
    /// Create boss-style settings with lower thresholds (yellow at 25%, red at 10%).
    /// </summary>
    public static HPBarSettings Boss =>
        new()
        {
            BarWidth = 1.2f,
            BarHeight = 0.12f,
            OffsetY = DefaultOffsetY,
            OffsetZ = DefaultOffsetZ,
            ShowOnDamageOnly = false,
            FadeDelay = DefaultFadeDelay,
            FadeDuration = DefaultFadeDuration,
            ThresholdMid = 0.25f,
            ThresholdLow = 0.1f,
            AnimationSpeed = DefaultAnimationSpeed,
            ColorFull = null,
            ColorMid = null,
            ColorLow = null,
            ColorBackground = null,
        };

    /// <summary>
    /// Create settings with custom color thresholds.
    /// </summary>
    public HPBarSettings WithThresholds(float mid, float low) =>
        this with
        {
            ThresholdMid = mid,
            ThresholdLow = low,
        };

    /// <summary>
    /// Create settings with custom colors.
    /// </summary>
    public HPBarSettings WithColors(Color full, Color mid, Color low) =>
        this with
        {
            ColorFull = full,
            ColorMid = mid,
            ColorLow = low,
        };

    /// <summary>
    /// Create settings with custom size.
    /// </summary>
    public HPBarSettings WithSize(float width, float height) =>
        this with
        {
            BarWidth = width,
            BarHeight = height,
        };
}
