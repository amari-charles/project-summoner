extends Node
# CardCatalog is registered as autoload, no class_name needed

## Card Catalog - Central Database of All Card Definitions
##
## Single source of truth for all card data in the game.
## Provides methods to look up cards by ID, type, rarity, etc.
##
## Usage:
##   var card_def = CardCatalog.get_card("warrior")
##   var card = CardCatalog.create_card_resource("fireball")
##   var all_commons = CardCatalog.get_cards_by_rarity("common")

## Card data structure
## Each card is defined as a Dictionary with all its properties
var _catalog: Dictionary = {}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CardCatalog: Initializing...")
	_init_catalog()
	print("CardCatalog: Loaded %d cards" % _catalog.size())

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# Warrior - Basic melee unit
	_catalog["warrior"] = {
		# Identity
		"catalog_id": "warrior",
		"card_name": "Warrior",
		"description": "A stalwart melee fighter. Charges into battle with sword and shield.",
		"rarity": "common",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"mana_cost": 3,
		"cooldown": 2.0,

		# Summon properties
		"unit_scene_path": "res://scenes/units/warrior.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 100.0,
		"attack_damage": 15.0,
		"attack_range": 80.0,
		"attack_speed": 1.0,  # attacks per second
		"move_speed": 60.0,
		"aggro_radius": 300.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",  # TODO: Add card art

		# Metadata
		"tags": ["melee", "starter", "durable"],
		"unlock_condition": "default"
	}

	# Archer - Ranged attacker
	_catalog["archer"] = {
		"catalog_id": "archer",
		"card_name": "Archer",
		"description": "A skilled ranged attacker. Rains arrows from a distance.",
		"rarity": "common",

		"card_type": 0,  # SUMMON
		"mana_cost": 3,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/archer.tscn",
		"spawn_count": 1,

		"max_hp": 60.0,
		"attack_damage": 15.0,
		"attack_range": 200.0,
		"attack_speed": 0.8,
		"move_speed": 50.0,
		"aggro_radius": 250.0,
		"is_ranged": true,
		"projectile_scene_path": "res://scenes/units/projectile.tscn",

		"card_icon_path": "",
		"tags": ["ranged", "starter", "agile"],
		"unlock_condition": "default"
	}

	# Fireball - AOE damage spell
	_catalog["fireball"] = {
		"catalog_id": "fireball",
		"card_name": "Fireball",
		"description": "Unleash a devastating explosion of flame. Deals area damage to all enemies caught in the blast.",
		"rarity": "rare",

		"card_type": 1,  # Card.CardType.SPELL
		"mana_cost": 5,
		"cooldown": 2.0,

		"unit_scene_path": "",
		"spawn_count": 0,

		# Spell properties
		"spell_damage": 100.0,
		"spell_radius": 80.0,
		"spell_duration": 0.5,

		"card_icon_path": "",
		"tags": ["spell", "aoe", "damage"],
		"unlock_condition": "default",

		# Modifier system categories
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Wall - Defensive structure
	_catalog["wall"] = {
		"catalog_id": "wall",
		"card_name": "Wall",
		"description": "A sturdy barrier to block enemy advances. High health but no attack.",
		"rarity": "common",

		"card_type": 0,  # SUMMON (structure is just a unit with 0 move_speed)
		"mana_cost": 2,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/wall.tscn",
		"spawn_count": 1,

		"max_hp": 300.0,
		"attack_damage": 0.0,
		"attack_range": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0,
		"aggro_radius": 0.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		"card_icon_path": "",
		"tags": ["structure", "defensive", "barrier"],
		"unlock_condition": "default"
	}

	# Training Dummy - Tutorial target
	_catalog["training_dummy"] = {
		"catalog_id": "training_dummy",
		"card_name": "Training Dummy",
		"description": "A stationary training target. Doesn't move or attack.",
		"rarity": "common",

		"card_type": 0,  # SUMMON
		"mana_cost": 1,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/training_dummy.tscn",
		"spawn_count": 1,

		"max_hp": 100.0,
		"attack_damage": 0.0,
		"attack_range": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0,
		"aggro_radius": 0.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		"card_icon_path": "",
		"tags": ["tutorial", "training", "target"],
		"unlock_condition": "enemy_only"
	}

	# Neade - Heavy lancer
	_catalog["neade"] = {
		"catalog_id": "neade",
		"card_name": "Neade",
		"description": "A fierce lancer who strikes with devastating precision. Slow but powerful melee attacks.",
		"rarity": "rare",

		"card_type": 0,  # SUMMON
		"mana_cost": 4,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/neade.tscn",
		"spawn_count": 1,

		"max_hp": 120.0,
		"attack_damage": 28.0,
		"attack_range": 70.0,
		"attack_speed": 0.55,
		"move_speed": 55.0,
		"aggro_radius": 220.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		"card_icon_path": "",
		"tags": ["melee", "lancer", "heavy", "rare"],
		"unlock_condition": "locked"
	}

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get card definition by catalog_id
## Returns Dictionary or empty {} if not found
func get_card(catalog_id: String) -> Dictionary:
	if not _catalog.has(catalog_id):
		push_warning("CardCatalog: Card '%s' not found in catalog" % catalog_id)
		return {}
	return _catalog[catalog_id]

## Check if a card exists in the catalog
func has_card(catalog_id: String) -> bool:
	return _catalog.has(catalog_id)

## Get all card IDs
func get_all_card_ids() -> Array:
	return _catalog.keys()

## Get all card definitions
func list_all_cards() -> Array:
	return _catalog.values()

## Get cards filtered by rarity
func get_cards_by_rarity(rarity: String) -> Array:
	var results = []
	for card in _catalog.values():
		if card.get("rarity") == rarity:
			results.append(card)
	return results

## Get cards filtered by type (0 = SUMMON, 1 = SPELL)
func get_cards_by_type(card_type: int) -> Array:
	var results = []
	for card in _catalog.values():
		if card.get("card_type") == card_type:
			results.append(card)
	return results

## Get cards filtered by tag
func get_cards_by_tag(tag: String) -> Array:
	var results = []
	for card in _catalog.values():
		var tags = card.get("tags", [])
		if tag in tags:
			results.append(card)
	return results

## Get starter/default cards (unlock_condition = "default")
func get_starter_cards() -> Array:
	var results = []
	for card in _catalog.values():
		if card.get("unlock_condition") == "default":
			results.append(card)
	return results

## =============================================================================
## RUNTIME CARD GENERATION
## =============================================================================

## Create a Card resource from a catalog definition
## This generates a runtime Card object that can be played in-game
func create_card_resource(catalog_id: String) -> Resource:
	var card_def = get_card(catalog_id)
	if card_def.is_empty():
		push_error("CardCatalog: Cannot create card resource, '%s' not found" % catalog_id)
		return null

	# Load the Card class (assuming it's at scripts/cards/card.gd)
	var Card = load("res://scripts/cards/card.gd")
	var card = Card.new()

	# Set basic properties
	card.catalog_id = catalog_id
	card.card_name = card_def.get("card_name", "Unknown")
	card.card_type = card_def.get("card_type", 0)
	card.description = card_def.get("description", "")
	card.mana_cost = card_def.get("mana_cost", 0)
	card.cooldown = card_def.get("cooldown", 2.0)

	# Set type-specific properties
	if card.card_type == 0:  # SUMMON
		var unit_scene_path = card_def.get("unit_scene_path", "")
		if unit_scene_path != "":
			card.unit_scene = load(unit_scene_path)
		card.spawn_count = card_def.get("spawn_count", 1)
	elif card.card_type == 1:  # SPELL
		card.spell_damage = card_def.get("spell_damage", 0.0)
		card.spell_radius = card_def.get("spell_radius", 0.0)
		card.spell_duration = card_def.get("spell_duration", 0.0)

	# Set icon if available
	var icon_path = card_def.get("card_icon_path", "")
	if icon_path != "":
		card.card_icon = load(icon_path)

	return card

## =============================================================================
## UTILITY METHODS
## =============================================================================

## Get card display name (for UI)
func get_card_name(catalog_id: String) -> String:
	var card = get_card(catalog_id)
	return card.get("card_name", catalog_id)

## Get card rarity (for UI coloring, etc.)
func get_card_rarity(catalog_id: String) -> String:
	var card = get_card(catalog_id)
	return card.get("rarity", "common")

## Get card mana cost (for deck building validation)
func get_card_cost(catalog_id: String) -> int:
	var card = get_card(catalog_id)
	return card.get("mana_cost", 0)

## Print catalog summary (debug)
func print_catalog_summary() -> void:
	print("\n=== CARD CATALOG SUMMARY ===")
	print("Total Cards: %d" % _catalog.size())

	var by_rarity = {}
	var by_type = {"summon": 0, "spell": 0}

	for card in _catalog.values():
		# Count by rarity
		var rarity = card.get("rarity", "common")
		if not by_rarity.has(rarity):
			by_rarity[rarity] = 0
		by_rarity[rarity] += 1

		# Count by type
		var type = card.get("card_type", 0)
		if type == 0:
			by_type["summon"] += 1
		else:
			by_type["spell"] += 1

	print("\nBy Rarity:")
	for rarity in by_rarity:
		print("  %s: %d" % [rarity, by_rarity[rarity]])

	print("\nBy Type:")
	print("  Summon: %d" % by_type["summon"])
	print("  Spell: %d" % by_type["spell"])

	print("\nStarter Cards:")
	for card in get_starter_cards():
		print("  - %s (%s, %d mana)" % [card.card_name, card.rarity, card.mana_cost])

	print("===========================\n")
