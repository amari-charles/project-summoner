class_name ElementCategoryIds

## Element Category ID Constants - Type-Safe Element Category References
##
## Use these constants instead of string literals when checking element categories.
##
## Usage:
##   if element.category == ElementCategoryIds.CORE:
##       print("This is a core element")
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# ELEMENT CATEGORIES
# ============================================================================

## Core elements (Fire, Water, Wind, Earth)
const CORE: StringName = &"core"

## Outer elements (Lightning, Shadow, Life, Death)
const OUTER: StringName = &"outer"

## Elevated elements (special/hybrid elements)
const ELEVATED: StringName = &"elevated"

## Neutral (no element)
const NEUTRAL: StringName = &"neutral"

## Occultist (special dark element)
const OCCULTIST: StringName = &"occultist"

# ============================================================================
# UTILITY
# ============================================================================

## All element categories
const ALL_CATEGORIES: Array[StringName] = [CORE, OUTER, ELEVATED, NEUTRAL, OCCULTIST]

## Default category used as fallback
const DEFAULT: StringName = NEUTRAL

## Check if a category string is valid
static func is_valid(category: String) -> bool:
	return StringName(category) in ALL_CATEGORIES

## Check if a category represents a main element type
static func is_elemental(category: StringName) -> bool:
	return category in [CORE, OUTER, ELEVATED]
