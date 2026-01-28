using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Services.Cards.Handlers;

namespace ProjectSummoner.Services.Cards;

/// <summary>
/// Card Service - Unified service for card ownership and progression.
///
/// Combines functionality from CollectionService (ownership) and PlayerCardService (progression).
/// Uses Facade + Handlers pattern for clean separation of concerns.
/// </summary>
[GlobalClass]
public partial class CardService : Node
{
    public static CardService? Instance { get; private set; }

    private IProfileRepository? _profileRepo;
    private CardOwnershipHandler? _ownership;
    private CardProgressionHandler? _progression;

    // =========================================================================
    // SIGNALS
    // =========================================================================

    [Signal]
    public delegate void CardsGrantedEventHandler(Godot.Collections.Array<string> instanceIds);

    [Signal]
    public delegate void CardRemovedEventHandler(string instanceId);

    [Signal]
    public delegate void CardXpChangedEventHandler(string cardInstanceId, int newXp, int newLevel);

    [Signal]
    public delegate void CardLeveledUpEventHandler(string cardInstanceId, int newLevel);

    [Signal]
    public delegate void UpgradeAppliedEventHandler(string cardInstanceId, string upgradeId);

    [Signal]
    public delegate void CollectionChangedEventHandler();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        Initialize();
    }

    private void Initialize()
    {
        GD.Print("CardService: Initializing...");

        _profileRepo = ProfileRepository.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("CardService: ProfileRepository.Instance not available");
            return;
        }

        _ownership = new CardOwnershipHandler(_profileRepo);
        _progression = new CardProgressionHandler(_profileRepo);

        _profileRepo.DataChanged += OnRepoDataChanged;

        GD.Print("CardService: Ready");
    }

    public override void _ExitTree()
    {
        if (_profileRepo != null)
        {
            _profileRepo.DataChanged -= OnRepoDataChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Initialize for testing with mock dependencies.</summary>
    public void InitForTesting(IProfileRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _profileRepo = repo;
        _ownership = new CardOwnershipHandler(repo);
        _progression = new CardProgressionHandler(repo);
    }

    private void OnRepoDataChanged()
    {
        EmitSignal(SignalName.CollectionChanged);
    }

    // =========================================================================
    // OWNERSHIP - QUERIES
    // =========================================================================

    /// <summary>Get all card instances in the collection.</summary>
    public CardInstance[] ListCards()
    {
        return _ownership?.ListCards() ?? [];
    }

    /// <summary>Get a specific card instance by ID.</summary>
    public CardInstance? GetCard(string cardInstanceId)
    {
        return _ownership?.GetCard(cardInstanceId);
    }

    /// <summary>Get count of cards by catalog ID.</summary>
    public int GetCardCount(string catalogId)
    {
        return _ownership?.GetCardCount(catalogId) ?? 0;
    }

    /// <summary>Check if player owns at least one of a card.</summary>
    public bool HasCard(string catalogId)
    {
        return _ownership?.HasCard(catalogId) ?? false;
    }

    /// <summary>Get all instances of a specific catalog ID.</summary>
    public CardInstance[] GetCardsByCatalogId(string catalogId)
    {
        return _ownership?.GetCardsByCatalogId(catalogId) ?? [];
    }

    /// <summary>Get all AccountWide cards.</summary>
    public CardInstance[] GetAccountWideCards()
    {
        return _ownership?.GetAccountWideCards() ?? [];
    }

    /// <summary>Get SummonerBound cards for a specific summoner.</summary>
    public CardInstance[] GetSummonerBoundCards(string summonerId)
    {
        return _ownership?.GetSummonerBoundCards(summonerId) ?? [];
    }

    /// <summary>Get all cards owned by a summoner (AccountWide + SummonerBound).</summary>
    public CardInstance[] GetOwnedCards(string summonerId)
    {
        return _ownership?.GetOwnedCards(summonerId) ?? [];
    }

    /// <summary>Get collection grouped by catalog ID.</summary>
    public Dictionary<string, CardInstance[]> GetCollectionGrouped()
    {
        return _ownership?.GetCollectionGrouped() ?? [];
    }

    /// <summary>Get collection summary for UI display.</summary>
    public CollectionSummaryEntry[] GetCollectionSummary()
    {
        return _ownership?.GetCollectionSummary() ?? [];
    }

    // =========================================================================
    // OWNERSHIP - OPERATIONS
    // =========================================================================

    /// <summary>Grant cards to the player's collection.</summary>
    public string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards)
    {
        var instanceIds = _ownership?.GrantCards(cards) ?? [];

        if (instanceIds.Length > 0)
        {
            var gdArray = new Godot.Collections.Array<string>();
            foreach (var id in instanceIds)
                gdArray.Add(id);
            EmitSignal(SignalName.CardsGranted, gdArray);
        }

        return instanceIds;
    }

    /// <summary>Grant a single card.</summary>
    public string GrantCard(string catalogId, string rarity = "common")
    {
        return _ownership?.GrantCard(catalogId, rarity) ?? "";
    }

    /// <summary>Remove a card instance from the collection.</summary>
    public bool RemoveCard(string cardInstanceId)
    {
        var success = _ownership?.RemoveCard(cardInstanceId) ?? false;
        if (success)
        {
            EmitSignal(SignalName.CardRemoved, cardInstanceId);
        }
        return success;
    }

    /// <summary>Calculate dismantle value for a rarity.</summary>
    public static int GetDismantleValue(string rarity)
    {
        return CardOwnershipHandler.GetDismantleValue(rarity);
    }

    // =========================================================================
    // PROGRESSION - XP
    // =========================================================================

    /// <summary>Grant XP to a card.</summary>
    public int GrantXp(string cardInstanceId, int amount)
    {
        var newXp = _progression?.GrantXp(cardInstanceId, amount) ?? 0;
        if (newXp > 0)
        {
            var card = GetCard(cardInstanceId);
            EmitSignal(SignalName.CardXpChanged, cardInstanceId, newXp, card?.Level ?? 1);
        }
        return newXp;
    }

    /// <summary>Grant XP to multiple cards.</summary>
    public Dictionary<string, int> GrantXpToCards(IEnumerable<string> cardInstanceIds, int amount)
    {
        return _progression?.GrantXpToCards(cardInstanceIds, amount) ?? [];
    }

    /// <summary>Get XP required for a level (base).</summary>
    public int GetXpForLevel(int level)
    {
        return _progression?.GetXpForLevel(level) ?? 0;
    }

    /// <summary>Get XP required for a level with rarity scaling.</summary>
    public int GetXpForLevelWithRarity(int level, string rarity)
    {
        return _progression?.GetXpForLevelWithRarity(level, rarity) ?? 0;
    }

    /// <summary>Get rarity multiplier.</summary>
    public float GetRarityMultiplier(string rarity)
    {
        return _progression?.GetRarityMultiplier(rarity) ?? 1.0f;
    }

    /// <summary>Get XP needed for next level.</summary>
    public int GetXpToNextLevel(string cardInstanceId)
    {
        return _progression?.GetXpToNextLevel(cardInstanceId) ?? 0;
    }

    /// <summary>Get progress toward next level (0.0 - 1.0).</summary>
    public float GetLevelProgress(string cardInstanceId)
    {
        return _progression?.GetLevelProgress(cardInstanceId) ?? 0f;
    }

    // =========================================================================
    // PROGRESSION - LEVEL-UP
    // =========================================================================

    /// <summary>Check if card can level up (has enough XP).</summary>
    public bool CanLevelUp(string cardInstanceId)
    {
        return _progression?.CanLevelUp(cardInstanceId) ?? false;
    }

    /// <summary>Level up a card with chosen upgrade (XP-only, no gold cost).</summary>
    public bool LevelUpCard(string cardInstanceId, string upgradeId)
    {
        var success = _progression?.LevelUpCard(cardInstanceId, upgradeId) ?? false;
        if (success)
        {
            var card = GetCard(cardInstanceId);
            EmitSignal(SignalName.CardLeveledUp, cardInstanceId, card?.Level ?? 1);
            EmitSignal(SignalName.UpgradeApplied, cardInstanceId, upgradeId);
        }
        return success;
    }

    // =========================================================================
    // PROGRESSION - UPGRADES
    // =========================================================================

    /// <summary>Get available upgrades for card's next level.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableUpgrades(string cardInstanceId)
    {
        var upgrades = _progression?.GetAvailableUpgrades(cardInstanceId) ?? [];
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var upgrade in upgrades)
        {
            result.Add(new Godot.Collections.Dictionary
            {
                ["id"] = upgrade.Id,
                ["name"] = upgrade.Name,
                ["description"] = upgrade.Description
            });
        }
        return result;
    }

    /// <summary>Get all upgrades applied to a card.</summary>
    public Godot.Collections.Array<string> GetAppliedUpgrades(string cardInstanceId)
    {
        var upgrades = _progression?.GetAppliedUpgrades(cardInstanceId) ?? [];
        var result = new Godot.Collections.Array<string>();
        foreach (var u in upgrades)
            result.Add(u);
        return result;
    }

    /// <summary>Get stat modifiers from card's upgrades (for C# callers).</summary>
    public Dictionary<string, float> GetUpgradeStatModifiersTyped(string cardInstanceId)
    {
        return _progression?.GetUpgradeStatModifiers(cardInstanceId) ?? [];
    }

    /// <summary>Get stat modifiers from card's upgrades (for GDScript callers).</summary>
    public Godot.Collections.Dictionary GetUpgradeStatModifiers(string cardInstanceId)
    {
        var mods = _progression?.GetUpgradeStatModifiers(cardInstanceId) ?? [];
        var result = new Godot.Collections.Dictionary();
        foreach (var (stat, mult) in mods)
            result[stat] = mult;
        return result;
    }

    // =========================================================================
    // PROGRESSION - INFO
    // =========================================================================

    /// <summary>Get card progression info.</summary>
    public Godot.Collections.Dictionary GetCardProgressionInfoDict(string cardInstanceId)
    {
        var info = _progression?.GetCardProgressionInfo(cardInstanceId);
        if (info == null)
            return [];

        var upgradesArray = new Godot.Collections.Array<string>();
        foreach (var u in info.Upgrades)
            upgradesArray.Add(u);

        return new Godot.Collections.Dictionary
        {
            ["card_instance_id"] = info.CardInstanceId,
            ["catalog_id"] = info.CatalogId,
            ["rarity"] = info.Rarity,
            ["rarity_multiplier"] = info.RarityMultiplier,
            ["level"] = info.Level,
            ["max_level"] = info.MaxLevel,
            ["xp"] = info.Xp,
            ["xp_for_next_level"] = info.XpForNextLevel,
            ["xp_progress"] = info.XpProgress,
            ["can_level_up"] = info.CanLevelUp,
            ["upgrades"] = upgradesArray,
            ["is_max_level"] = info.IsMaxLevel
        };
    }

    /// <summary>Get all cards that can level up.</summary>
    public CardInstance[] GetCardsReadyToLevelUp()
    {
        return _progression?.GetCardsReadyToLevelUp() ?? [];
    }

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>List cards as array of dictionaries for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListCardsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in ListCards())
        {
            result.Add(DtoConverters.ToDict(card));
        }
        return result;
    }

    /// <summary>Get card as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetCardDict(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        return card != null ? DtoConverters.ToDict(card) : [];
    }

    /// <summary>Get owned cards for summoner as array for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetOwnedCardsDict(string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetOwnedCards(summonerId))
        {
            result.Add(DtoConverters.ToDict(card));
        }
        return result;
    }

    /// <summary>Grant cards from GDScript array.</summary>
    public Godot.Collections.Array<string> GrantCardsFromArray(Godot.Collections.Array<Godot.Collections.Dictionary> cardsArray)
    {
        var cards = new List<(string catalogId, string rarity)>();
        foreach (var dict in cardsArray)
        {
            if (dict.TryGetValue("catalog_id", out var catalogIdVar) &&
                dict.TryGetValue("rarity", out var rarityVar))
            {
                cards.Add((catalogIdVar.AsString(), rarityVar.AsString()));
            }
        }

        var instanceIds = GrantCards(cards);
        var result = new Godot.Collections.Array<string>();
        foreach (var id in instanceIds)
            result.Add(id);
        return result;
    }

    /// <summary>Get collection summary for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCollectionSummaryDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var entry in GetCollectionSummary())
        {
            var instancesArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var inst in entry.Instances)
                instancesArray.Add(DtoConverters.ToDict(inst));

            result.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = entry.CatalogId,
                ["count"] = entry.Count,
                ["rarity"] = entry.Rarity,
                ["instances"] = instancesArray
            });
        }
        return result;
    }
}
