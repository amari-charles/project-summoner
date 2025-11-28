extends Node
# HeroSelectionService is registered as autoload "HeroSelection", no class_name needed

## Hero Selection Service - Active Hero Management
##
## Manages which hero is currently active across the game.
## Emits signals when hero changes so UI can react.
##
## Usage:
##   HeroSelection.get_active_hero_id()
##   HeroSelection.set_active_hero("hero_fire")
##   HeroSelection.hero_changed.connect(_on_hero_changed)
##
## Active hero affects:
##   - Which decks are visible (hero-bound decks)
##   - Which campaign progress loads (per-hero progress)
##   - Who receives XP from battles

## Signals
signal hero_changed(old_hero_id: String, new_hero_id: String)
signal hero_selection_blocked(reason: String)

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
## ACTIVE HERO QUERIES
## =============================================================================

## Get the currently active hero ID
func get_active_hero_id() -> String:
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		return ""

	# Check for selected_hero in meta
	var meta: Dictionary = profile.get("meta", {})
	var selected_hero: String = meta.get("selected_hero", "")
	if not selected_hero.is_empty():
		return selected_hero

	# Fallback: return first hero instance
	var hero_instances: Array = ProfileRepo.get_hero_instances()
	if hero_instances.size() > 0:
		var first_hero: Dictionary = hero_instances[0]
		return first_hero.get("hero_id", "")

	return ""

## Get the active hero's config (from HeroCatalog)
func get_active_hero_config() -> HeroConfig:
	var hero_id: String = get_active_hero_id()
	if hero_id.is_empty():
		return null
	return HeroCatalog.get_hero_config(hero_id)

## Get the active hero's instance data (level, xp, modifiers)
func get_active_hero_instance() -> Dictionary:
	var hero_id: String = get_active_hero_id()
	if hero_id.is_empty():
		return {}
	return ProfileRepo.get_hero_instance(hero_id)

## Get list of all unlocked hero IDs
func get_unlocked_hero_ids() -> Array[String]:
	var hero_instances: Array = ProfileRepo.get_hero_instances()
	var ids: Array[String] = []
	for hero_data: Variant in hero_instances:
		if hero_data is Dictionary:
			var hero_dict: Dictionary = hero_data
			var hero_id: String = hero_dict.get("hero_id", "")
			if not hero_id.is_empty():
				ids.append(hero_id)
	return ids

## =============================================================================
## HERO SWITCHING
## =============================================================================

## Check if hero switching is currently allowed
func can_switch_hero() -> bool:
	if _is_in_battle:
		return false
	return true

## Set the active hero
## Returns true if successful, false if blocked or invalid
func set_active_hero(hero_id: String) -> bool:
	# Validate hero ID
	if hero_id.is_empty():
		push_warning("HeroSelectionService: Cannot set empty hero ID")
		return false

	# Check if hero is unlocked
	if not ProfileRepo.is_hero_unlocked(hero_id):
		push_warning("HeroSelectionService: Hero not unlocked: %s" % hero_id)
		return false

	# Check if switching is allowed
	if not can_switch_hero():
		hero_selection_blocked.emit(Loc.t("ui.hero_panel.switch_blocked_battle"))
		return false

	# Get old hero ID for signal
	var old_hero_id: String = get_active_hero_id()

	# No change needed
	if old_hero_id == hero_id:
		return true

	# Update profile meta
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		push_error("HeroSelectionService: No active profile")
		return false

	if not profile.has("meta"):
		profile["meta"] = {}
	var meta: Dictionary = profile.get("meta", {})
	meta["selected_hero"] = hero_id
	ProfileRepo.save_profile(true)  # Immediate save

	# Emit signal for UI updates
	hero_changed.emit(old_hero_id, hero_id)

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
