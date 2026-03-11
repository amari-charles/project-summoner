using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Commit-slot attack phase ticker.
/// Phase timing runs here while damage application remains in SimBehavior/attack vector logic.
/// </summary>
public static class SimAttackLoop
{
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
            unit.AttackPhaseLockTargetId = null;
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
        state.CombatWindupsCancelled++;
    }

    private static float ResolveWindupSeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.WindupSeconds;
        if (authored > 0f)
            return authored;
        return 0.02f;
    }

    private static float ResolveActiveSeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.ActiveSeconds;
        if (authored > 0f)
            return authored;
        return 0.05f;
    }

    private static float ResolveRecoverySeconds(UnitData unit)
    {
        float authored = unit.Attack.Timing.RecoverySeconds;
        if (authored > 0f)
            return authored;
        return 0.15f;
    }
}
