extends Node
class_name CardVisualHelper

## CardVisualHelper - Utility functions for card visual presentation
##
## Provides element-to-color mappings and visual styling helpers for cards

## =============================================================================
## ELEMENT COLOR MAPPING
## =============================================================================

## Get border/accent color for a card based on its elemental affinity
## Returns the primary color for the element, or a neutral color if no affinity
static func get_element_border_color(element_id: String) -> Color:
	if element_id.is_empty():
		return GameColorPalette.NEUTRAL_MID

	match element_id.to_lower():
		# Neutral
		"neutral":
			return GameColorPalette.NEUTRAL_MID

		# Core elements
		"fire":
			return GameColorPalette.FIRE_PRIMARY
		"water":
			return GameColorPalette.WATER_PRIMARY
		"wind":
			return Color("#87ceeb")  # Sky blue
		"earth":
			return GameColorPalette.EARTH_PRIMARY

		# Outer elements
		"lightning":
			return GameColorPalette.STORM_PRIMARY
		"shadow":
			return Color("#4a0e4e")  # Deep purple-black
		"poison":
			return Color("#8fbc8f")  # Toxic green
		"life":
			return GameColorPalette.NATURE_PRIMARY
		"death":
			return Color("#2f2f2f")  # Dark gray

		# Occultist
		"occultist":
			return Color("#6a0dad")  # Deep occult purple

		# Elevated elements
		"holy":
			return Color("#ffd700")  # Divine gold
		"ice":
			return Color("#b0e0e6")  # Pale ice blue
		"metal":
			return Color("#c0c0c0")  # Metallic silver
		"spirit":
			return Color("#e6e6fa")  # Ethereal lavender

		_:
			push_warning("CardVisualHelper: Unknown element '%s', using neutral color" % element_id)
			return GameColorPalette.NEUTRAL_MID

## Get a secondary/lighter color for element (for glows, highlights)
static func get_element_glow_color(element_id: String) -> Color:
	var base_color = get_element_border_color(element_id)
	# Lighten the color and increase saturation slightly
	return base_color.lightened(0.3)

## Get element color from a card's elemental affinity
## Handles both Card resources and Dictionary catalog data
static func get_card_element_color(card_data) -> Color:
	var catalog_dict: Dictionary = {}

	# Handle Card resource vs Dictionary
	if card_data is Card:
		# Get catalog data from CardCatalog
		catalog_dict = CardCatalog.get_card(card_data.catalog_id)
	elif card_data is Dictionary:
		catalog_dict = card_data
	else:
		push_warning("CardVisualHelper: Invalid card_data type")
		return GameColorPalette.NEUTRAL_MID

	# Check if card has elemental affinity in categories
	if catalog_dict.has("categories"):
		var categories = catalog_dict.categories
		if categories is Dictionary and categories.has("elemental_affinity"):
			var affinity = categories.elemental_affinity
			if affinity:
				# Convert Element object to string if needed
				var affinity_id = affinity.id if affinity is ElementTypes.Element else str(affinity)
				return get_element_border_color(affinity_id)

	# Fallback: use card type-based colors
	var card_type = catalog_dict.get("card_type", 0)
	if card_type == 0:
		return GameColorPalette.PLAYER_ZONE_ACCENT  # Summon
	elif card_type == 1:
		return GameColorPalette.STORM_PRIMARY  # Spell
	else:
		return GameColorPalette.NEUTRAL_MID

## =============================================================================
## CARD LAYOUT HELPERS
## =============================================================================

## Calculate layout dimensions for a card of given size
static func get_card_layout(card_size: Vector2, show_description: bool) -> Dictionary:
	var layout = {}

	# Border width (scales with card size)
	layout.border_width = max(2, int(card_size.x * 0.025))

	# Cost circle (top-left corner)
	layout.cost_circle_radius = card_size.x * 0.15
	layout.cost_circle_pos = Vector2(
		layout.cost_circle_radius + layout.border_width + 4,
		layout.cost_circle_radius + layout.border_width + 4
	)

	# Card name (top-middle)
	layout.name_height = card_size.y * 0.12
	layout.name_rect = Rect2(
		layout.cost_circle_pos.x + layout.cost_circle_radius + 4,
		layout.border_width + 4,
		card_size.x - (layout.cost_circle_pos.x + layout.cost_circle_radius + 8),
		layout.name_height
	)

	# Description (bottom area, optional)
	if show_description:
		layout.desc_height = card_size.y * 0.25
		layout.desc_rect = Rect2(
			layout.border_width + 4,
			card_size.y - layout.desc_height - layout.border_width - 4,
			card_size.x - (layout.border_width * 2) - 8,
			layout.desc_height
		)
	else:
		layout.desc_height = 0
		layout.desc_rect = Rect2()

	# Card art (center, fills remaining space)
	var art_top = layout.cost_circle_pos.y + layout.cost_circle_radius + 4
	var art_bottom = card_size.y - layout.desc_height - layout.border_width - 4
	layout.art_rect = Rect2(
		layout.border_width + 4,
		art_top,
		card_size.x - (layout.border_width * 2) - 8,
		art_bottom - art_top
	)

	return layout

## =============================================================================
## TEXT FORMATTING
## =============================================================================

## Format card name for display (handle wrapping for long names)
static func format_card_name(name: String, max_length: int = 20) -> String:
	if name.length() <= max_length:
		return name

	# Try to wrap at word boundaries
	var words = name.split(" ")
	if words.size() > 1:
		return "\n".join(words)

	# If single long word, just truncate
	return name.substr(0, max_length - 3) + "..."

## Format description text for card (truncate if too long)
static func format_card_description(desc: String, max_chars: int = 100) -> String:
	if desc.length() <= max_chars:
		return desc
	return desc.substr(0, max_chars - 3) + "..."
