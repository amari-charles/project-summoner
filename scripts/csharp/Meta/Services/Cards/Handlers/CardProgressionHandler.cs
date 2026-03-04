using System.Collections.Generic;
using Godot;
using Fateforged.Cards;
using Fateforged.Data;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;

namespace Fateforged.Meta.Cards.Handlers;

/// <summary>
/// Handles card progression: XP, leveling, and traits.
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
    public int GrantXp(string cardInstanceId, int amount)
    {
        if (amount <= 0)
            return 0;

        var typedCardId = new CardInstanceId(cardInstanceId);
        var card = _profileRepo.GetCard(typedCardId);
        if (card == null)
        {
            GD.PushWarning($"CardProgressionHandler: Card not found: {cardInstanceId}");
            return 0;
        }

        var newXp = card.Xp + amount;
        _profileRepo.UpdateCard(typedCardId, new CardUpdate { Xp = newXp });

        GD.Print($"CardProgressionHandler: Granted {amount} XP to card '{cardInstanceId}' (now: {newXp})");
        return newXp;
    }

    /// <summary>Grant XP to multiple cards.</summary>
    public Dictionary<string, int> GrantXpToCards(IEnumerable<string> cardInstanceIds, int amount)
    {
        var results = new Dictionary<string, int>();
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
    public int GetXpToNextLevel(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null || card.Level >= MaxLevel)
            return 0;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return Mathf.Max(0, nextLevelXp - card.Xp);
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null)
            return 0f;

        if (card.Level >= MaxLevel)
            return 1f;

        int currentLevelXp = GetXpForLevelWithRarity(card.Level, card.Rarity);
        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        int levelRange = nextLevelXp - currentLevelXp;

        if (levelRange <= 0)
            return 1f;

        return Mathf.Clamp((float)(card.Xp - currentLevelXp) / levelRange, 0f, 1f);
    }

    // =========================================================================
    // LEVEL-UP OPERATIONS
    // =========================================================================

    /// <summary>Check if card has enough XP to level up.</summary>
    public bool CanLevelUp(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null || card.Level >= MaxLevel)
            return false;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return card.Xp >= nextLevelXp;
    }

    /// <summary>
    /// Level up a card with chosen trait.
    /// Requires only XP - no gold cost.
    /// Returns true if successful.
    /// </summary>
    public bool LevelUpCard(string cardInstanceId, CardTraitId traitId) =>
        LevelUpCardInternal(cardInstanceId, traitId);

    /// <summary>
    /// Level up a card with chosen trait (string overload).
    /// </summary>
    public bool LevelUpCard(string cardInstanceId, string traitId) =>
        LevelUpCardInternal(cardInstanceId, new CardTraitId(traitId));

    private bool LevelUpCardInternal(string cardInstanceId, CardTraitId traitId)
    {
        var typedCardId = new CardInstanceId(cardInstanceId);
        var card = _profileRepo.GetCard(typedCardId);
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

        // Validate trait choice
        var availableTraits = GetAvailableTraits(cardInstanceId);
        if (!availableTraits.Exists(t => t.Id == traitId))
        {
            GD.PushWarning($"CardProgressionHandler: Invalid trait choice: {traitId}");
            return false;
        }

        // Apply level up (XP-only, no gold cost)
        var newLevel = card.Level + 1;
        var newTraits = new List<CardTraitId>(card.Traits) { traitId };

        _profileRepo.UpdateCard(typedCardId, new CardUpdate
        {
            Level = newLevel,
            Traits = newTraits
        });

        GD.Print($"CardProgressionHandler: Leveled up card '{cardInstanceId}' to level {newLevel} with trait '{traitId}'");
        return true;
    }

    // =========================================================================
    // TRAIT OPERATIONS
    // =========================================================================

    /// <summary>Get available traits for card's next level.</summary>
    public List<CardTrait> GetAvailableTraits(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null || card.Level >= MaxLevel)
            return [];

        return CardTraitCatalog.GetTraitsForLevel(card.CatalogId, card.Level + 1);
    }

    /// <summary>Get all traits applied to a card.</summary>
    public List<CardTraitId> GetAppliedTraits(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null)
            return [];

        return new List<CardTraitId>(card.Traits);
    }

    /// <summary>Get stat modifiers from card's traits.</summary>
    public Dictionary<string, float> GetTraitStatModifiers(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null)
            return [];

        var modifiers = new Dictionary<string, float>();

        foreach (var traitId in card.Traits)
        {
            var trait = CardTraitCatalog.GetTrait(card.CatalogId, traitId);
            if (trait == null)
                continue;

            foreach (var (stat, mult) in trait.StatMods)
            {
                if (modifiers.TryGetValue(stat, out var existing))
                    modifiers[stat] = existing * mult;
                else
                    modifiers[stat] = mult;
            }
        }

        return modifiers;
    }

    // =========================================================================
    // PROGRESSION INFO
    // =========================================================================

    /// <summary>Get card progression info for UI.</summary>
    public CardProgressionInfo? GetCardProgressionInfo(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(new CardInstanceId(cardInstanceId));
        if (card == null)
            return null;

        return new CardProgressionInfo
        {
            CardInstanceId = cardInstanceId,
            CatalogId = card.CatalogId,
            Rarity = card.Rarity,
            RarityMultiplier = GetRarityMultiplier(card.Rarity),
            Level = card.Level,
            MaxLevel = MaxLevel,
            Xp = card.Xp,
            XpForNextLevel = card.Level < MaxLevel ? GetXpForLevelWithRarity(card.Level + 1, card.Rarity) : 0,
            XpProgress = GetLevelProgress(cardInstanceId),
            CanLevelUp = CanLevelUp(cardInstanceId),
            Traits = card.Traits.ConvertAll(t => t.Value),
            IsMaxLevel = card.Level >= MaxLevel
        };
    }

    /// <summary>Get all cards that can level up.</summary>
    public CardInstance[] GetCardsReadyToLevelUp()
    {
        var cards = _profileRepo.ListCards();
        var ready = new List<CardInstance>();

        foreach (var card in cards)
        {
            if (card.Level < MaxLevel)
            {
                int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
                if (card.Xp >= nextLevelXp)
                    ready.Add(card);
            }
        }

        return [.. ready];
    }
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
}
