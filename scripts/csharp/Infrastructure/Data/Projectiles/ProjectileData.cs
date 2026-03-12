using Fateforged.Vfx;
using Godot;
using Godot.Collections;

namespace Fateforged.Projectiles;

/// <summary>
/// Data definition for projectile behavior.
/// Loaded from JSON via ProjectileCatalog.
///
/// Tuning Guide:
/// - Speed: Initial velocity in units/second (typical range: 10-30)
/// - Acceleration: Speed change per second. Use negative for deceleration
/// - MinSpeed: Floor for deceleration - prevents projectiles from stopping
/// - FadeInDuration: Time in seconds to fade from invisible to visible
/// </summary>
public class ProjectileData
{
    // =========================================================================
    // IDENTITY
    // =========================================================================

    public ProjectileId ProjectileId { get; set; } = ProjectileId.None;
    public string ProjectileName { get; set; } = "";

    // =========================================================================
    // VISUALS
    // =========================================================================

    /// <summary>Path to 3D model or sprite scene.</summary>
    public string ModelScenePath { get; set; } = "";

    /// <summary>Direct scene reference (loaded at runtime).</summary>
    public PackedScene? VisualScene { get; set; }

    /// <summary>VFX trail behind projectile.</summary>
    public VfxId TrailVfx { get; set; } = VfxId.None;

    /// <summary>VFX on hit.</summary>
    public VfxId HitVfx { get; set; } = VfxId.None;

    // =========================================================================
    // BEHAVIOR
    // =========================================================================

    public ProjectileMovementType MovementType { get; set; } = ProjectileMovementType.Straight;

    /// <summary>Initial velocity in units/second.</summary>
    public float Speed { get; set; } = 15f;

    /// <summary>Speed change per second (negative = decelerate).</summary>
    public float Acceleration { get; set; } = 0f;

    /// <summary>Minimum speed floor (prevents stopping).</summary>
    public float MinSpeed { get; set; } = 1f;

    // =========================================================================
    // SPEED EASING (for curved speed transitions)
    // =========================================================================

    /// <summary>Starting speed for eased transitions. If null, uses Speed.</summary>
    public float? SpeedStart { get; set; } = null;

    /// <summary>Final speed for eased transitions. If null, uses Speed.</summary>
    public float? SpeedEnd { get; set; } = null;

    /// <summary>Time to transition from SpeedStart to SpeedEnd (seconds).</summary>
    public float SpeedTransitionDuration { get; set; } = 1.0f;

    /// <summary>Easing curve type for speed transition.</summary>
    public SpeedEasingType SpeedEasing { get; set; } = SpeedEasingType.Linear;

    /// <summary>Exponent for EaseIn/EaseOut curves (higher = steeper curve).</summary>
    public float SpeedEaseExponent { get; set; } = 2.0f;

    /// <summary>Max time before despawn.</summary>
    public float Lifetime { get; set; } = 5f;

    /// <summary>Whether projectile rotates to face direction.</summary>
    public bool RotateToDirection { get; set; } = true;

    /// <summary>Time to fade in (0 = instant).</summary>
    public float FadeInDuration { get; set; } = 0f;

    /// <summary>Whether to fade out on hit (true) or despawn immediately (false).</summary>
    public bool FadeOnHit { get; set; } = true;

    /// <summary>Duration of fade out on hit.</summary>
    public float FadeDuration { get; set; } = 0.5f;

    // =========================================================================
    // ARC/BALLISTIC PROPERTIES
    // =========================================================================

    /// <summary>Height of arc for arc movement type.</summary>
    public float ArcHeight { get; set; } = 2f;

    /// <summary>Gravity for ballistic movement type.</summary>
    public float Gravity { get; set; } = -9.8f;

    // =========================================================================
    // WEAVING HOMING PROPERTIES
    // =========================================================================

    /// <summary>Time flying straight toward target before veering (seconds).</summary>
    public float VeerDelay { get; set; } = 0.15f;

