extends RefCounted
class_name ActiveModifier

## ActiveModifier - Runtime instance of a modifier on a hero
##
## Represents a trait or boon that has been applied to a hero.
## Tracks the modifier ID (enum), how it was acquired, and stack count.
## The actual effects come from ModifierDatabase.get_modifier(modifier_id)

## The modifier this instance represents (enum ID)
var modifier_id: int = -1

## How this modifier was acquired
var source: ModifierConfig.ModifierSource = ModifierConfig.ModifierSource.INNATE

## Number of stacks (for stackable modifiers)
var stacks: int = 1

## Serialize to dictionary for saving
func to_dict() -> Dictionary:
	return {
		"modifier_key": ModifierRegistry.get_key_from_id(modifier_id),  ## Save as StringName
		"source": source,
		"stacks": stacks
	}

## Create from dictionary (when loading from save)
static func from_dict(data: Dictionary) -> ActiveModifier:
	var active: ActiveModifier = ActiveModifier.new()

	# Handle both new (modifier_key) and legacy (modifier_id) formats
	var modifier_var: Variant = data.get("modifier_key", data.get("modifier_id", &""))
	if modifier_var is StringName or modifier_var is String:
		var key: StringName = StringName(modifier_var)
		if ModifierRegistry.is_valid_key(key):
			active.modifier_id = ModifierRegistry.get_id_from_key(key)
		else:
			push_warning("ActiveModifier.from_dict: Unknown modifier key '%s'" % key)
			active.modifier_id = -1
	elif modifier_var is int:
		# Already an enum ID (shouldn't happen in saves, but handle it)
		active.modifier_id = modifier_var
	else:
		active.modifier_id = -1

	var source_var: Variant = data.get("source", ModifierConfig.ModifierSource.INNATE)
	active.source = source_var if source_var is int else ModifierConfig.ModifierSource.INNATE

	active.stacks = data.get("stacks", 1)
	return active

## Get the config for this modifier
func get_config() -> ModifierConfig:
	return ModifierDatabase.get_modifier(modifier_id)

## Validation
func is_valid() -> bool:
	if modifier_id < 0:
		return false
	if not ModifierRegistry.is_valid_id(modifier_id):
		return false
	return ModifierDatabase.has_modifier(modifier_id)
