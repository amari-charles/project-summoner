extends Character2D5Component
class_name SpriteCharacter2D5Component

## Sprite-based 2.5D Character Rendering Component
## Renders 2D sprite animations in 3D space using AnimatedSprite2D + SubViewport

## Sprite scaling constants
const BASE_VIEWPORT_SIZE: int = 250  ## Base viewport height in pixels
const LEGACY_SPRITE_SIZE: int = 100  ## Original sprite height (soldier, archer, etc.)
const DEFAULT_SPRITE_SCALE: float = 2.5  ## Default scale for 100px sprites (250 / 100 = 2.5)

## =============================================================================
## ATTACK ANIMATION CONSTANTS
## =============================================================================
## These values are tuned for visual appeal and game feel. Timings are in seconds.

# Lunge attack - quick thrust forward and snap back
const LUNGE_DISTANCE_PX: float = 20.0  ## How far to lunge toward enemy (pixels)
const LUNGE_FORWARD_DURATION: float = 0.1  ## Time to reach full lunge
const LUNGE_RETURN_DURATION: float = 0.15  ## Time to snap back (slightly slower for impact feel)

# Squash & Spring attack - cartoon-style compression then spring
const SPRING_DISTANCE_PX: float = 15.0  ## How far to spring forward (pixels)
const SQUASH_SCALE: Vector2 = Vector2(1.3, 0.7)  ## Horizontal stretch, vertical compress
const SPRING_SCALE: Vector2 = Vector2(0.85, 1.2)  ## Vertical stretch during spring
const SQUASH_DURATION: float = 0.08  ## Time to compress
const SPRING_DURATION: float = 0.1  ## Time to spring forward
const SPRING_RETURN_DURATION: float = 0.12  ## Time to return to normal

# Spin attack - full rotation with scale pulse
const SPIN_WINDUP_DEGREES: float = 15.0  ## Slight wind-up rotation before spin
const SPIN_FULL_ROTATION: float = 360.0  ## Full spin amount
const SPIN_SCALE_MULTIPLIER: float = 1.1  ## Scale pulse during spin
const SPIN_WINDUP_DURATION: float = 0.05  ## Time for wind-up
const SPIN_DURATION: float = 0.2  ## Time for full rotation
const SPIN_SCALE_DURATION: float = 0.1  ## Time for scale pulse (each direction)

# Pulse attack - rapid expand then shrink with brightness
const PULSE_SCALE_MULTIPLIER: float = 1.4  ## How much to expand
const PULSE_BRIGHTNESS: Color = Color(1.5, 1.5, 1.5, 1.0)  ## Brightness during pulse
const PULSE_EXPAND_DURATION: float = 0.08  ## Time to expand
const PULSE_SHRINK_DURATION: float = 0.15  ## Time to shrink back

## Offset in pixels from texture bottom to actual character feet
## Use this when sprite artwork has empty space below the character's feet
## Example: 100px texture with feet at 70px from top = 30px offset
@export var feet_offset_pixels: float = 0.0
## Offset in pixels from texture top to actual character head
## Use this when sprite artwork has empty space above the character's head
## Example: 310px texture with head at 60px from top = 60px offset
@export var head_offset_pixels: float = 0.0
@export var hp_bar_offset_x: float = 0.0  ## Horizontal offset for HP bar in world units (negative = left, positive = right)
## Scale for sprite within viewport
## Use calculate_sprite_scale() helper or calculate manually: BASE_VIEWPORT_SIZE / sprite_height
@export var sprite_scale: float = DEFAULT_SPRITE_SCALE
## Scale multiplier for the viewport (for large units like titans)
## Set to 4.0 for a unit 4x the normal size
@export var viewport_scale: float = 1.0

## Walking/bobbing animation settings
@export var enable_bobbing: bool = false  ## Enable walking bob animation
@export var bob_speed: float = 8.0  ## Speed of walk cycle (higher = faster steps)
@export var bob_amplitude: float = 6.0  ## Vertical bounce in pixels
@export var bob_rotation_amplitude: float = 4.0  ## Side-to-side tilt in degrees

