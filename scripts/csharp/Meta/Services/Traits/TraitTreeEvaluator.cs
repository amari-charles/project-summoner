using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Traits;

namespace Fateforged.Meta.Services.Traits;

public static class TraitTreeEvaluator
{
    public static string ResolveOwnerTypeTag(CardDefinition cardDef)
    {
        return cardDef.Type switch
        {
            CardType.Summon => TraitTags.Summon,
            CardType.Spell => TraitTags.Spell,
            _ => "",
        };
    }

    public static HashSet<string> BuildEffectiveCardTagSet(
        CardDefinition cardDef,
        string ownerTypeTag
    )
    {
        var tags = new HashSet<string>(cardDef.TraitEligibilityTags, StringComparer.Ordinal);

        // Normalize stale catalog defaults so owner type always matches CardDefinition.Type.
        tags.Remove(TraitTags.Summon);
        tags.Remove(TraitTags.Spell);

        if (!string.IsNullOrEmpty(ownerTypeTag))
            tags.Add(ownerTypeTag);

        return tags;
    }

    public static bool MatchesOwnerTags(TraitDefinition trait, TraitTreeOwnerContext context)
    {
        if (!trait.Tags.Contains(context.OwnerTypeTag, StringComparer.Ordinal))
            return false;

        var hasAnyEligibilityTag =
            trait.Tags.Length == 0 || trait.Tags.Any(tag => context.EligibilityTags.Contains(tag));
        if (!hasAnyEligibilityTag)
            return false;

        return trait.RequiredTags.All(tag => context.EligibilityTags.Contains(tag));
    }

    public static TraitUnlockEvaluation EvaluateProgressionTrait(
        TraitDefinition trait,
        TraitTreeOwnerContext context,
        int? levelOverride = null
    )
    {
        var isOwned = context.OwnedTraitIds.Contains(trait.Id.Value);
        if (isOwned)
        {
            return new TraitUnlockEvaluation
            {
                IsOwned = true,
                IsAcquirableTrait = true,
                MatchesTags = true,
                MeetsLevelRequirements = true,
                MeetsPrerequisites = true,
                HasTraitPoint = context.UnspentTraitPoints > 0,
                IsEligibleWithoutPoints = false,
                CanUnlockNow = false,
                LockedReason = "",
                UnlockBlockedReason = "",
                MissingPrerequisiteIds = [],
            };
        }

        if (trait.IsInnate)
        {
            return Locked("Innate trait", canUnlockNow: false);
        }

        if (trait.AcquisitionMode != TraitAcquisitionMode.LevelUpOffer)
        {
            return Locked("Granted from events or rewards", canUnlockNow: false);
        }

        var matchesTags = MatchesOwnerTags(trait, context);
        if (!matchesTags)
        {
            return Locked(
                "Not available for this owner",
                canUnlockNow: false,
                isAcquirable: true,
                matchesTags: false
            );
        }

        if (context.OwnerTypeTag == TraitTags.Summon || context.OwnerTypeTag == TraitTags.Spell)
        {
            if (trait.AllowedCardCatalogIds.Length > 0)
            {
                var cardId = context.CardCatalogId?.Trim() ?? "";
                if (!trait.AllowedCardCatalogIds.Contains(cardId, StringComparer.Ordinal))
                    return Locked(
                        "Not available for this card",
                        canUnlockNow: false,
                        isAcquirable: true
                    );
            }

            if (trait.AllowedRarities.Length > 0)
            {
                var rarity = context.CardRarity?.Trim().ToLowerInvariant() ?? "";
                if (!trait.AllowedRarities.Contains(rarity, StringComparer.OrdinalIgnoreCase))
                    return Locked(
                        "Not available for this card rarity",
                        canUnlockNow: false,
                        isAcquirable: true
                    );
            }
        }

        var evaluationLevel = levelOverride ?? context.CurrentLevel;
        var meetsMinLevel = evaluationLevel >= trait.MinLevel;
        var meetsMaxLevel = trait.MaxLevel <= 0 || evaluationLevel <= trait.MaxLevel;
        if (!meetsMinLevel)
        {
            return Locked(
                $"Requires level {trait.MinLevel}",
                canUnlockNow: false,
                isAcquirable: true
            );
        }

        if (!meetsMaxLevel)
        {
            return Locked(
                $"Only available through level {trait.MaxLevel}",
                canUnlockNow: false,
                isAcquirable: true
            );
        }

        var missingPrerequisites = trait
            .Prerequisites.Where(prereq =>
                !string.IsNullOrWhiteSpace(prereq) && !context.OwnedTraitIds.Contains(prereq)
            )
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (missingPrerequisites.Count > 0)
        {
            return new TraitUnlockEvaluation
            {
                IsOwned = false,
                IsAcquirableTrait = true,
                MatchesTags = true,
                MeetsLevelRequirements = true,
                MeetsPrerequisites = false,
                HasTraitPoint = context.UnspentTraitPoints > 0,
                IsEligibleWithoutPoints = false,
                CanUnlockNow = false,
                LockedReason = "Requires prerequisite traits",
                UnlockBlockedReason = "Requires prerequisite traits",
                MissingPrerequisiteIds = missingPrerequisites,
            };
        }

        var hasTraitPoint = context.UnspentTraitPoints > 0;
        return new TraitUnlockEvaluation
        {
            IsOwned = false,
            IsAcquirableTrait = true,
            MatchesTags = true,
            MeetsLevelRequirements = true,
            MeetsPrerequisites = true,
            HasTraitPoint = hasTraitPoint,
            IsEligibleWithoutPoints = true,
            CanUnlockNow = hasTraitPoint,
            LockedReason = "",
            UnlockBlockedReason = hasTraitPoint ? "" : "Need 1 trait point",
            MissingPrerequisiteIds = [],
        };
    }

