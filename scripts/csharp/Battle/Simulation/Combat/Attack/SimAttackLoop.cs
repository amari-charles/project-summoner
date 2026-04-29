using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Target-commit attack phase ticker.
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
        if (unit.Action.AttackPhase == AttackPhase.None)
            return;

        unit.Action.AttackPhaseTimer = MathF.Max(0f, unit.Action.AttackPhaseTimer - delta);
        if (unit.Action.AttackPhaseTimer > 0f)
            return;

        switch (unit.Action.AttackPhase)
        {
            case AttackPhase.Windup:
                unit.Action.AttackPhase = AttackPhase.Active;
                unit.Action.AttackPhaseTimer = ResolveActiveSeconds(unit);
                SimBehavior.ResolvePendingAttackCommit(unit, state, events);
                break;
            case AttackPhase.Active:
                unit.Action.AttackPhase = AttackPhase.Recovery;
                unit.Action.AttackPhaseTimer = ResolveRecoverySeconds(unit);
                break;
            default:
                unit.Action.AttackPhase = AttackPhase.None;
                unit.Action.AttackPhaseTimer = 0f;
                unit.Action.AttackPhaseLockTargetId = null;
                break;
        }

        if (unit.Action.AttackPhase == AttackPhase.None)
        {
            unit.Action.AttackPhaseLockTargetId = null;
            SimBehavior.ClearPendingAttack(unit);
        }
    }

    public static void Begin(UnitData unit, MatchState state, int? targetId)
    {
        unit.Action.AttackPhase = AttackPhase.Windup;
        unit.Action.AttackPhaseTimer = ResolveWindupSeconds(unit);
        unit.Action.AttackPhaseLockTargetId = targetId;
        state.CombatWindupsStarted++;
    }

    public static void Cancel(UnitData unit, MatchState state)
    {
        if (unit.Action.AttackPhase == AttackPhase.None)
            return;

        unit.Action.AttackPhase = AttackPhase.None;
        unit.Action.AttackPhaseTimer = 0f;
        unit.Action.AttackPhaseLockTargetId = null;
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
