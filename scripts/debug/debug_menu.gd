extends Node
# DebugMenu is registered as an autoload, no class_name needed

## Debug Menu - Development utility panel for testing and debugging
##
## Provides on-screen controls for debugging and testing.
## Only active in debug builds - automatically disabled in release.
##
## Toggle UI: ` (backtick) or F12
## FPS Hotkeys (work even when UI hidden):
##   F5 - Set to 30 FPS (low-end mobile simulation)
##   F6 - Set to 60 FPS (standard)
##   F7 - Set to 120 FPS (high refresh rate)
##   F8 - Uncapped FPS
##   F9 - Toggle projectile hit radius visualization

const SETTINGS_PATH: String = "user://debug_menu_settings.cfg"
const ENABLE_FLAG: String = "--enable-debug-menu"
const DISABLE_FLAG: String = "--disable-debug-menu"
const DEFAULT_ARENA_PRESET_ID: String = "all_test_arena"
const EXPERIMENTAL_ROOMS_PRESET_ID: String = "experimental_rooms"
const DEBUG_ARENA_PRESETS = preload("res://scripts/debug/debug_arena_menu_presets.gd")

## UI references
var _panel: PanelContainer
var _tabs: TabContainer
var _fps_label: Label
var _target_label: Label
var _buttons: Dictionary = {}  # fps -> Button
var _skip_prep_button: Button
var _hurtbox_button: Button
var _target_point_button: Button
var _attack_range_button: Button
var _damage_shape_button: Button
var _navigation_footprint_button: Button
var _projectile_hit_geometry_button: Button
var _summoner_bubble_button: Button
var _ability_logs_button: Button
var _spawn_boundary_button: Button
var _camera_overlay_button: Button
var _camera_auto_log_button: Button
var _camera_zoom_solver_log_button: Button
var _bypass_spawn_boundary: bool = false  # Local state (formerly in SpatialGrid autoload)
var _unit_debug: Node
var _command_input: LineEdit  # Console command input
var _command_output: Label  # Console command output
var _autocomplete_list: ItemList  # Autocomplete suggestions
var _autocomplete_visible: bool = false
var _camera_auto_log_enabled: bool = false
var _camera_auto_log_elapsed: float = 0.0
var _menu_enabled: bool = false
var _arena_preset_id: String = DEFAULT_ARENA_PRESET_ID
var _arena_biome_id: StringName = BiomeIDs.DEFAULT
var _arena_preset_dropdown: OptionButton
var _arena_biome_dropdown: OptionButton
var _arena_button_grid: GridContainer
var _battlefield_debug_service_override: Node
var _camera_controller_override: Node
var _campaign_setter_override: Callable
var _campaign_battle_getter_override: Callable
var _scene_transition_override: Callable
var _progression_start_override: Callable
var _battle_context_configure_override: Callable
var _battle_context_biome_setter_override: Callable
var _console_execute_override: Callable
var _console_all_commands_override: Callable
var _console_matching_commands_override: Callable

const CAMERA_AUTO_LOG_INTERVAL_SECONDS: float = 5.0

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	if not OS.is_debug_build():
		queue_free()
		return

	_menu_enabled = _compute_menu_enabled()
	if not _menu_enabled:
		set_process(false)
		set_process_input(false)
		print("[Debug] DebugMenu disabled (remove %s to enable)." % DISABLE_FLAG)
		return

	# Always process, even when paused
	process_mode = Node.PROCESS_MODE_ALWAYS

	# Resolve debug services once autoloads are initialized.
	_unit_debug = _get_unit_debug_service()

	# Load saved settings before creating UI
	_load_settings()
	_apply_spawn_boundary_bypass()

	# Create UI after a frame to ensure tree is ready
	call_deferred("_create_ui")
	print("[Debug] Ready - Press ` or F12 to toggle panel, F5-F8 for quick FPS change")


func _process(_delta: float) -> void:
	if _fps_label:
		var current_fps: float = Engine.get_frames_per_second()
		_fps_label.text = "FPS: %.1f" % current_fps

	if _panel and _panel.visible:
		_refresh_camera_overlay_button_state()
		_refresh_camera_auto_log_button_state()
		_refresh_camera_zoom_solver_log_button_state()

	if _camera_auto_log_enabled:
		_camera_auto_log_elapsed += _delta
		if _camera_auto_log_elapsed >= CAMERA_AUTO_LOG_INTERVAL_SECONDS:
			_camera_auto_log_elapsed = 0.0
			_log_active_camera_snapshot()


func _input(event: InputEvent) -> void:
	if not _menu_enabled:
		return

	if not event is InputEventKey:
		return

	var key_event: InputEventKey = event as InputEventKey
	if not key_event.pressed or key_event.echo:
		return

	match key_event.keycode:
		KEY_QUOTELEFT, KEY_F12:  # Backtick (`) or F12 to toggle
			_toggle_panel()
		KEY_F5:
			_set_fps(30)
		KEY_F6:
			_set_fps(60)
		KEY_F7:
			_set_fps(120)
		KEY_F8:
			_set_fps(0)
		KEY_F9:
			_on_projectile_hit_geometry_toggle_pressed()


func _compute_menu_enabled() -> bool:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if DISABLE_FLAG in args:
		return false
	# Default to enabled in debug builds.
	return true


## =============================================================================
## UI CREATION
## =============================================================================

func _create_ui() -> void:
	# Create CanvasLayer to render on top of all game UI
	var canvas_layer: CanvasLayer = CanvasLayer.new()
	const DEBUG_CANVAS_LAYER: int = 100
	canvas_layer.layer = DEBUG_CANVAS_LAYER
	add_child(canvas_layer)

	# Main panel
	_panel = PanelContainer.new()
	_panel.position = Vector2(10, 10)
	canvas_layer.add_child(_panel)

	# Style the panel
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.1, 0.85)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 12
	style.content_margin_right = 12
	style.content_margin_top = 8
	style.content_margin_bottom = 8
	_panel.add_theme_stylebox_override("panel", style)

	# Main container
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	_panel.add_child(vbox)

	# Title
	var title: Label = Label.new()
	title.text = "Debug Menu"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", Color(0.9, 0.9, 0.9))
	vbox.add_child(title)

	# FPS display
	_fps_label = Label.new()
	_fps_label.text = "FPS: --"
	_fps_label.add_theme_font_size_override("font_size", 24)
	_fps_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	vbox.add_child(_fps_label)

	# Target display
	_target_label = Label.new()
	_target_label.text = "Target: Uncapped"
	_target_label.add_theme_font_size_override("font_size", 14)
	_target_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	vbox.add_child(_target_label)

	# Separator
	var separator: HSeparator = HSeparator.new()
	vbox.add_child(separator)

	_tabs = TabContainer.new()
	vbox.add_child(_tabs)

	var quick_vbox: VBoxContainer = VBoxContainer.new()
	quick_vbox.name = "Quick"
	quick_vbox.add_theme_constant_override("separation", 8)
	quick_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tabs.add_child(quick_vbox)

	var more_vbox: VBoxContainer = VBoxContainer.new()
	more_vbox.name = "More"
	more_vbox.add_theme_constant_override("separation", 8)
	more_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tabs.add_child(more_vbox)

	_build_quick_tab(quick_vbox)
	_build_more_tab(more_vbox)

	# Start hidden by default (press ` or F12 to show)
	_panel.visible = false

	# Update button text to reflect loaded settings
	_update_button_states()
	_apply_spawn_boundary_bypass()


