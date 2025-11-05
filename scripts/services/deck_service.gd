extends Node
class_name DeckService

## Deck Service - Deck Management
##
## Handles all deck operations (creating, updating, deleting, validating).
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   var deck_id = Decks.create_deck("My Deck", [instance_id1, instance_id2, ...])
##   var is_valid = Decks.validate_deck(deck_id)
##   var deck = Decks.get_deck(deck_id)
##   Decks.delete_deck(deck_id)
##
## Emits signals for reactive UI updates.

const IProfileRepo = preload("res://scripts/data/profile_repository.gd")

## Constants
const MIN_DECK_SIZE = 30  # Minimum cards required in a deck
const MAX_DECK_SIZE = 30  # Maximum cards allowed in a deck

## Signals
signal deck_changed(deck_id: String)
signal deck_created(deck_id: String)
signal deck_deleted(deck_id: String)
signal validation_failed(deck_id: String, reason: String)

## Repository reference (injected by autoload order)
var _repo: IProfileRepo = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DeckService: Initializing...")

	# Wait for ProfileRepo to be ready
	await get_tree().process_frame

	_repo = get_node("/root/ProfileRepo")
	if _repo == null:
		push_error("DeckService: ProfileRepo not found! Ensure it's registered as autoload.")
		return

	# Connect to repo signals
	_repo.data_changed.connect(_on_repo_data_changed)

	print("DeckService: Ready")

## =============================================================================
## DECK QUERIES
## =============================================================================

## Get all decks for the current profile
func list_decks() -> Array:
	if _repo == null:
		return []
	return _repo.list_decks()

## Get a specific deck by ID
func get_deck(deck_id: String) -> Dictionary:
	if _repo == null:
		return {}
	return _repo.get_deck(deck_id)

## Check if a deck exists
func has_deck(deck_id: String) -> bool:
	return not get_deck(deck_id).is_empty()

## Get deck count
func get_deck_count() -> int:
	return list_decks().size()

## =============================================================================
## DECK OPERATIONS
## =============================================================================

## Create a new deck
## Returns: deck_id
func create_deck(deck_name: String, card_instance_ids: Array = []) -> String:
	if _repo == null:
		push_error("DeckService: Cannot create deck, repo not initialized")
		return ""

	var deck = {
		"name": deck_name,
		"card_instance_ids": card_instance_ids
	}

	var deck_id = _repo.upsert_deck(deck)

	print("DeckService: Created deck '%s' (id: %s)" % [deck_name, deck_id])
	deck_created.emit(deck_id)
	deck_changed.emit(deck_id)

	return deck_id

## Update an existing deck
## Returns true if successful
func update_deck(deck_id: String, deck_name: String = "", card_instance_ids: Array = []) -> bool:
	if _repo == null:
		push_error("DeckService: Cannot update deck, repo not initialized")
		return false

	var existing_deck = get_deck(deck_id)
	if existing_deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var updated_deck = {
		"id": deck_id,
		"name": deck_name if deck_name != "" else existing_deck.get("name"),
		"card_instance_ids": card_instance_ids if card_instance_ids.size() > 0 else existing_deck.get("card_instance_ids", [])
	}

	var result_id = _repo.upsert_deck(updated_deck)

	if result_id != "":
		print("DeckService: Updated deck '%s'" % deck_id)
		deck_changed.emit(deck_id)
		return true
	else:
		push_error("DeckService: Failed to update deck '%s'" % deck_id)
		return false

## Delete a deck
## Returns true if successful
func delete_deck(deck_id: String) -> bool:
	if _repo == null:
		push_error("DeckService: Cannot delete deck, repo not initialized")
		return false

	var success = _repo.delete_deck(deck_id)

	if success:
		print("DeckService: Deleted deck '%s'" % deck_id)
		deck_deleted.emit(deck_id)
	else:
		push_warning("DeckService: Failed to delete deck '%s'" % deck_id)

	return success

## Add a card to a deck
## Returns true if successful
func add_card_to_deck(deck_id: String, card_instance_id: String) -> bool:
	var deck = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var card_instance_ids = deck.get("card_instance_ids", [])

	# Check if at max size
	if card_instance_ids.size() >= MAX_DECK_SIZE:
		push_warning("DeckService: Deck is at maximum size (%d)" % MAX_DECK_SIZE)
		return false

	# Check if card exists in collection
	var collection = get_node("/root/Collection")
	if collection and collection.get_card(card_instance_id).is_empty():
		push_warning("DeckService: Card instance not found in collection: %s" % card_instance_id)
		return false

	card_instance_ids.append(card_instance_id)

	return update_deck(deck_id, "", card_instance_ids)

