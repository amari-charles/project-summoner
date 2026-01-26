using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Data.Serialization;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Deck;

/// <summary>
/// Deck Service - Handles deck management operations.
///
/// Provides methods for creating, updating, deleting, and validating decks.
/// UI and gameplay code should call this, never the repository directly.
/// </summary>
[GlobalClass]
public partial class DeckService : Node
{
    public static DeckService? Instance { get; private set; }

    /// <summary>Minimum cards required in a deck.</summary>
    public const int MinDeckSize = 1;

    /// <summary>Maximum cards allowed in a deck.</summary>
    public const int MaxDeckSize = 30;

    [Signal]
    public delegate void DeckChangedEventHandler(string deckId);

    [Signal]
    public delegate void DeckCreatedEventHandler(string deckId);

    [Signal]
    public delegate void DeckDeletedEventHandler(string deckId);

    [Signal]
    public delegate void ValidationFailedEventHandler(string deckId, string reason);

    private IProfileRepository? _profileRepo;

    // Func delegate for checking if a card is owned by a summoner (injected from GDScript)
    private Func<string, string, bool>? _cardOwnershipChecker;

    // Func delegate for checking if a summoner is valid (injected from GDScript)
    private Func<string, bool>? _summonerValidator;

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
        GD.Print("DeckService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("DeckService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        _profileRepo.DataChanged += OnRepoDataChanged;

        GD.Print("DeckService: Ready");
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
    public void InitForTesting(IProfileRepository repo, Func<string, string, bool>? cardOwnershipChecker = null, Func<string, bool>? summonerValidator = null)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _profileRepo = repo;
        _cardOwnershipChecker = cardOwnershipChecker;
        _summonerValidator = summonerValidator;
    }

    /// <summary>Set the card ownership checker (called from GDScript wrapper).</summary>
    public void SetCardOwnershipChecker(Callable checker)
    {
        _cardOwnershipChecker = (cardInstanceId, summonerId) =>
        {
            var result = checker.Call(cardInstanceId, summonerId);
            return result.AsBool();
        };
    }

    /// <summary>Set the summoner validator (called from GDScript wrapper).</summary>
    public void SetSummonerValidator(Callable validator)
    {
        _summonerValidator = (summonerId) =>
        {
            var result = validator.Call(summonerId);
            return result.AsBool();
        };
    }

    // =========================================================================
    // DECK QUERIES
    // =========================================================================

    /// <summary>Get all decks for the current profile.</summary>
    public DeckData[] ListDecks()
    {
        if (_profileRepo == null) return [];
        return _profileRepo.ListDecks();
    }

