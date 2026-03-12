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

## UI references
var _panel: PanelContainer
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
var _spawn_boundary_button: Button
var _camera_overlay_button: Button
var _camera_auto_log_button: Button
var _bypass_spawn_boundary: bool = false  # Local state (formerly in SpatialGrid autoload)
var _unit_debug: Node
var _command_input: LineEdit  # Console command input
var _command_output: Label  # Console command output
var _autocomplete_list: ItemList  # Autocomplete suggestions
var _autocomplete_visible: bool = false
var _camera_auto_log_enabled: bool = false
var _camera_auto_log_elapsed: float = 0.0

const CAMERA_AUTO_LOG_INTERVAL_SECONDS: float = 5.0

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	if not OS.is_debug_build():
		queue_free()
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

	if _camera_auto_log_enabled:
		_camera_auto_log_elapsed += _delta
		if _camera_auto_log_elapsed >= CAMERA_AUTO_LOG_INTERVAL_SECONDS:
			_camera_auto_log_elapsed = 0.0
			_log_active_camera_snapshot()


func _input(event: InputEvent) -> void:
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

	# Button grid
	var grid: GridContainer = GridContainer.new()
	grid.columns = 2
	grid.add_theme_constant_override("h_separation", 8)
	grid.add_theme_constant_override("v_separation", 6)
	vbox.add_child(grid)

	# Create buttons
	_create_fps_button(grid, 30, "30 FPS", "F5")
	_create_fps_button(grid, 60, "60 FPS", "F6")
	_create_fps_button(grid, 120, "120 FPS", "F7")
	_create_fps_button(grid, 0, "Uncapped", "F8")

	# Instructions
	var instructions: Label = Label.new()
	instructions.text = "` or F12 to hide"
	instructions.add_theme_font_size_override("font_size", 11)
	instructions.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	instructions.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(instructions)

	# Debug toggles separator
	var debug_separator: HSeparator = HSeparator.new()
	vbox.add_child(debug_separator)

	# Skip Prep Phase button
	_skip_prep_button = Button.new()
	_skip_prep_button.text = "Skip Prep Phase"
	_skip_prep_button.custom_minimum_size = Vector2(200, 32)
	_skip_prep_button.pressed.connect(_on_skip_prep_pressed)
	vbox.add_child(_skip_prep_button)

	# Hurtbox toggle button
	_hurtbox_button = Button.new()
	_hurtbox_button.text = "Hurtboxes: Off"
	_hurtbox_button.custom_minimum_size = Vector2(200, 32)
	_hurtbox_button.pressed.connect(_on_hurtbox_toggle_pressed)
	vbox.add_child(_hurtbox_button)

	# Target Point toggle button
	_target_point_button = Button.new()
	_target_point_button.text = "Target Points: Off"
	_target_point_button.custom_minimum_size = Vector2(200, 32)
	_target_point_button.pressed.connect(_on_target_point_toggle_pressed)
	vbox.add_child(_target_point_button)

	# Attack Range toggle button
	_attack_range_button = Button.new()
	_attack_range_button.text = "Engage Range: Off"
	_attack_range_button.custom_minimum_size = Vector2(200, 32)
	_attack_range_button.pressed.connect(_on_attack_range_toggle_pressed)
	vbox.add_child(_attack_range_button)

	# Damage Shape toggle button
	_damage_shape_button = Button.new()
	_damage_shape_button.text = "Damage Shapes: Off"
	_damage_shape_button.custom_minimum_size = Vector2(200, 32)
	_damage_shape_button.pressed.connect(_on_damage_shape_toggle_pressed)
	vbox.add_child(_damage_shape_button)

	# Navigation Footprint toggle button
	_navigation_footprint_button = Button.new()
	_navigation_footprint_button.text = "Navigation Footprint: Off"
	_navigation_footprint_button.custom_minimum_size = Vector2(200, 32)
	_navigation_footprint_button.pressed.connect(_on_navigation_footprint_toggle_pressed)
	vbox.add_child(_navigation_footprint_button)

	# Projectile Hit Geometry toggle button
	_projectile_hit_geometry_button = Button.new()
	_projectile_hit_geometry_button.text = "Projectile Hit Radius: Off"
	_projectile_hit_geometry_button.custom_minimum_size = Vector2(200, 32)
	_projectile_hit_geometry_button.pressed.connect(_on_projectile_hit_geometry_toggle_pressed)
	vbox.add_child(_projectile_hit_geometry_button)

	# Spawn Boundary Bypass toggle button
	_spawn_boundary_button = Button.new()
	_spawn_boundary_button.text = "Spawn Boundary: On"
	_spawn_boundary_button.custom_minimum_size = Vector2(200, 32)
	_spawn_boundary_button.pressed.connect(_on_spawn_boundary_toggle_pressed)
	vbox.add_child(_spawn_boundary_button)

	# Camera bounds overlay toggle button
	_camera_overlay_button = Button.new()
	_camera_overlay_button.text = "Camera Overlay: N/A"
	_camera_overlay_button.custom_minimum_size = Vector2(200, 32)
	_camera_overlay_button.pressed.connect(_on_camera_overlay_toggle_pressed)
	vbox.add_child(_camera_overlay_button)

	_camera_auto_log_button = Button.new()
	_camera_auto_log_button.text = "Camera Auto-Log: Off"
	_camera_auto_log_button.custom_minimum_size = Vector2(200, 32)
	_camera_auto_log_button.pressed.connect(_on_camera_auto_log_toggle_pressed)
	vbox.add_child(_camera_auto_log_button)

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
	_command_input.custom_minimum_size = Vector2(200, 32)
	_command_input.text_submitted.connect(_on_command_submitted)
	_command_input.text_changed.connect(_on_command_text_changed)
	_command_input.gui_input.connect(_on_command_input_gui_input)
	vbox.add_child(_command_input)

	# Autocomplete list
	_autocomplete_list = ItemList.new()
	_autocomplete_list.custom_minimum_size = Vector2(200, 150)
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
	_command_output.custom_minimum_size = Vector2(200, 0)
	vbox.add_child(_command_output)

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

	var open_arena_map_button: Button = Button.new()
	open_arena_map_button.text = "Open Test Arena Map"
	open_arena_map_button.custom_minimum_size = Vector2(200, 32)
	open_arena_map_button.pressed.connect(_on_open_test_arena_map_pressed)
	vbox.add_child(open_arena_map_button)

	var arena_grid: GridContainer = GridContainer.new()
	arena_grid.columns = 2
	arena_grid.add_theme_constant_override("h_separation", 8)
	arena_grid.add_theme_constant_override("v_separation", 6)
	vbox.add_child(arena_grid)

	_add_debug_arena_button(arena_grid, "Earth Sprite", BattleIDs.ARENA_EARTH_SPRITE)
	_add_debug_arena_button(arena_grid, "Puff", BattleIDs.ARENA_PUFF)
	_add_debug_arena_button(arena_grid, "Fire Wisp", BattleIDs.ARENA_FIRE_WISP)
	_add_debug_arena_button(arena_grid, "Cloud Swarm", BattleIDs.ARENA_CLOUD_SWARM)
	_add_debug_arena_button(arena_grid, "Mana Bolt", BattleIDs.ARENA_MANA_BOLT)
	_add_debug_arena_button(arena_grid, "Debug Arena", BattleIDs.DEBUG_ARENA)

	# Snapshots separator
	var snapshot_separator: HSeparator = HSeparator.new()
	vbox.add_child(snapshot_separator)

	# Manage Snapshots button
	var snapshots_button: Button = Button.new()
	snapshots_button.text = "Manage Snapshots"
	snapshots_button.custom_minimum_size = Vector2(200, 32)
	snapshots_button.pressed.connect(_on_snapshots_pressed)
	vbox.add_child(snapshots_button)

	# Start hidden by default (press ` or F12 to show)
	_panel.visible = false

	# Update button text to reflect loaded settings
	_update_button_states()
	_apply_spawn_boundary_bypass()


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
	var success: bool = DevConsole.execute_command(command)

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
		matches = DevConsole.get_all_commands()
	else:
		matches = DevConsole.get_matching_commands(text)

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
		_command_input.grab_focus()

	_hide_autocomplete()


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
	var success: bool = CampaignApi.set_current_campaign(campaign_id)
	if not success:
		print("[Debug] Failed to switch campaign to '%s'" % campaign_id)
		return

	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
	print("[Debug] Opened Test Arena campaign map")


