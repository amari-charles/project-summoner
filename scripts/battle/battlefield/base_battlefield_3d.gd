extends Node3D
class_name BaseBattlefield3D

## Reusable battlefield scene that can be configured for different visual themes and layouts

# Environment configuration
@export_group("Environment")
@export var sky_color: Color = Color(0.53, 0.81, 0.98, 1.0)
@export var ambient_light_color: Color = Color.WHITE
@export var ambient_light_energy: float = 0.5

# Battlefield layout
@export_group("Layout")
## Spawn positions for player and enemy bases/units
## Depth (Z) is centered at 0 so summoners sit on the arena midpoint
## along the camera-facing axis.
@export var player_spawn_position: Vector3 = Vector3(-40, 0, 0)
@export var enemy_spawn_position: Vector3 = Vector3(40, 0, 0)

@export_group("Camera Bounds")
## Extra camera-clamp space beyond arena mesh on the X axis (left/right), in world units.
## Positive values let the camera show outside the arena horizontally.
@export var camera_bounds_padding_x: float = 0.0
## Extra camera-clamp space beyond arena mesh on the Z axis (near/far), in world units.
## Positive values let the camera show outside the arena in depth.
@export var camera_bounds_padding_z: float = 0.0
## Additional clamp room on the left edge beyond `camera_bounds_padding_x`.
@export var camera_bounds_padding_left: float = 0.0
## Additional clamp room on the right edge beyond `camera_bounds_padding_x`.
@export var camera_bounds_padding_right: float = 0.0
## Additional clamp room on the edge closest to the camera beyond `camera_bounds_padding_z`.
@export var camera_bounds_padding_toward_camera: float = 0.0
## Additional clamp room on the edge farther from the camera beyond `camera_bounds_padding_z`.
@export var camera_bounds_padding_away_from_camera: float = 0.0
## Expand live camera bounds to include the startup camera framing from the active profile.
## This preserves the authored match-start pose without relaxing zoom restrictions.
@export var include_startup_camera_footprint_in_bounds: bool = true

@onready var world_environment: WorldEnvironment = $WorldEnvironment
@onready var camera: CameraController3D = get_node_or_null("Camera3D") as CameraController3D
@onready var background: MeshInstance3D = $Background
@onready var player_spawn_marker: Marker3D = $PlayerSpawnMarker
@onready var enemy_spawn_marker: Marker3D = $EnemySpawnMarker
@onready var gameplay_layer: Node3D = $GameplayLayer
@onready var effects_layer: Node3D = $EffectsLayer

func _ready() -> void:
	_apply_biome_from_context()
	_configure_camera_bounds()
	_apply_spawn_positions()

func _configure_camera_bounds() -> void:
	if not camera:
		push_warning("BaseBattlefield3D: CameraController3D not found; cannot set map bounds")
		return

	var bounds_xz: Rect2 = get_ground_bounds_xz()
	if bounds_xz.size == Vector2.ZERO:
		push_warning("BaseBattlefield3D: Ground bounds unavailable; camera will keep existing bounds")
		return

	if include_startup_camera_footprint_in_bounds:
		bounds_xz = _merge_rects_xz(bounds_xz, _get_startup_camera_footprint_bounds())

	camera.set_arena_floor_bounds(_get_arena_mesh_bounds_xz())
	camera.set_map_bounds(bounds_xz)

func get_ground_bounds_xz() -> Rect2:
	var arena_bounds: Rect2 = _get_arena_mesh_bounds_xz()
	if arena_bounds.size == Vector2.ZERO:
		return Rect2()

	var safe_padding_x: float = max(camera_bounds_padding_x, 0.0)
	var safe_padding_z: float = max(camera_bounds_padding_z, 0.0)
	var padding_left: float = safe_padding_x + max(camera_bounds_padding_left, 0.0)
	var padding_right: float = safe_padding_x + max(camera_bounds_padding_right, 0.0)
	var padding_toward_camera: float = safe_padding_z + max(camera_bounds_padding_toward_camera, 0.0)
	var padding_away_from_camera: float = safe_padding_z + max(camera_bounds_padding_away_from_camera, 0.0)

	return Rect2(
		Vector2(arena_bounds.position.x - padding_left, arena_bounds.position.y - padding_toward_camera),
		Vector2(
			arena_bounds.size.x + padding_left + padding_right,
			arena_bounds.size.y + padding_toward_camera + padding_away_from_camera
		)
	)

