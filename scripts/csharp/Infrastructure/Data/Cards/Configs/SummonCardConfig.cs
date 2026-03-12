using Fateforged.Cards.Formations;
using Godot;

namespace Fateforged.Cards.Configs;

/// <summary>
/// Configuration for summon cards.
/// Contains spawn count, unit scene, formation, and unit stats.
/// </summary>
public partial class SummonCardConfig : CardConfig
{
    // =========================================================================
    // SPAWN PROPERTIES
    // =========================================================================

    /// <summary>
    /// Path to the unit scene to instantiate.
    /// </summary>
    [Export]
    public string UnitScenePath { get; set; } = "";

    /// <summary>
    /// Number of units to spawn.
    /// </summary>
    [Export]
    public int SpawnCount { get; set; } = 1;

    /// <summary>
    /// Time in seconds for the summon animation.
    /// </summary>
    [Export]
    public float SummonTime { get; set; } = 1.0f;

    /// <summary>
    /// Unit type classification (melee, ranged, support, etc.).
    /// </summary>
    [Export]
    public string UnitType { get; set; } = "melee";

    // =========================================================================
    // FORMATION
    // =========================================================================

    /// <summary>
    /// Formation strategy for positioning spawned units.
    /// References FormationPresets directly for type-safe configuration.
    /// </summary>
    public IFormationStrategy Formation { get; set; } = FormationPresets.StandardGrid;

    // =========================================================================
    // UNIT STATS
    // =========================================================================

    /// <summary>
    /// Maximum health points.
    /// </summary>
    [Export]
    public float MaxHp { get; set; } = 100.0f;

    /// <summary>
    /// Base attack damage per hit.
    /// </summary>
    [Export]
    public float AttackDamage { get; set; } = 10.0f;

    /// <summary>
    /// Distance at which unit can attack.
    /// </summary>
    [Export]
    public float AttackRange { get; set; } = 2.0f;

    /// <summary>
    /// Attacks per second.
    /// </summary>
    [Export]
    public float AttackSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Movement speed (units per second).
    /// </summary>
    [Export]
    public float MoveSpeed { get; set; } = 3.0f;

    /// <summary>
    /// Radius within which unit will acquire targets.
    /// </summary>
    [Export]
    public float AggroRadius { get; set; } = 20.0f;

    /// <summary>
    /// Whether the unit uses ranged attacks.
    /// </summary>
    [Export]
    public bool IsRanged { get; set; } = false;

    /// <summary>
    /// Path to projectile scene for ranged units.
    /// </summary>
    [Export]
    public string ProjectileScenePath { get; set; } = "";

    /// <summary>
    /// Projectile ID for ranged units (uses ProjectileManager).
    /// </summary>
    [Export]
    public string ProjectileId { get; set; } = "";

    // =========================================================================
    // CONVERSION
    // =========================================================================

    /// <summary>
    /// Create a SummonCardConfig from a GDScript dictionary.
    /// </summary>
    public static new SummonCardConfig FromDictionary(Godot.Collections.Dictionary dict)
    {
        var config = new SummonCardConfig();
        PopulateFromDictionary(config, dict);
        PopulateSummonFromDictionary(config, dict);
        return config;
    }

    /// <summary>
    /// Populate summon-specific fields from dictionary.
    /// </summary>
    protected static void PopulateSummonFromDictionary(
        SummonCardConfig config,
        Godot.Collections.Dictionary dict
    )
    {
        if (dict.TryGetValue("unit_scene_path", out var scenePath))
            config.UnitScenePath = scenePath.AsString();

        if (dict.TryGetValue("spawn_count", out var spawnCount))
            config.SpawnCount = spawnCount.AsInt32();

        if (dict.TryGetValue("summon_time", out var summonTime))
            config.SummonTime = summonTime.AsSingle();

        if (dict.TryGetValue("unit_type", out var unitType))
            config.UnitType = unitType.AsString();

        // Formation - look up from C# CardCatalog by catalog_id
        if (dict.TryGetValue("catalog_id", out var catalogId))
        {
            var card = CardCatalog.GetCard(catalogId.AsString());
            if (card != null)
            {
                config.Formation = card.Formation;
            }
        }

        // Unit stats
        if (dict.TryGetValue("max_hp", out var maxHp))
            config.MaxHp = maxHp.AsSingle();

        if (dict.TryGetValue("attack_damage", out var attackDamage))
            config.AttackDamage = attackDamage.AsSingle();

        if (dict.TryGetValue("attack_range", out var attackRange))
            config.AttackRange = attackRange.AsSingle();

        if (dict.TryGetValue("attack_speed", out var attackSpeed))
            config.AttackSpeed = attackSpeed.AsSingle();

        if (dict.TryGetValue("move_speed", out var moveSpeed))
            config.MoveSpeed = moveSpeed.AsSingle();

        if (dict.TryGetValue("aggro_radius", out var aggroRadius))
            config.AggroRadius = aggroRadius.AsSingle();

        if (dict.TryGetValue("is_ranged", out var isRanged))
            config.IsRanged = isRanged.AsBool();

        if (dict.TryGetValue("projectile_scene_path", out var projPath))
            config.ProjectileScenePath = projPath.AsString();

        if (dict.TryGetValue("projectile_id", out var projId))
            config.ProjectileId = projId.AsString();
    }

    /// <summary>
    /// Convert to GDScript-compatible dictionary.
    /// </summary>
    public override Godot.Collections.Dictionary ToDictionary()
    {
        var dict = base.ToDictionary();

        // Spawn properties
        dict["unit_scene_path"] = UnitScenePath;
        dict["spawn_count"] = SpawnCount;
        dict["summon_time"] = SummonTime;
        dict["unit_type"] = UnitType;

        // Formation is now IFormationStrategy - lookup happens via catalog_id

        // Unit stats
        dict["max_hp"] = MaxHp;
        dict["attack_damage"] = AttackDamage;
        dict["attack_range"] = AttackRange;
        dict["attack_speed"] = AttackSpeed;
        dict["move_speed"] = MoveSpeed;
        dict["aggro_radius"] = AggroRadius;
        dict["is_ranged"] = IsRanged;
        dict["projectile_scene_path"] = ProjectileScenePath;
        dict["projectile_id"] = ProjectileId;

        return dict;
    }
}
