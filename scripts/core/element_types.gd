extends Node
# Note: No class_name needed - this will be registered as an autoload

## ElementTypes - Central registry for all elemental types in Project Summoner
##
## Provides type-safe Element objects for elemental affinities.
## Elements are used in the modifier system, card categorization, and hero bonuses.
##
## Usage:
##   var affinity = ElementTypes.FIRE
##   if affinity.matches(ElementTypes.FIRE):
##       print(affinity.display_name)
##
## Elevated elements inherit bonuses from their origin:
##   ElementTypes.HOLY.origin_element == ElementTypes.FIRE
##   Fire bonuses automatically apply to Holy cards

## =============================================================================
## ELEMENT CLASS
## =============================================================================

class Element:
	var id: String
	var display_name: String
	var description: String
	var category: String  # "core", "outer", "elevated", "occultist"
	var origin_element: Element = null  # For elevated elements only

	func _init(p_id: String, p_display_name: String, p_description: String, p_category: String, p_origin: Element = null) -> void:
		id = p_id
		display_name = p_display_name
		description = p_description
		category = p_category
		origin_element = p_origin

	## Convert to string (returns id)
	func _to_string() -> String:
		return id

	## Check if this element matches another element or string
	func matches(other: Variant) -> bool:
		# Compare with another Element
		if other is Element:
			return id == other.id
		# Compare with string
		elif other is String:
			return id == other
		return false

	## Check if this element should match a given affinity (including origin check)
	func matches_affinity(affinity: Variant) -> bool:
		# Direct match
		if matches(affinity):
			return true
		# Origin match (for elevated elements)
		if origin_element != null:
			if affinity is Element:
				return origin_element.id == affinity.id
			elif affinity is String:
				return origin_element.id == affinity
		return false

## =============================================================================
## ELEMENT CONSTANTS (Objects)
## =============================================================================

## Neutral - No elemental affinity
var NEUTRAL: Element = null

## Core Elements - Foundation of the world and main campaign pillars
var FIRE: Element = null
var WATER: Element = null
var WIND: Element = null
var EARTH: Element = null

## Outer Elements - Expansion content and advanced mechanics
var LIGHTNING: Element = null
var SHADOW: Element = null
var POISON: Element = null
var LIFE: Element = null
var DEATH: Element = null

## Occultist - Antagonist element that inverts/corrupts other forces
var OCCULTIST: Element = null

## Elevated Elements - Philosophical transformations of base elements
var HOLY: Element = null      # Fire elevated → Sacred
var ICE: Element = null       # Water elevated → Immutable
var METAL: Element = null     # Earth elevated → Forged
var SPIRIT: Element = null    # Life elevated → Metaphysical

## Lookup cache for O(1) element retrieval by ID
var _element_lookup: Dictionary = {}

## =============================================================================
## INITIALIZATION
## =============================================================================

func _ready() -> void:
	# Create neutral element (no affinity)
	NEUTRAL = Element.new(
		"neutral",
		"Neutral",
		"No elemental affinity",
		"neutral"
	)

	# Create base elements first (no origin)
	FIRE = Element.new(
		"fire",
		"Fire",
		"Embodies vitality, passion, and transformation",
		"core"
	)

	WATER = Element.new(
		"water",
		"Water",
		"Symbolizes adaptability, empathy, and memory",
		"core"
	)

	WIND = Element.new(
		"wind",
		"Wind",
		"Represents motion, freedom, and volatility",
		"core"
	)

	EARTH = Element.new(
		"earth",
		"Earth",
		"Stands for stability, structure, and endurance",
		"core"
	)

	LIGHTNING = Element.new(
		"lightning",
		"Lightning",
		"Pure energy, speed, and precision",
		"outer"
	)

	SHADOW = Element.new(
		"shadow",
		"Shadow",
		"The unseen, deceptive force",
		"outer"
	)

	POISON = Element.new(
		"poison",
		"Poison",
		"Corruption, persistence, and decay",
		"outer"
	)

	LIFE = Element.new(
		"life",
		"Life",
		"Growth, restoration, and empathy",
		"outer"
	)

	DEATH = Element.new(
		"death",
		"Death",
		"Endings, transition, and inevitability",
		"outer"
	)

	OCCULTIST = Element.new(
		"occultist",
		"Occultist",
		"Corruption and forbidden knowledge",
		"occultist"
	)

	# Create elevated elements WITH origin references
	HOLY = Element.new(
		"holy",
		"Holy",
		"Sacred fire - divinity and purpose",
		"elevated",
		FIRE  # Origin element
	)

	ICE = Element.new(
		"ice",
		"Ice",
		"Frozen water - preservation and control",
		"elevated",
		WATER  # Origin element
	)

	METAL = Element.new(
		"metal",
		"Metal",
		"Forged earth - civilization and artifice",
		"elevated",
		EARTH  # Origin element
	)

	SPIRIT = Element.new(
		"spirit",
		"Spirit",
		"Transcendent life - consciousness as form",
		"elevated",
		LIFE  # Origin element
	)

	# Build lookup cache for O(1) element retrieval
	_element_lookup.clear()
	for element: Element in get_all_elements():
		_element_lookup[element.id] = element

	# Initialize variant mappings
	VARIANT_TO_ELEMENT = {
		VARIANT_SOLAR: FIRE,
		VARIANT_MIST: WATER,
		VARIANT_TEMPEST: WIND,
		VARIANT_CRYSTAL: EARTH
	}

	print("ElementTypes: Initialized with %d element types" % get_all_elements().size())