func _get_arena_mesh_bounds_xz() -> Rect2:
	if not background:
		return Rect2()
	if not background.mesh or not background.mesh is PlaneMesh:
		return Rect2()

	var plane_mesh: PlaneMesh = background.mesh as PlaneMesh
	var width: float = plane_mesh.size.x * background.global_basis.x.length()
	var depth: float = plane_mesh.size.y * background.global_basis.z.length()
	var center: Vector3 = background.global_position

	var min_x: float = center.x - width * 0.5
	var min_z: float = center.z - depth * 0.5
	return Rect2(Vector2(min_x, min_z), Vector2(width, depth))

func _get_startup_camera_footprint_bounds() -> Rect2:
	if not camera:
		return Rect2()

	var profile: BattleCameraProjectionProfile = (
		camera.perspective_camera_profile
		if camera.projection_mode == BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
		else camera.orthographic_camera_profile
	)
	if not profile:
		return Rect2()

	var saved_transform: Transform3D = camera.transform
	var saved_projection: int = camera.projection
	var saved_keep_aspect: int = camera.keep_aspect
	var saved_fov: float = camera.fov
	var saved_size: float = camera.size
	var saved_near: float = camera.near
	var saved_far: float = camera.far

	camera.transform = profile.camera_transform
	camera.keep_aspect = profile.keep_aspect as Camera3D.KeepAspect
	camera.near = profile.near_clip
	camera.far = profile.far_clip
	if int(profile.projection_mode) == int(BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE):
		camera.projection = Camera3D.PROJECTION_PERSPECTIVE
		camera.fov = clamp(profile.default_zoom, profile.min_zoom, profile.max_zoom)
	else:
		camera.projection = Camera3D.PROJECTION_ORTHOGONAL
		camera.size = clamp(profile.default_zoom, profile.min_zoom, profile.max_zoom)

	camera.force_update_transform()
	var footprint: Rect2 = camera.get_ground_footprint_xz()

	camera.transform = saved_transform
	camera.projection = saved_projection as Camera3D.ProjectionType
	camera.keep_aspect = saved_keep_aspect as Camera3D.KeepAspect
	camera.fov = saved_fov
	camera.size = saved_size
	camera.near = saved_near
	camera.far = saved_far
	camera.force_update_transform()

	return footprint

func _merge_rects_xz(a: Rect2, b: Rect2) -> Rect2:
	if a.size == Vector2.ZERO:
		return b
	if b.size == Vector2.ZERO:
		return a

	var min_x: float = min(a.position.x, b.position.x)
	var min_z: float = min(a.position.y, b.position.y)
	var max_x: float = max(a.position.x + a.size.x, b.position.x + b.size.x)
	var max_z: float = max(a.position.y + a.size.y, b.position.y + b.size.y)
	return Rect2(Vector2(min_x, min_z), Vector2(max_x - min_x, max_z - min_z))

## Load and apply biome from BattleContext
func _apply_biome_from_context() -> void:
	var biome_id_variant: Variant = BattleContext.biome_id
	var biome_id: String = SafeTypeUtils.string(biome_id_variant, "")
	if biome_id.is_empty():
		push_warning("BaseBattlefield3D: No biome_id in BattleContext, using default visuals")
		return

	# Load biome resource
	var biome_path: String = "res://resources/biomes/%s.tres" % biome_id
	var loaded_biome: Resource = load(biome_path)

	if not loaded_biome or not loaded_biome is BiomeConfig:
		push_error("BaseBattlefield3D: Failed to load biome: %s" % biome_path)
		return

	# Type narrow to BiomeConfig for safe property access
	var biome: BiomeConfig = loaded_biome

	# Apply biome to battlefield
	biome.apply_to_battlefield(self)

## Apply spawn positions for bases and units
func _apply_spawn_positions() -> void:
	if player_spawn_marker:
		player_spawn_marker.position = player_spawn_position

	if enemy_spawn_marker:
		enemy_spawn_marker.position = enemy_spawn_position

func _update_ground_position() -> void:
	## Positions the ground plane below the camera's lowest visible Y coordinate
	## This ensures the ground is always below the viewport, preventing blue void
	if not camera or not background:
		return

	# Calculate the lowest Y the camera can see
	var camera_up: Vector3 = camera.transform.basis.y
	var lowest_view_y: float = camera.position.y - (camera_up.y * camera.size)

	# Position ground below visible area with small margin
	background.position.y = lowest_view_y - 1.0

func get_gameplay_layer() -> Node3D:
	return gameplay_layer

func get_effects_layer() -> Node3D:
	return effects_layer

func get_player_spawn_position() -> Vector3:
	return player_spawn_marker.global_position if player_spawn_marker else player_spawn_position

func get_enemy_spawn_position() -> Vector3:
	return enemy_spawn_marker.global_position if enemy_spawn_marker else enemy_spawn_position
