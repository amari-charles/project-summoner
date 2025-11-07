extends Node

## Centralized damage calculation and combat event system
## Usage: DamageSystem.apply_damage(attacker, defender, base_damage, "physical")
## Autoload as: /root/DamageSystem

## Signals for combat events
signal damage_dealt(event: CombatEvent)
signal damage_taken(event: CombatEvent)
signal unit_killed(event: CombatEvent)
signal unit_died(event: CombatEvent)
signal unit_healed(event: CombatEvent)
signal attack_started(event: CombatEvent)
signal attack_completed(event: CombatEvent)
signal spell_cast(event: CombatEvent)

## Damage type multipliers (can be expanded for elemental system)
const DAMAGE_TYPES = {
	"physical": 1.0,
	"magical": 1.0,
	"true": 1.0,  # Ignores armor
	"fire": 1.0,
	"ice": 1.0,
	"poison": 1.0
}

## Critical hit settings
const CRIT_CHANCE: float = 0.15  # 15% base crit chance
const CRIT_MULTIPLIER: float = 2.0  # 2x damage on crit

func _ready() -> void:
	print("DamageSystem: Initialized")

## Apply damage from attacker to target
## Returns the actual damage dealt (after modifiers, armor, etc.)
func apply_damage(
	attacker: Node3D,
	target: Node3D,
	base_damage: float,
	damage_type: String = "physical",
	flags: Dictionary = {}
) -> float:
	if not target or not target.has_method("take_damage"):
		push_warning("DamageSystem: Target cannot take damage")
		return 0.0

	# Calculate final damage
	var final_damage = base_damage
	var is_crit = false

	# Check for critical hit (unless forced by flags)
	if flags.get("force_crit", false):
		is_crit = true
	elif not flags.get("cannot_crit", false):
		is_crit = randf() < CRIT_CHANCE

	# Apply crit multiplier
	if is_crit:
		final_damage *= CRIT_MULTIPLIER

	# Apply damage type multiplier
	if DAMAGE_TYPES.has(damage_type):
		final_damage *= DAMAGE_TYPES[damage_type]

	# Apply custom multiplier from flags
	if flags.has("damage_multiplier"):
		final_damage *= flags.damage_multiplier

	# Round to avoid floating point issues
	final_damage = round(final_damage * 10) / 10.0

	# Store target's HP before damage
	var target_hp_before = 0.0
	if "current_hp" in target:
		target_hp_before = target.current_hp

	# Apply damage to target
	target.take_damage(final_damage)

	# Create metadata for event
	var metadata = {
		"is_crit": is_crit,
		"base_damage": base_damage,
		"final_damage": final_damage,
		"hp_before": target_hp_before
	}
	metadata.merge(flags)  # Include any custom flags

	# Emit DAMAGE_DEALT event (from attacker's perspective)
	var dealt_event = CombatEvent.new(
		CombatEvent.EventType.DAMAGE_DEALT,
		attacker,
		target,
		final_damage,
		damage_type,
		metadata
	)
	damage_dealt.emit(dealt_event)

	# Emit DAMAGE_TAKEN event (from target's perspective)
	var taken_event = CombatEvent.new(
		CombatEvent.EventType.DAMAGE_TAKEN,
		attacker,
		target,
		final_damage,
		damage_type,
		metadata
	)
	damage_taken.emit(taken_event)

	# Check if target died
	var target_died = false
	if "is_alive" in target:
		target_died = not target.is_alive
	elif "current_hp" in target:
		target_died = target.current_hp <= 0

	if target_died:
		# Emit UNIT_KILLED event (from attacker's perspective)
		var killed_event = CombatEvent.new(
			CombatEvent.EventType.UNIT_KILLED,
			attacker,
			target,
			final_damage,
			damage_type,
			metadata
		)
		unit_killed.emit(killed_event)

		# Emit UNIT_DIED event (from target's perspective)
		var died_event = CombatEvent.new(
			CombatEvent.EventType.UNIT_DIED,
			attacker,
			target,
			final_damage,
			damage_type,
			metadata
		)
		unit_died.emit(died_event)

	return final_damage

## Apply healing to target
## Returns the actual amount healed
func apply_healing(
	healer: Node3D,
	target: Node3D,
	heal_amount: float,
	flags: Dictionary = {}
) -> float:
	if not target or not ("current_hp" in target) or not ("max_hp" in target):
		push_warning("DamageSystem: Target cannot be healed")
		return 0.0

	# Calculate final heal amount
	var final_heal = heal_amount

	# Apply custom multiplier from flags
	if flags.has("heal_multiplier"):
		final_heal *= flags.heal_multiplier

	# Store HP before healing
	var hp_before = target.current_hp

	# Apply healing (clamp to max_hp)
	var new_hp = min(target.current_hp + final_heal, target.max_hp)
	var actual_heal = new_hp - target.current_hp
	target.current_hp = new_hp

	# Create metadata
	var metadata = {
		"requested_heal": heal_amount,
		"final_heal": final_heal,
		"actual_heal": actual_heal,
		"hp_before": hp_before,
		"hp_after": new_hp,
		"overheal": final_heal - actual_heal
	}
	metadata.merge(flags)

	# Emit UNIT_HEALED event
	var healed_event = CombatEvent.new(
		CombatEvent.EventType.UNIT_HEALED,
		healer,
		target,
		actual_heal,
		"healing",
		metadata
	)
	unit_healed.emit(healed_event)

	return actual_heal

## Emit attack started event (for animation/VFX timing)
func emit_attack_started(attacker: Node3D, target: Node3D, metadata: Dictionary = {}) -> void:
	var event = CombatEvent.new(
		CombatEvent.EventType.ATTACK_STARTED,
		attacker,
		target,
		0.0,
		"",
		metadata
	)
	attack_started.emit(event)

## Emit attack completed event (for animation/VFX timing)
func emit_attack_completed(attacker: Node3D, target: Node3D, metadata: Dictionary = {}) -> void:
	var event = CombatEvent.new(
		CombatEvent.EventType.ATTACK_COMPLETED,
		attacker,
		target,
		0.0,
		"",
		metadata
	)
	attack_completed.emit(event)

## Emit spell cast event
func emit_spell_cast(caster: Node3D, spell_id: String, metadata: Dictionary = {}) -> void:
	metadata["spell_id"] = spell_id
	var event = CombatEvent.new(
		CombatEvent.EventType.SPELL_CAST,
		caster,
		null,
		0.0,
		"",
		metadata
	)
	spell_cast.emit(event)

## Calculate damage with preview (doesn't apply, just calculates)
## Useful for damage prediction UI
func preview_damage(
	attacker: Node3D,
	target: Node3D,
	base_damage: float,
	damage_type: String = "physical",
	assume_crit: bool = false
) -> Dictionary:
	var final_damage = base_damage

	# Apply crit if assumed
	if assume_crit:
		final_damage *= CRIT_MULTIPLIER

	# Apply damage type multiplier
	if DAMAGE_TYPES.has(damage_type):
		final_damage *= DAMAGE_TYPES[damage_type]

	final_damage = round(final_damage * 10) / 10.0

	return {
		"base_damage": base_damage,
		"final_damage": final_damage,
		"is_crit": assume_crit,
		"damage_type": damage_type,
		"will_kill": ("current_hp" in target) and target.current_hp <= final_damage
	}
