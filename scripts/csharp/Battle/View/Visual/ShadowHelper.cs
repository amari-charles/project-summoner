using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Static helper for creating and updating diagonal silhouette shadows.
/// Used by both SpriteVisualComponent and SkeletalVisualComponent.
/// </summary>
public static class ShadowHelper
{
    private static Shader? _shaderCache;

    /// <summary>
    /// Creates and returns a shadow Sprite3D.
    /// The caller is responsible for adding the shadow as a child.
    /// FlipH is always true to correct for the horizontal mirror caused by the -90° X rotation.
    /// The viewport texture already contains the correctly-oriented content (both visual
    /// components flip their internal 2D content), so no per-unit flip adjustment is needed.
    /// </summary>
    /// <param name="parentSprite3D">The main Sprite3D used for rendering the unit.</param>
    /// <param name="viewport">The SubViewport providing the unit's texture.</param>
    /// <returns>The created shadow Sprite3D, or null if the shader failed to load.</returns>
    public static Sprite3D? CreateShadow(
        Sprite3D parentSprite3D, SubViewport viewport, ShadowProfile profile)
    {
        ShadowProfile effectiveProfile = profile.Sanitize();
        _shaderCache ??= GD.Load<Shader>("res://shaders/vfx/silhouette_shadow.gdshader");
        if (_shaderCache == null)
        {
            GD.PushWarning("ShadowHelper: Failed to load silhouette shadow shader");
            return null;
        }

        var material = new ShaderMaterial();
        material.Shader = _shaderCache;
        material.SetShaderParameter("shadow_opacity", effectiveProfile.Opacity);
        material.SetShaderParameter("skew_x", effectiveProfile.SkewX);
        material.SetShaderParameter("skew_y", effectiveProfile.SkewY);

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
        shadow.RenderPriority = effectiveProfile.RenderPriority;
        shadow.MaterialOverride = material;
        shadow.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        shadow.GIMode = GeometryInstance3D.GIModeEnum.Disabled;

        // Diagonal shadow: base Z aligns with sprite center, offset right and away from camera
        shadow.Position = new Vector3(
            effectiveProfile.OffsetX,
            effectiveProfile.GroundClearance,
            spriteCenterZ + effectiveProfile.OffsetZ
        );
        shadow.Scale = new Vector3(1.0f, effectiveProfile.ScaleY, 1.0f);

        return shadow;
    }

    /// <summary>
    /// Pin shadow to ground plane regardless of unit elevation.
    /// Call from _Process each frame.
    /// </summary>
    /// <param name="shadow">The shadow Sprite3D.</param>
    /// <param name="parentGlobalY">The GlobalPosition.Y of the owning component.</param>
    /// <param name="profile">Shadow profile containing ground clearance.</param>
    public static void PinToGround(Sprite3D shadow, float parentGlobalY, ShadowProfile profile)
    {
        var pos = shadow.Position;
        pos.Y = profile.GroundClearance - parentGlobalY;
        shadow.Position = pos;
    }
}
