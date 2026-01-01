extends Node
# CollectionService is registered as autoload "Collection", no class_name needed

## Collection Service - Card Management
##
## Handles all card collection operations (granting, removing, querying).
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   var instance_ids = Collection.grant_cards([
##       {"catalog_id": "fireball", "rarity": "rare"},
##       {"catalog_id": "fire_elemental", "rarity": "common"}
##   ])
##   var count = Collection.get_card_count("fireball")
##   var all_cards = Collection.list_cards()
##
## Emits signals for reactive UI updates.

## Signals
signal collection_changed
signal cards_granted(instance_ids: Array)
signal card_removed(card_instance_id: String)

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CollectionService: Initializing...")

	# Connect to repo signals for reactive updates
	ProfileRepo.data_changed.connect(_on_repo_data_changed)

	print("CollectionService: Ready")

## =============================================================================
## CARD QUERIES
## =============================================================================

## Get all card instances in the collection
func list_cards() -> Array[Dictionary]:
	var result: Array = ProfileRepo.list_cards()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## Get a specific card instance by ID
func get_card(card_instance_id: String) -> Dictionary:
	return ProfileRepo.get_card(card_instance_id)

## Get count of cards by catalog ID
func get_card_count(catalog_id: String) -> int:
	return ProfileRepo.get_card_count(catalog_id)

## Check if player owns at least one of a card
func has_card(catalog_id: String) -> bool:
	return get_card_count(catalog_id) > 0

## Get all instances of a specific catalog_id
func get_cards_by_catalog_id(catalog_id: String) -> Array[Dictionary]:
	var collection: Array[Dictionary] = list_cards()
	var matching: Array[Dictionary] = []
	for card: Dictionary in collection:
		if card.get("catalog_id") == catalog_id:
			matching.append(card)
	return matching

## Get collection grouped by catalog_id
## Returns: {catalog_id: [instance1, instance2, ...]}
func get_collection_grouped() -> Dictionary:
	var collection: Array[Dictionary] = list_cards()
	var grouped: Dictionary = {}

	for card: Dictionary in collection:
		var catalog_id: String = card.get("catalog_id", "unknown")
		if not catalog_id in grouped:
			grouped[catalog_id] = []
		var card_list: Array = grouped[catalog_id]
		card_list.append(card)

	return grouped

## Get collection summary (for UI display)
## Returns: [{catalog_id: String, count: int, rarity: String, instances: Array}]
func get_collection_summary() -> Array[Dictionary]:
	var grouped: Dictionary = get_collection_grouped()
	var summary: Array[Dictionary] = []

	for catalog_id: String in grouped:
		var instances_var: Variant = grouped.get(catalog_id)
		if not instances_var is Array:
			continue
		var instances: Array = instances_var
		var rarity: String = RarityIDs.COMMON
		if instances.size() > 0:
			var first_item: Variant = instances[0]
			if first_item is Dictionary:
				var first_dict: Dictionary = first_item
				rarity = first_dict.get("rarity", RarityIDs.COMMON)
		var summary_entry: Dictionary = {
			"catalog_id": catalog_id,
			"count": instances.size(),
			"rarity": rarity,
			"instances": instances
		}
		summary.append(summary_entry)

	return summary

## =============================================================================
## CARD OPERATIONS
## =============================================================================

## Grant cards to the player's collection
## cards: Array of {catalog_id: String, rarity: String}
## Returns: Array of created card instance IDs
func grant_cards(cards: Array) -> Array[String]:
	# Validate all cards exist in catalog
	var valid_cards: Array[Dictionary] = []
	for card_data: Variant in cards:
		if card_data is Dictionary:
			var card_dict: Dictionary = card_data
			var catalog_id: String = card_dict.get("catalog_id", "")
			if CardCatalog.has_card(catalog_id):
				valid_cards.append(card_dict)
			else:
				push_warning("CollectionService: Cannot grant card '%s' - not found in CardCatalog" % catalog_id)

	if valid_cards.size() == 0:
		push_warning("CollectionService: No valid cards to grant")
		return []

	var result: Array = ProfileRepo.grant_cards(valid_cards)
	var instance_ids: Array[String] = []
	for item: Variant in result:
		if item is String:
			instance_ids.append(item)

	print("CollectionService: Granted %d cards (requested: %d, valid: %d)" % [instance_ids.size(), cards.size(), valid_cards.size()])
	cards_granted.emit(instance_ids)
	collection_changed.emit()

	return instance_ids

## Grant a single card (convenience method)
## Returns: card instance ID
func grant_card(catalog_id: String, rarity: String = RarityIDs.COMMON) -> String:
	var instance_ids: Array[String] = grant_cards([{"catalog_id": catalog_id, "rarity": rarity}])
	return instance_ids[0] if instance_ids.size() > 0 else ""

## Remove a card instance from the collection
## Also removes the card from any decks containing it (cascade delete)
## Returns true if successful, false if card not found
func remove_card(card_instance_id: String) -> bool:
	var success: bool = ProfileRepo.remove_card(card_instance_id)

	if success:
		print("CollectionService: Removed card instance: %s" % card_instance_id)

		# Cascade delete: remove from all decks
		var decks: Array[Dictionary] = Decks.list_decks()
		for deck: Dictionary in decks:
			var deck_id: String = deck.get("id", "")
			if deck_id.is_empty():
				continue
			var removed_count: int = Decks.clean_deck(deck_id)
			if removed_count > 0:
				print("CollectionService: Cascade delete removed %d cards from deck '%s'" % [removed_count, deck_id])

		card_removed.emit(card_instance_id)
		collection_changed.emit()
	else:
		push_warning("CollectionService: Failed to remove card instance: %s" % card_instance_id)

	return success

## Dismantle a card for resources (remove + grant essence)
## Returns true if successful
func dismantle_card(card_instance_id: String) -> bool:
	var card: Dictionary = get_card(card_instance_id)
	if card.is_empty():
		push_warning("CollectionService: Card instance not found: %s" % card_instance_id)
		return false

	# Calculate essence value based on rarity
	var rarity: StringName = card.get("rarity", RarityIDs.COMMON)
	var essence_value: int = _get_dismantle_value(rarity)

	# Remove card from collection
	if not remove_card(card_instance_id):
		return false

	# Grant essence
	Economy.add_essence(essence_value)

	print("CollectionService: Dismantled card %s for %d essence" % [card_instance_id, essence_value])
	return true

## =============================================================================
## INTERNAL
## =============================================================================

func _get_dismantle_value(rarity: String) -> int:
	match rarity:
		RarityIDs.COMMON:
			return 5
		RarityIDs.RARE:
			return 20
		RarityIDs.EPIC:
			return 100
		RarityIDs.LEGENDARY:
			return 500
		_:
			return 5

func _on_repo_data_changed() -> void:
	# Repo data changed (from external source or load)
	collection_changed.emit()
