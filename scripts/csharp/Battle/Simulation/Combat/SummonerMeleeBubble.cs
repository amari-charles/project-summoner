using System;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Shared runtime configuration for summoner melee bubble targeting.
/// Used by targeting gates, sloting, and debug controls.
/// </summary>
public static class SummonerMeleeBubble
{
    public const float DefaultRadius = 5.4f;
    private const float MinRadius = 0.1f;

    private static float? _overrideRadius;

    public static float DefaultRadiusValue => DefaultRadius;
    public static float? OverrideRadiusValue => _overrideRadius;
    public static bool HasOverride => _overrideRadius.HasValue;
    public static float EffectiveRadius => _overrideRadius ?? DefaultRadius;

    public static void SetOverrideRadius(float radius)
    {
        _overrideRadius = MathF.Max(MinRadius, radius);
    }

    public static void ClearOverrideRadius()
    {
        _overrideRadius = null;
    }

    public static SimVector3 ResolveClosestPoint(SimVector3 summonerPosition, SimVector3 attackerPosition)
    {
        float radius = EffectiveRadius;
        var toAttacker = attackerPosition - summonerPosition;
        toAttacker = new SimVector3(toAttacker.X, 0f, toAttacker.Z);
        float distanceSq = toAttacker.LengthSquared();
        float radiusSq = radius * radius;

        // Bubble is a filled XZ area around the summoner.
        // If attacker is already inside, engage is evaluated at the attacker point.
        if (distanceSq <= radiusSq)
            return new SimVector3(attackerPosition.X, summonerPosition.Y, attackerPosition.Z);

        if (distanceSq < 0.000001f)
            return new SimVector3(summonerPosition.X + radius, summonerPosition.Y, summonerPosition.Z);

        var dir = toAttacker.Normalized();
        return new SimVector3(
            summonerPosition.X + (dir.X * radius),
            summonerPosition.Y,
            summonerPosition.Z + (dir.Z * radius)
        );
    }
}
