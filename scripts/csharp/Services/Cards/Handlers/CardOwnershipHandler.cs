using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Cards.Handlers;

/// <summary>
/// Handles card ownership operations: granting, removing, and querying cards.
/// </summary>
public class CardOwnershipHandler
{
    private readonly IProfileRepository _profileRepo;

    public CardOwnershipHandler(IProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    // =========================================================================
    // CARD QUERIES - OWNERSHIP
    // =========================================================================

    /// <summary>Get all AccountWide cards (accessible by any summoner).</summary>
    public CardInstance[] GetAccountWideCards()
    {
        return _profileRepo.ListCards()
            .Where(c => c.Binding == ContentBinding.AccountWide)
            .ToArray();
    }

    /// <summary>Get SummonerBound cards for a specific summoner.</summary>
    public CardInstance[] GetSummonerBoundCards(string summonerId)
    {
        var typedSummonerId = new SummonerId(summonerId);
        return _profileRepo.ListCards()
            .Where(c => c.Binding == ContentBinding.SummonerBound && c.BoundToSummonerId == typedSummonerId)
            .ToArray();
    }

    /// <summary>
    /// Get all cards owned by a summoner based on binding rules.
    /// Returns AccountWide cards + SummonerBound cards bound to this summoner.
    /// </summary>
    public CardInstance[] GetOwnedCards(string summonerId)
    {
        return GetAccountWideCards()
            .Concat(GetSummonerBoundCards(summonerId))
            .ToArray();
    }

    // =========================================================================
    // CARD QUERIES - GENERAL
    // =========================================================================

    /// <summary>Get all card instances in the collection.</summary>
    public CardInstance[] ListCards()
    {
        return _profileRepo.ListCards();
    }

    /// <summary>Get a specific card instance by ID.</summary>
    public CardInstance? GetCard(string cardInstanceId)
    {
        return _profileRepo.GetCard(cardInstanceId);
    }

    /// <summary>Get count of cards by catalog ID.</summary>
    public int GetCardCount(string catalogId)
    {
        return _profileRepo.GetCardCount(catalogId);
    }

    /// <summary>Check if player owns at least one of a card.</summary>
    public bool HasCard(string catalogId)
    {
        return GetCardCount(catalogId) > 0;
    }

    /// <summary>Get all instances of a specific catalog ID.</summary>
    public CardInstance[] GetCardsByCatalogId(string catalogId)
    {
        var typedCatalogId = new CardId(catalogId);
        return _profileRepo.ListCards().Where(c => c.CatalogId == typedCatalogId).ToArray();
    }

    /// <summary>
    /// Get collection grouped by catalog ID.
    /// Returns a dictionary mapping catalog_id to array of instances.
    /// </summary>
    public Dictionary<string, CardInstance[]> GetCollectionGrouped()
    {
        return _profileRepo.ListCards()
            .GroupBy(c => c.CatalogId)
            .ToDictionary(g => (string)g.Key, g => g.ToArray());
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
                GD.PushWarning($"CardOwnershipHandler: Cannot grant card '{catalogId}' - not found in CardCatalog");
            }
        }

        if (validCards.Count == 0)
        {
            GD.PushWarning("CardOwnershipHandler: No valid cards to grant");
            return [];
        }

        var instanceIds = _profileRepo.GrantCards(validCards);

        GD.Print($"CardOwnershipHandler: Granted {instanceIds.Length} cards (requested: {cards.Count()}, valid: {validCards.Count})");

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
    /// Returns true if successful.
    /// </summary>
    public bool RemoveCard(string cardInstanceId)
    {
        var success = _profileRepo.RemoveCard(cardInstanceId);

        if (success)
        {
            GD.Print($"CardOwnershipHandler: Removed card instance: {cardInstanceId}");
        }
        else
        {
            GD.PushWarning($"CardOwnershipHandler: Failed to remove card instance: {cardInstanceId}");
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
}

/// <summary>
/// Collection summary entry for UI display.
/// </summary>
public class CollectionSummaryEntry
{
    public required string CatalogId { get; init; }
    public int Count { get; init; }
    public string Rarity { get; init; } = "common";
    public CardInstance[] Instances { get; init; } = [];
}
