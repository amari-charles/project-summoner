extends RefCounted
class_name SummonerInstance

## SummonerInstance - Runtime instance of a summoner during a run
##
## Represents an active summoner with level progression and traits.
## Computes final stats by applying trait modifiers to base config stats.
## Serializes to JSON (saves only IDs + state, reconstructs from config).
##
## All traits come from TraitCatalog:
## - Innate traits: Defined in SummonerConfig.innate_trait_ids
## - Acquired boons: Earned through gameplay, stored in acquired_boon_ids

## Reference to the summoner's configuration (template)
var config: SummonerConfig = null

## Progression
var level: int = 1
var xp: int = 0

## Acquired Boons (from TraitCatalog, earned through gameplay)
var acquired_boon_ids: Array[String] = []

## Cached computed stats (updated when traits change)
var _cached_stats: Dictionary = {}
var _stats_dirty: bool = true

## Level progression constants
const LEVEL_STAT_BONUS_PERCENT: float = 0.05  ## 5% stat bonus per level above 1

## Initialize from a SummonerConfig (called when starting a new run)
func init_from_config(summoner_config: SummonerConfig) -> void:
	config = summoner_config
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
		push_warning("SummonerInstance.add_boon: Boon already acquired: %s" % boon_id)
		return false

	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("TraitCatalog")
	if trait_catalog and not trait_catalog.has_trait(boon_id):
		push_error("SummonerInstance.add_boon: Unknown boon ID: %s" % boon_id)
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

## Check if summoner has a specific boon
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
		"summoner_id": config.summoner_id,
		"level": level,
		"xp": xp,
		"acquired_boon_ids": boons_array
	}

## Create from dictionary (when loading from save)
static func from_dict(data: Dictionary) -> SummonerInstance:
	var instance: SummonerInstance = SummonerInstance.new()

	# Load config from SummonerCatalog
	var summoner_id: String = data.get("summoner_id", "")
	if summoner_id.is_empty():
		push_error("SummonerInstance.from_dict: Missing summoner_id")
		return null

	var summoner_config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not summoner_config:
		push_error("SummonerInstance.from_dict: Summoner config not found: %s" % summoner_id)
		return null

	instance.config = summoner_config
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
		push_error("SummonerInstance: config is null")
		return false
	if not config.is_valid():
		return false
	if level < 1:
		push_error("SummonerInstance: level must be >= 1")
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
		# Trait-related bonus stats (default to 0, modified by traits)
		"fire_damage_bonus": 0.0,
		"water_damage_bonus": 0.0,
		"wind_damage_bonus": 0.0,
		"earth_damage_bonus": 0.0,
		"damage_bonus": 0.0,
		"damage_reduction": 0.0
	}

	# Apply level bonuses
	var level_multiplier: float = 1.0 + (level - 1) * LEVEL_STAT_BONUS_PERCENT
	stats["health"] *= level_multiplier
	stats["max_mana"] *= level_multiplier

	# Apply all trait modifiers from TraitCatalog
	_apply_trait_modifiers(stats)

	_cached_stats = stats
	_stats_dirty = false

## Apply all trait modifiers from TraitCatalog
func _apply_trait_modifiers(stats: Dictionary) -> void:
	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("TraitCatalog")
	if not trait_catalog:
		push_warning("SummonerInstance: TraitCatalog not found, traits will not be applied")
		return

	var all_trait_ids: Array[String] = get_all_trait_ids()
	for trait_id: String in all_trait_ids:
		var trait_data: Dictionary = trait_catalog.get_trait(trait_id)
		if trait_data.is_empty():
			push_warning("SummonerInstance: Unknown trait '%s' - skipping" % trait_id)
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
## - "flat": Adds value directly to the stat (e.g., +50 health)
## - "percent": For BASE stats (health, max_mana): Multiplies by (1 + value/100)
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
