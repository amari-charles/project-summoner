extends Node3D
class_name WalkableAcademyHub

const UI_SHOWCASE_QUEST_ID: String = "introduction_to_magic"

const WalkableAcademyBuildingScene: PackedScene = preload("res://scenes/meta/components/walkable_academy_building.tscn")
const SummonerIconWidgetScene: PackedScene = preload("res://scenes/meta/components/summoner_icon_widget.tscn")
const InteractiveNpcScene: PackedScene = preload("res://scenes/meta/components/interactive_npc.tscn")
const QuestWorldTargetScene: PackedScene = preload("res://scenes/meta/components/quest_world_target.tscn")
const ObjectivePathTrailScript: Script = preload("res://scripts/meta/components/objective_path_trail.gd")
const PLACEHOLDER_CAMPUS_SHOP: Texture2D = preload("res://assets/placeholders/tiny_swords/buildings/placeholder_campus_shop.png")
const PLACEHOLDER_MISSION_HALL: Texture2D = preload("res://assets/placeholders/tiny_swords/buildings/placeholder_mission_hall.png")
const PLACEHOLDER_ONLINE_ARENA: Texture2D = preload("res://assets/placeholders/tiny_swords/buildings/placeholder_online_arena.png")
const PLACEHOLDER_GROUND_CENTER: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_color3.png")
const PLACEHOLDER_GROUND_TOP_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top_left.png")
const PLACEHOLDER_GROUND_TOP: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top.png")
const PLACEHOLDER_GROUND_TOP_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top_right.png")
const PLACEHOLDER_GROUND_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_left.png")
const PLACEHOLDER_GROUND_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_right.png")
const PLACEHOLDER_GROUND_BOTTOM_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom_left.png")
const PLACEHOLDER_GROUND_BOTTOM: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom.png")
const PLACEHOLDER_GROUND_BOTTOM_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom_right.png")
const PLACEHOLDER_CLIFF_MIDDLE_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle_left.png")
const PLACEHOLDER_CLIFF_MIDDLE: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle.png")
const PLACEHOLDER_CLIFF_MIDDLE_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle_right.png")
const PLACEHOLDER_WATER_BACKGROUND: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_water_background.png")
const PLACEHOLDER_WATER_FOAM: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_water_foam.png")
const PLACEHOLDER_WATER_FOAM_SHADER: Shader = preload("res://shaders/meta/placeholder_water_foam.gdshader")
const WATER_FOAM_FRAME_COUNT: int = 16
const WATER_FOAM_TILE_SPAN: float = 3.0
const CITY_GRAYBOX_ENABLED: bool = true
const CITY_GRAYBOX_SIZE: Vector2 = Vector2(160.0, 120.0)

const DESTINATION_SHOP: StringName = &"shop"
const DESTINATION_MISSION_HALL: StringName = &"mission_hall"
const DESTINATION_SPELLBOOK: StringName = &"spellbook"
const DESTINATION_ONLINE: StringName = &"online"
const DESTINATION_SUMMONER: StringName = &"summoner"
const DESTINATION_JOURNAL: StringName = &"journal"

const PROFESSOR_POSITIONS: Dictionary = {
	"general_magic": Vector3(0.0, 0.0, -38.0),
	"fire": Vector3(-43.0, 0.0, 4.0),
	"water": Vector3(43.0, 0.0, -17.0),
	"earth": Vector3(-52.0, 0.0, -12.0),
	"wind": Vector3(42.0, 0.0, -41.0),
}

## Physical world locations own both their building interaction and arrival
## waypoint. Persistent UI actions intentionally live outside this catalog.
const WORLD_LOCATIONS: Array[Dictionary] = [
	{
		"id": DESTINATION_SHOP,
		"name_key": "academy.campus.shop.name",
		"description_key": "academy.campus.shop.description",
		"target_scene": SceneManagerClass.SCENE_SHOP_SCREEN,
		"placeholder_texture": PLACEHOLDER_CAMPUS_SHOP,
		"position": Vector3(-21.0, 0.0, 42.0),
		"travel_position": Vector3(-21.0, 1.2, 47.0),
	},
	{
		"id": DESTINATION_MISSION_HALL,
		"name_key": "academy.campus.mission_hall.name",
		"description_key": "academy.campus.mission_hall.description",
		"target_scene": SceneManagerClass.SCENE_SPECIAL_EVENTS,
		"placeholder_texture": PLACEHOLDER_MISSION_HALL,
		"position": Vector3(-34.0, 0.0, -32.0),
		"travel_position": Vector3(-34.0, 1.2, -27.0),
	},
	{
		"id": DESTINATION_ONLINE,
		"name_key": "academy.campus.online.name",
		"description_key": "academy.campus.online.description",
		"target_scene": SceneManagerClass.SCENE_ONLINE,
		"placeholder_texture": PLACEHOLDER_ONLINE_ARENA,
		"position": Vector3(58.0, 0.0, 43.0),
		"travel_position": Vector3(58.0, 1.2, 48.0),
	},
]

const DIRECT_UI_DESTINATIONS: Array[Dictionary] = []

@export_category("Placeholder Ground")
@export_range(0.5, 20.0, 0.25) var ground_tile_world_size: float = 2.25
@export var ground_tint: Color = Color(0.78, 0.8, 0.76, 1.0)

@export_category("Placeholder Water")
@export_range(1, 40, 1) var water_margin_tiles: int = 20
@export_range(1.0, 24.0, 0.5) var water_foam_fps: float = 8.0
@export var water_tint: Color = Color.WHITE

@export_category("Camera")
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

