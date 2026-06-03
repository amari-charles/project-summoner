using System;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Effects;

/// <summary>
/// Canonical stat/materialization rules derived from active effects.
/// </summary>
public static class EffectStatResolver
{
    public static float GetEffectiveMoveSpeed(UnitData unit)
    {
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Root)
                return 0f;
        }

        float speed = unit.MoveSpeed;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Slow)
                speed *= (1f - buff.Value);
            else if (buff.EffectType == EffectType.Haste)
                speed *= (1f + buff.Value);
        }
        return MathF.Max(speed, 0f);
    }

    public static float GetEffectiveRangedDamageMultiplier(UnitData unit)
    {
        float multiplier = 1f;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.RangedDamageModifier)
                multiplier *= 1f + buff.Value;
        }
        return MathF.Max(multiplier, 0f);
    }

    public static float GetEffectiveMissChance(UnitData unit)
    {
        float missChance = 0f;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.AccuracyModifier && buff.Value < 0f)
                missChance += -buff.Value;
        }

        if (missChance <= 0f)
            return 0f;
        if (missChance >= 1f)
            return 1f;
        return missChance;
    }

    public static float GetEffectiveAttackDamage(UnitData unit)
    {
        float damage = unit.AttackDamage;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.DamageBoost)
                damage *= (1f + buff.Value);
        }
        return damage;
    }

    public static float GetEffectiveAttackSpeed(UnitData unit)
    {
        float speed = unit.AttackSpeed;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType != EffectType.AttackSpeedModifier)
                continue;

            speed *= (1f + buff.Value);
        }

        return MathF.Max(speed, 0f);
    }

    public static float GetFlatDamageReduction(UnitData unit)
    {
        float reduction = 0f;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType != EffectType.FlatDamageReduction)
                continue;

            reduction += MathF.Max(0f, buff.Value);
        }
        return reduction;
    }

    public static float ApplyFlatDamageReduction(UnitData unit, float incomingDamage)
    {
        if (incomingDamage <= 0f)
            return 0f;

        float reduced = incomingDamage - GetFlatDamageReduction(unit);
        return MathF.Max(reduced, 0f);
    }

    public static float GetEffectiveEvasion(UnitData unit)
    {
        float evasion = unit.Evasion;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType != EffectType.EvasionModifier)
                continue;

            evasion += buff.Value;
        }

        if (evasion <= 0f)
            return 0f;
        if (evasion >= 1f)
            return 1f;
        return evasion;
    }
}
