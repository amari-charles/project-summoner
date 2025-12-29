class_name ButtonStyleFactory

## ButtonStyleFactory - Generates StyleBoxFlat resources for polished buttons
##
## Creates consistent button styles with shadows, rounded corners, and borders.
## All buttons use code-generated styles (no art assets required).

## Button style constants
const CORNER_RADIUS: int = 8
const BORDER_WIDTH: int = 2
const BORDER_WIDTH_SUBTLE: int = 1
const SHADOW_SIZE: int = 4
const SHADOW_OFFSET: Vector2 = Vector2(0, 2)
const CONTENT_MARGIN_H: int = 16
const CONTENT_MARGIN_V: int = 10


## Create a primary button style (gold border, main actions)
static func create_primary_normal() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_PRIMARY_BG,
		GameColorPalette.BUTTON_PRIMARY_BORDER,
		BORDER_WIDTH,
		true  # with shadow
	)


static func create_primary_hover() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_PRIMARY_BG_HOVER,
		GameColorPalette.BUTTON_PRIMARY_BORDER.lightened(0.1),
		BORDER_WIDTH,
		true
	)


static func create_primary_pressed() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_PRIMARY_BG_PRESSED,
		GameColorPalette.BUTTON_PRIMARY_BORDER.darkened(0.1),
		BORDER_WIDTH,
		false  # no shadow when pressed (appears inset)
	)


static func create_primary_disabled() -> StyleBoxFlat:
	var style: StyleBoxFlat = _create_style(
		GameColorPalette.BUTTON_PRIMARY_BG.darkened(0.2),
		GameColorPalette.BUTTON_PRIMARY_BORDER.darkened(0.4),
		BORDER_WIDTH,
		false
	)
	return style


## Create a secondary button style (neutral border, cancel/back)
static func create_secondary_normal() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_SECONDARY_BG,
		GameColorPalette.BUTTON_SECONDARY_BORDER,
		BORDER_WIDTH,
		true  # with shadow for visibility
	)


static func create_secondary_hover() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_SECONDARY_BG_HOVER,
		GameColorPalette.BUTTON_SECONDARY_BORDER.lightened(0.2),
		BORDER_WIDTH,
		true
	)


static func create_secondary_pressed() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_SECONDARY_BG_PRESSED,
		GameColorPalette.BUTTON_SECONDARY_BORDER.darkened(0.1),
		BORDER_WIDTH,
		false
	)


static func create_secondary_disabled() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_DISABLED,
		GameColorPalette.BUTTON_SECONDARY_BORDER.darkened(0.3),
		BORDER_WIDTH,
		false
	)


## Create a danger button style (red border, destructive actions)
static func create_danger_normal() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_DANGER_BG,
		GameColorPalette.BUTTON_DANGER_BORDER,
		BORDER_WIDTH,
		true
	)


static func create_danger_hover() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_DANGER_BG_HOVER,
		GameColorPalette.BUTTON_DANGER_BORDER.lightened(0.1),
		BORDER_WIDTH,
		true
	)


static func create_danger_pressed() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_DANGER_BG_PRESSED,
		GameColorPalette.BUTTON_DANGER_BORDER.darkened(0.1),
		BORDER_WIDTH,
		false
	)


static func create_danger_disabled() -> StyleBoxFlat:
	return _create_style(
		GameColorPalette.BUTTON_DANGER_BG.darkened(0.2),
		GameColorPalette.BUTTON_DANGER_BORDER.darkened(0.4),
		BORDER_WIDTH,
		false
	)


## Internal helper to create a StyleBoxFlat with common properties
static func _create_style(
	bg_color: Color,
	border_color: Color,
	border_width: int,
	with_shadow: bool
) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()

	# Background
	style.bg_color = bg_color

	# Border
	style.border_color = border_color
	style.border_width_left = border_width
	style.border_width_top = border_width
	style.border_width_right = border_width
	style.border_width_bottom = border_width

	# Rounded corners
	style.set_corner_radius_all(CORNER_RADIUS)

	# Anti-aliasing for smooth edges
	style.anti_aliasing = true
	style.anti_aliasing_size = 1.0

	# Shadow (optional)
	if with_shadow:
		style.shadow_color = GameColorPalette.BUTTON_SHADOW
		style.shadow_offset = SHADOW_OFFSET
		style.shadow_size = SHADOW_SIZE
		# Expand margin to show shadow outside bounds
		style.expand_margin_bottom = SHADOW_SIZE

	# Content margins for padding
	style.content_margin_left = CONTENT_MARGIN_H
	style.content_margin_right = CONTENT_MARGIN_H
	style.content_margin_top = CONTENT_MARGIN_V
	style.content_margin_bottom = CONTENT_MARGIN_V

	return style
