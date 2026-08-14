extends Node3D
class_name WalkableAcademyHub

const WalkableAcademyBuildingScene: PackedScene = preload("res://scenes/meta/components/walkable_academy_building.tscn")
const SummonerIconWidgetScene: PackedScene = preload("res://scenes/meta/components/summoner_icon_widget.tscn")

## One destination catalog drives both building entrances and fast shortcuts.
## Entries without a position remain shortcut-only in the current prototype.
const DESTINATIONS: Array[Dictionary] = [
	{
		"id": &"class_hall",
		"name_key": "academy.campus.class_hall.name",
		"description_key": "academy.campus.class_hall.description",
		"placeholder_art_path": "res://assets/placeholders/tiny_swords/buildings/placeholder_class_hall.png",
		"position": Vector3(-12.0, 0.0, -8.0),
	},
	{
		"id": &"shop",
		"name_key": "academy.campus.shop.name",
		"description_key": "academy.campus.shop.description",
		"placeholder_art_path": "res://assets/placeholders/tiny_swords/buildings/placeholder_campus_shop.png",
		"position": Vector3(12.0, 0.0, -7.0),
	},
	{
		"id": &"mission_hall",
		"name_key": "academy.campus.mission_hall.name",
		"description_key": "academy.campus.mission_hall.description",
		"placeholder_art_path": "res://assets/placeholders/tiny_swords/buildings/placeholder_mission_hall.png",
		"position": Vector3(-13.0, 0.0, 7.0),
	},
	{
		"id": &"dorms",
		"name_key": "academy.campus.dorms.name",
		"description_key": "academy.campus.dorms.description",
		"placeholder_art_path": "res://assets/placeholders/tiny_swords/buildings/placeholder_dorms.png",
		"position": Vector3(0.0, 0.0, -11.0),
	},
	{
		"id": &"online",
		"name_key": "academy.campus.online.name",
		"description_key": "academy.campus.online.description",
		"placeholder_art_path": "res://assets/placeholders/tiny_swords/buildings/placeholder_online_arena.png",
		"position": Vector3(13.0, 0.0, 8.0),
	},
	{"id": &"summoner", "name_key": "ui.summoner_screen.title", "description_key": "academy.walkable.summoner_description"},
	{"id": &"settings", "name_key": "ui.nav.settings", "description_key": "academy.walkable.settings_description"},
]

@export var camera_follow_offset: Vector3 = Vector3(0.0, 22.0, 22.0)
@export var camera_follow_focus_height: float = 2.45
@export var camera_follow_lerp_speed: float = 8.0
@export var camera_zoomed_follow_lerp_speed: float = 18.0
@export var camera_min_fov: float = 32.0
@export var camera_max_fov: float = 62.0
@export var camera_zoom_step: float = 4.0
@export var camera_zoom_lerp_speed: float = 10.0
@export var camera_zoom_pitch_enabled: bool = true
@export var camera_zoom_pitch_max_degrees: float = 8.0

@onready var ground_label: Label3D = %GroundLabel
@onready var buildings: Node3D = %Buildings
@onready var camera: Camera3D = %Camera3D
@onready var player: Node3D = %Player
@onready var shortcut_button: Button = %ShortcutButton
@onready var shortcut_panel: PanelContainer = %ShortcutPanel
@onready var shortcut_title: Label = %ShortcutTitle
@onready var shortcut_close_button: Button = %ShortcutCloseButton
@onready var shortcut_list: VBoxContainer = %ShortcutList
@onready var summoner_slot: Control = %SummonerSlot

var _camera_target_fov: float = 46.0
var _camera_default_fov: float = 46.0
var _camera_base_basis: Basis = Basis.IDENTITY
var _camera_focus_position: Vector3 = Vector3.ZERO
var _camera_follow_distance: float = 31.0
var _transition_started: bool = false


func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	ground_label.text = Loc.t("academy.walkable.placeholder_ground")
	shortcut_button.text = Loc.t("academy.walkable.open_shortcuts")
	shortcut_title.text = Loc.t("academy.walkable.shortcuts_title")
	shortcut_close_button.text = Loc.t("ui.common.close")
	shortcut_button.pressed.connect(_toggle_shortcuts)
	shortcut_close_button.pressed.connect(_close_shortcuts)
	_setup_summoner_icon()
	_populate_shortcuts()

	camera.current = true
	_camera_target_fov = clampf(camera.fov, camera_min_fov, camera_max_fov)
	_camera_default_fov = _camera_target_fov
	_camera_base_basis = camera.transform.basis
	_camera_follow_distance = camera_follow_offset.length()
	camera.fov = _camera_target_fov
	_snap_camera_to_player()
	_spawn_buildings()


func _process(delta: float) -> void:
	_follow_player(delta)
	_update_camera_zoom(delta)
	_update_camera_transform()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") and shortcut_panel.visible:
		_close_shortcuts()
		get_viewport().set_input_as_handled()
		return
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if not mouse_event.pressed:
			return
		if mouse_event.button_index == MOUSE_BUTTON_WHEEL_UP:
			_adjust_camera_zoom(-camera_zoom_step)
			get_viewport().set_input_as_handled()
		elif mouse_event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_adjust_camera_zoom(camera_zoom_step)
			get_viewport().set_input_as_handled()
	elif event is InputEventMagnifyGesture:
		var magnify_event: InputEventMagnifyGesture = event
		_adjust_camera_zoom((1.0 - magnify_event.factor) * camera_zoom_step * 2.0)
		get_viewport().set_input_as_handled()
	elif event is InputEventPanGesture:
		var pan_gesture: InputEventPanGesture = event
		_adjust_camera_zoom(pan_gesture.delta.y * camera_zoom_step * 0.25)
		get_viewport().set_input_as_handled()


