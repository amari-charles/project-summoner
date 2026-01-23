using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Collection;

/// <summary>
/// Collection Service - Handles card collection operations.
///
/// Provides methods for granting, removing, and querying cards.
/// UI and gameplay code should call this, never the repository directly.
/// </summary>
[GlobalClass]
public partial class CollectionService : Node
{
    public static CollectionService? Instance { get; private set; }

    [Signal]
    public delegate void CollectionChangedEventHandler();

    [Signal]
    public delegate void CardsGrantedEventHandler(string[] instanceIds);

    [Signal]
    public delegate void CardRemovedEventHandler(string cardInstanceId);

    private IProfileRepository? _profileRepo;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        GD.Print("CollectionService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("CollectionService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        _profileRepo.DataChanged += OnRepoDataChanged;

        GD.Print("CollectionService: Ready");
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
    }

    // =========================================================================
    // CARD QUERIES
    // =========================================================================

    /// <summary>Get all card instances in the collection.</summary>
    public CardInstanceData[] ListCards()
    {
        if (_profileRepo == null) return [];
        return _profileRepo.ListCards();
    }

    /// <summary>Get a specific card instance by ID.</summary>
    public CardInstanceData? GetCard(string cardInstanceId)
    {
        if (_profileRepo == null) return null;
        return _profileRepo.GetCard(cardInstanceId);
    }

    /// <summary>Get count of cards by catalog ID.</summary>
    public int GetCardCount(string catalogId)
    {
        if (_profileRepo == null) return 0;
        return _profileRepo.GetCardCount(catalogId);
    }

    /// <summary>Check if player owns at least one of a card.</summary>
    public bool HasCard(string catalogId)
    {
        return GetCardCount(catalogId) > 0;
    }

    /// <summary>Get all instances of a specific catalog ID.</summary>
    public CardInstanceData[] GetCardsByCatalogId(string catalogId)
    {
        if (_profileRepo == null) return [];
        return _profileRepo.ListCards().Where(c => c.CatalogId == catalogId).ToArray();
    }

    /// <summary>
    /// Get collection grouped by catalog ID.
    /// Returns a dictionary mapping catalog_id to array of instances.
    /// </summary>
    public Dictionary<string, CardInstanceData[]> GetCollectionGrouped()
    {
        if (_profileRepo == null) return [];

        return _profileRepo.ListCards()
            .GroupBy(c => c.CatalogId)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    /// <summary>
    /// Get collection summary for UI display.
    /// Returns array of {CatalogId, Count, Rarity, Instances}.
    /// </summary>
    public CollectionSummaryEntry[] GetCollectionSummary()
    {
        var grouped = GetCollectionGrouped();
        var result = new List<CollectionSummaryEntry>();

        foreach (var (catalogId, instances) in grouped)
        {
            var rarity = instances.Length > 0 ? instances[0].Rarity : "common";
            result.Add(new CollectionSummaryEntry
            {
                CatalogId = catalogId,
                Count = instances.Length,
                Rarity = rarity,
                Instances = instances
            });
        }

        return [.. result];
    }

    // =========================================================================
    // CARD OPERATIONS
    // =========================================================================

    /// <summary>
    /// Grant cards to the player's collection.
    /// Returns array of created card instance IDs.
    /// </summary>
    public string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards)
    {
        if (_profileRepo == null) return [];

        // Validate all cards exist in catalog
        var validCards = new List<(string catalogId, string rarity)>();
        foreach (var (catalogId, rarity) in cards)
        {
            if (CardCatalog.HasCard(catalogId))
            {
                validCards.Add((catalogId, rarity));
            }
            else
            {
                GD.PushWarning($"CollectionService: Cannot grant card '{catalogId}' - not found in CardCatalog");
            }
        }

        if (validCards.Count == 0)
        {
            GD.PushWarning("CollectionService: No valid cards to grant");
            return [];
        }

        var instanceIds = _profileRepo.GrantCards(validCards);

        GD.Print($"CollectionService: Granted {instanceIds.Length} cards (requested: {cards.Count()}, valid: {validCards.Count})");
        EmitSignal(SignalName.CardsGranted, instanceIds);
        EmitSignal(SignalName.CollectionChanged);

        return instanceIds;
    }

