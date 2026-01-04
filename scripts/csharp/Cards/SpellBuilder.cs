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
    /// Get the spell effect for a given card ID.
    /// </summary>
    public static ISpellEffect GetEffect(string catalogId)
    {
        return catalogId switch
        {
            "fireball" => Fireball(),
            "rally" => Rally(),
            "guard" => Guard(),
            "charge" => Charge(),
            _ => null
        };
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
