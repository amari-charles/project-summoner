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

	print("EnemyDeckLoader: Starting deck load...")

	# Get battle context
	var battle_context: Variant = _get_autoload("BattleContext")
	if not battle_context:
		push_error("EnemyDeckLoader: BattleContext not found!")
		return cards

	print("EnemyDeckLoader: BattleContext found, reading battle_config...")

	var battle_config_variant: Variant = {}
	if battle_context is Object:
		var battle_context_obj: Object = battle_context
		battle_config_variant = battle_context_obj.get("battle_config")
	var battle_config: Dictionary = battle_config_variant if battle_config_variant is Dictionary else {}

	print("EnemyDeckLoader: battle_config is_empty=%s, keys=%s" % [battle_config.is_empty(), battle_config.keys()])

	if battle_config.is_empty():
		push_error("EnemyDeckLoader: Battle config is empty! Configure BattleContext before loading battle scene.")
		push_error("EnemyDeckLoader: Did you call BattleContext.configure_campaign_battle() before changing scene?")
		return cards

	# Get enemy deck definition from config
	var enemy_deck_def_variant: Variant = battle_config.get("enemy_deck", [])
	var enemy_deck_def: Array = enemy_deck_def_variant if enemy_deck_def_variant is Array else []

	print("EnemyDeckLoader: enemy_deck definition: %s (size: %d)" % [enemy_deck_def, enemy_deck_def.size()])

	if enemy_deck_def.is_empty():
		# Check if this battle uses event sequence for spawning
		if battle_config.has("event_sequence"):
			print("EnemyDeckLoader: Battle uses event_sequence for enemy spawning - deck intentionally empty")
		else:
			push_warning("EnemyDeckLoader: Battle has no enemy deck defined!")
		return cards

	print("EnemyDeckLoader: Loading enemy deck from BattleContext")

	# Convert deck definition to Card resources
	for entry_variant: Variant in enemy_deck_def:
		var entry: Dictionary = entry_variant if entry_variant is Dictionary else {}
		var catalog_id_variant: Variant = entry.get("catalog_id", "")
		var catalog_id: String = catalog_id_variant if catalog_id_variant is String else ""
		var count_variant: Variant = entry.get("count", 1)
		var count: int = int(count_variant) if (count_variant is int or count_variant is float) else 1

		if catalog_id == "":
			push_warning("EnemyDeckLoader: Empty catalog_id in enemy deck definition")
			continue

		# Create 'count' copies of this card
		for i: int in range(count):
			var card: Card = _create_card_from_catalog(catalog_id)
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
	var campaign: Variant = _get_autoload("Campaign")
	if not campaign:
		push_error("EnemyDeckLoader: Campaign service not found!")
		return cards

	# Get battle data
	var battle_variant: Variant = {}
	if campaign is Object:
		var campaign_obj: Object = campaign
		battle_variant = campaign_obj.call("get_battle", battle_id)
	var battle: Dictionary = battle_variant if battle_variant is Dictionary else {}
	if battle.is_empty():
		push_error("EnemyDeckLoader: Battle not found: %s" % battle_id)
		return cards

	# Get enemy deck definition
	var enemy_deck_def_variant: Variant = battle.get("enemy_deck", [])
	var enemy_deck_def: Array = enemy_deck_def_variant if enemy_deck_def_variant is Array else []
	if enemy_deck_def.is_empty():
		push_warning("EnemyDeckLoader: Battle '%s' has no enemy deck defined!" % battle_id)
		return cards

	print("EnemyDeckLoader: Battle '%s' enemy deck: %s" % [battle.get("name", ""), enemy_deck_def])

	# Convert deck definition to Card resources
	for entry_variant: Variant in enemy_deck_def:
		var entry: Dictionary = entry_variant if entry_variant is Dictionary else {}
		var catalog_id_variant: Variant = entry.get("catalog_id", "")
		var catalog_id: String = catalog_id_variant if catalog_id_variant is String else ""
		var count_variant: Variant = entry.get("count", 1)
		var count: int = int(count_variant) if (count_variant is int or count_variant is float) else 1

		if catalog_id == "":
			push_warning("EnemyDeckLoader: Empty catalog_id in enemy deck definition")
			continue

		# Create 'count' copies of this card
		for i: int in range(count):
			var card: Card = _create_card_from_catalog(catalog_id)
			if card:
				cards.append(card)
			else:
				push_warning("EnemyDeckLoader: Failed to create card: %s" % catalog_id)

	print("EnemyDeckLoader: Successfully loaded %d cards for enemy deck" % cards.size())
	return cards

## Create a Card resource from a catalog ID
static func _create_card_from_catalog(catalog_id: String) -> Card:
	# Get card catalog
	var catalog: Variant = _get_autoload("CardCatalog")
	if not catalog:
		push_error("EnemyDeckLoader: CardCatalog not found!")
		return null

	# Use CardCatalog.CreateCard() which creates cards dynamically
	# This works for all cards (units and spells) registered in the catalog
	if catalog is Object:
		var catalog_obj: Object = catalog
		var card_resource: Variant = catalog_obj.call("CreateCard", catalog_id)
		if card_resource and card_resource is Card:
			return card_resource
		else:
			push_error("EnemyDeckLoader: Failed to create card '%s' from catalog" % catalog_id)
			return null

	push_error("EnemyDeckLoader: CardCatalog is not a valid Object!")
	return null

## Helper to get autoload service by name (static context can't access autoloads directly)
static func _get_autoload(name: String) -> Variant:
	var main_loop: MainLoop = Engine.get_main_loop()
	if main_loop is SceneTree:
		var tree: SceneTree = main_loop
		if tree and tree.root:
			return tree.root.get_node_or_null(name)
	return null
