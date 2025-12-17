extends Control
class_name StatBar

## Simple stat bar for HP, Mana, or other values
## Displays a fill bar with optional label

## Bar colors (configurable via set_colors)
var fill_color: Color = Color(0.8, 0.2, 0.2)  ## Default red for HP
var bg_color: Color = Color(0.1, 0.1, 0.15, 0.9)
var highlight_color: Color = Color(1.0, 1.0, 1.0, 0.3)

## UI styling
const FILL_PADDING: float = 3.0
const HIGHLIGHT_HEIGHT: float = 3.0
const FILL_ANIM_DURATION: float = 0.15

## Node references (created dynamically)
var background: ColorRect = null
var fill: ColorRect = null
var highlight: ColorRect = null
var value_label: Label = null

## State
var current_value: float = 0.0
var max_value: float = 100.0
var fill_tween: Tween = null
var show_label: bool = true
var label_format: String = "{current}/{max}"

func _ready() -> void:
	_create_ui()
	# Initialize display after layout
	await get_tree().process_frame
	_update_display(max_value, max_value, false)

func _create_ui() -> void:
	# Background
	background = ColorRect.new()
	background.name = "Background"
	background.color = bg_color
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(background)

	# Fill bar
	fill = ColorRect.new()
	fill.name = "Fill"
	fill.color = fill_color
	fill.position = Vector2(FILL_PADDING, FILL_PADDING)
	fill.size = Vector2(0, 0)
	add_child(fill)

	# Highlight (top edge shine)
	highlight = ColorRect.new()
	highlight.name = "Highlight"
	highlight.color = highlight_color
	highlight.position = Vector2(FILL_PADDING, FILL_PADDING)
	highlight.size = Vector2(0, HIGHLIGHT_HEIGHT)
	add_child(highlight)

	# Value label
	value_label = Label.new()
	value_label.name = "ValueLabel"
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	value_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	value_label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	value_label.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0))
	value_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
	value_label.add_theme_constant_override("outline_size", 3)
	add_child(value_label)

## Set bar colors
func set_colors(fill_col: Color, bg_col: Color = Color(0.1, 0.1, 0.15, 0.9)) -> void:
	fill_color = fill_col
	bg_color = bg_col
	if fill:
		fill.color = fill_color
	if background:
		background.color = bg_color

## Update the bar value
func update_value(current: float, maximum: float) -> void:
	current_value = current
	max_value = maximum
	_update_display(current, maximum, true)

func _update_display(current: float, maximum: float, animate: bool) -> void:
	if not background or not fill:
		return

	# Calculate bar dimensions
	var bar_width: float = size.x - (FILL_PADDING * 2)
	var bar_height: float = size.y - (FILL_PADDING * 2)

	# Calculate fill percentage
	var percent: float = clampf(current / maximum, 0.0, 1.0) if maximum > 0 else 0.0
	var target_width: float = bar_width * percent

	# Update fill height
	fill.size.y = bar_height

	# Cancel existing animation
	if animate and fill_tween and fill_tween.is_running():
		fill_tween.kill()

	if animate:
		fill_tween = create_tween()
		fill_tween.set_ease(Tween.EASE_OUT)
		fill_tween.set_trans(Tween.TRANS_CUBIC)
		fill_tween.set_parallel(true)
		fill_tween.tween_property(fill, "size:x", target_width, FILL_ANIM_DURATION)
		fill_tween.tween_property(highlight, "size:x", target_width, FILL_ANIM_DURATION)
	else:
		fill.size.x = target_width
		highlight.size.x = target_width

	# Update label
	if value_label:
		value_label.visible = show_label
		if show_label:
			value_label.text = label_format.replace("{current}", str(int(current))).replace("{max}", str(int(maximum)))

## Configure label visibility and format
func set_label_config(visible: bool, format: String = "{current}/{max}") -> void:
	show_label = visible
	label_format = format
	if value_label:
		value_label.visible = visible
