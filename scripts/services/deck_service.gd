extends Node
# DeckService is registered as autoload "Decks", no class_name needed

## Deck Service - Deck Management (GDScript wrapper for C# DeckService)
##
## Handles all deck operations (creating, updating, deleting, validating).
## UI and gameplay code should call this, never the repository directly.
## Delegates to C# DeckServiceCS for core operations.
##
## Usage:
##   var deck_id = Decks.create_deck("My Deck", [instance_id1, instance_id2, ...])
##   var is_valid = Decks.validate_deck(deck_id)
##   var deck = Decks.get_deck(deck_id)
##   Decks.delete_deck(deck_id)
##
## Emits signals for reactive UI updates.

## Constants
const MIN_DECK_SIZE: int = 1  # Minimum 1 card required
const MAX_DECK_SIZE: int = 30  # Maximum cards allowed in a deck

## Signals
signal deck_changed(deck_id: String)
signal deck_created(deck_id: String)
signal deck_deleted(deck_id: String)
signal validation_failed(deck_id: String, reason: String)

## C# service reference
var _cs_service: Node

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DeckService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/DeckServiceCS")
	if _cs_service == null:
		push_error("DeckService: DeckServiceCS autoload not found")
		return

	# Connect to C# service signals
	_cs_service.DeckChanged.connect(_on_cs_deck_changed)
	_cs_service.DeckCreated.connect(_on_cs_deck_created)
	_cs_service.DeckDeleted.connect(_on_cs_deck_deleted)
	_cs_service.ValidationFailed.connect(_on_cs_validation_failed)

	# Inject card ownership checker (uses GDScript Collection service)
	_cs_service.SetCardOwnershipChecker(_card_owned_by_summoner)

	# Inject summoner validator (uses GDScript SummonerCatalog)
	_cs_service.SetSummonerValidator(_is_valid_summoner)

	print("DeckService: Ready")

## Card ownership checker for C# service - checks if card is owned by summoner
func _card_owned_by_summoner(card_instance_id: String, summoner_id: String) -> bool:
	var owned_cards: Array[Dictionary] = Collection.get_owned_cards(summoner_id)
	for card: Dictionary in owned_cards:
		if card.get("id", "") == card_instance_id:
			return true
	return false

## Summoner validator for C# service
func _is_valid_summoner(summoner_id: String) -> bool:
	return SummonerCatalog.is_valid_summoner(summoner_id)

## =============================================================================
## DECK QUERIES (delegated to C#)
## =============================================================================

## Get all decks for the current profile
func list_decks() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.ListDecksDict()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## Get a specific deck by ID
func get_deck(deck_id: String) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetDeckDict(deck_id)

## Check if a deck exists
func has_deck(deck_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.HasDeck(deck_id)

## Get deck count
func get_deck_count() -> int:
	if _cs_service == null:
		return 0
	return _cs_service.GetDeckCount()

## Get the active deck ID (the deck used for battles)
func get_active_deck_id() -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GetActiveDeckId()

## Set the active deck (the deck used for battles)
## Returns true if successful
func set_active_deck(deck_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.SetActiveDeck(deck_id)

## Get all decks for a specific summoner
func list_decks_for_summoner(summoner_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.ListDecksForSummonerDict(summoner_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## DECK OPERATIONS (delegated to C#)
## =============================================================================

## Create a new deck
## Returns: deck_id
func create_deck(deck_name: String, card_instance_ids: Array = [], summoner_id: String = "") -> String:
	if _cs_service == null:
		return ""
	var typed_ids: Array[String] = []
	for item: Variant in card_instance_ids:
		if item is String:
			typed_ids.append(item)
	return _cs_service.CreateDeckFromDict(deck_name, typed_ids, summoner_id)

## Update an existing deck
## Returns true if successful
func update_deck(deck_id: String, deck_name: String = "", card_instance_ids: Array = [], summoner_id: String = "") -> bool:
	if _cs_service == null:
		return false
	var typed_ids: Array[String] = []
	for item: Variant in card_instance_ids:
		if item is String:
			typed_ids.append(item)
	return _cs_service.UpdateDeckFromDict(deck_id, deck_name, typed_ids, summoner_id)

## Delete a deck
## Returns true if successful
func delete_deck(deck_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.DeleteDeck(deck_id)

## Add a card to a deck
## Returns true if successful
func add_card_to_deck(deck_id: String, card_instance_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.AddCardToDeck(deck_id, card_instance_id)

## Remove a card from a deck
## Returns true if successful
func remove_card_from_deck(deck_id: String, card_instance_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.RemoveCardFromDeck(deck_id, card_instance_id)

## Set the summoner for a deck
## Returns true if successful
func set_deck_summoner(deck_id: String, summoner_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.SetDeckSummoner(deck_id, summoner_id)

## Get the summoner ID for a deck
## Returns empty string if deck not found or summoner not set
func get_deck_summoner(deck_id: String) -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GetDeckSummoner(deck_id)

## =============================================================================
## DECK VALIDATION (delegated to C#)
## =============================================================================

## Validate a deck
## Returns true if deck is valid and playable
func validate_deck(deck_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.ValidateDeck(deck_id)

## Get validation errors for a deck (for UI display)
## Returns: Array of error strings
func get_validation_errors(deck_id: String) -> Array[String]:
	if _cs_service == null:
		return ["Service unavailable"]
	var result: Array = _cs_service.GetValidationErrorsArray(deck_id)
	var typed_result: Array[String] = []
	typed_result.assign(result)
	return typed_result

## Clean a deck by removing missing cards
## Returns: number of cards removed
func clean_deck(deck_id: String) -> int:
	if _cs_service == null:
		return 0
	return _cs_service.CleanDeck(deck_id)

## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_deck_changed(deck_id: String) -> void:
	deck_changed.emit(deck_id)

func _on_cs_deck_created(deck_id: String) -> void:
	deck_created.emit(deck_id)

func _on_cs_deck_deleted(deck_id: String) -> void:
	deck_deleted.emit(deck_id)

func _on_cs_validation_failed(deck_id: String, reason: String) -> void:
	validation_failed.emit(deck_id, reason)
