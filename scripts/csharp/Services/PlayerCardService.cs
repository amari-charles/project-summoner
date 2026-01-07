using Godot;
using System.Collections.Generic;
using ProjectSummoner.Cards;
using ProjectSummoner.Data;
using ProjectSummoner.Services.Interfaces;

namespace ProjectSummoner.Services;

/// <summary>
/// Player Card Service - Manages card progression, XP, levels, and upgrades.
/// This C# service abstracts storage (ProfileRepo now, DB later) and provides
/// the stat calculation pipeline for cards.
///
/// Uses PlayerCardInstance for type-safe internal operations while maintaining
/// GDScript compatibility via dictionary-based methods.
///
/// Replaces GDScript card_progression_service.gd
/// </summary>
[GlobalClass]
public partial class PlayerCardService : Node, IPlayerCardService
{
    // Singleton instance for easy access
    public static PlayerCardService? Instance { get; private set; }

    // =============================================================================
    // CONSTANTS
    // =============================================================================

    public const int MaxLevel = 10;

    /// <summary>
    /// XP thresholds for each level (cumulative XP needed)
    /// Index 0 = Level 1 (no XP needed), Index 1 = Level 2, etc.
    /// </summary>
    private static readonly int[] XpThresholds = { 0, 30, 75, 150, 300, 500, 800, 1200, 1800, 2500 };

    /// <summary>
    /// Gold cost per level-up (index = target level - 1)
    /// </summary>
    private static readonly int[] LevelUpGoldCost = { 0, 25, 50, 100, 200, 350, 500, 750, 1000, 1500 };

    /// <summary>
    /// Rarity multipliers for XP thresholds and gold costs
    /// </summary>
    private static readonly Dictionary<string, float> RarityMultipliers = new()
    {
        ["common"] = 1.0f,
        ["rare"] = 1.5f,
        ["epic"] = 2.0f,
        ["legendary"] = 3.0f
    };

    // =============================================================================
    // TYPED CARD CACHE
    // =============================================================================

    /// <summary>
    /// Internal cache of typed card instances.
    /// Populated lazily from ProfileRepo, invalidated on updates.
    /// </summary>
    private readonly Dictionary<string, PlayerCardInstance> _cardCache = new();

    // =============================================================================
    // SIGNALS (for reactive UI)
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
    // STORAGE ABSTRACTION (currently uses ProfileRepo, later can use DB)
    // =============================================================================

    private Node? GetProfileRepo()
    {
        return GetTree()?.Root?.GetNodeOrNull("/root/ProfileRepo");
    }

    private Node? GetEconomy()
    {
        return GetTree()?.Root?.GetNodeOrNull("/root/Economy");
    }

    private Node? GetCardCatalog()
    {
        return GetTree()?.Root?.GetNodeOrNull("/root/CardCatalog");
    }

    /// <summary>
    /// Get a typed card instance (internal use).
    /// Uses cache for performance, fetches from ProfileRepo if not cached.
    /// </summary>
    private PlayerCardInstance? GetCard(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return null;

        // Check cache first
        if (_cardCache.TryGetValue(instanceId, out var cached))
            return cached;

        // Fetch from ProfileRepo
        var dict = GetCardInstanceDict(instanceId);
        if (dict.Count == 0)
            return null;

        // Create typed instance and cache it
        var card = PlayerCardInstance.FromDictionary(dict);
        _cardCache[instanceId] = card;
        return card;
    }

