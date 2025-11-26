extends Node
# CardProgressionService is registered as autoload "CardProgression", no class_name needed

## Card Progression Service - XP and Level Management
##
## Handles card XP gains, level-ups, and upgrade applications.
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   CardProgression.grant_card_xp(card_instance_id, 15)
##   if CardProgression.can_level_up(card_instance_id):
##       var upgrades = CardProgression.get_available_upgrades(card_instance_id)
##       CardProgression.apply_upgrade(card_instance_id, upgrade_id)
##
## Emits signals for reactive UI updates.

## Constants
const MAX_LEVEL: int = 10

## XP thresholds for each level (cumulative XP needed)
## Index 0 = Level 1 (no XP needed), Index 1 = Level 2, etc.
const XP_THRESHOLDS: Array[int] = [
	0,      # Level 1 (start)
	30,     # Level 2
	75,     # Level 3
	150,    # Level 4
	300,    # Level 5
	500,    # Level 6
	800,    # Level 7
	1200,   # Level 8
	1800,   # Level 9
	2500    # Level 10
]

## Gold cost per level-up (index = target level - 1)
const LEVEL_UP_GOLD_COST: Array[int] = [
	0,      # Level 1 (no cost)
	25,     # Level 2
	50,     # Level 3
	100,    # Level 4
	200,    # Level 5
	350,    # Level 6
	500,    # Level 7
	750,    # Level 8
	1000,   # Level 9
	1500    # Level 10
]

## Rarity multipliers for XP thresholds and gold costs
## Higher rarity cards require more XP AND gold to level up
const RARITY_MULTIPLIERS: Dictionary = {
	RarityIDs.COMMON: 1.0,
	RarityIDs.RARE: 1.5,
	RarityIDs.EPIC: 2.0,
	RarityIDs.LEGENDARY: 3.0
}

## Signals
signal card_xp_changed(card_instance_id: String, new_xp: int, new_level: int)
signal card_leveled_up(card_instance_id: String, new_level: int)
signal upgrade_applied(card_instance_id: String, upgrade_id: String)

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	pass

## =============================================================================
## XP OPERATIONS
## =============================================================================

## Grant XP to a card instance
## Returns the new total XP
func grant_card_xp(card_instance_id: String, amount: int) -> int:
	if amount <= 0:
		return 0

	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		push_warning("CardProgressionService: Card instance not found: %s" % card_instance_id)
		return 0

	var current_xp: int = card.get("xp", 0)
	var current_level: int = card.get("level", 1)
	var new_xp: int = current_xp + amount

	# Update card
	ProfileRepo.update_card(card_instance_id, {"xp": new_xp})

	card_xp_changed.emit(card_instance_id, new_xp, current_level)

	return new_xp

## Grant XP to multiple cards at once (e.g., all cards played in a battle)
## Returns: Dictionary of {card_instance_id: new_xp}
func grant_xp_to_cards(card_instance_ids: Array, amount: int) -> Dictionary:
	var results: Dictionary = {}
	for card_id: Variant in card_instance_ids:
		if card_id is String:
			var new_xp: int = grant_card_xp(card_id, amount)
			results[card_id] = new_xp
	return results

## Get XP required to reach a specific level (base, no rarity scaling)
func get_xp_for_level(level: int) -> int:
	if level < 1 or level > MAX_LEVEL:
		return 0
	return XP_THRESHOLDS[level - 1]

## Get XP required to reach a specific level with rarity scaling
func get_xp_for_level_with_rarity(level: int, rarity: StringName) -> int:
	var base_xp: int = get_xp_for_level(level)
	var multiplier: float = RARITY_MULTIPLIERS.get(rarity, 1.0)
	return int(base_xp * multiplier)

## Get gold cost for leveling up with rarity scaling
func get_gold_cost_for_level_with_rarity(level: int, rarity: StringName) -> int:
	if level < 1 or level > MAX_LEVEL:
		return 0
	var base_cost: int = LEVEL_UP_GOLD_COST[level - 1]
	var multiplier: float = RARITY_MULTIPLIERS.get(rarity, 1.0)
	return int(base_cost * multiplier)

