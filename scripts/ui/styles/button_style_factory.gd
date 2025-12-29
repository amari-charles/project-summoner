class_name ButtonStyleFactory

## ButtonStyleFactory - Generates StyleBoxTexture resources using Kenny fantasy borders
##
## Creates consistent button styles using NinePatch-style textures from Kenny assets.
## Different variants use different panel textures for visual distinction.

## NinePatch margins (matching Kenny fantasy-ui-borders panel sizing)
const PATCH_MARGIN: int = 14

## Content margins for text padding
const CONTENT_MARGIN_H: int = 16
const CONTENT_MARGIN_V: int = 10

## Texture paths for different button variants
const PANEL_PRIMARY: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/Default/Panel/panel-002.png"
const PANEL_SECONDARY: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/Default/Panel/panel-000.png"
const PANEL_DANGER: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/Default/Panel/panel-003.png"


## Create a primary button style (main actions)
static func create_primary_normal() -> StyleBoxTexture:
	return _create_texture_style(PANEL_PRIMARY, Color(0.9, 0.85, 0.7))


static func create_primary_hover() -> StyleBoxTexture:
	return _create_texture_style(PANEL_PRIMARY, Color(1.0, 0.95, 0.8))


static func create_primary_pressed() -> StyleBoxTexture:
	return _create_texture_style(PANEL_PRIMARY, Color(0.7, 0.65, 0.5))


static func create_primary_disabled() -> StyleBoxTexture:
	return _create_texture_style(PANEL_PRIMARY, Color(0.5, 0.5, 0.5, 0.7))


## Create a secondary button style (cancel/back)
static func create_secondary_normal() -> StyleBoxTexture:
	return _create_texture_style(PANEL_SECONDARY, Color(0.8, 0.8, 0.85))


static func create_secondary_hover() -> StyleBoxTexture:
	return _create_texture_style(PANEL_SECONDARY, Color(0.95, 0.95, 1.0))


static func create_secondary_pressed() -> StyleBoxTexture:
	return _create_texture_style(PANEL_SECONDARY, Color(0.6, 0.6, 0.65))


static func create_secondary_disabled() -> StyleBoxTexture:
	return _create_texture_style(PANEL_SECONDARY, Color(0.5, 0.5, 0.5, 0.7))


## Create a danger button style (destructive actions)
static func create_danger_normal() -> StyleBoxTexture:
	return _create_texture_style(PANEL_DANGER, Color(1.0, 0.7, 0.7))


static func create_danger_hover() -> StyleBoxTexture:
	return _create_texture_style(PANEL_DANGER, Color(1.0, 0.85, 0.85))


static func create_danger_pressed() -> StyleBoxTexture:
	return _create_texture_style(PANEL_DANGER, Color(0.8, 0.5, 0.5))


static func create_danger_disabled() -> StyleBoxTexture:
	return _create_texture_style(PANEL_DANGER, Color(0.5, 0.5, 0.5, 0.7))


## Internal helper to create a StyleBoxTexture with NinePatch margins
static func _create_texture_style(texture_path: String, modulate_color: Color) -> StyleBoxTexture:
	var style: StyleBoxTexture = StyleBoxTexture.new()

	# Load texture
	var texture: Texture2D = load(texture_path)
	style.texture = texture

	# Apply color modulation
	style.modulate_color = modulate_color

	# Set NinePatch margins (which parts don't stretch)
	style.texture_margin_left = PATCH_MARGIN
	style.texture_margin_top = PATCH_MARGIN
	style.texture_margin_right = PATCH_MARGIN
	style.texture_margin_bottom = PATCH_MARGIN

	# Content margins for text padding
	style.content_margin_left = CONTENT_MARGIN_H
	style.content_margin_right = CONTENT_MARGIN_H
	style.content_margin_top = CONTENT_MARGIN_V
	style.content_margin_bottom = CONTENT_MARGIN_V

	return style