## Attack animation styles for single-frame sprites
enum AttackStyle { NONE, LUNGE, SQUASH_SPRING, SPIN, PULSE }
@export var attack_style: AttackStyle = AttackStyle.NONE
@export var cycle_attack_styles: bool = false  ## Cycle through all styles for testing
@export var attacks_per_style: int = 2  ## How many attacks before switching style

## Bobbing state
var _bob_time: float = 0.0  ## Accumulated time for bobbing animation
var _base_sprite_y: float = 0.0  ## Base Y position before bobbing
var _base_sprite_scale: Vector2 = Vector2.ONE  ## Base scale before attack effects
var _attack_tween: Tween = null  ## Current attack animation tween
var _cycle_attack_count: int = 0  ## Counter for cycling through styles
var _cycle_current_style: int = 1  ## Current style when cycling (starts at LUNGE)
var _is_attacking: bool = false  ## True during attack animations (pauses bobbing)

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var character_sprite: AnimatedSprite2D = $Sprite3D/SubViewport/Model2D/CharacterSprite

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Apply viewport scaling for large units (titans, bosses, etc.)
	if viewport_scale != 1.0:
		var scaled_size: int = int(BASE_VIEWPORT_SIZE * viewport_scale)
		viewport.size = Vector2i(scaled_size, scaled_size)
		# Keep pixel_size constant so world-space size scales with viewport
		# (larger viewport = larger unit in world)

	# Apply sprite scale
	character_sprite.scale = Vector2(sprite_scale, sprite_scale)

	# Bottom-align sprite using offset (feet at origin)
	_setup_sprite_alignment()

	# Store base values for animations
	_base_sprite_scale = character_sprite.scale
	_base_sprite_y = character_sprite.position.y

	# Setup bobbing animation
	if enable_bobbing:
		# Randomize starting phase so multiple units don't bob in sync
		_bob_time = randf() * TAU

func _process(delta: float) -> void:
	if not enable_bobbing or not character_sprite or _is_attacking:
		return

	# Update walk animation (wrap to prevent unbounded growth)
	_bob_time = fmod(_bob_time + delta * bob_speed, TAU * 2.0)

	# Bouncy vertical motion using abs(sin()) - simulates stepping/bouncing
	# The -abs() makes it bounce DOWN from base position (feet hitting ground)
	var bob_offset: float = -abs(sin(_bob_time)) * bob_amplitude

	# Side-to-side tilt like a waddle - alternates left/right with each "step"
	var rotation_offset: float = sin(_bob_time) * bob_rotation_amplitude

	# Apply walk animation
	character_sprite.position.y = _base_sprite_y + bob_offset
	character_sprite.rotation_degrees = rotation_offset

## Set the SpriteFrames resource for this character
func set_sprite_frames(frames: SpriteFrames) -> void:
	if character_sprite:
		character_sprite.sprite_frames = frames
		# Recalculate alignment now that we have actual texture data
		_setup_sprite_alignment()
		# Update base Y position for bobbing
		if enable_bobbing:
			_base_sprite_y = character_sprite.position.y

## Flip the sprite horizontally
func set_flip_h(_flip: bool) -> void:
	if character_sprite:
		character_sprite.flip_h = _flip

## Play an animation
func play_animation(_anim_name: String, _auto_play: bool = false) -> void:
	if character_sprite and character_sprite.sprite_frames:
		# Check if animation exists before trying to play it
		if character_sprite.sprite_frames.has_animation(_anim_name):
			character_sprite.animation = _anim_name
			# Note: autoplay should only be set before node is added to scene
			# Since we're already in the scene, just play() is sufficient
			character_sprite.play()

			# Trigger attack effect for single-frame sprites
			if _anim_name == "attack" and attack_style != AttackStyle.NONE:
				_play_attack_effect()
		else:
			push_warning("Animation '%s' not found in sprite_frames, falling back to 'idle'" % _anim_name)
			if character_sprite.sprite_frames.has_animation("idle"):
				character_sprite.animation = "idle"
				character_sprite.play()

