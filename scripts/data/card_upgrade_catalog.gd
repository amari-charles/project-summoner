extends Node
class_name CardUpgradeCatalog

## Card Upgrade Catalog - Defines upgrade choices for each card at each level
##
## Each card can have 2-3 upgrade choices per level (levels 2-10).
## Upgrades should be meaningful choices that enhance the card's identity,
## not just raw stat increases.
##
## Usage:
##   var upgrades = CardUpgradeCatalog.get_upgrades_for_level("fire_recruit", 2)
##   var upgrade = CardUpgradeCatalog.get_upgrade("fire_recruit", "fire_recruit_hardy_2")

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
	# FIRE RECRUIT - Cheap melee soldier, versatile upgrades
	# ==========================================================================
	"fire_recruit": {
		2: [
			{"id": "fire_recruit_hardy_2", "name": "Hardy Recruit", "description": "+12% HP",
			 "stat_mods": {"max_hp": 1.12}},
			{"id": "fire_recruit_fierce_2", "name": "Fierce Recruit", "description": "+10% Damage",
			 "stat_mods": {"attack_damage": 1.10}}
		],
		3: [
			{"id": "fire_recruit_bulwark_3", "name": "Bulwark", "description": "+15% HP",
			 "stat_mods": {"max_hp": 1.15}},
			{"id": "fire_recruit_aggressor_3", "name": "Aggressor", "description": "+12% Damage",
			 "stat_mods": {"attack_damage": 1.12}},
			{"id": "fire_recruit_swift_3", "name": "Swift Footwork", "description": "+10% Move Speed",
			 "stat_mods": {"move_speed": 1.10}}
		],
		4: [
			{"id": "fire_recruit_ironclad_4", "name": "Ironclad", "description": "+18% HP",
			 "stat_mods": {"max_hp": 1.18}},
			{"id": "fire_recruit_berserker_4", "name": "Berserker", "description": "+8% Attack Speed",
			 "stat_mods": {"attack_speed": 1.08}}
		],
		5: [
			{"id": "fire_recruit_veteran_hp_5", "name": "Veteran's Fortitude", "description": "+20% HP",
			 "stat_mods": {"max_hp": 1.20}},
			{"id": "fire_recruit_veteran_dmg_5", "name": "Veteran's Might", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}},
			{"id": "fire_recruit_veteran_spd_5", "name": "Veteran's Agility", "description": "+12% Attack Speed",
			 "stat_mods": {"attack_speed": 1.12}}
		],
		6: [
			{"id": "fire_recruit_champion_hp_6", "name": "Champion's Resolve", "description": "+22% HP",
			 "stat_mods": {"max_hp": 1.22}},
			{"id": "fire_recruit_champion_dmg_6", "name": "Champion's Strike", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}}
		],
		7: [
			{"id": "fire_recruit_elite_tank_7", "name": "Elite Tank", "description": "+25% HP",
			 "stat_mods": {"max_hp": 1.25}},
			{"id": "fire_recruit_elite_dmg_7", "name": "Elite Striker", "description": "+20% Damage",
			 "stat_mods": {"attack_damage": 1.20}},
			{"id": "fire_recruit_balanced_7", "name": "Balanced Training", "description": "+10% HP, +10% Damage",
			 "stat_mods": {"max_hp": 1.10, "attack_damage": 1.10}}
		],
		8: [
			{"id": "fire_recruit_master_hp_8", "name": "Master's Endurance", "description": "+28% HP",
			 "stat_mods": {"max_hp": 1.28}},
			{"id": "fire_recruit_master_dmg_8", "name": "Master's Fury", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}}
		],
		9: [
			{"id": "fire_recruit_legend_hp_9", "name": "Legendary Fortitude", "description": "+30% HP",
			 "stat_mods": {"max_hp": 1.30}},
			{"id": "fire_recruit_legend_dmg_9", "name": "Legendary Might", "description": "+25% Damage",
			 "stat_mods": {"attack_damage": 1.25}},
			{"id": "fire_recruit_legend_spd_9", "name": "Legendary Swiftness", "description": "+18% Attack Speed",
			 "stat_mods": {"attack_speed": 1.18}}
		],
		10: [
			{"id": "fire_recruit_apex_tank_10", "name": "Apex Guardian", "description": "+35% HP",
			 "stat_mods": {"max_hp": 1.35}},
			{"id": "fire_recruit_apex_dmg_10", "name": "Apex Warrior", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}},
			{"id": "fire_recruit_apex_all_10", "name": "Perfect Soldier", "description": "+15% HP, +15% Damage, +10% Speed",
			 "stat_mods": {"max_hp": 1.15, "attack_damage": 1.15, "attack_speed": 1.10}}
		]
	},

	# ==========================================================================
	# EMBER SLINGER - Fragile ranged attacker, focus on damage or survival
	# ==========================================================================
	"ember_slinger": {
		2: [
			{"id": "ember_slinger_precise_2", "name": "Precise Shot", "description": "+12% Damage",
			 "stat_mods": {"attack_damage": 1.12}},
			{"id": "ember_slinger_nimble_2", "name": "Nimble", "description": "+15% Move Speed",
			 "stat_mods": {"move_speed": 1.15}}
		],
		3: [
			{"id": "ember_slinger_rapid_3", "name": "Rapid Fire", "description": "+10% Attack Speed",
			 "stat_mods": {"attack_speed": 1.10}},
			{"id": "ember_slinger_heavy_3", "name": "Heavy Ammunition", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}},
			{"id": "ember_slinger_hardy_3", "name": "Hardy", "description": "+20% HP",
			 "stat_mods": {"max_hp": 1.20}}
		],
		4: [
			{"id": "ember_slinger_quickdraw_4", "name": "Quickdraw", "description": "+12% Attack Speed",
			 "stat_mods": {"attack_speed": 1.12}},
			{"id": "ember_slinger_focused_4", "name": "Focused Fire", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}}
		],
		5: [
			{"id": "ember_slinger_marksman_5", "name": "Marksman", "description": "+20% Damage",
			 "stat_mods": {"attack_damage": 1.20}},
			{"id": "ember_slinger_agile_5", "name": "Agile", "description": "+20% Move Speed",
			 "stat_mods": {"move_speed": 1.20}},
			{"id": "ember_slinger_resilient_5", "name": "Resilient", "description": "+25% HP",
			 "stat_mods": {"max_hp": 1.25}}
		],
		6: [
			{"id": "ember_slinger_volley_6", "name": "Volley Expert", "description": "+15% Attack Speed",
			 "stat_mods": {"attack_speed": 1.15}},
			{"id": "ember_slinger_piercing_6", "name": "Piercing Shots", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}}
		],
		7: [
			{"id": "ember_slinger_sharpshooter_7", "name": "Sharpshooter", "description": "+25% Damage",
			 "stat_mods": {"attack_damage": 1.25}},
			{"id": "ember_slinger_skirmisher_7", "name": "Skirmisher", "description": "+25% Move Speed",
			 "stat_mods": {"move_speed": 1.25}},
			{"id": "ember_slinger_toughened_7", "name": "Toughened", "description": "+30% HP",
			 "stat_mods": {"max_hp": 1.30}}
		],
		8: [
			{"id": "ember_slinger_elite_shot_8", "name": "Elite Marksman", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}},
			{"id": "ember_slinger_elite_speed_8", "name": "Elite Reflexes", "description": "+18% Attack Speed",
			 "stat_mods": {"attack_speed": 1.18}}
		],
		9: [
			{"id": "ember_slinger_master_dmg_9", "name": "Master Archer", "description": "+30% Damage",
			 "stat_mods": {"attack_damage": 1.30}},
			{"id": "ember_slinger_master_speed_9", "name": "Rapid Assault", "description": "+20% Attack Speed",
			 "stat_mods": {"attack_speed": 1.20}},
			{"id": "ember_slinger_master_mobile_9", "name": "Mobile Artillery", "description": "+12% Damage, +15% Move Speed",
			 "stat_mods": {"attack_damage": 1.12, "move_speed": 1.15}}
		],
		10: [
			{"id": "ember_slinger_apex_dmg_10", "name": "Apex Sniper", "description": "+35% Damage",
			 "stat_mods": {"attack_damage": 1.35}},
			{"id": "ember_slinger_apex_rapid_10", "name": "Storm of Arrows", "description": "+22% Attack Speed, +10% Damage",
			 "stat_mods": {"attack_speed": 1.22, "attack_damage": 1.10}},
			{"id": "ember_slinger_apex_survivor_10", "name": "Battle-Hardened", "description": "+40% HP, +15% Move Speed",
			 "stat_mods": {"max_hp": 1.40, "move_speed": 1.15}}
		]
	},

	# ==========================================================================
	# NEADE - Heavy lancer/tank, focus on survivability or impact damage
	# ==========================================================================
	"neade": {
		2: [
			{"id": "neade_armored_2", "name": "Reinforced Armor", "description": "+15% HP",
			 "stat_mods": {"max_hp": 1.15}},
			{"id": "neade_lance_2", "name": "Heavy Lance", "description": "+12% Damage",
			 "stat_mods": {"attack_damage": 1.12}}
		],
		3: [
			{"id": "neade_fortress_3", "name": "Fortress", "description": "+18% HP",
			 "stat_mods": {"max_hp": 1.18}},
			{"id": "neade_charger_3", "name": "Charger", "description": "+15% Move Speed",
			 "stat_mods": {"move_speed": 1.15}},
			{"id": "neade_crusher_3", "name": "Crusher", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}}
		],
		4: [
			{"id": "neade_bulwark_4", "name": "Living Bulwark", "description": "+20% HP",
			 "stat_mods": {"max_hp": 1.20}},
			{"id": "neade_impaler_4", "name": "Impaler", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}}
		],
		5: [
			{"id": "neade_titan_hp_5", "name": "Titan's Constitution", "description": "+25% HP",
			 "stat_mods": {"max_hp": 1.25}},
			{"id": "neade_titan_dmg_5", "name": "Titan's Wrath", "description": "+20% Damage",
			 "stat_mods": {"attack_damage": 1.20}},
			{"id": "neade_relentless_5", "name": "Relentless Advance", "description": "+20% Move Speed",
			 "stat_mods": {"move_speed": 1.20}}
		],
		6: [
			{"id": "neade_juggernaut_6", "name": "Juggernaut", "description": "+28% HP",
			 "stat_mods": {"max_hp": 1.28}},
			{"id": "neade_devastator_6", "name": "Devastator", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}}
		],
		7: [
			{"id": "neade_immovable_7", "name": "Immovable Object", "description": "+32% HP",
			 "stat_mods": {"max_hp": 1.32}},
			{"id": "neade_unstoppable_7", "name": "Unstoppable Force", "description": "+25% Damage, +10% Move Speed",
			 "stat_mods": {"attack_damage": 1.25, "move_speed": 1.10}},
			{"id": "neade_warlord_7", "name": "Warlord", "description": "+15% HP, +15% Damage",
			 "stat_mods": {"max_hp": 1.15, "attack_damage": 1.15}}
		],
		8: [
			{"id": "neade_colossus_8", "name": "Colossus", "description": "+35% HP",
			 "stat_mods": {"max_hp": 1.35}},
			{"id": "neade_annihilator_8", "name": "Annihilator", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}}
		],
		9: [
			{"id": "neade_legend_tank_9", "name": "Legendary Guardian", "description": "+40% HP",
			 "stat_mods": {"max_hp": 1.40}},
			{"id": "neade_legend_dmg_9", "name": "Legendary Destroyer", "description": "+32% Damage",
			 "stat_mods": {"attack_damage": 1.32}},
			{"id": "neade_legend_balanced_9", "name": "Legendary Champion", "description": "+20% HP, +20% Damage",
			 "stat_mods": {"max_hp": 1.20, "attack_damage": 1.20}}
		],
		10: [
			{"id": "neade_apex_fortress_10", "name": "Apex Fortress", "description": "+50% HP",
			 "stat_mods": {"max_hp": 1.50}},
			{"id": "neade_apex_siege_10", "name": "Siege Engine", "description": "+40% Damage",
			 "stat_mods": {"attack_damage": 1.40}},
			{"id": "neade_apex_perfect_10", "name": "Perfect Warrior", "description": "+25% HP, +25% Damage, +15% Move Speed",
			 "stat_mods": {"max_hp": 1.25, "attack_damage": 1.25, "move_speed": 1.15}}
		]
	},

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

	# ==========================================================================
	# SLIME_GREEN - Basic small slime, earth element
	# ==========================================================================
	"slime_green": {
		2: [
			{"id": "slime_green_thick_2", "name": "Thick Membrane", "description": "+15% HP",
			 "stat_mods": {"max_hp": 1.15}},
			{"id": "slime_green_acidic_2", "name": "Acidic Core", "description": "+12% Damage",
			 "stat_mods": {"attack_damage": 1.12}}
		],
		3: [
			{"id": "slime_green_bouncy_3", "name": "Bouncy", "description": "+15% Move Speed",
			 "stat_mods": {"move_speed": 1.15}},
			{"id": "slime_green_viscous_3", "name": "Viscous Body", "description": "+18% HP",
			 "stat_mods": {"max_hp": 1.18}},
			{"id": "slime_green_corrosive_3", "name": "Corrosive Touch", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}}
		],
		4: [
			{"id": "slime_green_gelatinous_4", "name": "Gelatinous Form", "description": "+20% HP",
			 "stat_mods": {"max_hp": 1.20}},
			{"id": "slime_green_potent_4", "name": "Potent Secretion", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}}
		],
		5: [
			{"id": "slime_green_resilient_5", "name": "Resilient Gel", "description": "+25% HP",
			 "stat_mods": {"max_hp": 1.25}},
			{"id": "slime_green_volatile_5", "name": "Volatile Compound", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}},
			{"id": "slime_green_agile_5", "name": "Agile Blob", "description": "+20% Move Speed",
			 "stat_mods": {"move_speed": 1.20}}
		],
		6: [
			{"id": "slime_green_dense_6", "name": "Dense Core", "description": "+28% HP",
			 "stat_mods": {"max_hp": 1.28}},
			{"id": "slime_green_toxic_6", "name": "Toxic Slime", "description": "+25% Damage",
			 "stat_mods": {"attack_damage": 1.25}}
		],
		7: [
			{"id": "slime_green_massive_7", "name": "Massive Form", "description": "+32% HP",
			 "stat_mods": {"max_hp": 1.32}},
			{"id": "slime_green_deadly_7", "name": "Deadly Ooze", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}},
			{"id": "slime_green_swift_7", "name": "Swift Slime", "description": "+25% Move Speed",
			 "stat_mods": {"move_speed": 1.25}}
		],
		8: [
			{"id": "slime_green_colossal_8", "name": "Colossal Blob", "description": "+35% HP",
			 "stat_mods": {"max_hp": 1.35}},
			{"id": "slime_green_lethal_8", "name": "Lethal Compound", "description": "+32% Damage",
			 "stat_mods": {"attack_damage": 1.32}}
		],
		9: [
			{"id": "slime_green_titan_9", "name": "Slime Titan", "description": "+40% HP",
			 "stat_mods": {"max_hp": 1.40}},
			{"id": "slime_green_apex_dmg_9", "name": "Apex Predator", "description": "+35% Damage",
			 "stat_mods": {"attack_damage": 1.35}},
			{"id": "slime_green_balanced_9", "name": "Perfect Evolution", "description": "+20% HP, +20% Damage",
			 "stat_mods": {"max_hp": 1.20, "attack_damage": 1.20}}
		],
		10: [
			{"id": "slime_green_ultimate_hp_10", "name": "Ultimate Form", "description": "+50% HP",
			 "stat_mods": {"max_hp": 1.50}},
			{"id": "slime_green_ultimate_dmg_10", "name": "Ultimate Toxicity", "description": "+40% Damage",
			 "stat_mods": {"attack_damage": 1.40}},
			{"id": "slime_green_ultimate_all_10", "name": "Perfect Organism", "description": "+25% HP, +25% Damage, +15% Speed",
			 "stat_mods": {"max_hp": 1.25, "attack_damage": 1.25, "move_speed": 1.15}}
		]
	},

	# ==========================================================================
	# BLAZE RIDER - Fast cavalry charger
	# ==========================================================================
	"blaze_rider": {
		2: [
			{"id": "blaze_rider_speed_2", "name": "Swift Hooves", "description": "+12% Move Speed",
			 "stat_mods": {"move_speed": 1.12}},
			{"id": "blaze_rider_impact_2", "name": "Heavy Impact", "description": "+10% Damage",
			 "stat_mods": {"attack_damage": 1.10}}
		],
		3: [
			{"id": "blaze_rider_gallop_3", "name": "Full Gallop", "description": "+15% Move Speed",
			 "stat_mods": {"move_speed": 1.15}},
			{"id": "blaze_rider_lance_3", "name": "Lance Mastery", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}},
			{"id": "blaze_rider_armor_3", "name": "Rider's Armor", "description": "+15% HP",
			 "stat_mods": {"max_hp": 1.15}}
		],
		4: [
			{"id": "blaze_rider_charge_4", "name": "Devastating Charge", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}},
			{"id": "blaze_rider_wind_4", "name": "Wind Rider", "description": "+18% Move Speed",
			 "stat_mods": {"move_speed": 1.18}}
		],
		5: [
			{"id": "blaze_rider_cavalry_5", "name": "Elite Cavalry", "description": "+22% Move Speed",
			 "stat_mods": {"move_speed": 1.22}},
			{"id": "blaze_rider_knight_5", "name": "Knight's Fury", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}},
			{"id": "blaze_rider_balanced_5", "name": "Balanced Training", "description": "+12% Speed, +12% Damage",
			 "stat_mods": {"move_speed": 1.12, "attack_damage": 1.12}}
		],
		6: [
			{"id": "blaze_rider_thunder_6", "name": "Thunder Charge", "description": "+25% Move Speed",
			 "stat_mods": {"move_speed": 1.25}},
			{"id": "blaze_rider_crusher_6", "name": "Line Crusher", "description": "+25% Damage",
			 "stat_mods": {"attack_damage": 1.25}}
		],
		7: [
			{"id": "blaze_rider_storm_7", "name": "Storm Rider", "description": "+28% Move Speed",
			 "stat_mods": {"move_speed": 1.28}},
			{"id": "blaze_rider_destroyer_7", "name": "Destroyer", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}},
			{"id": "blaze_rider_champion_7", "name": "Champion Rider", "description": "+15% Speed, +15% Damage",
			 "stat_mods": {"move_speed": 1.15, "attack_damage": 1.15}}
		],
		8: [
			{"id": "blaze_rider_lightning_8", "name": "Lightning Speed", "description": "+32% Move Speed",
			 "stat_mods": {"move_speed": 1.32}},
			{"id": "blaze_rider_annihilator_8", "name": "Annihilator", "description": "+32% Damage",
			 "stat_mods": {"attack_damage": 1.32}}
		],
		9: [
			{"id": "blaze_rider_legend_spd_9", "name": "Legendary Speed", "description": "+35% Move Speed",
			 "stat_mods": {"move_speed": 1.35}},
			{"id": "blaze_rider_legend_dmg_9", "name": "Legendary Might", "description": "+35% Damage",
			 "stat_mods": {"attack_damage": 1.35}},
			{"id": "blaze_rider_legend_bal_9", "name": "Perfect Cavalry", "description": "+20% Speed, +20% Damage",
			 "stat_mods": {"move_speed": 1.20, "attack_damage": 1.20}}
		],
		10: [
			{"id": "blaze_rider_apex_speed_10", "name": "Apex Velocity", "description": "+40% Move Speed",
			 "stat_mods": {"move_speed": 1.40}},
			{"id": "blaze_rider_apex_dmg_10", "name": "Apex Destruction", "description": "+40% Damage",
			 "stat_mods": {"attack_damage": 1.40}},
			{"id": "blaze_rider_apex_perfect_10", "name": "Ultimate Rider", "description": "+25% Speed, +25% Damage, +15% HP",
			 "stat_mods": {"move_speed": 1.25, "attack_damage": 1.25, "max_hp": 1.15}}
		]
	},

	# ==========================================================================
	# ASH VANGUARD - Explosive tank
	# ==========================================================================
	"ash_vanguard": {
		2: [
			{"id": "ash_vanguard_fortified_2", "name": "Fortified", "description": "+15% HP",
			 "stat_mods": {"max_hp": 1.15}},
			{"id": "ash_vanguard_explosive_2", "name": "Explosive Coating", "description": "+12% Damage",
			 "stat_mods": {"attack_damage": 1.12}}
		],
		3: [
			{"id": "ash_vanguard_ironhide_3", "name": "Ironhide", "description": "+18% HP",
			 "stat_mods": {"max_hp": 1.18}},
			{"id": "ash_vanguard_volatile_3", "name": "Volatile Core", "description": "+15% Damage",
			 "stat_mods": {"attack_damage": 1.15}},
			{"id": "ash_vanguard_steady_3", "name": "Steady Advance", "description": "+12% Move Speed",
			 "stat_mods": {"move_speed": 1.12}}
		],
		4: [
			{"id": "ash_vanguard_bastion_4", "name": "Bastion", "description": "+22% HP",
			 "stat_mods": {"max_hp": 1.22}},
			{"id": "ash_vanguard_detonation_4", "name": "Enhanced Detonation", "description": "+18% Damage",
			 "stat_mods": {"attack_damage": 1.18}}
		],
		5: [
			{"id": "ash_vanguard_fortress_5", "name": "Walking Fortress", "description": "+28% HP",
			 "stat_mods": {"max_hp": 1.28}},
			{"id": "ash_vanguard_devastator_5", "name": "Devastator", "description": "+22% Damage",
			 "stat_mods": {"attack_damage": 1.22}},
			{"id": "ash_vanguard_balanced_5", "name": "Balanced Power", "description": "+15% HP, +12% Damage",
			 "stat_mods": {"max_hp": 1.15, "attack_damage": 1.12}}
		],
		6: [
			{"id": "ash_vanguard_titan_6", "name": "Titan's Constitution", "description": "+32% HP",
			 "stat_mods": {"max_hp": 1.32}},
			{"id": "ash_vanguard_cataclysm_6", "name": "Cataclysmic Force", "description": "+25% Damage",
			 "stat_mods": {"attack_damage": 1.25}}
		],
		7: [
			{"id": "ash_vanguard_juggernaut_7", "name": "Juggernaut", "description": "+35% HP",
			 "stat_mods": {"max_hp": 1.35}},
			{"id": "ash_vanguard_apocalypse_7", "name": "Apocalypse Engine", "description": "+28% Damage",
			 "stat_mods": {"attack_damage": 1.28}},
			{"id": "ash_vanguard_champion_7", "name": "Champion's Might", "description": "+18% HP, +18% Damage",
			 "stat_mods": {"max_hp": 1.18, "attack_damage": 1.18}}
		],
		8: [
			{"id": "ash_vanguard_colossus_8", "name": "Colossus", "description": "+40% HP",
			 "stat_mods": {"max_hp": 1.40}},
			{"id": "ash_vanguard_annihilation_8", "name": "Annihilation Core", "description": "+32% Damage",
			 "stat_mods": {"attack_damage": 1.32}}
		],
		9: [
			{"id": "ash_vanguard_legend_hp_9", "name": "Legendary Bulwark", "description": "+45% HP",
			 "stat_mods": {"max_hp": 1.45}},
			{"id": "ash_vanguard_legend_dmg_9", "name": "Legendary Destruction", "description": "+35% Damage",
			 "stat_mods": {"attack_damage": 1.35}},
			{"id": "ash_vanguard_legend_bal_9", "name": "Perfect Vanguard", "description": "+22% HP, +22% Damage",
			 "stat_mods": {"max_hp": 1.22, "attack_damage": 1.22}}
		],
		10: [
			{"id": "ash_vanguard_apex_hp_10", "name": "Apex Fortress", "description": "+55% HP",
			 "stat_mods": {"max_hp": 1.55}},
			{"id": "ash_vanguard_apex_dmg_10", "name": "Apex Devastation", "description": "+40% Damage",
			 "stat_mods": {"attack_damage": 1.40}},
			{"id": "ash_vanguard_apex_perfect_10", "name": "Ultimate Vanguard", "description": "+30% HP, +30% Damage",
			 "stat_mods": {"max_hp": 1.30, "attack_damage": 1.30}}
		]
	}
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
