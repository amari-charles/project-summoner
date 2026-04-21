using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Effects;

/// <summary>
/// Canonical resolver for typed lifetime + legacy duration compatibility.
/// Keep all bridge semantics centralized here during migration.
/// </summary>
public static class EffectLifetimeResolver
{
    public static EffectLifetime Resolve(EffectLifetime lifetime, float legacyDuration)
    {
        if (lifetime.IsPersistent || lifetime.RemainingSeconds > 0f)
            return lifetime;

        if (legacyDuration == 0f)
            return lifetime;

        return EffectLifetime.FromLegacyDuration(legacyDuration);
    }

    public static float ResolveDuration(EffectLifetime lifetime, float legacyDuration)
    {
        return Resolve(lifetime, legacyDuration).ToLegacyDuration();
    }
}
