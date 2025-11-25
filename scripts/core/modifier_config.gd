extends Resource
class_name ModifierConfig

## ModifierConfig - Configuration for a trait or boon modifier
##
## Defines what a modifier does, how it's acquired, and stacking rules.
## All balance (numbers, effects) lives here for data-driven design.

enum ModifierCategory {
	TRAIT,           ## Hero trait (Level Trait or Story Trait)
	BOON             ## Boon (from events/achievements)
}

enum ModifierSource {
	INNATE,          ## Baked into hero (starting traits)
	LEVEL_UP_CHOICE, ## Chosen during level-up
	EARNED,          ## Quests / achievements / events
	SYSTEM           ## Granted by systems (tutorial, debug, special events)
}

## Identity
@export_enum("FORTUNE_FAVORS_BOLD")
var id: int = ModifierRegistry.ModifierId.FORTUNE_FAVORS_BOLD
@export var name: String = ""
@export var description: String = ""

## Get the modifier key for serialization
func get_modifier_key() -> StringName:
	return ModifierRegistry.get_key_from_id(id)

## Classification
@export var category: ModifierCategory = ModifierCategory.TRAIT
@export var allowed_sources: Array[ModifierSource] = []

## Requirements
@export var min_hero_level: int = 0

## Stacking
@export var max_stacks: int = 0        ## 0 = unlimited stacks

## Effects
@export var effects: Array[ModifierEffect] = []

## Validation
func is_valid() -> bool:
	if id < 0:
		push_error("ModifierConfig: id is invalid")
		return false
	if not ModifierRegistry.is_valid_id(id):
		push_error("ModifierConfig: unknown id %s" % id)
		return false
	if name.is_empty():
		push_error("ModifierConfig: name is empty for %s" % id)
		return false
	if effects.is_empty():
		push_warning("ModifierConfig: %s has no effects" % id)
	return true
