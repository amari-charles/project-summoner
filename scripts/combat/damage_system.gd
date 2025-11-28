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
const DAMAGE_TYPES: Dictionary = {
	"physical": 1.0 as float,
	"magical": 1.0 as float,
	"true": 1.0 as float,  # Ignores armor
	"fire": 1.0 as float,
	"ice": 1.0 as float,
	"poison": 1.0 as float
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
	var final_damage: float = base_damage
	var is_crit: bool = false

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

	# Apply player hero damage bonuses if attacker is on player team
	if _is_player_team(attacker):
		final_damage = _apply_hero_damage_bonuses(final_damage, damage_type)

	# Apply player hero damage reduction if target is on player team
	if _is_player_team(target):
		final_damage = _apply_hero_damage_reduction(final_damage)

	# Round to avoid floating point issues
	final_damage = round(final_damage * 10) / 10.0

	# Store target's HP before damage
	var target_hp_before: float = 0.0
	if "current_hp" in target:
		target_hp_before = target.get("current_hp")

	# Apply damage to target
	target.call("take_damage", final_damage)

	# Create metadata for event
	var metadata: Dictionary = {
		"is_crit": is_crit,
		"base_damage": base_damage,
		"final_damage": final_damage,
		"hp_before": target_hp_before
	}
	metadata.merge(flags)  # Include any custom flags

	# Emit DAMAGE_DEALT event (from attacker's perspective)
	var dealt_event: CombatEvent = CombatEvent.new(
		CombatEvent.EventType.DAMAGE_DEALT,
		attacker,
		target,
		final_damage,
		damage_type,
		metadata
	)
	damage_dealt.emit(dealt_event)

	# Emit DAMAGE_TAKEN event (from target's perspective)
	var taken_event: CombatEvent = CombatEvent.new(
		CombatEvent.EventType.DAMAGE_TAKEN,
		attacker,
		target,
		final_damage,
		damage_type,
		metadata
	)
	damage_taken.emit(taken_event)

	# Check if target died
	var target_died: bool = false
	if "is_alive" in target:
		target_died = not target.get("is_alive")
	elif "current_hp" in target:
		target_died = target.get("current_hp") <= 0

	if target_died:
		# Emit UNIT_KILLED event (from attacker's perspective)
		var killed_event: CombatEvent = CombatEvent.new(
			CombatEvent.EventType.UNIT_KILLED,
			attacker,
			target,
			final_damage,
			damage_type,
			metadata
		)
		unit_killed.emit(killed_event)

		# Emit UNIT_DIED event (from target's perspective)
		var died_event: CombatEvent = CombatEvent.new(
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
	var final_heal: float = heal_amount

	# Apply custom multiplier from flags
	if flags.has("heal_multiplier"):
		final_heal *= flags.heal_multiplier

	# Store HP before healing
	var hp_before: float = target.get("current_hp")

	# Apply healing (clamp to max_hp)
	var new_hp: float = min(target.get("current_hp") + final_heal, target.get("max_hp"))
	var actual_heal: float = new_hp - target.get("current_hp")
	target.set("current_hp", new_hp)

	# Create metadata
	var metadata: Dictionary = {
		"requested_heal": heal_amount,
		"final_heal": final_heal,
		"actual_heal": actual_heal,
		"hp_before": hp_before,
		"hp_after": new_hp,
		"overheal": final_heal - actual_heal
	}
	metadata.merge(flags)

	# Emit UNIT_HEALED event
	var healed_event: CombatEvent = CombatEvent.new(
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
	var event: CombatEvent = CombatEvent.new(
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
	var event: CombatEvent = CombatEvent.new(
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
	var event: CombatEvent = CombatEvent.new(
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
	_attacker: Node3D,
	target: Node3D,
	base_damage: float,
	damage_type: String = "physical",
	assume_crit: bool = false
) -> Dictionary:
	var final_damage: float = base_damage

	# Apply crit if assumed
	if assume_crit:
		final_damage *= CRIT_MULTIPLIER

	# Apply damage type multiplier
	if DAMAGE_TYPES.has(damage_type):
		final_damage *= DAMAGE_TYPES[damage_type]

	final_damage = round(final_damage * 10) / 10.0

	var result: Dictionary = {
		"base_damage": base_damage,
		"final_damage": final_damage,
		"is_crit": assume_crit,
		"damage_type": damage_type,
		"will_kill": ("current_hp" in target) and target.get("current_hp") <= final_damage
	}
	return result

## =============================================================================
## HERO TRAIT INTEGRATION
## =============================================================================

## Check if a node is on the player team
func _is_player_team(node: Node3D) -> bool:
	if not node:
		return false
	# Check for team property (Unit3D, Unit, Summoner all have this)
	if "team" in node:
		var team_value: Variant = node.get("team")
		# Use Unit.Team enum constant instead of magic number
		return team_value == Unit.Team.PLAYER
	return false

## Apply hero damage bonuses from traits
func _apply_hero_damage_bonuses(damage: float, damage_type: String) -> float:
	var hero_stats: Dictionary = BattleContext.get_player_hero_stats()
	if hero_stats.is_empty():
		# In campaign mode, empty hero stats indicates a timing/initialization bug
		if BattleContext.current_mode == BattleContext.BattleMode.CAMPAIGN:
			push_warning("DamageSystem: No hero stats cached in campaign mode - trait bonuses not applied")
		return damage

	var modified_damage: float = damage

	# Apply general damage bonus (applies to all damage)
	var damage_bonus: float = hero_stats.get("damage_bonus", 0.0)
	if damage_bonus > 0.0:
		modified_damage *= (1.0 + damage_bonus / 100.0)

	# Apply elemental damage bonus based on damage type
	var elemental_bonus_key: String = damage_type + "_damage_bonus"
	var elemental_bonus: float = hero_stats.get(elemental_bonus_key, 0.0)
	if elemental_bonus > 0.0:
		modified_damage *= (1.0 + elemental_bonus / 100.0)

	return modified_damage

## Apply hero damage reduction from traits
func _apply_hero_damage_reduction(damage: float) -> float:
	var hero_stats: Dictionary = BattleContext.get_player_hero_stats()
	if hero_stats.is_empty():
		# In campaign mode, empty hero stats indicates a timing/initialization bug
		if BattleContext.current_mode == BattleContext.BattleMode.CAMPAIGN:
			push_warning("DamageSystem: No hero stats cached in campaign mode - trait reduction not applied")
		return damage

	# Apply flat damage reduction
	var damage_reduction: float = hero_stats.get("damage_reduction", 0.0)
	if damage_reduction > 0.0:
		damage = maxf(damage - damage_reduction, 0.0)

	return damage
