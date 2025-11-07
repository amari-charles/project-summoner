extends Node3D
class_name FloatingHPBar

## 3D floating health bar that follows a unit
## Managed by HPBarManager for pooling

## Visual settings
@export var bar_width: float = 1.0
@export var bar_height: float = 0.1
@export var offset_y: float = 1.5  ## Height above unit
@export var show_on_damage_only: bool = false  ## Hide when at full HP
@export var fade_delay: float = 3.0  ## Seconds before fading when damaged
@export var fade_duration: float = 0.5  ## Fade out time

## Colors
@export var color_full: Color = Color.GREEN
@export var color_mid: Color = Color.YELLOW
@export var color_low: Color = Color.RED
@export var background_color: Color = Color(0.2, 0.2, 0.2, 0.8)

## State
var target_unit: Node3D = null
var current_hp: float = 100.0
var max_hp: float = 100.0
var is_pooled: bool = false
var fade_timer: float = 0.0
var is_visible: bool = true

## Visual components
var background_mesh: MeshInstance3D = null
var bar_mesh: MeshInstance3D = null
var camera: Camera3D = null

signal bar_hidden()  ## Emitted when bar fades out (for pooling)

func _ready() -> void:
	_create_visuals()
	_find_camera()

func _process(delta: float) -> void:
	if not target_unit or not is_instance_valid(target_unit):
		return

	# Follow target unit
	global_position = target_unit.global_position + Vector3(0, offset_y, 0)

	# Billboard effect - always face camera
	if camera:
		look_at(camera.global_position, Vector3.UP)

	# Handle fade timer
	if show_on_damage_only and fade_timer > 0.0:
		fade_timer -= delta
		if fade_timer <= 0.0:
			_fade_out()

func _create_visuals() -> void:
	# Create background bar
	background_mesh = MeshInstance3D.new()
	var bg_quad = QuadMesh.new()
	bg_quad.size = Vector2(bar_width, bar_height)
	background_mesh.mesh = bg_quad

	var bg_material = StandardMaterial3D.new()
	bg_material.albedo_color = background_color
	bg_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	bg_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	bg_material.no_depth_test = true
	background_mesh.material_override = bg_material

	add_child(background_mesh)

	# Create health bar (foreground)
	bar_mesh = MeshInstance3D.new()
	var bar_quad = QuadMesh.new()
	bar_quad.size = Vector2(bar_width, bar_height)
	bar_mesh.mesh = bar_quad

	var bar_material = StandardMaterial3D.new()
	bar_material.albedo_color = color_full
	bar_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	bar_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	bar_material.no_depth_test = true
	bar_mesh.material_override = bar_material

	# Offset slightly forward to prevent z-fighting
	bar_mesh.position = Vector3(0, 0, -0.01)
	add_child(bar_mesh)

func _find_camera() -> void:
	# Find main camera in scene
	var viewport = get_viewport()
	if viewport:
		camera = viewport.get_camera_3d()

## Set target unit to follow
func set_target(unit: Node3D) -> void:
	target_unit = unit

	# Connect to unit signals if available
	if target_unit.has_signal("hp_changed"):
		if not target_unit.hp_changed.is_connected(_on_hp_changed):
			target_unit.hp_changed.connect(_on_hp_changed)

	# Update HP immediately
	if "current_hp" in target_unit and "max_hp" in target_unit:
		update_hp(target_unit.current_hp, target_unit.max_hp)

## Update health bar display
func update_hp(current: float, maximum: float) -> void:
	current_hp = current
	max_hp = maximum

	var hp_percent = current_hp / max_hp if max_hp > 0 else 0.0
	hp_percent = clamp(hp_percent, 0.0, 1.0)

	# Update bar width
	if bar_mesh and bar_mesh.mesh:
		var quad = bar_mesh.mesh as QuadMesh
		quad.size = Vector2(bar_width * hp_percent, bar_height)

		# Offset to align left
		bar_mesh.position.x = -bar_width * 0.5 + (bar_width * hp_percent * 0.5)

	# Update color based on HP percentage
	var bar_color = _get_hp_color(hp_percent)
	if bar_mesh and bar_mesh.material_override:
		var mat = bar_mesh.material_override as StandardMaterial3D
		mat.albedo_color = bar_color

	# Handle show_on_damage_only behavior
	if show_on_damage_only:
		if hp_percent < 1.0:
			_show()
			fade_timer = fade_delay
		else:
			_hide_immediate()

## Get color based on HP percentage
func _get_hp_color(hp_percent: float) -> Color:
	if hp_percent > 0.5:
		# Interpolate between full and mid
		var t = (hp_percent - 0.5) / 0.5
		return color_mid.lerp(color_full, t)
	else:
		# Interpolate between low and mid
		var t = hp_percent / 0.5
		return color_low.lerp(color_mid, t)

## Show the bar
func _show() -> void:
	if is_visible:
		return

	is_visible = true
	visible = true

	# Reset alpha
	if bar_mesh and bar_mesh.material_override:
		var mat = bar_mesh.material_override as StandardMaterial3D
		var color = mat.albedo_color
		color.a = 1.0
		mat.albedo_color = color

	if background_mesh and background_mesh.material_override:
		var mat = background_mesh.material_override as StandardMaterial3D
		mat.albedo_color = background_color

## Hide immediately
func _hide_immediate() -> void:
	is_visible = false
	visible = false

## Fade out animation
func _fade_out() -> void:
	if not is_visible:
		return

	# Animate alpha to 0
	var tween = create_tween()
	tween.set_parallel(true)

	if bar_mesh and bar_mesh.material_override:
		var mat = bar_mesh.material_override as StandardMaterial3D
		var color = mat.albedo_color
		tween.tween_property(mat, "albedo_color:a", 0.0, fade_duration).from(color.a)

	if background_mesh and background_mesh.material_override:
		var mat = background_mesh.material_override as StandardMaterial3D
		tween.tween_property(mat, "albedo_color:a", 0.0, fade_duration).from(background_color.a)

	tween.finished.connect(func():
		_hide_immediate()
		bar_hidden.emit()
	)

## Reset for pooling reuse
func reset() -> void:
	target_unit = null
	current_hp = 100.0
	max_hp = 100.0
	fade_timer = 0.0
	is_visible = true
	visible = true

	# Reset materials
	if bar_mesh and bar_mesh.material_override:
		var mat = bar_mesh.material_override as StandardMaterial3D
		mat.albedo_color = color_full
		mat.albedo_color.a = 1.0

	if background_mesh and background_mesh.material_override:
		var mat = background_mesh.material_override as StandardMaterial3D
		mat.albedo_color = background_color

	if bar_mesh:
		bar_mesh.position = Vector3(0, 0, -0.01)

## Signal handler for unit HP changes
func _on_hp_changed(new_hp: float, new_max_hp: float) -> void:
	update_hp(new_hp, new_max_hp)
