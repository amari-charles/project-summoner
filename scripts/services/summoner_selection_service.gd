extends Node
# SummonerSelectionService is registered as autoload "SummonerSelection", no class_name needed

## Summoner Selection Service - Active Summoner Management
##
## Manages which summoner is currently active across the game.
## Emits signals when summoner changes so UI can react.
##
## Usage:
##   SummonerSelection.get_active_summoner_id()
##   SummonerSelection.set_active_summoner(SummonerIDs.FIRE)
##   SummonerSelection.summoner_changed.connect(_on_summoner_changed)
##
## Active summoner affects:
##   - Which decks are visible (summoner-bound decks)
##   - Which campaign progress loads (per-summoner progress)
##   - Who receives XP from battles

## Signals
signal summoner_changed(old_summoner_id: String, new_summoner_id: String)
signal summoner_selection_blocked(reason: String)

## State tracking
var _is_in_battle: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect to battle state signals if available
	var game_state: Node = get_node_or_null("/root/GameStateEvents")
	if game_state and game_state.has_signal("battle_started"):
		game_state.battle_started.connect(_on_battle_started)
	if game_state and game_state.has_signal("battle_ended"):
		game_state.battle_ended.connect(_on_battle_ended)

## =============================================================================
## ACTIVE SUMMONER QUERIES
## =============================================================================

## Get the currently active summoner ID
func get_active_summoner_id() -> String:
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		return ""

	# Check for selected_summoner in meta
	var meta: Dictionary = profile.get("meta", {})
	var selected_summoner: String = meta.get("selected_summoner", "")
	if not selected_summoner.is_empty():
		return selected_summoner

	# Fallback: return first summoner instance
	var summoner_instances: Array = ProfileRepo.get_summoner_instances()
	if summoner_instances.size() > 0:
		var first_summoner: Dictionary = summoner_instances[0]
		return first_summoner.get("summoner_id", "")

	return ""

## Get the active summoner's config (from SummonerCatalog)
func get_active_summoner_config() -> SummonerConfig:
	var summoner_id: String = get_active_summoner_id()
	if summoner_id.is_empty():
		return null
	return SummonerCatalog.get_summoner_config(summoner_id)

## Get the active summoner's instance data (level, xp, modifiers)
func get_active_summoner_instance() -> Dictionary:
	var summoner_id: String = get_active_summoner_id()
	if summoner_id.is_empty():
		return {}
	return ProfileRepo.get_summoner_instance(summoner_id)

## Get list of all unlocked summoner IDs
func get_unlocked_summoner_ids() -> Array[String]:
	var summoner_instances: Array = ProfileRepo.get_summoner_instances()
	var ids: Array[String] = []
	for summoner_data: Variant in summoner_instances:
		if summoner_data is Dictionary:
			var summoner_dict: Dictionary = summoner_data
			var summoner_id: String = summoner_dict.get("summoner_id", "")
			if not summoner_id.is_empty():
				ids.append(summoner_id)
	return ids

## =============================================================================
## SUMMONER SWITCHING
## =============================================================================

## Check if summoner switching is currently allowed
func can_switch_summoner() -> bool:
	if _is_in_battle:
		return false
	return true

## Set the active summoner
## Returns true if successful, false if blocked or invalid
func set_active_summoner(summoner_id: String) -> bool:
	# Validate summoner ID
	if summoner_id.is_empty():
		push_warning("SummonerSelectionService: Cannot set empty summoner ID")
		return false

	# Check if summoner is unlocked
	if not ProfileRepo.is_summoner_unlocked(summoner_id):
		push_warning("SummonerSelectionService: Summoner not unlocked: %s" % summoner_id)
		return false

	# Check if switching is allowed
	if not can_switch_summoner():
		summoner_selection_blocked.emit(Loc.t("ui.summoner_panel.switch_blocked_battle"))
		return false

	# Get old summoner ID for signal
	var old_summoner_id: String = get_active_summoner_id()

	# No change needed
	if old_summoner_id == summoner_id:
		return true

	# Update profile meta
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		push_error("SummonerSelectionService: No active profile")
		return false

	if not profile.has("meta"):
		profile["meta"] = {}
	var meta: Dictionary = profile.get("meta", {})
	meta["selected_summoner"] = summoner_id
	ProfileRepo.save_profile(true)  # Immediate save

	# Emit signal for UI updates
	summoner_changed.emit(old_summoner_id, summoner_id)

	return true

## =============================================================================
## BATTLE STATE TRACKING
## =============================================================================

func _on_battle_started() -> void:
	_is_in_battle = true

func _on_battle_ended(_winner: int = 0) -> void:
	_is_in_battle = false

## Force set battle state (for testing or edge cases)
func set_in_battle(in_battle: bool) -> void:
	_is_in_battle = in_battle
