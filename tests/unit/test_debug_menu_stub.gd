extends GutTest

const SETTINGS_PATH: String = "user://debug_menu_settings.cfg"

const DEBUG_ARENA_PRESETS = preload("res://scripts/debug/debug_arena_menu_presets.gd")

var _menu_script: Script = load("res://scripts/debug/debug_menu.gd")


func before_each() -> void:
	_delete_settings_file()


func after_each() -> void:
	_delete_settings_file()


func test_c14_c17_debug_menu_contract_surface_exists() -> void:
	assert_not_null(_menu_script, "debug_menu.gd must exist")

	var menu: Object = _menu_script.new()
	if menu is Node:
		_track_owned_node(menu)
	assert_true(menu.has_method("_on_skip_prep_pressed"), "C14: battle utility hook")
	assert_true(menu.has_method("_on_win_pressed"), "C14: win hook")
	assert_true(menu.has_method("_on_lose_pressed"), "C14: lose hook")
	assert_true(menu.has_method("_on_debug_arena_battle_pressed"), "C14: arena quick launch hook")
	assert_true(menu.has_method("_on_hurtbox_toggle_pressed"), "C15: visualization toggle hook")
	assert_true(menu.has_method("_on_projectile_hit_geometry_toggle_pressed"), "C15: projectile toggle hook")
	assert_true(menu.has_method("_on_command_submitted"), "C16: console submit hook")
	assert_true(menu.has_method("_on_camera_overlay_toggle_pressed"), "C17: camera overlay hook")
	assert_true(menu.has_method("_on_camera_auto_log_toggle_pressed"), "C17: camera auto-log hook")
	assert_true(menu.has_method("_on_camera_zoom_solver_log_toggle_pressed"), "C17: zoom solver hook")
	assert_true(menu.has_method("_build_debug_arena_buttons"), "C21: preset-driven arena list hook")


func test_c21_debug_menu_preset_catalog_contract_exists() -> void:
	var presets_script: Script = load("res://scripts/debug/debug_arena_menu_presets.gd")
	assert_not_null(presets_script, "debug_arena_menu_presets.gd must exist")

	var default_id: String = DEBUG_ARENA_PRESETS.get_default_preset_id()
	assert_true(not default_id.is_empty(), "C21: default preset id should be defined")

	var entries: Array[Dictionary] = DEBUG_ARENA_PRESETS.get_preset_entries(default_id)
	assert_true(entries.size() > 0, "C21: default preset should have at least one entry")


func test_c21_build_debug_arena_buttons_uses_selected_preset_entries() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	menu._arena_preset_id = "new_cards_only"
	var grid: GridContainer = GridContainer.new()
	_track_owned_node(grid)

	menu._build_debug_arena_buttons(grid)

	assert_eq(grid.get_child_count(), 2, "new_cards_only should render exactly two arena buttons")
	var labels: Array[String] = []
	for child_var: Variant in grid.get_children():
		if child_var is Button:
			var button: Button = child_var
			labels.append(button.text)
	labels.sort()
	assert_eq(labels, ["Fire Wisp", "Wind + Earth New"])


func test_c14_quick_tab_omits_legacy_test_arena_map_button() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	var quick_tab: VBoxContainer = VBoxContainer.new()
	_track_owned_node(quick_tab)

	menu._build_quick_tab(quick_tab)

	var button_labels: Array[String] = []
	for child_var: Variant in quick_tab.get_children():
		if child_var is Button:
			var button: Button = child_var
			button_labels.append(button.text)

	assert_false("Open Test Arena Map" in button_labels, "obsolete map chooser must stay removed")
	assert_false("Launch Roster Debug Arena" in button_labels, "main debug tab should not force a specific roster battle")


func test_c21_selecting_preset_rebuilds_button_list_and_persists() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	menu._arena_preset_dropdown = OptionButton.new()
	_track_owned_node(menu._arena_preset_dropdown)
	menu._arena_button_grid = GridContainer.new()
	_track_owned_node(menu._arena_button_grid)
	menu._arena_preset_id = "all_test_arena"

	menu._populate_arena_preset_dropdown()
	menu._build_debug_arena_buttons(menu._arena_button_grid)
	var all_entries: Array[Dictionary] = DEBUG_ARENA_PRESETS.get_preset_entries("all_test_arena")
	assert_eq(
		menu._arena_button_grid.get_child_count(),
		all_entries.size(),
		"all_test_arena should render one button per preset entry"
	)

	var preset_index: int = -1
	for i: int in menu._arena_preset_dropdown.item_count:
		var preset_id: String = str(menu._arena_preset_dropdown.get_item_metadata(i))
		if preset_id == "new_cards_only":
			preset_index = i
			break

	assert_true(preset_index != -1, "new_cards_only preset should exist in dropdown")
	menu._on_arena_preset_selected(preset_index)

	assert_eq(menu._arena_preset_id, "new_cards_only")
	assert_eq(menu._arena_button_grid.get_child_count(), 2, "selected preset should rebuild button list")

	var reloaded: Node = _menu_script.new()
	_track_owned_node(reloaded)
	reloaded._load_settings()
	assert_eq(reloaded._arena_preset_id, "new_cards_only", "selected preset should persist in settings")


