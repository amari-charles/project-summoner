extends Node
# SummonerProgressionService is registered as autoload "SummonerProgression", no class_name needed

## Summoner Progression Service - XP and Level Management
##
## Handles summoner XP gains and level-ups.
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   SummonerProgression.grant_summoner_xp(summoner_id, 50)
##   if SummonerProgression.can_level_up(summoner_id):
##       SummonerProgression.level_up_summoner(summoner_id)
##
## Emits signals for reactive UI updates.
##
## Phase 2: Foundation Only
## - XP and level tracking
## - Level-up mechanics (no trait selection yet)
## - Signals for UI updates
##
## Phase 3 will add: Level Traits, Trait Lines, trait selection UI
## Phase 4 will add: Ultimate Traits at level 10

## Constants
const MAX_LEVEL: int = 10

## XP thresholds for each level (cumulative XP needed)
## Index 0 = Level 1 (no XP needed), Index 1 = Level 2, etc.
## Pacing: 1-5 fast, 6-9 moderate, 10 major milestone
const XP_THRESHOLDS: Array[int] = [
	0,      # Level 1 (start)
	100,    # Level 2
	250,    # Level 3
	500,    # Level 4
	850,    # Level 5
	1300,   # Level 6
	1900,   # Level 7
	2700,   # Level 8
	3800,   # Level 9
	5200    # Level 10
]

## Gold cost per level-up (index = target level - 1)
## Summoners cost more than cards since they're more significant
const LEVEL_UP_GOLD_COST: Array[int] = [
	0,      # Level 1 (no cost)
	50,     # Level 2
	100,    # Level 3
	200,    # Level 4
	400,    # Level 5
	700,    # Level 6
	1000,   # Level 7
	1500,   # Level 8
	2000,   # Level 9
	3000    # Level 10
]

## Signals
signal summoner_xp_changed(summoner_id: String, new_xp: int, new_level: int)
signal summoner_leveled_up(summoner_id: String, new_level: int)
signal summoner_ready_to_level_up(summoner_id: String)

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	pass

## =============================================================================
## XP OPERATIONS
## =============================================================================

## Grant XP to a summoner
## Returns the new total XP
func grant_summoner_xp(summoner_id: String, amount: int) -> int:
	if amount <= 0:
		return 0

	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		push_warning("SummonerProgressionService: Summoner instance not found: %s" % summoner_id)
		return 0

	var current_xp: int = summoner_data.get("xp", 0)
	var current_level: int = summoner_data.get("level", 1)
	var new_xp: int = current_xp + amount

	# Create updated summoner instance
	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_data)
	if not summoner_instance:
		push_error("SummonerProgressionService: Failed to load summoner instance: %s" % summoner_id)
		return 0

	summoner_instance.xp = new_xp
	ProfileRepo.save_summoner_instance(summoner_instance)

	summoner_xp_changed.emit(summoner_id, new_xp, current_level)

	# Check if summoner can now level up
	if can_level_up(summoner_id):
		summoner_ready_to_level_up.emit(summoner_id)

	return new_xp

## Grant XP to the active summoner (convenience method)
## Returns the new total XP, or 0 if no active summoner
func grant_active_summoner_xp(amount: int) -> int:
	var active_summoner_id: String = _get_active_summoner_id()
	if active_summoner_id.is_empty():
		push_warning("SummonerProgressionService: No active summoner to grant XP to")
		return 0
	return grant_summoner_xp(active_summoner_id, amount)

## Get XP required to reach a specific level
func get_xp_for_level(level: int) -> int:
	if level < 1 or level > MAX_LEVEL:
		return 0
	return XP_THRESHOLDS[level - 1]

## Get XP needed for the next level from current XP
func get_xp_to_next_level(summoner_id: String) -> int:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		return 0

	var current_xp: int = summoner_data.get("xp", 0)
	var current_level: int = summoner_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	var next_level_xp: int = get_xp_for_level(current_level + 1)
	return maxi(0, next_level_xp - current_xp)

## Get progress towards next level as a percentage (0.0 - 1.0)
func get_level_progress(summoner_id: String) -> float:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		return 0.0

	var current_xp: int = summoner_data.get("xp", 0)
	var current_level: int = summoner_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return 1.0  # Max level

	var current_level_xp: int = get_xp_for_level(current_level)
	var next_level_xp: int = get_xp_for_level(current_level + 1)
	var level_range: int = next_level_xp - current_level_xp

	if level_range <= 0:
		return 1.0

	var progress_in_level: int = current_xp - current_level_xp
	return clampf(float(progress_in_level) / float(level_range), 0.0, 1.0)

## =============================================================================
## LEVEL-UP OPERATIONS
## =============================================================================

