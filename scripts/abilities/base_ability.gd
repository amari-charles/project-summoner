extends Node
class_name BaseAbility

## BaseAbility - Abstract base class for all unit abilities
##
## Abilities are components that attach to units and modify their behavior.
## They react to unit events (attack, movement, death) to trigger special effects.
##
## Usage:
##   1. Extend this class for custom abilities
##   2. Override _initialize() for setup
##   3. Override _connect_to_unit_events() to listen to unit signals
##   4. Add as child node to unit in scene or via code

## Emitted when ability triggers an effect
signal ability_triggered(data: Dictionary)

## Reference to the unit this ability is attached to
var owner_unit: Unit3D = null

## Whether this ability is currently active
var is_active: bool = true

## =============================================================================
## LIFECYCLE
## =============================================================================

## Called when ability is added to a unit
## Do not override - use _initialize() instead
func setup(unit: Unit3D) -> void:
	if not unit:
		push_error("BaseAbility.setup: unit is null")
		return

	owner_unit = unit
	_connect_to_unit_events()
	_initialize()

	var resource_path: String = get_script().resource_path
	var script_name: String = resource_path.get_file().get_basename()
	print("BaseAbility: %s setup complete for unit %s" % [script_name, unit.name])

## Override in subclasses for custom initialization
## Called after owner_unit is set and events are connected
func _initialize() -> void:
	pass

## Override in subclasses to connect to unit signals
## Example: owner_unit.unit_died.connect(_on_owner_died)
func _connect_to_unit_events() -> void:
	pass

## =============================================================================
## ABILITY CONTROL
## =============================================================================

## Enable this ability
func activate() -> void:
	is_active = true

## Disable this ability
func deactivate() -> void:
	is_active = false

## Toggle ability on/off
func toggle() -> void:
	is_active = not is_active

## =============================================================================
## HELPER METHODS
## =============================================================================

## Get units within radius of a position
func _get_units_in_radius(center: Vector3, radius: float, target_enemies: bool = true, target_allies: bool = false, include_self: bool = false) -> Array[Unit3D]:
	var units: Array[Unit3D] = []

	if not owner_unit:
		return units

	# Determine which groups to check
	var groups_to_check: Array[String] = []
	if target_enemies:
		var enemy_group: String = "enemy_units" if owner_unit.team == Unit3D.Team.PLAYER else "player_units"
		groups_to_check.append(enemy_group)
	if target_allies:
		var ally_group: String = "player_units" if owner_unit.team == Unit3D.Team.PLAYER else "enemy_units"
		groups_to_check.append(ally_group)

	# Find units in range
	for group: String in groups_to_check:
		var group_units: Array[Node] = get_tree().get_nodes_in_group(group)
		for node: Node in group_units:
			if node is Unit3D:
				var unit: Unit3D = node as Unit3D
				if unit.is_alive:
					var distance: float = unit.global_position.distance_to(center)
					if distance <= radius:
						if unit != owner_unit or include_self:
							units.append(unit)

	return units

## Apply damage to a target
func _apply_damage(target: Unit3D, damage: float, damage_type: String = "physical") -> void:
	if not owner_unit or not target:
		return
	DamageSystem.apply_damage(owner_unit, target, damage, damage_type)

## Spawn VFX at position
func _spawn_vfx(vfx_id: String, position: Vector3, parent: Node = null) -> Node:
	if vfx_id.is_empty():
		return null

	var vfx: Node = VFXManager.play_effect(vfx_id, position)
	if vfx and parent:
		vfx.reparent(parent)
		if vfx is Node3D:
			(vfx as Node3D).position = Vector3.ZERO
	return vfx