## Get rarity multiplier for a given rarity
func get_rarity_multiplier(rarity: StringName) -> float:
	return RARITY_MULTIPLIERS.get(rarity, 1.0)

## Get XP needed for the next level from current XP (with rarity scaling)
func get_xp_to_next_level(card_instance_id: String) -> int:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return 0

	var current_xp: int = card.get("xp", 0)
	var current_level: int = card.get("level", 1)
	var rarity: StringName = StringName(card.get("rarity", RarityIDs.COMMON))

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	var next_level_xp: int = get_xp_for_level_with_rarity(current_level + 1, rarity)
	return maxi(0, next_level_xp - current_xp)

## Get progress towards next level as a percentage (0.0 - 1.0) with rarity scaling
func get_level_progress(card_instance_id: String) -> float:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return 0.0

	var current_xp: int = card.get("xp", 0)
	var current_level: int = card.get("level", 1)
	var rarity: StringName = StringName(card.get("rarity", RarityIDs.COMMON))

	if current_level >= MAX_LEVEL:
		return 1.0  # Max level

	var current_level_xp: int = get_xp_for_level_with_rarity(current_level, rarity)
	var next_level_xp: int = get_xp_for_level_with_rarity(current_level + 1, rarity)
	var level_range: int = next_level_xp - current_level_xp

	if level_range <= 0:
		return 1.0

	var progress_in_level: int = current_xp - current_level_xp
	return clampf(float(progress_in_level) / float(level_range), 0.0, 1.0)

## =============================================================================
## LEVEL-UP OPERATIONS
## =============================================================================

## Check if a card has enough XP to level up (with rarity scaling)
func can_level_up(card_instance_id: String) -> bool:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return false

	var current_xp: int = card.get("xp", 0)
	var current_level: int = card.get("level", 1)
	var rarity: StringName = StringName(card.get("rarity", RarityIDs.COMMON))

	if current_level >= MAX_LEVEL:
		return false  # Already max level

	var next_level_xp: int = get_xp_for_level_with_rarity(current_level + 1, rarity)
	return current_xp >= next_level_xp

## Get the gold cost to level up a card (with rarity scaling)
func get_level_up_gold_cost(card_instance_id: String) -> int:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return 0

	var current_level: int = card.get("level", 1)
	var rarity: StringName = StringName(card.get("rarity", RarityIDs.COMMON))

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	return get_gold_cost_for_level_with_rarity(current_level + 1, rarity)

## Check if player can afford to level up (XP + gold)
func can_afford_level_up(card_instance_id: String) -> bool:
	if not can_level_up(card_instance_id):
		return false

	var gold_cost: int = get_level_up_gold_cost(card_instance_id)
	return Economy.get_gold() >= gold_cost

## Level up a card (requires XP threshold met + gold + upgrade choice)
## upgrade_id: The chosen upgrade from get_available_upgrades()
## Returns true if successful
func level_up_card(card_instance_id: String, upgrade_id: String) -> bool:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		push_warning("CardProgressionService: Card not found: %s" % card_instance_id)
		return false

	# Check XP requirement
	if not can_level_up(card_instance_id):
		push_warning("CardProgressionService: Card does not have enough XP to level up")
		return false

	# Check gold cost
	var gold_cost: int = get_level_up_gold_cost(card_instance_id)
	if not Economy.can_afford({"gold": gold_cost}):
		push_warning("CardProgressionService: Cannot afford level-up cost (%d gold)" % gold_cost)
		return false

	# Validate upgrade choice
	var available_upgrades: Array = get_available_upgrades(card_instance_id)
	var valid_upgrade: bool = false
	for upgrade: Variant in available_upgrades:
		if upgrade is Dictionary:
			var upgrade_dict: Dictionary = upgrade
			if upgrade_dict.get("id") == upgrade_id:
				valid_upgrade = true
				break

	if not valid_upgrade:
		push_warning("CardProgressionService: Invalid upgrade choice: %s" % upgrade_id)
		return false

	# TODO: Support upgrade-specific resource costs from CardUpgradeCatalog
	# Currently uses flat gold cost; future upgrades may require essence, fragments, etc.

	# Spend gold
	Economy.spend({"gold": gold_cost})

	# Apply level up
	var current_level: int = card.get("level", 1)
	var new_level: int = current_level + 1
	var current_upgrades: Array = card.get("upgrades", [])
	current_upgrades.append(upgrade_id)

	ProfileRepo.update_card(card_instance_id, {
		"level": new_level,
		"upgrades": current_upgrades
	})

	card_leveled_up.emit(card_instance_id, new_level)
	upgrade_applied.emit(card_instance_id, upgrade_id)

	return true

