using System.Linq;
using Godot;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Account;
using Fateforged.Infrastructure.Persistence;
using DeckModel = Fateforged.Domain.Profile.Decks.Deck;

namespace Fateforged.Meta.Deck.Handlers;

/// <summary>
/// Handles deck CRUD operations: create, read, update, delete.
/// </summary>
public class DeckCrudHandler
{
    private readonly IProfileRepository _profileRepo;

    public DeckCrudHandler(IProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    // =========================================================================
    // QUERIES
    // =========================================================================

    /// <summary>Get all decks for the current profile.</summary>
    public DeckModel[] ListDecks()
    {
        return _profileRepo.ListDecks();
    }

    /// <summary>Get a specific deck by ID.</summary>
    public DeckModel? GetDeck(string deckId)
    {
        return _profileRepo.GetDeck(new DeckId(deckId));
    }

    /// <summary>Check if a deck exists.</summary>
    public bool HasDeck(string deckId)
    {
        return GetDeck(deckId) != null;
    }

    /// <summary>Get deck count.</summary>
    public int GetDeckCount()
    {
        return ListDecks().Length;
    }

    /// <summary>Get all decks for a specific summoner.</summary>
    public DeckModel[] ListDecksForSummoner(string summonerId)
    {
        return ListDecks().Where(d => d.SummonerId == summonerId).ToArray();
    }

    // =========================================================================
    // ACTIVE DECK
    // =========================================================================

    /// <summary>Get the active deck ID (the deck used for battles).</summary>
    public string GetActiveDeckId()
    {
        var profile = _profileRepo.GetProfileMetadata();
        return profile?.Meta.SelectedDeck ?? "";
    }

    /// <summary>Set the active deck. Returns true if successful.</summary>
    public bool SetActiveDeck(string deckId)
    {
        if (!string.IsNullOrEmpty(deckId) && !HasDeck(deckId))
            return false;

        // Update selected_deck in profile meta
        _profileRepo.UpdateProfileMeta(new MetaUpdate { SelectedDeck = deckId });
        return true;
    }

    // =========================================================================
    // CREATE / UPDATE / DELETE
    // =========================================================================

    /// <summary>
    /// Create a new deck.
    /// Returns the deck ID, or empty string if failed.
    /// </summary>
    public string CreateDeck(string deckName, string[] cardInstanceIds, string summonerId = "")
    {
        // Determine summoner_id
        SummonerId finalSummonerId;
        if (string.IsNullOrEmpty(summonerId))
        {
            // Default to first unlocked summoner
            var unlocked = _profileRepo.GetUnlockedSummoners();
            if (unlocked.Length == 0)
            {
                GD.PushError("DeckCrudHandler: Cannot create deck - no summoners unlocked");
                return "";
            }
            finalSummonerId = unlocked[0];
        }
        else
        {
            // Validate summoner is unlocked
            var typedSummonerId = new SummonerId(summonerId);
            if (!_profileRepo.IsSummonerUnlocked(typedSummonerId))
            {
                GD.PushError($"DeckCrudHandler: Cannot create deck - summoner not unlocked: {summonerId}");
                return "";
            }
            finalSummonerId = typedSummonerId;
        }

        var deck = new DeckModel
        {
            Id = DeckId.None, // Will be assigned by repository
            Name = deckName,
            CardInstanceIds = cardInstanceIds.Select(s => new CardInstanceId(s)).ToList(),
            SummonerId = finalSummonerId
        };

        var deckId = _profileRepo.UpsertDeck(deck);

        GD.Print($"DeckCrudHandler: Created deck '{deckName}' with summoner '{finalSummonerId}' (id: {deckId})");
        return deckId;
    }

    /// <summary>
    /// Update an existing deck.
    /// Pass empty/null values to keep existing values.
    /// Returns true if successful.
    /// </summary>
    public bool UpdateDeck(string deckId, string? deckName = null, string[]? cardInstanceIds = null, string? summonerId = null)
    {
        var existing = GetDeck(deckId);
        if (existing == null)
        {
            GD.PushWarning($"DeckCrudHandler: Deck not found: {deckId}");
            return false;
        }

        var updated = new DeckModel
        {
            Id = new DeckId(deckId),
            Name = !string.IsNullOrEmpty(deckName) ? deckName : existing.Name,
            CardInstanceIds = cardInstanceIds != null && cardInstanceIds.Length > 0
                ? cardInstanceIds.Select(s => new CardInstanceId(s)).ToList()
                : existing.CardInstanceIds,
            SummonerId = !string.IsNullOrEmpty(summonerId) ? new SummonerId(summonerId) : existing.SummonerId
        };

        var resultId = _profileRepo.UpsertDeck(updated);

        if (!string.IsNullOrEmpty(resultId))
        {
            GD.Print($"DeckCrudHandler: Updated deck '{deckId}'");
            return true;
        }

        GD.PushError($"DeckCrudHandler: Failed to update deck '{deckId}'");
        return false;
    }

    /// <summary>Delete a deck. Returns true if successful.</summary>
    public bool DeleteDeck(string deckId)
    {
        var success = _profileRepo.DeleteDeck(new DeckId(deckId));

        if (success)
        {
            GD.Print($"DeckCrudHandler: Deleted deck '{deckId}'");
        }
        else
        {
            GD.PushWarning($"DeckCrudHandler: Failed to delete deck '{deckId}'");
        }

        return success;
    }

    /// <summary>Get the summoner ID for a deck.</summary>
    public string GetDeckSummoner(string deckId)
    {
        var deck = GetDeck(deckId);
        return deck?.SummonerId ?? "";
    }

    /// <summary>
    /// Set the summoner for a deck.
    /// Returns true if successful.
    /// </summary>
    public bool SetDeckSummoner(string deckId, string summonerId, System.Func<string, bool>? summonerValidator = null)
    {
        var deck = GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckCrudHandler: Deck not found: {deckId}");
            return false;
        }

        // Validate summoner is valid (if validator is available)
        if (summonerValidator != null && !summonerValidator(summonerId))
        {
            GD.PushError($"DeckCrudHandler: Invalid summoner_id: {summonerId}");
            return false;
        }

        // Validate summoner is unlocked
        if (!_profileRepo.IsSummonerUnlocked(new SummonerId(summonerId)))
        {
            GD.PushError($"DeckCrudHandler: Summoner not unlocked for this profile: {summonerId}");
            return false;
        }

        return UpdateDeck(deckId, summonerId: summonerId);
    }
}
