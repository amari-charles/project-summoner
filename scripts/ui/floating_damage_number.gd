extends Node3D
class_name FloatingDamageNumber

## Floating damage number that rises and fades out
## Managed by DamageNumberManager for pooling

## Animation settings
@export var rise_speed: float = 1.5  ## Units per second
@export var rise_distance: float = 1.0  ## Total distance to rise
@export var fade_duration: float = 0.8  ## Seconds to fade out
@export var drift_amount: float = 0.3  ## Random horizontal drift

## State
var damage_value: float = 0.0
var is_crit: bool = false
var damage_type: String = "physical"
var is_pooled: bool = false
var lifetime: float = 0.0

## Visual components
var damage_label: Label3D = null

## Animation
var start_position: Vector3 = Vector3.ZERO
var drift_offset: Vector3 = Vector3.ZERO

signal number_finished()  ## Emitted when animation completes

func _ready() -> void:
	_create_label3d()

func _process(delta: float) -> void:
	lifetime += delta

	# Rise up with drift
	var progress = lifetime / fade_duration
	var rise = rise_distance * progress
	global_position = start_position + Vector3(0, rise, 0) + drift_offset * progress

	# Fade out
	if damage_label:
		damage_label.modulate.a = 1.0 - progress

	# Cleanup when done
	if lifetime >= fade_duration:
		number_finished.emit()
		set_process(false)

func _create_label3d() -> void:
	# Create Label3D for damage number
	damage_label = Label3D.new()

	# Billboard mode - always face camera
	damage_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED

	# Render on top of everything
	damage_label.no_depth_test = true

	# Text appearance
	damage_label.font_size = 32
	damage_label.outline_size = 4
	damage_label.outline_modulate = Color.BLACK

	# Ensure visible on all layers
	damage_label.layers = 0xFFFFF

	add_child(damage_label)

func show_damage(value: float, position: Vector3, is_critical: bool = false, dmg_type: String = "physical") -> void:
	damage_value = value
	is_crit = is_critical
	damage_type = dmg_type

	# Set starting position with random drift
	start_position = position
	drift_offset = Vector3(
		randf_range(-drift_amount, drift_amount),
		0,
		randf_range(-drift_amount * 0.5, drift_amount * 0.5)
	)
	global_position = start_position

	# Reset state
	lifetime = 0.0
	set_process(true)
	visible = true

	# Update label text and appearance
	if damage_label:
		# Set text
		var text = str(int(damage_value))
		if is_crit:
			text += "!"

		damage_label.text = text

		# Set color based on type/crit
		damage_label.modulate = _get_damage_color()

		# Larger font for crits
		damage_label.font_size = 48 if is_crit else 32


func _get_damage_color() -> Color:
	if is_crit:
		return Color(1.0, 0.8, 0.0)  # Gold for crits

	match damage_type:
		"physical":
			return Color.WHITE
		"magical", "spell":
			return Color(0.5, 0.8, 1.0)  # Light blue
		"fire":
			return Color(1.0, 0.5, 0.0)  # Orange
		_:
			return Color.WHITE

func reset() -> void:
	damage_value = 0.0
	is_crit = false
	damage_type = "physical"
	lifetime = 0.0
	set_process(false)
	visible = false

	if damage_label:
		damage_label.modulate.a = 1.0
		damage_label.text = ""