@onready var ground: MeshInstance3D = %Ground
@onready var placeholder_water: Node3D = %PlaceholderWater
@onready var ground_label: Label3D = %GroundLabel
@onready var buildings: Node3D = %Buildings
@onready var professors: Node3D = %Professors
@onready var quest_targets: Node3D = %QuestTargets
@onready var camera: Camera3D = %Camera3D
@onready var player: Node3D = %Player
@onready var travel_button: Button = %TravelButton
@onready var travel_panel: PanelContainer = %TravelPanel
@onready var travel_title: Label = %TravelTitle
@onready var travel_close_button: Button = %TravelCloseButton
@onready var travel_list: VBoxContainer = %TravelList
@onready var summoner_slot: Control = %SummonerSlot
@onready var tracked_quest_banner: Control = %TrackedQuestBanner
@onready var tracked_quest_button: Button = %TrackedQuestButton
@onready var spellbook_button: Button = %SpellbookButton
@onready var journal_button: Button = %JournalButton
@onready var inventory_button: Button = %InventoryButton
@onready var inventory_overlay: InventoryOverlay = %InventoryOverlay
@onready var summoner_profile: SummonerScreen = %SummonerProfile
@onready var collection_overlay: CollectionScreen = %CollectionOverlay
@onready var journal_overlay: QuestJournal = %JournalOverlay
@onready var dialogue_box: NpcDialogueBox = %NpcDialogueBox
@onready var reward_modal: RewardGrantModal = %RewardGrantModal
@onready var showcase_complete_dialog: AcceptDialog = %ShowcaseCompleteDialog
@onready var quest_offer_modal: QuestOfferModal = %QuestOfferModal
@onready var campus_system_menu: CampusSystemMenu = %CampusSystemMenu

var _camera_target_fov: float = 46.0
var _camera_default_fov: float = 46.0
var _camera_base_basis: Basis = Basis.IDENTITY
var _camera_focus_position: Vector3 = Vector3.ZERO
var _camera_follow_distance: float = 31.0
var _ground_source_size: Vector2 = Vector2.ZERO
var _transition_started: bool = false
var _dialog_quest_id: String = ""
var _dialog_speaker: String = ""
var _dialog_npc_id: String = ""
var _dialog_accepted_lines: Array[String] = []
var _dialog_turn_in_npc_id: String = ""
var _dialog_response_actions: Dictionary = {}
var _dialog_response_quest_ids: Dictionary = {}
var _dialog_opportunities_by_id: Dictionary = {}
var _dialog_offer_lines: Array[String] = []
var _dialog_offer_responses: Array[Dictionary] = []
var _objective_path_trail: ObjectivePathTrail = null
var _show_showcase_complete_after_rewards: bool = false


func _ready() -> void:
	if CITY_GRAYBOX_ENABLED:
		_configure_city_graybox_ground()
	else:
		_configure_placeholder_ground()
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	ground_label.text = "ACADEMY CITY GRAYBOX"
	travel_button.tooltip_text = Loc.t("academy.walkable.open_travel")
	travel_title.text = Loc.t("academy.walkable.travel_title")
	travel_close_button.text = Loc.t("ui.common.close")
	travel_button.pressed.connect(_toggle_travel)
	travel_close_button.pressed.connect(_close_travel)
	journal_button.tooltip_text = Loc.t("academy.journal.title")
	spellbook_button.tooltip_text = Loc.t("academy.campus.spellbook.name")
	inventory_button.tooltip_text = Loc.t("academy.walkable.inventory")
	spellbook_button.pressed.connect(_open_collection)
	journal_button.pressed.connect(_open_journal)
	inventory_button.pressed.connect(_open_inventory)
	summoner_profile.closed.connect(_on_utility_overlay_closed)
	collection_overlay.closed.connect(_on_utility_overlay_closed)
	journal_overlay.closed.connect(_on_utility_overlay_closed)
	inventory_overlay.closed.connect(_on_utility_overlay_closed)
	tracked_quest_button.pressed.connect(_open_journal)
	dialogue_box.choice_selected.connect(_on_dialogue_choice)
	dialogue_box.closed.connect(_on_dialogue_closed)
	reward_modal.closed.connect(_on_reward_modal_closed)
	showcase_complete_dialog.title = Loc.t("academy.quest.ui_showcase.complete_title")
	showcase_complete_dialog.dialog_text = Loc.t("academy.quest.ui_showcase.complete_message")
	showcase_complete_dialog.ok_button_text = Loc.t("academy.quest.ui_showcase.complete_action")
	showcase_complete_dialog.confirmed.connect(_on_showcase_complete_dialog_closed)
	showcase_complete_dialog.canceled.connect(_on_showcase_complete_dialog_closed)
	quest_offer_modal.accepted.connect(_on_quest_offer_accepted)
	quest_offer_modal.backed.connect(_on_quest_offer_backed)
	quest_offer_modal.cancelled.connect(_on_quest_offer_cancelled)
	if Quests.has_signal("ProgressChanged"):
		Quests.connect("ProgressChanged", _refresh_quest_presentation)
	_setup_summoner_icon()
	_populate_travel_points()

	camera.current = true
	_camera_target_fov = clampf(camera.fov, camera_min_fov, camera_max_fov)
	_camera_default_fov = _camera_target_fov
	_camera_base_basis = camera.transform.basis
	_camera_follow_distance = camera_follow_offset.length()
	camera.fov = _camera_target_fov
	_snap_camera_to_player()
	_spawn_buildings()
	_spawn_professors()
	_spawn_quest_targets()
	_setup_objective_path_trail()
	_refresh_quest_presentation()


func _configure_city_graybox_ground() -> void:
	var ground_plane: PlaneMesh = ground.mesh as PlaneMesh
	var ground_material: StandardMaterial3D = ground.material_override as StandardMaterial3D
	if ground_plane == null or ground_material == null:
		push_error("WalkableAcademyHub: City graybox requires a plane and standard material")
		return
	ground_plane.size = CITY_GRAYBOX_SIZE
	ground_material.albedo_color = Color(0.34, 0.34, 0.36, 1.0)
	ground_material.albedo_texture = null
	ground_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	placeholder_water.visible = false