    /// <summary>
    /// Grant a single card (convenience method).
    /// Returns the card instance ID.
    /// </summary>
    public string GrantCard(string catalogId, string rarity = "common")
    {
        var instanceIds = GrantCards([(catalogId, rarity)]);
        return instanceIds.Length > 0 ? instanceIds[0] : "";
    }

    /// <summary>
    /// Remove a card instance from the collection.
    /// Note: Does NOT perform cascade delete from decks - caller is responsible.
    /// Returns true if successful.
    /// </summary>
    public bool RemoveCard(string cardInstanceId)
    {
        if (_profileRepo == null) return false;

        var success = _profileRepo.RemoveCard(cardInstanceId);

        if (success)
        {
            GD.Print($"CollectionService: Removed card instance: {cardInstanceId}");
            EmitSignal(SignalName.CardRemoved, cardInstanceId);
            EmitSignal(SignalName.CollectionChanged);
        }
        else
        {
            GD.PushWarning($"CollectionService: Failed to remove card instance: {cardInstanceId}");
        }

        return success;
    }

    /// <summary>
    /// Calculate dismantle value for a rarity.
    /// </summary>
    public static int GetDismantleValue(string rarity)
    {
        return rarity switch
        {
            "common" => 5,
            "rare" => 20,
            "epic" => 100,
            "legendary" => 500,
            _ => 5
        };
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
            result.Add(CardInstanceToDict(card));
        }
        return result;
    }

    /// <summary>Get card as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetCardDict(string cardInstanceId)
    {
        var card = GetCard(cardInstanceId);
        return card != null ? CardInstanceToDict(card) : [];
    }

    /// <summary>Get cards by catalog ID as array for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByCatalogIdDict(string catalogId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByCatalogId(catalogId))
        {
            result.Add(CardInstanceToDict(card));
        }
        return result;
    }

    /// <summary>Get collection grouped as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetCollectionGroupedDict()
    {
        var grouped = GetCollectionGrouped();
        var result = new Godot.Collections.Dictionary();

        foreach (var (catalogId, instances) in grouped)
        {
            var instanceArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var instance in instances)
            {
                instanceArray.Add(CardInstanceToDict(instance));
            }
            result[catalogId] = instanceArray;
        }

        return result;
    }

    /// <summary>Get collection summary as array for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCollectionSummaryDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var entry in GetCollectionSummary())
        {
            var instanceArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var instance in entry.Instances)
            {
                instanceArray.Add(CardInstanceToDict(instance));
            }

            result.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = entry.CatalogId,
                ["count"] = entry.Count,
                ["rarity"] = entry.Rarity,
                ["instances"] = instanceArray
            });
        }

        return result;
    }

    /// <summary>Grant cards from GDScript (array of dictionaries).</summary>
    public Godot.Collections.Array<string> GrantCardsDict(Godot.Collections.Array<Godot.Collections.Dictionary> cards)
    {
        var cardList = new List<(string catalogId, string rarity)>();
        foreach (var card in cards)
        {
            var catalogIdVariant = card.GetValueOrDefault("catalog_id", "");
            var rarityVariant = card.GetValueOrDefault("rarity", "common");
            var catalogId = catalogIdVariant.AsString();
            var rarity = rarityVariant.AsString();
            if (!string.IsNullOrEmpty(catalogId))
            {
                cardList.Add((catalogId, rarity));
            }
        }

        var instanceIds = GrantCards(cardList);
        var result = new Godot.Collections.Array<string>();
        foreach (var id in instanceIds)
        {
            result.Add(id);
        }
        return result;
    }

    // =========================================================================
    // INTERNAL
    // =========================================================================

    private void OnRepoDataChanged()
    {
        EmitSignal(SignalName.CollectionChanged);
    }

    private static Godot.Collections.Dictionary CardInstanceToDict(CardInstanceData card)
    {
        var upgradesArray = new Godot.Collections.Array<string>();
        foreach (var upgrade in card.Upgrades)
        {
            upgradesArray.Add(upgrade);
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = card.Id,
            ["catalog_id"] = card.CatalogId,
            ["rarity"] = card.Rarity,
            ["level"] = card.Level,
            ["xp"] = card.Xp,
            ["upgrades"] = upgradesArray
        };
    }
}

/// <summary>
/// Collection summary entry for UI display.
/// </summary>
public class CollectionSummaryEntry
{
    public required string CatalogId { get; init; }
    public int Count { get; init; }
    public string Rarity { get; init; } = "common";
    public CardInstanceData[] Instances { get; init; } = [];
}
