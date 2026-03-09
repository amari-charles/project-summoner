using System.Collections.Generic;
using Godot;
using Fateforged.Cards;
using Fateforged.Data;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Traits.Unified;

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
        if (card == null || card.Level >= MaxLevel)
            return 0;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return Mathf.Max(0, nextLevelXp - card.Xp);
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
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
    public bool CanLevelUp(CardInstanceId cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return false;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return card.Xp >= nextLevelXp;
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

        // Apply level up (XP-only, no gold cost)
        var newLevel = card.Level + 1;

        _profileRepo.UpdateCard(cardInstanceId, new CardUpdate
        {
            Level = newLevel,
            UnspentTraitPoints = card.UnspentTraitPoints + 1
        });

        GD.Print($"CardProgressionHandler: Leveled up card '{cardInstanceId}' to level {newLevel} and granted 1 trait point");
        return true;
    }

    /// <summary>
    /// Legacy API retained as a wrapper in Pass 2.
    /// Trait choice is ignored; points are deferred for SpendCardTraitPoint.
    /// </summary>
    public bool LevelUpCard(CardInstanceId cardInstanceId, CardTraitId traitId)
    {
        _ = traitId;
        return LevelUpCard(cardInstanceId);
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
        _ = cardInstanceId;
        _ = count;
        // Pass 2 scaffold: offer rolling is implemented in Pass 3.
        return [];
    }

    public bool SpendCardTraitPoint(CardInstanceId cardInstanceId, CardTraitId traitId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null) return false;
        if (card.UnspentTraitPoints <= 0) return false;
        if (traitId == CardTraitId.None) return false;
        if (card.Traits.Contains(traitId)) return false;

        var newTraits = new List<CardTraitId>(card.Traits) { traitId };
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
        return SpendCardTraitPoint(cardInstanceId, CardTraitId.FromString(traitId));
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
        _ = cardInstanceId;
        // Pass 2 removes legacy CardTraitCatalog stat modifiers from runtime.
        return [];
    }

    // =========================================================================
    // TRAIT OPERATIONS (legacy compatibility stubs)
    // =========================================================================

    /// <summary>Deprecated by unified trait flow. Returns no offers in Pass 2.</summary>
    public List<CardTrait> GetAvailableTraits(CardInstanceId cardInstanceId)
    {
        _ = cardInstanceId;
        return [];
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
            XpForNextLevel = card.Level < MaxLevel ? GetXpForLevelWithRarity(card.Level + 1, card.Rarity) : 0,
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
            if (card.Level < MaxLevel)
            {
                int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
                if (card.Xp >= nextLevelXp)
                    ready.Add(card);
            }
        }

        return [.. ready];
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
