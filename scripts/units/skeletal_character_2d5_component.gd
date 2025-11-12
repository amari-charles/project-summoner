extends Character2D5Component
class_name SkeletalCharacter2D5Component

## Skeletal-based 2.5D Character Rendering Component
## Renders skeletal 2D animations in 3D space using Skeleton2D/AnimationPlayer + SubViewport

@export var skeletal_scene: PackedScene = null  ## The skeletal animation scene to instance
@export var scale_factor: Vector2 = Vector2(0.08, 0.08)  ## Scale of the skeletal model
@export var position_offset: Vector2 = Vector2(300, 200)  ## Position offset in viewport
@export var character_height_pixels: float = 0.0  ## Manual character height in texture space (0 = auto-calculate from bounds)
@export var hp_bar_offset_x: float = 0.0  ## Horizontal offset for HP bar in world units (negative = left, positive = right)

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

	# Bottom-align sprite using offset (feet at origin)
	# Wait one frame for skeletal instance to be fully in tree
	await get_tree().process_frame
	_setup_sprite_alignment()

## Instance the skeletal animation scene into the viewport
func _instance_skeletal_scene() -> void:
	if not skeletal_scene:
		push_error("Skeletal2D5Component: No skeletal_scene provided")
		return

	skeletal_instance = skeletal_scene.instantiate()
	if not skeletal_instance:
		push_error("Skeletal2D5Component: Failed to instance skeletal scene")
		return

	# Apply scale and position
	skeletal_instance.scale = scale_factor
	skeletal_instance.position = position_offset

	# Add to viewport
	model_container.add_child(skeletal_instance)

	# Find the AnimationPlayer in the skeletal scene
	animation_player = _find_animation_player(skeletal_instance)

	if not animation_player:
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

## Setup sprite alignment so character feet are at origin (Y=0)
## Positions BOTH the Sprite3D and the 2D skeletal content within viewport
func _setup_sprite_alignment() -> void:
	if not sprite_3d or not viewport:
		return

	# Calculate actual sprite height in world units
	var world_height = viewport.size.y * sprite_3d.pixel_size  # 600 * 0.01 = 6.0

	# Position Sprite3D so viewport bottom is at Y=0
	sprite_3d.position.y = world_height / 2.0  # 3.0 for skeletal sprites

	# Try to get skeletal bounds for precise positioning
	if skeletal_instance:
		var bounds = _get_skeletal_bounds()

		if bounds.size.y > 0:
			# PRECISE: Calculate position so model's bottom edge aligns with viewport bottom
			# Bottom edge = position.y + (bounds.end.y * scale.y)
			# We want: bottom edge = viewport.size.y
			# Therefore: position.y = viewport.size.y - (bounds.end.y * scale.y)
			skeletal_instance.position.y = viewport.size.y - (bounds.end.y * scale_factor.y)
		else:
			# FALLBACK: Use manually configured position_offset
			skeletal_instance.position.y = position_offset.y
			push_warning("SkeletalChar2D5: Could not calculate bounds, using manual position_offset - configure export for precise alignment")

## Get the world-space height of this sprite
## Used by HP bars, projectile spawns, etc.
func get_sprite_height() -> float:
	assert(viewport != null, "SkeletalChar2D5: viewport is null")
	assert(sprite_3d != null, "SkeletalChar2D5: sprite_3d is null")

	# Use manual height if specified
	if character_height_pixels > 0:
		# Validate reasonable bounds
		if character_height_pixels < 100 or character_height_pixels > 10000:
			push_warning("SkeletalChar2D5: character_height_pixels = %f is outside reasonable range (100-10000). Check configuration." % character_height_pixels)
		return character_height_pixels * scale_factor.y * sprite_3d.pixel_size

	# Auto-calculate from skeletal bounds
	assert(skeletal_instance != null, "SkeletalChar2D5: skeletal_instance is null and no manual height set")
	var bounds = _get_skeletal_bounds()
	assert(bounds.size.y > 0, "SkeletalChar2D5: calculated bounds height is 0 and no manual height set")

	return bounds.size.y * scale_factor.y * sprite_3d.pixel_size

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
	var min_y = INF
	var max_y = -INF
	var min_x = INF
	var max_x = -INF
	var found_sprites = false

	# Recursively find all Sprite2D nodes
	var sprites = _find_all_sprites(skeletal_instance)

	for sprite in sprites:
		if sprite is Sprite2D:
			# Get sprite's local rect
			var sprite_rect = sprite.get_rect()
			var sprite_pos = sprite.global_position - skeletal_instance.global_position

			# Calculate bounds including sprite size
			var sprite_min = sprite_pos + sprite_rect.position
			var sprite_max = sprite_pos + sprite_rect.position + sprite_rect.size

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
	var sprites = []

	if node is Sprite2D:
		sprites.append(node)

	for child in node.get_children():
		sprites.append_array(_find_all_sprites(child))

	return sprites
