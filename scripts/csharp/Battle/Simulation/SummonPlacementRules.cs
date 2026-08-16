using System;
using Fateforged.Constants;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation;

public enum SummonPlacementMode
{
    TeamHalf = 0,
    CardRangeFromSummoner = 1,
}

public readonly record struct SummonPlacementBounds(
    float MinX,
    float MaxX,
    float MinZ,
    float MaxZ
);

/// <summary>
/// Shared summon-placement rules used by input feedback, AI, and authoritative validation.
/// </summary>
public static class SummonPlacementRules
{
    public static bool IsValid(
        MatchState state,
        int team,
        SimCardData card,
        SimVector3 position
    )
    {
        if (!IsWithinBattlefield(state, position))
            return false;

        if (state.SummonPlacementMode == SummonPlacementMode.TeamHalf)
            return BattlefieldBounds.IsValidSpawnPositionForTeam(position, team);

        if (team < 0 || team >= state.Summoners.Length)
            return false;

        return IsWithinCardRange(state.Summoners[team].Position, position, card.SummonRange);
    }

    public static bool IsWithinBattlefield(MatchState state, SimVector3 position)
    {
        if (!state.SummonPlacementBounds.HasValue)
            return BattlefieldBounds.IsInBounds(position);

        var bounds = state.SummonPlacementBounds.Value;
        return position.X >= bounds.MinX
            && position.X <= bounds.MaxX
            && position.Z >= bounds.MinZ
            && position.Z <= bounds.MaxZ;
    }

    public static bool IsWithinCardRange(
        SimVector3 summonerPosition,
        SimVector3 position,
        float summonRange
    )
    {
        float dx = position.X - summonerPosition.X;
        float dz = position.Z - summonerPosition.Z;
        float safeRange = Math.Max(0f, summonRange);
        return dx * dx + dz * dz <= safeRange * safeRange;
    }

    /// <summary>
    /// Resolves a requested card-range summon position to the closest playable point.
    /// Positions beyond the card radius snap to its circumference, then to the
    /// encounter bounds. The summoner is expected to remain inside those bounds.
    /// </summary>
    public static SimVector3 ResolveCardRangePosition(
        MatchState state,
        int team,
        SimCardData card,
        SimVector3 requestedPosition
    )
    {
        if (team < 0 || team >= state.Summoners.Length)
            return requestedPosition;

        var position = ClampToCardRange(
            state.Summoners[team].Position,
            requestedPosition,
            card.SummonRange
        );
        return ClampToBattlefield(state, position);
    }

    public static SimVector3 ClampToCardRange(
        SimVector3 summonerPosition,
        SimVector3 requestedPosition,
        float summonRange
    )
    {
        float dx = requestedPosition.X - summonerPosition.X;
        float dz = requestedPosition.Z - summonerPosition.Z;
        float safeRange = Math.Max(0f, summonRange);
        float distanceSquared = dx * dx + dz * dz;
        float rangeSquared = safeRange * safeRange;
        if (distanceSquared <= rangeSquared)
            return requestedPosition;

        if (distanceSquared <= float.Epsilon || safeRange <= 0f)
        {
            return new SimVector3(
                summonerPosition.X,
                requestedPosition.Y,
                summonerPosition.Z
            );
        }

        float scale = safeRange / MathF.Sqrt(distanceSquared);
        return new SimVector3(
            summonerPosition.X + dx * scale,
            requestedPosition.Y,
            summonerPosition.Z + dz * scale
        );
    }

    public static SimVector3 ClampToBattlefield(MatchState state, SimVector3 position)
    {
        if (!state.SummonPlacementBounds.HasValue)
            return BattlefieldBounds.ClampToBounds(position);

        var bounds = state.SummonPlacementBounds.Value;
        return new SimVector3(
            Math.Clamp(position.X, bounds.MinX, bounds.MaxX),
            position.Y,
            Math.Clamp(position.Z, bounds.MinZ, bounds.MaxZ)
        );
    }

    public static SimVector3 SelectRandomPositionWithinCardRange(
        MatchState state,
        SummonerData summoner,
        SimCardData card
    )
    {
        if (state.Rng == null)
            return summoner.Position;

        float angle = state.Rng.RangeFloat(0f, MathF.PI * 2f);
        float distance = MathF.Sqrt(state.Rng.RangeFloat(0f, 1f)) * Math.Max(0f, card.SummonRange);
        var position = new SimVector3(
            summoner.Position.X + MathF.Cos(angle) * distance,
            summoner.Position.Y,
            summoner.Position.Z + MathF.Sin(angle) * distance
        );
        return ClampToBattlefield(state, position);
    }
}