## Remove a card from a deck
## Returns true if successful
func remove_card_from_deck(deck_id: String, card_instance_id: String) -> bool:
	var deck = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var card_instance_ids = deck.get("card_instance_ids", [])

	var index = card_instance_ids.find(card_instance_id)
	if index == -1:
		push_warning("DeckService: Card not found in deck: %s" % card_instance_id)
		return false

	card_instance_ids.remove_at(index)

	return update_deck(deck_id, "", card_instance_ids)

## =============================================================================
## DECK VALIDATION
## =============================================================================

## Validate a deck
## Returns true if deck is valid and playable
func validate_deck(deck_id: String) -> bool:
	var deck = get_deck(deck_id)
	if deck.is_empty():
		_emit_validation_failed(deck_id, "Deck not found")
		return false

	var card_instance_ids = deck.get("card_instance_ids", [])

	# Check minimum size
	if card_instance_ids.size() < MIN_DECK_SIZE:
		_emit_validation_failed(deck_id, "Deck has %d cards, minimum is %d" % [card_instance_ids.size(), MIN_DECK_SIZE])
		return false

	# Check maximum size
	if card_instance_ids.size() > MAX_DECK_SIZE:
		_emit_validation_failed(deck_id, "Deck has %d cards, maximum is %d" % [card_instance_ids.size(), MAX_DECK_SIZE])
		return false

	# Validate all cards exist in collection
	var collection = get_node("/root/Collection")
	if collection:
		for card_instance_id in card_instance_ids:
			if collection.get_card(card_instance_id).is_empty():
				_emit_validation_failed(deck_id, "Card instance not found in collection: %s" % card_instance_id)
				return false

	# All checks passed
	return true

## Get validation errors for a deck (for UI display)
## Returns: Array of error strings
func get_validation_errors(deck_id: String) -> Array:
	var errors = []
	var deck = get_deck(deck_id)

	if deck.is_empty():
		errors.append("Deck not found")
		return errors

	var card_instance_ids = deck.get("card_instance_ids", [])

	# Check size constraints
	if card_instance_ids.size() < MIN_DECK_SIZE:
		errors.append("Deck needs %d more cards (minimum: %d)" % [MIN_DECK_SIZE - card_instance_ids.size(), MIN_DECK_SIZE])

	if card_instance_ids.size() > MAX_DECK_SIZE:
		errors.append("Deck has %d too many cards (maximum: %d)" % [card_instance_ids.size() - MAX_DECK_SIZE, MAX_DECK_SIZE])

	# Check missing cards
	var collection = get_node("/root/Collection")
	if collection:
		var missing_count = 0
		for card_instance_id in card_instance_ids:
			if collection.get_card(card_instance_id).is_empty():
				missing_count += 1

		if missing_count > 0:
			errors.append("%d cards no longer exist in collection" % missing_count)

	return errors

## Clean a deck by removing missing cards
## Returns: number of cards removed
func clean_deck(deck_id: String) -> int:
	var deck = get_deck(deck_id)
	if deck.is_empty():
		return 0

	var card_instance_ids = deck.get("card_instance_ids", [])
	var collection = get_node("/root/Collection")
	if not collection:
		return 0

	var valid_cards = []
	var removed_count = 0

	for card_instance_id in card_instance_ids:
		if not collection.get_card(card_instance_id).is_empty():
			valid_cards.append(card_instance_id)
		else:
			removed_count += 1

	if removed_count > 0:
		update_deck(deck_id, "", valid_cards)
		print("DeckService: Cleaned deck '%s', removed %d missing cards" % [deck_id, removed_count])

	return removed_count

## =============================================================================
## INTERNAL
## =============================================================================

func _emit_validation_failed(deck_id: String, reason: String) -> void:
	push_warning("DeckService: Deck validation failed for '%s': %s" % [deck_id, reason])
	validation_failed.emit(deck_id, reason)

func _on_repo_data_changed() -> void:
	# Repo data changed (from external source or load)
	# Could emit a generic decks_changed signal here if needed
	pass
