extends BaseAbility
class_name AuraAbility

## AuraAbility - Affects nearby units periodically
##
## Highly configurable aura that can damage, heal, buff, or debuff targets.
## Triggers every tick_rate seconds and affects units within radius.
##
## Examples:
##   - Damage Aura: damages_per_tick=5, affects_enemies=true
##   - Heal Aura: heal_per_tick=10, affects_allies=true
##   - Speed Buff: speed_modifier=0.3, affects_allies=true

enum AuraType {
	DAMAGE,      ## Damages targets over time
	HEAL,        ## Heals targets over time
	BUFF_SPEED,  ## Increases movement speed
	DEBUFF_SLOW, ## Decreases movement speed
	CUSTOM       ## For future custom effects
}

## =============================================================================
## CONFIGURATION
## =============================================================================

## Type of aura effect
@export var aura_type: AuraType = AuraType.DAMAGE

## Radius of aura effect
@export var radius: float = 4.0

## How often the aura triggers (in seconds)
@export var tick_rate: float = 1.0

## =============================================================================
## EFFECT VALUES
## =============================================================================

## Damage dealt per tick (for DAMAGE type)
@export var damage_per_tick: float = 5.0

## Healing amount per tick (for HEAL type)
@export var heal_per_tick: float = 0.0

## Speed modifier (for BUFF_SPEED/DEBUFF_SLOW)
## 0.3 = +30% speed, -0.5 = -50% speed
@export var speed_modifier: float = 0.0

## How long speed modifiers last (should be >= tick_rate)
@export var modifier_duration: float = 1.5

## =============================================================================
## TARGETING
## =============================================================================

## Whether aura affects enemy units
@export var affects_enemies: bool = true

## Whether aura affects allied units
@export var affects_allies: bool = false

## Whether aura affects the owner unit
@export var affects_self: bool = false

## =============================================================================
## VISUAL/AUDIO
## =============================================================================

## Persistent aura visual effect (attached to unit)
@export var persistent_aura_vfx: String = ""

## VFX played each tick
@export var tick_vfx: String = ""

## Sound played each tick
@export var tick_sound: String = ""

## =============================================================================
## RUNTIME STATE
## =============================================================================

var tick_timer: float = 0.0
var persistent_vfx_node: Node = null

## =============================================================================
## INITIALIZATION
## =============================================================================

func _initialize() -> void:
	# Start tick timer at random offset to prevent all auras syncing
	tick_timer = randf() * tick_rate

	# Spawn persistent aura VFX
	if not persistent_aura_vfx.is_empty():
		persistent_vfx_node = _spawn_vfx(persistent_aura_vfx, owner_unit.global_position, owner_unit)
		if persistent_vfx_node:
			print("AuraAbility: Spawned persistent VFX '%s' for %s" % [persistent_aura_vfx, owner_unit.name])

func _connect_to_unit_events() -> void:
	# Clean up VFX when unit dies
	owner_unit.unit_died.connect(_on_owner_died)

## =============================================================================
## UPDATE
## =============================================================================

func _physics_process(delta: float) -> void:
	if not is_active or not owner_unit or not owner_unit.is_alive:
		return

	tick_timer -= delta
	if tick_timer <= 0:
		_trigger_aura()
		tick_timer = tick_rate

## =============================================================================
## AURA LOGIC
## =============================================================================

func _trigger_aura() -> void:
	var targets: Array[Unit3D] = _get_units_in_radius(owner_unit.global_position, radius, affects_enemies, affects_allies, affects_self)

	if targets.is_empty():
		return

	# Apply effect based on aura type
	match aura_type:
		AuraType.DAMAGE:
			_apply_damage_aura(targets)

		AuraType.HEAL:
			_apply_heal_aura(targets)

		AuraType.BUFF_SPEED:
			_apply_speed_buff_aura(targets)

		AuraType.DEBUFF_SLOW:
			_apply_speed_debuff_aura(targets)

	# Play tick VFX
	if not tick_vfx.is_empty():
		_spawn_vfx(tick_vfx, owner_unit.global_position)

	# Emit signal
	ability_triggered.emit({"targets_affected": targets.size(), "aura_type": AuraType.keys()[aura_type]})

## Apply damage to all targets
func _apply_damage_aura(targets: Array[Unit3D]) -> void:
	if damage_per_tick <= 0:
		return

	for target: Unit3D in targets:
		_apply_damage(target, damage_per_tick, "fire")

## Apply healing to all targets
func _apply_heal_aura(targets: Array[Unit3D]) -> void:
	if heal_per_tick <= 0:
		return

	for target: Unit3D in targets:
		if target.has_method("heal"):
			target.heal(heal_per_tick)

## Apply speed buff to all targets
func _apply_speed_buff_aura(targets: Array[Unit3D]) -> void:
	if speed_modifier == 0:
		return

	for target: Unit3D in targets:
		# Apply temporary speed modifier
		if target.has_method("apply_modifier"):
			target.apply_modifier({
				"source": "aura_speed_buff_%s" % owner_unit.name,
				"duration": modifier_duration,
				"stats": {"move_speed": speed_modifier},
				"amplification": "MULTIPLICATIVE"  # Percentage-based
			})

## Apply speed debuff to all targets
func _apply_speed_debuff_aura(targets: Array[Unit3D]) -> void:
	if speed_modifier == 0:
		return

	var debuff_value: float = -abs(speed_modifier)  # Ensure negative
	for target: Unit3D in targets:
		if target.has_method("apply_modifier"):
			target.apply_modifier({
				"source": "aura_slow_%s" % owner_unit.name,
				"duration": modifier_duration,
				"stats": {"move_speed": debuff_value},
				"amplification": "MULTIPLICATIVE"
			})

## =============================================================================
## CLEANUP
## =============================================================================

func _on_owner_died(_unit: Unit3D) -> void:
	# Remove persistent VFX
	if persistent_vfx_node and is_instance_valid(persistent_vfx_node):
		persistent_vfx_node.queue_free()
	deactivate()