## Stop current animation
func stop_animation() -> void:
	if character_sprite:
		character_sprite.stop()

## Get current animation name
func get_current_animation() -> String:
	if character_sprite:
		return character_sprite.animation
	return ""

## Check if animation is playing
func is_playing() -> bool:
	if character_sprite:
		return character_sprite.is_playing()
	return false

## Get the duration of an animation in seconds
func get_animation_duration(_anim_name: String) -> float:
	if character_sprite and character_sprite.sprite_frames:
		var frames: SpriteFrames = character_sprite.sprite_frames
		if frames.has_animation(_anim_name):
			var frame_count: int = frames.get_frame_count(_anim_name)
			var fps: float = frames.get_animation_speed(_anim_name)
			if fps > 0:
				return frame_count / fps
	return 1.0  # Fallback duration

## Setup sprite alignment so character feet are at origin (Y=0)
## Positions BOTH the Sprite3D and the 2D sprite content within viewport
func _setup_sprite_alignment() -> void:
	if not sprite_3d or not viewport or not character_sprite:
		return

	# Calculate actual sprite height in world units
	var world_height: float = viewport.size.y * sprite_3d.pixel_size  # VIEWPORT_SIZE * pixel_size

	# Position Sprite3D so viewport bottom is at Y=0
	sprite_3d.position.y = world_height / 2.0  # 3.125 for standard sprites

	# Center sprite horizontally in viewport (important for scaled viewports)
	character_sprite.position.x = viewport.size.x / 2.0

	# Get actual texture size for precise feet positioning
	var texture_size: Vector2 = _get_current_frame_size()

	if texture_size.y > 0:
		# PRECISE: Calculate position so sprite's actual feet align with viewport bottom
		# With centered=true: feet_y = sprite.position.y + ((texture_height / 2) - feet_offset) * sprite.scale.y
		# We want: feet at viewport bottom, accounting for empty space below feet
		# Therefore: sprite.position.y = viewport.size.y - ((texture_height / 2) - feet_offset) * sprite.scale.y
		character_sprite.position.y = viewport.size.y - ((texture_size.y / 2.0) - feet_offset_pixels) * character_sprite.scale.y
	else:
		# FALLBACK: No texture data available yet, use approximate positioning
		character_sprite.position.y = viewport.size.y * 0.8

## Get the world-space height of this sprite
## Used by HP bars, projectile spawns, etc.
func get_sprite_height() -> float:
	assert(viewport != null, "SpriteChar2D5: viewport is null")
	assert(sprite_3d != null, "SpriteChar2D5: sprite_3d is null")

	# Get actual texture size to calculate real character height
	var texture_size: Vector2 = _get_current_frame_size()

	if texture_size.y > 0:
		# Actual sprite height in world units, accounting for feet and head offsets
		return (texture_size.y - feet_offset_pixels - head_offset_pixels) * sprite_3d.pixel_size

	# Fallback: use viewport height (sprite frames not loaded yet)
	push_warning("SpriteChar2D5: No texture data available, using viewport fallback. Check sprite_frames configuration.")
	return viewport.size.y * sprite_3d.pixel_size

## Get the horizontal offset for HP bar positioning
func get_hp_bar_offset_x() -> float:
	return hp_bar_offset_x

