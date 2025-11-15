extends BaseAbility
class_name DeathExplosionAbility

## DeathExplosionAbility - Explodes on death, dealing AoE damage
##
## Triggers an explosion when the unit dies, damaging nearby units.
## Can be configured to affect enemies, allies, or both (friendly fire).
##
## Example:
##   explosion_radius = 3.0    # 3 unit radius
##   explosion_damage = 50.0   # 50 damage
##   affects_enemies = true    # Damage enemies
##   affects_allies = false    # No friendly fire

## =============================================================================
## CONFIGURATION
## =============================================================================

## Radius of explosion damage
@export var explosion_radius: float = 3.0

## Damage dealt to units in explosion
@export var explosion_damage: float = 50.0

## Damage type (physical, fire, etc.)
@export var damage_type: String = "fire"

## =============================================================================
## TARGETING
## =============================================================================

## Whether explosion damages enemy units
@export var affects_enemies: bool = true

## Whether explosion damages allied units (friendly fire)
@export var affects_allies: bool = false

## =============================================================================
## VISUAL/AUDIO
## =============================================================================

## VFX played at explosion center
@export var explosion_vfx: String = "explosion_default"

## Sound played on explosion
@export var explosion_sound: String = ""

## Delay before explosion triggers (in seconds)
@export var explosion_delay: float = 0.0

## =============================================================================
## RUNTIME STATE
## =============================================================================

var has_exploded: bool = false

## =============================================================================
## INITIALIZATION
## =============================================================================

func _connect_to_unit_events() -> void:
	# Trigger explosion when unit dies
	owner_unit.unit_died.connect(_on_owner_died)

## =============================================================================
## EXPLOSION LOGIC
## =============================================================================

func _on_owner_died(_unit: Unit3D) -> void:
	if not is_active or has_exploded:
		return

	if explosion_delay > 0:
		# Delayed explosion
		await get_tree().create_timer(explosion_delay).timeout

	_trigger_explosion()

func _trigger_explosion() -> void:
	has_exploded = true

	var explosion_center: Vector3 = owner_unit.global_position

	# Spawn VFX first
	if not explosion_vfx.is_empty():
		_spawn_vfx(explosion_vfx, explosion_center)

	# Get targets in explosion radius
	var targets: Array[Unit3D] = _get_units_in_radius(explosion_center, explosion_radius, affects_enemies, affects_allies, false)

	# Apply damage to all targets
	var hit_count: int = 0
	for target: Unit3D in targets:
		_apply_damage(target, explosion_damage, damage_type)
		hit_count += 1

	print("DeathExplosionAbility: %s exploded! Hit %d units in %.1f radius" % [owner_unit.name, hit_count, explosion_radius])

	# Emit signal
	ability_triggered.emit({
		"event": "explosion",
		"targets_hit": hit_count,
		"damage": explosion_damage,
		"radius": explosion_radius
	})

	deactivate()

## =============================================================================
## DEBUG
## =============================================================================

## Manually trigger explosion (for testing)
func trigger_manual_explosion() -> void:
	if has_exploded:
		push_warning("DeathExplosionAbility: Already exploded")
		return
	_trigger_explosion()
