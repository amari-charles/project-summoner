using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Editor-configurable shadow profile override.
/// Attach as a Resource to per-unit visual components when custom shadow tuning is needed.
/// </summary>
[GlobalClass]
public partial class ShadowProfileResource : Resource
{
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float Opacity { get; set; } = 0.5f;

    [Export(PropertyHint.Range, "-3.0,3.0,0.01")]
    public float SkewX { get; set; } = 0.8f;

    [Export(PropertyHint.Range, "-3.0,3.0,0.01")]
    public float SkewY { get; set; } = 0.6f;

    [Export]
    public float OffsetX { get; set; } = 0.2f;

    [Export]
    public float OffsetZ { get; set; } = 0.2f;

    [Export(PropertyHint.Range, "0.0,0.1,0.001")]
    public float GroundClearance { get; set; } = 0.01f;

    [Export(PropertyHint.Range, "0.01,3.0,0.01")]
    public float ScaleY { get; set; } = 1.1f;

    [Export(PropertyHint.Range, "-128,127,1")]
    public int RenderPriority { get; set; } = -100;

    public ShadowProfile ToShadowProfile()
    {
        return new ShadowProfile(
            Opacity,
            SkewX,
            SkewY,
            OffsetX,
            OffsetZ,
            GroundClearance,
            ScaleY,
            RenderPriority
        ).Sanitize();
    }
}
