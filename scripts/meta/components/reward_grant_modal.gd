extends Control
class_name RewardGrantModal

signal closed

const CardVisualScene: PackedScene = preload("res://scenes/shared/card_visual.tscn")
const REWARD_CARD_SIZE: Vector2 = CardVisualHelper.CARD_SIZE_LARGE

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
	var card_visual: CardVisual = CardVisualScene.instantiate() as CardVisual
	card_visual.set_display_size(REWARD_CARD_SIZE)
	card_visual.show_description = true
	card_visual.cost_font_size = 32
	card_visual.name_font_size = 18
	card_visual.description_font_size = 12
	card_visual.set_card_data(card_data, true)
	rewards.add_child(card_visual)


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