func _configure_placeholder_ground() -> void:
	if ground_tile_world_size <= 0.0:
		push_error("WalkableAcademyHub: Ground tile world size must be greater than zero")
		return
	var ground_plane: PlaneMesh = ground.mesh as PlaneMesh
	var source_material: StandardMaterial3D = ground.material_override as StandardMaterial3D
	if ground_plane == null or source_material == null:
		push_error("WalkableAcademyHub: Placeholder ground requires a PlaneMesh and StandardMaterial3D")
		return
	if _ground_source_size == Vector2.ZERO:
		_ground_source_size = ground_plane.size
	var ground_size: Vector2 = _ground_source_size
	var column_count: int = floori(ground_size.x / ground_tile_world_size)
	var row_count: int = floori(ground_size.y / ground_tile_world_size)
	if column_count < 3 or row_count < 3:
		push_error("WalkableAcademyHub: Ground is too small for the configured tile size")
		return
	var border_size: float = ground_tile_world_size
	var inner_columns: int = column_count - 2
	var inner_rows: int = row_count - 2
	var inner_size: Vector2 = Vector2(
		inner_columns * ground_tile_world_size,
		inner_rows * ground_tile_world_size
	)
	var rendered_size: Vector2 = Vector2(
		column_count * ground_tile_world_size,
		row_count * ground_tile_world_size
	)

	_clear_ground_border()
	_configure_ground_piece(
		ground,
		inner_size,
		Vector3.ZERO,
		PLACEHOLDER_GROUND_CENTER,
		Vector2(inner_columns, inner_rows),
		source_material
	)
	_add_ground_piece(
		"TopEdge",
		Vector2(inner_size.x, border_size),
		Vector3(0.0, 0.0, -rendered_size.y * 0.5 + border_size * 0.5),
		PLACEHOLDER_GROUND_TOP,
		Vector2(inner_columns, 1.0),
		source_material
	)
	_add_ground_piece(
		"BottomEdge",
		Vector2(inner_size.x, border_size),
		Vector3(0.0, 0.0, rendered_size.y * 0.5 - border_size * 0.5),
		PLACEHOLDER_GROUND_BOTTOM,
		Vector2(inner_columns, 1.0),
		source_material
	)
	_add_ground_piece(
		"LeftEdge",
		Vector2(border_size, inner_size.y),
		Vector3(-rendered_size.x * 0.5 + border_size * 0.5, 0.0, 0.0),
		PLACEHOLDER_GROUND_LEFT,
		Vector2(1.0, inner_rows),
		source_material
	)
	_add_ground_piece(
		"RightEdge",
		Vector2(border_size, inner_size.y),
		Vector3(rendered_size.x * 0.5 - border_size * 0.5, 0.0, 0.0),
		PLACEHOLDER_GROUND_RIGHT,
		Vector2(1.0, inner_rows),
		source_material
	)
	_add_ground_corner(
		"TopLeftCorner",
		Vector3(
			-rendered_size.x * 0.5 + border_size * 0.5,
			0.0,
			-rendered_size.y * 0.5 + border_size * 0.5
		),
		PLACEHOLDER_GROUND_TOP_LEFT,
		border_size,
		source_material
	)
	_add_ground_corner(
		"TopRightCorner",
		Vector3(
			rendered_size.x * 0.5 - border_size * 0.5,
			0.0,
			-rendered_size.y * 0.5 + border_size * 0.5
		),
		PLACEHOLDER_GROUND_TOP_RIGHT,
		border_size,
		source_material
	)
	_add_ground_corner(
		"BottomLeftCorner",
		Vector3(
			-rendered_size.x * 0.5 + border_size * 0.5,
			0.0,
			rendered_size.y * 0.5 - border_size * 0.5
		),
		PLACEHOLDER_GROUND_BOTTOM_LEFT,
		border_size,
		source_material
	)
	_add_ground_corner(
		"BottomRightCorner",
		Vector3(
			rendered_size.x * 0.5 - border_size * 0.5,
			0.0,
			rendered_size.y * 0.5 - border_size * 0.5
		),
		PLACEHOLDER_GROUND_BOTTOM_RIGHT,
		border_size,
		source_material
	)
	var grass_front_edge: float = rendered_size.y * 0.5
	_add_ground_cliff_row(
		"FrontCliff",
		-border_size * 0.5,
		grass_front_edge,
		inner_size.x,
		border_size,
		PLACEHOLDER_CLIFF_MIDDLE_LEFT,
		PLACEHOLDER_CLIFF_MIDDLE,
		PLACEHOLDER_CLIFF_MIDDLE_RIGHT,
		source_material
	)
	_configure_placeholder_water(rendered_size, column_count, row_count)