    /// <summary>Get a specific deck by ID.</summary>
    public DeckData? GetDeck(string deckId)
    {
        if (_profileRepo == null) return null;
        return _profileRepo.GetDeck(deckId);
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

    /// <summary>Get the active deck ID (the deck used for battles).</summary>
    public string GetActiveDeckId()
    {
        if (_profileRepo == null) return "";
        var profile = _profileRepo.GetProfileSnapshot();
        return profile?.Meta.SelectedDeck ?? "";
    }

    /// <summary>Set the active deck. Returns true if successful.</summary>
    public bool SetActiveDeck(string deckId)
    {
        if (_profileRepo == null) return false;

        if (!string.IsNullOrEmpty(deckId) && !HasDeck(deckId))
            return false;

        var profile = _profileRepo.GetProfileSnapshot();
        if (profile == null) return false;

        profile.Meta.SelectedDeck = deckId;
        _profileRepo.SaveProfile(immediate: false);
        return true;
    }

    /// <summary>Get all decks for a specific summoner.</summary>
    public DeckData[] ListDecksForSummoner(string summonerId)
    {
        return ListDecks().Where(d => d.SummonerId == summonerId).ToArray();
    }

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    /// <summary>
    /// Create a new deck.
    /// Returns the deck ID, or empty string if failed.
    /// </summary>
    public string CreateDeck(string deckName, string[] cardInstanceIds, string summonerId = "")
    {
        if (_profileRepo == null) return "";

        // Determine summoner_id
        var finalSummonerId = summonerId;
        if (string.IsNullOrEmpty(finalSummonerId))
        {
            // Default to first unlocked summoner
            var unlocked = _profileRepo.GetUnlockedSummoners();
            if (unlocked.Length == 0)
            {
                GD.PushError("DeckService: Cannot create deck - no summoners unlocked");
                return "";
            }
            finalSummonerId = unlocked[0];
        }
        else
        {
            // Validate summoner is unlocked
            if (!_profileRepo.IsSummonerUnlocked(finalSummonerId))
            {
                GD.PushError($"DeckService: Cannot create deck - summoner not unlocked: {finalSummonerId}");
                return "";
            }
        }

        var deck = new DeckData
        {
            Id = "", // Will be assigned by repository
            Name = deckName,
            CardInstanceIds = [.. cardInstanceIds],
            SummonerId = finalSummonerId
        };

        var deckId = _profileRepo.UpsertDeck(deck);

        GD.Print($"DeckService: Created deck '{deckName}' with summoner '{finalSummonerId}' (id: {deckId})");
        EmitSignal(SignalName.DeckCreated, deckId);
        EmitSignal(SignalName.DeckChanged, deckId);

        return deckId;
    }

    /// <summary>
    /// Update an existing deck.
    /// Pass empty/null values to keep existing values.
    /// Returns true if successful.
    /// </summary>
    public bool UpdateDeck(string deckId, string? deckName = null, string[]? cardInstanceIds = null, string? summonerId = null)
    {
        if (_profileRepo == null) return false;

        var existing = GetDeck(deckId);
        if (existing == null)
        {
            GD.PushWarning($"DeckService: Deck not found: {deckId}");
            return false;
        }

        var updated = new DeckData
        {
            Id = deckId,
            Name = !string.IsNullOrEmpty(deckName) ? deckName : existing.Name,
            CardInstanceIds = cardInstanceIds != null && cardInstanceIds.Length > 0
                ? [.. cardInstanceIds]
                : existing.CardInstanceIds,
            SummonerId = !string.IsNullOrEmpty(summonerId) ? summonerId : existing.SummonerId
        };

        var resultId = _profileRepo.UpsertDeck(updated);

        if (!string.IsNullOrEmpty(resultId))
        {
            GD.Print($"DeckService: Updated deck '{deckId}'");
            EmitSignal(SignalName.DeckChanged, deckId);
            return true;
        }

        GD.PushError($"DeckService: Failed to update deck '{deckId}'");
        return false;
    }

    /// <summary>Delete a deck. Returns true if successful.</summary>
    public bool DeleteDeck(string deckId)
    {
        if (_profileRepo == null) return false;

        var success = _profileRepo.DeleteDeck(deckId);

        if (success)
        {
            GD.Print($"DeckService: Deleted deck '{deckId}'");
            EmitSignal(SignalName.DeckDeleted, deckId);
        }
        else
        {
            GD.PushWarning($"DeckService: Failed to delete deck '{deckId}'");
        }

        return success;
    }

    /// <summary>
    /// Add a card to a deck.
    /// Returns true if successful.
    /// Note: Card existence validation must be done by GDScript wrapper.
    /// </summary>
    public bool AddCardToDeck(string deckId, string cardInstanceId)
    {
        var deck = GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckService: Deck not found: {deckId}");
            return false;
        }

        // Check max size
        if (deck.CardInstanceIds.Count >= MaxDeckSize)
        {
            GD.PushWarning($"DeckService: Deck is at maximum size ({MaxDeckSize})");
            return false;
        }

        // Check if already in deck
        if (deck.CardInstanceIds.Contains(cardInstanceId))
        {
            GD.PushWarning($"DeckService: Card instance already in deck: {cardInstanceId}");
            return false;
        }

        // Check if card is owned by the deck's summoner (if checker is available)
        if (_cardOwnershipChecker != null && !_cardOwnershipChecker(cardInstanceId, deck.SummonerId))
        {
            GD.PushWarning($"DeckService: Card instance not owned by summoner '{deck.SummonerId}': {cardInstanceId}");
            return false;
        }

        var newCards = deck.CardInstanceIds.ToList();
        newCards.Add(cardInstanceId);

        return UpdateDeck(deckId, cardInstanceIds: [.. newCards]);
    }

