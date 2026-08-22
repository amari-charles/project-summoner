extends VBoxContainer
class_name QuestDetailPanel

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")
const CARD_REWARD_PREVIEW_SIZE: Vector2 = Vector2(200, 300)

@onready var detail_empty: Label = %DetailEmpty
@onready var detail_content: VBoxContainer = %DetailContent
@onready var professor_name: Label = %ProfessorName
@onready var location_label: Label = %LocationLabel
@onready var detail_title: Label = %DetailTitle
@onready var detail_status: Label = %DetailStatus
@onready var detail_description: Label = %DetailDescription
@onready var detail_objective: Label = %DetailObjective
@onready var rewards_heading: Label = %RewardsHeading
@onready var rewards_scroll: ScrollContainer = %RewardsScroll
@onready var rewards_list: VBoxContainer = %RewardsList


func _ready() -> void:
	_apply_palette()


func present(
	entry: Dictionary,
	status_text: String = "",
	empty_text: String = "",
	show_unknown_reward: bool = false
) -> void:
	var has_entry: bool = not entry.is_empty()
	detail_empty.visible = not has_entry
	detail_content.visible = has_entry
	if not has_entry:
		detail_empty.text = empty_text
		return

	detail_title.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
	detail_status.text = status_text
	detail_status.visible = not status_text.is_empty()
	detail_description.text = Loc.t(SafeTypeUtils.string(entry.get("description_key")))
	var source_name_key: String = SafeTypeUtils.string(
		entry.get("source_name_key"),
		SafeTypeUtils.string(entry.get("professor_name_key"))
	)
	professor_name.text = Loc.t(source_name_key) if not source_name_key.is_empty() else ""
	professor_name.visible = not source_name_key.is_empty()
	var location_key: String = SafeTypeUtils.string(entry.get("location_key"))
	location_label.text = Loc.t(location_key) if not location_key.is_empty() else ""
	location_label.visible = not location_key.is_empty()

	var objective_key: String = SafeTypeUtils.string(entry.get("current_objective_key"))
	detail_objective.visible = not objective_key.is_empty()
	if not objective_key.is_empty():
		detail_objective.text = Loc.t(
			"academy.journal.current_objective",
			{"objective": Loc.t(objective_key)}
		)

	_render_rewards(
		SafeTypeUtils.array(entry.get("reward_previews")),
		show_unknown_reward
	)


func _render_rewards(reward_previews: Array, show_unknown_reward: bool) -> void:
	_clear_children(rewards_list)
	for value: Variant in reward_previews:
		var offer: Dictionary = SafeTypeUtils.dict(value)
		var options: Array = SafeTypeUtils.array(offer.get("options"))
		if options.is_empty():
			var category: Label = Label.new()
			category.text = Loc.t(SafeTypeUtils.string(offer.get("category_key")))
			category.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
			rewards_list.add_child(category)
			continue
		for option_value: Variant in options:
			var option: Dictionary = SafeTypeUtils.dict(option_value)
			for grant_value: Variant in SafeTypeUtils.array(option.get("grants")):
				rewards_list.add_child(_build_reward(SafeTypeUtils.dict(grant_value)))
	if rewards_list.get_child_count() == 0 and show_unknown_reward:
		var unknown: Label = Label.new()
		unknown.text = Loc.t("academy.quest.reward_unknown")
		unknown.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
		rewards_list.add_child(unknown)
	var has_rewards: bool = rewards_list.get_child_count() > 0
	rewards_heading.visible = has_rewards
	rewards_scroll.visible = has_rewards
	rewards_list.visible = has_rewards
	rewards_heading.text = Loc.t("academy.journal.rewards")


func _build_reward(grant: Dictionary) -> Control:
	if SafeTypeUtils.string(grant.get("kind")) == "card":
		return _build_card_reward(grant)
	var row: HBoxContainer = HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	var icon: Label = Label.new()
	icon.text = "◆"
	icon.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
	row.add_child(icon)
	var label: Label = Label.new()
	label.text = "%s ×%d" % [
		SafeTypeUtils.string(grant.get("id")).capitalize(),
		SafeTypeUtils.int_val(grant.get("amount"), 1),
	]
	label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	row.add_child(label)
	return row


func _build_card_reward(grant: Dictionary) -> Control:
	var card_id: String = SafeTypeUtils.string(grant.get("card_id", grant.get("id")))
	var card_widget: CardWidget = CardWidgetScene.instantiate() as CardWidget
	var card_panel: PanelContainer = card_widget.get_node("CardPanel") as PanelContainer
	card_panel.custom_minimum_size = CARD_REWARD_PREVIEW_SIZE
	card_widget.custom_minimum_size = CARD_REWARD_PREVIEW_SIZE
	card_widget.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	card_widget.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	card_widget.set_draggable(false)
	card_widget.ready.connect(
		func() -> void:
			card_widget.set_card(
				{"catalog_id": card_id},
				CardCatalogApi.get_card_as_dict(card_id)
			),
		CONNECT_ONE_SHOT
	)
	return card_widget


func _apply_palette() -> void:
	detail_empty.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	professor_name.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	location_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	detail_title.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
	detail_status.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	detail_description.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	detail_objective.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	rewards_heading.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)


func _clear_children(parent: Node) -> void:
	for child: Node in parent.get_children():
		parent.remove_child(child)
		child.queue_free()
