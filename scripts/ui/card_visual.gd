extends Control
class_name CardVisual

## CardVisual - Reusable card display component with element-based theming
##
## Displays a card with:
## - Element-colored border
## - Cost circle (top-left)
## - Card name (top-middle)
## - Card art (center)
## - Optional description (bottom)
##
## Usage:
##   var card_visual = CardVisual.new()
##   card_visual.set_card_data(card_data, show_description)

## =============================================================================
## EXPORTS - Layout Configuration
## =============================================================================

## General layout
@export_group("General")
@export var show_description: bool = false
@export var border_width: int = 3
@export var corner_radius: int = 8

## Cost label
@export_group("Cost Label")
@export var cost_font_size: int = 20

## Card name
@export_group("Card Name")
@export var name_font_size: int = 12

## Description
@export_group("Description")
@export var description_font_size: int = 9
@export var description_max_chars: int = 100

## Element badge
@export_group("Element Badge")
@export var element_badge_radius: int = 10

## =============================================================================
## NODES
## =============================================================================

@onready var border_panel: Panel = $BorderPanel
@onready var background_panel: Panel = $BackgroundPanel
@onready var cost_label: Label = $CostLabel
@onready var type_icon: TextureRect = $TypeIcon
@onready var name_label: Label = $NameLabel
@onready var art_container: Control = $ArtContainer
@onready var art_texture: TextureRect = $ArtContainer/ArtTexture
@onready var art_placeholder: ColorRect = $ArtContainer/ArtPlaceholder
@onready var description_label: Label = $DescriptionLabel
@onready var element_badge: Panel = $ElementBadge

## =============================================================================
## CARD DATA
## =============================================================================

var card_data: Dictionary = {}
var element_color: Color = Color.GRAY

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready():
	# Initial setup
	if card_data:
		_apply_visual_styling()

## =============================================================================
## PUBLIC API
## =============================================================================

## Set the card data and update visuals
func set_card_data(data: Dictionary, show_desc: bool = false) -> void:
	card_data = data
	show_description = show_desc

	# Get element color for this card
	element_color = CardVisualHelper.get_card_element_color(card_data)

	# Update all visual elements
	_apply_visual_styling()
	_update_cost()
	_update_name()
	_update_type_icon()
	_update_art()
	_update_description()

## Update just the element color (useful for theme switching)
func set_element_color(color: Color) -> void:
	element_color = color
	if is_inside_tree():
		_apply_border_color()

## =============================================================================
## VISUAL UPDATES
## =============================================================================

func _apply_visual_styling() -> void:
	if not is_inside_tree():
		return

	# Apply element-colored border
	_apply_border_color()

	# Apply dark background
	if background_panel:
		var bg_style = StyleBoxFlat.new()
		bg_style.bg_color = GameColorPalette.UI_BG_DARK
		bg_style.set_corner_radius_all(corner_radius - border_width)
		bg_style.anti_aliasing = true
		bg_style.anti_aliasing_size = 1
		background_panel.add_theme_stylebox_override("panel", bg_style)

	# Apply cost label font size
	if cost_label:
		cost_label.add_theme_font_size_override("font_size", cost_font_size)

	# Apply name label font size
	if name_label:
		name_label.add_theme_font_size_override("font_size", name_font_size)

	# Apply description label font size
	if description_label:
		description_label.add_theme_font_size_override("font_size", description_font_size)

	# Apply element badge styling
	if element_badge:
		var badge_style = StyleBoxFlat.new()
		badge_style.bg_color = element_color
		badge_style.set_corner_radius_all(element_badge_radius)
		badge_style.anti_aliasing = true
		badge_style.anti_aliasing_size = 1
		element_badge.add_theme_stylebox_override("panel", badge_style)

	# Show/hide description
	if description_label:
		description_label.visible = show_description

func _apply_border_color() -> void:
	if border_panel:
		var border_style = StyleBoxFlat.new()
		border_style.bg_color = element_color
		border_style.set_corner_radius_all(corner_radius)
		border_style.anti_aliasing = true
		border_style.anti_aliasing_size = 1
		border_panel.add_theme_stylebox_override("panel", border_style)

func _update_cost() -> void:
	if cost_label and card_data.has("mana_cost"):
		cost_label.text = str(card_data.mana_cost)

func _update_name() -> void:
	if name_label and card_data.has("card_name"):
		name_label.text = card_data.card_name

func _update_type_icon() -> void:
	if type_icon:
		var icon_path = CardVisualHelper.get_card_type_icon_path(card_data)
		if not icon_path.is_empty():
			type_icon.texture = load(icon_path)
			type_icon.visible = true
		else:
			type_icon.visible = false

func _update_art() -> void:
	if not art_container:
		return

	# Try to load card art if path is specified
	var art_loaded = false
	if card_data.has("card_icon_path") and not card_data.card_icon_path.is_empty():
		var texture = load(card_data.card_icon_path)
		if texture and art_texture:
			art_texture.texture = texture
			art_texture.visible = true
			art_placeholder.visible = false
			art_loaded = true

	# Fall back to colored placeholder
	if not art_loaded and art_placeholder:
		art_placeholder.color = element_color.darkened(0.3)
		art_placeholder.visible = true
		if art_texture:
			art_texture.visible = false

func _update_description() -> void:
	if description_label and card_data.has("description"):
		var desc = card_data.description
		# Truncate if too long
		if desc.length() > description_max_chars:
			desc = desc.substr(0, description_max_chars - 3) + "..."
		description_label.text = desc

## =============================================================================
## UTILITY
## =============================================================================

## Get the current element color
func get_element_color() -> Color:
	return element_color

## Enable/disable glow effect (can be extended with shader)
func set_glow(enabled: bool) -> void:
	# TODO: Add glow shader effect
	pass
