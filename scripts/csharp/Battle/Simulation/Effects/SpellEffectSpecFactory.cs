using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Effects;

public static class SpellEffectSpecFactory
{
    public static EffectApplicationSpec FromSpellEffect(
        SimSpellEffect effect,
        SpellExecutionContext context
    )
    {
        return new EffectApplicationSpec
        {
            EffectType = effect.EffectType,
            Value = effect.Value,
            Duration = EffectLifetimeResolver.ResolveDuration(effect.Lifetime, effect.Duration),
            Lifetime = effect.Lifetime,
            DamageType = effect.DamageType,
            StatusKind = effect.StatusKind,
            StatusTickInterval = effect.StatusTickInterval,
            StatusPotencyPerStack = effect.StatusPotencyPerStack,
            StatusMaxStacks = effect.StatusMaxStacks,
            RemovalEffect = effect.RemovalEffect,
            RequiredTargetElementId = effect.RequiredTargetElementId,
            TagRequirements = effect.TagRequirements.DeepClone(),
            GrantedTags = new List<string>(effect.GrantedTags),
            StackPolicy = effect.StackPolicy,
            StackKey = effect.StackKey,
            CueId = ResolveCueId(effect.CueId, context.CardData.CatalogId, effect.EffectType),
            Context = new EffectApplicationContext
            {
                SourceUnitId = context.SourceUnitId,
                SourceTeam = (Team)context.Team,
                SourcePosition = context.CastPosition,
                CardCatalogId = context.CardData.CatalogId,
            },
        };
    }

    public static EffectApplicationSpec FromDelayedEffect(DelayedEffect effect)
    {
        return new EffectApplicationSpec
        {
            EffectType = effect.EffectType,
            Value = effect.Value,
            Duration = EffectLifetimeResolver.ResolveDuration(effect.Lifetime, effect.Duration),
            Lifetime = effect.Lifetime,
            DamageType = effect.DamageType,
            StatusKind = effect.StatusKind,
            StatusTickInterval = effect.StatusTickInterval,
            StatusPotencyPerStack = effect.StatusPotencyPerStack,
            StatusMaxStacks = effect.StatusMaxStacks,
            RemovalEffect = effect.RemovalEffect,
            RequiredTargetElementId = effect.RequiredTargetElementId,
            TagRequirements = effect.TagRequirements.DeepClone(),
            GrantedTags = new List<string>(effect.GrantedTags),
            StackPolicy = effect.StackPolicy,
            StackKey = effect.StackKey,
            CueId = ResolveCueId(effect.CueId, effect.CardCatalogId, effect.EffectType),
            Context = new EffectApplicationContext
            {
                SourceUnitId = effect.SourceUnitId,
                SourceTeam = effect.SourceTeam,
                SourcePosition = effect.Position,
                CardCatalogId = effect.CardCatalogId,
            },
        };
    }

    private static string ResolveCueId(
        string explicitCueId,
        SimCardCatalogId cardCatalogId,
        EffectType effectType
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitCueId))
            return explicitCueId;
        return cardCatalogId.HasValue ? $"{cardCatalogId.Value}:{effectType}" : $"{effectType}";
    }
}
