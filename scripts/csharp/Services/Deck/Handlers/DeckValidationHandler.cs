using System;
using System.Collections.Generic;
using System.Linq;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Deck.Handlers;

/// <summary>
/// Handles deck validation: validate deck, get validation errors.
/// </summary>
public class DeckValidationHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly DeckCrudHandler _crud;
    private readonly int _minDeckSize;
    private readonly int _maxDeckSize;

    public DeckValidationHandler(IProfileRepository profileRepo, DeckCrudHandler crud, int minDeckSize, int maxDeckSize)
    {
        _profileRepo = profileRepo;
        _crud = crud;
        _minDeckSize = minDeckSize;
        _maxDeckSize = maxDeckSize;
    }

    // =========================================================================
    // VALIDATION
    // =========================================================================

    /// <summary>
    /// Validate a deck.
    /// Returns true if deck is valid and playable.
    /// </summary>
    public bool ValidateDeck(string deckId, Func<string, string, bool>? cardOwnershipChecker = null, Action<string, string>? onValidationFailed = null)
    {
        var deck = _crud.GetDeck(deckId);
        if (deck == null)
        {
            onValidationFailed?.Invoke(deckId, "Deck not found");
            return false;
        }

        // Check summoner is set and unlocked
        if (string.IsNullOrEmpty(deck.SummonerId))
        {
            onValidationFailed?.Invoke(deckId, "Deck has no summoner assigned");
            return false;
        }

        if (!_profileRepo.IsSummonerUnlocked(deck.SummonerId))
        {
            onValidationFailed?.Invoke(deckId, $"Summoner not unlocked: {deck.SummonerId}");
            return false;
        }

        // Check minimum size
        if (deck.CardInstanceIds.Count < _minDeckSize)
        {
            onValidationFailed?.Invoke(deckId, $"Deck has {deck.CardInstanceIds.Count} cards, minimum is {_minDeckSize}");
            return false;
        }

        // Check maximum size
        if (deck.CardInstanceIds.Count > _maxDeckSize)
        {
            onValidationFailed?.Invoke(deckId, $"Deck has {deck.CardInstanceIds.Count} cards, maximum is {_maxDeckSize}");
            return false;
        }

        // Validate all cards are owned by the deck's summoner (if checker is available)
        if (cardOwnershipChecker != null)
        {
            foreach (var cardId in deck.CardInstanceIds)
            {
                if (!cardOwnershipChecker(cardId, deck.SummonerId))
                {
                    onValidationFailed?.Invoke(deckId, $"Card instance not owned by summoner '{deck.SummonerId}': {cardId}");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Get validation errors for a deck (for UI display).
    /// </summary>
    public string[] GetValidationErrors(string deckId, Func<string, string, bool>? cardOwnershipChecker = null)
    {
        var errors = new List<string>();

        var deck = _crud.GetDeck(deckId);
        if (deck == null)
        {
            return ["Deck not found"];
        }

        // Check summoner
        if (string.IsNullOrEmpty(deck.SummonerId))
        {
            errors.Add("No summoner assigned");
        }
        else if (!_profileRepo.IsSummonerUnlocked(deck.SummonerId))
        {
            errors.Add($"Summoner not unlocked: {deck.SummonerId}");
        }

        // Check size constraints
        if (deck.CardInstanceIds.Count < _minDeckSize)
        {
            errors.Add($"Deck needs {_minDeckSize - deck.CardInstanceIds.Count} more cards (minimum: {_minDeckSize})");
        }

        if (deck.CardInstanceIds.Count > _maxDeckSize)
        {
            errors.Add($"Deck has {deck.CardInstanceIds.Count - _maxDeckSize} too many cards (maximum: {_maxDeckSize})");
        }

        // Check cards not owned by summoner (if checker is available)
        if (cardOwnershipChecker != null)
        {
            var notOwnedCount = deck.CardInstanceIds.Count(id => !cardOwnershipChecker(id, deck.SummonerId));
            if (notOwnedCount > 0)
            {
                errors.Add($"{notOwnedCount} cards not owned by summoner");
            }
        }

        return [.. errors];
    }
}
