using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic damage calculation operating on simulation data only.
/// Supports physical/magic damage types, evasion, defense, and shield absorption.
/// No Godot dependencies — uses DeterministicRng instead of BattleRNG.
/// </summary>
public static class SimDamage
{
    /// <summary>
    /// Calculate final damage from attacker to target.
    /// Returns (finalDamage, isCrit, wasEvaded).
    /// </summary>
    public static (float damage, bool isCrit, bool wasEvaded) Calculate(
        float baseDamage,
        DamageType damageType,
        UnitData? attacker,
        UnitData target,
        SummonerData? attackerSummoner,
        SummonerData? targetSummoner,
        DeterministicRng? rng,
        bool allowAttackProfileSplit = false,
        List<SimEvent>? events = null)
    {
        // 0. Evasion check (deterministic via RNG)
        if (target.Evasion > 0 && rng != null)
        {
            if (rng.NextFloat() < target.Evasion)
            {
                events?.Add(new AttackEvadedEvent(
                    target.UnitId,
                    attacker?.UnitId ?? -1
                ));
                return (0f, false, true);
            }
        }

        float damage = baseDamage;
        bool isCrit = false;

        // 1. Crit check (deterministic via RNG)
        if (attacker != null && attacker.CritChance > 0 && rng != null)
        {
            if (rng.NextFloat() < attacker.CritChance)
            {
                damage *= attacker.CritDamage;
                isCrit = true;
            }
        }

        // 2. Elemental matchup
        var attackerElement = attacker != null ? (Element)attacker.ElementId : Element.Neutral;
        var targetElement = (Element)target.ElementId;
        float elementalMultiplier = ElementMatchups.GetMultiplier(attackerElement, targetElement);
        damage *= elementalMultiplier;

        // 3. Summoner damage bonus (attacker's summoner)
        if (attackerSummoner != null)
        {
            if (attackerSummoner.DamageBonus > 0f)
            {
                damage *= 1f + attackerSummoner.DamageBonus / 100f;
            }

            float elementBonus = attackerSummoner.GetElementalDamageBonus(attackerElement);
            if (elementBonus > 0f)
            {
                damage *= 1f + elementBonus / 100f;
            }
        }

        // 4. Defense reduction (based on damage type)
        if (damageType != DamageType.True)
        {
            damage = ApplyDefenseReduction(
                damage,
                damageType,
                attacker,
                target,
                allowAttackProfileSplit);
        }

        // 5. Summoner damage reduction (target's summoner — flat reduction after defense)
        if (targetSummoner != null && targetSummoner.DamageReduction > 0f)
        {
            damage = MathF.Max(damage - targetSummoner.DamageReduction, 0f);
        }

        // 6. Shield absorption (oldest shield first)
        if (target.ActiveBuffs.Count > 0)
        {
            damage = SimEffects.AbsorbWithShields(target, damage, events);
        }

        // 7. Round to 1 decimal place (matches DamageSystem)
        damage = SimUtils.RoundToOneDecimal(damage);

        return (damage, isCrit, false);
    }

    /// <summary>
    /// Overload for backwards compatibility — defaults to Physical damage type.
    /// </summary>
    public static (float damage, bool isCrit) Calculate(
        float baseDamage,
        UnitData? attacker,
        UnitData target,
        SummonerData? attackerSummoner,
        SummonerData? targetSummoner,
        DeterministicRng? rng)
    {
        var (damage, isCrit, _) = Calculate(
            baseDamage,
            attacker?.AttackType ?? DamageType.Physical,
            attacker,
            target,
            attackerSummoner,
            targetSummoner,
            rng,
            allowAttackProfileSplit: true);
        return (damage, isCrit);
    }

    /// <summary>
    /// Defense reduction curve.
    /// Uses diminishing returns formula: multiplier = 100 / (100 + defense).
    /// At 0 defense: 100% damage. At 100 defense: 50% damage. At 200: 33%.
    /// </summary>
    public static float CalculateDefenseMultiplier(float defense)
    {
        if (defense <= 0f) return 1f;
        return 100f / (100f + defense);
    }

    private static float ApplyDefenseReduction(
        float damage,
        DamageType damageType,
        UnitData? attacker,
        UnitData target,
        bool allowAttackProfileSplit)
    {
        if (allowAttackProfileSplit && attacker != null && damageType == attacker.AttackType)
        {
            float physicalRatio = Clamp01(attacker.PhysicalDamageRatio);
            float elementalRatio = Clamp01(attacker.ElementalDamageRatio);
            float ratioTotal = physicalRatio + elementalRatio;

            // Mixed profile: split outgoing damage into physical + elemental lanes.
            if (physicalRatio > 0f && elementalRatio > 0f && ratioTotal > 0f)
            {
                physicalRatio /= ratioTotal;
                elementalRatio /= ratioTotal;

                float physicalPart = damage * physicalRatio * CalculateDefenseMultiplier(target.PhysicalDefense);
                float elementalPart = damage * elementalRatio * CalculateDefenseMultiplier(target.MagicDefense);
                return physicalPart + elementalPart;
            }
        }

        float defense = damageType == DamageType.Physical
            ? target.PhysicalDefense
            : target.MagicDefense;
        return damage * CalculateDefenseMultiplier(defense);
    }

    private static float Clamp01(float value)
    {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 1f;
        return value;
    }
}