    /// <summary>Angle to veer off course (degrees). Actual direction is randomized.</summary>
    public float VeerAngle { get; set; } = 25f;

    /// <summary>Time spent veering before homing kicks in (seconds).</summary>
    public float VeerDuration { get; set; } = 0.25f;

    /// <summary>Steering force when homing (degrees per second).</summary>
    public float SteerStrength { get; set; } = 180f;

    /// <summary>
    /// If true, projectile spawns at target's Y height (but source's X/Z).
    /// Creates effect of bolt "summoned in the air" rather than fired from caster.
    /// </summary>
    public bool SpawnAtTargetHeight { get; set; } = false;

    // =========================================================================
    // TRACKING PROPERTIES
    // =========================================================================

    /// <summary>
    /// Whether the projectile continuously tracks its target.
    /// For straight projectiles, this updates the endpoint to follow moving targets.
    /// Homing projectiles always track regardless of this setting.
    /// </summary>
    public bool Tracking { get; set; } = false;

    // =========================================================================
    // IMPACT
    // =========================================================================

    /// <summary>How many targets can it pierce through?</summary>
    public int PierceCount { get; set; } = 0;

    /// <summary>AOE damage radius on impact (0 = no AOE).</summary>
    public float AoeRadius { get; set; } = 0f;

    /// <summary>Projectile contact radius used for overlap checks.</summary>
    public float HitRadius { get; set; } = 2.5f;

    /// <summary>Hit-space model for overlap checks.</summary>
    public ProjectileHitSpace HitSpace { get; set; } = ProjectileHitSpace.GroundCylinder;

    // =========================================================================
    // AUDIO
    // =========================================================================

    public string LaunchSound { get; set; } = "";
    public string ImpactSound { get; set; } = "";

    // =========================================================================
    // FACTORY METHODS
    // =========================================================================