## Check if a summoner has enough XP to level up
func can_level_up(summoner_id: String) -> bool:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		return false

	var current_xp: int = summoner_data.get("xp", 0)
	var current_level: int = summoner_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return false  # Already max level

	var next_level_xp: int = get_xp_for_level(current_level + 1)
	return current_xp >= next_level_xp

## Get the gold cost to level up a summoner
func get_level_up_gold_cost(summoner_id: String) -> int:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		return 0

	var current_level: int = summoner_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	return LEVEL_UP_GOLD_COST[current_level]  # Index = current_level for next level cost

## Check if player can afford to level up (XP + gold)
func can_afford_level_up(summoner_id: String) -> bool:
	if not can_level_up(summoner_id):
		return false

	var gold_cost: int = get_level_up_gold_cost(summoner_id)
	return Economy.get_gold() >= gold_cost

## Level up a summoner (requires XP threshold met + gold)
## Phase 2: Simple level-up without trait selection
## Phase 3 will add: level_up_summoner(summoner_id, trait_id) for trait selection
## Returns true if successful
func level_up_summoner(summoner_id: String) -> bool:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		push_warning("SummonerProgressionService: Summoner not found: %s" % summoner_id)
		return false

	# Check XP requirement
	if not can_level_up(summoner_id):
		push_warning("SummonerProgressionService: Summoner does not have enough XP to level up")
		return false

	# Check gold cost
	var gold_cost: int = get_level_up_gold_cost(summoner_id)
	if not Economy.can_afford({"gold": gold_cost}):
		push_warning("SummonerProgressionService: Cannot afford level-up cost (%d gold)" % gold_cost)
		return false

	# TRANSACTION: Validate everything before making any changes
	# 1. Validate summoner instance can be loaded
	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_data)
	if not summoner_instance:
		push_error("SummonerProgressionService: Failed to load summoner instance: %s" % summoner_id)
		return false

	# 2. Validate new level is valid
	var current_level: int = summoner_data.get("level", 1)
	var new_level: int = current_level + 1
	if new_level > MAX_LEVEL:
		push_error("SummonerProgressionService: Cannot level beyond MAX_LEVEL")
		return false

	# All validations passed - now apply changes atomically
	# Spend gold first (this is the "point of no return")
	Economy.spend({"gold": gold_cost})

	# Apply level up and save
	summoner_instance.level = new_level
	var save_success: bool = ProfileRepo.save_summoner_instance(summoner_instance)

	if not save_success:
		# Refund gold if save failed
		push_error("SummonerProgressionService: Failed to save summoner instance, refunding gold")
		Economy.grant({"gold": gold_cost})
		return false

	summoner_leveled_up.emit(summoner_id, new_level)
	return true

## =============================================================================
## QUERY HELPERS
## =============================================================================

## Get summoner progression info (for UI display)
func get_summoner_progression_info(summoner_id: String) -> Dictionary:
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_data.is_empty():
		return {}

	var current_level: int = summoner_data.get("level", 1)
	var current_xp: int = summoner_data.get("xp", 0)

	return {
		"summoner_id": summoner_id,
		"level": current_level,
		"max_level": MAX_LEVEL,
		"xp": current_xp,
		"xp_for_current_level": get_xp_for_level(current_level),
		"xp_for_next_level": get_xp_for_level(current_level + 1) if current_level < MAX_LEVEL else 0,
		"xp_to_next_level": get_xp_to_next_level(summoner_id),
		"xp_progress": get_level_progress(summoner_id),
		"can_level_up": can_level_up(summoner_id),
		"can_afford_level_up": can_afford_level_up(summoner_id),
		"level_up_gold_cost": get_level_up_gold_cost(summoner_id),
		"is_max_level": current_level >= MAX_LEVEL
	}

## Get active summoner's progression info (convenience method)
func get_active_summoner_progression_info() -> Dictionary:
	var active_summoner_id: String = _get_active_summoner_id()
	if active_summoner_id.is_empty():
		return {}
	return get_summoner_progression_info(active_summoner_id)

## Get all summoners that can level up
func get_summoners_ready_to_level_up() -> Array[String]:
	var summoners: Array = ProfileRepo.get_summoner_instances()
	var ready: Array[String] = []

	for summoner_data: Variant in summoners:
		if summoner_data is Dictionary:
			var summoner_dict: Dictionary = summoner_data
			var summoner_id: String = summoner_dict.get("summoner_id", "")
			if can_level_up(summoner_id):
				ready.append(summoner_id)

	return ready

## =============================================================================
## PRIVATE HELPERS
## =============================================================================

## Get the active summoner ID from SummonerSelection service
func _get_active_summoner_id() -> String:
	return SummonerSelection.get_active_summoner_id()
