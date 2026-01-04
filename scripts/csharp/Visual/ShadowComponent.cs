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
        Position = new Vector3(0, 0.01f, 0);

        // Create radial gradient texture
        _shadowTexture = CreateRadialGradientTexture();

        // Create material with proper transparent gradient shadow
        _material = new StandardMaterial3D();
        _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _material.BlendMode = BaseMaterial3D.BlendModeEnum.Mix;
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _material.AlbedoTexture = _shadowTexture;
        _material.AlbedoColor = new Color(0, 0, 0, ShadowOpacity);

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
        ShadowRadius = radius;
        if (Mesh is QuadMesh quadMesh)
        {
            quadMesh.Size = new Vector2(ShadowRadius, ShadowRadius);
        }
    }

    /// <summary>
    /// Update shadow opacity at runtime.
    /// </summary>
    public void SetShadowOpacity(float opacity)
    {
        ShadowOpacity = opacity;
        if (_material != null)
        {
            var color = _material.AlbedoColor;
            color.A = opacity;
            _material.AlbedoColor = color;
        }
    }

    /// <summary>
    /// Update shadow for flying unit altitude.
    /// Call this in _PhysicsProcess for dynamic altitude changes.
    /// </summary>
    public void UpdateForAltitude(float altitude, float maxAltitude = 10.0f)
    {
        float altitudeFactor = Mathf.Clamp(altitude / maxAltitude, 0.0f, 1.0f);

        // Shadow shrinks and fades with altitude (using base values to avoid cumulative shrinking)
        float sizeScale = 1.0f - (altitudeFactor * 0.4f);      // 60% size at max altitude
        float opacityScale = 1.0f - (altitudeFactor * 0.6f);   // 40% opacity at max altitude

        SetShadowRadius(_baseShadowRadius * sizeScale);
        SetShadowOpacity(_baseShadowOpacity * opacityScale);

        // Keep shadow on ground (relative to parent unit position)
        Position = new Vector3(0, -altitude + 0.01f, 0);
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Create a radial gradient texture for the shadow.
    /// </summary>
    private static ImageTexture CreateRadialGradientTexture()
    {
        const int sizePx = 128;
        var image = Image.CreateEmpty(sizePx, sizePx, false, Image.Format.Rgba8);

        var center = new Vector2(sizePx / 2.0f, sizePx / 2.0f);
        float maxRadius = sizePx / 2.0f;

        for (int y = 0; y < sizePx; y++)
        {
            for (int x = 0; x < sizePx; x++)
            {
                var pos = new Vector2(x, y);
                float dist = pos.DistanceTo(center);

                // Normalize distance (0 at center, 1 at edge)
                float normalizedDist = dist / maxRadius;

                // Create soft falloff with smoothstep
                float alpha = 1.0f - Smoothstep(0.0f, 1.0f, normalizedDist);

                // Set pixel (white with varying alpha - color comes from albedo_color)
                image.SetPixel(x, y, new Color(1, 1, 1, alpha));
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
