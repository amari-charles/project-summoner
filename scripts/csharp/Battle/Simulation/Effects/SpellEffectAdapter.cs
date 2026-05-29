using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Effects;

public static class SpellEffectAdapter
{
    public static SimSpellEffect FromDelayedEffect(DelayedEffect effect)
    {
        return new SimSpellEffect
        {
            EffectType = effect.EffectType,
            Value = effect.Value,
            Duration = effect.Duration,
            Lifetime = effect.Lifetime,
            DamageType = effect.DamageType,
            AoeRadius = effect.AoeRadius,
            AreaShape = effect.AreaShape,
            Affinity = effect.Affinity,
            RequiredTargetElementId = effect.RequiredTargetElementId,
            StatusKind = effect.StatusKind,
            StatusTickInterval = effect.StatusTickInterval,
            StatusPotencyPerStack = effect.StatusPotencyPerStack,
            StatusMaxStacks = effect.StatusMaxStacks,
            RemovalEffect = effect.RemovalEffect,
            TagRequirements = effect.TagRequirements.DeepClone(),
            GrantedTags = [.. effect.GrantedTags],
            StackPolicy = effect.StackPolicy,
            StackKey = effect.StackKey,
            CueId = effect.CueId,
        };
    }
}