func _on_debug_arena_battle_pressed(battle_id: String) -> void:
	if battle_id.is_empty():
		return

	var campaign_id: String = String(CampaignIDs.TEST_ARENA)
	var campaign_set: bool = CampaignApi.set_current_campaign(campaign_id)
	if not campaign_set:
		print("[Debug] Failed to switch campaign to '%s'" % campaign_id)
		return

	ProfileRepoApi.update_campaign_progress_dict({"current_battle": battle_id}, "")
	BattleContext.configure_campaign_battle(battle_id)

	var event_data: Dictionary = CampaignApi.get_battle(battle_id)
	var battle_scene: String = SceneManager.SCENE_BATTLE_3D
	var custom_scene: String = SafeTypeUtils.string(event_data.get("scene_path", ""), "")
	if not custom_scene.is_empty():
		battle_scene = custom_scene

	SceneManager.transition_to(battle_scene)
	print("[Debug] Launched test arena battle '%s'" % battle_id)


func _on_snapshots_pressed() -> void:
	# Load and show the snapshot manager scene
	var snapshot_scene: PackedScene = load("res://scenes/meta/screens/snapshot_manager.tscn")
	if snapshot_scene:
		var snapshot_manager: Node = snapshot_scene.instantiate()
		get_tree().root.add_child(snapshot_manager)
		if snapshot_manager.has_method("show_manager"):
			snapshot_manager.call("show_manager")


func _get_battlefield_debug_service() -> Node:
	return get_node_or_null(CSharpAutoloads.BATTLEFIELD_DEBUG)


func _get_unit_debug_service() -> Node:
	return _get_battlefield_debug_service()


func _find_battle_camera_controller() -> Node:
	# Prefer active viewport camera first.
	var active_camera: Camera3D = get_viewport().get_camera_3d()
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
	_bypass_spawn_boundary = config.get_value("debug_menu", "bypass_spawn_boundary", false)
	_camera_auto_log_enabled = config.get_value("debug_menu", "camera_auto_log", false)
	_camera_auto_log_elapsed = 0.0

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
	config.set_value("debug_menu", "bypass_spawn_boundary", _bypass_spawn_boundary)
	config.set_value("debug_menu", "camera_auto_log", _camera_auto_log_enabled)

	config.save(SETTINGS_PATH)
