extends RefCounted
class_name CombatEvent

## Data class for combat events
## Used for effects that trigger on combat actions
## Passed to listeners via signals

enum EventType {
	DAMAGE_DEALT,      ## Attacker dealt damage to target
	DAMAGE_TAKEN,      ## Target took damage from attacker
	UNIT_KILLED,       ## Attacker killed target
	UNIT_DIED,         ## Target died
	ATTACK_STARTED,    ## Unit began attack animation
	ATTACK_COMPLETED,  ## Unit finished attack
	SPELL_CAST,        ## Spell was cast
	UNIT_HEALED,       ## Unit was healed
	UNIT_SPAWNED       ## Unit was spawned
}

var event_type: EventType
var source: Node3D  ## Who triggered the event (attacker, caster, healer)
var target: Node3D  ## Who was affected (defender, victim, heal target)
var value: float  ## Numeric value (damage amount, heal amount, etc.)
var damage_type: String = "physical"  ## Type of damage/effect
var metadata: Dictionary = {}  ## Additional data (is_crit, buff_id, etc.)

func _init(
	p_event_type: EventType,
	p_source: Node3D = null,
	p_target: Node3D = null,
	p_value: float = 0.0,
	p_damage_type: String = "physical",
	p_metadata: Dictionary = {}
) -> void:
	event_type = p_event_type
	source = p_source
	target = p_target
	value = p_value
	damage_type = p_damage_type
	metadata = p_metadata

## Helper: Check if this event is a critical hit
func is_critical() -> bool:
	return metadata.get("is_crit", false)

## Helper: Get source team (if available)
func get_source_team() -> int:
	if source and "team" in source:
		return (source as Node3D).get("team")
	return -1

## Helper: Get target team (if available)
func get_target_team() -> int:
	if target and "team" in target:
		return (target as Node3D).get("team")
	return -1