## Get the size of the current sprite frame texture
## Returns Vector2.ZERO if no texture available
func _get_current_frame_size() -> Vector2:
	if not character_sprite or not character_sprite.sprite_frames:
		return Vector2.ZERO

	# Get current animation name (default to "idle" if not set)
	var anim: String = character_sprite.animation
	if anim == "":
		anim = "idle"

	# Check if animation exists
	if not character_sprite.sprite_frames.has_animation(anim):
		return Vector2.ZERO

	# Get frame count
	var frame_count: int = character_sprite.sprite_frames.get_frame_count(anim)
	if frame_count == 0:
		return Vector2.ZERO

	# Get first frame texture (assume all frames same size)
	var frame_texture: Texture2D = character_sprite.sprite_frames.get_frame_texture(anim, 0)
	if not frame_texture:
		return Vector2.ZERO

	return frame_texture.get_size()

## =============================================================================
## STATIC HELPER METHODS
## =============================================================================

## Calculate appropriate sprite scale for a given sprite height
## This ensures sprites of different sizes render at consistent world scale
static func calculate_sprite_scale(sprite_height_pixels: int) -> float:
	if sprite_height_pixels <= 0:
		push_warning("SpriteChar2D5: Invalid sprite height %d, using default scale" % sprite_height_pixels)
		return DEFAULT_SPRITE_SCALE
	return float(BASE_VIEWPORT_SIZE) / float(sprite_height_pixels)

## Flash the character white briefly (hit feedback)
func flash_white() -> void:
	if not character_sprite:
		return

	# IMPORTANT: Must modulate the 2D content INSIDE the SubViewport, not the Sprite3D!
	# Sprite3D displays a ViewportTexture which is pre-rendered, so modulating it has no effect.
	# We need to modulate the source (AnimatedSprite2D) before it's rendered to the texture.

	# Store original color to restore it
	var original_color: Color = character_sprite.modulate

	# Create flash tween - longer and more visible
	var flash_tween: Tween = create_tween()
	# Flash to pure white, hold briefly, then fade back
	flash_tween.tween_property(character_sprite, "modulate", Color(2.0, 2.0, 2.0, 1.0), 0.05)  # Brighten quickly
	flash_tween.tween_property(character_sprite, "modulate", Color(2.0, 2.0, 2.0, 1.0), 0.1)   # Hold
	flash_tween.tween_property(character_sprite, "modulate", original_color, 0.15)             # Fade back

## =============================================================================
## ATTACK ANIMATION EFFECTS
## =============================================================================

## Play the appropriate attack effect based on attack_style
func _play_attack_effect() -> void:
	# Kill any existing attack tween
	if _attack_tween and _attack_tween.is_valid():
		_attack_tween.kill()

	# Pause bobbing during attack animation
	_is_attacking = true

	# Determine which style to use
	var style_to_use: int = attack_style
	if cycle_attack_styles:
		style_to_use = _cycle_current_style
		_cycle_attack_count += 1
		# Switch to next style after attacks_per_style attacks
		if _cycle_attack_count >= attacks_per_style:
			_cycle_attack_count = 0
			_cycle_current_style += 1
			# Wrap back to LUNGE (1) after PULSE (4)
			if _cycle_current_style > AttackStyle.PULSE:
				_cycle_current_style = AttackStyle.LUNGE

	match style_to_use:
		AttackStyle.LUNGE:
			_attack_lunge()
		AttackStyle.SQUASH_SPRING:
			_attack_squash_spring()
		AttackStyle.SPIN:
			_attack_spin()
		AttackStyle.PULSE:
			_attack_pulse()

## Lunge/Ram - Quick thrust forward then snap back
func _attack_lunge() -> void:
	if not character_sprite:
		return

	var base_x: float = character_sprite.position.x
	# Lunge direction based on flip (attacking toward enemy)
	var direction: float = -1.0 if character_sprite.flip_h else 1.0

	_attack_tween = create_tween()
	_attack_tween.tween_property(character_sprite, "position:x", base_x + (LUNGE_DISTANCE_PX * direction), LUNGE_FORWARD_DURATION).set_ease(Tween.EASE_OUT)
	_attack_tween.tween_property(character_sprite, "position:x", base_x, LUNGE_RETURN_DURATION).set_ease(Tween.EASE_IN)
	_attack_tween.tween_callback(_on_attack_finished)

