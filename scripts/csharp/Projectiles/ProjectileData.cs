using Godot;
using Godot.Collections;

namespace ProjectSummoner.Projectiles;

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

    public string ProjectileId { get; set; } = "";
    public string ProjectileName { get; set; } = "";

    // =========================================================================
    // VISUALS
    // =========================================================================

    /// <summary>Path to 3D model or sprite scene.</summary>
    public string ModelScenePath { get; set; } = "";

    /// <summary>Direct scene reference (loaded at runtime).</summary>
    public PackedScene? VisualScene { get; set; }

    /// <summary>VFX trail behind projectile.</summary>
    public string TrailVfx { get; set; } = "";

    /// <summary>VFX on hit.</summary>
    public string HitVfx { get; set; } = "";

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
    // TRACKING PROPERTIES
    // =========================================================================

    /// <summary>
    /// Whether the projectile continuously tracks its target.
    /// For straight projectiles, this updates the endpoint to follow moving targets.
    /// Homing projectiles always track regardless of this setting.
    /// </summary>
    public bool Tracking { get; set; } = false;

    /// <summary>Turn rate for homing projectiles.</summary>
    public float HomingStrength { get; set; } = 5f;

    /// <summary>Time before homing starts.</summary>
    public float HomingDelay { get; set; } = 0f;

    // =========================================================================
    // IMPACT
    // =========================================================================

    /// <summary>How many targets can it pierce through?</summary>
    public int PierceCount { get; set; } = 0;

    /// <summary>AOE damage radius on impact (0 = no AOE).</summary>
    public float AoeRadius { get; set; } = 0f;

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
            ProjectileId = GetString(dict, "projectile_id"),
            ProjectileName = GetString(dict, "projectile_name"),
            ModelScenePath = GetString(dict, "model_scene_path"),
            TrailVfx = GetString(dict, "trail_vfx", GetString(dict, "trail_effect_id")),
            HitVfx = GetString(dict, "hit_vfx", GetString(dict, "impact_effect_id")),
            Speed = GetFloat(dict, "speed", 15f),
            Acceleration = GetFloat(dict, "acceleration", 0f),
            MinSpeed = GetFloat(dict, "min_speed", 1f),
            Lifetime = GetFloat(dict, "lifetime", 5f),
            RotateToDirection = GetBool(dict, "rotate_to_direction", true),
            FadeInDuration = GetFloat(dict, "fade_in_duration", 0f),
            FadeOnHit = GetBool(dict, "fade_on_hit", true),
            FadeDuration = GetFloat(dict, "fade_duration", 0.5f),
            ArcHeight = GetFloat(dict, "arc_height", 2f),
            Gravity = GetFloat(dict, "gravity", -9.8f),
            Tracking = GetBool(dict, "tracking", false),
            HomingStrength = GetFloat(dict, "homing_strength", 5f),
            HomingDelay = GetFloat(dict, "homing_delay", 0f),
            PierceCount = GetInt(dict, "pierce_count", 0),
            AoeRadius = GetFloat(dict, "aoe_radius", 0f),
            LaunchSound = GetString(dict, "launch_sound"),
            ImpactSound = GetString(dict, "impact_sound")
        };

        // Parse movement type
        var movementStr = GetString(dict, "movement_type", "straight").ToLower();
        data.MovementType = movementStr switch
        {
            "homing" => ProjectileMovementType.Homing,
            "arc" => ProjectileMovementType.Arc,
            "ballistic" => ProjectileMovementType.Ballistic,
            _ => ProjectileMovementType.Straight
        };

        // Load visual scene if path provided
        if (!string.IsNullOrEmpty(data.ModelScenePath))
        {
            if (ResourceLoader.Exists(data.ModelScenePath))
            {
                data.VisualScene = ResourceLoader.Load<PackedScene>(data.ModelScenePath);
                if (data.VisualScene == null)
                {
                    GD.PushError($"ProjectileData: Failed to load visual scene at '{data.ModelScenePath}'");
                }
            }
            else
            {
                GD.PushError($"ProjectileData: Visual scene path does not exist: '{data.ModelScenePath}'");
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

}
