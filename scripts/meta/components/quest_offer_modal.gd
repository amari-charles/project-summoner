extends Control
class_name QuestOfferModal

signal accepted(quest_id: String)
signal backed

@onready var panel: PanelContainer = %Panel
@onready var quest_detail: QuestDetailPanel = %QuestDetailPanel
@onready var back_button: Button = %BackButton
@onready var accept_button: Button = %AcceptButton

var _quest_id: String = ""


func _ready() -> void:
	hide()
	back_button.text = Loc.t("ui.common.back")
	accept_button.text = Loc.t("academy.quest.accept")
	back_button.pressed.connect(_back)
	accept_button.pressed.connect(_accept)
	_apply_palette()


func present(quest: Dictionary) -> void:
	_quest_id = SafeTypeUtils.string(quest.get("id"))
	var cost: int = SafeTypeUtils.int_val(quest.get("curriculum_cost"), 0)
	quest_detail.present(
		quest,
		Loc.t("academy.quest.permanent_cost", {"cost": cost}),
		"",
		true
	)
	show()
	accept_button.call_deferred("grab_focus")


func _back() -> void:
	hide()
	backed.emit()


func _accept() -> void:
	var quest_id: String = _quest_id
	hide()
	accepted.emit(quest_id)


func _unhandled_key_input(event: InputEvent) -> void:
	if visible and event.pressed and not event.echo and event.is_action("ui_cancel"):
		_back()
		get_viewport().set_input_as_handled()


func _apply_palette() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	style.border_color = GameColorPalette.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(12)
	panel.add_theme_stylebox_override("panel", style)
