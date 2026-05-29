using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;

namespace Fateforged.Simulation;

/// <summary>
/// Shared utility methods used across simulation files (SimBehavior, SimMovement, SimEffects).
/// Prevents duplicated logic in critical code paths.
/// </summary>
public static class SimUtils
{
    /// <summary>
    /// Round to one decimal place for deterministic damage results.
    /// </summary>
    public static float RoundToOneDecimal(float value) => MathF.Round(value * 10f) / 10f;

    /// <summary>
    /// Resolve target position for either a unit or summoner target ID.
    /// Returns null if target is invalid/dead.
    /// Used by SimBehavior (for range checks) and SimMovement (for movement direction).
    /// </summary>
    public static SimVector3? ResolveTargetPosition(int? targetId, MatchState state)
    {
        if (!targetId.HasValue)
            return null;

        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
            if (team >= 0 && team <= 1 && state.Summoners[team].IsAlive)
                return state.Summoners[team].TargetPointPosition;
            return null;
        }

        var unit = state.GetAliveUnit(targetId.Value);
        return unit?.Position;
    }

    /// <summary>
    /// Kill a unit: set HP to 0, mark dead, start cleanup timer, increment kill count, emit event.
    /// Single source of truth for unit death — prevents duplicated death logic across
    /// SimBehavior (melee/pending damage) and SimEffects (direct/periodic damage).
    /// Does NOT fire triggers — caller is responsible for firing OnKill/OnDeath triggers
    /// since trigger context (attacker identity, trigger type) varies by call site.
    /// </summary>
    public static bool KillUnit(
        MatchState state,
        UnitData target,
        int killerUnitId,
        List<SimEvent> events
    )
    {
        if (TryConsumeReviveOnDeath(target, events))
            return false;

        target.CurrentHp = 0;
        target.IsAlive = false;
        target.DeathCleanupTimer = SimConstants.DeathCleanupSeconds;
        state.KillCount++;
        events.Add(new UnitDiedEvent(target.UnitId, killerUnitId));
        return true;
    }

    private static bool TryConsumeReviveOnDeath(UnitData target, List<SimEvent> events)
    {
        for (int i = 0; i < target.ActiveBuffs.Count; i++)
        {
            var buff = target.ActiveBuffs[i];
            if (buff.EffectType != EffectType.ReviveOnDeath)
                continue;

            float revivePercent = buff.Value > 0f ? buff.Value : 0.5f;
            target.ActiveBuffs.RemoveAt(i);
            target.CurrentHp = MathF.Max(1f, target.MaxHp * revivePercent);
            target.IsAlive = true;
            target.DeathCleanupTimer = 0f;
            events.Add(new BuffExpiredEvent(target.UnitId, buff.BuffId, buff.EffectType));
            return true;
        }

        return false;
    }
}
