extends Resource
class_name Card

## Card executor - handles playing cards (summons, spells)
## Data is stored in CardConfig; this class handles execution only
##
## Architecture:
## - CardConfig: Pure data (catalog_id, mana_cost, formation_spacing, etc.)
## - Card: Executor with config reference and runtime state

## Preload CardConfig to avoid class loading order issues
const CardConfigClass = preload("res://scripts/cards/card_config.gd")

enum CardType { SUMMON, SPELL }

## =============================================================================
## CONFIGURATION
## =============================================================================

## Card configuration data (stats, costs, formation, etc.)
var config: Resource = null  # CardConfig instance (uses Resource to avoid load order issues)

## =============================================================================
## INSTANCE STATE (runtime, not config)
## =============================================================================

## Instance tracking (for progression system - unique per card in collection)
var instance_id: String = ""

## C# spell effect delegation - if set, spell execution delegates to CardFactory
## This is set by CardCatalog when creating spell cards with C# effects available
var _csharp_spell_id: String = ""

## C# summon execution delegation - if set, summon execution delegates to CardFactory
## This is set by CardCatalog when creating summon cards
var _csharp_summon_id: String = ""

## Event sequence stat overrides (set by EventSequencer before spawning)
var custom_stat_overrides: Dictionary = {}

## =============================================================================
## PROPERTY ACCESSORS (delegate to config for backward compatibility)
## =============================================================================

var catalog_id: String:
	get: return config.catalog_id if config else ""

var card_name: String:
	get: return config.card_name if config else "Unknown Card"

var card_type: int:
	get: return config.card_type if config else CardType.SUMMON

var description: String:
	get: return config.description if config else ""

var mana_cost: int:
	get: return config.mana_cost if config else 1

var cooldown: float:
	get: return config.cooldown if config else 2.0

var summon_time: float:
	get: return config.summon_time if config else 1.0

var unit_scene: PackedScene:
	get: return config.unit_scene if config else null

var spawn_count: int:
	get: return config.spawn_count if config else 1

var spell_damage: float:
	get: return config.spell_damage if config else 0.0

var spell_radius: float:
	get: return config.spell_radius if config else 0.0

var spell_duration: float:
	get: return config.spell_duration if config else 0.0

var projectile_id: String:
	get: return config.projectile_id if config else ""

var spell_vfx: String:
	get: return config.spell_vfx if config else ""

var card_icon: Texture2D:
	get: return config.card_icon if config else null

var formation_type: String:
	get: return config.formation_type if config else "grid"

var formation_spacing: float:
	get: return config.formation_spacing if config else DEFAULT_FORMATION_SPACING

var formation_row_offset: float:
	get: return config.formation_row_offset if config else DEFAULT_FORMATION_ROW_OFFSET

var group_spacing: float:
	get: return config.group_spacing if config else 4.0

var units_per_group: int:
	get: return config.units_per_group if config else 2

## =============================================================================
## FORMATION PREVIEW (for UI spawn position indicators)
## =============================================================================

## Formation constants used by static preview method
const FORMATION_TWO_ROW_MAX: int = 20
const FORMATION_LARGE_ROW_DENSITY: float = 3.0
const DEFAULT_FORMATION_SPACING: float = 1.8
const DEFAULT_FORMATION_ROW_OFFSET: float = 0.5

## Static helper for UI spawn preview (uses default grid config values)
## Note: Actual spawning uses C# formations via CardFactory
## For cards with custom formations, use get_formation_offset() instance method
static func generate_formation_offset(unit_index: int, unit_count: int) -> Vector3:
	if unit_count <= 1:
		return Vector3.ZERO

	var rows: int = 2 if unit_count <= FORMATION_TWO_ROW_MAX else ceili(sqrt(float(unit_count) / FORMATION_LARGE_ROW_DENSITY))
	var cols: int = ceili(float(unit_count) / float(rows))

	var row: int = unit_index / cols
	var col: int = unit_index % cols
	var units_in_row: int = mini(cols, unit_count - row * cols)

	var stagger: float = DEFAULT_FORMATION_ROW_OFFSET * DEFAULT_FORMATION_SPACING if row % 2 == 1 else 0.0

	var formation_depth: float = (rows - 1) * DEFAULT_FORMATION_SPACING
	var x_offset: float = row * DEFAULT_FORMATION_SPACING - formation_depth / 2.0

	var row_width: float = (units_in_row - 1) * DEFAULT_FORMATION_SPACING
	var z_offset: float = col * DEFAULT_FORMATION_SPACING - row_width / 2.0 + stagger

	return Vector3(x_offset, 0, z_offset)


