using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Subsystems;

namespace Fateforged.Simulation;

/// <summary>
/// Shared utility methods used across simulation files (SimBehavior, SimMovement, SimEffects).
/// Prevents duplicated logic in critical code paths.
/// </summary>
public static class SimUtils
{
    /// <summary>
    /// Resolve target position for either a unit or summoner target ID.
    /// Returns null if target is invalid/dead.
    /// Used by SimBehavior (for range checks) and SimMovement (for movement direction).
    /// </summary>
    public static SimVector3? ResolveTargetPosition(int? targetId, MatchState state)
    {
        if (!targetId.HasValue) return null;

        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
            if (team >= 0 && team <= 1 && state.Summoners[team].IsAlive)
                return state.Summoners[team].Position;
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
    public static void KillUnit(MatchState state, UnitData target, int killerUnitId, List<SimEvent> events)
    {
        target.CurrentHp = 0;
        target.IsAlive = false;
        target.DeathCleanupTimer = SimConstants.DeathCleanupSeconds;
        state.KillCount++;
        events.Add(new UnitDiedSimEvent(target.UnitId, killerUnitId));
    }
}
