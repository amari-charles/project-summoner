extends Node
# SummonerSelectionService is registered as autoload "SummonerSelection", no class_name needed

## Summoner Selection Service - Active Summoner Management (GDScript wrapper for C#)
##
## Manages which summoner is currently active across the game.
## Emits signals when summoner changes so UI can react.
## Delegates to C# SummonerSelectionServiceCS for core operations.
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

## C# service reference
var _cs_service: Node

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("SummonerSelectionService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/SummonerSelectionCS")
	if _cs_service == null:
		push_error("SummonerSelectionService: SummonerSelectionCS autoload not found")
		return

	# Connect to C# service signals
	_cs_service.SummonerChanged.connect(_on_cs_summoner_changed)
	_cs_service.SummonerSelectionBlocked.connect(_on_cs_selection_blocked)

	# Connect to battle state signals (GDScript-specific)
	GameStateEvents.battle_started.connect(_on_battle_started)
	GameStateEvents.battle_ended.connect(_on_battle_ended)

	print("SummonerSelectionService: Ready")

## =============================================================================
## ACTIVE SUMMONER QUERIES (delegated to C#)
## =============================================================================

## Get the currently active summoner ID
func get_active_summoner_id() -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GetActiveSummonerId()

## Get the active summoner's config (from SummonerCatalog - GDScript only)
func get_active_summoner_config() -> SummonerConfig:
	var summoner_id: String = get_active_summoner_id()
	if summoner_id.is_empty():
		return null
	return SummonerCatalog.get_summoner_config(summoner_id)

## Get the active summoner's instance data (level, xp, modifiers)
func get_active_summoner_instance() -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetActiveSummonerInstanceDict()

## Get list of all unlocked summoner IDs
func get_unlocked_summoner_ids() -> Array[String]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetUnlockedSummonerIdsArray()
	var typed_result: Array[String] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## SUMMONER SWITCHING (delegated to C#)
## =============================================================================

## Check if summoner switching is currently allowed
func can_switch_summoner() -> bool:
	if _cs_service == null:
		return false
	return _cs_service.CanSwitchSummoner()

## Set the active summoner
## Returns true if successful, false if blocked or invalid
func set_active_summoner(summoner_id: String) -> bool:
	if _cs_service == null:
		return false

	# Pass localized blocked message to C# service
	var blocked_reason: String = Loc.t("ui.summoner_panel.switch_blocked_battle")
	return _cs_service.SetActiveSummoner(summoner_id, blocked_reason)

## =============================================================================
## BATTLE STATE TRACKING (GDScript-specific, forwards to C#)
## =============================================================================

func _on_battle_started() -> void:
	if _cs_service:
		_cs_service.SetInBattle(true)

func _on_battle_ended(_winner: int = 0) -> void:
	if _cs_service:
		_cs_service.SetInBattle(false)

## Force set battle state (for testing or edge cases)
func set_in_battle(in_battle: bool) -> void:
	if _cs_service:
		_cs_service.SetInBattle(in_battle)

## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_summoner_changed(old_summoner_id: String, new_summoner_id: String) -> void:
	summoner_changed.emit(old_summoner_id, new_summoner_id)

func _on_cs_selection_blocked(reason: String) -> void:
	summoner_selection_blocked.emit(reason)
