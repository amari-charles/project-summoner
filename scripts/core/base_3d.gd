extends StaticBody3D
class_name Base3D

## 3D Base structure that units attack
## Each team has one base - destroying it wins the game

enum Team { PLAYER, ENEMY }

## Hit feedback animation constants
const BASE_FLASH_DURATION: float = 0.3  # Duration at low attack intensity
const MIN_FLASH_DURATION: float = 0.05  # Minimum duration at high intensity
const FLASH_SPEED_MULTIPLIER: float = 0.3  # How much each hit speeds up animation
const RECENT_HITS_DECAY_RATE: float = 2.0  # Hits per second decay rate

@export var max_hp: float = 300.0
@export var team: Team = Team.PLAYER

var current_hp: float
var is_alive: bool = true

## Attack intensity tracking
var recent_hits: float = 0.0  # Tracks recent attack pressure for dynamic feedback

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

func _process(delta: float) -> void:
	# Decay recent hits counter over time (returns to normal animation speed)
	if recent_hits > 0:
		recent_hits -= RECENT_HITS_DECAY_RATE * delta
		recent_hits = max(recent_hits, 0.0)

## Take damage from units
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	# Track attack intensity for dynamic feedback
	recent_hits += 1.0

	# Play hit feedback animation (flash + shake, speed scales with intensity)
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
## Speed scales dynamically with attack intensity
func _play_hit_feedback() -> void:
	if not visual:
		return

	# Calculate duration based on attack intensity
	# More hits = faster animation (communicates danger level)
	var intensity_factor = 1.0 + (recent_hits * FLASH_SPEED_MULTIPLIER)
	var flash_duration = max(MIN_FLASH_DURATION, BASE_FLASH_DURATION / intensity_factor)

	# Scale all timings proportionally
	var flash_to_white = flash_duration * 0.4  # 40% of time flashing white
	var flash_return = flash_duration * 0.6    # 60% of time returning to original
	var shake_out = flash_duration * 0.35      # Shake out timing
	var shake_return = flash_duration * 0.25   # Shake return timing

	var tween = create_tween()
	tween.set_parallel(true)  # Run flash and shake simultaneously

	# Flash effect (modulate property)
	tween.tween_property(visual, "modulate", Color.WHITE, flash_to_white)
	tween.chain().tween_property(visual, "modulate", original_color, flash_return)

	# Shake effect (position offset)
	var original_pos = visual.position
	var shake_offset = Vector3(randf_range(-0.15, 0.15), randf_range(-0.15, 0.15), 0)
	tween.tween_property(visual, "position", original_pos + shake_offset, shake_out)
	tween.chain().tween_property(visual, "position", original_pos, shake_return)
