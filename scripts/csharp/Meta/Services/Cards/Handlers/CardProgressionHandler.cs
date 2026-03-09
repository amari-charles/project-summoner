using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Fateforged.Cards;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Progression.Core;
using Fateforged.Meta.Traits.Unified;
using Fateforged.Stats;

namespace Fateforged.Meta.Cards.Handlers;

/// <summary>
/// Handles card progression: XP, leveling, and traits.
/// Fully typed API — facades handle string conversion.
/// </summary>
public class CardProgressionHandler
{
    private readonly IProfileRepository _profileRepo;

    // =========================================================================
    // CONSTANTS
    // =========================================================================

    public const int MaxLevel = 10;

    private static readonly int[] XpThresholds = [0, 30, 75, 150, 300, 500, 800, 1200, 1800, 2500];

    private static readonly Dictionary<string, float> RarityMultipliers = new()
    {
        ["common"] = 1.0f,
        ["rare"] = 1.5f,
        ["epic"] = 2.0f,
        ["legendary"] = 3.0f
    };

    public CardProgressionHandler(IProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    // =========================================================================
    // XP OPERATIONS
    // =========================================================================

    /// <summary>Grant XP to a card. Returns the new XP total.</summary>
    public int GrantXp(CardInstanceId cardInstanceId, int amount)
    {
        if (amount <= 0)
            return 0;

        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
        {
            GD.PushWarning($"CardProgressionHandler: Card not found: {cardInstanceId}");
            return 0;
        }

        var newXp = card.Xp + amount;
        _profileRepo.UpdateCard(cardInstanceId, new CardUpdate { Xp = newXp });

        GD.Print($"CardProgressionHandler: Granted {amount} XP to card '{cardInstanceId}' (now: {newXp})");
        return newXp;
    }

    /// <summary>Grant XP to multiple cards.</summary>
    public Dictionary<CardInstanceId, int> GrantXpToCards(IEnumerable<CardInstanceId> cardInstanceIds, int amount)
    {
        var results = new Dictionary<CardInstanceId, int>();
        foreach (var cardId in cardInstanceIds)
        {
            results[cardId] = GrantXp(cardId, amount);
        }
        return results;
    }

    /// <summary>Get XP required for a level (base, no rarity scaling).</summary>
    public int GetXpForLevel(int level)
    {
        if (level < 1 || level > MaxLevel)
            return 0;
        return XpThresholds[level - 1];
    }

    /// <summary>Get XP required for a level with rarity scaling.</summary>
    public int GetXpForLevelWithRarity(int level, string rarity)
    {
        return (int)(GetXpForLevel(level) * GetRarityMultiplier(rarity));
    }

    /// <summary>Get rarity multiplier.</summary>
    public float GetRarityMultiplier(string rarity)
    {
        return RarityMultipliers.GetValueOrDefault(rarity.ToLower(), 1.0f);
    }

    /// <summary>Get XP needed for next level.</summary>
    public int GetXpToNextLevel(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return 0;

        var state = BuildProgressionState(card);
        var curve = BuildProgressionCurve(card);
        var xpCost = ProgressionEngine.GetXpCostForNextLevel(state, curve);
        return Math.Max(0, xpCost - state.XpTowardNext);
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return 0f;

        var state = BuildProgressionState(card);
        var curve = BuildProgressionCurve(card);
        return ProgressionEngine.GetProgress01(state, curve);
    }

    // =========================================================================
    // LEVEL-UP OPERATIONS
    // =========================================================================

    /// <summary>Check if card has enough XP to level up.</summary>
    public bool CanLevelUp(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return false;

        var state = BuildProgressionState(card);
        var curve = BuildProgressionCurve(card);
        return ProgressionEngine.CanLevelUp(state, curve);
    }

    /// <summary>
    /// Level up a card. Pass 2 unified flow grants a trait point and defers selection.
    /// Requires only XP - no gold cost.
    /// Returns true if successful.
    /// </summary>
    public bool LevelUpCard(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
        {
            GD.PushError($"CardProgressionHandler: Card not found: {cardInstanceId}");
            return false;
        }

        if (!CanLevelUp(cardInstanceId))
        {
            GD.PushWarning($"CardProgressionHandler: Card cannot level up: {cardInstanceId}");
            return false;
        }

        var state = BuildProgressionState(card);
        var curve = BuildProgressionCurve(card);
        var applyResult = ProgressionEngine.ApplyLevelUp(state, curve);
        if (!applyResult.Success)
            return false;

        _profileRepo.UpdateCard(cardInstanceId, new CardUpdate
        {
            Level = applyResult.NextState.Level,
            Xp = applyResult.NextState.XpTowardNext,
            UnspentTraitPoints = card.UnspentTraitPoints + 1
        });

        GD.Print($"CardProgressionHandler: Leveled up card '{cardInstanceId}' to level {applyResult.NextState.Level}, consumed {applyResult.XpCostSpent} XP (remaining: {applyResult.NextState.XpTowardNext}), and granted 1 trait point");
        return true;
    }

    public int GetCardUnspentTraitPoints(CardInstanceId cardInstanceId)
    {
        return _profileRepo.GetCard(cardInstanceId)?.UnspentTraitPoints ?? 0;
    }

    public int GrantCardTraitPoints(CardInstanceId cardInstanceId, int amount, string source = "")
    {
        if (amount <= 0)
            return 0;

        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return 0;

        var newValue = card.UnspentTraitPoints + amount;
        if (!_profileRepo.UpdateCard(cardInstanceId, new CardUpdate { UnspentTraitPoints = newValue }))
            return 0;

        if (!string.IsNullOrEmpty(source))
            GD.Print($"CardProgressionHandler: Granted {amount} trait points to '{cardInstanceId}' from source='{source}'");

        return newValue;
    }

    public List<UnifiedTraitOffer> RollCardTraitOffers(CardInstanceId cardInstanceId, int count)
    {
        if (count <= 0)
            return [];

        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return [];

        var cardDef = CardCatalog.GetCard(card.CatalogId);
        if (cardDef == null)
            return [];

        var ownerTypeTag = ResolveOwnerTypeTag(cardDef);
        if (string.IsNullOrEmpty(ownerTypeTag))
            return [];

        var evaluationLevel = ResolveOfferEvaluationLevel(cardInstanceId, card);
        var cardTagSet = BuildEffectiveCardTagSet(cardDef, ownerTypeTag);
        var ownedTraitSet = new HashSet<string>(card.Traits.Select(t => t.Value));
        var eligibleTraits = new List<TraitDefinition>();

        foreach (var trait in TraitCatalog.GetAllTraits())
        {
            if (trait.IsInnate)
                continue;
            if (ownedTraitSet.Contains(trait.Id))
                continue;
            if (!trait.Tags.Contains(ownerTypeTag))
                continue;

            var hasAnyEligibilityTag = trait.Tags.Length == 0 || trait.Tags.Any(tag => cardTagSet.Contains(tag));
            var hasAllRequiredTags = trait.RequiredTags.All(tag => cardTagSet.Contains(tag));
            if (!hasAnyEligibilityTag || !hasAllRequiredTags)
                continue;

            if (evaluationLevel < trait.MinLevel)
                continue;
            if (trait.MaxLevel > 0 && evaluationLevel > trait.MaxLevel)
                continue;

            if (trait.Prerequisites.Any(prereq => !ownedTraitSet.Contains(prereq)))
                continue;

            eligibleTraits.Add(trait);
        }

        if (eligibleTraits.Count == 0)
            return [];

        var orderedTraits = eligibleTraits
            .OrderBy(trait => ComputeStableOfferOrder($"{cardInstanceId.Value}|{evaluationLevel}", trait.Id))
            .ThenBy(trait => trait.Id.Value, StringComparer.Ordinal)
            .Take(count);

        var offers = new List<UnifiedTraitOffer>();
        foreach (var trait in orderedTraits)
        {
            offers.Add(new UnifiedTraitOffer
            {
                TraitId = trait.Id.Value,
                DisplayName = new UnifiedDisplayText { LocalizationKey = trait.NameKey },
                Description = new UnifiedDisplayText { LocalizationKey = trait.DescriptionKey },
                Weight = UnifiedWeight.One
            });
        }

        return offers;
    }

    public bool SpendCardTraitPoint(CardInstanceId cardInstanceId, CardTraitId traitId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null) return false;
        if (card.UnspentTraitPoints <= 0) return false;
        if (traitId == CardTraitId.None) return false;
        var normalizedTraitId = CardTraitId.FromString(traitId.Value.Trim());
        if (normalizedTraitId == CardTraitId.None) return false;
        if (card.Traits.Contains(normalizedTraitId)) return false;

        var traitDef = TraitCatalog.GetTrait(normalizedTraitId.Value);
        if (traitDef == null || traitDef.IsInnate)
            return false;

        var cardDef = CardCatalog.GetCard(card.CatalogId);
        if (cardDef == null)
            return false;

        var ownerTypeTag = ResolveOwnerTypeTag(cardDef);
        if (string.IsNullOrEmpty(ownerTypeTag))
            return false;
        if (!traitDef.Tags.Contains(ownerTypeTag))
            return false;

        var cardTagSet = BuildEffectiveCardTagSet(cardDef, ownerTypeTag);
        var hasAnyEligibilityTag = traitDef.Tags.Length == 0 || traitDef.Tags.Any(tag => cardTagSet.Contains(tag));
        var hasAllRequiredTags = traitDef.RequiredTags.All(tag => cardTagSet.Contains(tag));
        if (!hasAnyEligibilityTag || !hasAllRequiredTags)
            return false;

        if (card.Level < traitDef.MinLevel)
            return false;
        if (traitDef.MaxLevel > 0 && card.Level > traitDef.MaxLevel)
            return false;

        var ownedTraitSet = new HashSet<string>(card.Traits.Select(t => t.Value));
        if (traitDef.Prerequisites.Any(prereq => !ownedTraitSet.Contains(prereq)))
            return false;

        var newTraits = new List<CardTraitId>(card.Traits) { normalizedTraitId };
        var newPoints = card.UnspentTraitPoints - 1;
        var success = _profileRepo.UpdateCard(cardInstanceId, new CardUpdate
        {
            Traits = newTraits,
            UnspentTraitPoints = newPoints
        });

        return success;
    }