func _build_quick_tab(vbox: VBoxContainer) -> void:
	var flow_title: Label = Label.new()
	flow_title.text = "Battle Flow"
	flow_title.add_theme_font_size_override("font_size", 14)
	flow_title.add_theme_color_override("font_color", Color(0.9, 0.8, 0.5))
	vbox.add_child(flow_title)

	# Skip Prep Phase button
	_skip_prep_button = Button.new()
	_skip_prep_button.text = "Skip Prep Phase"
	_skip_prep_button.custom_minimum_size = Vector2(220, 32)
	_skip_prep_button.pressed.connect(_on_skip_prep_pressed)
	vbox.add_child(_skip_prep_button)

	var arena_separator: HSeparator = HSeparator.new()
	vbox.add_child(arena_separator)

	var arena_title: Label = Label.new()
	arena_title.text = "Test Arena"
	arena_title.add_theme_font_size_override("font_size", 14)
	arena_title.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
	vbox.add_child(arena_title)

	var open_arena_map_button: Button = Button.new()
	open_arena_map_button.text = "Open Test Arena Map"
	open_arena_map_button.custom_minimum_size = Vector2(220, 32)
	open_arena_map_button.pressed.connect(_on_open_test_arena_map_pressed)
	vbox.add_child(open_arena_map_button)

	var experimental_rooms_button: Button = Button.new()
	experimental_rooms_button.text = "Experimental Rooms"
	experimental_rooms_button.custom_minimum_size = Vector2(220, 32)
	experimental_rooms_button.pressed.connect(_on_open_experimental_rooms_pressed)
	vbox.add_child(experimental_rooms_button)

	var debug_separator: HSeparator = HSeparator.new()
	vbox.add_child(debug_separator)

	var toggles_title: Label = Label.new()
	toggles_title.text = "Debug Toggles"
	toggles_title.add_theme_font_size_override("font_size", 14)
	toggles_title.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
	vbox.add_child(toggles_title)

	# Hurtbox toggle button
	_hurtbox_button = Button.new()
	_hurtbox_button.text = "Hurtboxes: Off"
	_hurtbox_button.custom_minimum_size = Vector2(220, 32)
	_hurtbox_button.pressed.connect(_on_hurtbox_toggle_pressed)
	vbox.add_child(_hurtbox_button)

	# Target Point toggle button
	_target_point_button = Button.new()
	_target_point_button.text = "Target Points: Off"
	_target_point_button.custom_minimum_size = Vector2(220, 32)
	_target_point_button.pressed.connect(_on_target_point_toggle_pressed)
	vbox.add_child(_target_point_button)

	# Attack Range toggle button
	_attack_range_button = Button.new()
	_attack_range_button.text = "Engage Range: Off"
	_attack_range_button.custom_minimum_size = Vector2(220, 32)
	_attack_range_button.pressed.connect(_on_attack_range_toggle_pressed)
	vbox.add_child(_attack_range_button)

	# Damage Shape toggle button
	_damage_shape_button = Button.new()
	_damage_shape_button.text = "Damage Shapes: Off"
	_damage_shape_button.custom_minimum_size = Vector2(220, 32)
	_damage_shape_button.pressed.connect(_on_damage_shape_toggle_pressed)
	vbox.add_child(_damage_shape_button)

	# Navigation Footprint toggle button
	_navigation_footprint_button = Button.new()
	_navigation_footprint_button.text = "Navigation Footprint: Off"
	_navigation_footprint_button.custom_minimum_size = Vector2(220, 32)
	_navigation_footprint_button.pressed.connect(_on_navigation_footprint_toggle_pressed)
	vbox.add_child(_navigation_footprint_button)

	# Projectile Hit Geometry toggle button
	_projectile_hit_geometry_button = Button.new()
	_projectile_hit_geometry_button.text = "Projectile Hit Radius: Off"
	_projectile_hit_geometry_button.custom_minimum_size = Vector2(220, 32)
	_projectile_hit_geometry_button.pressed.connect(_on_projectile_hit_geometry_toggle_pressed)
	vbox.add_child(_projectile_hit_geometry_button)

	# Summoner Bubble toggle button
	_summoner_bubble_button = Button.new()
	_summoner_bubble_button.text = "Summoner Bubble: Off"
	_summoner_bubble_button.custom_minimum_size = Vector2(200, 32)
	_summoner_bubble_button.pressed.connect(_on_summoner_bubble_toggle_pressed)
	vbox.add_child(_summoner_bubble_button)

	# Ability Logs toggle button
	_ability_logs_button = Button.new()
	_ability_logs_button.text = "Ability Logs: Off"
	_ability_logs_button.custom_minimum_size = Vector2(220, 32)
	_ability_logs_button.pressed.connect(_on_ability_logs_toggle_pressed)
	vbox.add_child(_ability_logs_button)

	# Spawn Boundary Bypass toggle button
	_spawn_boundary_button = Button.new()
	_spawn_boundary_button.text = "Spawn Boundary: On"
	_spawn_boundary_button.custom_minimum_size = Vector2(220, 32)
	_spawn_boundary_button.pressed.connect(_on_spawn_boundary_toggle_pressed)
	vbox.add_child(_spawn_boundary_button)

	var camera_separator: HSeparator = HSeparator.new()
	vbox.add_child(camera_separator)

	var camera_title: Label = Label.new()
	camera_title.text = "Camera Debug"
	camera_title.add_theme_font_size_override("font_size", 14)
	camera_title.add_theme_color_override("font_color", Color(0.6, 1.0, 0.8))
	vbox.add_child(camera_title)

	# Camera bounds overlay toggle button
	_camera_overlay_button = Button.new()
	_camera_overlay_button.text = "Camera Overlay: N/A"
	_camera_overlay_button.custom_minimum_size = Vector2(220, 32)
	_camera_overlay_button.pressed.connect(_on_camera_overlay_toggle_pressed)
	vbox.add_child(_camera_overlay_button)

	# Camera auto-log toggle button
	_camera_auto_log_button = Button.new()
	_camera_auto_log_button.text = "Camera Auto-Log: Off"
	_camera_auto_log_button.custom_minimum_size = Vector2(220, 32)
	_camera_auto_log_button.pressed.connect(_on_camera_auto_log_toggle_pressed)
	vbox.add_child(_camera_auto_log_button)

	# Camera zoom solver log toggle button
	_camera_zoom_solver_log_button = Button.new()
	_camera_zoom_solver_log_button.text = "Zoom Solver Logs: N/A"
	_camera_zoom_solver_log_button.custom_minimum_size = Vector2(220, 32)
	_camera_zoom_solver_log_button.pressed.connect(_on_camera_zoom_solver_log_toggle_pressed)
	vbox.add_child(_camera_zoom_solver_log_button)

	# Console command separator
	var console_separator: HSeparator = HSeparator.new()
	vbox.add_child(console_separator)

	# Console command title
	var console_title: Label = Label.new()
	console_title.text = "Console Commands"
	console_title.add_theme_font_size_override("font_size", 14)
	console_title.add_theme_color_override("font_color", Color(0.3, 0.8, 1.0))
	vbox.add_child(console_title)

	# Command input
	_command_input = LineEdit.new()
	_command_input.placeholder_text = "Type / for commands"
	_command_input.custom_minimum_size = Vector2(220, 32)
	_command_input.text_submitted.connect(_on_command_submitted)
	_command_input.text_changed.connect(_on_command_text_changed)
	_command_input.gui_input.connect(_on_command_input_gui_input)
	vbox.add_child(_command_input)

	# Autocomplete list
	_autocomplete_list = ItemList.new()
	_autocomplete_list.custom_minimum_size = Vector2(220, 150)
	_autocomplete_list.select_mode = ItemList.SELECT_SINGLE
	_autocomplete_list.item_selected.connect(_on_autocomplete_item_selected)
	_autocomplete_list.visible = false
	vbox.add_child(_autocomplete_list)

	# Command output
	_command_output = Label.new()
	_command_output.text = ""
	_command_output.add_theme_font_size_override("font_size", 11)
	_command_output.add_theme_color_override("font_color", Color(0.6, 1.0, 0.6))
	_command_output.autowrap_mode = TextServer.AUTOWRAP_WORD
	_command_output.custom_minimum_size = Vector2(220, 0)
	vbox.add_child(_command_output)

	# Snapshots separator
	var snapshot_separator: HSeparator = HSeparator.new()
	vbox.add_child(snapshot_separator)

	var snapshot_title: Label = Label.new()
	snapshot_title.text = "Snapshots"
	snapshot_title.add_theme_font_size_override("font_size", 14)
	snapshot_title.add_theme_color_override("font_color", Color(0.8, 0.8, 1.0))
	vbox.add_child(snapshot_title)

	# Manage Snapshots button
	var snapshots_button: Button = Button.new()
	snapshots_button.text = "Manage Snapshots"
	snapshots_button.custom_minimum_size = Vector2(220, 32)
	snapshots_button.pressed.connect(_on_snapshots_pressed)
	vbox.add_child(snapshots_button)


