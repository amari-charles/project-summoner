extends Node
class_name EnemyDeckLoader

## EnemyDeckLoader - Loads enemy decks from campaign battle definitions
##
## Static utility class that converts enemy deck definitions to Card resources for battle.
## Unlike DeckLoader (which loads from player's collection), this directly creates
## Card resources from catalog IDs.

## Load enemy deck from BattleContext
static func load_enemy_deck_for_battle() -> Array[Card]:
	var cards: Array[Card] = []

	# Get battle context
	var battle_context = _get_service("/root/BattleContext")
	if not battle_context:
		push_error("EnemyDeckLoader: BattleContext not found!")
		return cards

	var battle_config = battle_context.battle_config
	if battle_config.is_empty():
		push_error("EnemyDeckLoader: Battle config is empty! Configure BattleContext before loading battle scene.")
		return cards

	# Get enemy deck definition from config
	var enemy_deck_def = battle_config.get("enemy_deck", [])
	if enemy_deck_def.is_empty():
		push_warning("EnemyDeckLoader: Battle has no enemy deck defined!")
		return cards

	print("EnemyDeckLoader: Loading enemy deck from BattleContext")

	# Convert deck definition to Card resources
	for entry in enemy_deck_def:
		var catalog_id = entry.get("catalog_id", "")
		var count = entry.get("count", 1)

		if catalog_id == "":
			push_warning("EnemyDeckLoader: Empty catalog_id in enemy deck definition")
			continue

		# Create 'count' copies of this card
		for i in range(count):
			var card = _create_card_from_catalog(catalog_id)
			if card:
				cards.append(card)
			else:
				push_warning("EnemyDeckLoader: Failed to create card: %s" % catalog_id)

	print("EnemyDeckLoader: Successfully loaded %d cards for enemy deck" % cards.size())
	return cards

## Load enemy deck for a specific battle by ID
static func load_deck_for_battle(battle_id: String) -> Array[Card]:
	var cards: Array[Card] = []

	# Get campaign service
	var campaign = _get_service("/root/Campaign")
	if not campaign:
		push_error("EnemyDeckLoader: Campaign service not found!")
		return cards

	# Get battle data
	var battle = campaign.get_battle(battle_id)
	if battle.is_empty():
		push_error("EnemyDeckLoader: Battle not found: %s" % battle_id)
		return cards

	# Get enemy deck definition
	var enemy_deck_def = battle.get("enemy_deck", [])
	if enemy_deck_def.is_empty():
		push_warning("EnemyDeckLoader: Battle '%s' has no enemy deck defined!" % battle_id)
		return cards

	print("EnemyDeckLoader: Battle '%s' enemy deck: %s" % [battle.get("name", ""), enemy_deck_def])

	# Convert deck definition to Card resources
	for entry in enemy_deck_def:
		var catalog_id = entry.get("catalog_id", "")
		var count = entry.get("count", 1)

		if catalog_id == "":
			push_warning("EnemyDeckLoader: Empty catalog_id in enemy deck definition")
			continue

		# Create 'count' copies of this card
		for i in range(count):
			var card = _create_card_from_catalog(catalog_id)
			if card:
				cards.append(card)
			else:
				push_warning("EnemyDeckLoader: Failed to create card: %s" % catalog_id)

	print("EnemyDeckLoader: Successfully loaded %d cards for enemy deck" % cards.size())
	return cards

## Create a Card resource from a catalog ID
static func _create_card_from_catalog(catalog_id: String) -> Card:
	# Get card catalog
	var catalog = _get_service("/root/CardCatalog")
	if not catalog:
		push_error("EnemyDeckLoader: CardCatalog not found!")
		return null

	# Check if card exists
	if not catalog.has_card(catalog_id):
		push_error("EnemyDeckLoader: Card '%s' not found in catalog!" % catalog_id)
		return null

	# Load the Card resource (.tres file)
	var card_path = "res://resources/cards/%s_card.tres" % catalog_id
	var loaded_card: Resource = load(card_path)

	if not loaded_card or not loaded_card is Card:
		push_error("EnemyDeckLoader: Failed to load card resource: %s" % card_path)
		return null

	# Type narrow to Card for safe property access
	var card_template: Card = loaded_card

	# Duplicate to avoid mutating shared resource
	var duplicated_card: Resource = card_template.duplicate()
	if not duplicated_card is Card:
		push_error("EnemyDeckLoader: Card duplicate failed for: %s" % catalog_id)
		return null

	var card: Card = duplicated_card
	card.catalog_id = catalog_id

	return card

## Helper to get autoload service safely
static func _get_service(path: String):
	var tree = Engine.get_main_loop() as SceneTree
	if tree and tree.root:
		return tree.root.get_node_or_null(path)
	return null