func _configure_placeholder_water(
	rendered_size: Vector2,
	column_count: int,
	row_count: int
) -> void:
	for child: Node in placeholder_water.get_children():
		child.free()

	var water_level: float = -ground_tile_world_size - 0.05
	var water_size: Vector2 = rendered_size + Vector2.ONE * ground_tile_world_size * water_margin_tiles * 2.0
	var surface: MeshInstance3D = MeshInstance3D.new()
	surface.name = "Surface"
	var surface_mesh: PlaneMesh = PlaneMesh.new()
	surface_mesh.size = water_size
	surface.mesh = surface_mesh
	surface.position.y = water_level
	var surface_material: StandardMaterial3D = StandardMaterial3D.new()
	surface_material.albedo_color = water_tint
	surface_material.albedo_texture = PLACEHOLDER_WATER_BACKGROUND
	surface_material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	surface_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	surface_material.uv1_scale = Vector3(
		water_size.x / ground_tile_world_size,
		water_size.y / ground_tile_world_size,
		1.0
	)
	surface.material_override = surface_material
	placeholder_water.add_child(surface)

	var foam: Node3D = Node3D.new()
	foam.name = "Foam"
	placeholder_water.add_child(foam)
	# Tiny Swords foam spans three source tiles but is centered on one shoreline
	# cell. The raised terrain hides its center, leaving the overlapping waves.
	var foam_index: int = 0
	var left: float = -rendered_size.x * 0.5 + ground_tile_world_size * 0.5
	var right: float = rendered_size.x * 0.5 - ground_tile_world_size * 0.5
	var top: float = -rendered_size.y * 0.5 + ground_tile_world_size * 0.5
	var bottom: float = rendered_size.y * 0.5 - ground_tile_world_size * 0.5
	for column: int in column_count:
		var x_position: float = left + column * ground_tile_world_size
		_add_water_foam_piece(
			foam,
			"Top%d" % column,
			Vector3(x_position, water_level + 0.02, top),
			foam_index
		)
		foam_index += 1
		_add_water_foam_piece(
			foam,
			"Bottom%d" % column,
			Vector3(x_position, water_level + 0.02, bottom),
			foam_index
		)
		foam_index += 1
	for row: int in range(1, row_count - 1):
		var z_position: float = top + row * ground_tile_world_size
		_add_water_foam_piece(
			foam,
			"Left%d" % row,
			Vector3(left, water_level + 0.02, z_position),
			foam_index
		)
		foam_index += 1
		_add_water_foam_piece(
			foam,
			"Right%d" % row,
			Vector3(right, water_level + 0.02, z_position),
			foam_index
		)
		foam_index += 1


func _add_water_foam_piece(
	parent: Node3D,
	piece_name: String,
	piece_position: Vector3,
	animation_offset: int
) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	var piece_mesh: PlaneMesh = PlaneMesh.new()
	piece_mesh.size = Vector2.ONE * ground_tile_world_size * WATER_FOAM_TILE_SPAN
	piece.mesh = piece_mesh
	piece.position = piece_position
	var piece_material: ShaderMaterial = ShaderMaterial.new()
	piece_material.shader = PLACEHOLDER_WATER_FOAM_SHADER
	piece_material.set_shader_parameter("foam_texture", PLACEHOLDER_WATER_FOAM)
	piece_material.set_shader_parameter("animation_fps", water_foam_fps)
	piece_material.set_shader_parameter("animation_offset", float(animation_offset % WATER_FOAM_FRAME_COUNT))
	piece.material_override = piece_material
	parent.add_child(piece)


func _clear_ground_border() -> void:
	for child: Node in ground.get_children():
		if child.is_in_group("placeholder_ground_border"):
			child.queue_free()


func _add_ground_corner(
	piece_name: String,
	piece_position: Vector3,
	texture: Texture2D,
	border_size: float,
	source_material: StandardMaterial3D
) -> void:
	_add_ground_piece(
		piece_name,
		Vector2.ONE * border_size,
		piece_position,
		texture,
		Vector2.ONE,
		source_material
	)


func _add_ground_cliff_row(
	piece_name: String,
	y_position: float,
	z_position: float,
	inner_width: float,
	tile_size: float,
	left_texture: Texture2D,
	middle_texture: Texture2D,
	right_texture: Texture2D,
	source_material: StandardMaterial3D
) -> void:
	_add_ground_cliff_piece(
		piece_name + "Left",
		Vector2.ONE * tile_size,
		Vector3(-inner_width * 0.5 - tile_size * 0.5, y_position, z_position),
		left_texture,
		Vector2.ONE,
		source_material
	)
	_add_ground_cliff_piece(
		piece_name + "Center",
		Vector2(inner_width, tile_size),
		Vector3(0.0, y_position, z_position),
		middle_texture,
		Vector2(inner_width / tile_size, 1.0),
		source_material
	)
	_add_ground_cliff_piece(
		piece_name + "Right",
		Vector2.ONE * tile_size,
		Vector3(inner_width * 0.5 + tile_size * 0.5, y_position, z_position),
		right_texture,
		Vector2.ONE,
		source_material
	)


func _add_ground_cliff_piece(
	piece_name: String,
	piece_size: Vector2,
	piece_position: Vector3,
	texture: Texture2D,
	uv_scale: Vector2,
	source_material: StandardMaterial3D
) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	piece.add_to_group("placeholder_ground_border")
	ground.add_child(piece)
	var piece_mesh: QuadMesh = QuadMesh.new()
	piece_mesh.size = piece_size
	piece.mesh = piece_mesh
	piece.position = piece_position
	var piece_material: StandardMaterial3D = source_material.duplicate() as StandardMaterial3D
	piece_material.albedo_color = ground_tint
	piece_material.albedo_texture = texture
	piece_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
	piece_material.alpha_scissor_threshold = 0.5
	piece_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	piece_material.uv1_scale = Vector3(uv_scale.x, uv_scale.y, 1.0)
	piece.material_override = piece_material


func _add_ground_piece(
	piece_name: String,
	piece_size: Vector2,
	piece_position: Vector3,
	texture: Texture2D,
	uv_scale: Vector2,
	source_material: StandardMaterial3D
) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	piece.add_to_group("placeholder_ground_border")
	ground.add_child(piece)
	_configure_ground_piece(piece, piece_size, piece_position, texture, uv_scale, source_material)


