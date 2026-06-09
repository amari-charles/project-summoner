using System.Collections.Generic;
using Fateforged.Cards.Formations;
using Fateforged.Cards.Spawning;
using Fateforged.Constants;
using Fateforged.Projectiles;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Stats;
using Fateforged.Units;
using Fateforged.Vfx;

namespace Fateforged.Cards;

/// <summary>
/// Central registry of all card definitions as static readonly fields.
/// Provides type-safe card definitions and lookup methods.
/// Follows the same pattern as UnitDefinitions for consistency.
/// </summary>
public static class CardDefinitions
{
    // =========================================================================
    // SPELLS
    // =========================================================================

    public static readonly CardDefinition Fireball = new()
    {
        Id = CardIds.Fireball,
        Name = "Fireball",
        Description =
            "Unleash a devastating explosion of flame. Deals area damage to all enemies caught in the blast.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellDamage = 100.0f,
        SpellRadius = 10.0f,
        SpellDuration = 0.5f,
        ProjectileId = ProjectileIds.Fireball,
        SpellVfx = VfxIds.FireballSpell,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 100f,
                DamageType = DamageType.Magic,
                RadiusOverride = 10f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition Rally = new()
    {
        Id = CardIds.Rally,
        Name = "Rally",
        Description =
            "Command nearby units to move to a target location and defend that zone until enemies are cleared.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 0,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        CommandType = CommandType.Rally,
        SelectionRadius = 8.0f,
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition Guard = new()
    {
        Id = CardIds.Guard,
        Name = "Guard",
        Description =
            "Command nearby units to form a defensive formation for 25 seconds. Melee units protect ranged units in the back line.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 0,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        CommandType = CommandType.Guard,
        SelectionRadius = 8.0f,
        FormationDuration = 25.0f,
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition Charge = new()
    {
        Id = CardIds.Charge,
        Name = "Charge",
        Description =
            "Command nearby units to launch a coordinated attack on the closest enemy (unit, structure, or base) to the target location for 30 seconds.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 0,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        CommandType = CommandType.Charge,
        SelectionRadius = 8.0f,
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition ManaBolt = new()
    {
        Id = CardIds.ManaBolt,
        Name = "Mana Bolt",
        Description = "Fire a bolt of arcane energy at the nearest enemy.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 1.5f,
        SummonTime = 0.0f,
        SpellDamage = 60.0f,
        ProjectileId = ProjectileIds.ManaBolt,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 60f,
                DamageType = DamageType.Magic,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
    };

    public static readonly CardDefinition MagicBolt = new()
    {
        Id = CardIds.MagicBolt,
        Name = "Magic Bolt",
        Description = "A simple bolt for teaching basic spell timing.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 2,
        Cooldown = 1.5f,
        SummonTime = 0.0f,
        SpellDamage = 35.0f,
        ProjectileId = ProjectileIds.ManaBolt,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 35f,
                DamageType = DamageType.Magic,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
    };

    public static readonly CardDefinition WeavingBolt = new()
    {
        Id = CardIds.WeavingBolt,
        Name = "TEST - Weaving Bolt",
        Description = "Fire a serpentine bolt that weaves through the air toward the target.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        SpellDamage = 50.0f,
        ProjectileId = ProjectileIds.WeavingBolt,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 50f,
                DamageType = DamageType.Magic,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.DevOnly,
    };

    public static readonly CardDefinition HealingField = new()
    {
        Id = CardIds.HealingField,
        Name = "Healing Field",
        Description = "Restore health to allied units in a targeted area.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellDamage = 65.0f,
        SpellRadius = 8.0f,
        SpellDuration = 0.0f,
        SpellCategory = SpellCategory.Heal,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Heal,
                Value = 65f,
                DamageType = DamageType.Magic,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Allies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Life,
    };

    public static readonly CardDefinition Cleanse = new()
    {
        Id = CardIds.Cleanse,
        Name = "Cleanse",
        Description = "Washes away debuffs from allies and restores a small amount of health.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellDamage = 25.0f,
        SpellRadius = 7.0f,
        SpellDuration = 0.0f,
        SpellVfx = VfxIds.CleanseSpell,
        SpellCategory = SpellCategory.Heal,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Heal,
                Value = 25f,
                DamageType = DamageType.Magic,
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Cleanse,
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterJet = new()
    {
        Id = CardIds.WaterJet,
        Name = "Water Jet",
        Description = "Fires a high-pressure water beam at one target and shoves it backward.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 1.5f,
        SummonTime = 0.0f,
        SpellDamage = 40.0f,
        SpellDuration = 0.0f,
        SpellVfx = VfxIds.WaterJetSpell,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 40f,
                DamageType = DamageType.Magic,
                Affinity = SpellAffinity.Enemies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Knockback,
                Value = 3f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition RainField = new()
    {
        Id = CardIds.RainField,
        Name = "Rain Field",
        Description = "Creates a rain zone that slows enemies and hits them with repeated water pulses.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.4f,
        SummonTime = 0.0f,
        SpellRadius = 8.0f,
        SpellDuration = 3.0f,
        SpellVfx = VfxIds.RainFieldSpell,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Slow,
                Value = 0.25f,
                Duration = 3.0f,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 5f,
                DamageType = DamageType.Magic,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                DelaySeconds = 0.6f,
                RepeatCount = 4,
                RepeatIntervalSeconds = 0.6f,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition TailWind = new()
    {
        Id = CardIds.TailWind,
        Name = "Tail Wind",
        Description =
            "Create a square wind zone. Allies inside attack faster, enemies inside attack slower.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellRadius = 6.0f,
        SpellDuration = 4.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.AttackSpeedModifier,
                Value = 0.25f,
                Duration = 4.0f,
                Lifetime = EffectLifetime.Timed(4.0f),
                RadiusOverride = 6f,
                AreaShape = SpellAreaShape.Square,
                Affinity = SpellAffinity.Allies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.AttackSpeedModifier,
                Value = -0.25f,
                Duration = 4.0f,
                Lifetime = EffectLifetime.Timed(4.0f),
                RadiusOverride = 6f,
                AreaShape = SpellAreaShape.Square,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition Fortify = new()
    {
        Id = CardIds.Fortify,
        Name = "Fortify",
        Description =
            "Reinforce allies in an area with flat damage reduction. This spell does not heal.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellDuration = 4.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.FlatDamageReduction,
                Value = 4f,
                Duration = 4.0f,
                Lifetime = EffectLifetime.Timed(4.0f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition FireAreaBurn = new()
    {
        Id = CardIds.FireAreaBurn,
        Name = "Fire Area Burn",
        Description = "Ignites enemies in an area with stacking burn.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellDuration = 4.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.StatusApply,
                Duration = 4f,
                Lifetime = EffectLifetime.Timed(4f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Enemies,
                StatusKind = StatusEffectKind.Burn,
                StatusTickInterval = 1f,
                StatusPotencyPerStack = 4f,
                StatusMaxStacks = 5,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition BurnCashout = new()
    {
        Id = CardIds.BurnCashout,
        Name = "Burn Cashout",
        Description = "Consumes burn on enemies in an area and deals the remaining burn value at bonus force.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellVfx = VfxIds.SpellAreaBurst,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.StatusConsume,
                Value = 1.5f,
                DamageType = DamageType.Magic,
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Enemies,
                StatusKind = StatusEffectKind.Burn,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition Overheat = new()
    {
        Id = CardIds.Overheat,
        Name = "Overheat",
        Description = "Pushes allied units past their limit with temporary damage and attack speed, then singes them.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 0.0f,
        SpellRadius = 6.0f,
        SpellDuration = 5.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.DamageBoost,
                Value = 0.28f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
                RadiusOverride = 6f,
                Affinity = SpellAffinity.Allies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.AttackSpeedModifier,
                Value = 0.25f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
                RadiusOverride = 6f,
                Affinity = SpellAffinity.Allies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 12f,
                DamageType = DamageType.True,
                RadiusOverride = 6f,
                Affinity = SpellAffinity.Allies,
                DelaySeconds = 5f,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition IgnitionMark = new()
    {
        Id = CardIds.IgnitionMark,
        Name = "Ignition Mark",
        Description = "Marks one enemy with a delayed burst and short burn window.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellVfx = VfxIds.SpellSingleTarget,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.StatModifier,
                Duration = 4f,
                Lifetime = EffectLifetime.Timed(4f),
                Affinity = SpellAffinity.Enemies,
                RemovalEffect = new BuffRemovalEffectConfig
                {
                    TriggerOnOwnerDeath = true,
                    EffectType = EffectType.Damage,
                    Value = 15f,
                    ScaleValueByOwnerHpAtApply = true,
                    OwnerHpAtApplyMultiplier = 0.25f,
                    DamageType = DamageType.Magic,
                    Radius = 4.5f,
                    Affinity = SpellAffinity.Enemies,
                },
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.StatusApply,
                Duration = 4f,
                Lifetime = EffectLifetime.Timed(4f),
                Affinity = SpellAffinity.Enemies,
                StatusKind = StatusEffectKind.Burn,
                StatusTickInterval = 1f,
                StatusPotencyPerStack = 4f,
                StatusMaxStacks = 4,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition FlareShield = new()
    {
        Id = CardIds.FlareShield,
        Name = "Flare Shield",
        Description = "Shields allies briefly, then releases a flare at the cast point.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 0.0f,
        SpellRadius = 5.5f,
        SpellDuration = 3.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Shield,
                Value = 35f,
                Duration = 3f,
                Lifetime = EffectLifetime.Timed(3f),
                RadiusOverride = 5.5f,
                Affinity = SpellAffinity.Allies,
                RemovalEffect = new BuffRemovalEffectConfig
                {
                    TriggerOnExpire = true,
                    TriggerOnShieldBreak = true,
                    EffectType = EffectType.Damage,
                    Value = 28f,
                    DamageType = DamageType.Magic,
                    Radius = 5.5f,
                    Affinity = SpellAffinity.Enemies,
                },
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition BubbleShield = new()
    {
        Id = CardIds.BubbleShield,
        Name = "Bubble Shield",
        Description = "Adds a protective shield to allied units in an area.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellDuration = 4.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Shield,
                Value = 45f,
                Duration = 4f,
                Lifetime = EffectLifetime.Timed(4f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition Whirlpool = new()
    {
        Id = CardIds.Whirlpool,
        Name = "Whirlpool",
        Description = "Pulls enemies toward the center of a water field while wearing them down.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.6f,
        SummonTime = 0.0f,
        SpellRadius = 8.0f,
        SpellDuration = 3.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Displacement,
                Value = -2.2f,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                RepeatCount = 4,
                RepeatIntervalSeconds = 0.6f,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 7f,
                DamageType = DamageType.Magic,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                RepeatCount = 4,
                RepeatIntervalSeconds = 0.6f,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition Flow = new()
    {
        Id = CardIds.Flow,
        Name = "Flow",
        Description = "Allies in the area gain dodge chance and deal more damage for a short time.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellDuration = 5.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.EvasionModifier,
                Value = 0.18f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.DamageBoost,
                Value = 0.18f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Allies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition Quake = new()
    {
        Id = CardIds.Quake,
        Name = "Quake",
        Description = "Damages and briefly stuns enemies in an earth impact zone.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.6f,
        SummonTime = 0.0f,
        SpellRadius = 8.0f,
        SpellDuration = 1.0f,
        SpellVfx = VfxIds.SpellAreaBurst,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 45f,
                DamageType = DamageType.Physical,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                TargetLayerFilter = TargetLayer.GroundOnly,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Stun,
                Value = 1f,
                Duration = 1f,
                Lifetime = EffectLifetime.Timed(1f),
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                TargetLayerFilter = TargetLayer.GroundOnly,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition StoneSpike = new()
    {
        Id = CardIds.StoneSpike,
        Name = "Stone Spike",
        Description = "Deals heavy single-target earth damage.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 0.0f,
        SpellVfx = VfxIds.SpellSingleTarget,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 80f,
                DamageType = DamageType.Physical,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition GravityWell = new()
    {
        Id = CardIds.GravityWell,
        Name = "Gravity Well",
        Description = "Pulls enemies inward and makes their attacks slower.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.8f,
        SummonTime = 0.0f,
        SpellRadius = 8.0f,
        SpellDuration = 4.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Displacement,
                Value = -1.4f,
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
                RepeatCount = 3,
                RepeatIntervalSeconds = 0.8f,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.AttackSpeedModifier,
                Value = -0.25f,
                Duration = 4f,
                Lifetime = EffectLifetime.Timed(4f),
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition ReformEarth = new()
    {
        Id = CardIds.ReformEarth,
        Name = "Reform Earth",
        Description = "Prepares nearby allied units to revive once at half health if they fall.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 0.0f,
        SpellRadius = 5.0f,
        SpellDuration = 6.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.ReviveOnDeath,
                Value = 0.5f,
                Duration = 6f,
                Lifetime = EffectLifetime.Timed(6f),
                RadiusOverride = 5f,
                Affinity = SpellAffinity.Allies,
                RequiredTargetElement = Element.Earth,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthenGrip = new()
    {
        Id = CardIds.EarthenGrip,
        Name = "Earthen Grip",
        Description = "Roots one enemy in place and deals light damage.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 0.0f,
        SpellDuration = 3.0f,
        SpellVfx = VfxIds.SpellSingleTarget,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Root,
                Value = 1f,
                Duration = 3f,
                Lifetime = EffectLifetime.Timed(3f),
                Affinity = SpellAffinity.Enemies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 24f,
                DamageType = DamageType.Physical,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition Tornado = new()
    {
        Id = CardIds.Tornado,
        Name = "Tornado",
        Description = "Lifts enemies into a wind vortex, carrying them in a circle while dealing repeated damage.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 5,
        Cooldown = 2.8f,
        SummonTime = 0.0f,
        SpellRadius = 7.0f,
        SpellDuration = 3.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.TornadoCarry,
                Value = 5.2f,
                Duration = 0.75f,
                Lifetime = EffectLifetime.Timed(0.75f),
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Enemies,
                RepeatCount = 4,
                RepeatIntervalSeconds = 0.55f,
                StackPolicy = EffectStackPolicy.RefreshDuration,
                StackKey = "tornado_carry",
                CueId = "spell.tornado.carry",
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 8f,
                DamageType = DamageType.Magic,
                RadiusOverride = 7f,
                Affinity = SpellAffinity.Enemies,
                RepeatCount = 4,
                RepeatIntervalSeconds = 0.55f,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition Crosswind = new()
    {
        Id = CardIds.Crosswind,
        Name = "Crosswind",
        Description = "A long-lasting wind field that reduces enemy ranged damage.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 0.0f,
        SpellRadius = 8.0f,
        SpellDuration = 15.0f,
        SpellVfx = VfxIds.SpellAreaField,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.RangedDamageModifier,
                Value = -0.35f,
                Duration = 15f,
                Lifetime = EffectLifetime.Timed(15f),
                RadiusOverride = 8f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition AirBullet = new()
    {
        Id = CardIds.AirBullet,
        Name = "Air Bullet",
        Description = "Hits one enemy with wind damage and knocks it away.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 1.8f,
        SummonTime = 0.0f,
        SpellVfx = VfxIds.SpellSingleTarget,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 42f,
                DamageType = DamageType.Magic,
                Affinity = SpellAffinity.Enemies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Knockback,
                Value = 4f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition Evacuate = new()
    {
        Id = CardIds.Evacuate,
        Name = "Evacuate",
        Description = "Pushes enemies away from the target point.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 0.0f,
        SpellRadius = 6.5f,
        SpellVfx = VfxIds.SpellAreaBurst,
        SpellCategory = SpellCategory.None,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Displacement,
                Value = 5f,
                RadiusOverride = 6.5f,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindShear = new()
    {
        Id = CardIds.WindShear,
        Name = "Wind Shear",
        Description = "Cuts a line of wind through enemies and pushes them off course.",
        Rarity = Rarity.Rare,
        Type = CardType.Spell,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 0.0f,
        SpellRadius = 10.0f,
        SpellVfx = VfxIds.SpellLine,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        SpellEffects =
        [
            new SpellEffectDefinition
            {
                EffectType = EffectType.Damage,
                Value = 42f,
                DamageType = DamageType.Magic,
                RadiusOverride = 10f,
                AreaShape = SpellAreaShape.Line,
                Affinity = SpellAffinity.Enemies,
            },
            new SpellEffectDefinition
            {
                EffectType = EffectType.Displacement,
                Value = 2.5f,
                RadiusOverride = 10f,
                AreaShape = SpellAreaShape.Line,
                Affinity = SpellAffinity.Enemies,
            },
        ],
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    // =========================================================================
    // ACADEMY PLAYTEST PLACEHOLDERS
    // =========================================================================

    public static readonly CardDefinition NeutralStarterUnit = new()
    {
        Id = CardIds.NeutralStarterUnit,
        Name = "Neutral Starter Unit",
        Description = "A simple unit for teaching basic summoning.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.NeutralStarterUnit,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
    };

    public static readonly CardDefinition TrainingTarget = new()
    {
        Id = CardIds.TrainingTarget,
        Name = "Training Target",
        Description = "A harmless target for teaching basic summoning.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 1,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.TrainingTarget,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.None,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
    };

    public static readonly CardDefinition WeakEnemyUnit = new()
    {
        Id = CardIds.WeakEnemyUnit,
        Name = "Weak Enemy Unit",
        Description = "A simple enemy unit for early combat practice.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WeakEnemyUnit,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
    };

    // =========================================================================
    // WISPS (Basic starter units for each element)
    // =========================================================================

    public static readonly CardDefinition FireWisp = new()
    {
        Id = CardIds.FireWisp,
        Name = "Fire Wisp",
        Description =
            "A teardrop of living flame. Drifts across the battlefield, burning all in its path.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.FireWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition WaterWisp = new()
    {
        Id = CardIds.WaterWisp,
        Name = "Water Wisp",
        Description = "A teardrop of living water. Flows across the battlefield with fluid grace.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WindWisp = new()
    {
        Id = CardIds.WindWisp,
        Name = "Wind Wisp",
        Description =
            "A teardrop of swirling wind. Darts across the battlefield with elusive speed.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition EarthWisp = new()
    {
        Id = CardIds.EarthWisp,
        Name = "Earth Wisp",
        Description =
            "A teardrop of compacted stone. Moves with sturdy determination across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition LightningWisp = new()
    {
        Id = CardIds.LightningWisp,
        Name = "Lightning Wisp",
        Description =
            "A teardrop of crackling energy. Strikes across the battlefield with shocking speed.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.LightningWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Lightning,
    };

    public static readonly CardDefinition LifeWisp = new()
    {
        Id = CardIds.LifeWisp,
        Name = "Life Wisp",
        Description =
            "A teardrop of living essence. Glows warmly as it drifts across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.LifeWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Life,
    };

    public static readonly CardDefinition DeathWisp = new()
    {
        Id = CardIds.DeathWisp,
        Name = "Death Wisp",
        Description =
            "A teardrop of spectral essence. Flickers between visible and ethereal as it haunts the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.DeathWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Death,
    };

    public static readonly CardDefinition ShadowWisp = new()
    {
        Id = CardIds.ShadowWisp,
        Name = "Shadow Wisp",
        Description =
            "A teardrop of living shadow. Shifts and fades as it stalks across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.ShadowWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Shadow,
    };

    public static readonly CardDefinition FireWispSwarm = new()
    {
        Id = CardIds.FireWispSwarm,
        Name = "Fire Wisp Swarm",
        Description =
            "Unleash a horde of flame wisps. Twelve smaller fire wisps surge forth to overwhelm the enemy.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 7,
        Cooldown = 4.0f,
        SummonTime = 2.5f,
        UnitId = UnitIds.FireWisp,
        UnitModifier = new StatModifier
        {
            Source = "card_swarm_variant",
            StatMults = new Dictionary<StatKey, float>
            {
                [StatKey.MaxHp] = 0.75f,
                [StatKey.AttackDamage] = 0.75f,
            },
        },
        SpawnCount = 12,
        Formation = FormationPresets.TightSwarmGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Swarm,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived,
    };

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition FireTitan = new()
    {
        Id = CardIds.FireTitan,
        Name = "Fire Titan",
        Description =
            "A colossal spirit of ancient flame. Towers over the battlefield, absorbing damage while scorching all who approach.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 7,
        Cooldown = 3.0f,
        SummonTime = 2.0f,
        UnitId = UnitIds.FireTitan,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Spirit,
        Roles = SummonRole.Tank | SummonRole.Giant,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition FireAnt = new()
    {
        Id = CardIds.FireAnt,
        Name = "Fire Ant",
        Description =
            "A swift and fierce fire ant. Scurries across the battlefield with blazing speed, overwhelming foes with relentless attacks.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 2,
        Cooldown = 1.5f,
        SummonTime = 0.8f,
        UnitId = UnitIds.FireAnt,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Insect,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition FireAntSwarm = new()
    {
        Id = CardIds.FireAntSwarm,
        Name = "Fire Ant Swarm",
        Description =
            "Release a colony of fire ants! Twenty tiny terrors surge forth in formation, overwhelming enemies with sheer numbers.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 6,
        Cooldown = 4.0f,
        SummonTime = 2.0f,
        UnitId = UnitIds.FireAnt,
        SpawnCount = 20,
        Formation = FormationPresets.FireAntSwarm,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Insect,
        Roles = SummonRole.Fast | SummonRole.Swarm,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition FireBoar = new()
    {
        Id = CardIds.FireBoar,
        Name = "Fire Boar",
        Description =
            "A charging bruiser wreathed in flame. Barrels through enemies with reckless aggression.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.2f,
        UnitId = UnitIds.FireBoar,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition FireWolf = new()
    {
        Id = CardIds.FireWolf,
        Name = "Fire Wolf",
        Description =
            "A blazing pack hunter. Sprints into melee and tears through enemies with rapid, fiery strikes.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.0f,
        SummonTime = 1.1f,
        UnitId = UnitIds.FireWolf,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition FireSpider = new()
    {
        Id = CardIds.FireSpider,
        Name = "Fire Spider",
        Description =
            "A skittering hunter that spins webs of flame. Its sticky projectiles slow enemies caught in its trap.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.FireSpider,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Insect,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition CinderCaster = new()
    {
        Id = CardIds.CinderCaster,
        Name = "Cinder Caster",
        Description = "Ranged fire unit whose attacks build burn stacks on a single target.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.CinderCaster,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition EmberBombCarrier = new()
    {
        Id = CardIds.EmberBombCarrier,
        Name = "Ember Bomb Carrier",
        Description = "Fast fragile melee unit that bursts on death and punishes clustered enemies.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 2,
        Cooldown = 1.8f,
        SummonTime = 0.8f,
        UnitId = UnitIds.EmberBombCarrier,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition KindlingSwarm = new()
    {
        Id = CardIds.KindlingSwarm,
        Name = "Kindling Swarm",
        Description = "A group of small, fast fire melee units for early pressure.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.8f,
        SummonTime = 1.1f,
        UnitId = UnitIds.KindlingSwarmUnit,
        SpawnCount = 5,
        Formation = FormationPresets.TightSwarmGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Swarm | SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition FireFrontliner = new()
    {
        Id = CardIds.FireFrontliner,
        Name = "Fire Frontliner",
        Description = "Simple fire tank that gives aggressive decks a durable body.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.6f,
        SummonTime = 1.2f,
        UnitId = UnitIds.FireFrontliner,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition OverheatBrawler = new()
    {
        Id = CardIds.OverheatBrawler,
        Name = "Overheat Brawler",
        Description = "Generalist fire fighter that grows stronger over time while burning itself down.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.1f,
        UnitId = UnitIds.OverheatBrawler,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    public static readonly CardDefinition FlameChanneler = new()
    {
        Id = CardIds.FlameChanneler,
        Name = "Flame Channeler",
        Description = "Shorter-ranged fire attacker that stacks small burns quickly on its current target.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.3f,
        SummonTime = 1.0f,
        UnitId = UnitIds.FlameChanneler,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
    };

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition Pebbloom = new()
    {
        Id = CardIds.Pebbloom,
        Name = "Pebbloom",
        Description =
            "A sturdy creature native to the elemental plane of earth. Pebblooms carry saplings that they nurture with elemental energy.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthSprite,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Nature,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthKomodoDragon = new()
    {
        Id = CardIds.EarthKomodoDragon,
        Name = "Earth Komodo Dragon",
        Description =
            "An ancient stone-backed predator. Heavy, relentless, and devastating up close.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 6,
        Cooldown = 3.0f,
        SummonTime = 1.6f,
        UnitId = UnitIds.EarthKomodoDragon,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast | CreatureType.Elemental,
        Roles = SummonRole.Tank | SummonRole.Giant,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition Rock = new()
    {
        Id = CardIds.Rock,
        Name = "Rock",
        Description = "A stationary target dummy for testing. Does not move or attack.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 0,
        Cooldown = 0.5f,
        SummonTime = 0.0f,
        UnitId = UnitIds.Rock,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        Roles = SummonRole.Stationary,
        Flags = CardFlags.DevOnly | CardFlags.Dummy | CardFlags.Archived,
        UnlockCondition = UnlockCondition.DevOnly,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition StoneApe = new()
    {
        Id = CardIds.StoneApe,
        Name = "Stone Ape",
        Description =
            "A massive gorilla made of living rock. Slow and deliberate, but devastating in close combat.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 6,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        UnitId = UnitIds.StoneApe,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthRockThrower = new()
    {
        Id = CardIds.EarthRockThrower,
        Name = "Rock Thrower",
        Description =
            "A tiny creature with impossible strength. Hurls boulders larger than itself at distant foes.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthRockThrower,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthFlatDamageReductionTank = new()
    {
        Id = CardIds.EarthFlatDamageReductionTank,
        Name = "Earth Flat Damage Reduction Tank",
        Description = "Frontline tank with a built-in flat damage reduction passive.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.8f,
        SummonTime = 1.2f,
        UnitId = UnitIds.EarthFlatDamageReductionTank,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthBulletUnit = new()
    {
        Id = CardIds.EarthBulletUnit,
        Name = "Earth Bullet Unit",
        Description = "Ranged unit that fires dense earth projectiles and slows targets on impact.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthBulletUnit,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition EarthShieldSupport = new()
    {
        Id = CardIds.EarthShieldSupport,
        Name = "Earth Shield Support",
        Description = "Support unit that periodically grants small shields to nearby allies.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthShieldSupport,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition BurrowAmbusher = new()
    {
        Id = CardIds.BurrowAmbusher,
        Name = "Burrow Ambusher",
        Description = "Fast earth melee unit with an intermittent stunning opening strike.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.0f,
        UnitId = UnitIds.BurrowAmbusher,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition Puff = new()
    {
        Id = CardIds.Puff,
        Name = "Puff",
        Description =
            "A mischievous cloud spirit that blows gusts of wind at its foes. Light and agile, it drifts across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.Puff,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Aerial,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition CloudSwarm = new()
    {
        Id = CardIds.CloudSwarm,
        Name = "Cloud Swarm",
        Description =
            "A swirling formation of cloud wisps. Six clouds drift together in pairs, overwhelming foes with their combined might.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        UnitId = UnitIds.Puff,
        SpawnCount = 6,
        Formation = FormationPresets.CloudSwarm,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Aerial,
        Roles = SummonRole.Swarm,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
        Flags = CardFlags.Archived,
    };

    public static readonly CardDefinition WindEvasionTank = new()
    {
        Id = CardIds.WindEvasionTank,
        Name = "Wind Evasion Tank",
        Description = "Frontline tank with a persistent evasion bonus.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.8f,
        SummonTime = 1.2f,
        UnitId = UnitIds.WindEvasionTank,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindPushbackUnit = new()
    {
        Id = CardIds.WindPushbackUnit,
        Name = "Wind Pushback Unit",
        Description = "Ranged unit with a targeted knockback ability on cooldown.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.2f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindPushbackUnit,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindCleaveUnit = new()
    {
        Id = CardIds.WindCleaveUnit,
        Name = "Wind Cleave Unit",
        Description = "Melee unit with a forward cleave attack profile.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.1f,
        UnitId = UnitIds.WindCleaveUnit,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindDiver = new()
    {
        Id = CardIds.WindDiver,
        Name = "Wind Diver",
        Description = "Fast fragile melee unit for backline pressure.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 0.9f,
        UnitId = UnitIds.WindDiver,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindSpeedSupport = new()
    {
        Id = CardIds.WindSpeedSupport,
        Name = "Wind Speed Support",
        Description = "Support unit that increases nearby allied attack speed.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindSpeedSupport,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindMissSupport = new()
    {
        Id = CardIds.WindMissSupport,
        Name = "Wind Miss Support",
        Description = "Support unit that makes nearby enemies more likely to miss.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindMissSupport,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition WindSwarm = new()
    {
        Id = CardIds.WindSwarm,
        Name = "Wind Swarm",
        Description = "A cluster of small fast wind melee units.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.7f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindSwarmUnit,
        SpawnCount = 5,
        Formation = FormationPresets.TightSwarmGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        Roles = SummonRole.Swarm | SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    public static readonly CardDefinition DashStriker = new()
    {
        Id = CardIds.DashStriker,
        Name = "Flow Striker",
        Description = "Fast melee striker that briefly gains dodge and attack speed after landing hits.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.3f,
        SummonTime = 1.0f,
        UnitId = UnitIds.DashStriker,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
    };

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition WaterFrog = new()
    {
        Id = CardIds.WaterFrog,
        Name = "Water Frog",
        Description =
            "A pudgy amphibian with a lightning-fast tongue. Strikes from a distance with surprising reach, snatching enemies before they can react.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterFrog,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Amphibian,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition MamaDuck = new()
    {
        Id = CardIds.MamaDuck,
        Name = "Mama Duck",
        Description =
            "A protective mother duck and her ducklings. Mama fights in melee while her babies pepper foes with water bullets.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        // SummonSpec replaces UnitId/SpawnCount for multi-unit spawning
        // Note: SummonTime/Cooldown come from CardDefinition, not SummonSpec
        Summon = new SummonSpec
        {
            Units =
            [
                new UnitSpawnEntry { UnitId = UnitIds.MamaDuck, Count = 1 },
                new UnitSpawnEntry
                {
                    UnitId = UnitIds.Duckling,
                    Count = 3,
                    Placement = SpawnPlacement.BehindLeader,
                    FollowsIndex = 0, // Ducklings follow mama's targeting
                    PlacementOffset = 1.5f,
                },
            ],
        },
        // Legacy fields kept for UI display compatibility
        UnitId = UnitIds.MamaDuck,
        SpawnCount = 4, // Total: 1 mama + 3 ducklings
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterBulwark = new()
    {
        Id = CardIds.WaterBulwark,
        Name = "Water Frontliner",
        Description = "Placeholder water tank slot: a heavy frontline unit that absorbs pressure for the team.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.6f,
        SummonTime = 1.2f,
        UnitId = UnitIds.WaterBulwark,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterMender = new()
    {
        Id = CardIds.WaterMender,
        Name = "Water Cleanser",
        Description = "Placeholder water support slot: periodically cleanses and lightly heals nearby allies.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.4f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterMender,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterSkimmer = new()
    {
        Id = CardIds.WaterSkimmer,
        Name = "Flying Water Skirmisher",
        Description = "Placeholder water flying slot: an aerial ranged unit that pressures enemies from above.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterSkimmer,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental | CreatureType.Aerial,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterRedistributor = new()
    {
        Id = CardIds.WaterRedistributor,
        Name = "Water Redistributor",
        Description = "Support unit that periodically shifts health among nearby allies toward the same HP percentage.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterRedistributor,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition SlipperyMelee = new()
    {
        Id = CardIds.SlipperyMelee,
        Name = "Slippery Melee",
        Description = "Mobile melee unit with a persistent dodge bonus.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.SlipperyMelee,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition WaterRanged = new()
    {
        Id = CardIds.WaterRanged,
        Name = "Water Ranged",
        Description = "Straightforward ranged water attacker.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterRanged,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    public static readonly CardDefinition BarbedInflator = new()
    {
        Id = CardIds.BarbedInflator,
        Name = "Barbed Inflator",
        Description = "Defensive water melee unit that periodically shields itself and damages nearby enemies.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.6f,
        SummonTime = 1.2f,
        UnitId = UnitIds.BarbedInflator,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water,
    };

    // =========================================================================
    // ABILITY SYSTEM V1 CARDS
    // =========================================================================

    public static readonly CardDefinition TauntPulseGuardian = new()
    {
        Id = CardIds.TauntPulseGuardian,
        Name = "Taunt Pulse Guardian",
        Description = "A durable frontline guardian that periodically taunts nearby enemies.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 1.2f,
        UnitId = UnitIds.TauntPulseGuardian,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth,
    };

    public static readonly CardDefinition LifeMedic = new()
    {
        Id = CardIds.LifeMedic,
        Name = "Life Medic",
        Description = "Support caster that fires healing projectiles at wounded allies.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.LifeMedic,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Life,
    };

    public static readonly CardDefinition PoisonNeedler = new()
    {
        Id = CardIds.PoisonNeedler,
        Name = "Poison Needler",
        Description = "Ranged attacker whose needles apply stacking poison.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.2f,
        SummonTime = 1.0f,
        UnitId = UnitIds.PoisonNeedler,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Beast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Poison,
    };

    public static readonly CardDefinition PiercingLaser = new()
    {
        Id = CardIds.PiercingLaser,
        Name = "Piercing Laser",
        Description = "Long-range shooter that fires a line beam through multiple enemies.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 2.5f,
        SummonTime = 1.1f,
        UnitId = UnitIds.PiercingLaser,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Lightning,
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    /// <summary>
    /// Lookup dictionary built from static fields.
    /// Uses CardId (which implicitly converts to string) as key.
    /// </summary>
    private static readonly Dictionary<string, CardDefinition> _lookup = new()
    {
        // Spells
        [CardIds.Fireball] = Fireball,
        [CardIds.Rally] = Rally,
        [CardIds.Guard] = Guard,
        [CardIds.Charge] = Charge,
        [CardIds.ManaBolt] = ManaBolt,
        [CardIds.MagicBolt] = MagicBolt,
        [CardIds.WeavingBolt] = WeavingBolt,
        [CardIds.HealingField] = HealingField,
        [CardIds.Cleanse] = Cleanse,
        [CardIds.WaterJet] = WaterJet,
        [CardIds.RainField] = RainField,
        [CardIds.TailWind] = TailWind,
        [CardIds.Fortify] = Fortify,
        [CardIds.FireAreaBurn] = FireAreaBurn,
        [CardIds.BurnCashout] = BurnCashout,
        [CardIds.Overheat] = Overheat,
        [CardIds.IgnitionMark] = IgnitionMark,
        [CardIds.FlareShield] = FlareShield,
        [CardIds.BubbleShield] = BubbleShield,
        [CardIds.Whirlpool] = Whirlpool,
        [CardIds.Flow] = Flow,
        [CardIds.Quake] = Quake,
        [CardIds.StoneSpike] = StoneSpike,
        [CardIds.GravityWell] = GravityWell,
        [CardIds.ReformEarth] = ReformEarth,
        [CardIds.EarthenGrip] = EarthenGrip,
        [CardIds.Tornado] = Tornado,
        [CardIds.Crosswind] = Crosswind,
        [CardIds.AirBullet] = AirBullet,
        [CardIds.Evacuate] = Evacuate,
        [CardIds.WindShear] = WindShear,

        // Academy playtest placeholders
        [CardIds.NeutralStarterUnit] = NeutralStarterUnit,
        [CardIds.TrainingTarget] = TrainingTarget,
        [CardIds.WeakEnemyUnit] = WeakEnemyUnit,

        // Wisps
        [CardIds.FireWisp] = FireWisp,
        [CardIds.WaterWisp] = WaterWisp,
        [CardIds.WindWisp] = WindWisp,
        [CardIds.EarthWisp] = EarthWisp,
        [CardIds.LightningWisp] = LightningWisp,
        [CardIds.LifeWisp] = LifeWisp,
        [CardIds.DeathWisp] = DeathWisp,
        [CardIds.ShadowWisp] = ShadowWisp,
        [CardIds.FireWispSwarm] = FireWispSwarm,

        // Fire units
        [CardIds.FireTitan] = FireTitan,
        [CardIds.FireAnt] = FireAnt,
        [CardIds.FireAntSwarm] = FireAntSwarm,
        [CardIds.FireBoar] = FireBoar,
        [CardIds.FireWolf] = FireWolf,
        [CardIds.FireSpider] = FireSpider,
        [CardIds.CinderCaster] = CinderCaster,
        [CardIds.EmberBombCarrier] = EmberBombCarrier,
        [CardIds.KindlingSwarm] = KindlingSwarm,
        [CardIds.FireFrontliner] = FireFrontliner,
        [CardIds.OverheatBrawler] = OverheatBrawler,
        [CardIds.FlameChanneler] = FlameChanneler,

        // Earth units
        [CardIds.Pebbloom] = Pebbloom,
        [CardIds.EarthKomodoDragon] = EarthKomodoDragon,
        [CardIds.Rock] = Rock,
        [CardIds.StoneApe] = StoneApe,
        [CardIds.EarthRockThrower] = EarthRockThrower,
        [CardIds.EarthFlatDamageReductionTank] = EarthFlatDamageReductionTank,
        [CardIds.EarthBulletUnit] = EarthBulletUnit,
        [CardIds.TauntPulseGuardian] = TauntPulseGuardian,
        [CardIds.EarthShieldSupport] = EarthShieldSupport,
        [CardIds.BurrowAmbusher] = BurrowAmbusher,

        // Wind units
        [CardIds.Puff] = Puff,
        [CardIds.CloudSwarm] = CloudSwarm,
        [CardIds.WindEvasionTank] = WindEvasionTank,
        [CardIds.WindPushbackUnit] = WindPushbackUnit,
        [CardIds.WindCleaveUnit] = WindCleaveUnit,
        [CardIds.WindDiver] = WindDiver,
        [CardIds.WindSpeedSupport] = WindSpeedSupport,
        [CardIds.WindMissSupport] = WindMissSupport,
        [CardIds.WindSwarm] = WindSwarm,
        [CardIds.DashStriker] = DashStriker,

        // Water units
        [CardIds.WaterFrog] = WaterFrog,
        [CardIds.MamaDuck] = MamaDuck,
        [CardIds.WaterBulwark] = WaterBulwark,
        [CardIds.WaterMender] = WaterMender,
        [CardIds.WaterSkimmer] = WaterSkimmer,
        [CardIds.WaterRedistributor] = WaterRedistributor,
        [CardIds.SlipperyMelee] = SlipperyMelee,
        [CardIds.WaterRanged] = WaterRanged,
        [CardIds.BarbedInflator] = BarbedInflator,
        [CardIds.LifeMedic] = LifeMedic,
        [CardIds.PoisonNeedler] = PoisonNeedler,
        [CardIds.PiercingLaser] = PiercingLaser,
    };

    /// <summary>Get a card definition by ID. Returns null if not found.</summary>
    public static CardDefinition? Get(CardId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get a card definition by string ID. Returns null if not found.</summary>
    public static CardDefinition? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Try to get a card definition by ID.</summary>
    public static bool TryGet(CardId id, out CardDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Try to get a card definition by string ID.</summary>
    public static bool TryGet(string id, out CardDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Check if a card exists.</summary>
    public static bool Has(CardId id) => _lookup.ContainsKey(id);

    /// <summary>Check if a card exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all card definitions.</summary>
    public static IReadOnlyCollection<CardDefinition> All => _lookup.Values;

    /// <summary>Get all card IDs.</summary>
    public static IReadOnlyCollection<string> AllIds => _lookup.Keys;

    /// <summary>Get card count.</summary>
    public static int Count => _lookup.Count;
}
