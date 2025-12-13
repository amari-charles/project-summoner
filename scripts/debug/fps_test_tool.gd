extends Node
# FPSTestTool is registered as an autoload, no class_name needed

## FPS Test Tool - Debug utility for testing framerate independence
##
## Provides an on-screen UI panel with FPS controls for manual testing.
## Only active in debug builds - automatically disabled in release.
##
## Toggle UI: ` (backtick) or F12
## Hotkeys (work even when UI hidden):
##   F5 - Set to 30 FPS (low-end mobile simulation)
##   F6 - Set to 60 FPS (standard)
##   F7 - Set to 120 FPS (high refresh rate)
##   F8 - Uncapped FPS
##
## Usage:
##   Run any scene - FPS panel shows by default.
##   Press ` (backtick) or F12 to hide/show.
##   Use buttons or hotkeys to switch FPS caps.
##   Verify that gameplay speed remains consistent across all settings.

## UI references
var _panel: PanelContainer
var _fps_label: Label
var _target_label: Label
var _buttons: Dictionary = {}  # fps -> Button
var _grid_button: Button


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	if not OS.is_debug_build():
		queue_free()
		return

	# Always process, even when paused
	process_mode = Node.PROCESS_MODE_ALWAYS

	# Create UI after a frame to ensure tree is ready
	call_deferred("_create_ui")
	print("[FPS Test] Ready - Press ` or F12 to toggle panel, F5-F8 for quick FPS change")


func _process(_delta: float) -> void:
	if _fps_label:
		var current_fps: float = Engine.get_frames_per_second()
		_fps_label.text = "FPS: %.1f" % current_fps


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
	canvas_layer.layer = 100  # High layer to ensure visibility above game UI and HUD
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
	title.text = "FPS Test Tool"
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

	# Grid Lines toggle button
	_grid_button = Button.new()
	_grid_button.text = "Grid Lines: Off"
	_grid_button.custom_minimum_size = Vector2(200, 32)
	_grid_button.pressed.connect(_on_grid_toggle_pressed)
	vbox.add_child(_grid_button)

	# Start hidden by default (press ` or F12 to show)
	_panel.visible = false


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


func _set_fps(target: int) -> void:
	Engine.max_fps = target

	var label: String = "Uncapped" if target == 0 else "%d FPS" % target
	if _target_label:
		_target_label.text = "Target: %s" % label

	# Update button states
	for fps: int in _buttons:
		var button: Button = _buttons[fps]
		button.disabled = (fps == target)

	print("[FPS Test] Set to %s" % label)


func _on_fps_button_pressed(fps: int) -> void:
	_set_fps(fps)


func _on_grid_toggle_pressed() -> void:
	SpatialGrid.toggle_debug()
	var state: String = "On" if SpatialGrid.is_debug_enabled() else "Off"
	_grid_button.text = "Grid Lines: %s" % state
