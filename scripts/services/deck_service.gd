extends Node
# DeckService is registered as autoload "Decks", no class_name needed

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

## Constants
const MIN_DECK_SIZE: int = 1  # Minimum 1 card required
const MAX_DECK_SIZE: int = 30  # Maximum cards allowed in a deck

## Signals
signal deck_changed(deck_id: String)
signal deck_created(deck_id: String)
signal deck_deleted(deck_id: String)
signal validation_failed(deck_id: String, reason: String)

## Repository reference (injected by autoload order)
var _repo: Node = null  # JsonProfileRepo instance

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
	if _repo.has_signal("data_changed"):
		var data_changed_signal: Signal = _repo.get("data_changed")
		data_changed_signal.connect(_on_repo_data_changed)

	print("DeckService: Ready")

## =============================================================================
## DECK QUERIES
## =============================================================================

## Get all decks for the current profile
func list_decks() -> Array[Dictionary]:
	if _repo == null:
		return []
	if _repo.has_method("list_decks"):
		var result: Variant = _repo.call("list_decks")
		if result is Array:
			var result_array: Array = result
			var typed_result: Array[Dictionary] = []
			typed_result.assign(result_array)
			return typed_result
	return []

## Get a specific deck by ID
func get_deck(deck_id: String) -> Dictionary:
	if _repo == null:
		var empty: Dictionary = {}
		return empty
	if _repo.has_method("get_deck"):
		var result: Variant = _repo.call("get_deck", deck_id)
		if result is Dictionary:
			return result
	var default: Dictionary = {}
	return default

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

	var deck: Dictionary = {
		"name": deck_name,
		"card_instance_ids": card_instance_ids
	}

	var deck_id: String = ""
	if _repo.has_method("upsert_deck"):
		var result: Variant = _repo.call("upsert_deck", deck)
		if result is String:
			deck_id = result

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

	var existing_deck: Dictionary = get_deck(deck_id)
	if existing_deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var deck_display_name: String = deck_name if deck_name != "" else existing_deck.get("name")
	var card_ids: Array = card_instance_ids if card_instance_ids.size() > 0 else existing_deck.get("card_instance_ids", [])

	var updated_deck: Dictionary = {
		"id": deck_id,
		"name": deck_display_name,
		"card_instance_ids": card_ids
	}

	var result_id: String = ""
	if _repo.has_method("upsert_deck"):
		var result: Variant = _repo.call("upsert_deck", updated_deck)
		if result is String:
			result_id = result

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

	var success: bool = false
	if _repo.has_method("delete_deck"):
		var result: Variant = _repo.call("delete_deck", deck_id)
		if result is bool:
			success = result

	if success:
		print("DeckService: Deleted deck '%s'" % deck_id)
		deck_deleted.emit(deck_id)
	else:
		push_warning("DeckService: Failed to delete deck '%s'" % deck_id)

	return success

## Add a card to a deck
## Returns true if successful
func add_card_to_deck(deck_id: String, card_instance_id: String) -> bool:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var card_instance_ids: Array = deck.get("card_instance_ids", [])

	# Check if at max size
	if card_instance_ids.size() >= MAX_DECK_SIZE:
		push_warning("DeckService: Deck is at maximum size (%d)" % MAX_DECK_SIZE)
		return false

	# Check if card exists in collection
	var collection: Node = get_node("/root/Collection")
	if collection and collection.has_method("get_card"):
		var card_result: Variant = collection.call("get_card", card_instance_id)
		if card_result is Dictionary:
			var card_dict: Dictionary = card_result
			if card_dict.is_empty():
				push_warning("DeckService: Card instance not found in collection: %s" % card_instance_id)
				return false

	card_instance_ids.append(card_instance_id)

	return update_deck(deck_id, "", card_instance_ids)

## Remove a card from a deck
## Returns true if successful
func remove_card_from_deck(deck_id: String, card_instance_id: String) -> bool:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var card_instance_ids: Array = deck.get("card_instance_ids", [])

	var index: int = card_instance_ids.find(card_instance_id)
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
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		_emit_validation_failed(deck_id, "Deck not found")
		return false

	var card_instance_ids: Array = deck.get("card_instance_ids", [])

	# Check minimum size
	if card_instance_ids.size() < MIN_DECK_SIZE:
		_emit_validation_failed(deck_id, "Deck has %d cards, minimum is %d" % [card_instance_ids.size(), MIN_DECK_SIZE])
		return false

	# Check maximum size
	if card_instance_ids.size() > MAX_DECK_SIZE:
		_emit_validation_failed(deck_id, "Deck has %d cards, maximum is %d" % [card_instance_ids.size(), MAX_DECK_SIZE])
		return false

	# Validate all cards exist in collection
	var collection: Node = get_node("/root/Collection")
	if collection and collection.has_method("get_card"):
		for card_instance_id: Variant in card_instance_ids:
			if card_instance_id is String:
				var card_result: Variant = collection.call("get_card", card_instance_id)
				if card_result is Dictionary:
					var card_dict: Dictionary = card_result
					if card_dict.is_empty():
						_emit_validation_failed(deck_id, "Card instance not found in collection: %s" % card_instance_id)
						return false

	# All checks passed
	return true

## Get validation errors for a deck (for UI display)
## Returns: Array of error strings
func get_validation_errors(deck_id: String) -> Array[String]:
	var errors: Array[String] = []
	var deck: Dictionary = get_deck(deck_id)

	if deck.is_empty():
		errors.append("Deck not found")
		return errors

	var card_instance_ids: Array = deck.get("card_instance_ids", [])

	# Check size constraints
	if card_instance_ids.size() < MIN_DECK_SIZE:
		errors.append("Deck needs %d more cards (minimum: %d)" % [MIN_DECK_SIZE - card_instance_ids.size(), MIN_DECK_SIZE])

	if card_instance_ids.size() > MAX_DECK_SIZE:
		errors.append("Deck has %d too many cards (maximum: %d)" % [card_instance_ids.size() - MAX_DECK_SIZE, MAX_DECK_SIZE])

	# Check missing cards
	var collection: Node = get_node("/root/Collection")
	if collection and collection.has_method("get_card"):
		var missing_count: int = 0
		for card_instance_id: Variant in card_instance_ids:
			if card_instance_id is String:
				var card_result: Variant = collection.call("get_card", card_instance_id)
				if card_result is Dictionary:
					var card_dict: Dictionary = card_result
					if card_dict.is_empty():
						missing_count += 1

		if missing_count > 0:
			errors.append("%d cards no longer exist in collection" % missing_count)

	return errors

## Clean a deck by removing missing cards
## Returns: number of cards removed
func clean_deck(deck_id: String) -> int:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		return 0

	var card_instance_ids: Array = deck.get("card_instance_ids", [])
	var collection: Node = get_node("/root/Collection")
	if not collection or not collection.has_method("get_card"):
		return 0

	var valid_cards: Array = []
	var removed_count: int = 0

	for card_instance_id: Variant in card_instance_ids:
		if card_instance_id is String:
			var card_result: Variant = collection.call("get_card", card_instance_id)
			if card_result is Dictionary:
				var card_dict: Dictionary = card_result
				if not card_dict.is_empty():
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
