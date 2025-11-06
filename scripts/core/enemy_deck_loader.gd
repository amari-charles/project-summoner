extends Node
class_name EnemyDeckLoader

## EnemyDeckLoader - Loads enemy decks from campaign battle definitions
##
## Static utility class that converts enemy deck definitions to Card resources for battle.
## Unlike DeckLoader (which loads from player's collection), this directly creates
## Card resources from catalog IDs.

## Load enemy deck for the current campaign battle
static func load_enemy_deck_for_battle() -> Array[Card]:
	var cards: Array[Card] = []

	# Get campaign service
	var campaign = _get_service("/root/Campaign")
	if not campaign:
		push_error("EnemyDeckLoader: Campaign service not found!")
		return cards

	# Get profile to find current battle
	var profile_repo = _get_service("/root/ProfileRepo")
	if not profile_repo:
		push_error("EnemyDeckLoader: ProfileRepo not found!")
		return cards

	var profile = profile_repo.get_active_profile()
	if profile.is_empty():
		push_error("EnemyDeckLoader: No active profile!")
		return cards

	# Get current battle ID
	var battle_id = profile.get("campaign_progress", {}).get("current_battle", "")
	if battle_id == "":
		push_warning("EnemyDeckLoader: No current battle set in profile!")
		return cards

	print("EnemyDeckLoader: Loading enemy deck for battle '%s'" % battle_id)

	# Load the deck for this battle
	return load_deck_for_battle(battle_id)

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
	var card = load(card_path) as Card

	if not card:
		push_error("EnemyDeckLoader: Failed to load card resource: %s" % card_path)
		return null

	return card

## Helper to get autoload service safely
static func _get_service(path: String):
	var tree = Engine.get_main_loop() as SceneTree
	if tree and tree.root:
		return tree.root.get_node_or_null(path)
	return null
