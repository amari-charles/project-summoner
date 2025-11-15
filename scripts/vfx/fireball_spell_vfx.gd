extends VFXInstance
## Fireball Spell VFX - Red fireball that descends from off-screen with angled trajectory

## Configurable parameters
@export var fall_duration: float = 0.8  ## How long the descent takes
@export var start_offset: Vector3 = Vector3(-15, 25, 0)  ## Offset from target for start position
@export var sprite_rotation_degrees: float = 0.0  ## Rotation of the sprite in degrees (Z-axis)
@export var damage: float = 100.0  ## AOE damage on impact
@export var damage_radius: float = 10.0  ## Fallback radius for AOE indicator sizing (overridden by Card's spell_radius at runtime)
@export var explosion_vfx_id: String = "fireball_explosion"  ## VFX to spawn on impact
@export var camera_shake_intensity: float = 0.2  ## Camera shake strength
@export var camera_shake_duration: float = 0.4  ## Camera shake duration
@export var indicator_linger_duration: float = 2.0  ## How long the AOE indicator lingers before fading
@export var indicator_visual_scale: float = 0.125  ## Scale factor for indicator size (visual radius / actual damage radius)

var target_position: Vector3 = Vector3.ZERO
var animated_sprite: AnimatedSprite3D = null
var aoe_indicator: MeshInstance3D = null
var tween: Tween = null

func _ready() -> void:
	# Find child nodes
	for child: Node in get_children():
		if child is AnimatedSprite3D:
			animated_sprite = child as AnimatedSprite3D
		elif child is MeshInstance3D and child.name == "AOEIndicator":
			aoe_indicator = child as MeshInstance3D

	# Duplicate material to avoid shared resource issues with pooling
	if aoe_indicator:
		var shared_material: ShaderMaterial = aoe_indicator.get_surface_override_material(0) as ShaderMaterial
		if shared_material:
			var instance_material: ShaderMaterial = shared_material.duplicate()
			aoe_indicator.set_surface_override_material(0, instance_material)

	# Apply sprite rotation
	if animated_sprite and sprite_rotation_degrees != 0.0:
		animated_sprite.rotation_degrees.z = sprite_rotation_degrees

	# Store the landing position (global_position is set by VFXManager)
	target_position = global_position

	super._ready()

## Override _on_play to start the descent animation
func _on_play() -> void:
	if not animated_sprite:
		push_error("FireballSpellVFX: No AnimatedSprite3D child found!")
		return

	# Update target position only on first call (ZERO from _on_reset)
	# This prevents double-call issues where global_position has already been modified
	if target_position == Vector3.ZERO:
		target_position = global_position

	# Start the fireball animation
	animated_sprite.play("default")

	# Set up start and end positions
	var start_pos: Vector3 = target_position + start_offset
	var end_pos: Vector3 = target_position

	# Set initial position
	global_position = start_pos

	# Create tween for descent animation
	if tween:
		tween.kill()

	tween = create_tween()
	tween.set_ease(Tween.EASE_IN)  # Accelerates as it falls
	tween.set_trans(Tween.TRANS_QUAD)
	tween.tween_property(self, "global_position", end_pos, fall_duration)
	tween.finished.connect(_on_impact)

## Called when fireball reaches target position
func _on_impact() -> void:
	# Show and position AOE indicator at impact location
	if aoe_indicator:
		# Dynamically scale the indicator to match damage radius
		# Use scale instead of modifying mesh.size to avoid issues with pooled resources
		# Base mesh is 20x20, scale it to match damage_radius * 2 (diameter)
		var base_size: float = 20.0  # Base QuadMesh size from scene
		var target_visual_radius: float = damage_radius * indicator_visual_scale
		var target_diameter: float = target_visual_radius * 2.0
		var scale_factor: float = target_diameter / base_size
		aoe_indicator.scale = Vector3(scale_factor, scale_factor, scale_factor)

		# Position just above ground to prevent z-fighting (script controls Y position)
		var ground_pos: Vector3 = target_position
		ground_pos.y = 0.5  # Higher above ground to prevent clipping
		aoe_indicator.global_position = ground_pos
		aoe_indicator.visible = true

		# Increase cull margin to prevent frustum culling of large mesh
		aoe_indicator.extra_cull_margin = 100.0

		# Get the shader material to fade it
		var material: ShaderMaterial = aoe_indicator.get_surface_override_material(0) as ShaderMaterial
		if material:
			# Reset colors to full visibility (gradient from bright center to darker edge)
			material.set_shader_parameter("center_color", Color(1.0, 0.5, 0.0, 0.9))
			material.set_shader_parameter("edge_color", Color(1.0, 0.2, 0.0, 0.6))

			# Create fade-out tween after linger duration
			var fade_tween: Tween = create_tween()
			fade_tween.tween_interval(indicator_linger_duration)

			# Fade both center and edge colors to transparent
			fade_tween.parallel().tween_method(
				func(alpha: float) -> void:
					material.set_shader_parameter("center_color", Color(1.0, 0.5, 0.0, alpha)),
				0.9,  # From
				0.0,  # To
				0.5   # Duration
			)
			fade_tween.parallel().tween_method(
				func(alpha: float) -> void:
					material.set_shader_parameter("edge_color", Color(1.0, 0.2, 0.0, alpha)),
				0.6,  # From
				0.0,  # To
				0.5   # Duration
			)

			fade_tween.finished.connect(func() -> void:
				if aoe_indicator:
					aoe_indicator.visible = false
				# Call stop() after fade completes
				stop()
			)

	# Spawn explosion VFX
	if not explosion_vfx_id.is_empty() and VFXManager:
		VFXManager.play_effect(explosion_vfx_id, global_position)

	# NOTE: Damage is applied by Card.gd, not by this VFX
	# This VFX is purely visual

	# NOTE: stop() is now called after the AOE indicator fade completes (see fade_tween.finished above)
	# This keeps the node active long enough for the indicator to be visible

## Override _on_reset for pooling
func _on_reset() -> void:
	if tween:
		tween.kill()
		tween = null

	if animated_sprite:
		animated_sprite.stop()

	# Hide and reset AOE indicator
	if aoe_indicator:
		aoe_indicator.visible = false
		aoe_indicator.scale = Vector3.ONE  # Reset scale for next use

	target_position = Vector3.ZERO
