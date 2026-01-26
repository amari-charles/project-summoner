using System;
using System.Linq;
using Godot;
using DeckModel = ProjectSummoner.Domain.Profile.Decks.Deck;

namespace ProjectSummoner.Services.Deck.Handlers;

/// <summary>
/// Handles deck card operations: add/remove cards, clean deck.
/// </summary>
public class DeckCardHandler
{
    private readonly DeckCrudHandler _crud;
    private readonly int _maxDeckSize;

    public DeckCardHandler(DeckCrudHandler crud, int maxDeckSize)
    {
        _crud = crud;
        _maxDeckSize = maxDeckSize;
    }

    // =========================================================================
    // CARD OPERATIONS
    // =========================================================================

    /// <summary>
    /// Add a card to a deck.
    /// Returns true if successful.
    /// </summary>
    public bool AddCardToDeck(string deckId, string cardInstanceId, Func<string, string, bool>? cardOwnershipChecker = null)
    {
        var deck = _crud.GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckCardHandler: Deck not found: {deckId}");
            return false;
        }

        // Check max size
        if (deck.CardInstanceIds.Count >= _maxDeckSize)
        {
            GD.PushWarning($"DeckCardHandler: Deck is at maximum size ({_maxDeckSize})");
            return false;
        }

        // Check if already in deck
        if (deck.CardInstanceIds.Contains(cardInstanceId))
        {
            GD.PushWarning($"DeckCardHandler: Card instance already in deck: {cardInstanceId}");
            return false;
        }

        // Check if card is owned by the deck's summoner (if checker is available)
        if (cardOwnershipChecker != null && !cardOwnershipChecker(cardInstanceId, deck.SummonerId))
        {
            GD.PushWarning($"DeckCardHandler: Card instance not owned by summoner '{deck.SummonerId}': {cardInstanceId}");
            return false;
        }

        var newCards = deck.CardInstanceIds.ToList();
        newCards.Add(cardInstanceId);

        return _crud.UpdateDeck(deckId, cardInstanceIds: [.. newCards]);
    }

    /// <summary>
    /// Remove a card from a deck.
    /// Returns true if successful.
    /// </summary>
    public bool RemoveCardFromDeck(string deckId, string cardInstanceId)
    {
        var deck = _crud.GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckCardHandler: Deck not found: {deckId}");
            return false;
        }

        var index = deck.CardInstanceIds.IndexOf(cardInstanceId);
        if (index == -1)
        {
            GD.PushWarning($"DeckCardHandler: Card not found in deck: {cardInstanceId}");
            return false;
        }

        var newCards = deck.CardInstanceIds.ToList();
        newCards.RemoveAt(index);

        return _crud.UpdateDeck(deckId, cardInstanceIds: [.. newCards]);
    }

    /// <summary>
    /// Clean a deck by removing cards not owned by the deck's summoner.
    /// Returns number of cards removed.
    /// </summary>
    public int CleanDeck(string deckId, Func<string, string, bool>? cardOwnershipChecker = null)
    {
        var deck = _crud.GetDeck(deckId);
        if (deck == null) return 0;

        if (cardOwnershipChecker == null)
        {
            // Can't clean without ownership checker
            return 0;
        }

        var validCards = deck.CardInstanceIds.Where(id => cardOwnershipChecker(id, deck.SummonerId)).ToList();
        var removedCount = deck.CardInstanceIds.Count - validCards.Count;

        if (removedCount > 0)
        {
            _crud.UpdateDeck(deckId, cardInstanceIds: [.. validCards]);
            GD.Print($"DeckCardHandler: Cleaned deck '{deckId}', removed {removedCount} cards not owned by summoner");
        }

        return removedCount;
    }
}
