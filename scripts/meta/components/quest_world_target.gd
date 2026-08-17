extends Node3D
class_name QuestWorldTarget

signal interacted(target_id: String)

@onready var interaction_area: Area3D = %InteractionArea
@onready var name_label: Label3D = %NameLabel
@onready var marker_label: Label3D = %MarkerLabel

var target_id: String = ""
var _display_name: String = ""
var _is_current_objective: bool = false
var _player_inside: bool = false


func _ready() -> void:
	interaction_area.body_entered.connect(_on_body_entered)
	interaction_area.body_exited.connect(_on_body_exited)
	_refresh()


func _process(_delta: float) -> void:
	if _is_current_objective and _player_inside and Input.is_action_just_pressed("interact"):
		interacted.emit(target_id)


func configure(world_target_id: String, display_name: String) -> void:
	target_id = world_target_id
	_display_name = display_name
	_refresh()


func set_current_objective(is_current: bool) -> void:
	_is_current_objective = is_current
	_refresh()


func _on_body_entered(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = true
	_refresh()


func _on_body_exited(body: Node3D) -> void:
	if not body.is_in_group("walkable_academy_player"):
		return
	_player_inside = false
	_refresh()


func _refresh() -> void:
	if name_label == null:
		return
	marker_label.visible = _is_current_objective
	marker_label.text = "!" if _is_current_objective else ""
	name_label.text = _display_name
	if _is_current_objective and _player_inside:
		name_label.text = "%s\n%s" % [
			_display_name,
			Loc.t("quest.world.interact_prompt"),
		]
