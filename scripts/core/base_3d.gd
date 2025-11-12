extends StaticBody3D
class_name Base3D

## 3D Base structure that units attack
## Each team has one base - destroying it wins the game

enum Team { PLAYER, ENEMY }

@export var max_hp: float = 300.0
@export var team: Team = Team.PLAYER

var current_hp: float
var is_alive: bool = true

## Visual components
@onready var visual: Sprite3D = $Visual if has_node("Visual") else null
var original_color: Color = Color.WHITE

## Signals
signal base_destroyed(base: Base3D)
signal base_damaged(base: Base3D, damage: float)
signal hp_changed(new_hp: float, new_max_hp: float)

func _ready() -> void:
	current_hp = max_hp

	# Add to groups
	add_to_group("bases")
	if team == Team.PLAYER:
		add_to_group("player_base")
	else:
		add_to_group("enemy_base")

	# Store original color for hit feedback
	if visual:
		original_color = visual.modulate

	# Create HP bar for base (larger and higher than units)
	HPBarManager.create_bar_for_unit(self, {
		"bar_width": 1.5,  # Wider than unit bars (1.5 vs 0.8)
		"offset_y": 2.5,   # Higher above base
		"show_on_damage_only": false  # Always visible
	})

	print("Base3D ready: Team %d, HP %d" % [team, max_hp])

## Take damage from units
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	# Play hit feedback animation (flash + shake)
	_play_hit_feedback()

	current_hp -= damage
	current_hp = max(current_hp, 0.0)

	# Emit signals for HP bar and damage feedback
	hp_changed.emit(current_hp, max_hp)
	base_damaged.emit(self, damage)

	if current_hp <= 0:
		_destroy()

## Destroy the base
func _destroy() -> void:
	is_alive = false

	# Remove HP bar
	HPBarManager.remove_bar_from_unit(self)

	base_destroyed.emit(self)
	print("Base3D destroyed! Team: ", team)

## Play hit feedback animation (2D standard: flash + shake)
func _play_hit_feedback() -> void:
	if not visual:
		return

	# Flash to white (0.15s) then back to original (0.2s)
	# Standard 2D game pattern for damage feedback
	var tween = create_tween()
	tween.set_parallel(true)  # Run flash and shake simultaneously

	# Flash effect (modulate property)
	tween.tween_property(visual, "modulate", Color.WHITE, 0.15)
	tween.chain().tween_property(visual, "modulate", original_color, 0.2)

	# Shake effect (position offset)
	var original_pos = visual.position
	var shake_offset = Vector3(randf_range(-0.15, 0.15), randf_range(-0.15, 0.15), 0)
	tween.tween_property(visual, "position", original_pos + shake_offset, 0.08)
	tween.chain().tween_property(visual, "position", original_pos, 0.07)