    /// <summary>
    /// Create ProjectileData from a GDScript Dictionary (JSON loading).
    /// </summary>
    public static ProjectileData FromDictionary(Dictionary dict)
    {
        var data = new ProjectileData
        {
            ProjectileId = new ProjectileId(GetString(dict, "projectile_id")),
            ProjectileName = GetString(dict, "projectile_name"),
            ModelScenePath = GetString(dict, "model_scene_path"),
            TrailVfx = new VfxId(GetString(dict, "trail_vfx", GetString(dict, "trail_effect_id"))),
            HitVfx = new VfxId(GetString(dict, "hit_vfx", GetString(dict, "impact_effect_id"))),
            Speed = GetFloat(dict, "speed", 15f),
            Acceleration = GetFloat(dict, "acceleration", 0f),
            MinSpeed = GetFloat(dict, "min_speed", 1f),
            SpeedStart = GetNullableFloat(dict, "speed_start"),
            SpeedEnd = GetNullableFloat(dict, "speed_end"),
            SpeedTransitionDuration = GetFloat(dict, "speed_transition_duration", 1.0f),
            SpeedEasing = ParseSpeedEasing(GetString(dict, "speed_easing", "linear")),
            SpeedEaseExponent = GetFloat(dict, "speed_ease_exponent", 2.0f),
            Lifetime = GetFloat(dict, "lifetime", 5f),
            RotateToDirection = GetBool(dict, "rotate_to_direction", true),
            FadeInDuration = GetFloat(dict, "fade_in_duration", 0f),
            FadeOnHit = GetBool(dict, "fade_on_hit", true),
            FadeDuration = GetFloat(dict, "fade_duration", 0.5f),
            ArcHeight = GetFloat(dict, "arc_height", 2f),
            Gravity = GetFloat(dict, "gravity", -9.8f),
            VeerDelay = GetFloat(dict, "veer_delay", 0.15f),
            VeerAngle = GetFloat(dict, "veer_angle", 25f),
            VeerDuration = GetFloat(dict, "veer_duration", 0.25f),
            SteerStrength = GetFloat(dict, "steer_strength", 180f),
            Tracking = GetBool(dict, "tracking", false),
            PierceCount = GetInt(dict, "pierce_count", 0),
            AoeRadius = GetFloat(dict, "aoe_radius", 0f),
            HitRadius = GetFloat(dict, "hit_radius", 2.5f),
            HitSpace = ParseHitSpace(GetString(dict, "hit_space", "ground_cylinder")),
            LaunchSound = GetString(dict, "launch_sound"),
            ImpactSound = GetString(dict, "impact_sound"),
        };

        // Parse movement type
        var movementStr = GetString(dict, "movement_type", "straight").ToLower();
        data.MovementType = movementStr switch
        {
            "homing" => ProjectileMovementType.Homing,
            "arc" => ProjectileMovementType.Arc,
            "ballistic" => ProjectileMovementType.Ballistic,
            "weaving_homing" => ProjectileMovementType.WeavingHoming,
            _ => ProjectileMovementType.Straight,
        };

        // Load visual scene if path provided
        if (!string.IsNullOrEmpty(data.ModelScenePath))
        {
            if (ResourceLoader.Exists(data.ModelScenePath))
            {
                data.VisualScene = ResourceLoader.Load<PackedScene>(data.ModelScenePath);
                if (data.VisualScene == null)
                {
                    GD.PushError(
                        $"ProjectileData: Failed to load visual scene at '{data.ModelScenePath}'"
                    );
                }
            }
            else
            {
                GD.PushError(
                    $"ProjectileData: Visual scene path does not exist: '{data.ModelScenePath}'"
                );
            }
        }

        return data;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static string GetString(Dictionary dict, string key, string defaultValue = "")
    {
        if (dict.ContainsKey(key))
        {
            var value = dict[key];
            if (value.VariantType == Variant.Type.String)
            {
                return value.AsString();
            }
        }
        return defaultValue;
    }

    private static float GetFloat(Dictionary dict, string key, float defaultValue = 0f)
    {
        if (dict.ContainsKey(key))
        {
            var value = dict[key];
            if (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int)
            {
                return value.AsSingle();
            }
        }
        return defaultValue;
    }

    private static int GetInt(Dictionary dict, string key, int defaultValue = 0)
    {
        if (dict.ContainsKey(key))
        {
            var value = dict[key];
            if (value.VariantType == Variant.Type.Int || value.VariantType == Variant.Type.Float)
            {
                return value.AsInt32();
            }
        }
        return defaultValue;
    }

    private static bool GetBool(Dictionary dict, string key, bool defaultValue = false)
    {
        if (dict.ContainsKey(key))
        {
            var value = dict[key];
            if (value.VariantType == Variant.Type.Bool)
            {
                return value.AsBool();
            }
            // Also handle string "true"/"false"
            if (value.VariantType == Variant.Type.String)
            {
                return value.AsString().ToLower() == "true";
            }
        }
        return defaultValue;
    }

    private static float? GetNullableFloat(Dictionary dict, string key)
    {
        if (dict.ContainsKey(key))
        {
            var value = dict[key];
            if (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int)
            {
                return value.AsSingle();
            }
        }
        return null;
    }

    private static SpeedEasingType ParseSpeedEasing(string value)
    {
        return value.ToLower() switch
        {
            "ease_in" or "easein" => SpeedEasingType.EaseIn,
            "ease_out" or "easeout" => SpeedEasingType.EaseOut,
            "ease_in_out" or "easeinout" => SpeedEasingType.EaseInOut,
            _ => SpeedEasingType.Linear,
        };
    }

    private static ProjectileHitSpace ParseHitSpace(string value)
    {
        return value.ToLower() switch
        {
            "sphere_3d" or "sphere3d" or "sphere" => ProjectileHitSpace.Sphere3D,
            _ => ProjectileHitSpace.GroundCylinder,
        };
    }
}