    public bool SpendCardTraitPoint(CardInstanceId cardInstanceId, string traitId)
    {
        if (string.IsNullOrWhiteSpace(traitId)) return false;
        return SpendCardTraitPoint(cardInstanceId, CardTraitId.FromString(traitId.Trim()));
    }

    public List<CardTraitId> GetAppliedTraits(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return [];

        return new List<CardTraitId>(card.Traits);
    }

    public Dictionary<string, float> GetTraitStatModifiers(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return [];

        var result = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var traitId in card.Traits)
        {
            var traitDef = TraitCatalog.GetTrait(traitId.Value);
            if (traitDef == null)
                continue;

            foreach (var modifier in traitDef.Modifiers)
            {
                if (modifier.StatMults == null || modifier.StatMults.Count == 0)
                    continue;

                foreach (var (stat, multiplier) in modifier.StatMults)
                {
                    if (multiplier <= 0f)
                        continue;

                    var statKey = stat.ToSnakeCase();
                    if (result.TryGetValue(statKey, out var existing))
                    {
                        result[statKey] = existing * multiplier;
                    }
                    else
                    {
                        result[statKey] = multiplier;
                    }
                }
            }
        }

        return result;
    }

    private int ResolveOfferEvaluationLevel(CardInstanceId cardInstanceId, CardInstance card)
    {
        if (card.UnspentTraitPoints > 0)
            return card.Level;

        if (CanLevelUp(cardInstanceId))
            return Math.Min(MaxLevel, card.Level + 1);

        return card.Level;
    }

    private static int ComputeStableOfferOrder(string context, TraitId traitId)
    {
        return DeterministicStringHash($"{context}|{traitId.Value}");
    }

    private static string ResolveOwnerTypeTag(CardDefinition cardDef)
    {
        return cardDef.Type switch
        {
            CardType.Summon => TraitTags.Summon,
            CardType.Spell => TraitTags.Spell,
            _ => ""
        };
    }

    private static HashSet<string> BuildEffectiveCardTagSet(CardDefinition cardDef, string ownerTypeTag)
    {
        var tags = new HashSet<string>(cardDef.TraitEligibilityTags, StringComparer.Ordinal);

        // Normalize stale catalog defaults so owner type always matches CardDefinition.Type.
        tags.Remove(TraitTags.Summon);
        tags.Remove(TraitTags.Spell);

        if (!string.IsNullOrEmpty(ownerTypeTag))
            tags.Add(ownerTypeTag);

        return tags;
    }

    private static int DeterministicStringHash(string value)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash;
        }
    }

    // =========================================================================
    // PROGRESSION INFO
    // =========================================================================

    /// <summary>Get card progression info for UI.</summary>
    public CardProgressionInfo? GetCardProgressionInfo(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return null;

        return new CardProgressionInfo
        {
            CardInstanceId = cardInstanceId.Value,
            CatalogId = card.CatalogId,
            Rarity = card.Rarity,
            RarityMultiplier = GetRarityMultiplier(card.Rarity),
            Level = card.Level,
            MaxLevel = MaxLevel,
            Xp = card.Xp,
            XpForNextLevel = ProgressionEngine.GetXpCostForNextLevel(BuildProgressionState(card), BuildProgressionCurve(card)),
            XpProgress = GetLevelProgress(cardInstanceId),
            CanLevelUp = CanLevelUp(cardInstanceId),
            Traits = card.Traits.ConvertAll(t => t.Value),
            IsMaxLevel = card.Level >= MaxLevel,
            UnspentTraitPoints = card.UnspentTraitPoints
        };
    }

    /// <summary>Get all cards that can level up.</summary>
    public CardInstance[] GetCardsReadyToLevelUp()
    {
        var cards = _profileRepo.ListCards();
        var ready = new List<CardInstance>();

        foreach (var card in cards)
        {
            var state = BuildProgressionState(card);
            var curve = BuildProgressionCurve(card);
            if (ProgressionEngine.CanLevelUp(state, curve))
                ready.Add(card);
        }

        return [.. ready];
    }

    private static ProgressionState BuildProgressionState(CardInstance card)
    {
        return new ProgressionState(card.Level, card.Xp, MaxLevel);
    }

    private static CardProgressionCurve BuildProgressionCurve(CardInstance card)
    {
        return new CardProgressionCurve(card.Rarity);
    }

    private readonly struct CardProgressionCurve(string rarity) : IProgressionCurve
    {
        public int GetXpCostForNextLevel(int currentLevel, int maxLevel)
        {
            if (currentLevel >= maxLevel)
                return 0;

            var multiplier = RarityMultipliers.GetValueOrDefault(rarity.ToLowerInvariant(), 1.0f);
            var currentThreshold = (int)(GetXpThreshold(currentLevel) * multiplier);
            var nextThreshold = (int)(GetXpThreshold(currentLevel + 1) * multiplier);
            return Math.Max(0, nextThreshold - currentThreshold);
        }

        private static int GetXpThreshold(int level)
        {
            if (level < 1 || level > MaxLevel)
                return 0;

            return XpThresholds[level - 1];
        }
    }

    // =========================================================================
    // LEGACY BLOCK REMOVED BELOW
    // =========================================================================
}

/// <summary>
/// Card progression info for UI display.
/// Note: Card leveling requires only XP, not gold.
/// </summary>
public class CardProgressionInfo
{
    public string CardInstanceId { get; set; } = "";
    public string CatalogId { get; set; } = "";
    public string Rarity { get; set; } = "common";
    public float RarityMultiplier { get; set; } = 1f;
    public int Level { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;
    public int Xp { get; set; } = 0;
    public int XpForNextLevel { get; set; } = 0;
    public float XpProgress { get; set; } = 0f;
    public bool CanLevelUp { get; set; } = false;
    public List<string> Traits { get; set; } = [];
    public bool IsMaxLevel { get; set; } = false;
    public int UnspentTraitPoints { get; set; } = 0;
}