func _build_more_tab(vbox: VBoxContainer) -> void:
	# Frame-rate controls
	var fps_title: Label = Label.new()
	fps_title.text = "Frame Rate"
	fps_title.add_theme_font_size_override("font_size", 14)
	fps_title.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
	vbox.add_child(fps_title)

	var grid: GridContainer = GridContainer.new()
	grid.columns = 2
	grid.add_theme_constant_override("h_separation", 8)
	grid.add_theme_constant_override("v_separation", 6)
	vbox.add_child(grid)

	_buttons.clear()
	_create_fps_button(grid, 30, "30 FPS", "F5")
	_create_fps_button(grid, 60, "60 FPS", "F6")
	_create_fps_button(grid, 120, "120 FPS", "F7")
	_create_fps_button(grid, 0, "Uncapped", "F8")

	var instructions: Label = Label.new()
	instructions.text = "` or F12 to hide"
	instructions.add_theme_font_size_override("font_size", 11)
	instructions.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	instructions.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(instructions)

	# Battle Control separator
	var battle_separator: HSeparator = HSeparator.new()
	vbox.add_child(battle_separator)

	# Battle Control title
	var battle_title: Label = Label.new()
	battle_title.text = "Battle Control"
	battle_title.add_theme_font_size_override("font_size", 14)
	battle_title.add_theme_color_override("font_color", Color(1.0, 0.5, 0.5))
	vbox.add_child(battle_title)

	# Win/Lose button container
	var battle_grid: GridContainer = GridContainer.new()
	battle_grid.columns = 2
	battle_grid.add_theme_constant_override("h_separation", 8)
	vbox.add_child(battle_grid)

	# Win button
	var win_button: Button = Button.new()
	win_button.text = "Win"
	win_button.custom_minimum_size = Vector2(96, 32)
	win_button.pressed.connect(_on_win_pressed)
	battle_grid.add_child(win_button)

	# Lose button
	var lose_button: Button = Button.new()
	lose_button.text = "Lose"
	lose_button.custom_minimum_size = Vector2(96, 32)
	lose_button.pressed.connect(_on_lose_pressed)
	battle_grid.add_child(lose_button)

	# Debug arena quick launch
	var arena_separator: HSeparator = HSeparator.new()
	vbox.add_child(arena_separator)

	var arena_title: Label = Label.new()
	arena_title.text = "Debug Arena Battles"
	arena_title.add_theme_font_size_override("font_size", 14)
	arena_title.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
	vbox.add_child(arena_title)

	var preset_row: HBoxContainer = HBoxContainer.new()
	preset_row.add_theme_constant_override("separation", 8)
	vbox.add_child(preset_row)

	var preset_label: Label = Label.new()
	preset_label.text = "Arena List"
	preset_label.add_theme_color_override("font_color", Color(0.75, 0.75, 0.75))
	preset_row.add_child(preset_label)

	_arena_preset_dropdown = OptionButton.new()
	_arena_preset_dropdown.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_arena_preset_dropdown.item_selected.connect(_on_arena_preset_selected)
	preset_row.add_child(_arena_preset_dropdown)
	_populate_arena_preset_dropdown()

	var biome_row: HBoxContainer = HBoxContainer.new()
	biome_row.add_theme_constant_override("separation", 8)
	vbox.add_child(biome_row)

	var biome_label: Label = Label.new()
	biome_label.text = "Biome"
	biome_label.add_theme_color_override("font_color", Color(0.75, 0.75, 0.75))
	biome_row.add_child(biome_label)

	_arena_biome_dropdown = OptionButton.new()
	_arena_biome_dropdown.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_arena_biome_dropdown.item_selected.connect(_on_arena_biome_selected)
	biome_row.add_child(_arena_biome_dropdown)
	_populate_arena_biome_dropdown()

	_arena_button_grid = GridContainer.new()
	_arena_button_grid.columns = 2
	_arena_button_grid.add_theme_constant_override("h_separation", 8)
	_arena_button_grid.add_theme_constant_override("v_separation", 6)
	vbox.add_child(_arena_button_grid)

	_build_debug_arena_buttons(_arena_button_grid)