## Instance method that respects card's formation config
## Delegates to C# CardFactory for formation calculation (single source of truth)
func get_formation_offset(unit_index: int) -> Vector3:
	if spawn_count <= 1:
		return Vector3.ZERO

	# Delegate to C# CardFactory for formation calculation
	var factory: Node = _get_card_factory()
	if not factory:
		push_error("Card: CardFactory not available! C# autoload may not be loaded.")
		return Vector3.ZERO

	var card_def: Dictionary = CardCatalog.get_card(catalog_id)
	return factory.get_formation_offset(card_def, unit_index, spawn_count)

## =============================================================================
## GAMEPLAY
## =============================================================================

## Validate if this card can be played
func can_play(current_mana: int) -> bool:
	return current_mana >= mana_cost

## Get effective stats with card upgrades applied
## Returns a Dictionary with the same structure as CardCatalog.get_card() but with
## upgrade modifiers (from PlayerCardService) applied multiplicatively.
func get_effective_stats() -> Dictionary:
	# Get base stats from catalog
	var base_stats: Dictionary = CardCatalog.get_card(catalog_id).duplicate(true)

	# If no instance_id, return base stats (enemy cards, test cards)
	if instance_id.is_empty():
		return base_stats

	# Delegate to PlayerCardService for full stat pipeline
	var card_service: Node = _get_autoload_node("/root/PlayerCardService")
	if card_service:
		var effective: Variant = card_service.call("get_effective_stats", instance_id)
		if effective is Dictionary and not effective.is_empty():
			return effective

	return base_stats

## Helper to get autoload nodes (Card is a Resource, not a Node)
func _get_autoload_node(path: String) -> Node:
	var main_loop: MainLoop = Engine.get_main_loop()
	if main_loop is SceneTree:
		var tree: SceneTree = main_loop
		if tree and tree.root:
			return tree.root.get_node_or_null(path)
	return null

## Check if this card needs click-targeting (Rally/Guard with command_type)
func needs_click_targeting() -> bool:
	if card_type != CardType.SPELL:
		return false

	var card_def: Dictionary = CardCatalog.get_card(catalog_id)
	return card_def.has("command_type")

## Execute the card effect at the given 3D position
## modifier_system: Optional ModifierSystem reference for more efficient access
## spawn_duration: If > 0, applies spawn reveal effect over this duration (for summon cards)
func play_3d(play_position: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit_3d(play_position, team, battlefield, modifier_system, spawn_duration)
		CardType.SPELL:
			_cast_spell_3d(play_position, team, battlefield, modifier_system)

## Spawn unit(s) at the 3D position
## All summons delegate to C# CardFactory for execution
func _summon_unit_3d(spawn_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	if _csharp_summon_id.is_empty():
		push_error("Card: Summon '%s' has no C# summon ID attached! All summons must use C# CardFactory." % card_name)
		return

	_execute_csharp_summon(spawn_pos, team, battlefield, modifier_system, spawn_duration)

## Execute spell effect at the 3D position
## All spells delegate to C# CardFactory for execution
func _cast_spell_3d(cast_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null) -> void:
	if _csharp_spell_id.is_empty():
		push_error("Card: Spell '%s' has no C# effect attached! All spells must use C# CardFactory." % card_name)
		return

	_execute_csharp_spell(cast_pos, team, battlefield, modifier_system)


## Execute spell via C# CardFactory
func _execute_csharp_spell(cast_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null) -> void:
	var factory: Node = _get_card_factory()
	if not factory:
		push_error("Card: CardFactory not available! C# may not be loaded. Spell '%s' cannot be cast." % _csharp_spell_id)
		return

	factory.execute_spell(_csharp_spell_id, cast_pos, int(team), battlefield, modifier_system, instance_id)


## Execute summon via C# CardFactory
func _execute_csharp_summon(spawn_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	var factory: Node = _get_card_factory()
	if not factory:
		push_error("Card: CardFactory not available! C# may not be loaded. Summon '%s' cannot spawn." % _csharp_summon_id)
		return

	# Get card definition from catalog
	var card_def: Dictionary = CardCatalog.get_card(catalog_id)
	if card_def.is_empty():
		push_error("Card: Cannot get card definition for '%s'" % catalog_id)
		return

	# Get effective stats (with upgrades applied)
	var effective_stats: Dictionary = get_effective_stats()

	# Execute summon via factory
	factory.execute_summon(
		_csharp_summon_id,
		spawn_pos,
		int(team),
		battlefield,
		card_def,
		effective_stats,
		custom_stat_overrides,
		modifier_system,
		instance_id,
		spawn_duration
	)


## Get CardFactory autoload safely
func _get_card_factory() -> Node:
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop or not main_loop is SceneTree:
		return null

	var tree: SceneTree = main_loop
	if not tree.root:
		return null

	return tree.root.get_node_or_null("/root/CardFactory")