func _setup_summoner_icon() -> void:
	var summoner_icon: SummonerIconWidget = SummonerIconWidgetScene.instantiate()
	summoner_slot.add_child(summoner_icon)
	summoner_icon.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	summoner_icon.icon_clicked.connect(_route_to.bind(&"summoner"))


func _populate_shortcuts() -> void:
	_clear_children(shortcut_list)
	for destination: Dictionary in DESTINATIONS:
		var destination_id: StringName = destination["id"]
		var button: Button = Button.new()
		button.text = Loc.t(destination["name_key"])
		button.tooltip_text = Loc.t(destination["description_key"])
		button.custom_minimum_size = Vector2(280.0, 48.0)
		button.pressed.connect(_route_to.bind(destination_id))
		shortcut_list.add_child(button)


func _toggle_shortcuts() -> void:
	shortcut_panel.visible = not shortcut_panel.visible


func _close_shortcuts() -> void:
	shortcut_panel.hide()


func _route_to(destination_id: StringName) -> void:
	if _transition_started:
		return
	var target_scene: String = _scene_for_destination(destination_id)
	if target_scene.is_empty():
		push_warning("WalkableAcademyHub: Unknown destination '%s'" % destination_id)
		return
	_transition_started = true
	NavigationContext.push_return(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)
	SceneManager.transition_to(target_scene)


func _scene_for_destination(destination_id: StringName) -> String:
	match destination_id:
		&"class_hall":
			return SceneManager.SCENE_ACADEMY_CLASS_HALL
		&"shop":
			return SceneManager.SCENE_SHOP_SCREEN
		&"mission_hall":
			return SceneManager.SCENE_SPECIAL_EVENTS
		&"dorms":
			return SceneManager.SCENE_COLLECTION_SCREEN
		&"online":
			return SceneManager.SCENE_ONLINE
		&"summoner":
			return SceneManager.SCENE_SUMMONER_SCREEN
		&"settings":
			return SceneManager.SCENE_SETTINGS
	return ""


func _follow_player(delta: float) -> void:
	if not is_instance_valid(player):
		return
	var target_focus: Vector3 = _get_player_focus_position()
	var zoom_ratio: float = _get_camera_zoom_pitch_ratio()
	var follow_speed: float = lerpf(camera_follow_lerp_speed, camera_zoomed_follow_lerp_speed, zoom_ratio)
	var follow_weight: float = 1.0 - exp(-follow_speed * delta)
	_camera_focus_position = _camera_focus_position.lerp(target_focus, follow_weight)


func _update_camera_zoom(delta: float) -> void:
	var zoom_weight: float = 1.0 - exp(-camera_zoom_lerp_speed * delta)
	camera.fov = lerpf(camera.fov, _camera_target_fov, zoom_weight)


func _adjust_camera_zoom(fov_delta: float) -> void:
	_camera_target_fov = clampf(_camera_target_fov + fov_delta, camera_min_fov, camera_max_fov)


func _update_camera_transform() -> void:
	var pitch_radians: float = deg_to_rad(camera_zoom_pitch_max_degrees * _get_camera_zoom_pitch_ratio())
	var pitched_basis: Basis = Transform3D(_camera_base_basis, Vector3.ZERO).rotated_local(Vector3.RIGHT, pitch_radians).basis
	var forward: Vector3 = -pitched_basis.z.normalized()
	camera.global_transform = Transform3D(pitched_basis, _camera_focus_position - forward * _camera_follow_distance)


func _get_camera_zoom_pitch_ratio() -> float:
	if not camera_zoom_pitch_enabled or camera.fov >= _camera_default_fov:
		return 0.0
	var zoom_span: float = _camera_default_fov - camera_min_fov
	if zoom_span <= 0.001:
		return 0.0
	return clampf((_camera_default_fov - camera.fov) / zoom_span, 0.0, 1.0)


func _snap_camera_to_player() -> void:
	if not is_instance_valid(player):
		return
	_camera_focus_position = _get_player_focus_position()
	_update_camera_transform()


func _get_player_focus_position() -> Vector3:
	var focus: Vector3 = player.global_position
	focus.y = camera_follow_focus_height
	return focus


func _spawn_buildings() -> void:
	_clear_children(buildings)
	for destination: Dictionary in DESTINATIONS:
		if not destination.has("position"):
			continue
		_add_building(
			destination["name_key"],
			_scene_for_destination(destination["id"]),
			load(destination["placeholder_art_path"]) as Texture2D,
			destination["position"]
		)


func _add_building(
	display_name_key: String,
	target_scene_path: String,
	placeholder_texture: Texture2D,
	map_position: Vector3
) -> void:
	var building: WalkableAcademyBuilding = WalkableAcademyBuildingScene.instantiate()
	if building == null:
		push_error("WalkableAcademyHub: Failed to instantiate walkable academy building")
		return
	building.position = map_position
	building.configure(
		display_name_key,
		target_scene_path,
		SceneManager.SCENE_WALKABLE_ACADEMY_HUB,
		placeholder_texture
	)
	buildings.add_child(building)


func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()


func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
