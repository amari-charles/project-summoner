extends Node
# HeroProgressionService is registered as autoload "HeroProgression", no class_name needed

## Hero Progression Service - XP and Level Management
##
## Handles hero XP gains and level-ups.
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   HeroProgression.grant_hero_xp(hero_id, 50)
##   if HeroProgression.can_level_up(hero_id):
##       HeroProgression.level_up_hero(hero_id)
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
## Heroes cost more than cards since they're more significant
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
signal hero_xp_changed(hero_id: String, new_xp: int, new_level: int)
signal hero_leveled_up(hero_id: String, new_level: int)
signal hero_ready_to_level_up(hero_id: String)

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	pass

## =============================================================================
## XP OPERATIONS
## =============================================================================

## Grant XP to a hero
## Returns the new total XP
func grant_hero_xp(hero_id: String, amount: int) -> int:
	if amount <= 0:
		return 0

	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		push_warning("HeroProgressionService: Hero instance not found: %s" % hero_id)
		return 0

	var current_xp: int = hero_data.get("xp", 0)
	var current_level: int = hero_data.get("level", 1)
	var new_xp: int = current_xp + amount

	# Create updated hero instance
	var hero_instance: HeroInstance = HeroInstance.from_dict(hero_data)
	if not hero_instance:
		push_error("HeroProgressionService: Failed to load hero instance: %s" % hero_id)
		return 0

	hero_instance.xp = new_xp
	ProfileRepo.save_hero_instance(hero_instance)

	hero_xp_changed.emit(hero_id, new_xp, current_level)

	# Check if hero can now level up
	if can_level_up(hero_id):
		hero_ready_to_level_up.emit(hero_id)

	return new_xp

## Grant XP to the active hero (convenience method)
## Returns the new total XP, or 0 if no active hero
func grant_active_hero_xp(amount: int) -> int:
	var active_hero_id: String = _get_active_hero_id()
	if active_hero_id.is_empty():
		push_warning("HeroProgressionService: No active hero to grant XP to")
		return 0
	return grant_hero_xp(active_hero_id, amount)

## Get XP required to reach a specific level
func get_xp_for_level(level: int) -> int:
	if level < 1 or level > MAX_LEVEL:
		return 0
	return XP_THRESHOLDS[level - 1]

## Get XP needed for the next level from current XP
func get_xp_to_next_level(hero_id: String) -> int:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		return 0

	var current_xp: int = hero_data.get("xp", 0)
	var current_level: int = hero_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	var next_level_xp: int = get_xp_for_level(current_level + 1)
	return maxi(0, next_level_xp - current_xp)

## Get progress towards next level as a percentage (0.0 - 1.0)
func get_level_progress(hero_id: String) -> float:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		return 0.0

	var current_xp: int = hero_data.get("xp", 0)
	var current_level: int = hero_data.get("level", 1)

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

## Check if a hero has enough XP to level up
func can_level_up(hero_id: String) -> bool:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		return false

	var current_xp: int = hero_data.get("xp", 0)
	var current_level: int = hero_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return false  # Already max level

	var next_level_xp: int = get_xp_for_level(current_level + 1)
	return current_xp >= next_level_xp

## Get the gold cost to level up a hero
func get_level_up_gold_cost(hero_id: String) -> int:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		return 0

	var current_level: int = hero_data.get("level", 1)

	if current_level >= MAX_LEVEL:
		return 0  # Already max level

	return LEVEL_UP_GOLD_COST[current_level]  # Index = current_level for next level cost

## Check if player can afford to level up (XP + gold)
func can_afford_level_up(hero_id: String) -> bool:
	if not can_level_up(hero_id):
		return false

	var gold_cost: int = get_level_up_gold_cost(hero_id)
	return Economy.get_gold() >= gold_cost

