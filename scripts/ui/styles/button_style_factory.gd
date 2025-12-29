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

## Texture path for border-only texture (transparent center)
const BORDER_TEXTURE: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/Double/Border/panel-border-000.png"


## Create a primary button style (main actions)
static func create_primary_normal() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color.WHITE)


static func create_primary_hover() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(1.0, 1.0, 0.85))


static func create_primary_pressed() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.7, 0.7, 0.7))


static func create_primary_disabled() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.5, 0.5, 0.5, 0.5))


## Create a secondary button style (cancel/back)
static func create_secondary_normal() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.8, 0.8, 0.8))


static func create_secondary_hover() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(1.0, 1.0, 1.0))


static func create_secondary_pressed() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.6, 0.6, 0.6))


static func create_secondary_disabled() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.4, 0.4, 0.4, 0.5))


## Create a danger button style (destructive actions)
static func create_danger_normal() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(1.0, 0.6, 0.6))


static func create_danger_hover() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(1.0, 0.8, 0.8))


static func create_danger_pressed() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.8, 0.4, 0.4))


static func create_danger_disabled() -> StyleBoxTexture:
	return _create_texture_style(BORDER_TEXTURE, Color(0.5, 0.4, 0.4, 0.5))


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
