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
var _separation_radius_button: Button
var _projectile_hit_geometry_button: Button
var _spawn_boundary_button: Button
var _camera_overlay_button: Button
var _camera_projection_button: Button
var _bypass_spawn_boundary: bool = false  # Local state (formerly in SpatialGrid autoload)
var _unit_debug: Node
var _command_input: LineEdit  # Console command input
var _command_output: Label  # Console command output
var _autocomplete_list: ItemList  # Autocomplete suggestions
var _autocomplete_visible: bool = false

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
		_refresh_camera_projection_button_state()


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
	_attack_range_button.text = "Attack Ranges: Off"
	_attack_range_button.custom_minimum_size = Vector2(200, 32)
	_attack_range_button.pressed.connect(_on_attack_range_toggle_pressed)
	vbox.add_child(_attack_range_button)

	# Separation Radius toggle button
	_separation_radius_button = Button.new()
	_separation_radius_button.text = "Separation Radius: Off"
	_separation_radius_button.custom_minimum_size = Vector2(200, 32)
	_separation_radius_button.pressed.connect(_on_separation_radius_toggle_pressed)
	vbox.add_child(_separation_radius_button)

	# Projectile Hit Geometry toggle button
	_projectile_hit_geometry_button = Button.new()
	_projectile_hit_geometry_button.text = "Projectile Hit Geometry: Off"
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

	# Camera projection mode toggle button
	_camera_projection_button = Button.new()
	_camera_projection_button.text = "Camera Mode: N/A"
	_camera_projection_button.custom_minimum_size = Vector2(200, 32)
	_camera_projection_button.pressed.connect(_on_camera_projection_toggle_pressed)
	vbox.add_child(_camera_projection_button)

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

	if _attack_range_button and _unit_debug and _unit_debug.has_method("IsDebugAttackRangeEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugAttackRangeEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_attack_range_button.text = "Attack Ranges: %s" % state

	if _separation_radius_button and _unit_debug and _unit_debug.has_method("IsDebugSeparationRadiusEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugSeparationRadiusEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_separation_radius_button.text = "Separation Radius: %s" % state

	if _projectile_hit_geometry_button and _unit_debug and _unit_debug.has_method("IsDebugProjectileHitGeometryEnabled"):
		var enabled: bool = SafeTypeUtils.bool_val(_unit_debug.call("IsDebugProjectileHitGeometryEnabled"), false)
		var state: String = "On" if enabled else "Off"
		_projectile_hit_geometry_button.text = "Projectile Hit Geometry: %s" % state

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
	_refresh_camera_projection_button_state()


func _create_fps_button(parent: Node, fps: int, text: String, hotkey: String) -> void:
	var button: Button = Button.new()
	button.text = "%s (%s)" % [text, hotkey]
	button.custom_minimum_size = Vector2(100, 32)
	button.pressed.connect(_on_fps_button_pressed.bind(fps))
	parent.add_child(button)
	_buttons[fps] = button


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
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugAttackRange"):
		return
	_unit_debug.call("ToggleDebugAttackRange")
	_update_button_states()
	_save_settings()


func _on_separation_radius_toggle_pressed() -> void:
	if not _unit_debug:
		_unit_debug = _get_unit_debug_service()
	if not _unit_debug or not _unit_debug.has_method("ToggleDebugSeparationRadius"):
		return
	_unit_debug.call("ToggleDebugSeparationRadius")
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
	_refresh_camera_overlay_button_state()


func _on_camera_projection_toggle_pressed() -> void:
	var camera: Node = _find_battle_camera_controller()
	if not camera or not camera.has_method("toggle_projection_mode"):
		print("[Debug] Battle camera projection toggle unavailable")
		_refresh_camera_projection_button_state()
		return

	camera.call("toggle_projection_mode")
	if camera.has_method("get_projection_mode_name"):
		print("[Debug] Camera mode -> %s" % camera.call("get_projection_mode_name"))
	_refresh_camera_projection_button_state()


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
	if active_camera and active_camera.has_method("set_projection_mode"):
		return active_camera

	# Fallback: search under battlefield root group.
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield:
		var stack: Array[Node] = [battlefield]
		while not stack.is_empty():
			var node: Node = stack.pop_back()
			if node.has_method("set_projection_mode"):
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


func _refresh_camera_projection_button_state() -> void:
	if not _camera_projection_button:
		return

	var camera: Node = _find_battle_camera_controller()
	if not camera or not camera.has_method("get_projection_mode_name"):
		_camera_projection_button.text = "Camera Mode: N/A"
		_camera_projection_button.disabled = true
		return

	_camera_projection_button.disabled = false
	var mode_name_var: Variant = camera.call("get_projection_mode_name")
	var mode_name: String = SafeTypeUtils.string(mode_name_var, "Unknown")
	_camera_projection_button.text = "Camera Mode: %s" % mode_name


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
		if _unit_debug.has_method("SetDebugAttackRangeEnabled"):
			_unit_debug.call("SetDebugAttackRangeEnabled", config.get_value("debug_menu", "attack_ranges", false))
		if _unit_debug.has_method("SetDebugSeparationRadiusEnabled"):
			_unit_debug.call("SetDebugSeparationRadiusEnabled", config.get_value("debug_menu", "separation_radius", false))
		if _unit_debug.has_method("SetDebugProjectileHitGeometryEnabled"):
			_unit_debug.call("SetDebugProjectileHitGeometryEnabled", config.get_value("debug_menu", "projectile_hit_geometry", false))
	_bypass_spawn_boundary = config.get_value("debug_menu", "bypass_spawn_boundary", false)

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
		if _unit_debug.has_method("IsDebugAttackRangeEnabled"):
			config.set_value("debug_menu", "attack_ranges", _unit_debug.call("IsDebugAttackRangeEnabled"))
		if _unit_debug.has_method("IsDebugSeparationRadiusEnabled"):
			config.set_value("debug_menu", "separation_radius", _unit_debug.call("IsDebugSeparationRadiusEnabled"))
		if _unit_debug.has_method("IsDebugProjectileHitGeometryEnabled"):
			config.set_value("debug_menu", "projectile_hit_geometry", _unit_debug.call("IsDebugProjectileHitGeometryEnabled"))
	config.set_value("debug_menu", "bypass_spawn_boundary", _bypass_spawn_boundary)

	config.save(SETTINGS_PATH)