## Squash & Spring - Compress then spring forward
func _attack_squash_spring() -> void:
	if not character_sprite:
		return

	var base_x: float = character_sprite.position.x
	var direction: float = -1.0 if character_sprite.flip_h else 1.0

	_attack_tween = create_tween()
	# Squash down (compress vertically, expand horizontally)
	_attack_tween.tween_property(character_sprite, "scale", _base_sprite_scale * SQUASH_SCALE, SQUASH_DURATION).set_ease(Tween.EASE_OUT)
	# Spring forward and stretch
	_attack_tween.tween_property(character_sprite, "scale", _base_sprite_scale * SPRING_SCALE, SPRING_DURATION).set_ease(Tween.EASE_OUT)
	_attack_tween.parallel().tween_property(character_sprite, "position:x", base_x + (SPRING_DISTANCE_PX * direction), SPRING_DURATION).set_ease(Tween.EASE_OUT)
	# Return to normal
	_attack_tween.tween_property(character_sprite, "scale", _base_sprite_scale, SPRING_RETURN_DURATION).set_ease(Tween.EASE_IN_OUT)
	_attack_tween.parallel().tween_property(character_sprite, "position:x", base_x, SPRING_RETURN_DURATION).set_ease(Tween.EASE_IN)
	_attack_tween.tween_callback(_on_attack_finished)

## Spin Attack - Quick rotation
func _attack_spin() -> void:
	if not character_sprite:
		return

	# Spin direction based on facing
	var spin_direction: float = 1.0 if character_sprite.flip_h else -1.0

	_attack_tween = create_tween()
	# Wind up slightly (opposite direction)
	_attack_tween.tween_property(character_sprite, "rotation_degrees", SPIN_WINDUP_DEGREES * -spin_direction, SPIN_WINDUP_DURATION)
	# Full spin
	_attack_tween.tween_property(character_sprite, "rotation_degrees", SPIN_FULL_ROTATION * spin_direction, SPIN_DURATION).set_ease(Tween.EASE_OUT)
	# Slight scale pulse during spin
	_attack_tween.parallel().tween_property(character_sprite, "scale", _base_sprite_scale * SPIN_SCALE_MULTIPLIER, SPIN_SCALE_DURATION)
	_attack_tween.parallel().chain().tween_property(character_sprite, "scale", _base_sprite_scale, SPIN_SCALE_DURATION)
	# Return rotation to neutral (0) so bobbing can take over smoothly
	_attack_tween.tween_property(character_sprite, "rotation_degrees", 0.0, SPIN_WINDUP_DURATION)
	_attack_tween.tween_callback(_on_attack_finished)

## Pulse/Expand - Rapidly grow then shrink
func _attack_pulse() -> void:
	if not character_sprite:
		return

	var original_color: Color = character_sprite.modulate

	_attack_tween = create_tween()
	# Step 1: Expand + brighten (parallel)
	_attack_tween.set_parallel(true)
	_attack_tween.tween_property(character_sprite, "scale", _base_sprite_scale * PULSE_SCALE_MULTIPLIER, PULSE_EXPAND_DURATION).set_ease(Tween.EASE_OUT)
	_attack_tween.tween_property(character_sprite, "modulate", PULSE_BRIGHTNESS, PULSE_EXPAND_DURATION)
	# Step 2: Shrink + dim (parallel)
	_attack_tween.chain().tween_property(character_sprite, "scale", _base_sprite_scale, PULSE_SHRINK_DURATION).set_ease(Tween.EASE_IN_OUT)
	_attack_tween.parallel().tween_property(character_sprite, "modulate", original_color, PULSE_SHRINK_DURATION)
	# Step 3: Callback
	_attack_tween.chain().tween_callback(_on_attack_finished)

## Called when attack animation finishes - resumes bobbing
func _on_attack_finished() -> void:
	if not is_instance_valid(self):
		return
	_is_attacking = false