func _configure_ground_piece(
	piece: MeshInstance3D,
	piece_size: Vector2,
	piece_position: Vector3,
	texture: Texture2D,
	uv_scale: Vector2,
	source_material: StandardMaterial3D
) -> void:
	var piece_mesh: PlaneMesh = PlaneMesh.new()
	piece_mesh.size = piece_size
	piece.mesh = piece_mesh
	piece.position = piece_position
	var piece_material: StandardMaterial3D = source_material.duplicate() as StandardMaterial3D
	piece_material.albedo_color = ground_tint
	piece_material.albedo_texture = texture
	if piece == ground:
		piece_material.transparency = BaseMaterial3D.TRANSPARENCY_DISABLED
	else:
		piece_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
		piece_material.alpha_scissor_threshold = 0.5
	piece_material.uv1_scale = Vector3(uv_scale.x, uv_scale.y, 1.0)
	piece.material_override = piece_material


func _process(delta: float) -> void:
	_follow_player(delta)
	_update_camera_zoom(delta)
	_update_camera_transform()


func _unhandled_input(event: InputEvent) -> void:
	if _utility_overlay_visible():
		return
	if event.is_action_pressed("ui_cancel") and travel_panel.visible:
		_close_travel()
		get_viewport().set_input_as_handled()
		return
	if event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		campus_system_menu.open_menu()
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
	summoner_icon.icon_clicked.connect(_open_summoner_profile)


func _open_summoner_profile() -> void:
	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if summoner_id.is_empty():
		return
	_pause_world_for_utility()
	summoner_profile.open_profile(summoner_id)


func _open_collection() -> void:
	_pause_world_for_utility()
	collection_overlay.open_collection()


func _open_journal() -> void:
	_pause_world_for_utility()
	journal_overlay.open_journal()


func _pause_world_for_utility() -> void:
	_close_travel()
	player.velocity = Vector3.ZERO
	player.set_physics_process(false)


func _on_utility_overlay_closed() -> void:
	if not _utility_overlay_visible():
		player.set_physics_process(true)
		_refresh_quest_presentation()


func _utility_overlay_visible() -> bool:
	return (
		summoner_profile.visible
		or collection_overlay.visible
		or journal_overlay.visible
		or inventory_overlay.visible
	)


func _open_inventory() -> void:
	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if not summoner_id.is_empty():
		_pause_world_for_utility()
		inventory_overlay.open_inventory(summoner_id)


func _populate_travel_points() -> void:
	_clear_children(travel_list)
	var objective_travel: Dictionary = _tracked_objective_travel()
	if not objective_travel.is_empty():
		var objective_waypoint_id: StringName = objective_travel["waypoint_id"]
		var objective_button: Button = Button.new()
		objective_button.text = Loc.t("academy.walkable.current_objective", {
			"objective": SafeTypeUtils.string(objective_travel.get("objective")),
		})
		objective_button.custom_minimum_size = Vector2(280.0, 52.0)
		objective_button.pressed.connect(_travel_to_world_location.bind(objective_waypoint_id))
		travel_list.add_child(objective_button)
	for location: Dictionary in WORLD_LOCATIONS:
		var destination_id: StringName = location["id"]
		var button: Button = Button.new()
		button.text = Loc.t(location["name_key"])
		button.tooltip_text = Loc.t(location["description_key"])
		button.custom_minimum_size = Vector2(280.0, 48.0)
		button.pressed.connect(_travel_to_world_location.bind(destination_id))
		travel_list.add_child(button)


func _toggle_travel() -> void:
	if not travel_panel.visible:
		_populate_travel_points()
	travel_panel.visible = not travel_panel.visible


func _close_travel() -> void:
	travel_panel.hide()


func _travel_to_world_location(destination_id: StringName) -> void:
	var location: Dictionary = _world_location(destination_id)
	if location.is_empty() or not location.has("travel_position"):
		push_warning("WalkableAcademyHub: Unknown travel point '%s'" % destination_id)
		return
	player.velocity = Vector3.ZERO
	player.global_position = SafeTypeUtils.vector3(
		location.get("travel_position"), player.global_position
	)
	_close_travel()
	_snap_camera_to_player()


func _tracked_objective_travel() -> Dictionary:
	var journal: Dictionary = QuestApi.get_journal_state()
	var tracked_id: String = SafeTypeUtils.string(journal.get("tracked_quest_id"))
	if tracked_id.is_empty():
		return {}
	for value: Variant in SafeTypeUtils.array(journal.get("active")):
		var quest: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(quest.get("id")) != tracked_id:
			continue
		var target_position: Variant = _quest_target_position(
			SafeTypeUtils.string(quest.get("current_target_id"))
		)
		if target_position is Vector3:
			return {
				"waypoint_id": _nearest_world_location_id(target_position),
				"objective": Loc.t(SafeTypeUtils.string(quest.get("current_objective_key"))),
			}
	return {}


func _quest_target_position(target_id: String) -> Variant:
	if PROFESSOR_POSITIONS.has(target_id):
		return PROFESSOR_POSITIONS[target_id]
	for child: Node in quest_targets.get_children():
		var target: QuestWorldTarget = child as QuestWorldTarget
		if target != null and target.target_id == target_id:
			return target.global_position
	var location: Dictionary = _world_location(StringName(target_id))
	if not location.is_empty():
		return location.get("position")
	return null


func _nearest_world_location_id(target_position: Vector3) -> StringName:
	var nearest_id: StringName = &""
	var nearest_distance: float = INF
	for location: Dictionary in WORLD_LOCATIONS:
		var anchor: Vector3 = SafeTypeUtils.vector3(location.get("travel_position"))
		var distance: float = anchor.distance_squared_to(target_position)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest_id = location["id"]
	return nearest_id


func _world_location(destination_id: StringName) -> Dictionary:
	for location: Dictionary in WORLD_LOCATIONS:
		if location["id"] == destination_id:
			return location
	return {}


