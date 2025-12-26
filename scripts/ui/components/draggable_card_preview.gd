extends Control
class_name DraggableCardPreview

## A drag preview with momentum-based swinging physics.
## The card rotates based on horizontal mouse velocity and has spring physics.

## Physics constants - Rotation (spring-damper system for realistic swinging)
## Higher SPRING_STIFFNESS = snappier return to center, higher DAMPING = less oscillation
const ROTATION_SENSITIVITY: float = 0.8  ## How much velocity affects target rotation angle
const MAX_ROTATION_DEG: float = 35.0  ## Maximum rotation in degrees (prevents extreme tilting)
const SPRING_STIFFNESS: float = 18.0  ## Spring force pulling rotation back to center
const DAMPING: float = 4.0  ## Friction that slows oscillation (lower = more swing)

## Physics constants - Scale/stretch effects
## Creates subtle "depth" illusion: faster movement = card appears further from camera
## Stretch adds squash-and-stretch animation principle for organic feel
const VELOCITY_SMOOTHING: float = 0.3  ## Lerp factor to smooth velocity (prevents jitter)
const SCALE_SPEED_FACTOR: float = 0.00008  ## Speed-to-scale multiplier (tuned for px/sec)
const MAX_SCALE_REDUCTION: float = 0.08  ## Cap at 8% shrink to keep card visible
const STRETCH_X_FACTOR: float = 0.00005  ## Horizontal squash sensitivity
const MAX_STRETCH_X: float = 0.05  ## Cap horizontal compression at 5%
const STRETCH_Y_FACTOR: float = 0.00003  ## Vertical stretch sensitivity
const MAX_STRETCH_Y: float = 0.03  ## Cap vertical stretch at 3%

## State
var _last_mouse_pos: Vector2 = Vector2.ZERO
var _mouse_velocity: Vector2 = Vector2.ZERO
var _current_rotation: float = 0.0
var _rotation_velocity: float = 0.0
var _card_widget: CardWidget = null


func _ready() -> void:
	_last_mouse_pos = get_global_mouse_position()


func _process(delta: float) -> void:
	# Calculate mouse velocity with smoothing
	var current_mouse_pos: Vector2 = get_global_mouse_position()
	var instant_velocity: Vector2 = (current_mouse_pos - _last_mouse_pos) / max(delta, 0.001)
	_mouse_velocity = _mouse_velocity.lerp(instant_velocity, VELOCITY_SMOOTHING)
	_last_mouse_pos = current_mouse_pos

	# Apply spring physics for rotation
	# Target rotation based on horizontal velocity (negative = tilt opposite to movement)
	var target_rotation: float = clampf(
		-_mouse_velocity.x * ROTATION_SENSITIVITY * 0.001,
		deg_to_rad(-MAX_ROTATION_DEG),
		deg_to_rad(MAX_ROTATION_DEG)
	)

	# Spring force towards target
	var spring_force: float = (target_rotation - _current_rotation) * SPRING_STIFFNESS
	# Damping force
	var damping_force: float = -_rotation_velocity * DAMPING

	# Update rotation velocity and position
	_rotation_velocity += (spring_force + damping_force) * delta
	_current_rotation += _rotation_velocity * delta

	# Apply rotation
	rotation = _current_rotation

	# Scale based on movement speed (faster = slightly smaller, like depth)
	var speed: float = _mouse_velocity.length()
	var scale_factor: float = 1.0 - clampf(speed * SCALE_SPEED_FACTOR, 0.0, MAX_SCALE_REDUCTION)

	# Add stretch when moving fast (compression in direction of movement)
	var stretch_x: float = 1.0 - clampf(abs(_mouse_velocity.x) * STRETCH_X_FACTOR, 0.0, MAX_STRETCH_X)
	var stretch_y: float = 1.0 + clampf(abs(_mouse_velocity.y) * STRETCH_Y_FACTOR, 0.0, MAX_STRETCH_Y)

	scale = Vector2(scale_factor * stretch_x, scale_factor * stretch_y)


## Set up the preview with card data
## grab_offset: where on the card the user clicked (for natural rotation pivot)
func setup(card_data: Dictionary, catalog_data: Dictionary, card_size: Vector2, grab_offset: Vector2 = Vector2.ZERO) -> void:
	# Create the actual card widget as a child
	var CardWidgetScene: PackedScene = preload("res://scenes/ui/components/card_widget.tscn")
	_card_widget = CardWidgetScene.instantiate()
	_card_widget.custom_minimum_size = card_size
	_card_widget.size = card_size
	add_child(_card_widget)

	# Set card data
	_card_widget.set_card(card_data, catalog_data)
	_card_widget.set_draggable(false)

	# Make slightly transparent
	_card_widget.modulate = Color(1, 1, 1, 0.9)

	# Offset the card so it appears held where clicked
	# (Godot positions drag preview origin at mouse cursor)
	_card_widget.position = -grab_offset

	# Set pivot to the grab point (which is now at origin after offset)
	pivot_offset = Vector2.ZERO
