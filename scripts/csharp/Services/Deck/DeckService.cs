using System;
using System.Linq;
using Godot;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Deck.Handlers;
using DeckModel = Fateforged.Domain.Profile.Decks.Deck;

namespace Fateforged.Meta.Deck;

/// <summary>
/// Deck Service - Handles deck management operations.
///
/// Provides methods for creating, updating, deleting, and validating decks.
/// UI and gameplay code should call this, never the repository directly.
///
/// Uses Facade + Handlers pattern for clean separation of concerns.
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
    private DeckCrudHandler _crud = null!;
    private DeckCardHandler _cards = null!;
    private DeckValidationHandler _validation = null!;
    private bool _isInitialized;

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
        Initialize();
        AutoInitializeDependencies();
    }

    /// <summary>
    /// Auto-initialize dependency callbacks by looking up sibling autoloads.
    /// Only sets callbacks that haven't been injected (e.g., via InitForTesting).
    /// </summary>
    private void AutoInitializeDependencies()
    {
        if (_cardOwnershipChecker == null)
        {
            var cardService = GetNodeOrNull<CardService>("/root/CardServiceCS");
            if (cardService != null)
            {
                _cardOwnershipChecker = (cardInstanceId, summonerId) =>
                    cardService.GetOwnedCards(summonerId)
                        .Any(c => c.Id.Value == cardInstanceId);
            }
        }

        if (_summonerValidator == null)
        {
            var catalog = GetNodeOrNull<SummonerCatalogBridge>("/root/SummonerCatalog");
            if (catalog != null)
            {
                _summonerValidator = (summonerId) => catalog.HasSummoner(summonerId);
            }
        }
    }

    private void Initialize()
    {
        GD.Print("DeckService: Initializing...");

        _profileRepo = ProfileRepository.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("DeckService: ProfileRepository.Instance not available");
            return;
        }

        _crud = new DeckCrudHandler(_profileRepo);
        _cards = new DeckCardHandler(_crud, MaxDeckSize);
        _validation = new DeckValidationHandler(_profileRepo, _crud, MinDeckSize, MaxDeckSize);
        _isInitialized = true;

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

        _crud = new DeckCrudHandler(repo);
        _cards = new DeckCardHandler(_crud, MaxDeckSize);
        _validation = new DeckValidationHandler(repo, _crud, MinDeckSize, MaxDeckSize);
        _isInitialized = true;
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
    // DECK QUERIES (delegates to DeckCrudHandler)
    // =========================================================================

    /// <summary>Get all decks for the current profile.</summary>
    public DeckModel[] ListDecks()
    {
        return _isInitialized ? _crud.ListDecks() : [];
    }

    /// <summary>Get a specific deck by ID.</summary>
    public DeckModel? GetDeck(string deckId)
    {
        return _isInitialized ? _crud.GetDeck(deckId) : null;
    }

    /// <summary>Check if a deck exists.</summary>
    public bool HasDeck(string deckId)
    {
        return _isInitialized && _crud.HasDeck(deckId);
    }

    /// <summary>Get deck count.</summary>
    public int GetDeckCount()
    {
        return _isInitialized ? _crud.GetDeckCount() : 0;
    }

    /// <summary>Get the active deck ID (the deck used for battles).</summary>
    public string GetActiveDeckId()
    {
        return _isInitialized ? _crud.GetActiveDeckId() : "";
    }

    /// <summary>Set the active deck. Returns true if successful.</summary>
    public bool SetActiveDeck(string deckId)
    {
        return _isInitialized && _crud.SetActiveDeck(deckId);
    }

    /// <summary>Get all decks for a specific summoner.</summary>
    public DeckModel[] ListDecksForSummoner(string summonerId)
    {
        return _isInitialized ? _crud.ListDecksForSummoner(summonerId) : [];
    }

    // =========================================================================
    // DECK OPERATIONS (delegates to DeckCrudHandler)
    // =========================================================================

    /// <summary>
    /// Create a new deck.
    /// Returns the deck ID, or empty string if failed.
    /// </summary>
    public string CreateDeck(string deckName, string[] cardInstanceIds, string summonerId = "")
    {
        if (!_isInitialized) return "";

        var deckId = _crud.CreateDeck(deckName, cardInstanceIds, summonerId);

        if (!string.IsNullOrEmpty(deckId))
        {
            EmitSignal(SignalName.DeckCreated, deckId);
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return deckId;
    }

    /// <summary>
    /// Update an existing deck.
    /// Pass empty/null values to keep existing values.
    /// Returns true if successful.
    /// </summary>
    public bool UpdateDeck(string deckId, string? deckName = null, string[]? cardInstanceIds = null, string? summonerId = null)
    {
        if (!_isInitialized) return false;

        var success = _crud.UpdateDeck(deckId, deckName, cardInstanceIds, summonerId);

        if (success)
        {
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return success;
    }

    /// <summary>Delete a deck. Returns true if successful.</summary>
    public bool DeleteDeck(string deckId)
    {
        if (!_isInitialized) return false;

        var success = _crud.DeleteDeck(deckId);

        if (success)
        {
            EmitSignal(SignalName.DeckDeleted, deckId);
        }

        return success;
    }

    /// <summary>Get the summoner ID for a deck.</summary>
    public string GetDeckSummoner(string deckId)
    {
        return _isInitialized ? _crud.GetDeckSummoner(deckId) : "";
    }

    /// <summary>
    /// Set the summoner for a deck.
    /// Returns true if successful.
    /// </summary>
    public bool SetDeckSummoner(string deckId, string summonerId)
    {
        if (!_isInitialized) return false;

        var success = _crud.SetDeckSummoner(deckId, summonerId, _summonerValidator);

        if (success)
        {
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return success;
    }

    // =========================================================================
    // CARD OPERATIONS (delegates to DeckCardHandler)
    // =========================================================================

    /// <summary>
    /// Add a card to a deck.
    /// Returns true if successful.
    /// </summary>
    public bool AddCardToDeck(string deckId, string cardInstanceId)
    {
        if (!_isInitialized) return false;

        var success = _cards.AddCardToDeck(deckId, cardInstanceId, _cardOwnershipChecker);

        if (success)
        {
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return success;
    }

    /// <summary>
    /// Remove a card from a deck.
    /// Returns true if successful.
    /// </summary>
    public bool RemoveCardFromDeck(string deckId, string cardInstanceId)
    {
        if (!_isInitialized) return false;

        var success = _cards.RemoveCardFromDeck(deckId, cardInstanceId);

        if (success)
        {
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return success;
    }

    /// <summary>
    /// Clean a deck by removing cards not owned by the deck's summoner.
    /// Returns number of cards removed.
    /// </summary>
    public int CleanDeck(string deckId)
    {
        if (!_isInitialized) return 0;

        var removedCount = _cards.CleanDeck(deckId, _cardOwnershipChecker);

        if (removedCount > 0)
        {
            EmitSignal(SignalName.DeckChanged, deckId);
        }

        return removedCount;
    }

    // =========================================================================
    // DECK VALIDATION (delegates to DeckValidationHandler)
    // =========================================================================

    /// <summary>
    /// Validate a deck.
    /// Returns true if deck is valid and playable.
    /// </summary>
    public bool ValidateDeck(string deckId)
    {
        return _isInitialized && _validation.ValidateDeck(deckId, _cardOwnershipChecker, EmitValidationFailed);
    }

    /// <summary>
    /// Get validation errors for a deck (for UI display).
    /// </summary>
    public string[] GetValidationErrors(string deckId)
    {
        return _isInitialized ? _validation.GetValidationErrors(deckId, _cardOwnershipChecker) : ["Service not initialized"];
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
