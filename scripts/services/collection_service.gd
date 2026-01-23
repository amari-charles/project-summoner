extends Node
# CollectionService is registered as autoload "Collection", no class_name needed

## Collection Service - Card Management (GDScript wrapper for C# CollectionService)
##
## Handles all card collection operations (granting, removing, querying).
## UI and gameplay code should call this, never the repository directly.
## Delegates to C# CollectionServiceCS for core operations.
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

## C# service reference
var _cs_service: Node

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CollectionService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/CollectionServiceCS")
	if _cs_service == null:
		push_error("CollectionService: CollectionServiceCS autoload not found")
		return

	# Connect to C# service signals
	_cs_service.CollectionChanged.connect(_on_cs_collection_changed)
	_cs_service.CardsGranted.connect(_on_cs_cards_granted)
	_cs_service.CardRemoved.connect(_on_cs_card_removed)

	print("CollectionService: Ready")

## =============================================================================
## CARD QUERIES (delegated to C#)
## =============================================================================

## Get all card instances in the collection
func list_cards() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.ListCardsDict()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result


## Get all cards owned by a summoner (AccountWide + SummonerBound cards bound to them)
func get_owned_cards(summoner_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetOwnedCardsDict(summoner_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result


## Get a specific card instance by ID
func get_card(card_instance_id: String) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetCardDict(card_instance_id)

## Get count of cards by catalog ID
func get_card_count(catalog_id: String) -> int:
	if _cs_service == null:
		return 0
	return _cs_service.GetCardCount(catalog_id)

## Check if player owns at least one of a card
func has_card(catalog_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.HasCard(catalog_id)

## Get all instances of a specific catalog_id
func get_cards_by_catalog_id(catalog_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetCardsByCatalogIdDict(catalog_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## Get collection grouped by catalog_id
## Returns: {catalog_id: [instance1, instance2, ...]}
func get_collection_grouped() -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetCollectionGroupedDict()

## Get collection summary (for UI display)
## Returns: [{catalog_id: String, count: int, rarity: String, instances: Array}]
func get_collection_summary() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetCollectionSummaryDict()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## CARD OPERATIONS (delegated to C#, with GDScript-specific cascade logic)
## =============================================================================

## Grant cards to the player's collection
## cards: Array of {catalog_id: String, rarity: String}
## Returns: Array of created card instance IDs
func grant_cards(cards: Array) -> Array[String]:
	if _cs_service == null:
		return []

	# Convert to typed array for C# interop
	var typed_cards: Array[Dictionary] = []
	for card_data: Variant in cards:
		if card_data is Dictionary:
			typed_cards.append(card_data)

	var result: Array = _cs_service.GrantCardsDict(typed_cards)
	var instance_ids: Array[String] = []
	for item: Variant in result:
		if item is String:
			instance_ids.append(item)

	return instance_ids

## Grant a single card (convenience method)
## Returns: card instance ID
func grant_card(catalog_id: String, rarity: String = RarityIDs.COMMON) -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GrantCard(catalog_id, rarity)

## Remove a card instance from the collection
## Also removes the card from any decks containing it (cascade delete)
## Returns true if successful, false if card not found
func remove_card(card_instance_id: String) -> bool:
	if _cs_service == null:
		return false

	var success: bool = _cs_service.RemoveCard(card_instance_id)

	if success:
		# Cascade delete: remove from all decks (GDScript-specific)
		var decks: Array[Dictionary] = Decks.list_decks()
		for deck: Dictionary in decks:
			var deck_id: String = deck.get("id", "")
			if deck_id.is_empty():
				continue
			var removed_count: int = Decks.clean_deck(deck_id)
			if removed_count > 0:
				print("CollectionService: Cascade delete removed %d cards from deck '%s'" % [removed_count, deck_id])

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
	var essence_value: int = _cs_service.GetDismantleValue(rarity) if _cs_service else _get_dismantle_value(rarity)

	# Remove card from collection
	if not remove_card(card_instance_id):
		return false

	# Grant essence (GDScript Economy service)
	Economy.add_essence(essence_value)

	print("CollectionService: Dismantled card %s for %d essence" % [card_instance_id, essence_value])
	return true

## =============================================================================
## INTERNAL
## =============================================================================

## Fallback dismantle value calculation (if C# service unavailable)
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

## Signal forwarding from C# service
func _on_cs_collection_changed() -> void:
	collection_changed.emit()

func _on_cs_cards_granted(instance_ids: Array) -> void:
	cards_granted.emit(instance_ids)

func _on_cs_card_removed(card_instance_id: String) -> void:
	card_removed.emit(card_instance_id)
