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

## Cached Card script for efficient resource creation
const CardScript = preload("res://scripts/cards/card.gd")

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
		"spell_radius": 10.0,  # Passed to VFX for accurate indicator sizing
		"spell_duration": 0.5,
		"spell_vfx": "fireball_spell",

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
		"unit_type": "structure",  # For icon display
		"mana_cost": 2,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/wall_3d.tscn",
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
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.EARTH
		}
	}

	# Neade - Heavy lancer
	_catalog["neade"] = {
		"catalog_id": "neade",
		"card_name": "Neade",
		"description": "A fierce lancer who strikes with devastating precision. Slow but powerful melee attacks.",
		"rarity": "rare",

		"card_type": 0,  # SUMMON
		"unit_type": "melee",  # For icon display
		"mana_cost": 4,
		"cooldown": 2.0,

		"unit_scene_path": "res://scenes/units/neade_3d.tscn",
		"spawn_count": 1,

		"max_hp": 9999.0,
		"attack_damage": 28.0,
		"attack_range": 2.0,
		"attack_speed": 0.55,
		"move_speed": 3.0,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		"card_icon_path": "",
		"tags": ["melee", "lancer", "heavy", "rare"],
		"unlock_condition": "locked",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.LIGHTNING
		}
	}

	# Slime cards - Using factory pattern to reduce duplication
	_add_slime_card("green", "small", ElementTypes.EARTH,
		"A small, speedy slime. Low health but quick attacks. Great for overwhelming enemies with numbers.",
		{"attack_damage": 2.0})  # Reduced for tutorial difficulty

	_add_slime_card("pink", "small", ElementTypes.LIFE,
		"A cheerful pink slime. Fast and eager to help, but fragile.")

	_add_slime_card("violet", "small", ElementTypes.SHADOW,
		"A mysterious violet slime. Quick and elusive.")

	_add_slime_card("blue", "medium", ElementTypes.WATER,
		"A well-rounded slime of medium size. Balanced stats make it reliable in any situation.")

	_add_slime_card("orange", "medium", ElementTypes.FIRE,
		"A fiery orange slime of medium size. Moderate health and attack with steady speed.")

	_add_slime_card("yellow", "medium", ElementTypes.LIGHTNING,
		"A bright yellow slime of medium size. Energetic and dependable.")

	_add_slime_card("grey", "large", ElementTypes.EARTH,
		"A massive grey slime. Slow but incredibly durable with devastating attacks.",
		{"rarity": "rare"})

	_add_slime_card("purple", "large", ElementTypes.POISON,
		"A huge, toxic purple slime. Extremely durable with powerful poison-infused attacks.",
		{"rarity": "rare"})

	_add_slime_card("red", "large", ElementTypes.FIRE,
		"An enormous crimson slime. The largest of its kind, boasting incredible strength and resilience.",
		{"rarity": "rare"})

	# Demon Imp - Flying melee attacker
	_catalog["demon_imp"] = {
		# Identity
		"catalog_id": "demon_imp",
		"card_name": "Demon Imp",
		"description": "A swift flying demon. Dives from above to strike ground forces while evading melee attackers.",
		"rarity": "uncommon",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "melee",  # For icon display (combat type, not movement type)
		"mana_cost": 4,
		"cooldown": 2.0,

		# Summon properties
		"unit_scene_path": "res://scenes/units/demon_imp_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 70.0,
		"attack_damage": 12.0,
		"attack_range": 2.0,
		"attack_speed": 1.2,
		"move_speed": 4.5,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",  # TODO: Add card art

		# Metadata
		"tags": ["flying", "melee", "fast", "agile"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.SHADOW
		}
	}

	# =========================================================================
	# FIRE ELEMENT UNITS
	# =========================================================================

	# Fire Recruit - Cheap melee soldier
	_catalog["fire_recruit"] = {
		# Identity
		"catalog_id": "fire_recruit",
		"card_name": "Fire Recruit",
		"description": "A basic fire soldier. Cheap and eager to fight, establishing early pressure on the battlefield.",
		"rarity": "common",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "melee",
		"mana_cost": 2,
		"cooldown": 1.5,

		# Summon properties
		"unit_scene_path": "res://scenes/units/fire_recruit_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 50.0,
		"attack_damage": 10.0,
		"attack_range": 2.0,
		"attack_speed": 1.0,
		"move_speed": 3.0,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "cheap", "starter"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Ember Slinger - Fragile ranged attacker
	_catalog["ember_slinger"] = {
		# Identity
		"catalog_id": "ember_slinger",
		"card_name": "Ember Slinger",
		"description": "A fragile ranged attacker. Flings burning embers for steady chip damage from a safe distance.",
		"rarity": "common",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "ranged",
		"mana_cost": 2,
		"cooldown": 1.5,

		# Summon properties
		"unit_scene_path": "res://scenes/units/ember_slinger_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 40.0,
		"attack_damage": 8.0,
		"attack_range": 10.0,
		"attack_speed": 0.8,
		"move_speed": 3.0,
		"aggro_radius": 20.0,
		"is_ranged": true,
		"projectile_scene_path": "",  # Projectile defined in unit scene (projectile_id: "ember")

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["ranged", "fire", "cheap", "fragile"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Blaze Rider - Fast charger
	_catalog["blaze_rider"] = {
		# Identity
		"catalog_id": "blaze_rider",
		"card_name": "Blaze Rider",
		"description": "A swift cavalry unit wreathed in flames. Charges across the battlefield to deliver explosive burst damage.",
		"rarity": "common",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "melee",
		"mana_cost": 3,
		"cooldown": 2.0,

		# Summon properties
		"unit_scene_path": "res://scenes/units/blaze_rider_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 80.0,
		"attack_damage": 15.0,
		"attack_range": 2.0,
		"attack_speed": 1.2,
		"move_speed": 5.0,  # Fast charger - high movement speed (but not crazy fast!)
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "fast", "charger"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Ash Vanguard - Explosive tank
	_catalog["ash_vanguard"] = {
		# Identity
		"catalog_id": "ash_vanguard",
		"card_name": "Ash Vanguard",
		"description": "A heavily armored warrior that explodes on death, dealing AoE damage to nearby enemies.",
		"rarity": "rare",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "melee",
		"mana_cost": 5,
		"cooldown": 3.0,

		# Summon properties
		"unit_scene_path": "res://scenes/units/ash_vanguard_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 150.0,
		"attack_damage": 25.0,
		"attack_range": 1.5,
		"attack_speed": 1.2,
		"move_speed": 2.0,  # Slow tank - slower than normal units
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "tank", "explosive", "death_explosion"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Ember Guard - Damage aura
	_catalog["ember_guard"] = {
		# Identity
		"catalog_id": "ember_guard",
		"card_name": "Ember Guard",
		"description": "A defensive unit that burns nearby enemies with a constant damage aura.",
		"rarity": "rare",

		# Card properties
		"card_type": 0,  # Card.CardType.SUMMON
		"unit_type": "melee",
		"mana_cost": 4,
		"cooldown": 2.5,

		# Summon properties
		"unit_scene_path": "res://scenes/units/ember_guard_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 120.0,
		"attack_damage": 12.0,
		"attack_range": 1.5,  # Proper melee range
		"attack_speed": 1.0,
		"move_speed": 1.5,  # Very slow defensive unit (slower than normal 3.0)
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "defensive", "aura", "damage_over_time"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

## Factory method for creating slime cards with size templates
func _add_slime_card(color: String, size: String, element: ElementTypes.Element, description: String, overrides: Dictionary = {}) -> void:
	# Size templates with default stats
	var size_templates: Dictionary = {
		"small": {
			"max_hp": 50.0,
			"attack_damage": 8.0,
			"attack_range": 60.0,
			"attack_speed": 1.5,
			"move_speed": 75.0,
			"aggro_radius": 250.0,
			"mana_cost": 2,
			"cooldown": 1.5,
			"tags": ["melee", "swarm", "fast"],
			"rarity": "common"
		},
		"medium": {
			"max_hp": 100.0,
			"attack_damage": 15.0,
			"attack_range": 80.0,
			"attack_speed": 1.0,
			"move_speed": 60.0,
			"aggro_radius": 260.0,
			"mana_cost": 3,
			"cooldown": 2.0,
			"tags": ["melee", "balanced"],
			"rarity": "common"
		},
		"large": {
			"max_hp": 180.0,
			"attack_damage": 25.0,
			"attack_range": 100.0,
			"attack_speed": 0.8,
			"move_speed": 50.0,
			"aggro_radius": 280.0,
			"mana_cost": 5,
			"cooldown": 2.5,
			"tags": ["melee", "tank", "heavy"],
			"rarity": "rare"
		}
	}

	# Validate size parameter
	var template_variant: Variant = size_templates.get(size)
	if not template_variant:
		push_error("CardCatalog: Invalid slime size '%s' for color '%s'. Must be small/medium/large" % [size, color])
		return

	# Type narrow to Dictionary for safe property access
	var template: Dictionary = template_variant

	var catalog_id: String = "slime_%s" % color

	# Build card definition from template + overrides
	_catalog[catalog_id] = {
		"catalog_id": catalog_id,
		"card_name": "%s Slime" % color.capitalize(),
		"description": description,
		"rarity": overrides.get("rarity", template.rarity),

		"card_type": 0,  # SUMMON
		"unit_type": "melee",
		"mana_cost": overrides.get("mana_cost", template.mana_cost),
		"cooldown": overrides.get("cooldown", template.cooldown),

		"unit_scene_path": "res://scenes/units/slime_%s_3d.tscn" % color,
		"spawn_count": 1,

		"max_hp": overrides.get("max_hp", template.max_hp),
		"attack_damage": overrides.get("attack_damage", template.attack_damage),
		"attack_range": overrides.get("attack_range", template.attack_range),
		"attack_speed": overrides.get("attack_speed", template.attack_speed),
		"move_speed": overrides.get("move_speed", template.move_speed),
		"aggro_radius": overrides.get("aggro_radius", template.aggro_radius),
		"is_ranged": false,
		"projectile_scene_path": "",

		"card_icon_path": "",
		"tags": overrides.get("tags", template.tags),
		"unlock_condition": "default",

		"categories": {
			"elemental_affinity": element
		}
	}


## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get card definition by catalog_id
## Returns Dictionary or empty {} if not found
## Returns a shallow duplicate to protect catalog data from external modifications
func get_card(catalog_id: String) -> Dictionary:
	if not _catalog.has(catalog_id):
		push_error("CardCatalog: Card '%s' not found in catalog. Fix typo or register card." % catalog_id)
		assert(false, "Card must exist in catalog!")
		var empty: Dictionary = {}
		return empty  # Unreachable in debug builds
	# Return shallow duplicate - preserves Element object references while preventing corruption
	var card_dict_variant: Variant = _catalog[catalog_id]
	if not card_dict_variant is Dictionary:
		push_error("CardCatalog: _catalog[%s] is not a Dictionary - catalog corrupted!" % catalog_id)
		assert(false, "Catalog data corruption detected!")
		var empty: Dictionary = {}
		return empty  # Unreachable in debug builds
	var card_dict: Dictionary = card_dict_variant
	return card_dict.duplicate(false)

## Check if a card exists in the catalog
func has_card(catalog_id: String) -> bool:
	return _catalog.has(catalog_id)

## Get all card IDs
func get_all_card_ids() -> Array[String]:
	var result: Array[String] = []
	result.assign(_catalog.keys())
	return result

## Get all card definitions
func list_all_cards() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	result.assign(_catalog.values())
	return result

## Get cards filtered by rarity
func get_cards_by_rarity(rarity: String) -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	for card: Dictionary in _catalog.values():
		if card.get("rarity") == rarity:
			results.append(card)
	return results

## Get cards filtered by type (0 = SUMMON, 1 = SPELL)
func get_cards_by_type(card_type: int) -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	for card: Dictionary in _catalog.values():
		if card.get("card_type") == card_type:
			results.append(card)
	return results

## Get cards filtered by tag
func get_cards_by_tag(tag: String) -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	for card: Dictionary in _catalog.values():
		var tags: Array = card.get("tags", [])
		if tag in tags:
			results.append(card)
	return results

## Get starter/default cards (unlock_condition = "default")
func get_starter_cards() -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	for card: Dictionary in _catalog.values():
		if card.get("unlock_condition") == "default":
			results.append(card)
	return results

## =============================================================================
## RUNTIME CARD GENERATION
## =============================================================================

## Create a Card resource from a catalog definition
## This generates a runtime Card object that can be played in-game
func create_card_resource(catalog_id: String) -> Resource:
	var card_def: Dictionary = get_card(catalog_id)
	if card_def.is_empty():
		push_error("CardCatalog: Cannot create card resource, '%s' not found" % catalog_id)
		assert(false, "Card must exist in catalog! Fix card registration or typo in catalog_id.")
		return null  # Unreachable in debug builds

	# Create Card instance from preloaded script
	# Type narrow to Card for safe property access
	var card: Card = CardScript.new()

	# Set basic properties
	card.catalog_id = catalog_id
	card.card_name = card_def.get("card_name", "Unknown")
	card.card_type = card_def.get("card_type", 0)
	card.description = card_def.get("description", "")
	card.mana_cost = card_def.get("mana_cost", 0)
	card.cooldown = card_def.get("cooldown", 2.0)

	# Set type-specific properties
	if card.card_type == 0:  # SUMMON
		var unit_scene_path: String = card_def.get("unit_scene_path", "")
		if unit_scene_path != "":
			var scene: PackedScene = load(unit_scene_path)
			if not scene:
				push_error("CardCatalog: Failed to load unit scene '%s' for card '%s'. Check if scene file exists and is valid." % [unit_scene_path, catalog_id])
				assert(false, "Unit scene must load successfully! Fix scene file or path.")
				return null  # Unreachable in debug builds
			card.unit_scene = scene
		card.spawn_count = card_def.get("spawn_count", 1)
	elif card.card_type == 1:  # SPELL
		card.spell_damage = card_def.get("spell_damage", 0.0)
		card.spell_radius = card_def.get("spell_radius", 0.0)
		card.spell_duration = card_def.get("spell_duration", 0.0)
		card.projectile_id = card_def.get("projectile_id", "")
		card.spell_vfx = card_def.get("spell_vfx", "")

	# Set icon if available
	var icon_path: String = card_def.get("card_icon_path", "")
	if icon_path != "":
		card.card_icon = load(icon_path)

	return card

## =============================================================================
## UTILITY METHODS
## =============================================================================

## Get card display name (for UI)
func get_card_name(catalog_id: String) -> String:
	var card: Dictionary = get_card(catalog_id)
	return card.get("card_name", catalog_id)

## Get card rarity (for UI coloring, etc.)
func get_card_rarity(catalog_id: String) -> String:
	var card: Dictionary = get_card(catalog_id)
	return card.get("rarity", "common")

## Get card mana cost (for deck building validation)
func get_card_cost(catalog_id: String) -> int:
	var card: Dictionary = get_card(catalog_id)
	return card.get("mana_cost", 0)

## Print catalog summary (debug)
func print_catalog_summary() -> void:
	print("\n=== CARD CATALOG SUMMARY ===")
	print("Total Cards: %d" % _catalog.size())

	var by_rarity: Dictionary = {}
	var by_type: Dictionary = {"summon": 0, "spell": 0}

	for card: Dictionary in _catalog.values():
		# Count by rarity
		var rarity: String = card.get("rarity", "common")
		if not by_rarity.has(rarity):
			by_rarity[rarity] = 0
		by_rarity[rarity] += 1

		# Count by type
		var type: int = card.get("card_type", 0)
		if type == 0:
			by_type["summon"] += 1
		else:
			by_type["spell"] += 1

	print("\nBy Rarity:")
	for rarity: String in by_rarity:
		print("  %s: %d" % [rarity, by_rarity[rarity]])

	print("\nBy Type:")
	print("  Summon: %d" % by_type["summon"])
	print("  Spell: %d" % by_type["spell"])

	print("\nStarter Cards:")
	for card: Dictionary in get_starter_cards():
		print("  - %s (%s, %d mana)" % [card.card_name, card.rarity, card.mana_cost])

	print("===========================\n")
