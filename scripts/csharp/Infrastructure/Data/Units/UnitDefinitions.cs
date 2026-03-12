using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Godot;
using Fateforged.Constants;
using Fateforged.Projectiles;
using Fateforged.Stats;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Units;

/// <summary>
/// Central registry of all unit type definitions.
/// This is the SINGLE SOURCE OF TRUTH for unit configuration.
/// Scene files define visuals only; all behavior comes from here.
/// </summary>
public static class UnitDefinitions
{
    // =========================================================================
    // WISPS (Basic melee units for each element)
    // =========================================================================

    public static readonly UnitDefinition FireWisp = new()
    {
        Id = UnitIds.FireWisp,
        DisplayName = "Fire Wisp",
        Stats = new UnitStats
        {
            MaxHp = 60f,
            AttackDamage = 12f,
            AttackRange = 3.0f,
            AttackSpeed = 1.2f,
            MoveSpeed = 3.5f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/fire_wisp_3d.tscn"
    };

    public static readonly UnitDefinition WaterWisp = new()
    {
        Id = UnitIds.WaterWisp,
        DisplayName = "Water Wisp",
        Stats = new UnitStats
        {
            MaxHp = 65f,
            AttackDamage = 10f,
            AttackRange = 3.0f,
            AttackSpeed = 1.1f,
            MoveSpeed = 3.2f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/water_wisp_3d.tscn"
    };

    public static readonly UnitDefinition WindWisp = new()
    {
        Id = UnitIds.WindWisp,
        DisplayName = "Wind Wisp",
        Stats = new UnitStats
        {
            MaxHp = 50f,
            AttackDamage = 10f,
            AttackRange = 3.0f,
            AttackSpeed = 1.4f,
            MoveSpeed = 4.0f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/wind_wisp_3d.tscn"
    };

    public static readonly UnitDefinition EarthWisp = new()
    {
        Id = UnitIds.EarthWisp,
        DisplayName = "Earth Wisp",
        Stats = new UnitStats
        {
            MaxHp = 80f,
            AttackDamage = 14f,
            AttackRange = 3.0f,
            AttackSpeed = 0.9f,
            MoveSpeed = 2.8f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/earth_wisp_3d.tscn"
    };

    public static readonly UnitDefinition LightningWisp = new()
    {
        Id = UnitIds.LightningWisp,
        DisplayName = "Lightning Wisp",
        Stats = new UnitStats
        {
            MaxHp = 45f,
            AttackDamage = 15f,
            AttackRange = 3.0f,
            AttackSpeed = 1.5f,
            MoveSpeed = 4.2f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/lightning_wisp_3d.tscn"
    };

    public static readonly UnitDefinition LifeWisp = new()
    {
        Id = UnitIds.LifeWisp,
        DisplayName = "Life Wisp",
        Stats = new UnitStats
        {
            MaxHp = 70f,
            AttackDamage = 8f,
            AttackRange = 3.0f,
            AttackSpeed = 1.0f,
            MoveSpeed = 3.0f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/life_wisp_3d.tscn"
    };

    public static readonly UnitDefinition DeathWisp = new()
    {
        Id = UnitIds.DeathWisp,
        DisplayName = "Death Wisp",
        Stats = new UnitStats
        {
            MaxHp = 55f,
            AttackDamage = 14f,
            AttackRange = 3.0f,
            AttackSpeed = 1.1f,
            MoveSpeed = 3.0f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/death_wisp_3d.tscn"
    };

    public static readonly UnitDefinition ShadowWisp = new()
    {
        Id = UnitIds.ShadowWisp,
        DisplayName = "Shadow Wisp",
        Stats = new UnitStats
        {
            MaxHp = 50f,
            AttackDamage = 12f,
            AttackRange = 3.0f,
            AttackSpeed = 1.3f,
            MoveSpeed = 3.8f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/shadow_wisp_3d.tscn"
    };

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly UnitDefinition FireTitan = new()
    {
        Id = UnitIds.FireTitan,
        DisplayName = "Fire Titan",
        Stats = new UnitStats
        {
            MaxHp = 300f,
            AttackDamage = 20f,
            AttackRange = 5.0f,
            AttackSpeed = 0.8f,
            MoveSpeed = 2.0f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.8f },
        ScenePath = "res://scenes/battle/units/fire_titan_3d.tscn"
    };

    public static readonly UnitDefinition FireAnt = new()
    {
        Id = UnitIds.FireAnt,
        DisplayName = "Fire Ant",
        Stats = new UnitStats
        {
            MaxHp = 40f,
            AttackDamage = 8f,
            AttackRange = 3.0f,
            AttackSpeed = 1.5f,
            MoveSpeed = 4.5f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.3f },
        ScenePath = "res://scenes/battle/units/fire_ant_3d.tscn"
    };

    public static readonly UnitDefinition FireBoar = new()
    {
        Id = UnitIds.FireBoar,
        DisplayName = "Fire Boar",
        Stats = new UnitStats
        {
            MaxHp = 120f,
            AttackDamage = 18f,
            AttackRange = 3.5f,
            AttackSpeed = 0.8f,
            MoveSpeed = 2.5f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.6f},
        ScenePath = "res://scenes/battle/units/fire_boar_3d.tscn"
    };

    public static readonly UnitDefinition FireWolf = new()
    {
        Id = UnitIds.FireWolf,
        DisplayName = "Fire Wolf",
        Stats = new UnitStats
        {
            MaxHp = 95f,
            AttackDamage = 16f,
            AttackRange = 3.5f,
            AttackSpeed = 1.1f,
            MoveSpeed = 3.4f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.55f },
        ScenePath = "res://scenes/battle/units/fire_wolf_3d.tscn"
    };

    public static readonly UnitDefinition FireSpider = new()
    {
        Id = UnitIds.FireSpider,
        DisplayName = "Fire Spider",
        Stats = new UnitStats
        {
            MaxHp = 50f,
            AttackDamage = 10f,
            AttackRange = 18f,
            AttackSpeed = 0.6f,
            MoveSpeed = 3.5f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.FireWeb),
        Visual = new VisualConfig { SeparationRadius = 0.4f },
        ScenePath = "res://scenes/battle/units/fire_spider_3d.tscn"
    };

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly UnitDefinition EarthSprite = new()
    {
        Id = UnitIds.EarthSprite,
        DisplayName = "Earth Sprite",
        Stats = new UnitStats
        {
            MaxHp = 150f,
            AttackDamage = 18f,
            AttackRange = 3.0f,
            AttackSpeed = 0.9f,
            MoveSpeed = 1.8f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Attack = new AttackVectorConfig
        {
            Preset = AttackPreset.AreaCleave,
            Selection = new AttackSelectionConfig
            {
                TargetLimit = 3
            },
            Area = new AttackAreaConfig
            {
                Shape = AttackAreaShape.Box,
                // Forward smash footprint: shifted ahead and substantially larger.
                Size = new Vector3(5.4f, 1.0f, 2.6f),
                ForwardOffset = 2.1f
            }
        },

        Visual = new VisualConfig { SeparationRadius = 0.6f},
        ScenePath = "res://scenes/battle/units/earth_sprite_3d.tscn"
    };

    public static readonly UnitDefinition EarthKomodoDragon = new()
    {
        Id = UnitIds.EarthKomodoDragon,
        DisplayName = "Earth Komodo Dragon",
        Stats = new UnitStats
        {
            MaxHp = 180f,
            AttackDamage = 22f,
            AttackRange = 4.0f,
            AttackSpeed = 0.7f,
            MoveSpeed = 2.2f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.7f },
        ScenePath = "res://scenes/battle/units/earth_komodo_dragon_3d.tscn"
    };

    public static readonly UnitDefinition Rock = new()
    {
        Id = UnitIds.Rock,
        DisplayName = "Rock",
        Stats = new UnitStats
        {
            MaxHp = 500f,
            AttackDamage = 0f,
            AttackRange = 3.0f,
            AttackSpeed = 0f,
            MoveSpeed = 0f,
            AggroRadius = 0f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.Passive,
        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/rock_3d.tscn"
    };

    public static readonly UnitDefinition StoneApe = new()
    {
        Id = UnitIds.StoneApe,
        DisplayName = "Stone Ape",
        Stats = new UnitStats
        {
            MaxHp = 200f,
            AttackDamage = 25f,
            AttackRange = 4.0f,
            AttackSpeed = 0.6f,
            MoveSpeed = 1.8f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.7f },
        ScenePath = "res://scenes/battle/units/stone_ape_3d.tscn"
    };

    public static readonly UnitDefinition EarthRockThrower = new()
    {
        Id = UnitIds.EarthRockThrower,
        DisplayName = "Rock Thrower",
        Stats = new UnitStats
        {
            MaxHp = 45f,
            AttackDamage = 22f,
            AttackRange = 22f,
            AttackSpeed = 0.4f,
            MoveSpeed = 2.0f,
            AggroRadius = 22f
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.Rock),
        Visual = new VisualConfig { SeparationRadius = 0.3f },
        ScenePath = "res://scenes/battle/units/earth_rock_thrower_3d.tscn"
    };

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly UnitDefinition Puff = new()
    {
        Id = UnitIds.Puff,
        DisplayName = "Puff",
        Stats = new UnitStats
        {
            MaxHp = 80f,
            AttackDamage = 12f,
            AttackRange = 24f,
            AttackSpeed = 0.4f,
            MoveSpeed = 2.5f,
            AggroRadius = 24f
        },
        UnitType = UnitType.Ranged,
        MovementLayer = MovementLayer.Air,
        TargetingProfile = UnitTargetingProfile.FlyingConeStrafe,
        TargetingLayerFilter = TargetLayer.Both,
        TargetingConeCenterOffsetDegrees = -20f,
        Ranged = new RangedConfig(ProjectileIds.WindPuff)
        {
            ProjectileDelay = 0.585f,
            IsDelayedProjectile = true
        },
        Flying = new FlyingConfig { Altitude = 2.5f },
        Visual = new VisualConfig
        {
            SeparationRadius = 0.5f,
            HpBarOffsetY = 2.2f,
            TargetPointOffset = new Vector3(1.4f, 0f, 0f),
            Hurtbox = new HurtboxConfig
            {
                Horizontal = true,
                Height = 3.0f,
                Radius = 0.75f,
                Offset = new Vector3(1.4f, 0f, 0f)
            }
        },
        ScenePath = "res://scenes/battle/units/puff_3d.tscn"
    };

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly UnitDefinition WaterFrog = new()
    {
        Id = UnitIds.WaterFrog,
        DisplayName = "Water Frog",
        Stats = new UnitStats
        {
            MaxHp = 70f,
            AttackDamage = 15f,
            AttackRange = 5.0f,  // Extended range for tongue attack
            AttackSpeed = 1.0f,
            MoveSpeed = 2.5f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/water_frog_3d.tscn"
    };

    public static readonly UnitDefinition MamaDuck = new()
    {
        Id = UnitIds.MamaDuck,
        DisplayName = "Mama Duck",
        Stats = new UnitStats
        {
            MaxHp = 100f,
            AttackDamage = 12f,
            AttackRange = 3.5f,
            AttackSpeed = 0.9f,
            MoveSpeed = 2.8f,
            AggroRadius = 20f
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f},
        ScenePath = "res://scenes/battle/units/mama_duck_3d.tscn"
    };

    public static readonly UnitDefinition Duckling = new()
    {
        Id = UnitIds.Duckling,
        DisplayName = "Duckling",
        Stats = new UnitStats
        {
            MaxHp = 25f,
            AttackDamage = 8f,
            AttackRange = 12f,
            AttackSpeed = 0.8f,
            MoveSpeed = 3.2f,
            AggroRadius = 16f
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedStrafe,
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.25f },
        ScenePath = "res://scenes/battle/units/duckling_3d.tscn"
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    private static readonly Dictionary<UnitId, UnitDefinition> _lookup = new()
    {
        // Wisps
        [UnitIds.FireWisp] = FireWisp,
        [UnitIds.WaterWisp] = WaterWisp,
        [UnitIds.WindWisp] = WindWisp,
        [UnitIds.EarthWisp] = EarthWisp,
        [UnitIds.LightningWisp] = LightningWisp,
        [UnitIds.LifeWisp] = LifeWisp,
        [UnitIds.DeathWisp] = DeathWisp,
        [UnitIds.ShadowWisp] = ShadowWisp,
        // Fire
        [UnitIds.FireTitan] = FireTitan,
        [UnitIds.FireAnt] = FireAnt,
        [UnitIds.FireBoar] = FireBoar,
        [UnitIds.FireWolf] = FireWolf,
        [UnitIds.FireSpider] = FireSpider,
        // Earth
        [UnitIds.EarthSprite] = EarthSprite,
        [UnitIds.EarthKomodoDragon] = EarthKomodoDragon,
        [UnitIds.Rock] = Rock,
        [UnitIds.StoneApe] = StoneApe,
        [UnitIds.EarthRockThrower] = EarthRockThrower,
        // Wind
        [UnitIds.Puff] = Puff,
        // Water
        [UnitIds.WaterFrog] = WaterFrog,
        [UnitIds.MamaDuck] = MamaDuck,
        [UnitIds.Duckling] = Duckling
    };

    /// <summary>Get a unit definition by ID. Throws if not found.</summary>
    public static UnitDefinition Get(UnitId id)
    {
        if (_lookup.TryGetValue(id, out var def))
            return def;
        throw new KeyNotFoundException($"Unit definition not found for ID: {id}");
    }

    /// <summary>Try to get a unit definition by ID.</summary>
    public static bool TryGet(UnitId id, out UnitDefinition? def)
    {
        return _lookup.TryGetValue(id, out def);
    }

    /// <summary>Get a unit definition by string ID. Returns null if not found.</summary>
    public static UnitDefinition? Get(string id)
    {
        return TryGet(new UnitId(id), out var def) ? def : null;
    }

    /// <summary>Check if a unit type exists in the catalog.</summary>
    public static bool HasUnit(UnitId id) => _lookup.ContainsKey(id);

    /// <summary>Get all unit type IDs.</summary>
    public static UnitId[] GetAllUnitIds() => [.. _lookup.Keys];

    /// <summary>Get all unit definitions.</summary>
    public static UnitDefinition[] GetAllUnits() => [.. _lookup.Values];

    /// <summary>Get unit count.</summary>
    public static int Count => _lookup.Count;

    // =========================================================================
    // SIM TEMPLATE FACTORY
    // =========================================================================

    /// <summary>
    /// Build a SimUnitTemplate from a unit definition.
    /// This is the bridge between the unit catalog and the simulation's flat template format.
    /// </summary>
    public static SimUnitTemplate BuildSimTemplate(UnitId unitId, int count, StatModifier? modifier = null)
    {
        var template = new SimUnitTemplate { Count = count, UnitTypeId = unitId.Value };

        if (!TryGet(unitId, out var def) || def == null)
            return template;

        var stats = modifier != null ? def.Stats.WithModifier(modifier) : def.Stats;

        template.MaxHp = stats.MaxHp;
        template.AttackDamage = stats.AttackDamage;
        template.AttackSpeed = stats.AttackSpeed;
        template.MoveSpeed = stats.MoveSpeed;
        template.AttackRange = stats.AttackRange;
        template.AggroRadius = stats.AggroRadius;
        template.CritChance = stats.CritChance;
        template.CritDamage = stats.CritDamage;
        template.SoulStrength = stats.SoulStrength;
        template.UnitType = def.UnitType;
        template.TacticalRole = ResolveTacticalRole(def, stats);
        template.MovementLayer = def.MovementLayer;
        template.ElementId = (int)(def.DamageProfile.Element ?? Fateforged.Cards.Element.Neutral);
        template.PhysicalDamageRatio = def.DamageProfile.PhysicalRatio;
        template.ElementalDamageRatio = def.DamageProfile.ElementalRatio;
        template.AttackType =
            def.DamageProfile.ElementalRatio > 0f && def.DamageProfile.PhysicalRatio <= 0f
                ? Fateforged.Simulation.Enums.DamageType.Magic
                : Fateforged.Simulation.Enums.DamageType.Physical;
        template.SeparationRadius = def.Visual.SeparationRadius;
        template.NavigationRadius = def.Visual.SeparationRadius;
        template.HurtboxRadius = def.Visual.Hurtbox?.Radius > 0f
            ? def.Visual.Hurtbox.Radius
            : template.NavigationRadius;
        template.HurtboxHeight = def.Visual.Hurtbox?.Height > 0f
            ? def.Visual.Hurtbox.Height
            : 0f;
        template.HurtboxHorizontal = def.Visual.Hurtbox?.Horizontal ?? false;
        template.HurtboxOffset = def.Visual.Hurtbox != null
            ? new SimVector3(def.Visual.Hurtbox.Offset.X, def.Visual.Hurtbox.Offset.Y, def.Visual.Hurtbox.Offset.Z)
            : SimVector3.Zero;
        template.PhysicalDefense = stats.Armor;
        template.MagicDefense = stats.MagicResist;
        template.Attack = AttackVectorStateBuilder.Build(def.Attack);

        // Ranged config
        if (def.Ranged != null)
        {
            template.ProjectileCatalogId = def.Ranged.ProjectileId.Value;
            template.ProjectileDelay = def.Ranged.ProjectileDelay;
        }

        // Flying config
        if (def.Flying != null)
            template.FlightAltitude = def.Flying.Altitude;

        // Targeting profile assignment
        template.DistanceScorerWeight = def.TargetingDistanceScorerWeight;
        SetTargetingProfile(def, template);

        return template;
    }

    /// <summary>
    /// Set targeting profile fields based on unit definition.
    /// Uses explicit profile + tuning fields from UnitDefinition.
    /// </summary>
    private static void SetTargetingProfile(UnitDefinition def, SimUnitTemplate template)
    {
        // Explicitly reset engage/tuning fields before profile-specific assignment.
        template.EngageShape = EngageShape.Circle;
        template.EngageRectLength = 0f;
        template.EngageRectHalfWidth = 0f;
        template.EngageRectForwardOffset = 0f;
        template.EngageCloseRadius = 0.4f;
        template.HasConeConstraint = false;
        template.ConeHalfAngle = 30f;
        template.ConeCenterOffsetDegrees = 0f;
        template.CloseRangeThreshold = 0.5f;

        switch (def.TargetingProfile)
        {
            case UnitTargetingProfile.Passive:
                template.FallbackMovement = FallbackMovement.Idle;
                template.TargetLayerFilter = def.TargetingLayerFilter;
                template.TargetPolicyId = TargetPolicyId.PreferAttackable;
                template.MovementIntentStrategy = MovementIntentStrategy.Direct;
                return;

            case UnitTargetingProfile.MeleeGround:
                template.FallbackMovement = FallbackMovement.MoveToward;
                template.EngageShape = EngageShape.ForwardRect;
                template.EngageRectLength = MathF.Max(template.AttackRange * 0.9f, 0.1f);
                template.EngageRectHalfWidth = MathF.Max(template.NavigationRadius, 0.45f);
                template.EngageRectForwardOffset = 0f;
                template.EngageCloseRadius = MathF.Max(template.NavigationRadius * 0.9f, 0.4f);
                ApplyMeleeEngageRectOverridesFromAttack(template);
                template.HealthScorerWeight = def.TargetingHealthScorerWeight;
                template.TargetLayerFilter = def.TargetingLayerFilter;
                template.TargetPolicyId = TargetPolicyId.PreferAttackableAndStick;
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.RangedGround:
                template.FallbackMovement = FallbackMovement.MoveToward;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(def, UnitTargetingProfile.RangedGround);
                template.TargetPolicyId = TargetPolicyId.PreferAttackableAndStick;
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.RangedStrafe:
                template.FallbackMovement = FallbackMovement.Strafe;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(def, UnitTargetingProfile.RangedStrafe);
                template.TargetPolicyId = TargetPolicyId.PreferAttackableAndStick;
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.FlyingConeStrafe:
                template.FallbackMovement = FallbackMovement.Strafe;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(def, UnitTargetingProfile.FlyingConeStrafe);
                template.EngageShape = EngageShape.Cone;
                template.HasConeConstraint = true;
                template.ConeHalfAngle = def.TargetingConeHalfAngle;
                template.ConeCenterOffsetDegrees = def.TargetingConeCenterOffsetDegrees;
                template.CloseRangeThreshold = def.TargetingCloseRangeThreshold;
                template.TargetPolicyId = TargetPolicyId.PreferAttackableAndStick;
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(def.TargetingProfile), def.TargetingProfile, "Unknown UnitTargetingProfile");
        }
    }

    private static void ApplyMeleeEngageRectOverridesFromAttack(SimUnitTemplate template)
    {
        bool shouldOverride = false;
        float length = 0f;
        float halfWidth = 0f;
        float forwardOffset = 0f;

        switch (template.Attack.Selection.Mode)
        {
            case AttackSelectionMode.LineCollect:
                shouldOverride = true;
                length = template.Attack.Area.LineLength;
                halfWidth = template.Attack.Area.LineHalfWidth;
                forwardOffset = template.Attack.Area.ForwardOffset;
                break;

            case AttackSelectionMode.AreaCollect when template.Attack.Area.Shape == AttackAreaShape.Box:
                shouldOverride = true;
                length = template.Attack.Area.Size.X;
                halfWidth = template.Attack.Area.Size.Z;
                forwardOffset = template.Attack.Area.ForwardOffset;
                break;

            case AttackSelectionMode.AreaCollect when template.Attack.Area.Shape == AttackAreaShape.Line:
                shouldOverride = true;
                length = template.Attack.Area.LineLength;
                halfWidth = template.Attack.Area.LineHalfWidth;
                forwardOffset = template.Attack.Area.ForwardOffset;
                break;
        }

        if (!shouldOverride)
            return;

        if (length > 0f)
            template.EngageRectLength = length;
        if (halfWidth > 0f)
            template.EngageRectHalfWidth = halfWidth;
        template.EngageRectForwardOffset = MathF.Max(forwardOffset, 0f);
        if (template.EngageRectForwardOffset > 0f)
        {
            // Forward-offset melee sloting reserves orbit radius at offset + 0.05f.
            // Match close bubble to that band so units don't endlessly circle just
            // outside the authored offset threshold.
            float offsetCloseRadius = template.EngageRectForwardOffset + 0.05f;
            template.EngageCloseRadius = MathF.Max(template.EngageCloseRadius, offsetCloseRadius);
        }
    }

    private static TargetLayer ResolveTargetLayerFilterForProfile(UnitDefinition def, UnitTargetingProfile profile)
    {
        bool isRangedProfile = profile == UnitTargetingProfile.RangedGround ||
                               profile == UnitTargetingProfile.RangedStrafe ||
                               profile == UnitTargetingProfile.FlyingConeStrafe;
        if (!isRangedProfile)
            return def.TargetingLayerFilter;

        // UnitDefinition defaults to GroundOnly; ranged units are expected to hit air by default.
        return def.TargetingLayerFilter == TargetLayer.GroundOnly
            ? TargetLayer.Both
            : def.TargetingLayerFilter;
    }

    private static TacticalRole ResolveTacticalRole(UnitDefinition def, UnitStats stats)
    {
        if (def.TacticalRole != TacticalRole.Auto)
            return def.TacticalRole;

        if (def.UnitType == UnitType.Ranged)
            return TacticalRole.Backliner;

        if (stats.MoveSpeed >= 3.8f)
            return TacticalRole.Flanker;

        return TacticalRole.Frontliner;
    }
}