func test_debug_arena_biome_dropdown_lists_registered_biomes_and_persists_selection() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	menu._arena_biome_dropdown = OptionButton.new()
	_track_owned_node(menu._arena_biome_dropdown)

	menu._populate_arena_biome_dropdown()
	assert_eq(menu._arena_biome_dropdown.item_count, BiomeIDs.ALL_BIOMES.size())

	var island_index: int = -1
	for i: int in menu._arena_biome_dropdown.item_count:
		var biome_id: String = str(menu._arena_biome_dropdown.get_item_metadata(i))
		if biome_id == String(BiomeIDs.ISLAND_WATER):
			island_index = i
			break

	assert_true(island_index != -1, "island water should be available in the biome dropdown")
	menu._on_arena_biome_selected(island_index)
	assert_eq(menu._arena_biome_id, BiomeIDs.ISLAND_WATER)

	var reloaded: Node = _menu_script.new()
	_track_owned_node(reloaded)
	reloaded._load_settings()
	assert_eq(reloaded._arena_biome_id, BiomeIDs.ISLAND_WATER, "selected biome should persist")


func test_c14_skip_win_lose_controls_trigger_game_controller_methods() -> void:
	var menu: Node = _menu_script.new()
	_add_root_node(menu)
	var controller: _FakeGameController = _FakeGameController.new()
	_add_root_node(controller)
	controller.add_to_group(GroupIDs.GAME_CONTROLLER)

	menu._on_skip_prep_pressed()
	menu._on_win_pressed()
	menu._on_lose_pressed()

	assert_true(controller.skip_called, "skip prep should call game controller")
	assert_eq(controller.end_calls.size(), 2, "win and lose should each call EndGame")


func test_c14_battle_launch_uses_expected_routing_hooks() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	var harness: _DebugMenuHarness = _DebugMenuHarness.new()
	menu._battle_getter_override = Callable(harness, "get_battle")
	menu._scene_transition_override = Callable(harness, "transition_to")
	menu._progression_start_override = Callable(harness, "start_progression")
	menu._battle_context_configure_override = Callable(harness, "configure_battle_context")
	menu._battle_context_biome_setter_override = Callable(harness, "set_battle_context_biome")
	menu._arena_biome_id = BiomeIDs.ISLAND_WATER

	menu._on_debug_arena_battle_pressed("arena_fire_wisp")
	assert_eq(harness.last_attempt_battle_id, "arena_fire_wisp")
	assert_eq(harness.last_context_battle_id, "arena_fire_wisp")
	assert_eq(harness.last_biome_id, String(BiomeIDs.ISLAND_WATER))
	assert_eq(harness.last_transition_scene, SceneManager.SCENE_DEBUG_ARENA)


func test_c15_visualization_toggles_and_persistence_round_trip() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	var service: _FakeDebugService = _FakeDebugService.new()
	_track_owned_node(service)
	menu._unit_debug = service
	menu._battlefield_debug_service_override = service
	menu._spawn_boundary_button = Button.new()
	_track_owned_node(menu._spawn_boundary_button)

	menu._on_hurtbox_toggle_pressed()
	menu._on_target_point_toggle_pressed()
	menu._on_attack_range_toggle_pressed()
	menu._on_damage_shape_toggle_pressed()
	menu._on_navigation_footprint_toggle_pressed()
	menu._on_projectile_hit_geometry_toggle_pressed()
	menu._on_summoner_bubble_toggle_pressed()
	menu._on_spawn_boundary_toggle_pressed()

	assert_true(service.hurtbox_enabled)
	assert_true(service.target_point_enabled)
	assert_true(service.engage_range_enabled)
	assert_true(service.damage_shape_enabled)
	assert_true(service.navigation_footprint_enabled)
	assert_true(service.projectile_geometry_enabled)
	assert_true(service.summoner_bubble_enabled)
	assert_true(service.spawn_boundary_bypass_enabled)

	var reloaded_menu: Node = _menu_script.new()
	_track_owned_node(reloaded_menu)
	var reloaded_service: _FakeDebugService = _FakeDebugService.new()
	_track_owned_node(reloaded_service)
	reloaded_menu._unit_debug = reloaded_service
	reloaded_menu._battlefield_debug_service_override = reloaded_service
	reloaded_menu._load_settings()

	assert_true(reloaded_service.hurtbox_enabled)
	assert_true(reloaded_service.target_point_enabled)
	assert_true(reloaded_service.engage_range_enabled)
	assert_true(reloaded_service.damage_shape_enabled)
	assert_true(reloaded_service.navigation_footprint_enabled)
	assert_true(reloaded_service.projectile_geometry_enabled)
	assert_true(reloaded_service.summoner_bubble_enabled)
	assert_true(reloaded_menu._bypass_spawn_boundary)


