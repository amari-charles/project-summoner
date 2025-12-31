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

## Preload ID constant classes for use in catalog definitions
const ProjectileIDsScript = preload("res://scripts/data/projectile_ids.gd")

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CardCatalog: Initializing...")
	_init_catalog()
	print("CardCatalog: Loaded %d cards" % _catalog.size())
	_validate_card_ids_sync()

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# Fireball - AOE damage spell
	_catalog["fireball"] = {
		"catalog_id": "fireball",
		"card_name": "Fireball",
		"description": "Unleash a devastating explosion of flame. Deals area damage to all enemies caught in the blast.",
		"rarity": RarityIDs.RARE,

		"card_type": Card.CardType.SPELL,
		"mana_cost": 5,
		"cooldown": 2.0,
		"summon_time": 0.0,  # Instant cast spell

		"unit_scene_path": "",
		"spawn_count": 0,

		# Spell properties
		"spell_damage": 100.0,
		"spell_radius": 10.0,  # Passed to VFX for accurate indicator sizing
		"spell_duration": 0.5,
		"projectile_id": ProjectileIDsScript.FIREBALL,  # Use projectile system for proper impact timing
		"spell_vfx": VFXIDs.FIREBALL_SPELL,

		"card_icon_path": "",
		"tags": ["spell", "aoe", "damage"],
		"unlock_condition": "default",

		# Modifier system categories
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Neade - Heavy lancer
	_catalog["neade"] = {
		"catalog_id": "neade",
		"card_name": "Neade",
		"description": "A fierce lancer who strikes with devastating precision. Slow but powerful melee attacks.",
		"rarity": RarityIDs.RARE,

		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,  # For icon display
		"mana_cost": 4,
		"cooldown": 2.0,
		"summon_time": 1.0,  # Medium unit (3-4 mana)

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
		{"rarity": RarityIDs.RARE})

	_add_slime_card("purple", "large", ElementTypes.POISON,
		"A huge, toxic purple slime. Extremely durable with powerful poison-infused attacks.",
		{"rarity": RarityIDs.RARE})

	_add_slime_card("red", "large", ElementTypes.FIRE,
		"An enormous crimson slime. The largest of its kind, boasting incredible strength and resilience.",
		{"rarity": RarityIDs.RARE})

	# Demon Imp - Flying melee attacker
	_catalog["demon_imp"] = {
		# Identity
		"catalog_id": "demon_imp",
		"card_name": "Demon Imp",
		"description": "A swift flying demon. Dives from above to strike ground forces while evading melee attackers.",
		"rarity": "uncommon",

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,  # For icon display (combat type, not movement type)
		"mana_cost": 4,
		"cooldown": 2.0,
		"summon_time": 1.0,  # Medium unit (3-4 mana)

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

	# Storm Cloud - Flying ranged attacker with lightning
	_catalog["storm_cloud"] = {
		# Identity
		"catalog_id": "storm_cloud",
		"card_name": "Storm Cloud",
		"description": "A hovering tempest that strikes foes with lightning. Floats above the battlefield, raining destruction on ground forces.",
		"rarity": RarityIDs.RARE,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.RANGED,
		"mana_cost": 4,
		"cooldown": 2.0,
		"summon_time": 1.0,  # Medium unit (4 mana)

		# Summon properties
		"unit_scene_path": "res://scenes/units/storm_cloud_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 60.0,
		"attack_damage": 25.0,
		"attack_range": 12.0,
		"attack_speed": 0.7,
		"move_speed": 2.5,
		"aggro_radius": 20.0,
		"is_ranged": true,
		"projectile_scene_path": "",  # Uses VFX-based attack (lightning)

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["flying", "ranged", "lightning", "magic"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.LIGHTNING
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
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 2,
		"cooldown": 1.5,
		"summon_time": 0.5,  # Cheap unit (2 mana)

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
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.RANGED,
		"mana_cost": 2,
		"cooldown": 1.5,
		"summon_time": 0.5,  # Cheap unit (2 mana)

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
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 3,
		"cooldown": 2.0,
		"summon_time": 1.0,  # Medium unit (3-4 mana)

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
		"rarity": RarityIDs.RARE,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 5,
		"cooldown": 3.0,
		"summon_time": 2.0,  # Expensive unit (5+ mana)

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

	# Fire Elemental - Floating fire spirit
	# Visual: Uses bobbing animation (enable_bobbing=true), Lunge attack style
	# Sprite scale 0.26 calculated for ~960px sprite (BASE_VIEWPORT_SIZE 250 / 960 ≈ 0.26)
	_catalog["fire_elemental"] = {
		# Identity
		"catalog_id": "fire_elemental",
		"card_name": "Fire Elemental",
		"description": "A floating spirit of pure flame. Hovers across the battlefield, burning all in its path.",
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 3,
		"cooldown": 2.0,
		"summon_time": 1.0,  # Medium unit (3-4 mana)

		# Summon properties
		"unit_scene_path": "res://scenes/units/fire_elemental_3d.tscn",
		"spawn_count": 1,

		# Unit stats (centralized here)
		"max_hp": 60.0,
		"attack_damage": 12.0,
		"attack_range": 2.0,
		"attack_speed": 1.2,
		"move_speed": 3.5,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "floating", "spirit"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Fire Titan - Giant tank version of Fire Elemental
	# Visual: 4x scaled fire elemental using viewport_scale system
	# Role: Heavy tank with high HP, moderate damage, slow movement
	#
	# Scene values derived from fire_elemental_3d.tscn × 4 (viewport_scale):
	#   - collision radius: 0.5 × 4 = 2.0
	#   - collision height: 1.6 × 4 = 6.4
	#   - collision Y pos:  0.8 × 4 = 3.2 (half of height)
	#   - projectile Y:     1.2 × 4 = 4.8
	#   - sprite_scale:     0.26 × 4 = 1.04
	#   - sprite_feet_offset_pixels: 40.0 (unchanged - pixel offset within texture)
	_catalog["fire_titan"] = {
		# Identity
		"catalog_id": "fire_titan",
		"card_name": "Fire Titan",
		"description": "A colossal spirit of ancient flame. Towers over the battlefield, absorbing damage while scorching all who approach.",
		"rarity": RarityIDs.EPIC,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 7,
		"cooldown": 3.0,
		"summon_time": 2.0,  # Expensive unit (5+ mana)

		# Summon properties
		"unit_scene_path": "res://scenes/units/fire_titan_3d.tscn",
		"spawn_count": 1,

		# Unit stats - Tank: high HP, moderate damage, slow
		"max_hp": 300.0,
		"attack_damage": 20.0,
		"attack_range": 3.0,
		"attack_speed": 0.8,
		"move_speed": 2.0,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "floating", "spirit", "tank", "giant"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.FIRE
		}
	}

	# Fire Elemental Swarm - Spawns 12 weaker fire elementals in 2 rows
	# Role: Swarm tactics - overwhelm with numbers
	_catalog["fire_elemental_swarm"] = {
		# Identity
		"catalog_id": "fire_elemental_swarm",
		"card_name": "Fire Swarm",
		"description": "Unleash a horde of flame spirits. Twelve smaller fire elementals surge forth to overwhelm the enemy.",
		"rarity": RarityIDs.RARE,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 7,
		"cooldown": 4.0,
		"summon_time": 2.5,  # Swarm card (extra time for multiple units)

		# Summon properties - uses same scene but spawns 12
		"unit_scene_path": "res://scenes/units/fire_elemental_3d.tscn",
		"spawn_count": 12,

		# Unit stats - slightly weaker than base fire elemental
		"max_hp": 45.0,
		"attack_damage": 9.0,
		"attack_range": 2.0,
		"attack_speed": 1.2,
		"move_speed": 3.5,
		"aggro_radius": 20.0,
		"is_ranged": false,
		"projectile_scene_path": "",

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["melee", "fire", "floating", "spirit", "swarm"],
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
		"rarity": RarityIDs.RARE,

		# Card properties
		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": 4,
		"cooldown": 2.5,
		"summon_time": 1.0,  # Medium unit (3-4 mana)

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

	# =========================================================================
	# TACTICAL COMMAND SPELLS
	# =========================================================================

	# Rally - Simple movement command
	_catalog["rally"] = {
		# Identity
		"catalog_id": "rally",
		"card_name": "Rally",
		"description": "Command nearby units to move to a target location and defend that zone until enemies are cleared.",
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SPELL,
		"mana_cost": 0,
		"cooldown": 1.0,
		"summon_time": 0.0,  # Instant cast spell

		# Spell properties (not a traditional damage spell)
		"unit_scene_path": "",
		"spawn_count": 0,
		"spell_damage": 0.0,
		"spell_radius": 0.0,
		"spell_duration": 0.0,
		"projectile_id": "",
		"spell_vfx": "",

		# Tactical command properties (custom handling)
		"command_type": "rally",
		"selection_radius": 8.0,  # Radius to select units

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["spell", "command", "tactical", "movement"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.NEUTRAL
		}
	}

	# Guard - Formation command
	_catalog["guard"] = {
		# Identity
		"catalog_id": "guard",
		"card_name": "Guard",
		"description": "Command nearby units to form a defensive formation for 25 seconds. Melee units protect ranged units in the back line.",
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SPELL,
		"mana_cost": 0,
		"cooldown": 1.0,
		"summon_time": 0.0,  # Instant cast spell

		# Spell properties (not a traditional damage spell)
		"unit_scene_path": "",
		"spawn_count": 0,
		"spell_damage": 0.0,
		"spell_radius": 0.0,
		"spell_duration": 0.0,
		"projectile_id": "",
		"spell_vfx": "",

		# Tactical command properties (custom handling)
		"command_type": "guard",
		"selection_radius": 8.0,  # Radius to select units
		"formation_duration": 25.0,  # Duration of guard mode

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["spell", "command", "tactical", "formation"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.NEUTRAL
		}
	}

	# Charge - Focus-fire command
	_catalog["charge"] = {
		# Identity
		"catalog_id": "charge",
		"card_name": "Charge",
		"description": "Command nearby units to launch a coordinated attack on the closest enemy (unit, structure, or base) to the target location for 30 seconds.",
		"rarity": RarityIDs.COMMON,

		# Card properties
		"card_type": Card.CardType.SPELL,
		"mana_cost": 0,
		"cooldown": 1.0,
		"summon_time": 0.0,  # Instant cast spell

		# Spell properties (not a traditional damage spell)
		"unit_scene_path": "",
		"spawn_count": 0,
		"spell_damage": 0.0,
		"spell_radius": 0.0,
		"spell_duration": 0.0,
		"projectile_id": "",
		"spell_vfx": "",

		# Tactical command properties (custom handling)
		"command_type": "charge",
		"selection_radius": 8.0,  # Radius to select units

		# Visual
		"card_icon_path": "",

		# Metadata
		"tags": ["spell", "command", "tactical", "focus_fire"],
		"unlock_condition": "default",

		# Elemental affinity
		"categories": {
			"elemental_affinity": ElementTypes.NEUTRAL
		}
	}

## Factory method for creating slime cards with size templates
func _add_slime_card(color: String, size: String, element: ElementTypes.Element, description: String, overrides: Dictionary = {}) -> void:
	# Size templates with default stats
	var size_templates: Dictionary = {
		"small": {
			"max_hp": 50.0,
			"attack_damage": 8.0,
			"attack_range": 1.5,
			"attack_speed": 1.5,
			"move_speed": 2.5,
			"aggro_radius": 18.0,
			"mana_cost": 2,
			"cooldown": 1.5,
			"summon_time": 0.5,  # Cheap unit (2 mana)
			"tags": ["melee", "swarm", "fast"],
			"rarity": RarityIDs.COMMON
		},
		"medium": {
			"max_hp": 100.0,
			"attack_damage": 15.0,
			"attack_range": 1.5,
			"attack_speed": 1.0,
			"move_speed": 2.0,
			"aggro_radius": 18.0,
			"mana_cost": 3,
			"cooldown": 2.0,
			"summon_time": 1.0,  # Medium unit (3-4 mana)
			"tags": ["melee", "balanced"],
			"rarity": RarityIDs.COMMON
		},
		"large": {
			"max_hp": 180.0,
			"attack_damage": 25.0,
			"attack_range": 1.5,
			"attack_speed": 0.8,
			"move_speed": 1.5,
			"aggro_radius": 18.0,
			"mana_cost": 5,
			"cooldown": 2.5,
			"summon_time": 2.0,  # Expensive unit (5+ mana)
			"tags": ["melee", "tank", "heavy"],
			"rarity": RarityIDs.RARE
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

		"card_type": Card.CardType.SUMMON,
		"unit_type": UnitTypeIDs.MELEE,
		"mana_cost": overrides.get("mana_cost", template.mana_cost),
		"cooldown": overrides.get("cooldown", template.cooldown),
		"summon_time": overrides.get("summon_time", template.summon_time),

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
## Accepts StringName (preferred) or String for backward compatibility
func get_card(catalog_id: StringName) -> Dictionary:
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
## Accepts StringName (preferred) or String for backward compatibility
func has_card(catalog_id: StringName) -> bool:
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

## Get cards filtered by type (Card.CardType.SUMMON or Card.CardType.SPELL)
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
## Accepts StringName (preferred) or String for backward compatibility
func create_card_resource(catalog_id: StringName) -> Resource:
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
	card.summon_time = card_def.get("summon_time", 1.0)

	# Set type-specific properties
	if card.card_type == Card.CardType.SUMMON:
		var unit_scene_path: String = card_def.get("unit_scene_path", "")
		if unit_scene_path != "":
			var scene: PackedScene = load(unit_scene_path)
			if not scene:
				push_error("CardCatalog: Failed to load unit scene '%s' for card '%s'. Check if scene file exists and is valid." % [unit_scene_path, catalog_id])
				assert(false, "Unit scene must load successfully! Fix scene file or path.")
				return null  # Unreachable in debug builds
			card.unit_scene = scene
		card.spawn_count = card_def.get("spawn_count", 1)
	elif card.card_type == Card.CardType.SPELL:
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
func get_card_rarity(catalog_id: String) -> StringName:
	var card: Dictionary = get_card(catalog_id)
	return card.get("rarity", RarityIDs.COMMON)

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
		var rarity: StringName = card.get("rarity", RarityIDs.COMMON)
		if not by_rarity.has(rarity):
			by_rarity[rarity] = 0
		by_rarity[rarity] += 1

		# Count by type
		var type: int = card.get("card_type", Card.CardType.SUMMON)
		if type == Card.CardType.SUMMON:
			by_type["summon"] += 1
		else:
			by_type["spell"] += 1

	print("\nBy Rarity:")
	for rarity: StringName in by_rarity:
		print("  %s: %d" % [rarity, by_rarity[rarity]])

	print("\nBy Type:")
	print("  Summon: %d" % by_type["summon"])
	print("  Spell: %d" % by_type["spell"])

	print("\nStarter Cards:")
	for card: Dictionary in get_starter_cards():
		print("  - %s (%s, %d mana)" % [card.card_name, card.rarity, card.mana_cost])

## =============================================================================
## VALIDATION
## =============================================================================

## Validate that CardIDs constants match catalog entries
## Called in _ready() to catch desync issues at startup
func _validate_card_ids_sync() -> void:
	# Get all constant names from CardIDs
	var card_ids_script: GDScript = load("res://scripts/data/card_ids.gd")
	var constants: Dictionary = card_ids_script.get_script_constant_map()

	var missing_in_catalog: Array[String] = []
	var missing_in_card_ids: Array[String] = []

	# Check: All CardIDs constants exist in catalog
	for const_name: String in constants.keys():
		var id_value: Variant = constants[const_name]
		if id_value is StringName or id_value is String:
			var id_string: String = str(id_value)
			if not _catalog.has(id_string):
				missing_in_catalog.append("%s = '%s'" % [const_name, id_string])

	# Check: All catalog cards have corresponding CardIDs constant
	for catalog_id: String in _catalog.keys():
		var found: bool = false
		for const_value: Variant in constants.values():
			if str(const_value) == catalog_id:
				found = true
				break
		if not found:
			missing_in_card_ids.append(catalog_id)

	# Report issues
	if missing_in_catalog.size() > 0:
		push_error("CardCatalog: CardIDs constants reference non-existent cards:")
		for missing: String in missing_in_catalog:
			push_error("  - CardIDs.%s" % missing)
		assert(false, "Fix CardIDs constants or add missing cards to catalog!")

	if missing_in_card_ids.size() > 0:
		push_warning("CardCatalog: Catalog has cards without CardIDs constants (test/mod cards?):")
		for missing: String in missing_in_card_ids:
			push_warning("  - '%s' (no constant in CardIDs)" % missing)
		print("  This is OK for test cards, but official cards should have CardIDs constants.")

	if missing_in_catalog.size() == 0 and missing_in_card_ids.size() == 0:
		print("CardCatalog: ✓ CardIDs validation passed - all %d constants match catalog" % constants.size())

	print("===========================\n")
