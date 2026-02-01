using System;
using System.Collections.Generic;
using ProjectSummoner.Cards.Effects.Concrete;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Cards.Effects.Targeting;

namespace ProjectSummoner.Cards;

/// <summary>
/// Factory for creating spell effects.
/// Provides type-safe construction of spell effects for the card catalog.
/// </summary>
public static class SpellBuilder
{
    // =========================================================================
    // SPELL DEFINITIONS
    // =========================================================================

    /// <summary>
    /// Check if a spell effect exists for the given catalog ID.
    /// Use this before calling GetEffect to avoid exceptions.
    /// Note: Example spells (FrostNova, Execute, SoulHarvest) are not included here
    /// as they are for documentation/demonstration purposes only.
    /// </summary>
    public static bool HasEffect(string catalogId)
    {
        return catalogId == CardIds.Fireball
            || catalogId == CardIds.Rally
            || catalogId == CardIds.Guard
            || catalogId == CardIds.Charge
            || catalogId == CardIds.ManaBolt;
    }

    /// <summary>
    /// Get the spell effect for a given card ID.
    /// Throws ArgumentException if catalogId is not a known spell.
    /// Use HasEffect() to check before calling if unknown IDs are possible.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when catalogId is not a registered spell.</exception>
    public static ISpellEffect GetEffect(string catalogId)
    {
        if (catalogId == CardIds.Fireball) return Fireball();
        if (catalogId == CardIds.Rally) return Rally();
        if (catalogId == CardIds.Guard) return Guard();
        if (catalogId == CardIds.Charge) return Charge();
        if (catalogId == CardIds.ManaBolt) return ManaBolt();

        throw new ArgumentException(
            $"[SpellBuilder] Unknown spell catalog ID: '{catalogId}'. " +
            $"Add spell definition to SpellBuilder or check spelling.",
            nameof(catalogId));
    }

    /// <summary>
    /// Create a Fireball spell effect.
    /// Deals damage in a circle around the target position.
    /// </summary>
    public static ISpellEffect Fireball(float damage = 100f, float radius = 10f)
    {
        return new DamageEffect
        {
            Targeting = new CircleTargeting(radius),
            Affinity = Affinity.Enemies,
            Damage = damage,
            DamageType = "fire",
            VFXId = "fireball_explosion"
        };
    }

    /// <summary>
    /// Create a Fireball spell effect with projectile delivery.
    /// Spawns a projectile that deals damage on impact.
    /// </summary>
    public static ISpellEffect FireballWithProjectile(float damage = 100f, float radius = 10f)
    {
        return new DamageEffect
        {
            Targeting = new CircleTargeting(radius),
            Affinity = Affinity.Enemies,
            Damage = damage,
            DamageType = "fire",
            ProjectileId = "fireball"
        };
    }

    /// <summary>
    /// Create a Rally spell effect.
    /// Selected allied units move to the target position.
    /// </summary>
    public static ISpellEffect Rally(float selectionRadius = 8f)
    {
        return new CommandEffect
        {
            Targeting = new CircleTargeting(selectionRadius),
            Affinity = Affinity.Allies,
            Command = CommandType.Rally
        };
    }

    /// <summary>
    /// Create a Guard spell effect.
    /// Selected allied units form defensive formation at the target position.
    /// </summary>
    public static ISpellEffect Guard(float selectionRadius = 8f, float duration = 25f)
    {
        return new CommandEffect
        {
            Targeting = new CircleTargeting(selectionRadius),
            Affinity = Affinity.Allies,
            Command = CommandType.Guard,
            CommandDuration = duration
        };
    }

    /// <summary>
    /// Create a Charge spell effect.
    /// Selected allied units focus fire on nearest enemy to target position.
    /// </summary>
    public static ISpellEffect Charge(float selectionRadius = 8f, float duration = 30f)
    {
        return new CommandEffect
        {
            Targeting = new CircleTargeting(selectionRadius),
            Affinity = Affinity.Allies,
            Command = CommandType.Charge,
            CommandDuration = duration
        };
    }

    /// <summary>
    /// Create a Mana Bolt spell effect.
    /// Fires a projectile at the nearest enemy unit.
    /// </summary>
    public static ISpellEffect ManaBolt(float damage = 60f)
    {
        return new DamageEffect
        {
            Targeting = new NearestEnemyTargeting(),
            Affinity = Affinity.Enemies,
            Damage = damage,
            DamageType = "arcane",
            ProjectileId = "mana_bolt"
        };
    }

    // =========================================================================
    // ADVANCED SPELL EXAMPLES (for future use)
    // =========================================================================

    /// <summary>
    /// Example: Frost Nova - freeze enemies then heal allies.
    /// Demonstrates CompositeEffect with sequencing.
    /// </summary>
    public static ISpellEffect FrostNova(float freezeDuration = 2f, float healAmount = 50f, float radius = 6f)
    {
        // Note: Would need HealEffect and DebuffEffect to be implemented
        // This is a placeholder showing the pattern
        return new CompositeEffect
        {
            Effects = new List<ISpellEffect>
            {
                // First effect: damage enemies (placeholder for freeze)
                new DamageEffect
                {
                    Targeting = new CircleTargeting(radius),
                    Affinity = Affinity.Enemies,
                    Damage = 25f,
                    DamageType = "frost"
                },
                // Second effect: after delay, would heal allies
                // (HealEffect not implemented yet)
            }
        };
    }

    /// <summary>
    /// Example: Execute - deals bonus damage to low HP targets.
    /// Demonstrates ConditionalEffect with HP threshold.
    /// </summary>
    public static ISpellEffect Execute(float baseDamage = 100f, float bonusDamage = 200f, float threshold = 0.3f)
    {
        return new ConditionalEffect
        {
            Condition = new Effects.Conditions.HPThresholdCondition
            {
                Threshold = threshold,
                Below = true
            },
            ThenEffect = new DamageEffect
            {
                Targeting = new CircleTargeting(3f), // Single target area
                Affinity = Affinity.Enemies,
                Damage = bonusDamage,
                VFXId = "execute_crit"
            },
            ElseEffect = new DamageEffect
            {
                Targeting = new CircleTargeting(3f),
                Affinity = Affinity.Enemies,
                Damage = baseDamage
            }
        };
    }

    /// <summary>
    /// Example: Chain spell that triggers additional effect on kill.
    /// Demonstrates OnKill hook.
    /// </summary>
    public static ISpellEffect SoulHarvest(float damage = 50f, float radius = 5f)
    {
        return new DamageEffect
        {
            Targeting = new CircleTargeting(radius),
            Affinity = Affinity.Enemies,
            Damage = damage,
            // OnKill could spawn a skeleton (would need SpawnEffect)
            // OnKill = new SpawnEffect { UnitId = "skeleton" }
        };
    }
}
