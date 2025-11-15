extends BaseAbility
class_name ChargeAbility

## ChargeAbility - Bonus damage after moving a distance
##
## Tracks unit movement and applies bonus damage when the unit has traveled
## far enough since their last attack. Rewards aggressive positioning.
##
## Example:
##   charge_threshold = 5.0  # Must travel 5 units
##   damage_bonus = 30.0     # +30 damage (or +30% if PERCENTAGE)
##   bonus_type = FLAT       # Flat damage increase

enum BonusType {
	FLAT,       ## Add bonus damage directly
	PERCENTAGE  ## Multiply base damage by (1 + bonus/100)
}

## =============================================================================
## CONFIGURATION
## =============================================================================

## Distance unit must travel to activate charge
@export var charge_threshold: float = 5.0

## Damage bonus when charged
@export var damage_bonus: float = 30.0

## How the bonus is applied
@export var bonus_type: BonusType = BonusType.FLAT

## Whether charge resets after ANY attack or only charged attacks
@export var reset_on_any_attack: bool = true

## VFX played when charge is ready
@export var charge_ready_vfx: String = ""

## VFX played on charged attack impact
@export var charge_impact_vfx: String = ""

## Sound played on charged attack
@export var charge_impact_sound: String = ""

## =============================================================================
## RUNTIME STATE
## =============================================================================

var distance_traveled_since_attack: float = 0.0
var last_position: Vector3 = Vector3.ZERO
var is_charged: bool = false
var charge_ready_vfx_node: Node = null

## =============================================================================
## INITIALIZATION
## =============================================================================

func _initialize() -> void:
	last_position = owner_unit.global_position

func _connect_to_unit_events() -> void:
	# Listen for attacks to apply bonus and reset
	owner_unit.unit_attacked.connect(_on_owner_attacked)

## =============================================================================
## UPDATE
## =============================================================================

func _physics_process(delta: float) -> void:
	if not is_active or not owner_unit or not owner_unit.is_alive:
		return

	# Track distance moved
	var current_position = owner_unit.global_position
	var distance_moved = current_position.distance_to(last_position)
	last_position = current_position

	distance_traveled_since_attack += distance_moved

	# Check if charge is ready
	var was_charged = is_charged
	is_charged = distance_traveled_since_attack >= charge_threshold

	# Show VFX when charge becomes ready
	if is_charged and not was_charged:
		_on_charge_ready()

## =============================================================================
## CHARGE LOGIC
## =============================================================================

func _on_charge_ready() -> void:
	print("ChargeAbility: %s charge ready! (traveled %.1f / %.1f)" % [owner_unit.name, distance_traveled_since_attack, charge_threshold])

	# Spawn "charge ready" VFX
	if not charge_ready_vfx.is_empty():
		if charge_ready_vfx_node:
			charge_ready_vfx_node.queue_free()
		charge_ready_vfx_node = _spawn_vfx(charge_ready_vfx, owner_unit.global_position, owner_unit)

	ability_triggered.emit({"event": "charge_ready", "distance": distance_traveled_since_attack})

func _on_owner_attacked(target: Unit3D) -> void:
	if not is_active or not target:
		return

	# Apply charge bonus if charged
	if is_charged:
		_apply_charge_damage(target)
		print("ChargeAbility: %s executed CHARGED attack on %s!" % [owner_unit.name, target.name])

	# Reset charge (either after charged attack or any attack)
	if is_charged or reset_on_any_attack:
		_reset_charge()

func _apply_charge_damage(target: Unit3D) -> void:
	var bonus_damage = damage_bonus

	# Calculate final damage based on bonus type
	match bonus_type:
		BonusType.FLAT:
			# Direct damage addition
			_apply_damage(target, bonus_damage, "physical")

		BonusType.PERCENTAGE:
			# Percentage of base attack damage
			var base_damage = owner_unit.attack_damage if owner_unit else 0
			var percentage_bonus = base_damage * (bonus_damage / 100.0)
			_apply_damage(target, percentage_bonus, "physical")

	# Play charge impact VFX
	if not charge_impact_vfx.is_empty():
		_spawn_vfx(charge_impact_vfx, target.global_position)

	ability_triggered.emit({
		"event": "charged_attack",
		"target": target.name,
		"bonus_damage": bonus_damage,
		"distance_traveled": distance_traveled_since_attack
	})

func _reset_charge() -> void:
	distance_traveled_since_attack = 0.0
	is_charged = false

	# Remove charge ready VFX
	if charge_ready_vfx_node and is_instance_valid(charge_ready_vfx_node):
		charge_ready_vfx_node.queue_free()
		charge_ready_vfx_node = null

## =============================================================================
## DEBUG
## =============================================================================

## Get current charge progress (0.0 to 1.0)
func get_charge_progress() -> float:
	return min(distance_traveled_since_attack / charge_threshold, 1.0) if charge_threshold > 0 else 0.0
