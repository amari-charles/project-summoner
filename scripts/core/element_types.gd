extends Node
# Note: No class_name needed - this will be registered as an autoload

## ElementTypes - Central registry for all elemental types in Project Summoner
##
## Provides type-safe constants and enums for elemental affinities.
## Elements are used in the modifier system, card categorization, and hero bonuses.
##
## Usage:
##   var affinity = ElementTypes.FIRE
##   if ElementTypes.is_valid(affinity):
##       print(ElementTypes.get_display_name(affinity))

## =============================================================================
## ELEMENT TYPE CONSTANTS
## =============================================================================

## Core Elements - Foundation of the world and main campaign pillars
const FIRE: String = "fire"
const WATER: String = "water"
const WIND: String = "wind"
const EARTH: String = "earth"

## Outer Elements - Expansion content and advanced mechanics
const LIGHTNING: String = "lightning"
const SHADOW: String = "shadow"
const POISON: String = "poison"
const LIFE: String = "life"
const DEATH: String = "death"

## Occultist - Antagonist element that inverts/corrupts other forces
const OCCULTIST: String = "occultist"

## Elevated Elements - Philosophical transformations of base elements
const HOLY: String = "holy"      # Fire elevated → Sacred
const ICE: String = "ice"        # Water elevated → Immutable
const METAL: String = "metal"    # Earth elevated → Forged
const SPIRIT: String = "spirit"  # Life elevated → Metaphysical

## =============================================================================
## ELEMENT METADATA
## =============================================================================

## All valid element type strings
const ALL_ELEMENTS: Array[String] = [
	# Core
	FIRE, WATER, WIND, EARTH,
	# Outer
	LIGHTNING, SHADOW, POISON, LIFE, DEATH,
	# Occultist
	OCCULTIST,
	# Elevated
	HOLY, ICE, METAL, SPIRIT
]

## Core elements only (main campaign)
const CORE_ELEMENTS: Array[String] = [FIRE, WATER, WIND, EARTH]

## Outer elements only (expansion content)
const OUTER_ELEMENTS: Array[String] = [LIGHTNING, SHADOW, POISON, LIFE, DEATH]

## Elevated elements only
const ELEVATED_ELEMENTS: Array[String] = [HOLY, ICE, METAL, SPIRIT]

## Display names for elements (user-facing)
const DISPLAY_NAMES: Dictionary = {
	FIRE: "Fire",
	WATER: "Water",
	WIND: "Wind",
	EARTH: "Earth",
	LIGHTNING: "Lightning",
	SHADOW: "Shadow",
	POISON: "Poison",
	LIFE: "Life",
	DEATH: "Death",
	OCCULTIST: "Occultist",
	HOLY: "Holy",
	ICE: "Ice",
	METAL: "Metal",
	SPIRIT: "Spirit"
}

## Element descriptions (for tooltips/UI)
const DESCRIPTIONS: Dictionary = {
	FIRE: "Embodies vitality, passion, and transformation",
	WATER: "Symbolizes adaptability, empathy, and memory",
	WIND: "Represents motion, freedom, and volatility",
	EARTH: "Stands for stability, structure, and endurance",
	LIGHTNING: "Pure energy, speed, and precision",
	SHADOW: "The unseen, deceptive force",
	POISON: "Corruption, persistence, and decay",
	LIFE: "Growth, restoration, and empathy",
	DEATH: "Endings, transition, and inevitability",
	OCCULTIST: "Corruption and forbidden knowledge",
	HOLY: "Sacred fire - divinity and purpose",
	ICE: "Frozen water - preservation and control",
	METAL: "Forged earth - civilization and artifice",
	SPIRIT: "Transcendent life - consciousness as form"
}

## Elevation mappings (base element → elevated form)
const ELEVATIONS: Dictionary = {
	FIRE: HOLY,
	WATER: ICE,
	EARTH: METAL,
	LIFE: SPIRIT
}

## =============================================================================
## VALIDATION & UTILITY
## =============================================================================

## Check if a string is a valid element type
func is_valid(element: String) -> bool:
	return element in ALL_ELEMENTS

## Check if element is a core element
func is_core(element: String) -> bool:
	return element in CORE_ELEMENTS

## Check if element is an outer element
func is_outer(element: String) -> bool:
	return element in OUTER_ELEMENTS

## Check if element is elevated
func is_elevated(element: String) -> bool:
	return element in ELEVATED_ELEMENTS

## Get display name for element
func get_display_name(element: String) -> String:
	return DISPLAY_NAMES.get(element, element.capitalize())

## Get description for element
func get_description(element: String) -> String:
	return DESCRIPTIONS.get(element, "Unknown element")

## Get elevated form of an element (if one exists)
func get_elevation(element: String) -> String:
	return ELEVATIONS.get(element, "")

## Check if element can be elevated
func can_elevate(element: String) -> bool:
	return ELEVATIONS.has(element)

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
const VARIANT_TO_ELEMENT: Dictionary = {
	VARIANT_SOLAR: FIRE,
	VARIANT_MIST: WATER,
	VARIANT_TEMPEST: WIND,
	VARIANT_CRYSTAL: EARTH
}

## Get parent element from variant name
func get_variant_element(variant_name: String) -> String:
	return VARIANT_TO_ELEMENT.get(variant_name, "")

## =============================================================================
## DEBUGGING
## =============================================================================

func _ready() -> void:
	print("ElementTypes: Initialized with %d element types" % ALL_ELEMENTS.size())

func print_summary() -> void:
	print("\n=== ELEMENT TYPES SUMMARY ===")
	print("Core Elements (%d): %s" % [CORE_ELEMENTS.size(), ", ".join(CORE_ELEMENTS)])
	print("Outer Elements (%d): %s" % [OUTER_ELEMENTS.size(), ", ".join(OUTER_ELEMENTS)])
	print("Occultist: %s" % OCCULTIST)
	print("Elevated Elements (%d): %s" % [ELEVATED_ELEMENTS.size(), ", ".join(ELEVATED_ELEMENTS)])
	print("Total: %d element types" % ALL_ELEMENTS.size())
	print("============================\n")
