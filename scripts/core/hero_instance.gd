extends RefCounted
class_name HeroInstance

## HeroInstance - Runtime instance of a hero during a run
##
## Represents an active hero with level progression and modifiers.
## Computes final stats by applying modifiers to base config stats.
## Serializes to JSON (saves only IDs + state, reconstructs from config).

## Reference to the hero's configuration (template)
var config: HeroConfig = null

## Progression
var level: int = 1
var xp: int = 0

## Active Modifiers (traits and boons)
var active_modifiers: Array[ActiveModifier] = []

## Cached computed stats (updated when modifiers change)
var _cached_stats: Dictionary = {}
var _stats_dirty: bool = true

## Initialize from a HeroConfig (called when starting a new run)
func init_from_config(hero_config: HeroConfig) -> void:
	config = hero_config
	level = 1
	xp = 0
	active_modifiers.clear()

	# Apply innate modifiers
	for modifier_id: int in config.innate_modifier_ids:
		var active_mod: ActiveModifier = ActiveModifier.new()
		active_mod.modifier_id = modifier_id
		active_mod.source = ModifierConfig.ModifierSource.INNATE
		active_mod.stacks = 1
		active_modifiers.append(active_mod)

	_mark_stats_dirty()

## Add a new modifier (trait or boon)
func add_modifier(modifier_id: int, source: ModifierConfig.ModifierSource, stacks: int = 1) -> bool:
	if not ModifierDatabase.has_modifier(modifier_id):
		push_error("HeroInstance.add_modifier: Unknown modifier: %s" % modifier_id)
		return false

	var modifier_config: ModifierConfig = ModifierDatabase.get_modifier(modifier_id)

	# Check if stackable or already exists
	var existing: ActiveModifier = _find_modifier(modifier_id)

	if existing:
		if modifier_config.max_stacks == 1:
			push_warning("HeroInstance.add_modifier: Modifier %s is not stackable" % modifier_id)
			return false

		var new_stacks: int = existing.stacks + stacks
		if modifier_config.max_stacks > 0 and new_stacks > modifier_config.max_stacks:
			push_warning("HeroInstance.add_modifier: Modifier %s would exceed max stacks (%d)" % [modifier_id, modifier_config.max_stacks])
			return false

		existing.stacks = new_stacks
	else:
		var active_mod: ActiveModifier = ActiveModifier.new()
		active_mod.modifier_id = modifier_id
		active_mod.source = source
		active_mod.stacks = stacks
		active_modifiers.append(active_mod)

	_mark_stats_dirty()
	return true

## Remove a modifier (e.g., temporary boon expires)
func remove_modifier(modifier_id: int, stacks_to_remove: int = 1) -> bool:
	var existing: ActiveModifier = _find_modifier(modifier_id)
	if not existing:
		return false

	existing.stacks -= stacks_to_remove
	if existing.stacks <= 0:
		active_modifiers.erase(existing)

	_mark_stats_dirty()
	return true

## Get computed stats (base + all modifiers applied)
func get_computed_stats() -> Dictionary:
	if _stats_dirty:
		_recompute_stats()
	return _cached_stats

## Get a specific computed stat
func get_stat(stat_name: String) -> float:
	var stats: Dictionary = get_computed_stats()
	return stats.get(stat_name, 0.0)

## Serialize to dictionary (for saving)
func to_dict() -> Dictionary:
	var modifiers_array: Array = []
	for mod: ActiveModifier in active_modifiers:
		modifiers_array.append(mod.to_dict())

	return {
		"hero_id": config.hero_id,
		"level": level,
		"xp": xp,
		"active_modifiers": modifiers_array
	}

## Create from dictionary (when loading from save)
static func from_dict(data: Dictionary) -> HeroInstance:
	var instance: HeroInstance = HeroInstance.new()

	# Load config from HeroCatalog
	var hero_id: String = data.get("hero_id", "")
	if hero_id.is_empty():
		push_error("HeroInstance.from_dict: Missing hero_id")
		return null

	var hero_config: HeroConfig = HeroCatalog.get_hero_config(hero_id)
	if not hero_config:
		push_error("HeroInstance.from_dict: Hero config not found: %s" % hero_id)
		return null

	instance.config = hero_config
	instance.level = data.get("level", 1)
	instance.xp = data.get("xp", 0)

	# Load active modifiers
	var modifiers_data: Variant = data.get("active_modifiers", [])
	if modifiers_data is Array:
		var modifiers_array: Array = modifiers_data
		for mod_data: Variant in modifiers_array:
			if mod_data is Dictionary:
				var active_mod: ActiveModifier = ActiveModifier.from_dict(mod_data)
				if active_mod.is_valid():
					instance.active_modifiers.append(active_mod)

	instance._mark_stats_dirty()
	return instance

## Validation
func is_valid() -> bool:
	if config == null:
		push_error("HeroInstance: config is null")
		return false
	if not config.is_valid():
		return false
	if level < 1:
		push_error("HeroInstance: level must be >= 1")
		return false
	return true

## PRIVATE METHODS

## Find an active modifier by ID
func _find_modifier(modifier_id: int) -> ActiveModifier:
	for mod: ActiveModifier in active_modifiers:
		if mod.modifier_id == modifier_id:
			return mod
	return null

## Mark stats as dirty (need recomputation)
func _mark_stats_dirty() -> void:
	_stats_dirty = true

## Recompute stats with all modifiers applied
func _recompute_stats() -> void:
	# Start with base stats from config
	var stats: Dictionary = {
		"health": config.base_health,
		"max_mana": config.max_mana,
		"mana_regen": config.mana_regen
	}

	# Apply all modifiers
	for active_mod: ActiveModifier in active_modifiers:
		var mod_config: ModifierConfig = active_mod.get_config()
		if not mod_config:
			continue

		# Apply each effect
		for effect: ModifierEffect in mod_config.effects:
			_apply_effect(stats, effect, active_mod.stacks)

	_cached_stats = stats
	_stats_dirty = false

## Apply a single modifier effect to stats
func _apply_effect(stats: Dictionary, effect: ModifierEffect, stacks: int) -> void:
	match effect.type:
		ModifierEffect.EffectType.HERO_STAT:
			_apply_hero_stat_effect(stats, effect, stacks)

		ModifierEffect.EffectType.UNIT_TAG_STAT:
			# Store for later (when spawning units)
			# For MVP, we'll just note this in cached stats
			if not stats.has("unit_tag_modifiers"):
				stats["unit_tag_modifiers"] = []
			stats["unit_tag_modifiers"].append({
				"tag": effect.tag,
				"stat": effect.stat,
				"mode": effect.mode,
				"value": effect.value * stacks
			})

		ModifierEffect.EffectType.UNLOCK_CARD:
			# Store unlocked cards
			if not stats.has("unlocked_cards"):
				stats["unlocked_cards"] = []
			if not stats["unlocked_cards"].has(effect.card_id):
				stats["unlocked_cards"].append(effect.card_id)

## Apply a hero stat effect
func _apply_hero_stat_effect(stats: Dictionary, effect: ModifierEffect, stacks: int) -> void:
	var stat_name: String = effect.stat
	if not stats.has(stat_name):
		push_warning("HeroInstance: Unknown stat '%s'" % stat_name)
		return

	var base_value: float = stats[stat_name]
	var effect_value: float = effect.value * stacks

	match effect.mode:
		ModifierEffect.StatMode.ADD:
			stats[stat_name] = base_value + effect_value

		ModifierEffect.StatMode.MULTIPLY:
			stats[stat_name] = base_value * (1.0 + effect_value)
