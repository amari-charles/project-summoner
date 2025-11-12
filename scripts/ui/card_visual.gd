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

	# Apply element-based gradient background
	_apply_gradient_background()

	# Apply shininess overlay
	_apply_shine_effect()

	# Apply cost label font size
	var cost_lbl: Label = get_node_or_null("CostLabel")
	if cost_lbl:
		cost_lbl.add_theme_font_size_override("font_size", cost_font_size)

	# Apply name label font size
	var name_lbl: Label = get_node_or_null("NameLabel")
	if name_lbl:
		name_lbl.add_theme_font_size_override("font_size", name_font_size)

	# Apply description label font size
	var desc_lbl: Label = get_node_or_null("DescriptionLabel")
	if desc_lbl:
		desc_lbl.add_theme_font_size_override("font_size", description_font_size)

	# Apply element badge styling
	var badge: Panel = get_node_or_null("ElementBadge")
	if badge:
		var badge_style: StyleBoxFlat = StyleBoxFlat.new()
		badge_style.bg_color = element_color
		badge_style.set_corner_radius_all(element_badge_radius)
		badge_style.anti_aliasing = true
		badge_style.anti_aliasing_size = 1
		badge.add_theme_stylebox_override("panel", badge_style)

	# Show/hide description
	if desc_lbl:
		desc_lbl.visible = show_description

func _apply_border_color() -> void:
	var border: Panel = get_node_or_null("BorderPanel")
	if border:
		var border_style: StyleBoxFlat = StyleBoxFlat.new()
		border_style.bg_color = element_color
		border_style.set_corner_radius_all(corner_radius)
		border_style.anti_aliasing = true
		border_style.anti_aliasing_size = 1
		border.add_theme_stylebox_override("panel", border_style)

func _apply_gradient_background() -> void:
	var bg_panel: Panel = get_node_or_null("BackgroundPanel")
	if not bg_panel:
		return

	# Get element ID from card data
	var element_id: String = _get_element_id_from_card_data()

	# Get gradient colors for this element
	var gradient_colors: Array = CardVisualHelper.get_element_gradient_colors(element_id)

	# Create radial gradient texture
	var gradient: Gradient = Gradient.new()
	gradient.set_color(0, gradient_colors[0])  # Center color (dark)
	gradient.set_color(1, gradient_colors[1])  # Edge color (light)

	var gradient_texture: GradientTexture2D = GradientTexture2D.new()
	gradient_texture.gradient = gradient
	gradient_texture.fill = GradientTexture2D.FILL_RADIAL
	gradient_texture.fill_from = Vector2(0.5, 0.5)  # Center point
	gradient_texture.fill_to = Vector2(1.0, 0.5)    # Radius
	gradient_texture.width = 256
	gradient_texture.height = 256

	# Use StyleBoxFlat with gradient-like appearance
	# Since StyleBoxFlat doesn't support gradients, we'll use the darker color
	# and rely on the overall design for depth
	var bg_style: StyleBoxFlat = StyleBoxFlat.new()
	bg_style.bg_color = gradient_colors[0]  # Use darker center color
	bg_style.set_corner_radius_all(corner_radius - border_width)
	bg_style.anti_aliasing = true
	bg_style.anti_aliasing_size = 1

	bg_panel.add_theme_stylebox_override("panel", bg_style)

func _apply_shine_effect() -> void:
	var border: Panel = get_node_or_null("BorderPanel")
	if not border:
		return

	# Add subtle highlight to top-left of border for glossy effect
	var border_style: StyleBox = border.get_theme_stylebox("panel")
	if border_style is StyleBoxFlat:
		var flat_style: StyleBoxFlat = border_style as StyleBoxFlat
		# Add a subtle border on the top-left for shine effect
		flat_style.border_color = element_color.lightened(0.4)
		flat_style.set_border_width(SIDE_TOP, 1)
		flat_style.set_border_width(SIDE_LEFT, 1)

func _get_element_id_from_card_data() -> String:
	# Extract element ID from card data
	var catalog_dict: Dictionary = card_data

	# Check if this is a Card resource, need to fetch from catalog
	if card_data.has("catalog_id") and not card_data.has("categories"):
		catalog_dict = CardCatalog.get_card(card_data.catalog_id)

	# Extract element ID
	if catalog_dict.has("categories"):
		var categories: Variant = catalog_dict.categories
		if categories is Dictionary and categories.has("elemental_affinity"):
			var affinity: Variant = categories.elemental_affinity
			if affinity and typeof(affinity) == TYPE_OBJECT and "id" in affinity:
				return affinity.id

	return "neutral"  # Fallback

func _update_cost() -> void:
	var label: Label = get_node_or_null("CostLabel")
	if label and card_data.has("mana_cost"):
		label.text = str(card_data.mana_cost)

func _update_name() -> void:
	var label: Label = get_node_or_null("NameLabel")
	if label and card_data.has("card_name"):
		label.text = card_data.card_name

func _update_type_icon() -> void:
	var icon: TextureRect = get_node_or_null("TypeIcon")
	if icon:
		var icon_path: String = CardVisualHelper.get_card_type_icon_path(card_data)
		if not icon_path.is_empty():
			var texture: Texture2D = load(icon_path)
			if texture:
				icon.texture = texture
				icon.visible = true
			else:
				push_warning("CardVisual: Failed to load icon at '%s'" % icon_path)
				icon.visible = false
		else:
			icon.visible = false

func _update_art() -> void:
	var container: Control = get_node_or_null("ArtContainer")
	if not container:
		return

	var art_tex: TextureRect = container.get_node_or_null("ArtTexture")
	var art_ph: ColorRect = container.get_node_or_null("ArtPlaceholder")

	# Try to load card art if path is specified
	var art_loaded: bool = false
	if card_data.has("card_icon_path") and not card_data.card_icon_path.is_empty():
		var texture: Texture2D = load(card_data.card_icon_path)
		if texture and art_tex:
			art_tex.texture = texture
			art_tex.visible = true
			if art_ph:
				art_ph.visible = false
			art_loaded = true

	# Fall back to colored placeholder
	if not art_loaded and art_ph:
		art_ph.color = element_color.darkened(0.3)
		art_ph.visible = true
		if art_tex:
			art_tex.visible = false

func _update_description() -> void:
	var label: Label = get_node_or_null("DescriptionLabel")
	if label and card_data.has("description"):
		var desc: String = card_data.description
		# Truncate if too long
		if desc.length() > description_max_chars:
			desc = desc.substr(0, description_max_chars - 3) + "..."
		label.text = desc

## =============================================================================
## UTILITY
## =============================================================================

## Get the current element color
func get_element_color() -> Color:
	return element_color

## Enable/disable glow effect (can be extended with shader)
func set_glow(_enabled: bool) -> void:
	# TODO: Add glow shader effect
	pass
