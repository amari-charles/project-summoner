extends Node3D
class_name WalkableAcademyBuilding

const COLOR_ART_IDLE: Color = Color(0.90, 0.90, 0.90, 1.0)
const COLOR_ART_READY: Color = Color.WHITE
const CUTOUT_RENDER_ORDER: Script = preload("res://scripts/meta/components/academy_cutout_render_order.gd")

@export var display_name_key: String = ""
@export var target_scene_path: String = ""
@export var return_scene_path: String = ""
@export var placeholder_art_pixel_size: float = 0.035
@export var placeholder_label_gap: float = 0.25
@export var name_label_gap: float = 1.0
@export var label_depth_offset: float = 0.05
@export_range(0.5, 1.0, 0.05) var collision_width_ratio: float = 0.8
@export_range(0.5, 3.0, 0.1) var collision_depth: float = 1.4
@export_range(0.5, 4.0, 0.1) var collision_height: float = 2.4

@onready var name_label: Label3D = %NameLabel
@onready var placeholder_label: Label3D = %PlaceholderLabel
@onready var door_area: Area3D = %DoorArea
@onready var placeholder_art: Sprite3D = %PlaceholderBuildingArt
@onready var collision_body: StaticBody3D = %CollisionBody
@onready var collision_shape: CollisionShape3D = %BuildingCollisionShape

var _player_inside: bool = false
var _transition_started: bool = false
var _placeholder_texture: Texture2D = null
var _campus_camera: Camera3D = null
var _placeholder_art_height: float = 0.0


func _ready() -> void:
	door_area.body_entered.connect(_on_door_body_entered)
	door_area.body_exited.connect(_on_door_body_exited)
	_refresh_placeholder_art()
	_refresh_text()
	_refresh_door_state()


func configure(
	name_key: String,
	target_scene: String,
	return_scene: String,
	placeholder_texture: Texture2D,
	campus_camera: Camera3D
) -> void:
	display_name_key = name_key
	target_scene_path = target_scene
	return_scene_path = return_scene
	_placeholder_texture = placeholder_texture
	_campus_camera = campus_camera
	if not is_instance_valid(_campus_camera):
		push_error("WalkableAcademyBuilding: A campus camera is required for billboard label placement")
	if is_inside_tree():
		_refresh_placeholder_art()
		_refresh_text()


func _process(_delta: float) -> void:
	_refresh_label_positions()
	if not _player_inside:
		return
	if _is_interact_pressed():
		_enter_target_scene()


func _on_door_body_entered(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = true
	_refresh_text()
	_refresh_door_state()


func _on_door_body_exited(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = false
	_refresh_text()
	_refresh_door_state()


func _refresh_text() -> void:
	if display_name_key.is_empty():
		name_label.text = ""
		placeholder_label.text = Loc.t("academy.walkable.placeholder_building", {"name": ""})
		return

	var display_name: String = Loc.t(display_name_key)
	name_label.text = display_name
	if _player_inside:
		name_label.text = "%s\n%s" % [display_name, Loc.t("academy.campus.enter")]
	placeholder_label.text = Loc.t("academy.walkable.placeholder_building", {"name": display_name.to_upper()})


func _refresh_door_state() -> void:
	if placeholder_art:
		placeholder_art.modulate = COLOR_ART_READY if _player_inside else COLOR_ART_IDLE


func _refresh_placeholder_art() -> void:
	if not placeholder_art or _placeholder_texture == null:
		return
	placeholder_art.texture = _placeholder_texture
	placeholder_art.pixel_size = placeholder_art_pixel_size
	placeholder_art.position.y = 0.0
	CUTOUT_RENDER_ORDER.anchor_visible_bottom(placeholder_art, _placeholder_texture)
	CUTOUT_RENDER_ORDER.apply_from_feet(placeholder_art, global_position.z)
	var visible_bounds: Rect2i = _placeholder_texture.get_image().get_used_rect()
	_placeholder_art_height = visible_bounds.size.y * placeholder_art_pixel_size
	_configure_collision(visible_bounds.size.x * placeholder_art_pixel_size)
	_refresh_label_positions()


func _configure_collision(visible_art_width: float) -> void:
	var box: BoxShape3D = collision_shape.shape.duplicate() as BoxShape3D
	box.size = Vector3(visible_art_width * collision_width_ratio, collision_height, collision_depth)
	collision_shape.shape = box
	collision_body.position = Vector3(0.0, collision_height * 0.5, -collision_depth * 0.5)


func _refresh_label_positions() -> void:
	if not is_instance_valid(_campus_camera) or _placeholder_art_height <= 0.0:
		return
	placeholder_label.global_position = CUTOUT_RENDER_ORDER.point_above_visible_art(
		global_position,
		_campus_camera.global_basis,
		_placeholder_art_height,
		placeholder_label_gap,
		label_depth_offset
	)
	name_label.global_position = CUTOUT_RENDER_ORDER.point_above_visible_art(
		global_position,
		_campus_camera.global_basis,
		_placeholder_art_height,
		name_label_gap,
		label_depth_offset
	)
	var label_priority: int = mini(
		placeholder_art.render_priority + 1,
		CUTOUT_RENDER_ORDER.MAX_RENDER_PRIORITY
	)
	placeholder_label.render_priority = label_priority
	name_label.render_priority = label_priority


func _is_interact_pressed() -> bool:
	return Input.is_action_just_pressed("interact")


func _enter_target_scene() -> void:
	if _transition_started:
		return
	if target_scene_path.is_empty():
		push_warning("WalkableAcademyBuilding: Missing target scene for %s" % display_name_key)
		return

	_transition_started = true
	set_process(false)
	var destination: String = return_scene_path
	if destination.is_empty():
		destination = SceneManager.SCENE_WALKABLE_ACADEMY_HUB
	NavigationContext.push_return(destination)
	SceneManager.transition_to(target_scene_path)