## Level up a hero (requires XP threshold met + gold)
## Phase 2: Simple level-up without trait selection
## Phase 3 will add: level_up_hero(hero_id, trait_id) for trait selection
## Returns true if successful
func level_up_hero(hero_id: String) -> bool:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		push_warning("HeroProgressionService: Hero not found: %s" % hero_id)
		return false

	# Check XP requirement
	if not can_level_up(hero_id):
		push_warning("HeroProgressionService: Hero does not have enough XP to level up")
		return false

	# Check gold cost
	var gold_cost: int = get_level_up_gold_cost(hero_id)
	if not Economy.can_afford({"gold": gold_cost}):
		push_warning("HeroProgressionService: Cannot afford level-up cost (%d gold)" % gold_cost)
		return false

	# TRANSACTION: Validate everything before making any changes
	# 1. Validate hero instance can be loaded
	var hero_instance: HeroInstance = HeroInstance.from_dict(hero_data)
	if not hero_instance:
		push_error("HeroProgressionService: Failed to load hero instance: %s" % hero_id)
		return false

	# 2. Validate new level is valid
	var current_level: int = hero_data.get("level", 1)
	var new_level: int = current_level + 1
	if new_level > MAX_LEVEL:
		push_error("HeroProgressionService: Cannot level beyond MAX_LEVEL")
		return false

	# All validations passed - now apply changes atomically
	# Spend gold first (this is the "point of no return")
	Economy.spend({"gold": gold_cost})

	# Apply level up and save
	hero_instance.level = new_level
	var save_success: bool = ProfileRepo.save_hero_instance(hero_instance)

	if not save_success:
		# Refund gold if save failed
		push_error("HeroProgressionService: Failed to save hero instance, refunding gold")
		Economy.grant({"gold": gold_cost})
		return false

	hero_leveled_up.emit(hero_id, new_level)
	return true

## =============================================================================
## QUERY HELPERS
## =============================================================================

## Get hero progression info (for UI display)
func get_hero_progression_info(hero_id: String) -> Dictionary:
	var hero_data: Dictionary = ProfileRepo.get_hero_instance(hero_id)
	if hero_data.is_empty():
		return {}

	var current_level: int = hero_data.get("level", 1)
	var current_xp: int = hero_data.get("xp", 0)

	return {
		"hero_id": hero_id,
		"level": current_level,
		"max_level": MAX_LEVEL,
		"xp": current_xp,
		"xp_for_current_level": get_xp_for_level(current_level),
		"xp_for_next_level": get_xp_for_level(current_level + 1) if current_level < MAX_LEVEL else 0,
		"xp_to_next_level": get_xp_to_next_level(hero_id),
		"xp_progress": get_level_progress(hero_id),
		"can_level_up": can_level_up(hero_id),
		"can_afford_level_up": can_afford_level_up(hero_id),
		"level_up_gold_cost": get_level_up_gold_cost(hero_id),
		"is_max_level": current_level >= MAX_LEVEL
	}

## Get active hero's progression info (convenience method)
func get_active_hero_progression_info() -> Dictionary:
	var active_hero_id: String = _get_active_hero_id()
	if active_hero_id.is_empty():
		return {}
	return get_hero_progression_info(active_hero_id)

## Get all heroes that can level up
func get_heroes_ready_to_level_up() -> Array[String]:
	var heroes: Array = ProfileRepo.get_hero_instances()
	var ready: Array[String] = []

	for hero_data: Variant in heroes:
		if hero_data is Dictionary:
			var hero_dict: Dictionary = hero_data
			var hero_id: String = hero_dict.get("hero_id", "")
			if can_level_up(hero_id):
				ready.append(hero_id)

	return ready

## =============================================================================
## PRIVATE HELPERS
## =============================================================================

## Get the active hero ID from HeroSelection service
func _get_active_hero_id() -> String:
	var hero_selection: Node = get_node_or_null("/root/HeroSelection")
	if hero_selection and hero_selection.has_method("get_active_hero_id"):
		return hero_selection.get_active_hero_id()
	return ""