func _route_to(destination_id: StringName) -> void:
	if _transition_started:
		return
	var target_scene: String = _scene_for_destination(destination_id)
	if target_scene.is_empty():
		push_warning("WalkableAcademyHub: Unknown destination '%s'" % destination_id)
		return
	_transition_started = true
	NavigationContext.push_return(SceneManager.SCENE_ACADEMY_CAMPUS)
	SceneManager.transition_to(target_scene)


func _scene_for_destination(destination_id: StringName) -> String:
	for destination: Dictionary in WORLD_LOCATIONS:
		if destination["id"] == destination_id:
			return destination["target_scene"]
	for destination: Dictionary in DIRECT_UI_DESTINATIONS:
		if destination["id"] == destination_id:
			return destination["target_scene"]
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
	for destination: Dictionary in WORLD_LOCATIONS:
		_add_building(
			SafeTypeUtils.string(destination["id"]),
			destination["name_key"],
			destination["target_scene"],
			destination["placeholder_texture"],
			destination["position"]
		)


func _spawn_professors() -> void:
	_clear_children(professors)
	for value: Variant in QuestApi.get_professor_quest_states():
		var state: Dictionary = SafeTypeUtils.dict(value)
		var professor_id: String = SafeTypeUtils.string(state.get("id"))
		if not PROFESSOR_POSITIONS.has(professor_id):
			continue
		var quest_state: Dictionary = QuestApi.get_npc_quest_state(professor_id)
		if not quest_state.is_empty():
			state["quest_marker"] = quest_state.get("quest_marker", "")
		var professor: InteractiveNpc = InteractiveNpcScene.instantiate()
		professor.position = PROFESSOR_POSITIONS[professor_id]
		professor.interacted.connect(_on_professor_interacted)
		professors.add_child(professor)
		_configure_professor(professor, state)


func _spawn_quest_targets() -> void:
	_clear_children(quest_targets)
	var practice_grounds: QuestWorldTarget = QuestWorldTargetScene.instantiate() as QuestWorldTarget
	practice_grounds.position = Vector3(-44.0, 0.0, 20.0) \
		if CITY_GRAYBOX_ENABLED else Vector3(6.0, 0.0, 8.0)
	practice_grounds.configure(
		"practice_grounds",
		Loc.t("quest.world.practice_grounds")
	)
	practice_grounds.interacted.connect(_on_quest_world_target_interacted)
	quest_targets.add_child(practice_grounds)


func _refresh_quest_presentation() -> void:
	QuestGuidance.clear()
	var state_by_id: Dictionary = {}
	for value: Variant in QuestApi.get_professor_quest_states():
		var state: Dictionary = SafeTypeUtils.dict(value)
		state_by_id[SafeTypeUtils.string(state.get("id"))] = state
	for child: Node in professors.get_children():
		var professor: InteractiveNpc = child as InteractiveNpc
		if professor != null and state_by_id.has(professor.npc_id):
			var professor_state: Dictionary = state_by_id[professor.npc_id]
			var quest_state: Dictionary = QuestApi.get_npc_quest_state(professor.npc_id)
			if not quest_state.is_empty():
				professor_state["quest_marker"] = quest_state.get("quest_marker", "")
			_configure_professor(professor, professor_state)

	var journal: Dictionary = QuestApi.get_journal_state()
	var current_target_id: String = ""
	for value: Variant in SafeTypeUtils.array(journal.get("active")):
		var active_quest: Dictionary = SafeTypeUtils.dict(value)
		var step_kind: String = SafeTypeUtils.string(active_quest.get("current_step_kind"))
		if step_kind in ["interact_with_world_target", "complete_encounter"]:
			current_target_id = SafeTypeUtils.string(active_quest.get("current_target_id"))
			break
	if current_target_id.is_empty() and QuestGuidance.is_target_active("battle_settings"):
		current_target_id = "practice_grounds"
	for child: Node in quest_targets.get_children():
		var target: QuestWorldTarget = child as QuestWorldTarget
		if target != null:
			target.set_current_objective(target.target_id == current_target_id)
	for child: Node in buildings.get_children():
		var building: WalkableAcademyBuilding = child as WalkableAcademyBuilding
		if building != null:
			building.set_current_objective(
				building.destination_id == QuestGuidance.current_target_id()
			)
	_show_current_ui_guidance(QuestGuidance.current_target_id())
	_refresh_objective_path(_world_guidance_target_id())
	var tracked_id: String = SafeTypeUtils.string(journal.get("tracked_quest_id"))
	tracked_quest_banner.visible = not tracked_id.is_empty()
	if tracked_id.is_empty():
		return
	for value: Variant in SafeTypeUtils.array(journal.get("active")):
		var quest: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(quest.get("id")) != tracked_id:
			continue
		var title: String = Loc.t(SafeTypeUtils.string(quest.get("title_key")))
		var objective: String = Loc.t(SafeTypeUtils.string(quest.get("current_objective_key")))
		tracked_quest_button.text = title if objective.is_empty() else "%s — %s" % [title, objective]
		tracked_quest_button.tooltip_text = tracked_quest_button.text
		return


func _show_current_ui_guidance(target_id: String) -> void:
	match target_id:
		"journal": QuestGuidance.show_for(journal_button, target_id)
		"summoner_profile": QuestGuidance.show_for(summoner_slot, target_id)
		"inventory": QuestGuidance.show_for(inventory_button, target_id)
		"spellbook": QuestGuidance.show_for(spellbook_button, target_id)
		"settings": QuestGuidance.show_for(
			tracked_quest_banner,
			target_id,
			"quest.guidance.press_escape"
		)


func _setup_objective_path_trail() -> void:
	_objective_path_trail = ObjectivePathTrailScript.new() as ObjectivePathTrail
	_objective_path_trail.name = "ObjectivePathTrail"
	add_child(_objective_path_trail)
	var obstacles: Array[Dictionary] = []
	obstacles.append_array(AcademyCityGraybox.USABLE_BUILDINGS)
	obstacles.append_array(AcademyCityGraybox.BACKGROUND_BUILDINGS)
	_objective_path_trail.configure(player, obstacles)


