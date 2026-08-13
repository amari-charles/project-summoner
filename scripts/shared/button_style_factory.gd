class_name ButtonStyleFactory

## ButtonStyleFactory - Generates consistent placeholder button styles.
##
## NinePatch helpers remain for legacy framed panels, while player actions use
## palette-driven flat styles so the global placeholder theme is easy to tune.

## Kenny Fantasy UI Border Variants
## DEFAULT: 48x48 textures, 14px margin - thinner borders
## DOUBLE:  96x96 textures, 28px margin - thicker borders
enum BorderVariant { DEFAULT, DOUBLE }

## Change this to switch all borders at once
const ACTIVE_VARIANT: BorderVariant = BorderVariant.DEFAULT

## Base path for Kenny fantasy-ui-borders assets
const BASE_PATH: String = "res://assets/ui/kenny/fantasy-ui-borders/PNG/"

## Content margins for text padding inside buttons
const CONTENT_MARGIN_H: int = 16


## Get the NinePatch margin for the active variant
static func get_patch_margin() -> int:
	return 28 if ACTIVE_VARIANT == BorderVariant.DOUBLE else 14


## Get the folder name for the active variant
static func get_variant_folder() -> String:
	return "Double" if ACTIVE_VARIANT == BorderVariant.DOUBLE else "Default"


## Get full path to a border texture by its ID (e.g., "panel-border-000")
static func get_border_path(border_id: String) -> String:
	return BASE_PATH + get_variant_folder() + "/Border/" + border_id + ".png"


## Default panel border ID used for UI panels, modals, and drawers
const PANEL_BORDER_ID: String = "panel-border-031"


## Apply panel border styling to a NinePatchRect
## Use this for consistent panel/modal/drawer borders throughout the UI
static func apply_panel_border(nine_patch: NinePatchRect, border_id: String = PANEL_BORDER_ID) -> void:
	nine_patch.texture = load(get_border_path(border_id))
	var margin: int = get_patch_margin()
	nine_patch.patch_margin_left = margin
	nine_patch.patch_margin_top = margin
	nine_patch.patch_margin_right = margin
	nine_patch.patch_margin_bottom = margin


## Button border style (different from panel border 031)
const BUTTON_BORDER_ID: String = "panel-border-028"


## Create a primary button style (main actions)
static func create_primary_normal() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_PRIMARY_BG, GameColorPalette.BUTTON_PRIMARY_BORDER)


static func create_primary_hover() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_PRIMARY_BG_HOVER, GameColorPalette.BUTTON_PRIMARY_BORDER)


static func create_primary_pressed() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_PRIMARY_BG_PRESSED, GameColorPalette.BUTTON_PRIMARY_BORDER)


static func create_primary_disabled() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DISABLED, GameColorPalette.UI_BORDER)


## Create a secondary button style (cancel/back)
static func create_secondary_normal() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_SECONDARY_BG, GameColorPalette.BUTTON_SECONDARY_BORDER)


static func create_secondary_hover() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_SECONDARY_BG_HOVER, GameColorPalette.UI_BORDER_STRONG)


static func create_secondary_pressed() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_SECONDARY_BG_PRESSED, GameColorPalette.BUTTON_SECONDARY_BORDER)


static func create_secondary_disabled() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DISABLED, GameColorPalette.UI_BORDER)


## Create a danger button style (destructive actions)
static func create_danger_normal() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DANGER_BG, GameColorPalette.BUTTON_DANGER_BORDER)


static func create_danger_hover() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DANGER_BG_HOVER, GameColorPalette.BUTTON_DANGER_BORDER)


static func create_danger_pressed() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DANGER_BG_PRESSED, GameColorPalette.BUTTON_DANGER_BORDER)


static func create_danger_disabled() -> StyleBoxFlat:
	return _create_flat_style(GameColorPalette.BUTTON_DISABLED, GameColorPalette.UI_BORDER)


static func _create_flat_style(background: Color, border: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = border
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	style.content_margin_left = CONTENT_MARGIN_H
	style.content_margin_right = CONTENT_MARGIN_H
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	return style
