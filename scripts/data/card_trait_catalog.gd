extends Node
class_name CardTraitCatalog

## Card Trait Catalog - Defines trait choices for each card at each level
##
## Each card can have 2-3 trait choices per level (levels 2-10).
## Traits should be meaningful choices that enhance the card's identity,
## not just raw stat increases.
##
## Usage:
##   var traits = CardTraitCatalog.get_traits_for_level("fire_wisp", 2)
##   var trait = CardTraitCatalog.get_trait("fire_wisp", "fire_wisp_hardy_2")

## =============================================================================
## TRAIT DEFINITIONS
## =============================================================================

## Trait data structure:
## - id: Unique identifier (format: {catalog_id}_{name}_{level})
## - name: Display name
## - description: Short description of the trait
## - stat_mods: Dictionary of stat multipliers (multiplicative, e.g., 1.1 = +10%)

## Available stat_mods keys:
## - max_hp: Unit health
## - attack_damage: Unit/spell damage
## - attack_speed: Attacks per second (higher = faster)
## - move_speed: Movement speed
## - spell_damage: Spell damage
## - spell_radius: AOE radius

const TRAITS: Dictionary = {
	# ==========================================================================
	# FIREBALL - AOE damage spell, focus on damage or radius
	# ==========================================================================
	"fireball": {
		2: [
			{"id": "fireball_intense_2", "name": "Intense Flames", "description": "+12% Damage",
			 "stat_mods": {"spell_damage": 1.12}},
			{"id": "fireball_spread_2", "name": "Wider Spread", "description": "+10% Radius",
			 "stat_mods": {"spell_radius": 1.10}}
		],
		3: [
			{"id": "fireball_scorching_3", "name": "Scorching Heat", "description": "+15% Damage",
			 "stat_mods": {"spell_damage": 1.15}},
			{"id": "fireball_expanded_3", "name": "Expanded Blast", "description": "+12% Radius",
			 "stat_mods": {"spell_radius": 1.12}}
		],
		4: [
			{"id": "fireball_inferno_4", "name": "Inferno", "description": "+18% Damage",
			 "stat_mods": {"spell_damage": 1.18}},
			{"id": "fireball_conflagration_4", "name": "Conflagration", "description": "+15% Radius",
			 "stat_mods": {"spell_radius": 1.15}}
		],
		5: [
			{"id": "fireball_meteor_5", "name": "Meteor Strike", "description": "+22% Damage",
			 "stat_mods": {"spell_damage": 1.22}},
			{"id": "fireball_firestorm_5", "name": "Firestorm", "description": "+18% Radius",
			 "stat_mods": {"spell_radius": 1.18}},
			{"id": "fireball_balanced_5", "name": "Refined Casting", "description": "+10% Damage, +10% Radius",
			 "stat_mods": {"spell_damage": 1.10, "spell_radius": 1.10}}
		],
		6: [
			{"id": "fireball_volcanic_6", "name": "Volcanic Fury", "description": "+25% Damage",
			 "stat_mods": {"spell_damage": 1.25}},
			{"id": "fireball_wildfire_6", "name": "Wildfire", "description": "+20% Radius",
			 "stat_mods": {"spell_radius": 1.20}}
		],
		7: [
			{"id": "fireball_cataclysm_7", "name": "Cataclysm", "description": "+28% Damage",
			 "stat_mods": {"spell_damage": 1.28}},
			{"id": "fireball_devastation_7", "name": "Devastation", "description": "+22% Radius",
			 "stat_mods": {"spell_radius": 1.22}},
			{"id": "fireball_efficient_7", "name": "Efficient Casting", "description": "+15% Damage, +12% Radius",
			 "stat_mods": {"spell_damage": 1.15, "spell_radius": 1.12}}
		],
		8: [
			{"id": "fireball_hellfire_8", "name": "Hellfire", "description": "+32% Damage",
			 "stat_mods": {"spell_damage": 1.32}},
			{"id": "fireball_apocalypse_8", "name": "Apocalyptic Blast", "description": "+25% Radius",
			 "stat_mods": {"spell_radius": 1.25}}
		],
		9: [
			{"id": "fireball_solar_9", "name": "Solar Flare", "description": "+35% Damage",
			 "stat_mods": {"spell_damage": 1.35}},
			{"id": "fireball_nova_9", "name": "Supernova", "description": "+28% Radius",
			 "stat_mods": {"spell_radius": 1.28}},
			{"id": "fireball_mastery_9", "name": "Fire Mastery", "description": "+20% Damage, +18% Radius",
			 "stat_mods": {"spell_damage": 1.20, "spell_radius": 1.18}}
		],
		10: [
			{"id": "fireball_apex_power_10", "name": "Apex Destruction", "description": "+40% Damage",
			 "stat_mods": {"spell_damage": 1.40}},
			{"id": "fireball_apex_area_10", "name": "Apex Devastation", "description": "+32% Radius",
			 "stat_mods": {"spell_radius": 1.32}},
			{"id": "fireball_apex_perfect_10", "name": "Perfect Inferno", "description": "+25% Damage, +25% Radius",
			 "stat_mods": {"spell_damage": 1.25, "spell_radius": 1.25}}
		]
	},

	# ==========================================================================
	# CHARGE - Tactical spell (focus-fire command)
	# ==========================================================================
	"charge": {
		2: [
			{"id": "charge_inspiring_2", "name": "Inspiring Charge", "description": "+10% Damage",
			 "stat_mods": {"spell_damage": 1.10}},
			{"id": "charge_swift_2", "name": "Swift Charge", "description": "+12% Effect Speed",
			 "stat_mods": {"move_speed": 1.12}}
		],
		3: [
			{"id": "charge_ferocious_3", "name": "Ferocious Assault", "description": "+15% Damage",
			 "stat_mods": {"spell_damage": 1.15}},
			{"id": "charge_coordinated_3", "name": "Coordinated Strike", "description": "+10% Damage, +8% Speed",
			 "stat_mods": {"spell_damage": 1.10, "move_speed": 1.08}}
		],
		4: [
			{"id": "charge_devastating_4", "name": "Devastating Charge", "description": "+18% Damage",
			 "stat_mods": {"spell_damage": 1.18}},
			{"id": "charge_lightning_4", "name": "Lightning Charge", "description": "+18% Effect Speed",
			 "stat_mods": {"move_speed": 1.18}}
		],
		5: [
			{"id": "charge_crushing_5", "name": "Crushing Assault", "description": "+22% Damage",
			 "stat_mods": {"spell_damage": 1.22}},
			{"id": "charge_blitz_5", "name": "Blitz", "description": "+22% Effect Speed",
			 "stat_mods": {"move_speed": 1.22}},
			{"id": "charge_tactical_5", "name": "Tactical Excellence", "description": "+12% Damage, +12% Speed",
			 "stat_mods": {"spell_damage": 1.12, "move_speed": 1.12}}
		],
		6: [
			{"id": "charge_overwhelming_6", "name": "Overwhelming Force", "description": "+25% Damage",
			 "stat_mods": {"spell_damage": 1.25}},
			{"id": "charge_rapid_6", "name": "Rapid Assault", "description": "+25% Effect Speed",
			 "stat_mods": {"move_speed": 1.25}}
		],
		7: [
			{"id": "charge_annihilating_7", "name": "Annihilating Charge", "description": "+28% Damage",
			 "stat_mods": {"spell_damage": 1.28}},
			{"id": "charge_thunder_7", "name": "Thunder Strike", "description": "+28% Effect Speed",
			 "stat_mods": {"move_speed": 1.28}},
			{"id": "charge_supreme_7", "name": "Supreme Command", "description": "+15% Damage, +15% Speed",
			 "stat_mods": {"spell_damage": 1.15, "move_speed": 1.15}}
		],
		8: [
			{"id": "charge_decimating_8", "name": "Decimating Assault", "description": "+32% Damage",
			 "stat_mods": {"spell_damage": 1.32}},
			{"id": "charge_sonic_8", "name": "Sonic Charge", "description": "+32% Effect Speed",
			 "stat_mods": {"move_speed": 1.32}}
		],
		9: [
			{"id": "charge_legendary_dmg_9", "name": "Legendary Assault", "description": "+35% Damage",
			 "stat_mods": {"spell_damage": 1.35}},
			{"id": "charge_legendary_spd_9", "name": "Legendary Speed", "description": "+35% Effect Speed",
			 "stat_mods": {"move_speed": 1.35}},
			{"id": "charge_legendary_bal_9", "name": "Perfect Charge", "description": "+20% Damage, +20% Speed",
			 "stat_mods": {"spell_damage": 1.20, "move_speed": 1.20}}
		],
		10: [
			{"id": "charge_apex_power_10", "name": "Apex Devastation", "description": "+40% Damage",
			 "stat_mods": {"spell_damage": 1.40}},
			{"id": "charge_apex_speed_10", "name": "Apex Velocity", "description": "+40% Effect Speed",
			 "stat_mods": {"move_speed": 1.40}},
			{"id": "charge_apex_perfect_10", "name": "Ultimate Command", "description": "+25% Damage, +25% Speed",
			 "stat_mods": {"spell_damage": 1.25, "move_speed": 1.25}}
		]
	},
}

