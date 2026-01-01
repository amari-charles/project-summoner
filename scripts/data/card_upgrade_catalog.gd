extends Node
class_name CardUpgradeCatalog

## Card Upgrade Catalog - Defines upgrade choices for each card at each level
##
## Each card can have 2-3 upgrade choices per level (levels 2-10).
## Upgrades should be meaningful choices that enhance the card's identity,
## not just raw stat increases.
##
## Usage:
##   var upgrades = CardUpgradeCatalog.get_upgrades_for_level("fire_elemental", 2)
##   var upgrade = CardUpgradeCatalog.get_upgrade("fire_elemental", "fire_elemental_hardy_2")

## =============================================================================
## UPGRADE DEFINITIONS
## =============================================================================

## Upgrade data structure:
## - id: Unique identifier (format: {catalog_id}_{name}_{level})
## - name: Display name
## - description: Short description of the upgrade
## - stat_mods: Dictionary of stat multipliers (multiplicative, e.g., 1.1 = +10%)

## Available stat_mods keys:
## - max_hp: Unit health
## - attack_damage: Unit/spell damage
## - attack_speed: Attacks per second (higher = faster)
## - move_speed: Movement speed
## - spell_damage: Spell damage
## - spell_radius: AOE radius

const UPGRADES: Dictionary = {
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

## Get upgrades available for a card at a specific level
## Returns: Array of upgrade dictionaries
static func get_upgrades_for_level(catalog_id: String, level: int) -> Array:
	if not UPGRADES.has(catalog_id):
		# Card doesn't have specific upgrades defined - return generic upgrades
		return _get_generic_upgrades(catalog_id, level)

	var card_upgrades: Dictionary = UPGRADES[catalog_id]
	if not card_upgrades.has(level):
		return []

	return card_upgrades[level]

## Get a specific upgrade by ID
## Returns: Upgrade dictionary or empty dictionary if not found
static func get_upgrade(catalog_id: String, upgrade_id: String) -> Dictionary:
	if not UPGRADES.has(catalog_id):
		# Check generic upgrades
		return _find_generic_upgrade(catalog_id, upgrade_id)

	var card_upgrades: Dictionary = UPGRADES[catalog_id]
	for level: Variant in card_upgrades:
		if level is int:
			var level_upgrades: Array = card_upgrades[level]
			for upgrade: Variant in level_upgrades:
				if upgrade is Dictionary:
					var upgrade_dict: Dictionary = upgrade
					if upgrade_dict.get("id") == upgrade_id:
						return upgrade_dict

	return {}

## Check if a card has specific upgrades defined
static func has_upgrades(catalog_id: String) -> bool:
	return UPGRADES.has(catalog_id)

## Get all upgrade IDs for a card (for validation)
static func get_all_upgrade_ids(catalog_id: String) -> Array[String]:
	var ids: Array[String] = []

	if not UPGRADES.has(catalog_id):
		return ids

	var card_upgrades: Dictionary = UPGRADES[catalog_id]
	for level: Variant in card_upgrades:
		if level is int:
			var level_upgrades: Array = card_upgrades[level]
			for upgrade: Variant in level_upgrades:
				if upgrade is Dictionary:
					var upgrade_dict: Dictionary = upgrade
					var upgrade_id: String = upgrade_dict.get("id", "")
					if not upgrade_id.is_empty():
						ids.append(upgrade_id)

	return ids

## =============================================================================
## GENERIC UPGRADES (for cards without specific definitions)
## =============================================================================

## Generate generic upgrades for cards without specific definitions
static func _get_generic_upgrades(catalog_id: String, level: int) -> Array:
	if level < 2 or level > 10:
		return []

	# Scale percentages with level
	var hp_bonus: float = 1.0 + (0.08 + level * 0.02)  # 10% at L2, up to 28% at L10
	var dmg_bonus: float = 1.0 + (0.06 + level * 0.02)  # 8% at L2, up to 26% at L10
	var spd_bonus: float = 1.0 + (0.05 + level * 0.015)  # 6.5% at L2, up to 20% at L10

	var hp_percent: int = int((hp_bonus - 1.0) * 100)
	var dmg_percent: int = int((dmg_bonus - 1.0) * 100)
	var spd_percent: int = int((spd_bonus - 1.0) * 100)

	var base_upgrades: Array = [
		{
			"id": "%s_hp_l%d" % [catalog_id, level],
			"name": "Fortitude",
			"description": "+%d%% HP" % hp_percent,
			"stat_mods": {"max_hp": hp_bonus}
		},
		{
			"id": "%s_dmg_l%d" % [catalog_id, level],
			"name": "Power",
			"description": "+%d%% Damage" % dmg_percent,
			"stat_mods": {"attack_damage": dmg_bonus}
		}
	]

	# Add third option at higher levels
	if level >= 4:
		base_upgrades.append({
			"id": "%s_spd_l%d" % [catalog_id, level],
			"name": "Swiftness",
			"description": "+%d%% Attack Speed" % spd_percent,
			"stat_mods": {"attack_speed": spd_bonus}
		})

	return base_upgrades

## Find a generic upgrade by ID
static func _find_generic_upgrade(catalog_id: String, upgrade_id: String) -> Dictionary:
	# Parse the upgrade ID to get level
	for level: int in range(2, 11):
		var upgrades: Array = _get_generic_upgrades(catalog_id, level)
		for upgrade: Variant in upgrades:
			if upgrade is Dictionary:
				var upgrade_dict: Dictionary = upgrade
				if upgrade_dict.get("id") == upgrade_id:
					return upgrade_dict
	return {}
