extends Control
class_name DraggableCardPreview

## A drag preview with momentum-based swinging physics.
## The card rotates based on horizontal mouse velocity and has spring physics.

## Physics constants
const ROTATION_SENSITIVITY: float = 0.8  ## How much velocity affects rotation
const MAX_ROTATION_DEG: float = 35.0  ## Maximum rotation in degrees
const SPRING_STIFFNESS: float = 18.0  ## How quickly rotation returns to center
const DAMPING: float = 4.0  ## How quickly oscillation dies down (lower = more swing)

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
	_mouse_velocity = _mouse_velocity.lerp(instant_velocity, 0.3)  # Smooth velocity
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
	var scale_factor: float = 1.0 - clampf(speed * 0.00008, 0.0, 0.08)

	# Add vertical stretch when moving fast
	var stretch_x: float = 1.0 - clampf(abs(_mouse_velocity.x) * 0.00005, 0.0, 0.05)
	var stretch_y: float = 1.0 + clampf(abs(_mouse_velocity.y) * 0.00003, 0.0, 0.03)

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