    /// <summary>
    /// Get all cards as typed instances (internal use).
    /// </summary>
    private List<PlayerCardInstance> GetAllCards()
    {
        var cards = new List<PlayerCardInstance>();
        var cardDicts = ListCards();

        foreach (var cardVar in cardDicts)
        {
            if (cardVar.VariantType != Variant.Type.Dictionary)
                continue;

            var dict = cardVar.AsGodotDictionary();
            if (!dict.TryGetValue("id", out var idVar))
                continue;

            string cardId = idVar.AsString();
            var card = GetCard(cardId);
            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    /// <summary>
    /// Update a card instance and sync to storage.
    /// Invalidates cache entry to ensure fresh data on next read.
    /// </summary>
    private void UpdateCard(PlayerCardInstance card)
    {
        if (string.IsNullOrEmpty(card.Id))
            return;

        // Update cache
        _cardCache[card.Id] = card;

        // Sync to ProfileRepo
        var repo = GetProfileRepo();
        if (repo == null)
        {
            GD.PushWarning("PlayerCardService: ProfileRepo not available");
            return;
        }

        repo.Call("update_card", card.Id, card.ToDictionary());
    }

    /// <summary>
    /// Invalidate cache for a specific card (forces re-fetch on next access)
    /// </summary>
    private void InvalidateCache(string instanceId)
    {
        _cardCache.Remove(instanceId);
    }

    /// <summary>
    /// Clear all cached cards
    /// </summary>
    private void ClearCache()
    {
        _cardCache.Clear();
    }

    /// <summary>
    /// Get a card instance from storage as dictionary (internal use)
    /// </summary>
    private Godot.Collections.Dictionary GetCardInstanceDict(string instanceId)
    {
        var repo = GetProfileRepo();
        if (repo == null)
        {
            GD.PushWarning("PlayerCardService: ProfileRepo not available");
            return new Godot.Collections.Dictionary();
        }

        var result = repo.Call("get_card", instanceId);
        if (result.VariantType == Variant.Type.Dictionary)
            return result.AsGodotDictionary();

        return new Godot.Collections.Dictionary();
    }

    /// <summary>
    /// List all cards in storage (returns array of dictionaries)
    /// </summary>
    private Godot.Collections.Array ListCards()
    {
        var repo = GetProfileRepo();
        if (repo == null)
            return new Godot.Collections.Array();

        var result = repo.Call("list_cards");
        if (result.VariantType == Variant.Type.Array)
            return result.AsGodotArray();

        return new Godot.Collections.Array();
    }

    /// <summary>
    /// Get base stats from CardCatalog
    /// </summary>
    private Godot.Collections.Dictionary GetBaseStats(string catalogId)
    {
        var catalog = GetCardCatalog();
        if (catalog == null)
        {
            GD.PushWarning("PlayerCardService: CardCatalog not available");
            return new Godot.Collections.Dictionary();
        }

        var result = catalog.Call("get_card", catalogId);
        if (result.VariantType == Variant.Type.Dictionary)
            return result.AsGodotDictionary();

        return new Godot.Collections.Dictionary();
    }

    // =============================================================================
    // XP OPERATIONS (snake_case for GDScript compatibility)
    // =============================================================================

    /// <summary>
    /// Grant XP to a card instance. Returns the new total XP.
    /// </summary>
    public int grant_card_xp(string cardInstanceId, int amount)
    {
        if (amount <= 0)
            return 0;

        var card = GetCard(cardInstanceId);
        if (card == null)
        {
            GD.PushWarning($"PlayerCardService: Card instance not found: {cardInstanceId}");
            return 0;
        }

        card.Xp += amount;
        UpdateCard(card);

        EmitSignal(SignalName.CardXpChanged, cardInstanceId, card.Xp, card.Level);

        return card.Xp;
    }

    /// <summary>
    /// Grant XP to multiple cards at once. Returns Dictionary of {card_instance_id: new_xp}
    /// </summary>
    public Godot.Collections.Dictionary grant_xp_to_cards(Godot.Collections.Array cardInstanceIds, int amount)
    {
        var results = new Godot.Collections.Dictionary();

        foreach (var cardIdVar in cardInstanceIds)
        {
            if (cardIdVar.VariantType == Variant.Type.String)
            {
                string cardId = cardIdVar.AsString();
                int newXp = grant_card_xp(cardId, amount);
                results[cardId] = newXp;
            }
        }

        return results;
    }

    /// <summary>
    /// Get XP required to reach a specific level (base, no rarity scaling)
    /// </summary>
    public int get_xp_for_level(int level)
    {
        if (level < 1 || level > MaxLevel)
            return 0;
        return XpThresholds[level - 1];
    }

    /// <summary>
    /// Get XP required to reach a specific level with rarity scaling
    /// </summary>
    public int get_xp_for_level_with_rarity(int level, string rarity)
    {
        int baseXp = get_xp_for_level(level);
        float multiplier = get_rarity_multiplier(rarity);
        return (int)(baseXp * multiplier);
    }

    /// <summary>
    /// Get gold cost for leveling up with rarity scaling
    /// </summary>
    public int get_gold_cost_for_level_with_rarity(int level, string rarity)
    {
        if (level < 1 || level > MaxLevel)
            return 0;
        int baseCost = LevelUpGoldCost[level - 1];
        float multiplier = get_rarity_multiplier(rarity);
        return (int)(baseCost * multiplier);
    }

    /// <summary>
    /// Get rarity multiplier for a given rarity
    /// </summary>
    public float get_rarity_multiplier(string rarity)
    {
        return RarityMultipliers.GetValueOrDefault(rarity.ToLower(), 1.0f);
    }

    /// <summary>
    /// Get XP needed for the next level from current XP (with rarity scaling)
    /// </summary>
    public int get_xp_to_next_level(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return 0;

        if (card.Level >= MaxLevel)
            return 0;

        int nextLevelXp = get_xp_for_level_with_rarity(card.Level + 1, card.Rarity);
        return Mathf.Max(0, nextLevelXp - card.Xp);
    }

    /// <summary>
    /// Get progress towards next level as a percentage (0.0 - 1.0) with rarity scaling
    /// </summary>
    public float get_level_progress(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return 0.0f;

        if (card.Level >= MaxLevel)
            return 1.0f;

        int currentLevelXp = get_xp_for_level_with_rarity(card.Level, card.Rarity);
        int nextLevelXp = get_xp_for_level_with_rarity(card.Level + 1, card.Rarity);
        int levelRange = nextLevelXp - currentLevelXp;

        if (levelRange <= 0)
            return 1.0f;

        int progressInLevel = card.Xp - currentLevelXp;
        return Mathf.Clamp((float)progressInLevel / levelRange, 0.0f, 1.0f);
    }

    // =============================================================================
    // LEVEL-UP OPERATIONS
    // =============================================================================

    /// <summary>
    /// Check if a card has enough XP to level up (with rarity scaling)
    /// </summary>
    public bool can_level_up(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return false;

        if (card.Level >= MaxLevel)
            return false;

        int nextLevelXp = get_xp_for_level_with_rarity(card.Level + 1, card.Rarity);
        return card.Xp >= nextLevelXp;
    }

    /// <summary>
    /// Get the gold cost to level up a card (with rarity scaling)
    /// </summary>
    public int get_level_up_gold_cost(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return 0;

        if (card.Level >= MaxLevel)
            return 0;

        return get_gold_cost_for_level_with_rarity(card.Level + 1, card.Rarity);
    }

    /// <summary>
    /// Check if player can afford to level up (XP + gold)
    /// </summary>
    public bool can_afford_level_up(string cardInstanceId)
    {
        if (!can_level_up(cardInstanceId))
            return false;

        int goldCost = get_level_up_gold_cost(cardInstanceId);

        var economy = GetEconomy();
        if (economy == null)
            return false;

        var goldResult = economy.Call("get_gold");
        int playerGold = goldResult.VariantType == Variant.Type.Int ? goldResult.AsInt32() : 0;

        return playerGold >= goldCost;
    }

    /// <summary>
    /// Level up a card (requires XP threshold met + gold + upgrade choice)
    /// Returns true if successful
    /// </summary>
    public bool level_up_card(string cardInstanceId, string upgradeId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
        {
            GD.PushWarning($"PlayerCardService: Card not found: {cardInstanceId}");
            return false;
        }

        // Check XP requirement
        if (!can_level_up(cardInstanceId))
        {
            GD.PushWarning("PlayerCardService: Card does not have enough XP to level up");
            return false;
        }

        // Check gold cost
        int goldCost = get_level_up_gold_cost(cardInstanceId);
        var economy = GetEconomy();
        if (economy == null)
        {
            GD.PushWarning("PlayerCardService: Economy not available");
            return false;
        }

        var canAffordResult = economy.Call("can_afford", new Godot.Collections.Dictionary { ["gold"] = goldCost });
        bool canAfford = canAffordResult.VariantType == Variant.Type.Bool && canAffordResult.AsBool();
        if (!canAfford)
        {
            GD.PushWarning($"PlayerCardService: Cannot afford level-up cost ({goldCost} gold)");
            return false;
        }

        // Validate upgrade choice
        var availableUpgrades = get_available_upgrades(cardInstanceId);
        bool validUpgrade = false;
        foreach (var upgradeVar in availableUpgrades)
        {
            if (upgradeVar.VariantType == Variant.Type.Dictionary)
            {
                var upgrade = upgradeVar.AsGodotDictionary();
                if (upgrade.TryGetValue("id", out var idVar) && idVar.AsString() == upgradeId)
                {
                    validUpgrade = true;
                    break;
                }
            }
        }

        if (!validUpgrade)
        {
            GD.PushWarning($"PlayerCardService: Invalid upgrade choice: {upgradeId}");
            return false;
        }

        // Spend gold
        economy.Call("spend", new Godot.Collections.Dictionary { ["gold"] = goldCost });

        // Apply level up using typed instance
        card.Level += 1;
        card.Upgrades.Add(upgradeId);
        UpdateCard(card);

        EmitSignal(SignalName.CardLeveledUp, cardInstanceId, card.Level);
        EmitSignal(SignalName.UpgradeApplied, cardInstanceId, upgradeId);

        return true;
    }

    // =============================================================================
    // UPGRADE OPERATIONS
    // =============================================================================

    /// <summary>
    /// Get available upgrades for a card's next level
    /// </summary>
    public Godot.Collections.Array get_available_upgrades(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Godot.Collections.Array();

        int nextLevel = card.Level + 1;

        if (nextLevel > MaxLevel)
            return new Godot.Collections.Array();

        return CardUpgradeCatalog.get_upgrades_for_level(card.CatalogId, nextLevel);
    }

    /// <summary>
    /// Get all upgrades currently applied to a card
    /// </summary>
    public Godot.Collections.Array get_applied_upgrades(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Godot.Collections.Array();

        // Convert typed array to Godot array
        var result = new Godot.Collections.Array();
        foreach (var upgrade in card.Upgrades)
        {
            result.Add(upgrade);
        }
        return result;
    }

    /// <summary>
    /// Get the stat modifiers from a card's upgrades
    /// Returns Dictionary of stat multipliers (e.g., {"max_hp": 1.1, "attack_damage": 1.05})
    /// </summary>
    public Godot.Collections.Dictionary get_upgrade_stat_modifiers(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Godot.Collections.Dictionary();

        var modifiers = new Godot.Collections.Dictionary();

        // Combine all stat modifiers from applied upgrades (multiplicative)
        foreach (var upgradeId in card.Upgrades)
        {
            var upgrade = CardUpgradeCatalog.get_upgrade(card.CatalogId, upgradeId);
            if (!upgrade.TryGetValue("stat_mods", out var statModsVar))
                continue;
            if (statModsVar.VariantType != Variant.Type.Dictionary)
                continue;

            var statMods = statModsVar.AsGodotDictionary();
            foreach (var key in statMods.Keys)
            {
                if (key.VariantType != Variant.Type.String)
                    continue;

                string statKey = key.AsString();
                var modValue = statMods[key];
                float modFloat = modValue.VariantType == Variant.Type.Float
                    ? modValue.AsSingle()
                    : modValue.AsInt32();

                if (modifiers.ContainsKey(statKey))
                {
                    float existing = modifiers[statKey].AsSingle();
                    modifiers[statKey] = existing * modFloat;
                }
                else
                {
                    modifiers[statKey] = modFloat;
                }
            }
        }

        return modifiers;
    }

    // =============================================================================
    // EFFECTIVE STATS (the main stat pipeline)
    // =============================================================================

    /// <summary>
    /// Get effective stats for a card instance, combining:
    /// 1. Base catalog stats
    /// 2. Level-based growth (TODO: not yet implemented)
    /// 3. Upgrade modifiers (multiplicative)
    /// 4. Summoner boons (TODO: from SummonerInstance)
    /// </summary>
    public Godot.Collections.Dictionary get_effective_stats(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Godot.Collections.Dictionary();

        // Get base stats from catalog (deep copy)
        var baseStats = GetBaseStats(card.CatalogId);
        if (baseStats.Count == 0)
            return new Godot.Collections.Dictionary();

        // Create a deep copy to avoid modifying the catalog
        var effectiveStats = DeepCopyDictionary(baseStats);

        // Apply upgrade modifiers (multiplicative)
        var modifiers = get_upgrade_stat_modifiers(cardInstanceId);
        foreach (var key in modifiers.Keys)
        {
            if (key.VariantType != Variant.Type.String)
                continue;

            string statKey = key.AsString();
            if (!effectiveStats.ContainsKey(statKey))
                continue;

            float multiplier = modifiers[key].AsSingle();
            var baseVal = effectiveStats[statKey];

            if (baseVal.VariantType == Variant.Type.Float)
            {
                effectiveStats[statKey] = baseVal.AsSingle() * multiplier;
            }
            else if (baseVal.VariantType == Variant.Type.Int)
            {
                effectiveStats[statKey] = (int)(baseVal.AsInt32() * multiplier);
            }
        }

        // TODO: Apply level-based growth
        // TODO: Apply summoner boons

        return effectiveStats;
    }

    private Godot.Collections.Dictionary DeepCopyDictionary(Godot.Collections.Dictionary original)
    {
        var copy = new Godot.Collections.Dictionary();
        foreach (var key in original.Keys)
        {
            var value = original[key];
            if (value.VariantType == Variant.Type.Dictionary)
            {
                copy[key] = DeepCopyDictionary(value.AsGodotDictionary());
            }
            else if (value.VariantType == Variant.Type.Array)
            {
                copy[key] = DeepCopyArray(value.AsGodotArray());
            }
            else
            {
                copy[key] = value;
            }
        }
        return copy;
    }

    private Godot.Collections.Array DeepCopyArray(Godot.Collections.Array original)
    {
        var copy = new Godot.Collections.Array();
        foreach (var item in original)
        {
            if (item.VariantType == Variant.Type.Dictionary)
            {
                copy.Add(DeepCopyDictionary(item.AsGodotDictionary()));
            }
            else if (item.VariantType == Variant.Type.Array)
            {
                copy.Add(DeepCopyArray(item.AsGodotArray()));
            }
            else
            {
                copy.Add(item);
            }
        }
        return copy;
    }

    // =============================================================================
    // QUERY HELPERS
    // =============================================================================

    /// <summary>
    /// Get card progression info (for UI display)
    /// </summary>
    public Godot.Collections.Dictionary get_card_progression_info(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        if (card == null)
            return new Godot.Collections.Dictionary();

        // Convert upgrades to Godot array
        var upgrades = new Godot.Collections.Array();
        foreach (var u in card.Upgrades)
            upgrades.Add(u);

        return new Godot.Collections.Dictionary
        {
            ["card_instance_id"] = cardInstanceId,
            ["catalog_id"] = card.CatalogId,
            ["rarity"] = card.Rarity,
            ["rarity_multiplier"] = get_rarity_multiplier(card.Rarity),
            ["level"] = card.Level,
            ["max_level"] = MaxLevel,
            ["xp"] = card.Xp,
            ["xp_for_next_level"] = card.Level < MaxLevel ? get_xp_for_level_with_rarity(card.Level + 1, card.Rarity) : 0,
            ["xp_progress"] = get_level_progress(cardInstanceId),
            ["can_level_up"] = can_level_up(cardInstanceId),
            ["can_afford_level_up"] = can_afford_level_up(cardInstanceId),
            ["level_up_gold_cost"] = get_level_up_gold_cost(cardInstanceId),
            ["upgrades"] = upgrades,
            ["is_max_level"] = card.Level >= MaxLevel
        };
    }

    /// <summary>
    /// Get all cards that can level up
    /// </summary>
    public Godot.Collections.Array<string> get_cards_ready_to_level_up()
    {
        var ready = new Godot.Collections.Array<string>();

        foreach (var card in GetAllCards())
        {
            if (can_level_up(card.Id))
                ready.Add(card.Id);
        }

        return ready;
    }
}
