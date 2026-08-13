extends Control
class_name RaisedPanel

## RaisedPanel - Creates a 3D raised effect using layered panels
##
## Uses the classic "offset shadow" technique: a darker panel behind
## the main panel, offset down and right, creates the illusion of depth.

## Shadow offset (positive = down-right, creates raised look)
@export var shadow_offset: Vector2 = Vector2(4, 4)

## Colors
@export var main_color: Color = GameColorPalette.UI_SURFACE
@export var shadow_color: Color = GameColorPalette.BUTTON_SHADOW
@export var border_color: Color = GameColorPalette.UI_BORDER

## Shape
@export var corner_radius: int = 12
@export var border_width: int = 2

## Internal panels
var _shadow_panel: PanelContainer
var _main_panel: PanelContainer
var _content_container: MarginContainer


func _ready() -> void:
	_create_panels()
	_reparent_children()


func _create_panels() -> void:
	# Shadow panel (behind, offset)
	_shadow_panel = PanelContainer.new()
	_shadow_panel.name = "ShadowPanel"
	add_child(_shadow_panel)
	_shadow_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_shadow_panel.position = shadow_offset
	_apply_shadow_style()

	# Main panel (on top)
	_main_panel = PanelContainer.new()
	_main_panel.name = "MainPanel"
	add_child(_main_panel)
	_main_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_apply_main_style()

	# Content container inside main panel
	_content_container = MarginContainer.new()
	_content_container.name = "ContentContainer"
	_main_panel.add_child(_content_container)
	_content_container.set_anchors_preset(Control.PRESET_FULL_RECT)


func _apply_shadow_style() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = shadow_color
	style.set_corner_radius_all(corner_radius)
	style.anti_aliasing = true
	style.anti_aliasing_size = 1.0
	_shadow_panel.add_theme_stylebox_override("panel", style)


func _apply_main_style() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = main_color
	style.border_color = border_color
	style.border_width_left = border_width
	style.border_width_top = border_width
	style.border_width_right = border_width
	style.border_width_bottom = border_width
	style.set_corner_radius_all(corner_radius)
	style.anti_aliasing = true
	style.anti_aliasing_size = 1.0
	_main_panel.add_theme_stylebox_override("panel", style)


## Move any existing children into the content container
func _reparent_children() -> void:
	# Get children that were added in the scene editor
	var children_to_move: Array[Node] = []
	for child: Node in get_children():
		if child != _shadow_panel and child != _main_panel:
			children_to_move.append(child)

	# Reparent them into the content container
	for child: Node in children_to_move:
		child.reparent(_content_container)


## Get the content container for adding children programmatically
func get_content_container() -> MarginContainer:
	return _content_container


## Update styles when properties change
func _set(property: StringName, value: Variant) -> bool:
	match property:
		"shadow_offset":
			shadow_offset = value
			if _shadow_panel:
				_shadow_panel.position = shadow_offset
			return true
		"main_color", "border_color", "corner_radius", "border_width":
			if _main_panel:
				_apply_main_style()
			return false  # Let default handling occur
		"shadow_color":
			if _shadow_panel:
				_apply_shadow_style()
			return false
	return false
