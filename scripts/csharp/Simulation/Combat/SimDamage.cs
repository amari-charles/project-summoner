using System;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic damage calculation operating on simulation data only.
/// Mirrors the DamageSystem.ApplyDamage() formula exactly so results are identical.
/// No Godot dependencies — uses DeterministicRng instead of BattleRNG.
/// </summary>
public static class SimDamage
{
    /// <summary>
    /// Calculate final damage from attacker to target.
    /// Returns (finalDamage, isCrit).
    /// </summary>
    public static (float damage, bool isCrit) Calculate(
        float baseDamage,
        UnitData? attacker,
        UnitData target,
        SummonerData? attackerSummoner,
        SummonerData? targetSummoner,
        DeterministicRng? rng)
    {
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

        // 2. Damage type multiplier (all 1.0 currently — no-op)

        // 3. Elemental matchup
        var attackerElement = attacker != null ? (Element)attacker.ElementId : Element.Neutral;
        var targetElement = (Element)target.ElementId;
        float elementalMultiplier = ElementMatchups.GetMultiplier(attackerElement, targetElement);
        damage *= elementalMultiplier;

        // 4. Summoner damage bonus (attacker's summoner)
        if (attackerSummoner != null)
        {
            // General damage bonus
            if (attackerSummoner.DamageBonus > 0f)
            {
                damage *= 1f + attackerSummoner.DamageBonus / 100f;
            }

            // Per-element damage bonus (bonus for attacker's element)
            float elementBonus = attackerSummoner.GetElementalDamageBonus(attackerElement);
            if (elementBonus > 0f)
            {
                damage *= 1f + elementBonus / 100f;
            }
        }

        // 5. Summoner damage reduction (target's summoner)
        if (targetSummoner != null && targetSummoner.DamageReduction > 0f)
        {
            damage = MathF.Max(damage - targetSummoner.DamageReduction, 0f);
        }

        // 6. Round to 1 decimal place (matches DamageSystem)
        damage = MathF.Round(damage * 10f) / 10f;

        return (damage, isCrit);
    }
}
