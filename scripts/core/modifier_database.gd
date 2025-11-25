extends Node

## ModifierDatabase - Singleton that loads and caches all modifier configurations
##
## Central registry for all traits and boons in the game.
## Loads from data files (JSON converted to ModifierConfig resources).
## Registered as autoload "ModifierDatabase"

## Cache of all loaded modifiers
var _modifiers: Dictionary = {}  ## Key: modifier_id (int enum), Value: ModifierConfig

func _ready() -> void:
	print("ModifierDatabase: Initializing...")
	_load_modifiers()
	print("ModifierDatabase: Loaded %d modifiers" % _modifiers.size())

## Load all modifier configurations
func _load_modifiers() -> void:
	# For MVP, we'll hardcode a few modifiers
	# Later, this can load from JSON files in res://data/modifiers/

	# Fortune Favors the Bold (Story Trait for Random selection)
	var fortune_favors: ModifierConfig = ModifierConfig.new()
	fortune_favors.id = ModifierRegistry.ModifierId.FORTUNE_FAVORS_BOLD
	fortune_favors.name = "Fortune Favors the Bold"
	fortune_favors.description = "Choosing random grants a small bonus to starting resources"
	fortune_favors.category = ModifierConfig.ModifierCategory.TRAIT
	fortune_favors.allowed_sources = [ModifierConfig.ModifierSource.SYSTEM]
	fortune_favors.min_hero_level = 0
	fortune_favors.max_stacks = 1

	# Effect: +50 HP
	var hp_effect: ModifierEffect = ModifierEffect.new()
	hp_effect.type = ModifierEffect.EffectType.HERO_STAT
	hp_effect.stat = "health"
	hp_effect.mode = ModifierEffect.StatMode.ADD
	hp_effect.value = 50.0
	fortune_favors.effects.append(hp_effect)

	_modifiers[fortune_favors.id] = fortune_favors

	# More modifiers can be added here or loaded from files

## Get a modifier configuration by ID (enum)
func get_modifier(modifier_id: int) -> ModifierConfig:
	if modifier_id < 0:
		push_warning("ModifierDatabase.get_modifier: Invalid modifier_id")
		return null

	if not _modifiers.has(modifier_id):
		push_error("ModifierDatabase.get_modifier: Modifier not found: %s" % modifier_id)
		return null

	return _modifiers[modifier_id]

## Check if a modifier exists
func has_modifier(modifier_id: int) -> bool:
	return _modifiers.has(modifier_id)

## Get all modifier IDs
func get_all_modifier_ids() -> Array[int]:
	var ids: Array[int] = []
	for key: int in _modifiers.keys():
		ids.append(key)
	return ids

## Get modifiers by category
func get_modifiers_by_category(category: ModifierConfig.ModifierCategory) -> Array[ModifierConfig]:
	var result: Array[ModifierConfig] = []
	for mod: ModifierConfig in _modifiers.values():
		if mod.category == category:
			result.append(mod)
	return result

## Print database summary for debugging
func print_summary() -> void:
	print("=== Modifier Database Summary ===")
	print("Total Modifiers: %d" % _modifiers.size())

	var trait_count: int = 0
	var boon_count: int = 0

	for mod: ModifierConfig in _modifiers.values():
		if mod.category == ModifierConfig.ModifierCategory.TRAIT:
			trait_count += 1
		else:
			boon_count += 1

	print("  Traits: %d" % trait_count)
	print("  Boons: %d" % boon_count)
	print("=================================")
