using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Constants;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Stats;
using Godot;

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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/fire_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/water_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/wind_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/earth_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/lightning_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/life_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/death_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/shadow_wisp_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.8f },
        ScenePath = "res://scenes/battle/units/fire_titan_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.3f },
        ScenePath = "res://scenes/battle/units/fire_ant_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.6f },
        ScenePath = "res://scenes/battle/units/fire_boar_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.55f },
        ScenePath = "res://scenes/battle/units/fire_wolf_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "fire_web_slow",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 0f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Slow,
                        Value = 0.25f,
                        DurationSeconds = 2.5f,
                        Lifetime = EffectLifetime.Timed(2.5f),
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.FireWeb),
        Visual = new VisualConfig { SeparationRadius = 0.4f },
        ScenePath = "res://scenes/battle/units/fire_spider_3d.tscn",
    };

    public static readonly UnitDefinition CinderCaster = new()
    {
        Id = UnitIds.CinderCaster,
        DisplayName = "Cinder Caster",
        Stats = new UnitStats
        {
            MaxHp = 65f,
            AttackDamage = 8f,
            AttackRange = 19f,
            AttackSpeed = 0.95f,
            MoveSpeed = 2.7f,
            AggroRadius = 21f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "burn_on_hit",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.StatusApply,
                        DurationSeconds = 4f,
                        Status = new ProjectileStatusConfig
                        {
                            Kind = StatusEffectKind.Burn,
                            DurationSeconds = 4f,
                            TickIntervalSeconds = 1f,
                            PotencyPerStack = 3f,
                            MaxStacks = 5,
                        },
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.Ember),
        Visual = new VisualConfig { SeparationRadius = 0.38f, DisplayScale = 0.92f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition EmberBombCarrier = new()
    {
        Id = UnitIds.EmberBombCarrier,
        DisplayName = "Ember Bomb Carrier",
        Stats = new UnitStats
        {
            MaxHp = 38f,
            AttackDamage = 6f,
            AttackRange = 2.4f,
            AttackSpeed = 1.2f,
            MoveSpeed = 4.4f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        TacticalRole = TacticalRole.Flanker,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "contact_self_destruct",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 0f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Damage,
                        Value = 999f,
                        DamageType = DamageType.True,
                    },
                ],
            },
            new UnitAbilityConfig
            {
                AbilityId = "death_burst",
                Trigger = UnitAbilityTrigger.OnDeath,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                Radius = 4.5f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Damage,
                        Value = 38f,
                        DamageType = DamageType.Magic,
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.3f, DisplayScale = 0.82f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition KindlingSwarmUnit = new()
    {
        Id = UnitIds.KindlingSwarmUnit,
        DisplayName = "Kindling Swarm Unit",
        Stats = new UnitStats
        {
            MaxHp = 32f,
            AttackDamage = 7f,
            AttackRange = 2.8f,
            AttackSpeed = 1.45f,
            MoveSpeed = 4.1f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "kindling_death_spark",
                Trigger = UnitAbilityTrigger.OnDeath,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                Radius = 2.5f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.StatusApply,
                        DurationSeconds = 2f,
                        Status = new ProjectileStatusConfig
                        {
                            Kind = StatusEffectKind.Burn,
                            DurationSeconds = 2f,
                            TickIntervalSeconds = 1f,
                            PotencyPerStack = 1.5f,
                            MaxStacks = 2,
                        },
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.28f, DisplayScale = 0.72f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition FireFrontliner = new()
    {
        Id = UnitIds.FireFrontliner,
        DisplayName = "Fire Frontliner",
        Stats = new UnitStats
        {
            MaxHp = 235f,
            AttackDamage = 17f,
            AttackRange = 3.2f,
            AttackSpeed = 0.75f,
            MoveSpeed = 2.1f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Visual = new VisualConfig { SeparationRadius = 0.72f, DisplayScale = 1.28f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition OverheatBrawler = new()
    {
        Id = UnitIds.OverheatBrawler,
        DisplayName = "Overheat Brawler",
        Stats = new UnitStats
        {
            MaxHp = 145f,
            AttackDamage = 13f,
            AttackRange = 3.2f,
            AttackSpeed = 0.9f,
            MoveSpeed = 3.0f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "overheat_ramp",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3.0f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.DamageBoost,
                        Value = 0.12f,
                        DurationSeconds = 5f,
                        Lifetime = EffectLifetime.Timed(5f),
                    },
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.AttackSpeedModifier,
                        Value = 0.10f,
                        DurationSeconds = 5f,
                        Lifetime = EffectLifetime.Timed(5f),
                    },
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Damage,
                        Value = 5f,
                        DamageType = DamageType.True,
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.52f, DisplayScale = 1.08f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition FlameChanneler = new()
    {
        Id = UnitIds.FlameChanneler,
        DisplayName = "Flame Channeler",
        Stats = new UnitStats
        {
            MaxHp = 75f,
            AttackDamage = 6f,
            AttackRange = 13f,
            AttackSpeed = 1.65f,
            MoveSpeed = 2.4f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "channel_burn_tick",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.StatusApply,
                        DurationSeconds = 3f,
                        Status = new ProjectileStatusConfig
                        {
                            Kind = StatusEffectKind.Burn,
                            DurationSeconds = 3f,
                            TickIntervalSeconds = 1f,
                            PotencyPerStack = 1.6f,
                            MaxStacks = 6,
                        },
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.Ember),
        Visual = new VisualConfig { SeparationRadius = 0.4f, DisplayScale = 0.96f },
        ScenePath = "res://scenes/battle/units/fire_roster_placeholder_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Attack = new AttackVectorConfig
        {
            Preset = AttackPreset.AreaCleave,
            Selection = new AttackSelectionConfig { TargetLimit = 3 },
            Area = new AttackAreaConfig
            {
                Shape = AttackAreaShape.Box,
                // Tuned forward smash footprint: still directional, but no longer
                // oversized enough to destabilize engage/pathing behavior.
                Size = new Vector3(2.7f, 1.0f, 1.3f),
                ForwardOffset = 1.05f,
            },
        },

        Visual = new VisualConfig { SeparationRadius = 0.6f },
        ScenePath = "res://scenes/battle/units/earth_sprite_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.7f },
        ScenePath = "res://scenes/battle/units/earth_komodo_dragon_3d.tscn",
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
            AggroRadius = 0f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.Passive,
        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/rock_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.7f },
        ScenePath = "res://scenes/battle/units/stone_ape_3d.tscn",
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
            AggroRadius = 22f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.Rock),
        Visual = new VisualConfig { SeparationRadius = 0.3f },
        ScenePath = "res://scenes/battle/units/earth_rock_thrower_3d.tscn",
    };

    public static readonly UnitDefinition EarthFlatDamageReductionTank = new()
    {
        Id = UnitIds.EarthFlatDamageReductionTank,
        DisplayName = "Earth Flat Damage Reduction Tank",
        Stats = new UnitStats
        {
            MaxHp = 250f,
            AttackDamage = 14f,
            AttackRange = 3.2f,
            AttackSpeed = 0.7f,
            MoveSpeed = 1.9f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "flat_damage_reduction_passive",
                Trigger = UnitAbilityTrigger.OnSpawn,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 10f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.FlatDamageReduction,
                        Value = 4f,
                        DurationSeconds = -1f,
                        Lifetime = EffectLifetime.Persistent(),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.75f, DisplayScale = 1.22f },
        ScenePath =
            "res://scenes/battle/units/earth_flat_damage_reduction_tank_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition EarthBulletUnit = new()
    {
        Id = UnitIds.EarthBulletUnit,
        DisplayName = "Earth Bullet Unit",
        Stats = new UnitStats
        {
            MaxHp = 85f,
            AttackDamage = 15f,
            AttackRange = 20f,
            AttackSpeed = 0.75f,
            MoveSpeed = 2.3f,
            AggroRadius = 22f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "impact_slow",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Slow,
                        Value = 0.20f,
                        DurationSeconds = 2f,
                        Lifetime = EffectLifetime.Timed(2f),
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.Rock),
        Visual = new VisualConfig { SeparationRadius = 0.4f, DisplayScale = 0.96f },
        ScenePath = "res://scenes/battle/units/earth_bullet_unit_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition TauntPulseGuardian = new()
    {
        Id = UnitIds.TauntPulseGuardian,
        DisplayName = "Taunt Pulse Guardian",
        Stats = new UnitStats
        {
            MaxHp = 260f,
            AttackDamage = 14f,
            AttackRange = 3.0f,
            AttackSpeed = 0.7f,
            MoveSpeed = 2.0f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "taunt_pulse",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3.0f,
                Radius = 8.0f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Taunt,
                        DurationSeconds = 2.5f,
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.7f },
        ScenePath = "res://scenes/battle/units/stone_ape_3d.tscn",
    };

    public static readonly UnitDefinition EarthShieldSupport = new()
    {
        Id = UnitIds.EarthShieldSupport,
        DisplayName = "Earth Shield Support",
        Stats = new UnitStats
        {
            MaxHp = 110f,
            AttackDamage = 7f,
            AttackRange = 14f,
            AttackSpeed = 0.6f,
            MoveSpeed = 2.0f,
            AggroRadius = 16f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "stone_shield_pulse",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.AlliesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 4f,
                Radius = 7f,
                TargetAffinity = AbilityTargetAffinity.Allies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Shield,
                        Value = 28f,
                        DurationSeconds = 4f,
                        Lifetime = EffectLifetime.Timed(4f),
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.Rock),
        Visual = new VisualConfig { SeparationRadius = 0.45f, DisplayScale = 0.92f },
        ScenePath = "res://scenes/battle/units/earth_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition BurrowAmbusher = new()
    {
        Id = UnitIds.BurrowAmbusher,
        DisplayName = "Burrow Ambusher",
        Stats = new UnitStats
        {
            MaxHp = 115f,
            AttackDamage = 16f,
            AttackRange = 3.0f,
            AttackSpeed = 1.05f,
            MoveSpeed = 2.2f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        TacticalRole = TacticalRole.Flanker,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "burrow_engage",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.CurrentTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 6.5f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.SourceLungeToTarget,
                        Value = 2.1f,
                    },
                ],
            },
            new UnitAbilityConfig
            {
                AbilityId = "ambush_opening",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 4f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Damage,
                        Value = 18f,
                        DamageType = DamageType.Physical,
                    },
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Stun,
                        Value = 1f,
                        DurationSeconds = 0.75f,
                        Lifetime = EffectLifetime.Timed(0.75f),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.5f, DisplayScale = 0.96f },
        ScenePath = "res://scenes/battle/units/earth_roster_placeholder_3d.tscn",
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
            AggroRadius = 24f,
        },
        UnitType = UnitType.Ranged,
        MovementLayer = MovementLayer.Air,
        TargetingProfile = UnitTargetingProfile.FlyingConeStrafe,
        TargetingLayerFilter = TargetLayer.Both,
        TargetingConeCenterOffsetDegrees = -20f,
        Ranged = new RangedConfig(ProjectileIds.WindPuff)
        {
            ProjectileDelay = 0.585f,
            IsDelayedProjectile = true,
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
                Offset = new Vector3(1.4f, 0f, 0f),
            },
        },
        ScenePath = "res://scenes/battle/units/puff_3d.tscn",
    };

    public static readonly UnitDefinition WindEvasionTank = new()
    {
        Id = UnitIds.WindEvasionTank,
        DisplayName = "Wind Evasion Tank",
        Stats = new UnitStats
        {
            MaxHp = 170f,
            AttackDamage = 13f,
            AttackRange = 3.2f,
            AttackSpeed = 0.85f,
            MoveSpeed = 2.7f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "evasion_passive",
                Trigger = UnitAbilityTrigger.OnSpawn,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 10f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.EvasionModifier,
                        Value = 0.2f,
                        DurationSeconds = -1f,
                        Lifetime = EffectLifetime.Persistent(),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.65f, DisplayScale = 1.16f },
        ScenePath = "res://scenes/battle/units/wind_evasion_tank_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindPushbackUnit = new()
    {
        Id = UnitIds.WindPushbackUnit,
        DisplayName = "Wind Pushback Unit",
        Stats = new UnitStats
        {
            MaxHp = 90f,
            AttackDamage = 10f,
            AttackRange = 16f,
            AttackSpeed = 0.8f,
            MoveSpeed = 2.9f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedStrafe,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "targeted_knockback",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 0f,
                Range = 18f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Knockback,
                        Value = 2.8f,
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.5f, DisplayScale = 0.98f },
        ScenePath = "res://scenes/battle/units/wind_pushback_unit_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindCleaveUnit = new()
    {
        Id = UnitIds.WindCleaveUnit,
        DisplayName = "Wind Cleave Unit",
        Stats = new UnitStats
        {
            MaxHp = 120f,
            AttackDamage = 18f,
            AttackRange = 3.6f,
            AttackSpeed = 1.0f,
            MoveSpeed = 3.4f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Attack = new AttackVectorConfig
        {
            Preset = AttackPreset.AreaCleave,
            Selection = new AttackSelectionConfig { TargetLimit = 3 },
            Area = new AttackAreaConfig
            {
                Shape = AttackAreaShape.Box,
                Size = new Vector3(4.2f, 1.0f, 2.1f),
                ForwardOffset = 1.5f,
            },
        },
        Visual = new VisualConfig { SeparationRadius = 0.55f, DisplayScale = 1.06f },
        ScenePath = "res://scenes/battle/units/wind_cleave_unit_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindDiver = new()
    {
        Id = UnitIds.WindDiver,
        DisplayName = "Wind Diver",
        Stats = new UnitStats
        {
            MaxHp = 75f,
            AttackDamage = 17f,
            AttackRange = 3.0f,
            AttackSpeed = 1.15f,
            MoveSpeed = 4.4f,
            AggroRadius = 22f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        TacticalRole = TacticalRole.Flanker,
        TargetPriority = UnitTargetPriority.PreferRangedOrSupport,
        Visual = new VisualConfig { SeparationRadius = 0.4f, DisplayScale = 0.84f },
        ScenePath = "res://scenes/battle/units/wind_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindSpeedSupport = new()
    {
        Id = UnitIds.WindSpeedSupport,
        DisplayName = "Wind Speed Support",
        Stats = new UnitStats
        {
            MaxHp = 80f,
            AttackDamage = 6f,
            AttackRange = 16f,
            AttackSpeed = 0.65f,
            MoveSpeed = 3.0f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "attack_speed_aura",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.AlliesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3f,
                Radius = 7f,
                TargetAffinity = AbilityTargetAffinity.Allies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.AttackSpeedModifier,
                        Value = 0.18f,
                        DurationSeconds = 3.5f,
                        Lifetime = EffectLifetime.Timed(3.5f),
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.42f, DisplayScale = 0.9f },
        ScenePath = "res://scenes/battle/units/wind_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindMissSupport = new()
    {
        Id = UnitIds.WindMissSupport,
        DisplayName = "Wind Miss Support",
        Stats = new UnitStats
        {
            MaxHp = 78f,
            AttackDamage = 5f,
            AttackRange = 16f,
            AttackSpeed = 0.65f,
            MoveSpeed = 3.1f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "accuracy_disrupt_pulse",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3.2f,
                Radius = 7f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.AccuracyModifier,
                        Value = -0.18f,
                        DurationSeconds = 3.5f,
                        Lifetime = EffectLifetime.Timed(3.5f),
                    },
                ],
            },
        ],
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.42f, DisplayScale = 0.9f },
        ScenePath = "res://scenes/battle/units/wind_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WindSwarmUnit = new()
    {
        Id = UnitIds.WindSwarmUnit,
        DisplayName = "Wind Swarm Unit",
        Stats = new UnitStats
        {
            MaxHp = 34f,
            AttackDamage = 6f,
            AttackRange = 2.8f,
            AttackSpeed = 1.6f,
            MoveSpeed = 4.6f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        TacticalRole = TacticalRole.Flanker,
        Visual = new VisualConfig { SeparationRadius = 0.28f, DisplayScale = 0.72f },
        ScenePath = "res://scenes/battle/units/wind_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition DashStriker = new()
    {
        Id = UnitIds.DashStriker,
        DisplayName = "Flow Striker",
        Stats = new UnitStats
        {
            MaxHp = 95f,
            AttackDamage = 14f,
            AttackRange = 3.1f,
            AttackSpeed = 1.25f,
            MoveSpeed = 4.0f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        TacticalRole = TacticalRole.Flanker,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "strike_flow",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.EvasionModifier,
                        Value = 0.20f,
                        DurationSeconds = 1.5f,
                        Lifetime = EffectLifetime.Timed(1.5f),
                    },
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.AttackSpeedModifier,
                        Value = 0.22f,
                        DurationSeconds = 1.5f,
                        Lifetime = EffectLifetime.Timed(1.5f),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.42f, DisplayScale = 0.88f },
        ScenePath = "res://scenes/battle/units/wind_roster_placeholder_3d.tscn",
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
            AttackRange = 5.0f, // Extended range for tongue attack
            AttackSpeed = 1.0f,
            MoveSpeed = 2.5f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig
        {
            SeparationRadius = 0.5f,
            TargetPointOffset = new Vector3(0f, -0.45f, 0f),
        },
        ScenePath = "res://scenes/battle/units/water_frog_3d.tscn",
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
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,

        Visual = new VisualConfig { SeparationRadius = 0.5f },
        ScenePath = "res://scenes/battle/units/mama_duck_3d.tscn",
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
            AggroRadius = 16f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedStrafe,
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.25f },
        ScenePath = "res://scenes/battle/units/duckling_3d.tscn",
    };

    public static readonly UnitDefinition WaterBulwark = new()
    {
        Id = UnitIds.WaterBulwark,
        DisplayName = "Water Frontliner",
        Stats = new UnitStats
        {
            MaxHp = 240f,
            AttackDamage = 15f,
            AttackRange = 3.2f,
            AttackSpeed = 0.7f,
            MoveSpeed = 1.9f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Visual = new VisualConfig { SeparationRadius = 0.75f, DisplayScale = 1.24f },
        ScenePath = "res://scenes/battle/units/earth_sprite_3d.tscn",
    };

    public static readonly UnitDefinition WaterMender = new()
    {
        Id = UnitIds.WaterMender,
        DisplayName = "Water Cleanser",
        Stats = new UnitStats
        {
            MaxHp = 85f,
            AttackDamage = 0f,
            AttackRange = 14f,
            AttackSpeed = 0f,
            MoveSpeed = 2.2f,
            AggroRadius = 14f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.Passive,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "cleanse_pulse",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.AlliesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 4.2f,
                Radius = 7f,
                TargetAffinity = AbilityTargetAffinity.Allies,
                Effects =
                [
                    new UnitAbilityEffectConfig { EffectType = EffectType.Cleanse },
                    new UnitAbilityEffectConfig { EffectType = EffectType.Heal, Value = 18f },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.45f },
        ScenePath = "res://scenes/battle/units/life_wisp_3d.tscn",
    };

    public static readonly UnitDefinition WaterSkimmer = new()
    {
        Id = UnitIds.WaterSkimmer,
        DisplayName = "Flying Water Skirmisher",
        Stats = new UnitStats
        {
            MaxHp = 70f,
            AttackDamage = 11f,
            AttackRange = 18f,
            AttackSpeed = 0.8f,
            MoveSpeed = 3.4f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Ranged,
        MovementLayer = MovementLayer.Air,
        TargetingProfile = UnitTargetingProfile.RangedStrafe,
        TargetingLayerFilter = TargetLayer.Both,
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Flying = new FlyingConfig { Altitude = 2.2f },
        Visual = new VisualConfig
        {
            SeparationRadius = 0.45f,
            HpBarOffsetY = 2.0f,
        },
        ScenePath = "res://scenes/battle/units/puff_3d.tscn",
    };

    public static readonly UnitDefinition WaterRedistributor = new()
    {
        Id = UnitIds.WaterRedistributor,
        DisplayName = "Water Redistributor",
        Stats = new UnitStats
        {
            MaxHp = 95f,
            AttackDamage = 0f,
            AttackRange = 12f,
            AttackSpeed = 0f,
            MoveSpeed = 2.2f,
            AggroRadius = 14f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.Passive,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "health_redistribution",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.HealthRedistributionPool,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 2.6f,
                Radius = 8f,
                TargetAffinity = AbilityTargetAffinity.Allies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.TransferHealth,
                        Value = 18f,
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.45f, DisplayScale = 0.88f },
        ScenePath = "res://scenes/battle/units/water_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition SlipperyMelee = new()
    {
        Id = UnitIds.SlipperyMelee,
        DisplayName = "Slippery Melee",
        Stats = new UnitStats
        {
            MaxHp = 105f,
            AttackDamage = 13f,
            AttackRange = 3.1f,
            AttackSpeed = 1.05f,
            MoveSpeed = 3.6f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "slippery_evasion",
                Trigger = UnitAbilityTrigger.OnSpawn,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.EvasionModifier,
                        Value = 0.14f,
                        DurationSeconds = -1f,
                        Lifetime = EffectLifetime.Persistent(),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.45f, DisplayScale = 0.92f },
        ScenePath = "res://scenes/battle/units/water_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition WaterRanged = new()
    {
        Id = UnitIds.WaterRanged,
        DisplayName = "Water Ranged",
        Stats = new UnitStats
        {
            MaxHp = 72f,
            AttackDamage = 13f,
            AttackRange = 20f,
            AttackSpeed = 0.85f,
            MoveSpeed = 2.6f,
            AggroRadius = 22f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.WindPuff),
        Visual = new VisualConfig { SeparationRadius = 0.38f, DisplayScale = 0.86f },
        ScenePath = "res://scenes/battle/units/water_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition BarbedInflator = new()
    {
        Id = UnitIds.BarbedInflator,
        DisplayName = "Barbed Inflator",
        Stats = new UnitStats
        {
            MaxHp = 155f,
            AttackDamage = 11f,
            AttackRange = 3.0f,
            AttackSpeed = 0.75f,
            MoveSpeed = 2.1f,
            AggroRadius = 18f,
        },
        UnitType = UnitType.Melee,
        TargetingProfile = UnitTargetingProfile.MeleeGround,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "barbed_pulse",
                Trigger = UnitAbilityTrigger.OnDamaged,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3.2f,
                Radius = 4.5f,
                TargetAffinity = AbilityTargetAffinity.Enemies,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Damage,
                        Value = 12f,
                        DamageType = DamageType.Physical,
                    },
                ],
            },
            new UnitAbilityConfig
            {
                AbilityId = "inflated_guard",
                Trigger = UnitAbilityTrigger.OnDamaged,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 5.5f,
                Effects =
                [
                    new UnitAbilityEffectConfig
                    {
                        EffectType = EffectType.Shield,
                        Value = 24f,
                        DurationSeconds = 4f,
                        Lifetime = EffectLifetime.Timed(4f),
                    },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.6f, DisplayScale = 1.1f },
        ScenePath = "res://scenes/battle/units/water_roster_placeholder_3d.tscn",
    };

    public static readonly UnitDefinition LifeMedic = new()
    {
        Id = UnitIds.LifeMedic,
        DisplayName = "Life Medic",
        Stats = new UnitStats
        {
            MaxHp = 75f,
            AttackDamage = 0f,
            AttackRange = 16f,
            AttackSpeed = 0f,
            MoveSpeed = 2.1f,
            AggroRadius = 16f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.Passive,
        Abilities =
        [
            new UnitAbilityConfig
            {
                AbilityId = "healer_bullet",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.LowestHpAlly,
                Delivery = UnitAbilityDelivery.Projectile,
                CooldownSeconds = 1.2f,
                Range = 18f,
                ProjectileId = ProjectileIds.HealingBolt,
                TargetAffinity = AbilityTargetAffinity.Allies,
                Effects =
                [
                    new UnitAbilityEffectConfig { EffectType = EffectType.Heal, Value = 14f },
                ],
            },
        ],
        Visual = new VisualConfig { SeparationRadius = 0.4f },
        ScenePath = "res://scenes/battle/units/life_wisp_3d.tscn",
    };

    public static readonly UnitDefinition PoisonNeedler = new()
    {
        Id = UnitIds.PoisonNeedler,
        DisplayName = "Poison Needler",
        Stats = new UnitStats
        {
            MaxHp = 65f,
            AttackDamage = 10f,
            AttackRange = 20f,
            AttackSpeed = 0.85f,
            MoveSpeed = 2.5f,
            AggroRadius = 20f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.PoisonNeedle)
        {
            Impact = new ProjectileImpactConfig
            {
                TargetAffinity = AbilityTargetAffinity.Enemies,
                ImpactKind = ProjectileImpactKind.Damage,
                Status = new ProjectileStatusConfig
                {
                    Kind = StatusEffectKind.Poison,
                    DurationSeconds = 4.0f,
                    TickIntervalSeconds = 1.0f,
                    PotencyPerStack = 2.0f,
                    MaxStacks = 5,
                },
            },
        },
        Visual = new VisualConfig { SeparationRadius = 0.35f },
        ScenePath = "res://scenes/battle/units/fire_spider_3d.tscn",
    };

    public static readonly UnitDefinition PiercingLaser = new()
    {
        Id = UnitIds.PiercingLaser,
        DisplayName = "Piercing Laser",
        Stats = new UnitStats
        {
            MaxHp = 70f,
            AttackDamage = 16f,
            AttackRange = 22f,
            AttackSpeed = 0.55f,
            MoveSpeed = 2.4f,
            AggroRadius = 22f,
        },
        UnitType = UnitType.Ranged,
        TargetingProfile = UnitTargetingProfile.RangedGround,
        Ranged = new RangedConfig(ProjectileIds.LaserBeam),
        Visual = new VisualConfig { SeparationRadius = 0.4f },
        ScenePath = "res://scenes/battle/units/mama_duck_3d.tscn",
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
        [UnitIds.CinderCaster] = CinderCaster,
        [UnitIds.EmberBombCarrier] = EmberBombCarrier,
        [UnitIds.KindlingSwarmUnit] = KindlingSwarmUnit,
        [UnitIds.FireFrontliner] = FireFrontliner,
        [UnitIds.OverheatBrawler] = OverheatBrawler,
        [UnitIds.FlameChanneler] = FlameChanneler,
        // Earth
        [UnitIds.EarthSprite] = EarthSprite,
        [UnitIds.EarthKomodoDragon] = EarthKomodoDragon,
        [UnitIds.Rock] = Rock,
        [UnitIds.StoneApe] = StoneApe,
        [UnitIds.EarthRockThrower] = EarthRockThrower,
        [UnitIds.EarthFlatDamageReductionTank] = EarthFlatDamageReductionTank,
        [UnitIds.EarthBulletUnit] = EarthBulletUnit,
        [UnitIds.TauntPulseGuardian] = TauntPulseGuardian,
        [UnitIds.EarthShieldSupport] = EarthShieldSupport,
        [UnitIds.BurrowAmbusher] = BurrowAmbusher,
        // Wind
        [UnitIds.Puff] = Puff,
        [UnitIds.WindEvasionTank] = WindEvasionTank,
        [UnitIds.WindPushbackUnit] = WindPushbackUnit,
        [UnitIds.WindCleaveUnit] = WindCleaveUnit,
        [UnitIds.WindDiver] = WindDiver,
        [UnitIds.WindSpeedSupport] = WindSpeedSupport,
        [UnitIds.WindMissSupport] = WindMissSupport,
        [UnitIds.WindSwarmUnit] = WindSwarmUnit,
        [UnitIds.DashStriker] = DashStriker,
        // Water
        [UnitIds.WaterFrog] = WaterFrog,
        [UnitIds.MamaDuck] = MamaDuck,
        [UnitIds.Duckling] = Duckling,
        [UnitIds.WaterBulwark] = WaterBulwark,
        [UnitIds.WaterMender] = WaterMender,
        [UnitIds.WaterSkimmer] = WaterSkimmer,
        [UnitIds.WaterRedistributor] = WaterRedistributor,
        [UnitIds.SlipperyMelee] = SlipperyMelee,
        [UnitIds.WaterRanged] = WaterRanged,
        [UnitIds.BarbedInflator] = BarbedInflator,
        [UnitIds.LifeMedic] = LifeMedic,
        [UnitIds.PoisonNeedler] = PoisonNeedler,
        [UnitIds.PiercingLaser] = PiercingLaser,
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
    public static SimUnitTemplate BuildSimTemplate(
        UnitId unitId,
        int count,
        StatModifier? modifier = null
    )
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
        template.TargetPriority = def.TargetPriority;
        template.MovementLayer = def.MovementLayer;
        template.CombatTags = def.CombatTags.ToList();
        template.ElementId = (int)(def.DamageProfile.Element ?? Fateforged.Cards.Element.Neutral);
        template.PhysicalDamageRatio = def.DamageProfile.PhysicalRatio;
        template.ElementalDamageRatio = def.DamageProfile.ElementalRatio;
        template.AttackType =
            def.DamageProfile.ElementalRatio > 0f && def.DamageProfile.PhysicalRatio <= 0f
                ? Fateforged.Simulation.Enums.DamageType.Magic
                : Fateforged.Simulation.Enums.DamageType.Physical;
        template.SeparationRadius = def.Visual.SeparationRadius;
        template.NavigationRadius = def.Visual.SeparationRadius;
        template.HurtboxRadius =
            def.Visual.Hurtbox?.Radius > 0f ? def.Visual.Hurtbox.Radius : template.NavigationRadius;
        template.HurtboxHeight = def.Visual.Hurtbox?.Height > 0f ? def.Visual.Hurtbox.Height : 0f;
        template.HurtboxHorizontal = def.Visual.Hurtbox?.Horizontal ?? false;
        template.HurtboxOffset =
            def.Visual.Hurtbox != null
                ? new SimVector3(
                    def.Visual.Hurtbox.Offset.X,
                    def.Visual.Hurtbox.Offset.Y,
                    def.Visual.Hurtbox.Offset.Z
                )
                : SimVector3.Zero;
        template.PhysicalDefense = stats.Armor;
        template.MagicDefense = stats.MagicResist;
        template.Attack = AttackVectorStateBuilder.Build(def.Attack);
        template.Abilities = BuildAbilityStates(def.Abilities);

        // Ranged config
        if (def.Ranged != null)
        {
            template.ProjectileCatalogId = def.Ranged.ProjectileId.Value;
            template.ProjectileDelay = def.Ranged.ProjectileDelay;
            template.ProjectileTargetAffinity = def.Ranged.Impact.TargetAffinity;
            template.ProjectileImpactKind = def.Ranged.Impact.ImpactKind;
            if (def.Ranged.Impact.Status != null)
            {
                template.ProjectileStatusKind = def.Ranged.Impact.Status.Kind;
                template.ProjectileStatusDuration = def.Ranged.Impact.Status.DurationSeconds;
                template.ProjectileStatusTickInterval = def.Ranged.Impact.Status.TickIntervalSeconds;
                template.ProjectileStatusPotencyPerStack = def.Ranged.Impact.Status.PotencyPerStack;
                template.ProjectileStatusMaxStacks = Math.Max(1, def.Ranged.Impact.Status.MaxStacks);
            }
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
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.RangedGround:
                template.FallbackMovement = FallbackMovement.MoveToward;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(
                    def,
                    UnitTargetingProfile.RangedGround
                );
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.RangedStrafe:
                template.FallbackMovement = FallbackMovement.Strafe;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(
                    def,
                    UnitTargetingProfile.RangedStrafe
                );
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            case UnitTargetingProfile.FlyingConeStrafe:
                template.FallbackMovement = FallbackMovement.Strafe;
                template.TargetLayerFilter = ResolveTargetLayerFilterForProfile(
                    def,
                    UnitTargetingProfile.FlyingConeStrafe
                );
                template.EngageShape = EngageShape.Cone;
                template.HasConeConstraint = true;
                template.ConeHalfAngle = def.TargetingConeHalfAngle;
                template.ConeCenterOffsetDegrees = def.TargetingConeCenterOffsetDegrees;
                template.CloseRangeThreshold = def.TargetingCloseRangeThreshold;
                template.MovementIntentStrategy = MovementIntentStrategy.Context;
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(def.TargetingProfile),
                    def.TargetingProfile,
                    "Unknown UnitTargetingProfile"
                );
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

            case AttackSelectionMode.AreaCollect
                when template.Attack.Area.Shape == AttackAreaShape.Box:
                shouldOverride = true;
                length = template.Attack.Area.Size.X;
                halfWidth = template.Attack.Area.Size.Z;
                forwardOffset = template.Attack.Area.ForwardOffset;
                break;

            case AttackSelectionMode.AreaCollect
                when template.Attack.Area.Shape == AttackAreaShape.Line:
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
            // Forward-offset melee needs a close fallback at the offset band so units
            // don't endlessly circle just
            // outside the authored offset threshold.
            float offsetCloseRadius = template.EngageRectForwardOffset + 0.05f;
            template.EngageCloseRadius = MathF.Max(template.EngageCloseRadius, offsetCloseRadius);
        }
    }

    private static TargetLayer ResolveTargetLayerFilterForProfile(
        UnitDefinition def,
        UnitTargetingProfile profile
    )
    {
        bool isRangedProfile =
            profile == UnitTargetingProfile.RangedGround
            || profile == UnitTargetingProfile.RangedStrafe
            || profile == UnitTargetingProfile.FlyingConeStrafe;
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

    private static List<UnitAbilityState> BuildAbilityStates(List<UnitAbilityConfig> abilities)
    {
        if (abilities.Count == 0)
            return new List<UnitAbilityState>();

        var result = new List<UnitAbilityState>(abilities.Count);
        foreach (var ability in abilities)
        {
            result.Add(
                new UnitAbilityState
                {
                    AbilityId = ability.AbilityId,
                    Trigger = ability.Trigger,
                    Targeting = ability.Targeting,
                    Delivery = ability.Delivery,
                    CooldownSeconds = ability.CooldownSeconds,
                    CooldownTimer = 0f,
                    Range = ability.Range,
                    Radius = ability.Radius,
                    Value = ability.Value,
                    DurationSeconds = ability.DurationSeconds,
                    EffectType = ability.EffectType,
                    Lifetime = EffectLifetimeResolver.Resolve(ability.Lifetime, ability.DurationSeconds),
                    WindupSeconds = ability.WindupSeconds,
                    WindupTimer = 0f,
                    DeliveryDelaySeconds = ability.DeliveryDelaySeconds,
                    RepeatCount = ability.RepeatCount,
                    RepeatIntervalSeconds = ability.RepeatIntervalSeconds,
                    LockedTargetUnitId = null,
                    ProjectileCatalogId = ability.ProjectileId.Value,
                    TargetAffinity = ability.TargetAffinity,
                    Effects = BuildAbilityEffectStates(ability),
                    TagRequirements = ability.TagRequirements.DeepClone(),
                    CueId = ability.CueId,
                }
            );
        }
        return result;
    }

    private static List<UnitAbilityEffectState> BuildAbilityEffectStates(UnitAbilityConfig ability)
    {
        var effects = ability.Effects.Length > 0
            ? ability.Effects
            :
            [
                new UnitAbilityEffectConfig
                {
                    EffectType = ability.EffectType,
                    Value = ability.Value,
                    DurationSeconds = ability.DurationSeconds,
                    Lifetime = ability.Lifetime,
                },
            ];

        var result = new List<UnitAbilityEffectState>(effects.Length);
        foreach (var effect in effects)
        {
            result.Add(
                new UnitAbilityEffectState
                {
                    EffectType = effect.EffectType,
                    Value = effect.Value,
                    DurationSeconds = effect.DurationSeconds,
                    Lifetime = EffectLifetimeResolver.Resolve(
                        effect.Lifetime,
                        effect.DurationSeconds
                    ),
                    DamageType = effect.DamageType,
                    StatusKind = effect.Status?.Kind ?? StatusEffectKind.None,
                    StatusDuration = effect.Status?.DurationSeconds ?? 0f,
                    StatusTickInterval = effect.Status?.TickIntervalSeconds ?? 1f,
                    StatusPotencyPerStack = effect.Status?.PotencyPerStack ?? 0f,
                    StatusMaxStacks = effect.Status?.MaxStacks ?? 1,
                    TagRequirements = effect.TagRequirements.DeepClone(),
                    GrantedTags = effect.GrantedTags.ToList(),
                    StackPolicy = effect.StackPolicy,
                    StackKey = effect.StackKey,
                    CueId = effect.CueId,
                }
            );
        }
        return result;
    }
}