## =============================================================================
## UPGRADE OPERATIONS
## =============================================================================

## Get available upgrades for a card's next level
## Returns array of upgrade options from CardUpgradeCatalog
func get_available_upgrades(card_instance_id: String) -> Array:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return []

	var catalog_id: String = card.get("catalog_id", "")
	var current_level: int = card.get("level", 1)
	var next_level: int = current_level + 1

	if next_level > MAX_LEVEL:
		return []  # No more upgrades at max level

	# Get from CardUpgradeCatalog
	return CardUpgradeCatalog.get_upgrades_for_level(catalog_id, next_level)

## Get all upgrades currently applied to a card
func get_applied_upgrades(card_instance_id: String) -> Array:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return []

	return card.get("upgrades", [])

## Get the stat modifiers from a card's upgrades
## Returns: Dictionary of stat multipliers (e.g., {"max_hp": 1.1, "attack_damage": 1.05})
func get_upgrade_stat_modifiers(card_instance_id: String) -> Dictionary:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return {}

	var catalog_id: String = card.get("catalog_id", "")
	var upgrades: Array = get_applied_upgrades(card_instance_id)
	var modifiers: Dictionary = {}

	# Combine all stat modifiers from applied upgrades (multiplicative)
	for upgrade_id: Variant in upgrades:
		if upgrade_id is String:
			var upgrade: Dictionary = CardUpgradeCatalog.get_upgrade(catalog_id, upgrade_id)
			var stat_mods: Dictionary = upgrade.get("stat_mods", {})
			for stat_key: Variant in stat_mods:
				if stat_key is String:
					var modifier_value: Variant = stat_mods[stat_key]
					if modifier_value is float or modifier_value is int:
						var mod_float: float = float(modifier_value)
						if modifiers.has(stat_key):
							modifiers[stat_key] *= mod_float
						else:
							modifiers[stat_key] = mod_float

	return modifiers

## =============================================================================
## QUERY HELPERS
## =============================================================================

## Get card progression info (for UI display)
func get_card_progression_info(card_instance_id: String) -> Dictionary:
	var card: Dictionary = ProfileRepo.get_card(card_instance_id)
	if card.is_empty():
		return {}

	var current_level: int = card.get("level", 1)
	var current_xp: int = card.get("xp", 0)
	var upgrades: Array = card.get("upgrades", [])
	var rarity: StringName = StringName(card.get("rarity", RarityIDs.COMMON))

	return {
		"card_instance_id": card_instance_id,
		"catalog_id": card.get("catalog_id", ""),
		"rarity": rarity,
		"rarity_multiplier": get_rarity_multiplier(rarity),
		"level": current_level,
		"max_level": MAX_LEVEL,
		"xp": current_xp,
		"xp_for_next_level": get_xp_for_level_with_rarity(current_level + 1, rarity) if current_level < MAX_LEVEL else 0,
		"xp_progress": get_level_progress(card_instance_id),
		"can_level_up": can_level_up(card_instance_id),
		"can_afford_level_up": can_afford_level_up(card_instance_id),
		"level_up_gold_cost": get_level_up_gold_cost(card_instance_id),
		"upgrades": upgrades,
		"is_max_level": current_level >= MAX_LEVEL
	}

## Get all cards that can level up
func get_cards_ready_to_level_up() -> Array[String]:
	var cards: Array = ProfileRepo.list_cards()
	var ready: Array[String] = []

	for card: Variant in cards:
		if card is Dictionary:
			var card_dict: Dictionary = card
			var card_id: String = card_dict.get("id", "")
			if can_level_up(card_id):
				ready.append(card_id)

	return ready

