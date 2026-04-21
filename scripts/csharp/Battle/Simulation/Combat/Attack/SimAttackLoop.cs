using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Commit-slot attack phase ticker.
/// Phase timing runs here while damage application remains in SimBehavior/attack vector logic.
/// </summary>
public static class SimAttackLoop
{
    private const float MinWindupSeconds = 0.02f;
    private const float DefaultFallbackWindupSeconds = 0.4f;
    private const float DefaultActiveSeconds = 0.05f;
    private const float DefaultRecoverySeconds = 0.15f;
    private const float CooldownGuardBufferSeconds = 0.01f;

    public static void Tick(UnitData unit, MatchState state, float delta, List<SimEvent> events)
    {
        if (unit.AttackPhase == AttackPhase.None)
            return;

        unit.AttackPhaseTimer = MathF.Max(0f, unit.AttackPhaseTimer - delta);
        if (unit.AttackPhaseTimer > 0f)
            return;

        switch (unit.AttackPhase)
        {
            case AttackPhase.Windup:
                unit.AttackPhase = AttackPhase.Active;
                unit.AttackPhaseTimer = ResolveActiveSeconds(unit);
                SimBehavior.ResolvePendingAttackCommit(unit, state, events);
                break;
            case AttackPhase.Active:
                unit.AttackPhase = AttackPhase.Recovery;
                unit.AttackPhaseTimer = ResolveRecoverySeconds(unit);
                break;
            default:
                unit.AttackPhase = AttackPhase.None;
                unit.AttackPhaseTimer = 0f;
                unit.AttackPhaseLockTargetId = null;
                break;
        }

        if (unit.AttackPhase == AttackPhase.None)
        {
            unit.AttackPhaseLockTargetId = null;
            SimBehavior.ClearPendingAttack(unit);
        }
    }

    public static void Begin(UnitData unit, MatchState state, int? targetId)
    {
        unit.AttackPhase = AttackPhase.Windup;
        unit.AttackPhaseTimer = ResolveWindupSeconds(unit);
        unit.AttackPhaseLockTargetId = targetId;
        state.CombatWindupsStarted++;
    }

    public static void Cancel(UnitData unit, MatchState state)
    {
        if (unit.AttackPhase == AttackPhase.None)
            return;

        unit.AttackPhase = AttackPhase.None;
        unit.AttackPhaseTimer = 0f;
        unit.AttackPhaseLockTargetId = null;
        SimBehavior.ClearPendingAttack(unit);
        state.CombatWindupsCancelled++;
    }

    public static float ResolveAttackAnimationDuration(UnitData unit)
    {
        return ResolveWindupSeconds(unit) + ResolveActiveSeconds(unit) + ResolveRecoverySeconds(unit);
    }

    private static float ResolveWindupSeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.WindupSeconds;
        if (authored > 0f)
            return authored;

        // Migration bridge: preserve legacy delayed-ranged feel if no authored timing exists.
        if (unit.UnitType == UnitType.Ranged && unit.ProjectileDelay > 0f)
            return unit.ProjectileDelay;

        return ClampFallbackWindupToCooldown(unit, DefaultFallbackWindupSeconds);
    }

    private static float ResolveActiveSeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.ActiveSeconds;
        if (authored > 0f)
            return authored;
        return DefaultActiveSeconds;
    }

    private static float ResolveRecoverySeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.RecoverySeconds;
        if (authored > 0f)
            return authored;
        return DefaultRecoverySeconds;
    }

    private static float ClampFallbackWindupToCooldown(UnitData unit, float windupSeconds)
    {
        float clamped = MathF.Max(windupSeconds, MinWindupSeconds);

        float effectiveAttackSpeed = SimEffects.GetEffectiveAttackSpeed(unit);
        if (effectiveAttackSpeed <= 0f)
            return clamped;

        float cooldownSeconds = 1f / effectiveAttackSpeed;
        if (cooldownSeconds <= 0f)
            return clamped;

        float maxWindupSeconds =
            cooldownSeconds
            - ResolveActiveSeconds(unit)
            - ResolveRecoverySeconds(unit)
            - CooldownGuardBufferSeconds;
        if (maxWindupSeconds <= MinWindupSeconds)
            return MinWindupSeconds;

        return MathF.Min(clamped, maxWindupSeconds);
    }
}
