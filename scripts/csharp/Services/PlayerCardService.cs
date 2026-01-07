using Godot;
using System.Collections.Generic;
using ProjectSummoner.Cards;
using ProjectSummoner.Data;
using ProjectSummoner.Services.Interfaces;

namespace ProjectSummoner.Services;

/// <summary>
/// Player Card Service - Manages card progression, XP, levels, and upgrades.
/// Pure C# implementation with typed PlayerCardInstance throughout.
/// </summary>
[GlobalClass]
public partial class PlayerCardService : Node, IPlayerCardService
{
    public static PlayerCardService? Instance { get; private set; }

    // =============================================================================
    // CONSTANTS
    // =============================================================================

    public const int MaxLevel = 10;

    private static readonly int[] XpThresholds = { 0, 30, 75, 150, 300, 500, 800, 1200, 1800, 2500 };
    private static readonly int[] LevelUpGoldCost = { 0, 25, 50, 100, 200, 350, 500, 750, 1000, 1500 };

    private static readonly Dictionary<string, float> RarityMultipliers = new()
    {
        ["common"] = 1.0f,
        ["rare"] = 1.5f,
        ["epic"] = 2.0f,
        ["legendary"] = 3.0f
    };

    // =============================================================================
    // CARD CACHE
    // =============================================================================

    private readonly Dictionary<string, PlayerCardInstance> _cardCache = new();

    // =============================================================================
    // SIGNALS
    // =============================================================================

    [Signal]
    public delegate void CardXpChangedEventHandler(string cardInstanceId, int newXp, int newLevel);

    [Signal]
    public delegate void CardLeveledUpEventHandler(string cardInstanceId, int newLevel);

    [Signal]
    public delegate void UpgradeAppliedEventHandler(string cardInstanceId, string upgradeId);

    // =============================================================================
    // LIFECYCLE
    // =============================================================================

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // =============================================================================
    // STORAGE ABSTRACTION
    // =============================================================================

    private Node? GetProfileRepo() => GetTree()?.Root?.GetNodeOrNull("/root/ProfileRepo");
    private Node? GetEconomy() => GetTree()?.Root?.GetNodeOrNull("/root/Economy");
    private Node? GetCardCatalog() => GetTree()?.Root?.GetNodeOrNull("/root/CardCatalog");

    // =============================================================================
    // CARD ACCESS
    // =============================================================================

    /// <summary>
    /// Get a card by instance ID.
    /// </summary>
    public PlayerCardInstance? GetCard(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return null;

        if (_cardCache.TryGetValue(instanceId, out var cached))
            return cached;

        var repo = GetProfileRepo();
        if (repo == null)
            return null;

        var result = repo.Call("get_card", instanceId);
        if (result.VariantType != Variant.Type.Dictionary)
            return null;

        var card = PlayerCardInstance.FromDictionary(result.AsGodotDictionary());
        _cardCache[instanceId] = card;
        return card;
    }

