class_name ElementNameIDs

## Element Name ID Constants - Type-Safe Element String References
##
## Use these constants instead of string literals when matching element IDs.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   match element.id:
##       ElementNameIDs.FIRE: return Color(1.0, 0.3, 0.2)
##       ElementNameIDs.WATER: return Color(0.2, 0.5, 1.0)
##
## Note: These correspond to Element.id values in the ElementTypes system.
## For Element objects, use ElementTypes.FIRE, ElementTypes.WATER, etc.
## For serialization, use ElementRegistry for enum ↔ StringName conversions.
##
## StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# NEUTRAL
# ============================================================================

const NEUTRAL: StringName = &"neutral"

# ============================================================================
# CORE ELEMENTS
# ============================================================================

const FIRE: StringName = &"fire"
const WATER: StringName = &"water"
const WIND: StringName = &"wind"
const EARTH: StringName = &"earth"

# ============================================================================
# OUTER ELEMENTS
# ============================================================================

const LIGHTNING: StringName = &"lightning"
const SHADOW: StringName = &"shadow"
const POISON: StringName = &"poison"
const LIFE: StringName = &"life"
const DEATH: StringName = &"death"

# ============================================================================
# OCCULTIST
# ============================================================================

const OCCULTIST: StringName = &"occultist"

# ============================================================================
# ELEVATED ELEMENTS
# ============================================================================

const HOLY: StringName = &"holy"
const ICE: StringName = &"ice"
const METAL: StringName = &"metal"
const SPIRIT: StringName = &"spirit"

# ============================================================================
# UTILITY
# ============================================================================

## All element name IDs
const ALL_ELEMENTS: Array[StringName] = [
	NEUTRAL,
	FIRE, WATER, WIND, EARTH,
	LIGHTNING, SHADOW, POISON, LIFE, DEATH,
	OCCULTIST,
	HOLY, ICE, METAL, SPIRIT
]

## Core elements only
const CORE_ELEMENTS: Array[StringName] = [FIRE, WATER, WIND, EARTH]

## Check if an element name is valid
static func is_valid(element_name: String) -> bool:
	return StringName(element_name) in ALL_ELEMENTS