func _update_button_states() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()

	if _hurtbox_button and _unit_debug and _unit_debug.has_method("IsDebugHurtboxEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugHurtboxEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_hurtbox_button.text = "Hurtboxes: %s" % state

	if _target_point_button and _unit_debug and _unit_debug.has_method("IsDebugTargetPointEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugTargetPointEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_target_point_button.text = "Target Points: %s" % state

	if _attack_range_button and _unit_debug:
		var enabled: bool = false
		if _unit_debug.has_method("IsDebugEngageRangeEnabled"):
			enabled = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugEngageRangeEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_attack_range_button.text = "Engage Range: %s" % state

	if _damage_shape_button and _unit_debug and _unit_debug.has_method("IsDebugDamageShapeEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugDamageShapeEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_damage_shape_button.text = "Damage Shapes: %s" % state

	if _navigation_footprint_button and _unit_debug:
		var enabled: bool = false
		if _unit_debug.has_method("IsDebugNavigationFootprintEnabled"):
			enabled = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugNavigationFootprintEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_navigation_footprint_button.text = "Navigation Footprint: %s" % state

	if _projectile_hit_geometry_button and _unit_debug and _unit_debug.has_method("IsDebugProjectileHitGeometryEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugProjectileHitGeometryEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_projectile_hit_geometry_button.text = "Projectile Hit Radius: %s" % state

	if _summoner_bubble_button and _unit_debug:
		var enabled: bool = false
		if _unit_debug.has_method("IsDebugSummonerBubbleEnabled"):
			enabled = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugSummonerBubbleEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_summoner_bubble_button.text = "Summoner Bubble: %s" % state

	if _ability_logs_button and _unit_debug:
		var enabled: bool = false
		if _unit_debug.has_method("IsDebugAbilityLogsEnabled"):
			enabled = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugAbilityLogsEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_ability_logs_button.text = "Ability Logs: %s" % state

	if _spawn_boundary_button:
		var debug_service: Node = _get_battlefield_debug_service()
		if debug_service and debug_service.has_method("IsSpawnBoundaryBypassEnabled"):
			var bypass_var: Variant = debug_service.call("IsSpawnBoundaryBypassEnabled")
			if bypass_var is bool:
				_bypass_spawn_boundary = bypass_var

		var bypass_enabled: bool = _bypass_spawn_boundary
		var state: String = "Off" if bypass_enabled else "On"
		_spawn_boundary_button.text = "Spawn Boundary: %s" % state

	_refresh_camera_overlay_button_state()
	_refresh_camera_auto_log_button_state()
	_refresh_camera_zoom_solver_log_button_state()


func _create_fps_button(parent: Node, fps: int, text: String, hotkey: String) -> void:
	var button: Button = Button.new()
	button.text = "%s (%s)" % [text, hotkey]
	button.custom_minimum_size = Vector2(100, 32)
	button.pressed.connect(_on_fps_button_pressed.bind(fps))
	parent.add_child(button)
	_buttons[fps] = button


func _add_debug_arena_button(parent: GridContainer, label: String, battle_id: StringName) -> void:
	var button: Button = Button.new()
	button.text = label
	button.custom_minimum_size = Vector2(96, 32)
	button.pressed.connect(_on_debug_arena_battle_pressed.bind(String(battle_id)))
	parent.add_child(button)


func _build_debug_arena_buttons(parent: GridContainer) -> void:
	for child_var: Variant in parent.get_children():
		if child_var is Node:
			var child: Node = child_var
			parent.remove_child(child)
			child.queue_free()

	var entries: Array[Dictionary] = DEBUG_ARENA_PRESETS.get_preset_entries(_arena_preset_id)
	if entries.is_empty():
		# Defensive fallback for malformed/missing preset catalog.
		entries = [
			{"label": "Earth Sprite", "battle_id": String(BattleIDs.ARENA_EARTH_SPRITE)},
			{"label": "Puff", "battle_id": String(BattleIDs.ARENA_PUFF)},
			{"label": "Fire Wisp", "battle_id": String(BattleIDs.ARENA_FIRE_WISP)},
			{"label": "Cloud Swarm", "battle_id": String(BattleIDs.ARENA_CLOUD_SWARM)},
			{"label": "Mana Bolt", "battle_id": String(BattleIDs.ARENA_MANA_BOLT)},
			{"label": "Debug Arena", "battle_id": String(BattleIDs.DEBUG_ARENA)}
		]

	for entry: Dictionary in entries:
		var battle_id: String = SafeTypeUtils.string(entry.get("battle_id", ""), "")
		if battle_id.is_empty():
			continue
		var label: String = SafeTypeUtils.string(entry.get("label", battle_id), battle_id)
		_add_debug_arena_button(parent, label, StringName(battle_id))


func _populate_arena_preset_dropdown() -> void:
	if not _arena_preset_dropdown:
		return

	_arena_preset_dropdown.clear()
	var presets: Array[Dictionary] = DEBUG_ARENA_PRESETS.get_available_presets()
	var selected_index: int = -1

	for preset: Dictionary in presets:
		var preset_id: String = SafeTypeUtils.string(preset.get("id", ""), "")
		if preset_id.is_empty():
			continue

		var preset_label: String = SafeTypeUtils.string(preset.get("label", preset_id), preset_id)
		_arena_preset_dropdown.add_item(preset_label)
		var item_index: int = _arena_preset_dropdown.item_count - 1
		_arena_preset_dropdown.set_item_metadata(item_index, preset_id)

		if preset_id == _arena_preset_id:
			selected_index = item_index

	if _arena_preset_dropdown.item_count == 0:
		_arena_preset_dropdown.add_item(DEFAULT_ARENA_PRESET_ID)
		_arena_preset_dropdown.set_item_metadata(0, DEFAULT_ARENA_PRESET_ID)
		_arena_preset_id = DEFAULT_ARENA_PRESET_ID
		_arena_preset_dropdown.select(0)
		return

	if selected_index < 0:
		selected_index = 0
		_arena_preset_id = SafeTypeUtils.string(
			_arena_preset_dropdown.get_item_metadata(selected_index),
			DEFAULT_ARENA_PRESET_ID
		)

	_arena_preset_dropdown.select(selected_index)


func _on_arena_preset_selected(index: int) -> void:
	if not _arena_preset_dropdown:
		return

	var selected_id: String = SafeTypeUtils.string(_arena_preset_dropdown.get_item_metadata(index), "")
	if selected_id.is_empty():
		return
	if _arena_preset_id == selected_id:
		return

	_arena_preset_id = selected_id
	_save_settings()
	if _arena_button_grid:
		_build_debug_arena_buttons(_arena_button_grid)


func _on_open_experimental_rooms_pressed() -> void:
	if not DEBUG_ARENA_PRESETS.has_preset(EXPERIMENTAL_ROOMS_PRESET_ID):
		push_warning("DebugMenu: Experimental rooms preset is missing")
		return

	_arena_preset_id = EXPERIMENTAL_ROOMS_PRESET_ID
	_save_settings()
	if _arena_preset_dropdown:
		_populate_arena_preset_dropdown()
	if _arena_button_grid:
		_build_debug_arena_buttons(_arena_button_grid)
	if _tabs:
		_tabs.current_tab = 1


func _populate_arena_biome_dropdown() -> void:
	if not _arena_biome_dropdown:
		return

	_arena_biome_dropdown.clear()
	var selected_index: int = -1
	for biome_id: StringName in BiomeIDs.ALL_BIOMES:
		_arena_biome_dropdown.add_item(String(biome_id).capitalize())
		var item_index: int = _arena_biome_dropdown.item_count - 1
		_arena_biome_dropdown.set_item_metadata(item_index, String(biome_id))
		if biome_id == _arena_biome_id:
			selected_index = item_index

	if selected_index < 0:
		selected_index = 0
		_arena_biome_id = BiomeIDs.DEFAULT
	_arena_biome_dropdown.select(selected_index)


func _on_arena_biome_selected(index: int) -> void:
	if not _arena_biome_dropdown:
		return
	if index < 0 or index >= _arena_biome_dropdown.item_count:
		return

	var selected_id: String = SafeTypeUtils.string(
		_arena_biome_dropdown.get_item_metadata(index),
		String(BiomeIDs.DEFAULT)
	)
	if not BiomeIDs.is_valid(selected_id):
		return
	if _arena_biome_id == StringName(selected_id):
		return

	_arena_biome_id = StringName(selected_id)
	_save_settings()


## =============================================================================
## ACTIONS
## =============================================================================

func _toggle_panel() -> void:
	if _panel:
		_panel.visible = not _panel.visible
		if _panel.visible:
			_update_button_states()


func _set_fps(target: int) -> void:
	Engine.max_fps = target

	var label: String = "Uncapped" if target == 0 else "%d FPS" % target
	if _target_label:
		_target_label.text = "Target: %s" % label

	# Update button states
	for fps: int in _buttons:
		var button: Button = _buttons[fps]
		button.disabled = (fps == target)

	print("[Debug] Set to %s" % label)


func _on_fps_button_pressed(fps: int) -> void:
	_set_fps(fps)


func _on_skip_prep_pressed() -> void:
	var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	if game_controller and game_controller.has_method("SkipPrepPhase"):
		game_controller.call("SkipPrepPhase")
		print("[Debug] Skipped prep phase")
	else:
		print("[Debug] No game controller found - not in battle?")


func _on_hurtbox_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugHurtbox"):
		return
	_unit_debug.call("ToggleDebugHurtbox")
	_update_button_states()
	_save_settings()


func _on_target_point_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugTargetPoint"):
		return
	_unit_debug.call("ToggleDebugTargetPoint")
	_update_button_states()
	_save_settings()


func _on_attack_range_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugEngageRange"):
		return

	_unit_debug.call("ToggleDebugEngageRange")

	_update_button_states()
	_save_settings()


func _on_damage_shape_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugDamageShape"):
		return
	_unit_debug.call("ToggleDebugDamageShape")
	_update_button_states()
	_save_settings()


func _on_navigation_footprint_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugNavigationFootprint"):
		return

	_unit_debug.call("ToggleDebugNavigationFootprint")

	_update_button_states()
	_save_settings()


func _on_projectile_hit_geometry_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugProjectileHitGeometry"):
		return
	_unit_debug.call("ToggleDebugProjectileHitGeometry")
	_update_button_states()
	_save_settings()


func _on_summoner_bubble_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugSummonerBubble"):
		return
	_unit_debug.call("ToggleDebugSummonerBubble")
	_update_button_states()
	_save_settings()


func _on_ability_logs_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugAbilityLogs"):
		return
	_unit_debug.call("ToggleDebugAbilityLogs")
	_update_button_states()
	_save_settings()
	var enabled: bool = false
	if _unit_debug.has_method("IsDebugAbilityLogsEnabled"):
		enabled = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugAbilityLogsEnabled"), false)
	print("[Debug] Ability logs %s" % ("enabled" if enabled else "disabled"))


func _on_spawn_boundary_toggle_pressed() -> void:
	_bypass_spawn_boundary = !_bypass_spawn_boundary
	var bypass_enabled: bool = _bypass_spawn_boundary
	var state: String = "Off" if bypass_enabled else "On"
	_spawn_boundary_button.text = "Spawn Boundary: %s" % state
	_apply_spawn_boundary_bypass()
	_save_settings()


func _apply_spawn_boundary_bypass() -> void:
	var battlefield_debug: Node = _get_battlefield_debug_service()
	if battlefield_debug and battlefield_debug.has_method("SetSpawnBoundaryBypassEnabled"):
		battlefield_debug.call("SetSpawnBoundaryBypassEnabled", _bypass_spawn_boundary)


func _on_camera_overlay_toggle_pressed() -> void:
	var camera: Node = _find_battle_camera_controller()
	if not camera:
		print("[Debug] No battle camera found - start a battle to toggle overlay")
		_refresh_camera_overlay_button_state()
		return

	var enabled_var: Variant = camera.get("debug_show_pan_bounds_overlay")
	var enabled: bool = enabled_var if enabled_var is bool else false
	camera.set("debug_show_pan_bounds_overlay", not enabled)
	_log_camera_clamp_diagnostics(camera)
	_refresh_camera_overlay_button_state()


func _on_camera_auto_log_toggle_pressed() -> void:
	_camera_auto_log_enabled = not _camera_auto_log_enabled
	_camera_auto_log_elapsed = 0.0
	_refresh_camera_auto_log_button_state()
	_save_settings()

	if _camera_auto_log_enabled:
		print("[Debug] Camera auto-log enabled (every %.1fs)" % CAMERA_AUTO_LOG_INTERVAL_SECONDS)
		_log_active_camera_snapshot()
	else:
		print("[Debug] Camera auto-log disabled")


func _on_camera_zoom_solver_log_toggle_pressed() -> void:
	var camera: Node = _find_battle_camera_controller()
	if not camera:
		print("[Debug] No battle camera found - start a battle to toggle zoom solver logs")
		_refresh_camera_zoom_solver_log_button_state()
		return

	var enabled_var: Variant = camera.get("debug_log_zoom_solver")
	var enabled: bool = enabled_var if enabled_var is bool else false
	var new_enabled: bool = not enabled
	camera.set("debug_log_zoom_solver", new_enabled)
	print("[Debug] Camera zoom solver logs %s" % ("enabled" if new_enabled else "disabled"))
	_refresh_camera_zoom_solver_log_button_state()


func _log_active_camera_snapshot() -> void:
	var camera: Node = _find_battle_camera_controller()
	if not camera:
		print("[Debug] Camera auto-log: no battle camera found")
		return

	var pos_var: Variant = camera.get("global_position")
	var camera_pos: Vector3 = pos_var if pos_var is Vector3 else Vector3.ZERO
	print(
		"[Debug] Camera pos -> x=%.4f y=%.4f z=%.4f" % [
			camera_pos.x,
			camera_pos.y,
			camera_pos.z
		]
	)
	_log_camera_clamp_diagnostics(camera)


func _log_camera_clamp_diagnostics(camera: Node) -> void:
	if not camera or not camera.has_method("get_clamp_diagnostics"):
		return

	var diagnostics_var: Variant = camera.call("get_clamp_diagnostics")
	if not diagnostics_var is Dictionary:
		return
	var diagnostics: Dictionary = diagnostics_var

	var view_bounds: Rect2 = diagnostics.get("view_bounds_xz", Rect2())
	var map_bounds: Rect2 = diagnostics.get("map_bounds_xz", Rect2())
	var horizontal_mode: String = str(diagnostics.get("horizontal_mode", "unknown"))
	var vertical_mode: String = str(diagnostics.get("vertical_mode", "unknown"))
	var oversize_x: bool = bool(diagnostics.get("oversize_x", false))
	var oversize_z: bool = bool(diagnostics.get("oversize_z", false))
	var target_dx: float = float(diagnostics.get("target_dx", 0.0))
	var target_dz: float = float(diagnostics.get("target_dz", 0.0))
	var vertical_center_anchor_z: float = float(diagnostics.get("vertical_center_anchor_z", 0.0))
	var vertical_center_reference_screen_y: float = float(diagnostics.get("vertical_center_reference_screen_y", 0.5))

	print(
		"[Debug] Camera clamp diag -> mode_x=%s mode_z=%s oversize_x=%s oversize_z=%s " %
		[horizontal_mode, vertical_mode, str(oversize_x), str(oversize_z)] +
		"view_z=[%.4f..%.4f] map_z=[%.4f..%.4f] target_d=(%.4f, %.4f) anchor_z=%.4f ref_y=%.4f" % [
			view_bounds.position.y,
			view_bounds.position.y + view_bounds.size.y,
			map_bounds.position.y,
			map_bounds.position.y + map_bounds.size.y,
			target_dx,
			target_dz,
			vertical_center_anchor_z,
			vertical_center_reference_screen_y
		]
	)


func _on_command_submitted(command: String) -> void:
	if command.is_empty():
		return

	# Hide autocomplete
	_hide_autocomplete()

	# Execute via DevConsole
	var success: bool = _execute_console_command(command)

	# Show result
	if _command_output:
		if success:
			_command_output.add_theme_color_override("font_color", Color(0.6, 1.0, 0.6))
			_command_output.text = "OK: %s" % command
		else:
			_command_output.add_theme_color_override("font_color", Color(1.0, 0.5, 0.5))
			_command_output.text = "Failed: %s" % command

	# Clear input
	if _command_input:
		_command_input.clear()


func _on_command_text_changed(new_text: String) -> void:
	_update_autocomplete(new_text)


func _on_command_input_gui_input(event: InputEvent) -> void:
	if not event is InputEventKey:
		return

	var key_event: InputEventKey = event
	if not key_event.pressed:
		return

	match key_event.keycode:
		KEY_TAB:
			# Accept current selection or first suggestion
			_accept_autocomplete()
			get_viewport().set_input_as_handled()
		KEY_UP:
			if _autocomplete_visible:
				_navigate_autocomplete(-1)
				get_viewport().set_input_as_handled()
		KEY_DOWN:
			if _autocomplete_visible:
				_navigate_autocomplete(1)
				get_viewport().set_input_as_handled()
		KEY_ESCAPE:
			if _autocomplete_visible:
				_hide_autocomplete()
				get_viewport().set_input_as_handled()


func _on_autocomplete_item_selected(index: int) -> void:
	_select_autocomplete_item(index)


## =============================================================================
## AUTOCOMPLETE
## =============================================================================

func _update_autocomplete(text: String) -> void:
	if not _autocomplete_list:
		return

	# Clear previous items
	_autocomplete_list.clear()

	# Get matching commands
	var matches: Array[Dictionary]
	if text.is_empty() or text == "/":
		matches = _get_all_console_commands()
	else:
		matches = _get_matching_console_commands(text)

	# No matches - hide
	if matches.is_empty():
		_hide_autocomplete()
		return

	# Add items
	for cmd_info: Dictionary in matches:
		var cmd: String = cmd_info.get("cmd", "")
		var args: String = cmd_info.get("args", "")
		var desc: String = cmd_info.get("desc", "")

		var display: String = cmd
		if not args.is_empty():
			display += " " + args

		_autocomplete_list.add_item(display)
		_autocomplete_list.set_item_tooltip(_autocomplete_list.item_count - 1, desc)

	# Show and select first
	_autocomplete_list.visible = true
	_autocomplete_visible = true
	if _autocomplete_list.item_count > 0:
		_autocomplete_list.select(0)


func _hide_autocomplete() -> void:
	if _autocomplete_list:
		_autocomplete_list.visible = false
		_autocomplete_list.clear()
	_autocomplete_visible = false


func _navigate_autocomplete(direction: int) -> void:
	if not _autocomplete_list or _autocomplete_list.item_count == 0:
		return

	var selected: PackedInt32Array = _autocomplete_list.get_selected_items()
	var current_idx: int = selected[0] if selected.size() > 0 else -1
	var new_idx: int = current_idx + direction

	# Wrap around
	if new_idx < 0:
		new_idx = _autocomplete_list.item_count - 1
	elif new_idx >= _autocomplete_list.item_count:
		new_idx = 0

	_autocomplete_list.select(new_idx)
	_autocomplete_list.ensure_current_is_visible()


func _accept_autocomplete() -> void:
	if not _autocomplete_list or _autocomplete_list.item_count == 0:
		# Show all commands if nothing shown
		_update_autocomplete("/")
		return

	var selected: PackedInt32Array = _autocomplete_list.get_selected_items()
	var idx: int = selected[0] if selected.size() > 0 else 0
	_select_autocomplete_item(idx)


func _select_autocomplete_item(index: int) -> void:
	if not _autocomplete_list or index < 0 or index >= _autocomplete_list.item_count:
		return

	var item_text: String = _autocomplete_list.get_item_text(index)
	# Extract just the command (before any space for args)
	var cmd: String = item_text.split(" ")[0]

	if _command_input:
		_command_input.text = cmd
		_command_input.caret_column = cmd.length()
		if _command_input.is_inside_tree():
			_command_input.grab_focus()

	_hide_autocomplete()


func _set_current_campaign(campaign_id: String) -> bool:
	if _campaign_setter_override.is_valid():
		return SafeTypeUtils.bool_val(_campaign_setter_override.call(campaign_id), false)
	return CampaignApi.set_current_campaign(campaign_id)


func _get_campaign_battle(battle_id: String) -> Dictionary:
	if _campaign_battle_getter_override.is_valid():
		return SafeTypeUtils.dict(_campaign_battle_getter_override.call(battle_id))
	return CampaignApi.get_battle(battle_id)


func _transition_to_scene(scene_path: String) -> void:
	if _scene_transition_override.is_valid():
		_scene_transition_override.call(scene_path)
		return
	SceneManager.transition_to(scene_path)


func _start_debug_battle_attempt(campaign_id: String, battle_id: String) -> Dictionary:
	if _progression_start_override.is_valid():
		return SafeTypeUtils.dict(_progression_start_override.call(campaign_id, battle_id))
	return ProgressionAuthority.StartCampaignBattleAttempt(campaign_id, battle_id)


func _configure_campaign_battle_context(battle_id: String) -> void:
	if _battle_context_configure_override.is_valid():
		_battle_context_configure_override.call(battle_id)
		return
	BattleContext.configure_campaign_battle(battle_id)


func _set_debug_arena_biome(biome_id: StringName) -> void:
	if _battle_context_biome_setter_override.is_valid():
		_battle_context_biome_setter_override.call(String(biome_id))
		return
	BattleContext.biome_id = biome_id


func _execute_console_command(command: String) -> bool:
	if _console_execute_override.is_valid():
		return SafeTypeUtils.bool_val(_console_execute_override.call(command), false)
	return DevConsole.execute_command(command)


func _get_all_console_commands() -> Array[Dictionary]:
	if _console_all_commands_override.is_valid():
		return _normalize_console_commands(SafeTypeUtils.array(_console_all_commands_override.call()))
	return _normalize_console_commands(DevConsole.get_all_commands())


func _get_matching_console_commands(text: String) -> Array[Dictionary]:
	if _console_matching_commands_override.is_valid():
		return _normalize_console_commands(SafeTypeUtils.array(_console_matching_commands_override.call(text)))
	return _normalize_console_commands(DevConsole.get_matching_commands(text))


func _normalize_console_commands(raw: Array) -> Array[Dictionary]:
	var normalized: Array[Dictionary] = []
	for item: Variant in raw:
		var cmd: Dictionary = SafeTypeUtils.dict(item)
		if cmd.is_empty():
			continue
		normalized.append(cmd)
	return normalized


func _on_win_pressed() -> void:
	var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	if game_controller and game_controller.has_method("EndGame"):
		game_controller.call("EndGame", UnitConstants.Team.PLAYER)
		print("[Debug] Triggered instant WIN")
	else:
		print("[Debug] No game controller found - not in battle?")


func _on_lose_pressed() -> void:
	var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	if game_controller and game_controller.has_method("EndGame"):
		game_controller.call("EndGame", UnitConstants.Team.ENEMY)
		print("[Debug] Triggered instant LOSE")
	else:
		print("[Debug] No game controller found - not in battle?")


func _on_open_test_arena_map_pressed() -> void:
	var campaign_id: String = String(CampaignIDs.TEST_ARENA)
	var success: bool = _set_current_campaign(campaign_id)
	if not success:
		print("[Debug] Failed to switch campaign to '%s'" % campaign_id)
		return

	_transition_to_scene(SceneManager.SCENE_LEGACY_CAMPAIGN_MAP)
	print("[Debug] Opened Test Arena campaign map")


func _on_debug_arena_battle_pressed(battle_id: String) -> void:
	if battle_id.is_empty():
		return

	var campaign_id: String = String(CampaignIDs.TEST_ARENA)
	var campaign_set: bool = _set_current_campaign(campaign_id)
	if not campaign_set:
		print("[Debug] Failed to switch campaign to '%s'" % campaign_id)
		return

	var attempt_result: Dictionary = _start_debug_battle_attempt(campaign_id, battle_id)
	if not attempt_result.get("is_success", false):
		push_error("Debug battle launch could not persist an attempt: %s" % attempt_result.get("errors", []))
		return
	BattleContext.set_battle_attempt_id(attempt_result.get("attempt_id", ""))
	_configure_campaign_battle_context(battle_id)
	_set_debug_arena_biome(_arena_biome_id)

	var event_data: Dictionary = _get_campaign_battle(battle_id)
	var battle_scene: String = SceneManager.SCENE_BATTLE_3D
	var custom_scene: String = SafeTypeUtils.string(event_data.get("scene_path", ""), "")
	if not custom_scene.is_empty():
		battle_scene = custom_scene

	_transition_to_scene(battle_scene)
	print(
		"[Debug] Launched test arena battle '%s' with biome '%s'"
		% [battle_id, String(_arena_biome_id)]
	)


func _on_snapshots_pressed() -> void:
	# Load and show the snapshot manager scene
	var snapshot_scene: PackedScene = load("res://scenes/meta/screens/snapshot_manager.tscn")
	if snapshot_scene:
		var snapshot_manager: Node = snapshot_scene.instantiate()
		get_tree().root.add_child(snapshot_manager)
		if snapshot_manager.has_method("show_manager"):
			snapshot_manager.call("show_manager")


func _get_battlefield_debug_service() -> Node:
	if _battlefield_debug_service_override:
		return _battlefield_debug_service_override
	if not is_inside_tree():
		return null
	var tree: SceneTree = get_tree()
	if tree == null or tree.root == null:
		return null
	return tree.root.get_node_or_null(CSharpAutoloads.BATTLEFIELD_DEBUG)


func _get_unit_debug_service() -> Node:
	return _get_battlefield_debug_service()


func _find_battle_camera_controller() -> Node:
	if _camera_controller_override:
		return _camera_controller_override
	if not is_inside_tree():
		return null

	# Prefer active viewport camera first.
	var viewport: Viewport = get_viewport()
	if viewport == null:
		return null
	var active_camera: Camera3D = viewport.get_camera_3d()
	if active_camera and active_camera.has_method("get_clamp_diagnostics"):
		return active_camera

	# Fallback: search under battlefield root group.
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield:
		var stack: Array[Node] = [battlefield]
		while not stack.is_empty():
			var node: Node = stack.pop_back()
			if node.has_method("get_clamp_diagnostics"):
				return node
			for child_var: Variant in node.get_children():
				if child_var is Node:
					var child_node: Node = child_var
					stack.append(child_node)

	return null


func _refresh_camera_overlay_button_state() -> void:
	if not _camera_overlay_button:
		return

	var camera: Node = _find_battle_camera_controller()
	if not camera:
		_camera_overlay_button.text = "Camera Overlay: N/A"
		_camera_overlay_button.disabled = true
		return

	_camera_overlay_button.disabled = false
	var enabled_var: Variant = camera.get("debug_show_pan_bounds_overlay")
	var enabled: bool = enabled_var if enabled_var is bool else false
	_camera_overlay_button.text = "Camera Overlay: %s" % ("On" if enabled else "Off")


func _refresh_camera_auto_log_button_state() -> void:
	if not _camera_auto_log_button:
		return

	var camera: Node = _find_battle_camera_controller()
	if not camera:
		_camera_auto_log_button.text = "Camera Auto-Log: N/A"
		_camera_auto_log_button.disabled = true
		return

	_camera_auto_log_button.disabled = false
	_camera_auto_log_button.text = "Camera Auto-Log: %s" % ("On" if _camera_auto_log_enabled else "Off")


func _refresh_camera_zoom_solver_log_button_state() -> void:
	if not _camera_zoom_solver_log_button:
		return

	var camera: Node = _find_battle_camera_controller()
	if not camera:
		_camera_zoom_solver_log_button.text = "Zoom Solver Logs: N/A"
		_camera_zoom_solver_log_button.disabled = true
		return

	_camera_zoom_solver_log_button.disabled = false
	var enabled_var: Variant = camera.get("debug_log_zoom_solver")
	var enabled: bool = enabled_var if enabled_var is bool else false
	_camera_zoom_solver_log_button.text = "Zoom Solver Logs: %s" % ("On" if enabled else "Off")


## =============================================================================
## SETTINGS PERSISTENCE
## =============================================================================

func _load_settings() -> void:
	var config: ConfigFile = ConfigFile.new()
	var err: Error = config.load(SETTINGS_PATH)
	if err != OK:
		return  # No saved settings, use defaults

	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()

	# Load visualization toggles
	if _unit_debug:
		if _unit_debug.has_method("SetDebugHurtboxEnabled"):
			_unit_debug.call("SetDebugHurtboxEnabled", config.get_value("debug_menu", "hurtboxes", false))
		if _unit_debug.has_method("SetDebugTargetPointEnabled"):
			_unit_debug.call("SetDebugTargetPointEnabled", config.get_value("debug_menu", "target_points", false))
		var attack_range_enabled: bool = config.get_value("debug_menu", "engage_ranges", false)
		if _unit_debug.has_method("SetDebugEngageRangeEnabled"):
			_unit_debug.call("SetDebugEngageRangeEnabled", attack_range_enabled)

		if _unit_debug.has_method("SetDebugDamageShapeEnabled"):
			_unit_debug.call("SetDebugDamageShapeEnabled", config.get_value("debug_menu", "damage_shapes", false))

		var navigation_footprint_enabled: bool = config.get_value("debug_menu", "navigation_footprint", false)
		if _unit_debug.has_method("SetDebugNavigationFootprintEnabled"):
			_unit_debug.call("SetDebugNavigationFootprintEnabled", navigation_footprint_enabled)
		if _unit_debug.has_method("SetDebugProjectileHitGeometryEnabled"):
			_unit_debug.call("SetDebugProjectileHitGeometryEnabled", config.get_value("debug_menu", "projectile_hit_geometry", false))
		if _unit_debug.has_method("SetDebugSummonerBubbleEnabled"):
			_unit_debug.call("SetDebugSummonerBubbleEnabled", config.get_value("debug_menu", "summoner_bubble", false))
		if _unit_debug.has_method("SetDebugAbilityLogsEnabled"):
			_unit_debug.call("SetDebugAbilityLogsEnabled", config.get_value("debug_menu", "ability_logs", false))
	_bypass_spawn_boundary = config.get_value("debug_menu", "bypass_spawn_boundary", false)
	_camera_auto_log_enabled = config.get_value("debug_menu", "camera_auto_log", false)
	_camera_auto_log_elapsed = 0.0

	var preset_default: String = DEBUG_ARENA_PRESETS.get_default_preset_id()
	if preset_default.is_empty():
		preset_default = DEFAULT_ARENA_PRESET_ID
	_arena_preset_id = config.get_value("debug_menu", "arena_preset_id", preset_default)
	if not DEBUG_ARENA_PRESETS.has_preset(_arena_preset_id):
		_arena_preset_id = preset_default
	var saved_biome_id: String = SafeTypeUtils.string(
		config.get_value("debug_menu", "arena_biome_id", String(BiomeIDs.DEFAULT)),
		String(BiomeIDs.DEFAULT)
	)
	_arena_biome_id = StringName(saved_biome_id) if BiomeIDs.is_valid(saved_biome_id) else BiomeIDs.DEFAULT

	print("[Debug] Loaded settings from %s" % SETTINGS_PATH)


func _save_settings() -> void:
	var config: ConfigFile = ConfigFile.new()

	# Save visualization toggles
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if _unit_debug:
		if _unit_debug.has_method("IsDebugHurtboxEnabled"):
			config.set_value("debug_menu", "hurtboxes", _unit_debug.call("IsDebugHurtboxEnabled"))
		if _unit_debug.has_method("IsDebugTargetPointEnabled"):
			config.set_value("debug_menu", "target_points", _unit_debug.call("IsDebugTargetPointEnabled"))
		if _unit_debug.has_method("IsDebugEngageRangeEnabled"):
			config.set_value("debug_menu", "engage_ranges", _unit_debug.call("IsDebugEngageRangeEnabled"))

		if _unit_debug.has_method("IsDebugDamageShapeEnabled"):
			config.set_value("debug_menu", "damage_shapes", _unit_debug.call("IsDebugDamageShapeEnabled"))

		var navigation_footprint_enabled: bool = false
		if _unit_debug.has_method("IsDebugNavigationFootprintEnabled"):
			navigation_footprint_enabled = _unit_debug.call("IsDebugNavigationFootprintEnabled")
		config.set_value("debug_menu", "navigation_footprint", navigation_footprint_enabled)
		if _unit_debug.has_method("IsDebugProjectileHitGeometryEnabled"):
			config.set_value("debug_menu", "projectile_hit_geometry", _unit_debug.call("IsDebugProjectileHitGeometryEnabled"))
		if _unit_debug.has_method("IsDebugSummonerBubbleEnabled"):
			config.set_value("debug_menu", "summoner_bubble", _unit_debug.call("IsDebugSummonerBubbleEnabled"))
		if _unit_debug.has_method("IsDebugAbilityLogsEnabled"):
			config.set_value("debug_menu", "ability_logs", _unit_debug.call("IsDebugAbilityLogsEnabled"))
	config.set_value("debug_menu", "bypass_spawn_boundary", _bypass_spawn_boundary)
	config.set_value("debug_menu", "camera_auto_log", _camera_auto_log_enabled)
	config.set_value("debug_menu", "arena_preset_id", _arena_preset_id)
	config.set_value("debug_menu", "arena_biome_id", String(_arena_biome_id))

	config.save(SETTINGS_PATH)
