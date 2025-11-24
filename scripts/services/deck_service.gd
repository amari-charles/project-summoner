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
func create_deck(deck_name: String, card_instance_ids: Array = [], hero_id: String = "") -> String:
	if _repo == null:
		push_error("DeckService: Cannot create deck, repo not initialized")
		return ""

	# Determine hero_id
	var final_hero_id: String = hero_id
	if final_hero_id.is_empty():
		# Default to first unlocked hero
		var unlocked: Array = []
		if _repo.has_method("get_unlocked_heroes"):
			var result: Variant = _repo.call("get_unlocked_heroes")
			if result is Array:
				unlocked = result

		if unlocked.is_empty():
			push_error("DeckService: Cannot create deck - no heroes unlocked")
			return ""

		final_hero_id = unlocked[0]
	else:
		# Validate hero is unlocked
		var is_unlocked: bool = false
		if _repo.has_method("is_hero_unlocked"):
			var result: Variant = _repo.call("is_hero_unlocked", final_hero_id)
			if result is bool:
				is_unlocked = result

		if not is_unlocked:
			push_error("DeckService: Cannot create deck - hero not unlocked: %s" % final_hero_id)
			return ""

	var deck: Dictionary = {
		"name": deck_name,
		"card_instance_ids": card_instance_ids,
		"hero_id": final_hero_id
	}

	var deck_id: String = ""
	if _repo.has_method("upsert_deck"):
		var result: Variant = _repo.call("upsert_deck", deck)
		if result is String:
			deck_id = result

	print("DeckService: Created deck '%s' with hero '%s' (id: %s)" % [deck_name, final_hero_id, deck_id])
	deck_created.emit(deck_id)
	deck_changed.emit(deck_id)

	return deck_id

## Update an existing deck
## Returns true if successful
func update_deck(deck_id: String, deck_name: String = "", card_instance_ids: Array = [], hero_id: String = "") -> bool:
	if _repo == null:
		push_error("DeckService: Cannot update deck, repo not initialized")
		return false

	var existing_deck: Dictionary = get_deck(deck_id)
	if existing_deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var existing_name: Variant = existing_deck.get("name")
	var deck_display_name: String = deck_name if deck_name != "" else (existing_name if existing_name is String else "")

	var existing_cards: Variant = existing_deck.get("card_instance_ids", [])
	var card_ids: Array = card_instance_ids if card_instance_ids.size() > 0 else (existing_cards if existing_cards is Array else [])

	var existing_hero: Variant = existing_deck.get("hero_id", "")
	var final_hero_id: String = hero_id if hero_id != "" else (existing_hero if existing_hero is String else "")

	var updated_deck: Dictionary = {
		"id": deck_id,
		"name": deck_display_name,
		"card_instance_ids": card_ids,
		"hero_id": final_hero_id
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

## Set the hero for a deck
## Returns true if successful
func set_deck_hero(deck_id: String, hero_id: String) -> bool:
	if _repo == null:
		push_error("DeckService: Cannot set deck hero, repo not initialized")
		return false

	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	# Validate hero exists in catalog
	var hero_catalog: Node = get_node("/root/HeroCatalog")
	if hero_catalog and hero_catalog.has_method("is_valid_hero"):
		var is_valid: Variant = hero_catalog.call("is_valid_hero", hero_id)
		if is_valid is bool and not is_valid:
			push_error("DeckService: Invalid hero_id: %s" % hero_id)
			return false

	# Validate hero is unlocked
	var is_unlocked: bool = false
	if _repo.has_method("is_hero_unlocked"):
		var result: Variant = _repo.call("is_hero_unlocked", hero_id)
		if result is bool:
			is_unlocked = result

	if not is_unlocked:
		push_error("DeckService: Hero not unlocked for this profile: %s" % hero_id)
		return false

	return update_deck(deck_id, "", [], hero_id)

## Get the hero ID for a deck
## Returns empty string if deck not found or hero not set
func get_deck_hero(deck_id: String) -> String:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		return ""

	var hero_id: Variant = deck.get("hero_id", "")
	if hero_id is String:
		return hero_id
	return ""

## =============================================================================
## DECK VALIDATION
## =============================================================================

## Validate a deck
## Returns true if deck is valid and playable
func validate_deck(deck_id: String) -> bool:
	print("DeckService.validate_deck: Validating deck '%s'" % deck_id)
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		_emit_validation_failed(deck_id, "Deck not found")
		return false

	# Check hero is set and unlocked
	var hero_id: String = deck.get("hero_id", "")
	print("DeckService.validate_deck: Deck hero_id = '%s'" % hero_id)
	if hero_id.is_empty():
		_emit_validation_failed(deck_id, "Deck has no hero assigned")
		return false

	var is_unlocked: bool = false
	if _repo and _repo.has_method("is_hero_unlocked"):
		var result: Variant = _repo.call("is_hero_unlocked", hero_id)
		if result is bool:
			is_unlocked = result

	print("DeckService.validate_deck: Hero '%s' unlocked = %s" % [hero_id, is_unlocked])
	if not is_unlocked:
		# Debug: Show what heroes ARE unlocked
		var unlocked_heroes: Array = []
		if _repo and _repo.has_method("get_unlocked_heroes"):
			unlocked_heroes = _repo.call("get_unlocked_heroes")
		print("DeckService.validate_deck: Unlocked heroes in profile: %s" % str(unlocked_heroes))
		_emit_validation_failed(deck_id, "Hero not unlocked: %s" % hero_id)
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

	# Check hero
	var hero_id: String = deck.get("hero_id", "")
	if hero_id.is_empty():
		errors.append("No hero assigned")
	else:
		var is_unlocked: bool = false
		if _repo and _repo.has_method("is_hero_unlocked"):
			var result: Variant = _repo.call("is_hero_unlocked", hero_id)
			if result is bool:
				is_unlocked = result

		if not is_unlocked:
			errors.append("Hero not unlocked: %s" % hero_id)

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