    /// <summary>
    /// Get all player cards.
    /// </summary>
    public List<PlayerCardInstance> GetAllCards()
    {
        var cards = new List<PlayerCardInstance>();
        var repo = GetProfileRepo();
        if (repo == null)
            return cards;

        var result = repo.Call("list_cards");
        if (result.VariantType != Variant.Type.Array)
            return cards;

        foreach (var cardVar in result.AsGodotArray())
        {
            if (cardVar.VariantType != Variant.Type.Dictionary)
                continue;

            var dict = cardVar.AsGodotDictionary();
            if (!dict.TryGetValue("id", out var idVar))
                continue;

            var card = GetCard(idVar.AsString());
            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    /// <summary>
    /// Save card changes to storage.
    /// </summary>
    private void SaveCard(PlayerCardInstance card)
    {
        if (string.IsNullOrEmpty(card.Id))
            return;

        _cardCache[card.Id] = card;

        var repo = GetProfileRepo();
        repo?.Call("update_card", card.Id, card.ToDictionary());
    }

    // =============================================================================
    // XP OPERATIONS
    // =============================================================================

    /// <summary>
    /// Grant XP to a card.
    /// </summary>
    public int GrantXp(string cardInstanceId, int amount)
    {
        if (amount <= 0)
            return 0;

        var card = GetCard(cardInstanceId);
        if (card == null)
            return 0;

        card.Xp += amount;
        SaveCard(card);

        EmitSignal(SignalName.CardXpChanged, cardInstanceId, card.Xp, card.Level);
        return card.Xp;
    }

    /// <summary>
    /// Grant XP to multiple cards.
    /// </summary>
    public Dictionary<string, int> GrantXpToCards(IEnumerable<string> cardInstanceIds, int amount)
    {
        var results = new Dictionary<string, int>();
        foreach (var cardId in cardInstanceIds)
        {
            results[cardId] = GrantXp(cardId, amount);
        }
        return results;
    }

    /// <summary>
    /// Get XP required for a level (base, no rarity scaling).
    /// </summary>
    public int GetXpForLevel(int level)
    {
        if (level < 1 || level > MaxLevel)
            return 0;
        return XpThresholds[level - 1];
    }

    /// <summary>
    /// Get XP required for a level with rarity scaling.
    /// </summary>
    public int GetXpForLevelWithRarity(int level, string rarity)
    {
        return (int)(GetXpForLevel(level) * GetRarityMultiplier(rarity));
    }

    /// <summary>
    /// Get gold cost for level-up with rarity scaling.
    /// </summary>
    public int GetGoldCostForLevelWithRarity(int level, string rarity)
    {
        if (level < 1 || level > MaxLevel)
            return 0;
        return (int)(LevelUpGoldCost[level - 1] * GetRarityMultiplier(rarity));
    }

    /// <summary>
    /// Get rarity multiplier.
    /// </summary>
    public float GetRarityMultiplier(string rarity)
    {
        return RarityMultipliers.GetValueOrDefault(rarity.ToLower(), 1.0f);
    }

    /// <summary>
    /// Get XP needed for next level.
    /// </summary>
    public int GetXpToNextLevel(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return 0;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return Mathf.Max(0, nextLevelXp - card.Xp);
    }

    /// <summary>
    /// Get progress toward next level (0.0 - 1.0).
    /// </summary>
    public float GetLevelProgress(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
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

    // =============================================================================
    // LEVEL-UP OPERATIONS
    // =============================================================================

    /// <summary>
    /// Check if card has enough XP to level up.
    /// </summary>
    public bool CanLevelUp(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return false;

        int nextLevelXp = GetXpForLevelWithRarity(card.Level + 1, card.Rarity);
        return card.Xp >= nextLevelXp;
    }

    /// <summary>
    /// Get gold cost to level up a card.
    /// </summary>
    public int GetLevelUpGoldCost(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return 0;

        return GetGoldCostForLevelWithRarity(card.Level + 1, card.Rarity);
    }

    /// <summary>
    /// Check if player can afford level-up (XP + gold).
    /// </summary>
    public bool CanAffordLevelUp(string cardInstanceId)
    {
        if (!CanLevelUp(cardInstanceId))
            return false;

        int goldCost = GetLevelUpGoldCost(cardInstanceId);
        var economy = GetEconomy();
        if (economy == null)
            return false;

        var goldResult = economy.Call("get_gold");
        int playerGold = goldResult.VariantType == Variant.Type.Int ? goldResult.AsInt32() : 0;

        return playerGold >= goldCost;
    }

    /// <summary>
    /// Level up a card with chosen upgrade.
    /// </summary>
    public bool LevelUpCard(string cardInstanceId, string upgradeId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return false;

        if (!CanLevelUp(cardInstanceId))
            return false;

        int goldCost = GetLevelUpGoldCost(cardInstanceId);
        var economy = GetEconomy();
        if (economy == null)
            return false;

        var canAffordResult = economy.Call("can_afford", new Godot.Collections.Dictionary { ["gold"] = goldCost });
        if (canAffordResult.VariantType != Variant.Type.Bool || !canAffordResult.AsBool())
            return false;

        // Validate upgrade choice
        var availableUpgrades = GetAvailableUpgrades(cardInstanceId);
        if (!availableUpgrades.Exists(u => u.Id == upgradeId))
            return false;

        // Spend gold
        economy.Call("spend", new Godot.Collections.Dictionary { ["gold"] = goldCost });

        // Apply level up
        card.Level += 1;
        card.Upgrades.Add(upgradeId);
        SaveCard(card);

        EmitSignal(SignalName.CardLeveledUp, cardInstanceId, card.Level);
        EmitSignal(SignalName.UpgradeApplied, cardInstanceId, upgradeId);

        return true;
    }

    // =============================================================================
    // UPGRADE OPERATIONS
    // =============================================================================

    /// <summary>
    /// Get available upgrades for card's next level.
    /// </summary>
    public List<CardUpgrade> GetAvailableUpgrades(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null || card.Level >= MaxLevel)
            return new List<CardUpgrade>();

        return CardUpgradeCatalog.GetUpgradesForLevel(card.CatalogId, card.Level + 1);
    }

    /// <summary>
    /// Get all upgrades applied to a card.
    /// </summary>
    public List<string> GetAppliedUpgrades(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new List<string>();

        return new List<string>(card.Upgrades);
    }

    /// <summary>
    /// Get stat modifiers from card's upgrades.
    /// </summary>
    public Dictionary<string, float> GetUpgradeStatModifiers(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Dictionary<string, float>();

        var modifiers = new Dictionary<string, float>();

        foreach (var upgradeId in card.Upgrades)
        {
            var upgrade = CardUpgradeCatalog.GetUpgrade(card.CatalogId, upgradeId);
            if (upgrade == null)
                continue;

            foreach (var (stat, mult) in upgrade.StatMods)
            {
                if (modifiers.ContainsKey(stat))
                    modifiers[stat] *= mult;
                else
                    modifiers[stat] = mult;
            }
        }

        return modifiers;
    }

    // =============================================================================
    // EFFECTIVE STATS
    // =============================================================================

    /// <summary>
    /// Get effective stats for a card (base + upgrades).
    /// </summary>
    public Dictionary<string, float> GetEffectiveStats(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Dictionary<string, float>();

        var catalog = GetCardCatalog();
        if (catalog == null)
            return new Dictionary<string, float>();

        var baseResult = catalog.Call("get_card", card.CatalogId);
        if (baseResult.VariantType != Variant.Type.Dictionary)
            return new Dictionary<string, float>();

        var baseStats = baseResult.AsGodotDictionary();
        var effectiveStats = new Dictionary<string, float>();

        // Extract numeric stats
        string[] statKeys = { "max_hp", "attack_damage", "attack_range", "attack_speed", "move_speed", "aggro_radius" };
        foreach (var key in statKeys)
        {
            if (baseStats.TryGetValue(key, out var val))
            {
                effectiveStats[key] = val.VariantType == Variant.Type.Float
                    ? val.AsSingle()
                    : val.AsInt32();
            }
        }

        // Apply upgrade modifiers
        var modifiers = GetUpgradeStatModifiers(cardInstanceId);
        foreach (var (stat, mult) in modifiers)
        {
            if (effectiveStats.ContainsKey(stat))
                effectiveStats[stat] *= mult;
        }

        return effectiveStats;
    }

    // =============================================================================
    // QUERY HELPERS
    // =============================================================================

    /// <summary>
    /// Get card progression info.
    /// </summary>
    public CardProgressionInfo? GetCardProgressionInfo(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
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
            CanAffordLevelUp = CanAffordLevelUp(cardInstanceId),
            LevelUpGoldCost = GetLevelUpGoldCost(cardInstanceId),
            Upgrades = new List<string>(card.Upgrades),
            IsMaxLevel = card.Level >= MaxLevel
        };
    }

    /// <summary>
    /// Get all cards that can level up.
    /// </summary>
    public List<PlayerCardInstance> GetCardsReadyToLevelUp()
    {
        var ready = new List<PlayerCardInstance>();
        foreach (var card in GetAllCards())
        {
            if (CanLevelUp(card.Id))
                ready.Add(card);
        }
        return ready;
    }
}

/// <summary>
/// Card progression info for UI display.
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
    public bool CanAffordLevelUp { get; set; } = false;
    public int LevelUpGoldCost { get; set; } = 0;
    public List<string> Upgrades { get; set; } = new();
    public bool IsMaxLevel { get; set; } = false;
}

