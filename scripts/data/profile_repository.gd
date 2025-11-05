extends Node
class_name IProfileRepo

## Profile Repository Interface
##
## Abstract base class defining the contract for profile data storage.
## Implementations can use JSON files, SQL databases, cloud services, etc.
##
## UI and gameplay code should NEVER call this directly - use domain services instead:
## - EconomyService for resources (gold, essence, fragments)
## - CollectionService for cards
## - DeckService for decks
##
## This abstraction allows swapping storage backends without changing game logic.

## Signals
signal profile_loaded(profile_id: String)
signal profile_saved(profile_id: String)
signal save_failed(error: String)
signal data_changed

## =============================================================================
## PROFILE OPERATIONS
## =============================================================================

## Load profile data from storage
## Returns true if successful, false if profile doesn't exist or load failed
func load_profile(profile_id: String) -> bool:
	push_error("IProfileRepo.load_profile() not implemented")
	return false

## Save profile data to storage
## Set immediate=true to bypass debouncing and save immediately
func save_profile(immediate: bool = false) -> void:
	push_error("IProfileRepo.save_profile() not implemented")

## Get the currently loaded profile ID
func get_current_profile_id() -> String:
	push_error("IProfileRepo.get_current_profile_id() not implemented")
	return ""

## Reset profile to fresh state (new game)
func reset_profile() -> void:
	push_error("IProfileRepo.reset_profile() not implemented")

## Get a deep copy of current profile data (for debugging)
func snapshot() -> Dictionary:
	push_error("IProfileRepo.snapshot() not implemented")
	return {}

## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

## Get current resource values
func get_resources() -> Dictionary:
	push_error("IProfileRepo.get_resources() not implemented")
	return {}

## Update resources (delta can be positive or negative)
## Example: {"gold": 50, "essence": -10}
func update_resources(delta: Dictionary) -> void:
	push_error("IProfileRepo.update_resources() not implemented")

## =============================================================================
## CARD COLLECTION OPERATIONS
## =============================================================================

## Grant cards to the profile
## cards: Array of {catalog_id: String, rarity: String}
## Returns: Array of created card instance IDs
func grant_cards(cards: Array) -> Array:
	push_error("IProfileRepo.grant_cards() not implemented")
	return []

## Remove a card instance from the collection
## Returns true if successful, false if card not found
func remove_card(card_instance_id: String) -> bool:
	push_error("IProfileRepo.remove_card() not implemented")
	return false

## Get all card instances in the collection
func list_cards() -> Array:
	push_error("IProfileRepo.list_cards() not implemented")
	return []

## Get count of cards by catalog ID
func get_card_count(catalog_id: String) -> int:
	push_error("IProfileRepo.get_card_count() not implemented")
	return 0

## Get a specific card instance by ID
func get_card(card_instance_id: String) -> Dictionary:
	push_error("IProfileRepo.get_card() not implemented")
	return {}

## =============================================================================
## DECK OPERATIONS
## =============================================================================

## Create or update a deck
## deck: {id: String (optional), name: String, card_instance_ids: Array[String]}
## Returns: deck_id
func upsert_deck(deck: Dictionary) -> String:
	push_error("IProfileRepo.upsert_deck() not implemented")
	return ""

## Delete a deck
## Returns true if successful, false if deck not found
func delete_deck(deck_id: String) -> bool:
	push_error("IProfileRepo.delete_deck() not implemented")
	return false

## Get all decks for the current profile
func list_decks() -> Array:
	push_error("IProfileRepo.list_decks() not implemented")
	return []

## Get a specific deck by ID
func get_deck(deck_id: String) -> Dictionary:
	push_error("IProfileRepo.get_deck() not implemented")
	return {}

## =============================================================================
## METADATA OPERATIONS
## =============================================================================

## Get profile metadata (tutorial flags, achievements, etc.)
func get_profile_meta() -> Dictionary:
	push_error("IProfileRepo.get_profile_meta() not implemented")
	return {}

## Update profile metadata
func update_profile_meta(meta: Dictionary) -> void:
	push_error("IProfileRepo.update_profile_meta() not implemented")

## Get user settings (volume, language, etc.)
func get_settings() -> Dictionary:
	push_error("IProfileRepo.get_settings() not implemented")
	return {}

## Update user settings
func update_settings(settings: Dictionary) -> void:
	push_error("IProfileRepo.update_settings() not implemented")

## Get last match info (seed, result, duration)
func get_last_match() -> Dictionary:
	push_error("IProfileRepo.get_last_match() not implemented")
	return {}

## Update last match info
func update_last_match(match_info: Dictionary) -> void:
	push_error("IProfileRepo.update_last_match() not implemented")
