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

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DeckService: Initializing...")

	# Connect to repo signals for reactive updates
	ProfileRepo.data_changed.connect(_on_repo_data_changed)

	print("DeckService: Ready")

## =============================================================================
## DECK QUERIES
## =============================================================================

## Get all decks for the current profile
func list_decks() -> Array[Dictionary]:
	var result: Array = ProfileRepo.list_decks()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## Get a specific deck by ID
func get_deck(deck_id: String) -> Dictionary:
	return ProfileRepo.get_deck(deck_id)

## Check if a deck exists
func has_deck(deck_id: String) -> bool:
	return not get_deck(deck_id).is_empty()

## Get deck count
func get_deck_count() -> int:
	return list_decks().size()


## Get the active deck ID (the deck used for battles)
func get_active_deck_id() -> String:
	var meta: Dictionary = ProfileRepo.get_profile_meta()
	var deck_id: Variant = meta.get("selected_deck", "")
	return deck_id if deck_id is String else ""


## Set the active deck (the deck used for battles)
## Returns true if successful
func set_active_deck(deck_id: String) -> bool:
	if deck_id != "" and not has_deck(deck_id):
		return false
	var meta: Dictionary = ProfileRepo.get_profile_meta()
	meta["selected_deck"] = deck_id
	ProfileRepo.update_profile_meta(meta)
	return true


## Get all decks for a specific summoner
func list_decks_for_summoner(summoner_id: String) -> Array[Dictionary]:
	var all_decks: Array[Dictionary] = list_decks()
	var filtered: Array[Dictionary] = []
	for deck: Dictionary in all_decks:
		var deck_summoner: Variant = deck.get("summoner_id", "")
		if deck_summoner is String and deck_summoner == summoner_id:
			filtered.append(deck)
	return filtered

## =============================================================================
## DECK OPERATIONS
## =============================================================================

## Create a new deck
## Returns: deck_id
func create_deck(deck_name: String, card_instance_ids: Array = [], summoner_id: String = "") -> String:
	# Determine summoner_id
	var final_summoner_id: String = summoner_id
	if final_summoner_id.is_empty():
		# Default to first unlocked summoner
		var unlocked: Array = ProfileRepo.get_unlocked_summoners()

		if unlocked.is_empty():
			push_error("DeckService: Cannot create deck - no summoners unlocked")
			return ""

		final_summoner_id = unlocked[0]
	else:
		# Validate summoner is unlocked
		if not ProfileRepo.is_summoner_unlocked(final_summoner_id):
			push_error("DeckService: Cannot create deck - summoner not unlocked: %s" % final_summoner_id)
			return ""

	var deck: Dictionary = {
		"name": deck_name,
		"card_instance_ids": card_instance_ids,
		"summoner_id": final_summoner_id
	}

	var deck_id: String = ProfileRepo.upsert_deck(deck)

	print("DeckService: Created deck '%s' with summoner '%s' (id: %s)" % [deck_name, final_summoner_id, deck_id])
	deck_created.emit(deck_id)
	deck_changed.emit(deck_id)

	return deck_id

## Update an existing deck
## Returns true if successful
func update_deck(deck_id: String, deck_name: String = "", card_instance_ids: Array = [], summoner_id: String = "") -> bool:
	var existing_deck: Dictionary = get_deck(deck_id)
	if existing_deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	var existing_name: Variant = existing_deck.get("name")
	var deck_display_name: String = deck_name if deck_name != "" else (existing_name if existing_name is String else "")

	var existing_cards: Variant = existing_deck.get("card_instance_ids", [])
	var card_ids: Array = card_instance_ids if card_instance_ids.size() > 0 else (existing_cards if existing_cards is Array else [])

	var existing_summoner: Variant = existing_deck.get("summoner_id", "")
	var final_summoner_id: String = summoner_id if summoner_id != "" else (existing_summoner if existing_summoner is String else "")

	var updated_deck: Dictionary = {
		"id": deck_id,
		"name": deck_display_name,
		"card_instance_ids": card_ids,
		"summoner_id": final_summoner_id
	}

	var result_id: String = ProfileRepo.upsert_deck(updated_deck)

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
	var success: bool = ProfileRepo.delete_deck(deck_id)

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

	# Check if card is already in this deck (each instance can only appear once)
	if card_instance_id in card_instance_ids:
		push_warning("DeckService: Card instance already in deck: %s" % card_instance_id)
		return false

	# Check if card exists in collection
	var card: Dictionary = Collection.get_card(card_instance_id)
	if card.is_empty():
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

## Set the summoner for a deck
## Returns true if successful
func set_deck_summoner(deck_id: String, summoner_id: String) -> bool:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		push_warning("DeckService: Deck not found: %s" % deck_id)
		return false

	# Validate summoner exists in catalog
	if not SummonerCatalog.is_valid_summoner(summoner_id):
		push_error("DeckService: Invalid summoner_id: %s" % summoner_id)
		return false

	# Validate summoner is unlocked
	if not ProfileRepo.is_summoner_unlocked(summoner_id):
		push_error("DeckService: Summoner not unlocked for this profile: %s" % summoner_id)
		return false

	return update_deck(deck_id, "", [], summoner_id)

## Get the summoner ID for a deck
## Returns empty string if deck not found or summoner not set
func get_deck_summoner(deck_id: String) -> String:
	var deck: Dictionary = get_deck(deck_id)
	if deck.is_empty():
		return ""

	var summoner_id: Variant = deck.get("summoner_id", "")
	if summoner_id is String:
		return summoner_id
	return ""

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

	# Check summoner is set and unlocked
	var summoner_id: String = deck.get("summoner_id", "")
	if summoner_id.is_empty():
		_emit_validation_failed(deck_id, "Deck has no summoner assigned")
		return false

	if not ProfileRepo.is_summoner_unlocked(summoner_id):
		_emit_validation_failed(deck_id, "Summoner not unlocked: %s" % summoner_id)
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
	for card_instance_id: Variant in card_instance_ids:
		if card_instance_id is String:
			var card: Dictionary = Collection.get_card(card_instance_id)
			if card.is_empty():
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

	# Check summoner
	var summoner_id: String = deck.get("summoner_id", "")
	if summoner_id.is_empty():
		errors.append("No summoner assigned")
	elif not ProfileRepo.is_summoner_unlocked(summoner_id):
		errors.append("Summoner not unlocked: %s" % summoner_id)

	var card_instance_ids: Array = deck.get("card_instance_ids", [])

	# Check size constraints
	if card_instance_ids.size() < MIN_DECK_SIZE:
		errors.append("Deck needs %d more cards (minimum: %d)" % [MIN_DECK_SIZE - card_instance_ids.size(), MIN_DECK_SIZE])

	if card_instance_ids.size() > MAX_DECK_SIZE:
		errors.append("Deck has %d too many cards (maximum: %d)" % [card_instance_ids.size() - MAX_DECK_SIZE, MAX_DECK_SIZE])

	# Check missing cards
	var missing_count: int = 0
	for card_instance_id: Variant in card_instance_ids:
		if card_instance_id is String:
			var card: Dictionary = Collection.get_card(card_instance_id)
			if card.is_empty():
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
	var valid_cards: Array = []
	var removed_count: int = 0

	for card_instance_id: Variant in card_instance_ids:
		if card_instance_id is String:
			var card: Dictionary = Collection.get_card(card_instance_id)
			if not card.is_empty():
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
