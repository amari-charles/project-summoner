extends Node3D
class_name WalkableAcademyBuilding

const COLOR_ART_IDLE: Color = Color(0.90, 0.90, 0.90, 1.0)
const COLOR_ART_READY: Color = Color.WHITE

@export var display_name_key: String = ""
@export var target_scene_path: String = ""
@export var return_scene_path: String = ""
@export var placeholder_art_pixel_size: float = 0.035

@onready var name_label: Label3D = %NameLabel
@onready var placeholder_label: Label3D = %PlaceholderLabel
@onready var door_area: Area3D = %DoorArea
@onready var placeholder_art: Sprite3D = %PlaceholderBuildingArt

var _player_inside: bool = false
var _transition_started: bool = false
var _placeholder_texture: Texture2D = null


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
	placeholder_texture: Texture2D
) -> void:
	display_name_key = name_key
	target_scene_path = target_scene
	return_scene_path = return_scene
	_placeholder_texture = placeholder_texture
	if is_inside_tree():
		_refresh_placeholder_art()
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
	if placeholder_art:
		placeholder_art.modulate = COLOR_ART_READY if _player_inside else COLOR_ART_IDLE


func _refresh_placeholder_art() -> void:
	if not placeholder_art or _placeholder_texture == null:
		return
	placeholder_art.texture = _placeholder_texture
	placeholder_art.pixel_size = placeholder_art_pixel_size
	var art_height: float = _placeholder_texture.get_height() * placeholder_art_pixel_size
	placeholder_art.position.y = art_height * 0.5
	placeholder_art.offset = Vector2.ZERO
	CutoutRenderOrder.apply_from_feet(placeholder_art, global_position.z)
	placeholder_label.position.y = art_height + 0.2
	name_label.position.y = art_height + 1.0


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
