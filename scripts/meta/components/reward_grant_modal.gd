extends Control
class_name RewardGrantModal

signal closed

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")

@onready var panel: PanelContainer = %Panel
@onready var title_label: Label = %TitleLabel
@onready var rewards: HBoxContainer = %Rewards
@onready var continue_button: Button = %ContinueButton


func _ready() -> void:
	hide()
	continue_button.text = Loc.t("ui.common.continue")
	continue_button.pressed.connect(_close)
	_apply_palette()


func present(grants: Array, title: String = "") -> void:
	for child: Node in rewards.get_children():
		child.queue_free()
	title_label.text = title if not title.is_empty() else Loc.t("ui.reward_modal.title")
	for value: Variant in grants:
		var grant: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(grant.get("kind")) == "card":
			_add_card_reward(grant)
		else:
			_add_text_reward(grant)
	show()
	continue_button.call_deferred("grab_focus")


func _add_card_reward(grant: Dictionary) -> void:
	var card_id: String = SafeTypeUtils.string(grant.get("card_id", grant.get("id")))
	var card_data: Dictionary = CardCatalogApi.get_card_as_dict(card_id)
	var card_widget: CardWidget = CardWidgetScene.instantiate() as CardWidget
	card_widget.set_draggable(false)
	card_widget.ready.connect(
		func() -> void: card_widget.set_card({"catalog_id": card_id}, card_data),
		CONNECT_ONE_SHOT
	)
	rewards.add_child(card_widget)


func _add_text_reward(grant: Dictionary) -> void:
	var reward_id: String = SafeTypeUtils.string(grant.get("id"))
	var amount: int = SafeTypeUtils.int_val(grant.get("amount"), 1)
	var label: Label = Label.new()
	label.text = reward_id.capitalize() if amount == 1 else "%s ×%d" % [
		reward_id.capitalize(),
		amount,
	]
	label.add_theme_font_size_override("font_size", 24)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	rewards.add_child(label)


func _close() -> void:
	hide()
	closed.emit()


func _apply_palette() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	style.border_color = GameColorPalette.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(12)
	panel.add_theme_stylebox_override("panel", style)
	title_label.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