func _refresh_objective_path(target_id: String) -> void:
	if _objective_path_trail == null:
		return
	var location: Dictionary = _world_location(StringName(target_id))
	if not location.is_empty():
		_objective_path_trail.set_target(location.get("travel_position"))
		return
	_objective_path_trail.set_target(_quest_target_position(target_id))


func _world_guidance_target_id() -> String:
	var active_target_id: String = QuestGuidance.current_target_id()
	if active_target_id == "battle_settings":
		return "practice_grounds"
	if not active_target_id.is_empty():
		return active_target_id
	var general_state: Dictionary = QuestApi.get_npc_quest_state("general_magic")
	if not SafeTypeUtils.array(general_state.get("opportunities")).is_empty():
		return "general_magic"
	return ""


func _on_professor_interacted(professor_id: String) -> void:
	_close_travel()
	player.velocity = Vector3.ZERO
	player.set_physics_process(false)
	var professor_state: Dictionary = QuestApi.get_professor_quest_state(professor_id)
	var state: Dictionary = QuestApi.get_npc_quest_state(professor_id)
	var professor_name: String = Loc.t(
		SafeTypeUtils.string(professor_state.get("name_key"))
	)
	var opportunities: Array = SafeTypeUtils.array(state.get("opportunities"))
	_dialog_quest_id = ""
	_dialog_speaker = professor_name
	_dialog_npc_id = professor_id
	_dialog_accepted_lines.clear()
	_dialog_turn_in_npc_id = ""
	_dialog_response_actions.clear()
	_dialog_response_quest_ids.clear()
	_dialog_opportunities_by_id.clear()
	_dialog_offer_lines.clear()
	_dialog_offer_responses.clear()

	if not opportunities.is_empty():
		var quest: Dictionary = SafeTypeUtils.dict(opportunities[0])
		_dialog_quest_id = SafeTypeUtils.string(quest.get("id"))
		var offer_lines: Array[String] = _localized_dialogue_lines(
			SafeTypeUtils.array(quest.get("offer_dialogue_keys"))
		)
		if offer_lines.is_empty():
			offer_lines.append(Loc.t("academy.quest.offer_intro"))
		var responses: Array[Dictionary] = []
		for opportunity_value: Variant in opportunities:
			var opportunity: Dictionary = SafeTypeUtils.dict(opportunity_value)
			_dialog_opportunities_by_id[SafeTypeUtils.string(opportunity.get("id"))] = opportunity
			responses.append_array(_dialogue_responses(
				SafeTypeUtils.array(opportunity.get("response_choices")),
				SafeTypeUtils.string(opportunity.get("id"))
			))
		_dialog_offer_lines = offer_lines
		_dialog_offer_responses = responses
		_present_quest_opportunity_dialogue()
	else:
		var active: Array = SafeTypeUtils.array(state.get("active"))
		if active.is_empty():
			dialogue_box.present(professor_name, [Loc.t("academy.quest.no_assignment")])
		else:
			var quest: Dictionary = SafeTypeUtils.dict(active[0])
			var is_turn_in: bool = (
				SafeTypeUtils.string(quest.get("current_step_kind")) == "talk_to_npc"
				and SafeTypeUtils.string(quest.get("current_target_id")) == professor_id
			)
			var reminder_lines: Array[String] = _localized_dialogue_lines(
				SafeTypeUtils.array(quest.get("active_dialogue_keys"))
			)
			var objective: String = Loc.t(
				SafeTypeUtils.string(quest.get("current_objective_key"))
			)
			if reminder_lines.is_empty() and not is_turn_in:
				reminder_lines.append(
					Loc.t("academy.quest.active_reminder", {"objective": objective})
				)
			if not is_turn_in:
				reminder_lines.append(
					_accent_text(Loc.t("academy.quest.objective_callout", {"objective": objective}))
				)
			else:
				_dialog_turn_in_npc_id = professor_id
			dialogue_box.present(
				professor_name,
				reminder_lines
			)


func _on_dialogue_choice(choice_id: String) -> void:
	var action: String = SafeTypeUtils.string(_dialog_response_actions.get(choice_id))
	var selected_quest_id: String = SafeTypeUtils.string(
		_dialog_response_quest_ids.get(choice_id),
		_dialog_quest_id
	)
	if action != "accept_quest" or selected_quest_id.is_empty():
		_dialog_response_actions.clear()
		_dialog_response_quest_ids.clear()
		_dialog_quest_id = ""
		return
	var quest: Dictionary = SafeTypeUtils.dict(
		_dialog_opportunities_by_id.get(selected_quest_id)
	)
	if quest.is_empty():
		push_warning("WalkableAcademyHub: Missing offer data for quest '%s'" % selected_quest_id)
		return
	player.set_physics_process(false)
	quest_offer_modal.present(quest)


func _on_quest_offer_accepted(selected_quest_id: String) -> void:
	_dialog_accepted_lines = _accepted_lines_for_quest(_dialog_npc_id, selected_quest_id)
	if not QuestApi.accept_quest(selected_quest_id):
		push_warning("WalkableAcademyHub: Failed to accept quest '%s'" % selected_quest_id)
		_on_quest_offer_backed()
		return
	_dialog_response_actions.clear()
	_dialog_response_quest_ids.clear()
	if not _dialog_accepted_lines.is_empty():
		player.set_physics_process(false)
		dialogue_box.present(_dialog_speaker, _dialog_accepted_lines)
	_dialog_quest_id = ""


func _on_quest_offer_backed() -> void:
	player.set_physics_process(false)
	_present_quest_opportunity_dialogue()