func test_c16_console_submit_and_autocomplete_flow_behaves_as_expected() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	var harness: _DebugMenuHarness = _DebugMenuHarness.new()
	menu._console_execute_override = Callable(harness, "execute_console")
	menu._console_all_commands_override = Callable(harness, "all_console_commands")
	menu._console_matching_commands_override = Callable(harness, "matching_console_commands")
	menu._command_input = LineEdit.new()
	_track_owned_node(menu._command_input)
	menu._command_output = Label.new()
	_track_owned_node(menu._command_output)
	menu._autocomplete_list = ItemList.new()
	_track_owned_node(menu._autocomplete_list)

	menu._update_autocomplete("/")
	assert_true(menu._autocomplete_visible, "autocomplete should be visible for root query")
	assert_eq(menu._autocomplete_list.item_count, 2)

	menu._on_command_submitted("/ok")
	assert_eq(harness.last_console_command, "/ok")
	assert_eq(menu._command_output.text, "OK: /ok")
	assert_eq(menu._command_input.text, "")

	menu._on_command_submitted("/bad")
	assert_eq(harness.last_console_command, "/bad")
	assert_eq(menu._command_output.text, "Failed: /bad")

	menu._on_command_text_changed("/m")
	assert_eq(menu._autocomplete_list.item_count, 1)
	menu._accept_autocomplete()
	assert_eq(menu._command_input.text, "/mock")


func test_c17_camera_overlay_auto_log_and_zoom_solver_toggles_work() -> void:
	var menu: Node = _menu_script.new()
	_track_owned_node(menu)
	var camera: _FakeCameraController = _FakeCameraController.new()
	_track_owned_node(camera)
	menu._camera_controller_override = camera
	menu._camera_overlay_button = Button.new()
	_track_owned_node(menu._camera_overlay_button)
	menu._camera_auto_log_button = Button.new()
	_track_owned_node(menu._camera_auto_log_button)
	menu._camera_zoom_solver_log_button = Button.new()
	_track_owned_node(menu._camera_zoom_solver_log_button)

	menu._refresh_camera_overlay_button_state()
	assert_eq(menu._camera_overlay_button.text, "Camera Overlay: Off")
	menu._on_camera_overlay_toggle_pressed()
	assert_true(camera.debug_show_pan_bounds_overlay)

	menu._refresh_camera_zoom_solver_log_button_state()
	assert_eq(menu._camera_zoom_solver_log_button.text, "Zoom Solver Logs: Off")
	menu._on_camera_zoom_solver_log_toggle_pressed()
	assert_true(camera.debug_log_zoom_solver)

	menu._on_camera_auto_log_toggle_pressed()
	assert_true(menu._camera_auto_log_enabled)
	var reloaded: Node = _menu_script.new()
	_track_owned_node(reloaded)
	reloaded._load_settings()
	assert_true(reloaded._camera_auto_log_enabled)


func _delete_settings_file() -> void:
	if not FileAccess.file_exists(SETTINGS_PATH):
		return
	DirAccess.remove_absolute(ProjectSettings.globalize_path(SETTINGS_PATH))


func _add_root_node(node: Node) -> void:
	get_tree().root.add_child(node)
	autoqfree(node)


func _track_owned_node(node: Node) -> void:
	autoqfree(node)


class _FakeGameController extends Node:
	var skip_called: bool = false
	var end_calls: Array[int] = []

	func SkipPrepPhase() -> void:
		skip_called = true

	func EndGame(team: int) -> void:
		end_calls.append(team)


