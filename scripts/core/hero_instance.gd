extends RefCounted
class_name HeroInstance

## HeroInstance - Runtime instance of a hero during a run
##
## Represents an active hero with level progression and traits.
## Computes final stats by applying trait modifiers to base config stats.
## Serializes to JSON (saves only IDs + state, reconstructs from config).
##
## All traits come from TraitCatalog:
## - Innate traits: Defined in HeroConfig.innate_trait_ids
## - Acquired boons: Earned through gameplay, stored in acquired_boon_ids

## Reference to the hero's configuration (template)
var config: HeroConfig = null

## Progression
var level: int = 1
var xp: int = 0

## Acquired Boons (from TraitCatalog, earned through gameplay)
var acquired_boon_ids: Array[String] = []

## Cached computed stats (updated when traits change)
var _cached_stats: Dictionary = {}
var _stats_dirty: bool = true

## Initialize from a HeroConfig (called when starting a new run)
func init_from_config(hero_config: HeroConfig) -> void:
	config = hero_config
	level = 1
	xp = 0
	acquired_boon_ids.clear()
	_mark_stats_dirty()

## =============================================================================
## BOON MANAGEMENT (from TraitCatalog)
## =============================================================================

## Add an acquired boon
func add_boon(boon_id: String) -> bool:
	if boon_id in acquired_boon_ids:
		push_warning("HeroInstance.add_boon: Boon already acquired: %s" % boon_id)
		return false

	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("/root/TraitCatalog")
	if trait_catalog and trait_catalog.has_method("has_trait"):
		if not trait_catalog.call("has_trait", boon_id):
			push_error("HeroInstance.add_boon: Unknown boon ID: %s" % boon_id)
			return false

	acquired_boon_ids.append(boon_id)
	_mark_stats_dirty()
	return true

## Remove an acquired boon
func remove_boon(boon_id: String) -> bool:
	var index: int = acquired_boon_ids.find(boon_id)
	if index == -1:
		return false

	acquired_boon_ids.remove_at(index)
	_mark_stats_dirty()
	return true

## Check if hero has a specific boon
func has_boon(boon_id: String) -> bool:
	return boon_id in acquired_boon_ids

## Get all trait IDs (innate from config + acquired boons)
func get_all_trait_ids() -> Array[String]:
	var all_traits: Array[String] = []

	# Add innate traits from config
	if config:
		for trait_id: String in config.innate_trait_ids:
			all_traits.append(trait_id)

	# Add acquired boons
	for boon_id: String in acquired_boon_ids:
		if not boon_id in all_traits:
			all_traits.append(boon_id)

	return all_traits

## =============================================================================
## STAT COMPUTATION
## =============================================================================

## Get computed stats (base + all trait modifiers applied)
func get_computed_stats() -> Dictionary:
	if _stats_dirty:
		_recompute_stats()
	return _cached_stats

## Get a specific computed stat
func get_stat(stat_name: String) -> float:
	var stats: Dictionary = get_computed_stats()
	return stats.get(stat_name, 0.0)

## =============================================================================
## SERIALIZATION
## =============================================================================

## Serialize to dictionary (for saving)
func to_dict() -> Dictionary:
	# Convert acquired_boon_ids to regular Array for JSON
	var boons_array: Array = []
	for boon_id: String in acquired_boon_ids:
		boons_array.append(boon_id)

	return {
		"hero_id": config.hero_id,
		"level": level,
		"xp": xp,
		"acquired_boon_ids": boons_array
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

	# Load acquired boons
	var boons_data: Variant = data.get("acquired_boon_ids", [])
	if boons_data is Array:
		var boons_array: Array = boons_data
		for boon_id_var: Variant in boons_array:
			if boon_id_var is String:
				instance.acquired_boon_ids.append(boon_id_var)

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

## =============================================================================
## PRIVATE METHODS
## =============================================================================

## Mark stats as dirty (need recomputation)
func _mark_stats_dirty() -> void:
	_stats_dirty = true

## Recompute stats with all trait modifiers applied
func _recompute_stats() -> void:
	# Start with base stats from config
	var stats: Dictionary = {
		"health": config.base_health,
		"max_mana": config.max_mana,
		"mana_regen": config.mana_regen,
		# Trait-related bonus stats (default to 0, modified by traits)
		"fire_damage_bonus": 0.0,
		"water_damage_bonus": 0.0,
		"wind_damage_bonus": 0.0,
		"earth_damage_bonus": 0.0,
		"damage_bonus": 0.0,
		"damage_reduction": 0.0
	}

	# Apply all trait modifiers from TraitCatalog
	_apply_trait_modifiers(stats)

	_cached_stats = stats
	_stats_dirty = false

## Apply all trait modifiers from TraitCatalog
func _apply_trait_modifiers(stats: Dictionary) -> void:
	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		push_warning("HeroInstance: TraitCatalog not found, traits will not be applied")
		return

	var all_trait_ids: Array[String] = get_all_trait_ids()
	for trait_id: String in all_trait_ids:
		if not trait_catalog.has_method("get_trait"):
			continue

		var trait_data: Dictionary = trait_catalog.call("get_trait", trait_id)
		if trait_data.is_empty():
			push_warning("HeroInstance: Unknown trait '%s' - skipping" % trait_id)
			continue

		var modifiers: Variant = trait_data.get("modifiers", [])
		if not modifiers is Array:
			continue

		for mod: Variant in modifiers:
			if mod is Dictionary:
				_apply_single_trait_modifier(stats, mod)

## Apply a single trait modifier to stats
##
## Modifier types:
## - "flat": Adds value directly to the stat (e.g., +50 health, +0.3 mana_regen)
## - "percent": For BASE stats (health, mana_regen, max_mana): Multiplies by (1 + value/100)
##              For BONUS stats (fire_damage_bonus, etc.): Adds value as the percentage amount
##
## Example: trait_fire_affinity has {"stat": "fire_damage_bonus", "type": "percent", "value": 10.0}
##          This sets fire_damage_bonus = 10.0 (meaning 10% bonus fire damage)
##
## Example: trait_tidal_resilience has {"stat": "max_health", "type": "percent", "value": 10.0}
##          This multiplies health by 1.10 (10% more health)
func _apply_single_trait_modifier(stats: Dictionary, modifier: Dictionary) -> void:
	var stat_name: String = modifier.get("stat", "")
	var mod_type: String = modifier.get("type", "flat")
	var value: float = modifier.get("value", 0.0)

	if stat_name.is_empty():
		return

	# Map trait stat names to internal stat names
	var mapped_stat: String = _map_trait_stat_name(stat_name)

	if not stats.has(mapped_stat):
		# Unknown stat - store it anyway for future use
		stats[mapped_stat] = 0.0

	var current_value: float = stats[mapped_stat]

	match mod_type:
		"flat":
			# Flat modifier: add directly to the stat
			stats[mapped_stat] = current_value + value
		"percent":
			# Percent modifier has context-dependent behavior:
			# - BASE stats (non-zero initial value): multiplicative bonus
			# - BONUS stats (zero initial value): the value IS the percentage
			if current_value > 0.0:
				# Base stat: multiply (e.g., 1000 health * 1.10 = 1100 health)
				stats[mapped_stat] = current_value * (1.0 + value / 100.0)
			else:
				# Bonus stat: add directly (e.g., fire_damage_bonus = 0 + 10 = 10%)
				stats[mapped_stat] = current_value + value

## Map trait stat names to internal stat names
func _map_trait_stat_name(trait_stat: String) -> String:
	match trait_stat:
		"max_health":
			return "health"
		_:
			return trait_stat