## =============================================================================
## ELEMENT METADATA
## =============================================================================

## Get all element objects
func get_all_elements() -> Array[Element]:
	return [
		# Neutral
		NEUTRAL,
		# Core
		FIRE, WATER, WIND, EARTH,
		# Outer
		LIGHTNING, SHADOW, POISON, LIFE, DEATH,
		# Occultist
		OCCULTIST,
		# Elevated
		HOLY, ICE, METAL, SPIRIT
	]

## Get core elements only
func get_core_elements() -> Array[Element]:
	return [FIRE, WATER, WIND, EARTH]

## Get outer elements only
func get_outer_elements() -> Array[Element]:
	return [LIGHTNING, SHADOW, POISON, LIFE, DEATH]

## Get elevated elements only
func get_elevated_elements() -> Array[Element]:
	return [HOLY, ICE, METAL, SPIRIT]

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get element by string ID
## Returns null if element not found - caller should check for null
func from_string(element_id: String) -> Element:
	var element: Element = _element_lookup.get(element_id, null)
	if element == null:
		push_error("ElementTypes: Unknown element ID '%s' - this will cause errors downstream!" % element_id)
	return element

## Check if a string or Element is valid
func is_valid(element: Variant) -> bool:
	if element is Element:
		return element in get_all_elements()
	elif element is String:
		return from_string(element as String) != null
	return false

## Check if element is a core element
func is_core(element: Variant) -> bool:
	if element is Element:
		return element.category == "core"
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem != null and elem.category == "core"
	return false

## Check if element is an outer element
func is_outer(element: Variant) -> bool:
	if element is Element:
		return element.category == "outer"
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem != null and elem.category == "outer"
	return false

## Check if element is elevated
func is_elevated(element: Variant) -> bool:
	if element is Element:
		return element.category == "elevated"
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem != null and elem.category == "elevated"
	return false

## Get display name for element
func get_display_name(element: Variant) -> String:
	if element is Element:
		return element.display_name
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem.display_name if elem != null else (element as String).capitalize()
	return str(element)

## Get description for element
func get_description(element: Variant) -> String:
	if element is Element:
		return element.description
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem.description if elem != null else "Unknown element"
	return "Unknown element"

## Get origin element (for elevated elements)
func get_origin(element: Variant) -> Element:
	if element is Element:
		return element.origin_element
	elif element is String:
		var elem: Element = from_string(element as String)
		return elem.origin_element if elem != null else null
	return null

## Check if element can be elevated (is a base that has elevated form)
func can_elevate(element: Variant) -> bool:
	var elem: Element = element if element is Element else (from_string(element as String) if element is String else null)
	if elem == null:
		return false
	# Check if any elevated element has this as origin
	for elevated: Element in get_elevated_elements():
		if elevated.origin_element == elem:
			return true
	return false

## Get elevated form of an element (if one exists)
func get_elevation(element: Variant) -> Element:
	var elem: Element = element if element is Element else (from_string(element as String) if element is String else null)
	if elem == null:
		return null
	# Find elevated form
	for elevated: Element in get_elevated_elements():
		if elevated.origin_element == elem:
			return elevated
	return null

## =============================================================================
## CARD FLAVOR METADATA (Variants & Hybrids)
## =============================================================================

## Variant names - these are NOT separate element types
## They are thematic/reward versions that use parent element's affinity
const VARIANT_SOLAR: String = "Solar"      # Fire variant
const VARIANT_MIST: String = "Mist"        # Water variant
const VARIANT_TEMPEST: String = "Tempest"  # Wind variant
const VARIANT_CRYSTAL: String = "Crystal"  # Earth variant

## Hybrid names - these are NOT separate element types
## They are thematic fusions that pick one parent's affinity
const HYBRID_MAGMA: String = "Magma"       # Fire + Earth

## Mapping of variants to their parent element
var VARIANT_TO_ELEMENT: Dictionary = {}

## Get parent element from variant name
func get_variant_element(variant_name: String) -> Element:
	return VARIANT_TO_ELEMENT.get(variant_name, null)

## =============================================================================
## DEBUGGING
## =============================================================================

func print_summary() -> void:
	print("\n=== ELEMENT TYPES SUMMARY ===")
	print("Core Elements (%d):" % get_core_elements().size())
	for elem: Element in get_core_elements():
		print("  - %s" % elem.display_name)
	print("Outer Elements (%d):" % get_outer_elements().size())
	for elem: Element in get_outer_elements():
		print("  - %s" % elem.display_name)
	print("Occultist: %s" % OCCULTIST.display_name)
	print("Elevated Elements (%d):" % get_elevated_elements().size())
	for elem: Element in get_elevated_elements():
		print("  - %s (from %s)" % [elem.display_name, elem.origin_element.display_name])
	print("Total: %d element types" % get_all_elements().size())
	print("============================\n")
