extends PanelContainer
class_name CardXPItem

## CardXPItem - Displays card XP info on the reward screen
##
## Shows card name, current level, and "LEVEL UP!" indicator if ready.
## Clickable to view card details (stats, progression, upgrades).

## Signals
signal clicked(instance_id: String)

## State
var instance_id: String = ""
var catalog_id: String = ""
var can_level_up: bool = false

## Node references
@onready var card_name_label: Label = %CardNameLabel
@onready var level_label: Label = %LevelLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var level_up_indicator: Label = %LevelUpIndicator

## Colors
const LEVEL_UP_COLOR: Color = Color(0.2, 0.9, 0.3)  ## Green for level up
const NORMAL_COLOR: Color = Color(0.8, 0.8, 0.8)  ## Gray for normal

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect click handling
	gui_input.connect(_on_gui_input)
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

## =============================================================================
## PUBLIC API
## =============================================================================

## Set up the item with card data
func setup(p_instance_id: String, p_catalog_id: String, card_name: String, level: int, p_can_level_up: bool, xp_progress: float = 0.0) -> void:
	instance_id = p_instance_id
	catalog_id = p_catalog_id
	can_level_up = p_can_level_up

	# Update display
	card_name_label.text = _truncate_name(card_name, 12)
	level_label.text = Loc.t("ui.reward.card_level", {"level": level})

	# Update XP progress bar
	if xp_progress_bar:
		xp_progress_bar.value = xp_progress * 100.0
		_apply_xp_bar_style()

	# Set tooltip to indicate clickability
	tooltip_text = Loc.t("ui.reward.card_xp_tooltip")

	# Show/hide level up indicator
	if can_level_up:
		level_up_indicator.text = Loc.t("ui.reward.card_level_up_ready")
		level_up_indicator.visible = true
		_apply_level_up_style()
	else:
		level_up_indicator.visible = false
		_apply_normal_style()

## =============================================================================
## DISPLAY HELPERS
## =============================================================================

func _truncate_name(name: String, max_length: int) -> String:
	if name.length() > max_length:
		return name.substr(0, max_length - 2) + ".."
	return name

func _apply_level_up_style() -> void:
	# Highlight border for level-up ready cards
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.15, 0.1)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = LEVEL_UP_COLOR
	style.set_corner_radius_all(6)
	add_theme_stylebox_override("panel", style)

func _apply_normal_style() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.15)
	style.border_width_left = 1
	style.border_width_top = 1
	style.border_width_right = 1
	style.border_width_bottom = 1
	style.border_color = Color(0.3, 0.3, 0.35)
	style.set_corner_radius_all(6)
	add_theme_stylebox_override("panel", style)

func _apply_hover_style() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	if can_level_up:
		style.bg_color = Color(0.15, 0.2, 0.15)
		style.border_color = LEVEL_UP_COLOR.lightened(0.2)
	else:
		style.bg_color = Color(0.18, 0.18, 0.22)
		style.border_color = Color(0.5, 0.5, 0.55)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.set_corner_radius_all(6)
	add_theme_stylebox_override("panel", style)

func _apply_xp_bar_style() -> void:
	if not xp_progress_bar:
		return

	# Background/track style
	var bg_style: StyleBoxFlat = StyleBoxFlat.new()
	bg_style.bg_color = Color(0.15, 0.15, 0.18)
	bg_style.set_corner_radius_all(2)
	xp_progress_bar.add_theme_stylebox_override("background", bg_style)

	# Fill style - green if can level up, cyan otherwise
	var fill_style: StyleBoxFlat = StyleBoxFlat.new()
	if can_level_up:
		fill_style.bg_color = LEVEL_UP_COLOR
	else:
		fill_style.bg_color = Color(0.25, 0.88, 0.82)  # Cyan
	fill_style.set_corner_radius_all(2)
	xp_progress_bar.add_theme_stylebox_override("fill", fill_style)

## =============================================================================
## INPUT HANDLING
## =============================================================================

func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
			clicked.emit(instance_id)

func _on_mouse_entered() -> void:
	_apply_hover_style()
	# Change cursor to indicate clickable
	mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

func _on_mouse_exited() -> void:
	if can_level_up:
		_apply_level_up_style()
	else:
		_apply_normal_style()
	mouse_default_cursor_shape = Control.CURSOR_ARROW