class _DebugMenuHarness extends RefCounted:
	var last_transition_scene: String = ""
	var last_attempt_battle_id: String = ""
	var last_context_battle_id: String = ""
	var last_biome_id: String = ""
	var last_console_command: String = ""

	func get_battle(_battle_id: String) -> Dictionary:
		return {"runtime_surface": "debug_arena"}

	func transition_to(scene_path: String) -> void:
		last_transition_scene = scene_path

	func start_progression(battle_id: String) -> Dictionary:
		last_attempt_battle_id = battle_id
		return {"is_success": true, "attempt_id": "debug-attempt"}

	func configure_battle_context(battle_id: String) -> void:
		last_context_battle_id = battle_id

	func set_battle_context_biome(biome_id: String) -> void:
		last_biome_id = biome_id

	func execute_console(command: String) -> bool:
		last_console_command = command
		return command == "/ok"

	func all_console_commands() -> Array:
		return [
			{"cmd": "/ok", "args": "", "desc": "ok"},
			{"cmd": "/mock", "args": "", "desc": "mock"}
		]

	func matching_console_commands(text: String) -> Array:
		if text.begins_with("/m"):
			return [{"cmd": "/mock", "args": "", "desc": "mock"}]
		return []


class _FakeDebugService extends Node:
	var hurtbox_enabled: bool = false
	var target_point_enabled: bool = false
	var engage_range_enabled: bool = false
	var damage_shape_enabled: bool = false
	var navigation_footprint_enabled: bool = false
	var projectile_geometry_enabled: bool = false
	var summoner_bubble_enabled: bool = false
	var spawn_boundary_bypass_enabled: bool = false

	func ToggleDebugHurtbox() -> void:
		hurtbox_enabled = not hurtbox_enabled

	func ToggleDebugTargetPoint() -> void:
		target_point_enabled = not target_point_enabled

	func ToggleDebugEngageRange() -> void:
		engage_range_enabled = not engage_range_enabled

	func ToggleDebugDamageShape() -> void:
		damage_shape_enabled = not damage_shape_enabled

	func ToggleDebugNavigationFootprint() -> void:
		navigation_footprint_enabled = not navigation_footprint_enabled

	func ToggleDebugProjectileHitGeometry() -> void:
		projectile_geometry_enabled = not projectile_geometry_enabled

	func ToggleDebugSummonerBubble() -> void:
		summoner_bubble_enabled = not summoner_bubble_enabled

	func IsDebugHurtboxEnabled() -> bool:
		return hurtbox_enabled

	func IsDebugTargetPointEnabled() -> bool:
		return target_point_enabled

	func IsDebugEngageRangeEnabled() -> bool:
		return engage_range_enabled

	func IsDebugDamageShapeEnabled() -> bool:
		return damage_shape_enabled

	func IsDebugNavigationFootprintEnabled() -> bool:
		return navigation_footprint_enabled

	func IsDebugProjectileHitGeometryEnabled() -> bool:
		return projectile_geometry_enabled

	func IsDebugSummonerBubbleEnabled() -> bool:
		return summoner_bubble_enabled

	func SetDebugHurtboxEnabled(enabled: bool) -> void:
		hurtbox_enabled = enabled

	func SetDebugTargetPointEnabled(enabled: bool) -> void:
		target_point_enabled = enabled

	func SetDebugEngageRangeEnabled(enabled: bool) -> void:
		engage_range_enabled = enabled

	func SetDebugDamageShapeEnabled(enabled: bool) -> void:
		damage_shape_enabled = enabled

	func SetDebugNavigationFootprintEnabled(enabled: bool) -> void:
		navigation_footprint_enabled = enabled

	func SetDebugProjectileHitGeometryEnabled(enabled: bool) -> void:
		projectile_geometry_enabled = enabled

	func SetDebugSummonerBubbleEnabled(enabled: bool) -> void:
		summoner_bubble_enabled = enabled

	func SetSpawnBoundaryBypassEnabled(enabled: bool) -> void:
		spawn_boundary_bypass_enabled = enabled

	func IsSpawnBoundaryBypassEnabled() -> bool:
		return spawn_boundary_bypass_enabled


class _FakeCameraController extends Node:
	var debug_show_pan_bounds_overlay: bool = false
	var debug_log_zoom_solver: bool = false

	func get_clamp_diagnostics() -> Dictionary:
		return {
			"view_bounds_xz": Rect2(Vector2.ZERO, Vector2.ONE),
			"map_bounds_xz": Rect2(Vector2.ZERO, Vector2.ONE),
			"horizontal_mode": "fit",
			"vertical_mode": "fit"
		}