    public static List<TraitDefinition> GetEligibleProgressionTraits(
        IEnumerable<TraitDefinition> traits,
        TraitTreeOwnerContext context,
        int evaluationLevel
    )
    {
        var result = new List<TraitDefinition>();

        foreach (var trait in traits)
        {
            var evaluation = EvaluateProgressionTrait(trait, context, evaluationLevel);
            if (!evaluation.IsEligibleWithoutPoints)
                continue;

            result.Add(trait);
        }

        return result;
    }

    public static Dictionary<string, int> ComputeDepthByTraitId(
        IEnumerable<TraitDefinition> progressionTraits
    )
    {
        var byId = progressionTraits
            .Where(t => !string.IsNullOrWhiteSpace(t.Id.Value))
            .ToDictionary(t => t.Id.Value, t => t, StringComparer.Ordinal);

        var depthCache = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var traitId in byId.Keys)
        {
            ComputeDepth(traitId, byId, depthCache, new HashSet<string>(StringComparer.Ordinal));
        }

        return depthCache;
    }

    private static int ComputeDepth(
        string traitId,
        IReadOnlyDictionary<string, TraitDefinition> byId,
        IDictionary<string, int> depthCache,
        ISet<string> visiting
    )
    {
        if (depthCache.TryGetValue(traitId, out var cached))
            return cached;

        if (visiting.Contains(traitId))
            return 0;

        visiting.Add(traitId);

        var maxParentDepth = -1;
        if (byId.TryGetValue(traitId, out var trait))
        {
            foreach (var prereqId in trait.Prerequisites)
            {
                if (string.IsNullOrWhiteSpace(prereqId) || !byId.ContainsKey(prereqId))
                    continue;

                var parentDepth = ComputeDepth(prereqId, byId, depthCache, visiting);
                maxParentDepth = Math.Max(maxParentDepth, parentDepth);
            }
        }

        visiting.Remove(traitId);
        var depth = Math.Max(0, maxParentDepth + 1);
        depthCache[traitId] = depth;
        return depth;
    }

    private static TraitUnlockEvaluation Locked(
        string reason,
        bool canUnlockNow,
        bool isAcquirable = false,
        bool matchesTags = true
    )
    {
        return new TraitUnlockEvaluation
        {
            IsOwned = false,
            IsAcquirableTrait = isAcquirable,
            MatchesTags = matchesTags,
            MeetsLevelRequirements = false,
            MeetsPrerequisites = false,
            HasTraitPoint = false,
            IsEligibleWithoutPoints = false,
            CanUnlockNow = canUnlockNow,
            LockedReason = reason,
            UnlockBlockedReason = reason,
            MissingPrerequisiteIds = [],
        };
    }
}
