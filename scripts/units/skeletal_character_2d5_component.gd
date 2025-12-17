extends Character2D5Component
class_name SkeletalCharacter2D5Component

## Skeletal-based 2.5D Character Rendering Component
## Renders skeletal 2D animations in 3D space using Node2D pivots/AnimationPlayer + SubViewport
## Viewport is automatically sized to fit the character content.

@export var skeletal_scene: PackedScene = null  ## The skeletal animation scene to instance
@export var scale_factor: Vector2 = Vector2(0.1, 0.1)  ## Scale of the skeletal model in 3D world
@export var viewport_padding: float = 200.0  ## Padding around character in viewport (extra room for animation movement)
@export var hp_bar_offset_x: float = 0.0  ## Horizontal offset for HP bar in world units (negative = left, positive = right)

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var model_container: Node2D = $Sprite3D/SubViewport/ModelContainer

var animation_player: AnimationPlayer = null
var skeletal_instance: Node2D = null
var _cached_bounds: Rect2 = Rect2()
var _is_flipped: bool = false  ## Track flip state for race condition handling
var _initialization_complete: bool = false  ## Track if bounds have been calculated

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Instance the skeletal scene if provided
	if skeletal_scene:
		await _instance_skeletal_scene()

	# Setup sprite alignment after bounds are calculated
	_setup_sprite_alignment()

	# Randomize animation start position so multiple units don't sync
	_randomize_animation_phase()

## Instance the skeletal animation scene into the viewport
func _instance_skeletal_scene() -> void:
	if not skeletal_scene:
		push_error("SkeletalChar2D5: No skeletal_scene provided")
		return

	skeletal_instance = skeletal_scene.instantiate()
	if not skeletal_instance:
		push_error("SkeletalChar2D5: Failed to instance skeletal scene")
		return

	# Add to viewport first at origin with no scale (to calculate bounds)
	skeletal_instance.position = Vector2.ZERO
	skeletal_instance.scale = Vector2.ONE
	model_container.add_child(skeletal_instance)

	# Find the AnimationPlayer in the skeletal scene
	animation_player = _find_animation_player(skeletal_instance)
	if not animation_player:
		push_warning("SkeletalChar2D5: No AnimationPlayer found in skeletal scene")

	# Connect animation event signals (e.g., attack_impact)
	if skeletal_instance.has_signal("attack_impact"):
		var attack_impact_signal: Signal = skeletal_instance.get("attack_impact")
		attack_impact_signal.connect(_on_attack_impact)

	# Wait for tree to update so we can calculate bounds
	await get_tree().process_frame

	# Calculate bounds and resize viewport dynamically
	_cached_bounds = _get_skeletal_bounds()

	if _cached_bounds.size.x > 0 and _cached_bounds.size.y > 0:
		# Resize viewport to fit content with padding
		var new_width: int = int(_cached_bounds.size.x + viewport_padding * 2)
		var new_height: int = int(_cached_bounds.size.y + viewport_padding * 2)
		viewport.size = Vector2i(new_width, new_height)

		# Position content: center horizontally, bottom-align vertically
		skeletal_instance.position.x = (new_width / 2.0) - _cached_bounds.get_center().x
		skeletal_instance.position.y = new_height - _cached_bounds.end.y - viewport_padding
	else:
		push_warning("SkeletalChar2D5: Could not calculate bounds, using default viewport size")

	# Mark initialization complete and re-apply flip state
	# This handles the race condition where set_flip_h was called during await
	_initialization_complete = true
	if _is_flipped:
		_apply_flip_position(true)

## Recursively find AnimationPlayer in node tree
func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node

	for child: Node in node.get_children():
		var result: AnimationPlayer = _find_animation_player(child)
		if result:
			return result

	return null

## Play an animation
func play_animation(anim_name: String, _auto_play: bool = false) -> void:
	if not animation_player:
		return

	# Map animation names (unit_3d uses these names)
	var mapped_name: String = anim_name
	match anim_name:
		"walk":
			mapped_name = "idle"  # Use idle for walking (no walk animation yet)
		"hurt":
			mapped_name = "idle"  # Use idle for hurt (no hurt animation yet)
		"death":
			mapped_name = "idle"  # Use idle for death (no death animation yet)

	# Only play if the animation exists
	var mapped_stringname: StringName = StringName(mapped_name)
	if animation_player.has_animation(mapped_stringname):
		animation_player.play(mapped_stringname)
	else:
		push_warning("SkeletalChar2D5: Animation '%s' not found, available: %s" % [mapped_name, animation_player.get_animation_list()])

## Stop current animation
func stop_animation() -> void:
	if animation_player:
		animation_player.stop()

## Set animation playback speed (1.0 = normal, 2.0 = double speed, 0.5 = half speed)
func set_animation_speed(speed: float) -> void:
	if animation_player:
		animation_player.speed_scale = speed

## Get current animation speed
func get_animation_speed() -> float:
	if animation_player:
		return animation_player.speed_scale
	return 1.0

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
	_is_flipped = flip  # Always track the desired state

	if not skeletal_instance:
		return

	# Apply scale flip
	skeletal_instance.scale.x = abs(skeletal_instance.scale.x) * (-1 if flip else 1)

	# Only adjust position if initialization is complete (bounds are valid)
	# If called during initialization, the flip will be re-applied after bounds are calculated
	if _initialization_complete:
		_apply_flip_position(flip)

## Apply position adjustment for flip state
## Separated from set_flip_h to handle race condition during initialization
func _apply_flip_position(flip: bool) -> void:
	if not skeletal_instance or not viewport or _cached_bounds.size.x <= 0:
		return

	var center_x: float = viewport.size.x / 2.0
	if flip:
		# When flipped, offset from center in opposite direction
		skeletal_instance.position.x = center_x + _cached_bounds.get_center().x
	else:
		# Normal: offset to center the bounds
		skeletal_instance.position.x = center_x - _cached_bounds.get_center().x

