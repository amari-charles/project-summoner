using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Effects;

public static class SpellTargetResolver
{
    public static List<UnitData> Resolve(
        MatchState state,
        SpellExecutionContext context,
        SimSpellEffect effect
    )
    {
        return Resolve(
            state,
            context.CardData.SpellTargetingMode,
            context.Team,
            context.CastPosition,
            context.TargetUnitId,
            context.SourcePosition,
            context.CardData.SpellRadius,
            effect
        );
    }

    public static List<UnitData> Resolve(MatchState state, DelayedEffect effect)
    {
        return Resolve(
            state,
            effect.TargetingMode,
            (int)effect.SourceTeam,
            effect.Position,
            effect.TargetUnitId,
            ResolveSourcePosition(state, effect.SourceUnitId, effect.SourceTeam, effect.SourcePosition),
            effect.AoeRadius,
            SpellEffectAdapter.FromDelayedEffect(effect)
        );
    }

    private static List<UnitData> Resolve(
        MatchState state,
        SpellTargetingMode targetingMode,
        int sourceTeam,
        SimVector3 position,
        int? targetUnitId,
        SimVector3 origin,
        float defaultRadius,
        SimSpellEffect effect
    )
    {
        return targetingMode switch
        {
            SpellTargetingMode.NearestEnemy => ResolveSingleTarget(
                state,
                sourceTeam,
                targetUnitId,
                origin,
                effect
            ),
            SpellTargetingMode.AlliesInRadius => ResolveArea(
                state,
                sourceTeam,
                position,
                origin,
                defaultRadius,
                effect,
                forceAllies: true
            ),
            _ => ResolveArea(
                state,
                sourceTeam,
                position,
                origin,
                defaultRadius,
                effect,
                forceAllies: false
            ),
        };
    }

    private static List<UnitData> ResolveSingleTarget(
        MatchState state,
        int sourceTeam,
        int? targetUnitId,
        SimVector3 origin,
        SimSpellEffect effect
    )
    {
        var targets = new List<UnitData>();
        if (targetUnitId.HasValue)
        {
            var specified = state.GetAliveUnit(targetUnitId.Value);
            if (specified != null && PassesFilters(specified, sourceTeam, effect))
                targets.Add(specified);
            return targets;
        }

        UnitData? best = null;
        float bestDistSq = float.MaxValue;
        foreach (var candidate in state.GetAliveActiveUnits())
        {
            if (!PassesFilters(candidate, sourceTeam, effect))
                continue;

            float distSq = candidate.Position.DistanceSquaredTo(origin);
            if (distSq >= bestDistSq)
                continue;

            best = candidate;
            bestDistSq = distSq;
        }

        if (best != null)
            targets.Add(best);
        return targets;
    }

    private static List<UnitData> ResolveArea(
        MatchState state,
        int sourceTeam,
        SimVector3 position,
        SimVector3 origin,
        float defaultRadius,
        SimSpellEffect effect,
        bool forceAllies
    )
    {
        var targets = new List<UnitData>();
        float radius = effect.AoeRadius > 0f ? effect.AoeRadius : defaultRadius;

        foreach (var candidate in state.GetAliveActiveUnits())
        {
            if (!PassesFilters(candidate, sourceTeam, effect, forceAllies))
                continue;
            if (
                !SpellAreaResolver.IsWithinArea(
                    effect.AreaShape,
                    position,
                    candidate.Position,
                    radius,
                    origin
                )
            )
                continue;

            targets.Add(candidate);
        }

        return targets;
    }

    private static bool PassesFilters(
        UnitData unit,
        int sourceTeam,
        SimSpellEffect effect,
        bool forceAllies = false
    )
    {
        if (effect.RequiredTargetElementId >= 0 && unit.ElementId != effect.RequiredTargetElementId)
            return false;

        if (forceAllies)
            return (int)unit.Team == sourceTeam;

        return effect.Affinity switch
        {
            SpellAffinity.Allies => (int)unit.Team == sourceTeam,
            SpellAffinity.Both => true,
            _ => (int)unit.Team == MatchState.GetEnemyTeam(sourceTeam),
        };
    }

    private static SimVector3 ResolveSourcePosition(
        MatchState state,
        int sourceUnitId,
        Team sourceTeam,
        SimVector3 fallback
    )
    {
        if (sourceUnitId >= 0 && state.Units.TryGetValue(sourceUnitId, out var sourceUnit))
            return sourceUnit.Position;

        if (MatchState.IsSummonerTarget(sourceUnitId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(sourceUnitId);
            if (team >= 0 && team < state.Summoners.Length)
                return state.Summoners[team].Position;
        }

        int sourceTeamIndex = (int)sourceTeam;
        if (sourceTeamIndex >= 0 && sourceTeamIndex < state.Summoners.Length)
            return state.Summoners[sourceTeamIndex].Position;

        return fallback;
    }
}
