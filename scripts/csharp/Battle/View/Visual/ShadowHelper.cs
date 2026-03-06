using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Static helper for creating and updating diagonal silhouette shadows.
/// Used by both SpriteVisualComponent and SkeletalVisualComponent.
/// </summary>
public static class ShadowHelper
{
    // Shadow appearance
    private const float ShadowOpacity = 0.5f;
    private const float SkewX = 0.8f;

    // Positioning: diagonal offset simulating light from upper-left
    private const float DiagonalOffsetX = 0.2f;
    private const float DiagonalOffsetZ = 0.2f;
    private const float GroundClearance = 0.01f;

    // Shadow is slightly stretched along Z to look more natural on the ground
    private const float ShadowScaleY = 1.1f;
    private const int ShadowRenderPriority = -100;

    private static Shader? _shaderCache;

    /// <summary>
    /// Creates and returns a shadow Sprite3D with its ShaderMaterial.
    /// The shadow is added as a child of the calling component.
    /// FlipH is always true to correct for the horizontal mirror caused by the -90° X rotation.
    /// The viewport texture already contains the correctly-oriented content (both visual
    /// components flip their internal 2D content), so no per-unit flip adjustment is needed.
    /// </summary>
    /// <param name="parentSprite3D">The main Sprite3D used for rendering the unit.</param>
    /// <param name="viewport">The SubViewport providing the unit's texture.</param>
    /// <returns>The created shadow Sprite3D and its ShaderMaterial.</returns>
    public static (Sprite3D shadow, ShaderMaterial material)? CreateShadow(
        Sprite3D parentSprite3D, SubViewport viewport)
    {
        _shaderCache ??= GD.Load<Shader>("res://shaders/vfx/silhouette_shadow.gdshader");
        if (_shaderCache == null)
        {
            GD.PushWarning("ShadowHelper: Failed to load silhouette shadow shader");
            return null;
        }

        var material = new ShaderMaterial();
        material.Shader = _shaderCache;
        material.SetShaderParameter("shadow_opacity", ShadowOpacity);
        material.SetShaderParameter("skew_x", SkewX);

        var viewportTexture = viewport.GetTexture();
        material.SetShaderParameter("sprite_texture", viewportTexture);

        // _sprite3D.Position.Y is the vertical offset that centers the sprite above feet.
        // Shadow is rotated -90° on X, so local Y maps to world Z.
        // We must use that offset as the shadow's Z base so it aligns with the sprite body.
        float spriteCenterZ = parentSprite3D.Position.Y;

        var shadow = new Sprite3D();
        shadow.Texture = viewportTexture;
        shadow.PixelSize = parentSprite3D.PixelSize;
        shadow.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        shadow.RotationDegrees = new Vector3(-90, 0, 0);
        shadow.FlipH = true;
        shadow.FlipV = true;
        shadow.RenderPriority = ShadowRenderPriority;
        shadow.MaterialOverride = material;
        shadow.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        shadow.GIMode = GeometryInstance3D.GIModeEnum.Disabled;

        // Diagonal shadow: base Z aligns with sprite center, offset right and away from camera
        shadow.Position = new Vector3(DiagonalOffsetX, GroundClearance, spriteCenterZ + DiagonalOffsetZ);
        shadow.Scale = new Vector3(1.0f, ShadowScaleY, 1.0f);

        return (shadow, material);
    }

    /// <summary>
    /// Pin shadow to ground plane regardless of unit elevation.
    /// Call from _Process each frame.
    /// </summary>
    /// <param name="shadow">The shadow Sprite3D.</param>
    /// <param name="parentGlobalY">The GlobalPosition.Y of the owning component.</param>
    public static void PinToGround(Sprite3D shadow, float parentGlobalY)
    {
        var pos = shadow.Position;
        pos.Y = GroundClearance - parentGlobalY;
        shadow.Position = pos;
    }
}