## Randomize animation phase so multiple units don't animate in sync
func _randomize_animation_phase() -> void:
	if not animation_player:
		return

	# Get current animation length
	var current_anim: String = animation_player.current_animation
	if current_anim == "":
		return

	var anim: Animation = animation_player.get_animation(current_anim)
	if not anim:
		return

	# Seek to a random point in the animation
	var random_offset: float = randf() * anim.length
	animation_player.seek(random_offset, true)

## Get the duration of an animation in seconds
func get_animation_duration(_anim_name: String) -> float:
	if not animation_player:
		return 1.0  # Fallback duration

	# Map animation names (same mapping as play_animation)
	var mapped_name: String = _anim_name
	match _anim_name:
		"walk":
			mapped_name = "idle"
		"hurt":
			mapped_name = "idle"
		"death":
			mapped_name = "idle"

	# Get animation duration from AnimationPlayer
	var mapped_stringname: StringName = StringName(mapped_name)
	if animation_player.has_animation(mapped_stringname):
		var animation: Animation = animation_player.get_animation(mapped_stringname)
		return animation.length

	return 1.0  # Fallback duration

## Animation event handler - called when attack animation fires impact event
func _on_attack_impact() -> void:
	# Forward to parent Unit3D
	var unit: Node = get_parent()
	if unit and unit.has_method("_on_attack_impact"):
		unit.call("_on_attack_impact")

## Setup sprite alignment so character feet are at origin (Y=0)
func _setup_sprite_alignment() -> void:
	if not sprite_3d or not viewport:
		return

	# Calculate world height based on viewport size and scale
	var world_height: float = viewport.size.y * scale_factor.y * sprite_3d.pixel_size

	# Position Sprite3D so viewport bottom is at Y=0 (feet on ground)
	sprite_3d.position.y = world_height / 2.0

	# Apply scale to the Sprite3D to control character size in 3D world
	sprite_3d.pixel_size = 0.01 * scale_factor.y

## Get the world-space height of this sprite
## Used by HP bars, projectile spawns, etc.
func get_sprite_height() -> float:
	if not viewport or not sprite_3d:
		return 1.0

	# Use cached bounds if available
	if _cached_bounds.size.y > 0:
		return _cached_bounds.size.y * sprite_3d.pixel_size

	return viewport.size.y * sprite_3d.pixel_size

## Get the horizontal offset for HP bar positioning
func get_hp_bar_offset_x() -> float:
	return hp_bar_offset_x

## Calculate bounding rectangle of the skeletal model
## Returns Rect2 with local bounds (before scaling), or empty rect if unavailable
func _get_skeletal_bounds() -> Rect2:
	if not skeletal_instance:
		return Rect2()

	# Validate skeletal instance is in the scene tree
	if not skeletal_instance.is_inside_tree():
		push_warning("SkeletalChar2D5: Cannot calculate bounds - skeletal instance not in tree yet")
		return Rect2()

	# Try to find all Sprite2D children and calculate combined bounds
	var min_y: float = INF
	var max_y: float = -INF
	var min_x: float = INF
	var max_x: float = -INF
	var found_sprites: bool = false

	# Recursively find all Sprite2D nodes
	var sprites: Array = _find_all_sprites(skeletal_instance)

	for sprite: Variant in sprites:
		if sprite is Sprite2D:
			var sprite_2d: Sprite2D = sprite
			# Get sprite's local rect
			var sprite_rect: Rect2 = sprite_2d.get_rect()
			var sprite_pos: Vector2 = sprite_2d.global_position - skeletal_instance.global_position

			# Calculate bounds including sprite size
			var sprite_min: Vector2 = sprite_pos + sprite_rect.position
			var sprite_max: Vector2 = sprite_pos + sprite_rect.position + sprite_rect.size

			min_x = min(min_x, sprite_min.x)
			max_x = max(max_x, sprite_max.x)
			min_y = min(min_y, sprite_min.y)
			max_y = max(max_y, sprite_max.y)
			found_sprites = true

	if found_sprites:
		return Rect2(Vector2(min_x, min_y), Vector2(max_x - min_x, max_y - min_y))

	return Rect2()

## Recursively find all Sprite2D nodes in the tree
func _find_all_sprites(node: Node) -> Array:
	var sprites: Array = []

	if node is Sprite2D:
		sprites.append(node)

	for child: Node in node.get_children():
		sprites.append_array(_find_all_sprites(child))

	return sprites

## Flash the character white briefly (hit feedback)
func flash_white() -> void:
	if not skeletal_instance:
		return

	# IMPORTANT: Must modulate the 2D content INSIDE the SubViewport, not the Sprite3D!
	# Sprite3D displays a ViewportTexture which is pre-rendered, so modulating it has no effect.
	# We need to modulate the source (skeletal instance) before it's rendered to the texture.

	# Store original color to restore it
	var original_color: Color = skeletal_instance.modulate

	# Create flash tween - longer and more visible
	var flash_tween: Tween = create_tween()
	# Flash to pure white, hold briefly, then fade back
	flash_tween.tween_property(skeletal_instance, "modulate", Color(2.0, 2.0, 2.0, 1.0), 0.05)  # Brighten quickly
	flash_tween.tween_property(skeletal_instance, "modulate", Color(2.0, 2.0, 2.0, 1.0), 0.1)   # Hold
	flash_tween.tween_property(skeletal_instance, "modulate", original_color, 0.15)             # Fade back