    /// <summary>
    /// Remove a card from a deck.
    /// Returns true if successful.
    /// </summary>
    public bool RemoveCardFromDeck(string deckId, string cardInstanceId)
    {
        var deck = GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckService: Deck not found: {deckId}");
            return false;
        }

        var index = deck.CardInstanceIds.IndexOf(cardInstanceId);
        if (index == -1)
        {
            GD.PushWarning($"DeckService: Card not found in deck: {cardInstanceId}");
            return false;
        }

        var newCards = deck.CardInstanceIds.ToList();
        newCards.RemoveAt(index);

        return UpdateDeck(deckId, cardInstanceIds: [.. newCards]);
    }

    /// <summary>
    /// Set the summoner for a deck.
    /// Returns true if successful.
    /// Note: Summoner catalog validation must be done by GDScript wrapper.
    /// </summary>
    public bool SetDeckSummoner(string deckId, string summonerId)
    {
        if (_profileRepo == null) return false;

        var deck = GetDeck(deckId);
        if (deck == null)
        {
            GD.PushWarning($"DeckService: Deck not found: {deckId}");
            return false;
        }

        // Validate summoner is valid (if validator is available)
        if (_summonerValidator != null && !_summonerValidator(summonerId))
        {
            GD.PushError($"DeckService: Invalid summoner_id: {summonerId}");
            return false;
        }

        // Validate summoner is unlocked
        if (!_profileRepo.IsSummonerUnlocked(summonerId))
        {
            GD.PushError($"DeckService: Summoner not unlocked for this profile: {summonerId}");
            return false;
        }

        return UpdateDeck(deckId, summonerId: summonerId);
    }

    /// <summary>Get the summoner ID for a deck.</summary>
    public string GetDeckSummoner(string deckId)
    {
        var deck = GetDeck(deckId);
        return deck?.SummonerId ?? "";
    }

    // =========================================================================
    // DECK VALIDATION
    // =========================================================================

    /// <summary>
    /// Validate a deck.
    /// Returns true if deck is valid and playable.
    /// </summary>
    public bool ValidateDeck(string deckId)
    {
        if (_profileRepo == null) return false;

        var deck = GetDeck(deckId);
        if (deck == null)
        {
            EmitValidationFailed(deckId, "Deck not found");
            return false;
        }

        // Check summoner is set and unlocked
        if (string.IsNullOrEmpty(deck.SummonerId))
        {
            EmitValidationFailed(deckId, "Deck has no summoner assigned");
            return false;
        }

        if (!_profileRepo.IsSummonerUnlocked(deck.SummonerId))
        {
            EmitValidationFailed(deckId, $"Summoner not unlocked: {deck.SummonerId}");
            return false;
        }

        // Check minimum size
        if (deck.CardInstanceIds.Count < MinDeckSize)
        {
            EmitValidationFailed(deckId, $"Deck has {deck.CardInstanceIds.Count} cards, minimum is {MinDeckSize}");
            return false;
        }

        // Check maximum size
        if (deck.CardInstanceIds.Count > MaxDeckSize)
        {
            EmitValidationFailed(deckId, $"Deck has {deck.CardInstanceIds.Count} cards, maximum is {MaxDeckSize}");
            return false;
        }

        // Validate all cards are owned by the deck's summoner (if checker is available)
        if (_cardOwnershipChecker != null)
        {
            foreach (var cardId in deck.CardInstanceIds)
            {
                if (!_cardOwnershipChecker(cardId, deck.SummonerId))
                {
                    EmitValidationFailed(deckId, $"Card instance not owned by summoner '{deck.SummonerId}': {cardId}");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Get validation errors for a deck (for UI display).
    /// </summary>
    public string[] GetValidationErrors(string deckId)
    {
        var errors = new List<string>();

        if (_profileRepo == null) return ["Repository not available"];

        var deck = GetDeck(deckId);
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
        if (deck.CardInstanceIds.Count < MinDeckSize)
        {
            errors.Add($"Deck needs {MinDeckSize - deck.CardInstanceIds.Count} more cards (minimum: {MinDeckSize})");
        }

        if (deck.CardInstanceIds.Count > MaxDeckSize)
        {
            errors.Add($"Deck has {deck.CardInstanceIds.Count - MaxDeckSize} too many cards (maximum: {MaxDeckSize})");
        }

        // Check cards not owned by summoner (if checker is available)
        if (_cardOwnershipChecker != null)
        {
            var notOwnedCount = deck.CardInstanceIds.Count(id => !_cardOwnershipChecker(id, deck.SummonerId));
            if (notOwnedCount > 0)
            {
                errors.Add($"{notOwnedCount} cards not owned by summoner");
            }
        }

        return [.. errors];
    }

    /// <summary>
    /// Clean a deck by removing cards not owned by the deck's summoner.
    /// Returns number of cards removed.
    /// </summary>
    public int CleanDeck(string deckId)
    {
        var deck = GetDeck(deckId);
        if (deck == null) return 0;

        if (_cardOwnershipChecker == null)
        {
            // Can't clean without ownership checker
            return 0;
        }

        var validCards = deck.CardInstanceIds.Where(id => _cardOwnershipChecker(id, deck.SummonerId)).ToList();
        var removedCount = deck.CardInstanceIds.Count - validCards.Count;

        if (removedCount > 0)
        {
            UpdateDeck(deckId, cardInstanceIds: [.. validCards]);
            GD.Print($"DeckService: Cleaned deck '{deckId}', removed {removedCount} cards not owned by summoner");
        }

        return removedCount;
    }

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>List decks as array of dictionaries for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListDecksDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var deck in ListDecks())
        {
            result.Add(DtoConverters.ToDict(deck));
        }
        return result;
    }

    /// <summary>Get deck as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetDeckDict(string deckId)
    {
        var deck = GetDeck(deckId);
        return deck != null ? DtoConverters.ToDict(deck) : [];
    }

    /// <summary>List decks for summoner as array for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListDecksForSummonerDict(string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var deck in ListDecksForSummoner(summonerId))
        {
            result.Add(DtoConverters.ToDict(deck));
        }
        return result;
    }

    /// <summary>Create deck from GDScript.</summary>
    public string CreateDeckFromDict(string deckName, Godot.Collections.Array<string> cardInstanceIds, string summonerId = "")
    {
        return CreateDeck(deckName, [.. cardInstanceIds], summonerId);
    }

    /// <summary>Update deck from GDScript.</summary>
    public bool UpdateDeckFromDict(string deckId, string deckName, Godot.Collections.Array<string> cardInstanceIds, string summonerId)
    {
        string[]? cards = cardInstanceIds.Count > 0 ? [.. cardInstanceIds] : null;
        string? name = !string.IsNullOrEmpty(deckName) ? deckName : null;
        string? summoner = !string.IsNullOrEmpty(summonerId) ? summonerId : null;
        return UpdateDeck(deckId, name, cards, summoner);
    }

    /// <summary>Get validation errors as array for GDScript.</summary>
    public Godot.Collections.Array<string> GetValidationErrorsArray(string deckId)
    {
        var result = new Godot.Collections.Array<string>();
        foreach (var error in GetValidationErrors(deckId))
        {
            result.Add(error);
        }
        return result;
    }

    // =========================================================================
    // INTERNAL
    // =========================================================================

    private void EmitValidationFailed(string deckId, string reason)
    {
        GD.PushWarning($"DeckService: Deck validation failed for '{deckId}': {reason}");
        EmitSignal(SignalName.ValidationFailed, deckId, reason);
    }

    private void OnRepoDataChanged()
    {
        // Could emit a generic signal here if needed
    }

}
