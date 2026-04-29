extends GutTest

const SETTINGS_PATH: String = "user://debug_arena_settings.cfg"

var _panel_script: Script = load("res://scripts/battle/ui/debug/unit_spawner_panel.gd")


func before_each() -> void:
	_delete_settings_file()


func after_each() -> void:
	_delete_settings_file()


func test_c13_spawner_panel_contract_surface_exists() -> void:
	assert_not_null(_panel_script, "unit_spawner_panel.gd must exist")

	var panel: Object = _panel_script.new()
	if panel is Node:
		_track_node(panel)
	assert_true(panel.has_method("get_spawn_settings"), "C13: spawn settings getter contract")
	assert_true(panel.has_method("get_skip_prep_phase"), "C13: skip prep getter contract")
	assert_true(panel.has_method("get_enemy_ai_enabled"), "C13: enemy AI getter contract")
	assert_true(panel.has_method("get_player_ai_enabled"), "C13: player AI getter contract")


func test_c13_spawner_settings_round_trip_persists_across_instances() -> void:
	var panel: PanelContainer = _panel_script.new()
	_track_node(panel)
	panel._skip_prep_phase = true
	panel._enemy_ai_enabled = true
	panel._player_ai_enabled = false
	panel._spawn_mode = "paint"
	panel._burst_count = 6
	panel._formation_mode = "arc"
	panel._formation_spacing = 3.5
	panel._save_settings()

	var reloaded: PanelContainer = _panel_script.new()
	_track_node(reloaded)
	reloaded._load_settings()

	assert_true(reloaded.get_skip_prep_phase(), "skip prep should round-trip")
	assert_true(reloaded.get_enemy_ai_enabled(), "enemy AI toggle should round-trip")
	assert_false(reloaded.get_player_ai_enabled(), "player AI toggle should round-trip")

	var spawn_settings: Dictionary = reloaded.get_spawn_settings()
	assert_eq(spawn_settings.get("spawn_mode", ""), "paint")
	assert_eq(spawn_settings.get("burst_count", 0), 6)
	assert_eq(spawn_settings.get("formation_mode", ""), "arc")

	var spacing: float = float(spawn_settings.get("formation_spacing", 0.0))
	assert_true(absf(spacing - 3.5) < 0.001, "formation spacing should round-trip")


func test_c20_set_debug_deck_entries_overrides_loaded_debug_deck() -> void:
	var panel: PanelContainer = _panel_script.new()
	_track_node(panel)
	var injected_entries: Array = [
		{"catalog_id": "fire_wisp", "count": 2},
		{"catalog_id": "wind_cleave_unit", "count": 1}
	]
	panel.set_debug_deck_entries(injected_entries)

	var loaded: Array = panel._load_debug_deck()
	assert_eq(loaded.size(), 2)
	assert_eq(str(loaded[0].get("catalog_id", "")), "fire_wisp")
	assert_eq(int(loaded[0].get("count", 0)), 2)
	assert_eq(str(loaded[1].get("catalog_id", "")), "wind_cleave_unit")


func _delete_settings_file() -> void:
	if not FileAccess.file_exists(SETTINGS_PATH):
		return
	DirAccess.remove_absolute(ProjectSettings.globalize_path(SETTINGS_PATH))


func _track_node(node: Node) -> void:
	autoqfree(node)
