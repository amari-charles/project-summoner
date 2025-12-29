class_name ButtonStyleFactory

## ButtonStyleFactory - Generates StyleBoxTexture resources using Kenny fantasy borders
##
## Creates consistent button styles using NinePatch-style textures from Kenny assets.

## Kenny Fantasy UI Border Variants
## DEFAULT: 48x48 textures, 14px margin - thinner borders
## DOUBLE:  96x96 textures, 28px margin - thicker borders
enum BorderVariant { DEFAULT, DOUBLE }

## Change this to switch all borders at once
const ACTIVE_VARIANT: BorderVariant = BorderVariant.DEFAULT

## Base path for Kenny fantasy-ui-borders assets
const BASE_PATH: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/"

## Content margins for text padding
const CONTENT_MARGIN_H: int = 16
const CONTENT_MARGIN_V: int = 10


## Get the NinePatch margin for the active variant
static func get_patch_margin() -> int:
	return 28 if ACTIVE_VARIANT == BorderVariant.DOUBLE else 14


## Get the folder name for the active variant
static func get_variant_folder() -> String:
	return "Double" if ACTIVE_VARIANT == BorderVariant.DOUBLE else "Default"


## Get full path to a border texture by its ID (e.g., "panel-border-000")
static func get_border_path(border_id: String) -> String:
	return BASE_PATH + get_variant_folder() + "/Border/" + border_id + ".png"


## Create a primary button style (main actions)
static func create_primary_normal() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color.WHITE)


static func create_primary_hover() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(1.0, 1.0, 0.85))


static func create_primary_pressed() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.7, 0.7, 0.7))


static func create_primary_disabled() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.5, 0.5, 0.5, 0.5))


## Create a secondary button style (cancel/back)
static func create_secondary_normal() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.8, 0.8, 0.8))


static func create_secondary_hover() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(1.0, 1.0, 1.0))


static func create_secondary_pressed() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.6, 0.6, 0.6))


static func create_secondary_disabled() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.4, 0.4, 0.4, 0.5))


## Create a danger button style (destructive actions)
static func create_danger_normal() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(1.0, 0.6, 0.6))


static func create_danger_hover() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(1.0, 0.8, 0.8))


static func create_danger_pressed() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.8, 0.4, 0.4))


static func create_danger_disabled() -> StyleBoxTexture:
	return _create_texture_style(get_border_path("panel-border-000"), Color(0.5, 0.4, 0.4, 0.5))


## Internal helper to create a StyleBoxTexture with NinePatch margins
static func _create_texture_style(texture_path: String, modulate_color: Color) -> StyleBoxTexture:
	var style: StyleBoxTexture = StyleBoxTexture.new()

	# Load texture
	var texture: Texture2D = load(texture_path)
	style.texture = texture

	# Apply color modulation
	style.modulate_color = modulate_color

	# Set NinePatch margins (which parts don't stretch)
	var margin: int = get_patch_margin()
	style.texture_margin_left = margin
	style.texture_margin_top = margin
	style.texture_margin_right = margin
	style.texture_margin_bottom = margin

	# Content margins for text padding
	style.content_margin_left = CONTENT_MARGIN_H
	style.content_margin_right = CONTENT_MARGIN_H
	style.content_margin_top = CONTENT_MARGIN_V
	style.content_margin_bottom = CONTENT_MARGIN_V

	return style
