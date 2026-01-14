using Godot;

namespace ProjectSummoner.Visual;

/// <summary>
/// Simple blob shadow for 2.5D units.
/// Uses a QuadMesh with transparent gradient texture.
/// Works with any ground material (doesn't require StandardMaterial3D like Decals).
/// </summary>
[GlobalClass]
public partial class ShadowComponent : MeshInstance3D
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    // Y offset above ground to prevent z-fighting
    private const float GroundOffset = 0.01f;

    // =========================================================================
    // EXPORTED PROPERTIES
    // =========================================================================

    [Export]
    public float ShadowRadius { get; set; } = 1.0f;

    [Export]
    public float ShadowOpacity { get; set; } = 0.6f;

    // =========================================================================
    // PRIVATE STATE
    // =========================================================================

    private ImageTexture? _shadowTexture;
    private StandardMaterial3D? _material;
    private float _baseShadowRadius;
    private float _baseShadowOpacity;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Initialize the shadow with specified radius and opacity.
    /// Must be called after adding to scene tree.
    /// </summary>
    public void Initialize(float radius, float opacity)
    {
        ShadowRadius = radius;
        ShadowOpacity = opacity;
        _baseShadowRadius = radius;
        _baseShadowOpacity = opacity;

        // Create quad mesh
        var quad = new QuadMesh();
        quad.Size = new Vector2(ShadowRadius, ShadowRadius);
        Mesh = quad;

        // Orient flat on ground (rotate -90° around X axis)
        RotationDegrees = new Vector3(-90, 0, 0);

        // Position just above ground (prevent z-fighting)
        Position = new Vector3(0, GroundOffset, 0);

        // Create radial gradient texture with opacity baked in
        _shadowTexture = CreateRadialGradientTexture(ShadowOpacity);

        // Create material with multiply blend for non-stacking shadows
        _material = new StandardMaterial3D();
        _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _material.BlendMode = BaseMaterial3D.BlendModeEnum.Mul;  // Multiply: overlapping shadows don't compound
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _material.NoDepthTest = true;       // Don't depth-test against other objects
        _material.RenderPriority = -100;    // Render shadows first (before units)
        _material.AlbedoTexture = _shadowTexture;
        // Don't tint with AlbedoColor - opacity is baked into texture to keep corners white
        _material.AlbedoColor = Colors.White;

        SetSurfaceOverrideMaterial(0, _material);

        // Rendering settings
        CastShadow = ShadowCastingSetting.Off;
        GIMode = GIModeEnum.Disabled;
        Visible = true;
        Layers = 1;
    }

    /// <summary>
    /// Update shadow radius at runtime.
    /// </summary>
    public void SetShadowRadius(float radius)
    {
        // Early out if radius hasn't changed significantly
        const float RadiusTolerance = 0.01f;
        if (Mathf.Abs(ShadowRadius - radius) < RadiusTolerance)
            return;

        ShadowRadius = radius;
        if (Mesh is QuadMesh quadMesh)
        {
            quadMesh.Size = new Vector2(ShadowRadius, ShadowRadius);
        }
    }

    /// <summary>
    /// Update shadow opacity at runtime.
    /// Regenerates the texture to bake new opacity value, keeping corners white.
    /// Only regenerates if opacity changed significantly (avoids expensive texture generation).
    /// </summary>
    public void SetShadowOpacity(float opacity)
    {
        // Early out if opacity hasn't changed significantly (avoid regenerating 128x128 texture every frame)
        const float OpacityTolerance = 0.01f;
        if (Mathf.Abs(ShadowOpacity - opacity) < OpacityTolerance)
            return;

        ShadowOpacity = opacity;
        if (_material != null)
        {
            // Regenerate texture with new opacity baked in
            _shadowTexture = CreateRadialGradientTexture(opacity);
            _material.AlbedoTexture = _shadowTexture;
        }
    }

    /// <summary>
    /// Update shadow for flying unit altitude.
    /// Call this in _PhysicsProcess for dynamic altitude changes.
    /// </summary>
    public void UpdateForAltitude(float altitude, float xOffset = 0f, float zOffset = 0f, float maxAltitude = 10.0f)
    {
        float altitudeFactor = Mathf.Clamp(altitude / maxAltitude, 0.0f, 1.0f);

        // Shadow shrinks and fades with altitude (using base values to avoid cumulative shrinking)
        float sizeScale = 1.0f - (altitudeFactor * 0.4f);      // 60% size at max altitude
        float opacityScale = 1.0f - (altitudeFactor * 0.6f);   // 40% opacity at max altitude

        SetShadowRadius(_baseShadowRadius * sizeScale);
        SetShadowOpacity(_baseShadowOpacity * opacityScale);

        // Keep shadow on ground with XZ offset preserved
        Position = new Vector3(xOffset, -altitude + GroundOffset, zOffset);
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Create a radial gradient texture for multiply-blend shadows.
    /// Opacity is baked directly into the texture to avoid square artifacts from AlbedoColor tinting.
    /// For multiply: darker center (darkens background), white edge (no effect).
    /// </summary>
    private static ImageTexture CreateRadialGradientTexture(float opacity)
    {
        const int sizePx = 128;
        var image = Image.CreateEmpty(sizePx, sizePx, false, Image.Format.Rgba8);

        var center = new Vector2(sizePx / 2.0f, sizePx / 2.0f);
        float maxRadius = sizePx / 2.0f;

        // Calculate center darkness based on opacity (0 = white/no shadow, 1 = black/full shadow)
        float centerDarkness = opacity * 0.7f;  // Scale factor for visual appearance

        for (int y = 0; y < sizePx; y++)
        {
            for (int x = 0; x < sizePx; x++)
            {
                var pos = new Vector2(x, y);
                float dist = pos.DistanceTo(center);

                // Normalize distance (0 at center, 1 at edge)
                float normalizedDist = dist / maxRadius;

                // Outside the circle should be fully white (no effect in multiply blend)
                if (normalizedDist > 1.0f)
                {
                    image.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, 1.0f));
                    continue;
                }

                // Create soft falloff with smoothstep (0 at center, 1 at edge)
                float falloff = Smoothstep(0.0f, 1.0f, normalizedDist);

                // Interpolate from dark center to white edge
                // Center: 1.0 - centerDarkness (e.g., 0.58 for 0.6 opacity)
                // Edge: 1.0 (white, no effect)
                float brightness = Mathf.Lerp(1.0f - centerDarkness, 1.0f, falloff);

                // Set pixel with gradient in RGB, full alpha for multiply blend
                image.SetPixel(x, y, new Color(brightness, brightness, brightness, 1.0f));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Smoothstep interpolation (GLSL-style).
    /// </summary>
    private static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }
}
