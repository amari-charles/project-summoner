extends Node3D
class_name InteractiveNpc

signal interacted(npc_id: String)

const CUTOUT_RENDER_ORDER: Script = preload("res://scripts/meta/components/academy_cutout_render_order.gd")

@onready var interaction_area: Area3D = %InteractionArea
@onready var character_visual: Sprite3D = %CharacterVisual
@onready var name_label: Label3D = %NameLabel
@onready var marker_label: Label3D = %MarkerLabel

var npc_id: String = ""
var _player_inside: bool = false
var _display_name: String = ""


func _ready() -> void:
	interaction_area.body_entered.connect(_on_body_entered)
	interaction_area.body_exited.connect(_on_body_exited)
	CUTOUT_RENDER_ORDER.anchor_visible_bottom(character_visual, character_visual.texture)
	_update_render_order()


func _process(_delta: float) -> void:
	_update_render_order()
	if _player_inside and Input.is_action_just_pressed("interact"):
		interacted.emit(npc_id)


func configure(
	character_id: String,
	display_name: String,
	marker: String = "",
	texture: Texture2D = null,
	frame_count: int = 1
) -> void:
	npc_id = character_id
	_display_name = display_name
	if texture != null:
		character_visual.texture = texture
		character_visual.hframes = maxi(frame_count, 1)
		CUTOUT_RENDER_ORDER.anchor_visible_bottom(character_visual, texture)
	marker_label.text = marker
	marker_label.visible = not marker.is_empty()
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
	CUTOUT_RENDER_ORDER.apply_from_feet(character_visual, global_position.z)
	name_label.render_priority = character_visual.render_priority + 1
	marker_label.render_priority = character_visual.render_priority + 1
