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
			return Color("#eeeeee")  # White with gray tint
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

## Get gradient color pair for element background
## Returns [dark_color, light_color] for radial gradient (center to edge)
static func get_element_gradient_colors(element_id: String) -> Array[Color]:
	match element_id.to_lower():
		# Neutral
		"neutral":
			return [Color("#3a3a3a"), Color("#6a6a6a")]  # Dark gray → Mid gray

		# Core elements
		"fire":
			return [GameColorPalette.FIRE_DARK, GameColorPalette.FIRE_PRIMARY]  # Deep ember → Bright orange
		"water":
			return [GameColorPalette.WATER_DARK, GameColorPalette.WATER_PRIMARY]  # Deep ocean → Bright blue
		"wind":
			return [Color("#e0e0e0"), Color("#ffffff")]  # Light gray → White
		"earth":
			return [GameColorPalette.EARTH_PRIMARY, GameColorPalette.EARTH_SECONDARY]  # Dark brown → Tan

		# Outer elements
		"lightning":
			return [GameColorPalette.STORM_DARK, GameColorPalette.STORM_PRIMARY]  # Deep violet → Bright purple
		"shadow":
			return [Color("#1a0520"), Color("#4a0e4e")]  # Very dark purple → Deep purple-black
		"poison":
			return [Color("#2d4a2d"), Color("#8fbc8f")]  # Dark green → Toxic green
		"life":
			return [GameColorPalette.NATURE_DARK, GameColorPalette.NATURE_PRIMARY]  # Deep forest → Bright green
		"death":
			return [Color("#1a1a1a"), Color("#2f2f2f")]  # Very dark → Dark gray

		# Occultist
		"occultist":
			return [Color("#2d0547"), Color("#6a0dad")]  # Very dark purple → Deep occult purple

		# Elevated elements
		"holy":
			return [Color("#c49a00"), Color("#ffd700")]  # Dark gold → Divine gold
		"ice":
			return [Color("#6fa8b0"), Color("#b0e0e6")]  # Cool blue → Pale ice blue
		"metal":
			return [Color("#808080"), Color("#c0c0c0")]  # Dark silver → Metallic silver
		"spirit":
			return [Color("#a0a0d0"), Color("#e6e6fa")]  # Muted lavender → Ethereal lavender

		_:
			return [Color("#3a3a3a"), Color("#6a6a6a")]  # Default to neutral

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
				# Validate Element object - fail loudly if invalid
				if typeof(affinity) == TYPE_OBJECT and "id" in affinity:
					var affinity_id = affinity.id
					return get_element_border_color(affinity_id)
				else:
					push_error("CardVisualHelper: Invalid elemental_affinity for card '%s' - expected Element object, got type %s with value: %s" % [catalog_dict.get("card_name", "unknown"), typeof(affinity), affinity])
					assert(false, "Corrupted element data - fix the card catalog!")
					return Color.MAGENTA  # Unreachable in debug, but needed for release builds

	# Fallback: use card type-based colors (should rarely happen)
	var card_type = catalog_dict.get("card_type", 0)
	if card_type == 0:
		return GameColorPalette.PLAYER_ZONE_ACCENT  # Summon
	elif card_type == 1:
		return GameColorPalette.STORM_PRIMARY  # Spell
	else:
		return GameColorPalette.NEUTRAL_MID

## =============================================================================
## CARD TYPE ICON MAPPING
## =============================================================================

## Get icon path for a card based on its type and unit_type
## Returns the appropriate icon for display in card UI
static func get_card_type_icon_path(card_data) -> String:
	var catalog_dict: Dictionary = {}

	# Handle Card resource vs Dictionary
	if card_data is Card:
		catalog_dict = CardCatalog.get_card(card_data.catalog_id)
	elif card_data is Dictionary:
		catalog_dict = card_data
	else:
		push_warning("CardVisualHelper: Invalid card_data type for icon lookup")
		return ""

	# Get card type and unit type
	var card_type = catalog_dict.get("card_type", 0)
	var unit_type = catalog_dict.get("unit_type", "")

	# Map to icon path
	if card_type == 1:  # SPELL
		return "res://assets/icons/card_types/wizard_hat.png"
	elif card_type == 0:  # SUMMON
		match unit_type:
			"melee":
				return "res://assets/icons/card_types/sword.png"
			"ranged":
				return "res://assets/icons/card_types/bow.png"
			"structure":
				return "res://assets/icons/card_types/tower.png"
			_:
				push_warning("CardVisualHelper: Unknown unit_type '%s', defaulting to sword" % unit_type)
				return "res://assets/icons/card_types/sword.png"

	return ""

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
static func format_card_name(card_name: String, max_length: int = 20) -> String:
	if card_name.length() <= max_length:
		return card_name

	# Try to wrap at word boundaries
	var words = card_name.split(" ")
	if words.size() > 1:
		return "\n".join(words)

	# If single long word, just truncate
	return card_name.substr(0, max_length - 3) + "..."

## Format description text for card (truncate if too long)
static func format_card_description(desc: String, max_chars: int = 100) -> String:
	if desc.length() <= max_chars:
		return desc
	return desc.substr(0, max_chars - 3) + "..."
