extends Node
# SummonerProgressionService is registered as autoload "SummonerProgression", no class_name needed

## Summoner Progression Service - XP and Level Management (GDScript wrapper for C#)
##
## Handles summoner XP gains and level-ups.
## UI and gameplay code should call this, never the repository directly.
## Delegates to C# SummonerProgressionServiceCS for core operations.
##
## Usage:
##   SummonerProgression.grant_summoner_xp(summoner_id, 50)
##   if SummonerProgression.can_level_up(summoner_id):
##       SummonerProgression.level_up_summoner(summoner_id)
##
## Emits signals for reactive UI updates.

## Constants
const MAX_LEVEL: int = 10

## XP thresholds for each level (cumulative XP needed)
## Note: Summoner leveling requires only XP, not gold.
const XP_THRESHOLDS: Array[int] = [
	0, 100, 250, 500, 850, 1300, 1900, 2700, 3800, 5200
]

## Signals
signal summoner_xp_changed(summoner_id: String, new_xp: int, new_level: int)
signal summoner_leveled_up(summoner_id: String, new_level: int)
signal summoner_ready_to_level_up(summoner_id: String)

## C# service reference
var _cs_service: Node

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("SummonerProgressionService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/SummonerProgressionCS")
	if _cs_service == null:
		push_error("SummonerProgressionService: SummonerProgressionCS autoload not found")
		return

	# Connect to C# service signals
	_cs_service.SummonerXpChanged.connect(_on_cs_xp_changed)
	_cs_service.SummonerLeveledUp.connect(_on_cs_leveled_up)
	_cs_service.SummonerReadyToLevelUp.connect(_on_cs_ready_to_level_up)

	# Inject active summoner getter
	_cs_service.SetActiveSummonerGetter(_get_active_summoner)

	print("SummonerProgressionService: Ready")

## Active summoner getter for C# service
func _get_active_summoner() -> String:
	return SummonerSelection.GetActiveSummonerId()

## =============================================================================
## XP OPERATIONS (delegated to C#)
## =============================================================================

## Grant XP to a summoner
## Returns the new total XP
func grant_summoner_xp(summoner_id: String, amount: int) -> int:
	if _cs_service == null:
		return 0
	return _cs_service.GrantSummonerXp(summoner_id, amount)

## Grant XP to the active summoner (convenience method)
## Returns the new total XP, or 0 if no active summoner
func grant_active_summoner_xp(amount: int) -> int:
	if _cs_service == null:
		return 0
	return _cs_service.GrantActiveSummonerXp(amount)

## Get XP required to reach a specific level
func get_xp_for_level(level: int) -> int:
	if level < 1 or level > MAX_LEVEL:
		return 0
	return XP_THRESHOLDS[level - 1]

## Get XP needed for the next level from current XP
func get_xp_to_next_level(summoner_id: String) -> int:
	if _cs_service == null:
		return 0
	return _cs_service.GetXpToNextLevel(summoner_id)

## Get progress towards next level as a percentage (0.0 - 1.0)
func get_level_progress(summoner_id: String) -> float:
	if _cs_service == null:
		return 0.0
	return _cs_service.GetLevelProgress(summoner_id)

## =============================================================================
## LEVEL-UP OPERATIONS (delegated to C#)
## =============================================================================

## Check if a summoner has enough XP to level up
func can_level_up(summoner_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.CanLevelUp(summoner_id)

## Level up a summoner (requires only XP threshold met - no gold cost)
## Returns true if successful
func level_up_summoner(summoner_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.LevelUpSummoner(summoner_id)

## =============================================================================
## QUERY HELPERS (delegated to C#)
## =============================================================================

## Get summoner progression info (for UI display)
func get_summoner_progression_info(summoner_id: String) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetSummonerProgressionInfo(summoner_id)

## Get active summoner's progression info (convenience method)
func get_active_summoner_progression_info() -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetActiveSummonerProgressionInfo()

## Get all summoners that can level up
func get_summoners_ready_to_level_up() -> Array[String]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetSummonersReadyToLevelUp()
	var typed_result: Array[String] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_xp_changed(summoner_id: String, new_xp: int, new_level: int) -> void:
	summoner_xp_changed.emit(summoner_id, new_xp, new_level)

func _on_cs_leveled_up(summoner_id: String, new_level: int) -> void:
	summoner_leveled_up.emit(summoner_id, new_level)

func _on_cs_ready_to_level_up(summoner_id: String) -> void:
	summoner_ready_to_level_up.emit(summoner_id)
