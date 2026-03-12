using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Immutable shadow configuration used by visual components and ShadowHelper.
/// </summary>
public readonly record struct ShadowProfile(
    float Opacity,
    float SkewX,
    float SkewY,
    float OffsetX,
    float OffsetZ,
    float GroundClearance,
    float ScaleY,
    int RenderPriority
)
{
    public ShadowProfile Sanitize()
    {
        return new ShadowProfile(
            Mathf.Clamp(Opacity, 0.0f, 1.0f),
            Mathf.Clamp(SkewX, -3.0f, 3.0f),
            Mathf.Clamp(SkewY, -3.0f, 3.0f),
            OffsetX,
            OffsetZ,
            Mathf.Max(GroundClearance, 0.0f),
            Mathf.Max(ScaleY, 0.01f),
            Mathf.Clamp(RenderPriority, -128, 127)
        );
    }
}

/// <summary>
/// Built-in shadow profile presets for quick tuning without per-property overrides.
/// </summary>
public enum ShadowProfilePreset
{
    Default = 0,
    Soft = 1,
    Dramatic = 2,
}

/// <summary>
/// Factory for profile presets.
/// </summary>
public static class ShadowProfiles
{
    public static ShadowProfile FromPreset(ShadowProfilePreset preset)
    {
        return preset switch
        {
            ShadowProfilePreset.Soft => new ShadowProfile(
                Opacity: 0.40f,
                SkewX: 0.65f,
                SkewY: 0.50f,
                OffsetX: 0.15f,
                OffsetZ: 0.15f,
                GroundClearance: 0.01f,
                ScaleY: 1.05f,
                RenderPriority: -100
            ),
            ShadowProfilePreset.Dramatic => new ShadowProfile(
                Opacity: 0.62f,
                SkewX: 1.00f,
                SkewY: 0.75f,
                OffsetX: 0.26f,
                OffsetZ: 0.26f,
                GroundClearance: 0.01f,
                ScaleY: 1.15f,
                RenderPriority: -100
            ),
            _ => new ShadowProfile(
                Opacity: 0.50f,
                SkewX: 0.80f,
                SkewY: 0.60f,
                OffsetX: 0.20f,
                OffsetZ: 0.20f,
                GroundClearance: 0.01f,
                ScaleY: 1.10f,
                RenderPriority: -100
            ),
        };
    }
}
