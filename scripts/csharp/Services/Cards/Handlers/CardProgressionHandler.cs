using System.Collections.Generic;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Data;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Cards.Handlers;

/// <summary>
/// Handles card progression: XP, leveling, and upgrades.
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
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return 0;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return Mathf.Max(0, nextLevelXp - card.Xp);
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(string cardInstanceId)
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
    public bool CanLevelUp(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return false;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return card.Xp >= nextLevelXp;
    }

    /// <summary>
    /// Level up a card with chosen upgrade.
    /// Requires only XP - no gold cost.
    /// Returns true if successful.
    /// </summary>
    public bool LevelUpCard(string cardInstanceId, CardUpgradeId upgradeId) =>
        LevelUpCardInternal(cardInstanceId, upgradeId);

    /// <summary>
    /// Level up a card with chosen upgrade (string overload).
    /// </summary>
    public bool LevelUpCard(string cardInstanceId, string upgradeId) =>
        LevelUpCardInternal(cardInstanceId, new CardUpgradeId(upgradeId));

    private bool LevelUpCardInternal(string cardInstanceId, CardUpgradeId upgradeId)
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

        // Validate upgrade choice
        var availableUpgrades = GetAvailableUpgrades(cardInstanceId);
        if (!availableUpgrades.Exists(u => u.Id == upgradeId))
        {
            GD.PushWarning($"CardProgressionHandler: Invalid upgrade choice: {upgradeId}");
            return false;
        }

        // Apply level up (XP-only, no gold cost)
        var newLevel = card.Level + 1;
        var newUpgrades = new List<string>(card.Upgrades) { upgradeId };

        _profileRepo.UpdateCard(cardInstanceId, new CardUpdate
        {
            Level = newLevel,
            Upgrades = newUpgrades
        });

        GD.Print($"CardProgressionHandler: Leveled up card '{cardInstanceId}' to level {newLevel} with upgrade '{upgradeId}'");
        return true;
    }

    // =========================================================================
    // UPGRADE OPERATIONS
    // =========================================================================

    /// <summary>Get available upgrades for card's next level.</summary>
    public List<CardUpgrade> GetAvailableUpgrades(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return [];

        return CardUpgradeCatalog.GetUpgradesForLevel(card.CatalogId, card.Level + 1);
    }

    /// <summary>Get all upgrades applied to a card.</summary>
    public List<string> GetAppliedUpgrades(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return [];

        return new List<string>(card.Upgrades);
    }

    /// <summary>Get stat modifiers from card's upgrades.</summary>
    public Dictionary<string, float> GetUpgradeStatModifiers(string cardInstanceId)
    {
        var card = _profileRepo.GetCard(cardInstanceId);
        if (card == null)
            return [];

        var modifiers = new Dictionary<string, float>();

        foreach (var upgradeId in card.Upgrades)
        {
            var upgrade = CardUpgradeCatalog.GetUpgrade(card.CatalogId, upgradeId);
            if (upgrade == null)
                continue;

            foreach (var (stat, mult) in upgrade.StatMods)
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
        var card = _profileRepo.GetCard(cardInstanceId);
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
            Upgrades = new List<string>(card.Upgrades),
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
    public List<string> Upgrades { get; set; } = [];
    public bool IsMaxLevel { get; set; } = false;
}
