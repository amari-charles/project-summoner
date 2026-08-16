extends Node3D
class_name AcademyProfessor

signal interacted(professor_id: String)

const CUTOUT_RENDER_ORDER: Script = preload("res://scripts/meta/components/academy_cutout_render_order.gd")

@onready var interaction_area: Area3D = %InteractionArea
@onready var professor_visual: Sprite3D = %ProfessorVisual
@onready var name_label: Label3D = %NameLabel
@onready var marker_label: Label3D = %MarkerLabel

var professor_id: String = ""
var _player_inside: bool = false
var _display_name: String = ""


func _ready() -> void:
	interaction_area.body_entered.connect(_on_body_entered)
	interaction_area.body_exited.connect(_on_body_exited)
	CUTOUT_RENDER_ORDER.anchor_visible_bottom(professor_visual, professor_visual.texture)
	_update_render_order()


func _process(_delta: float) -> void:
	_update_render_order()
	if _player_inside and Input.is_action_just_pressed("interact"):
		interacted.emit(professor_id)


func configure(state: Dictionary) -> void:
	professor_id = SafeTypeUtils.string(state.get("id"))
	_display_name = Loc.t(SafeTypeUtils.string(state.get("name_key")))
	marker_label.text = SafeTypeUtils.string(state.get("quest_marker"))
	marker_label.visible = not marker_label.text.is_empty()
	_refresh_prompt()


func _on_body_entered(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = true
	_refresh_prompt()


func _on_body_exited(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = false
	_refresh_prompt()


func _refresh_prompt() -> void:
	if name_label == null:
		return
	name_label.text = _display_name
	if _player_inside:
		name_label.text = "%s\n%s" % [_display_name, Loc.t("academy.quest.talk_prompt")]


func _update_render_order() -> void:
	CUTOUT_RENDER_ORDER.apply_from_feet(professor_visual, global_position.z)
	name_label.render_priority = professor_visual.render_priority + 1
	marker_label.render_priority = professor_visual.render_priority + 1