func _on_quest_offer_cancelled() -> void:
	_clear_quest_dialogue_context()
	player.set_physics_process(true)


func _present_quest_opportunity_dialogue() -> void:
	dialogue_box.present(
		_dialog_speaker,
		_dialog_offer_lines,
		_dialog_offer_responses
	)


func _clear_quest_dialogue_context() -> void:
	_dialog_quest_id = ""
	_dialog_speaker = ""
	_dialog_npc_id = ""
	_dialog_accepted_lines.clear()
	_dialog_turn_in_npc_id = ""
	_dialog_response_actions.clear()
	_dialog_response_quest_ids.clear()
	_dialog_opportunities_by_id.clear()
	_dialog_offer_lines.clear()
	_dialog_offer_responses.clear()


func _on_dialogue_closed() -> void:
	if not _dialog_turn_in_npc_id.is_empty():
		var turn_in_npc_id: String = _dialog_turn_in_npc_id
		_dialog_turn_in_npc_id = ""
		var result: Dictionary = QuestApi.record_npc_interaction(turn_in_npc_id)
		if SafeTypeUtils.bool_val(result.get("completed"), false):
			_show_showcase_complete_after_rewards = (
				SafeTypeUtils.string(result.get("quest_id")) == UI_SHOWCASE_QUEST_ID
			)
			var summary: Dictionary = SafeTypeUtils.dict(result.get("completion_summary"))
			var rewards: Array = SafeTypeUtils.array(summary.get("granted_rewards"))
			if not rewards.is_empty():
				reward_modal.present(rewards, Loc.t("academy.quest.complete"))
				return
			if _show_showcase_complete_after_rewards:
				_show_showcase_complete_popup()
				return
	player.set_physics_process(true)


func _on_reward_modal_closed() -> void:
	if _show_showcase_complete_after_rewards:
		_show_showcase_complete_popup()
		return
	player.set_physics_process(true)


func _show_showcase_complete_popup() -> void:
	_show_showcase_complete_after_rewards = false
	showcase_complete_dialog.popup_centered()
	showcase_complete_dialog.get_ok_button().call_deferred("grab_focus")


func _on_showcase_complete_dialog_closed() -> void:
	player.set_physics_process(true)


func _on_quest_world_target_interacted(target_id: String) -> void:
	var result: Dictionary = QuestApi.record_world_interaction(target_id)
	var step: Dictionary = SafeTypeUtils.dict(result.get("current_step"))
	var encounter_id: String = SafeTypeUtils.string(step.get("encounter_id"))
	if encounter_id.is_empty() and target_id == "practice_grounds":
		encounter_id = _guided_battle_settings_encounter_id()
	if encounter_id.is_empty():
		return
	BattleContext.select_encounter(encounter_id)
	NavigationContext.push_return(SceneManager.SCENE_ACADEMY_CAMPUS)
	SceneManager.transition_to(SceneManager.SCENE_ENCOUNTER_PREPARATION)


func _guided_battle_settings_encounter_id() -> String:
	var journal: Dictionary = QuestApi.get_journal_state()
	var tracked_id: String = SafeTypeUtils.string(journal.get("tracked_quest_id"))
	for value: Variant in SafeTypeUtils.array(journal.get("active")):
		var quest: Dictionary = SafeTypeUtils.dict(value)
		if not tracked_id.is_empty() and SafeTypeUtils.string(quest.get("id")) != tracked_id:
			continue
		if SafeTypeUtils.string(quest.get("current_target_id")) != "battle_settings":
			continue
		return SafeTypeUtils.string(quest.get("current_encounter_id"))
	return ""


func _configure_professor(professor: InteractiveNpc, state: Dictionary) -> void:
	professor.configure(
		SafeTypeUtils.string(state.get("id")),
		Loc.t(SafeTypeUtils.string(state.get("name_key"))),
		SafeTypeUtils.string(state.get("quest_marker"))
	)


func _accent_text(text: String) -> String:
	return "[color=#%s][b]%s[/b][/color]" % [
		GameColorPalette.TEXT_HIGHLIGHT.to_html(false),
		text,
	]


func _localized_dialogue_lines(keys: Array) -> Array[String]:
	var lines: Array[String] = []
	for value: Variant in keys:
		var key: String = SafeTypeUtils.string(value)
		if not key.is_empty():
			lines.append(Loc.t(key))
	return lines


func _dialogue_responses(
	authored_responses: Array,
	quest_id: String
) -> Array[Dictionary]:
	var responses: Array[Dictionary] = []
	for value: Variant in authored_responses:
		var response: Dictionary = SafeTypeUtils.dict(value)
		var action: String = SafeTypeUtils.string(response.get("action"))
		if action not in ["accept_quest", "decline_quest"]:
			continue
		var response_id: String = SafeTypeUtils.string(response.get("id"))
		_dialog_response_actions[response_id] = action
		_dialog_response_quest_ids[response_id] = quest_id
		responses.append({
			"id": response_id,
			"text": Loc.t(SafeTypeUtils.string(response.get("text_key"))),
		})
	return responses


func _accepted_lines_for_quest(professor_id: String, quest_id: String) -> Array[String]:
	var state: Dictionary = QuestApi.get_npc_quest_state(professor_id)
	for value: Variant in SafeTypeUtils.array(state.get("opportunities")):
		var quest: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(quest.get("id")) == quest_id:
			return _localized_dialogue_lines(
				SafeTypeUtils.array(quest.get("accepted_dialogue_keys"))
			)
	return []


func _add_building(
	destination_id: String,
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
		SceneManager.SCENE_ACADEMY_CAMPUS,
		placeholder_texture,
		camera
	)
	building.set_destination_id(destination_id)
	buildings.add_child(building)


func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()


func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