## =============================================================================
## API
## =============================================================================

## Get traits available for a card at a specific level
## Returns: Array of trait dictionaries, empty if card has no traits defined
static func get_traits_for_level(catalog_id: String, level: int) -> Array:
	if not TRAITS.has(catalog_id):
		# Card doesn't have traits defined - warn and return empty
		if level >= 2 and level <= 10:
			push_warning("CardTraitCatalog: No traits defined for card '%s' at level %d" % [catalog_id, level])
		return []

	var card_traits: Dictionary = TRAITS[catalog_id]
	if not card_traits.has(level):
		return []

	return card_traits[level]

## Get a specific trait by ID
## Returns: Trait dictionary or empty dictionary if not found
static func get_trait(catalog_id: String, trait_id: String) -> Dictionary:
	if not TRAITS.has(catalog_id):
		return {}

	var card_traits: Dictionary = TRAITS[catalog_id]
	for level: Variant in card_traits:
		if level is int:
			var level_traits: Array = card_traits[level]
			for trait_item: Variant in level_traits:
				if trait_item is Dictionary:
					var trait_dict: Dictionary = trait_item
					if trait_dict.get("id") == trait_id:
						return trait_dict

	return {}

## Check if a card has specific traits defined
static func has_traits(catalog_id: String) -> bool:
	return TRAITS.has(catalog_id)

## Get all trait IDs for a card (for validation)
static func get_all_trait_ids(catalog_id: String) -> Array[String]:
	var ids: Array[String] = []

	if not TRAITS.has(catalog_id):
		return ids

	var card_traits: Dictionary = TRAITS[catalog_id]
	for level: Variant in card_traits:
		if level is int:
			var level_traits: Array = card_traits[level]
			for trait_item: Variant in level_traits:
				if trait_item is Dictionary:
					var trait_dict: Dictionary = trait_item
					var id: String = trait_dict.get("id", "")
					if not id.is_empty():
						ids.append(id)

	return ids
