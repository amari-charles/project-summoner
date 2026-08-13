extends Node3D
class_name WalkableAcademyBuilding

const COLOR_DOOR_IDLE: Color = Color(0.22, 0.16, 0.11, 1.0)
const COLOR_DOOR_READY: Color = Color(0.80, 0.58, 0.24, 1.0)

@export var display_name_key: String = ""
@export var target_scene_path: String = ""
@export var return_scene_path: String = ""

@onready var name_label: Label3D = %NameLabel
@onready var placeholder_label: Label3D = %PlaceholderLabel
@onready var door_area: Area3D = %DoorArea
@onready var door_material: StandardMaterial3D = %DoorVisual.get_surface_override_material(0) as StandardMaterial3D

var _player_inside: bool = false
var _transition_started: bool = false


func _ready() -> void:
	door_area.body_entered.connect(_on_door_body_entered)
	door_area.body_exited.connect(_on_door_body_exited)
	_refresh_text()
	_refresh_door_state()


func configure(name_key: String, target_scene: String, return_scene: String) -> void:
	display_name_key = name_key
	target_scene_path = target_scene
	return_scene_path = return_scene
	if is_inside_tree():
		_refresh_text()


func _process(_delta: float) -> void:
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
	if door_material:
		door_material.albedo_color = COLOR_DOOR_READY if _player_inside else COLOR_DOOR_IDLE


func _is_interact_pressed() -> bool:
	if InputMap.has_action("interact") and Input.is_action_just_pressed("interact"):
		return true
	return Input.is_action_just_pressed("ui_accept")


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
