extends Character2D5Component
class_name SkeletalCharacter2D5Component

## Skeletal-based 2.5D Character Rendering Component
## Renders skeletal 2D animations in 3D space using Skeleton2D/AnimationPlayer + SubViewport

## Sprite anchor point in viewport (0-1 range)
## X: 0=left, 0.5=center, 1=right
## Y: 0=top, 0.5=center, 1=bottom (feet on ground)
@export var sprite_anchor: Vector2 = Vector2(0.5, 1.0)

@export var skeletal_scene: PackedScene = null  ## The skeletal animation scene to instance
@export var scale_factor: Vector2 = Vector2(0.08, 0.08)  ## Scale of the skeletal model
@export var position_offset: Vector2 = Vector2(0, 0)  ## DEPRECATED: Use sprite_anchor instead

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var model_container: Node2D = $Sprite3D/SubViewport/ModelContainer

var animation_player: AnimationPlayer = null
var skeletal_instance: Node2D = null

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Instance the skeletal scene if provided
	if skeletal_scene:
		_instance_skeletal_scene()

	# Position model based on anchor
	_update_model_position()

## Instance the skeletal animation scene into the viewport
func _instance_skeletal_scene() -> void:
	if not skeletal_scene:
		push_error("Skeletal2D5Component: No skeletal_scene provided")
		return

	skeletal_instance = skeletal_scene.instantiate()
	if not skeletal_instance:
		push_error("Skeletal2D5Component: Failed to instance skeletal scene")
		return

	# Apply scale
	skeletal_instance.scale = scale_factor

	# Position will be set by _update_model_position()

	# Add to viewport
	model_container.add_child(skeletal_instance)

	# Find the AnimationPlayer in the skeletal scene
	animation_player = _find_animation_player(skeletal_instance)

	if animation_player:
		print("Skeletal2D5Component: Found AnimationPlayer with animations: %s" % animation_player.get_animation_list())
	else:
		push_warning("Skeletal2D5Component: No AnimationPlayer found in skeletal scene")

	# Connect animation event signals (e.g., attack_impact)
	if skeletal_instance.has_signal("attack_impact"):
		skeletal_instance.attack_impact.connect(_on_attack_impact)

## Recursively find AnimationPlayer in node tree
func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node

	for child in node.get_children():
		var result = _find_animation_player(child)
		if result:
			return result

	return null

## Play an animation
func play_animation(anim_name: String, auto_play: bool = false) -> void:
	if not animation_player:
		return

	# Map animation names (unit_3d uses these names)
	var mapped_name = anim_name
	match anim_name:
		"walk":
			mapped_name = "idle"  # Use idle for walking (no walk animation yet)
		"hurt":
			mapped_name = "idle"  # Use idle for hurt (no hurt animation yet)
		"death":
			mapped_name = "idle"  # Use idle for death (no death animation yet)

	# Only play if the animation exists
	if animation_player.has_animation(mapped_name):
		animation_player.play(mapped_name)
	else:
		push_warning("Skeletal2D5Component: Animation '%s' not found, available: %s" % [mapped_name, animation_player.get_animation_list()])

## Stop current animation
func stop_animation() -> void:
	if animation_player:
		animation_player.stop()

## Get current animation name
func get_current_animation() -> String:
	if animation_player:
		return animation_player.current_animation
	return ""

## Check if animation is playing
func is_playing() -> bool:
	if animation_player:
		return animation_player.is_playing()
	return false

## Flip the sprite horizontally (for enemy units)
func set_flip_h(flip: bool) -> void:
	if skeletal_instance:
		skeletal_instance.scale.x = abs(skeletal_instance.scale.x) * (-1 if flip else 1)

## Get the duration of an animation in seconds
func get_animation_duration(anim_name: String) -> float:
	if not animation_player:
		return 1.0  # Fallback duration

	# Map animation names (same mapping as play_animation)
	var mapped_name = anim_name
	match anim_name:
		"walk":
			mapped_name = "idle"
		"hurt":
			mapped_name = "idle"
		"death":
			mapped_name = "idle"

	# Get animation duration from AnimationPlayer
	if animation_player.has_animation(mapped_name):
		var animation = animation_player.get_animation(mapped_name)
		return animation.length

	return 1.0  # Fallback duration

## Animation event handler - called when attack animation fires impact event
func _on_attack_impact() -> void:
	# Forward to parent Unit3D
	var unit = get_parent()
	if unit and unit.has_method("_on_attack_impact"):
		unit._on_attack_impact()

## Position the 2D skeletal model within viewport based on anchor point
## This ensures character feet align with the bottom edge when anchor.y = 1.0
func _update_model_position() -> void:
	if not model_container or not viewport:
		return

	# Get viewport size
	var viewport_size = viewport.size

	# Calculate position based on anchor
	# anchor (0.5, 1.0) means center-bottom
	var target_pos = Vector2(
		viewport_size.x * sprite_anchor.x,
		viewport_size.y * sprite_anchor.y
	)

	# Apply position_offset for backwards compatibility if needed
	if position_offset != Vector2.ZERO:
		push_warning("SkeletalCharacter2D5Component: position_offset is deprecated, use sprite_anchor instead")
		target_pos += position_offset

	model_container.position = target_pos
	print("SkeletalCharacter2D5: Positioned model at %v (viewport: %v, anchor: %v)" % [target_pos, viewport_size, sprite_anchor])
