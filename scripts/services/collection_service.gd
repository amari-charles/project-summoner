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
##       {"catalog_id": "warrior", "rarity": "common"}
##   ])
##   var count = Collection.get_card_count("fireball")
##   var all_cards = Collection.list_cards()
##
## Emits signals for reactive UI updates.

## Signals
signal collection_changed
signal cards_granted(instance_ids: Array)
signal card_removed(card_instance_id: String)

## Service references (injected by autoload order)
var _repo = null  # JsonProfileRepo instance
var _catalog = null  # CardCatalog instance

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CollectionService: Initializing...")

	# Wait for autoloads to be ready
	await get_tree().process_frame

	_repo = get_node("/root/ProfileRepo")
	if _repo == null:
		push_error("CollectionService: ProfileRepo not found! Ensure it's registered as autoload.")
		return

	_catalog = get_node("/root/CardCatalog")
	if _catalog == null:
		push_error("CollectionService: CardCatalog not found! Ensure it's registered as autoload.")
		return

	# Connect to repo signals
	_repo.data_changed.connect(_on_repo_data_changed)

	print("CollectionService: Ready")

## =============================================================================
## CARD QUERIES
## =============================================================================

## Get all card instances in the collection
func list_cards() -> Array[Dictionary]:
	if _repo == null:
		return []
	return _repo.list_cards()

## Get a specific card instance by ID
func get_card(card_instance_id: String) -> Dictionary:
	if _repo == null:
		return {}
	return _repo.get_card(card_instance_id)

## Get count of cards by catalog ID
func get_card_count(catalog_id: String) -> int:
	if _repo == null:
		return 0
	return _repo.get_card_count(catalog_id)

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
		var instances: Array = grouped[catalog_id]
		var rarity: String = "common"
		if instances.size() > 0 and instances[0] is Dictionary:
			rarity = instances[0].get("rarity", "common")
		summary.append({
			"catalog_id": catalog_id,
			"count": instances.size(),
			"rarity": rarity,
			"instances": instances
		})

	return summary

## =============================================================================
## CARD OPERATIONS
## =============================================================================

## Grant cards to the player's collection
## cards: Array of {catalog_id: String, rarity: String}
## Returns: Array of created card instance IDs
func grant_cards(cards: Array) -> Array[String]:
	if _repo == null:
		push_error("CollectionService: Cannot grant cards, repo not initialized")
		return []

	if _catalog == null:
		push_error("CollectionService: Cannot grant cards, catalog not initialized")
		return []

	# Validate all cards exist in catalog
	var valid_cards: Array[Dictionary] = []
	for card_data in cards:
		if card_data is Dictionary:
			var catalog_id: String = card_data.get("catalog_id", "")
			if _catalog.has_card(catalog_id):
				valid_cards.append(card_data)
			else:
				push_warning("CollectionService: Cannot grant card '%s' - not found in CardCatalog" % catalog_id)

	if valid_cards.size() == 0:
		push_warning("CollectionService: No valid cards to grant")
		return []

	var instance_ids: Array[String] = _repo.grant_cards(valid_cards)

	print("CollectionService: Granted %d cards (requested: %d, valid: %d)" % [instance_ids.size(), cards.size(), valid_cards.size()])
	cards_granted.emit(instance_ids)
	collection_changed.emit()

	return instance_ids

## Grant a single card (convenience method)
## Returns: card instance ID
func grant_card(catalog_id: String, rarity: String = "common") -> String:
	var instance_ids: Array[String] = grant_cards([{"catalog_id": catalog_id, "rarity": rarity}])
	return instance_ids[0] if instance_ids.size() > 0 else ""

## Remove a card instance from the collection
## Returns true if successful, false if card not found
func remove_card(card_instance_id: String) -> bool:
	if _repo == null:
		push_error("CollectionService: Cannot remove card, repo not initialized")
		return false

	var success: bool = _repo.remove_card(card_instance_id)

	if success:
		print("CollectionService: Removed card instance: %s" % card_instance_id)
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
	var rarity: String = card.get("rarity", "common")
	var essence_value: int = _get_dismantle_value(rarity)

	# Remove card from collection
	if not remove_card(card_instance_id):
		return false

	# Grant essence
	var economy: Node = get_node("/root/Economy")
	if economy:
		economy.add_essence(essence_value)

	print("CollectionService: Dismantled card %s for %d essence" % [card_instance_id, essence_value])
	return true

## =============================================================================
## INTERNAL
## =============================================================================

func _get_dismantle_value(rarity: String) -> int:
	match rarity:
		"common":
			return 5
		"rare":
			return 20
		"epic":
			return 100
		"legendary":
			return 500
		_:
			return 5

func _on_repo_data_changed() -> void:
	# Repo data changed (from external source or load)
	collection_changed.emit()
