extends Control
class_name AcademyActivityPreparation

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var details_label: Label = %DetailsLabel
@onready var rewards_label: Label = %RewardsLabel
@onready var validation_label: Label = %ValidationLabel
@onready var loadout_grid: HBoxContainer = %LoadoutGrid
@onready var available_grid: GridContainer = %AvailableGrid
@onready var loadout_label: Label = %LoadoutLabel
@onready var available_label: Label = %AvailableLabel
@onready var deck_selector: OptionButton = %DeckSelector
@onready var save_button: Button = %SaveButton
@onready var start_button: Button = %StartButton
@onready var edit_deck_button: Button = %EditDeckButton
@onready var close_edit_button: Button = %CloseEditButton
@onready var edit_panel: PanelContainer = %EditPanel

var _state: Dictionary = {}
var _course_id: String = ""
var _activity_id: String = ""

func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.flow.course")
	back_button.accessibility_name = Loc.t("academy.flow.course")
	back_button.pressed.connect(_go_back)
	start_button.text = Loc.t("academy.flow.start")
	save_button.text = Loc.t("academy.flow.save_as_deck")
	loadout_label.text = Loc.t("academy.flow.active_deck")
	available_label.text = Loc.t("academy.flow.owned_cards")
	edit_deck_button.tooltip_text = Loc.t("academy.flow.edit_deck")
	edit_deck_button.accessibility_name = Loc.t("academy.flow.edit_deck")
	start_button.pressed.connect(_start)
	save_button.pressed.connect(_save_as_deck)
	edit_deck_button.pressed.connect(_show_deck_editor)
	close_edit_button.pressed.connect(_hide_deck_editor)
	deck_selector.item_selected.connect(_select_owned_deck)
	_course_id = BattleContext.academy_course_id
	_activity_id = BattleContext.academy_activity_id
	BattleContext.set_battle_attempt_id(NarrativeDirectorApi.begin_attempt())
	_refresh()
	if _state.is_empty():
		_go_back()
		return
	NarrativeDirectorApi.publish_event(
		NarrativeDirectorApi.EventType.PREPARATION_OPENED,
		_activity_id,
		{"course_id": _course_id}
	)

func _refresh() -> void:
	_state = CampaignApi.get_academy_activity_preparation_state(_course_id, _activity_id)
	if _state.is_empty():
		return
	title_label.text = Loc.t(SafeTypeUtils.string(_state.get("label_key")))
	var validation: Dictionary = SafeTypeUtils.dict(_state.get("deck_validation"))
	var issues: Array = SafeTypeUtils.array(validation.get("issues"))
	details_label.text = "%s • %s\n\n%s" % [
		Loc.t("academy.flow.role_%s" % SafeTypeUtils.string(_state.get("role")).to_lower()),
		Loc.t("academy.flow.mode_%s" % SafeTypeUtils.string(_state.get("deck_mode")).to_lower()),
		Loc.t("academy.flow.rules", {"rules": _rule_summary(SafeTypeUtils.dict(_state.get("loadout")))}),
	]
	rewards_label.text = _reward_summary(SafeTypeUtils.array(_state.get("reward_previews")))
	validation_label.text = Loc.t("academy.flow.loadout_valid") \
		if SafeTypeUtils.bool_val(validation.get("is_valid")) \
		else _validation_issue_summary(issues)
	start_button.disabled = not SafeTypeUtils.bool_val(_state.get("can_start"))
	_render_loadout()

func _render_loadout() -> void:
	_clear(loadout_grid)
	_clear(available_grid)
	deck_selector.clear()
	var loadout: Dictionary = SafeTypeUtils.dict(_state.get("loadout"))
	var mode: String = SafeTypeUtils.string(loadout.get("mode"))
	edit_deck_button.visible = mode != "Fixed"
	deck_selector.visible = mode == "Owned"
	save_button.visible = mode == "ClassLoadout"
	for value: Variant in SafeTypeUtils.array(loadout.get("supplied_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, true, Callable())
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, false, _toggle_class_card.bind(SafeTypeUtils.string(card.get("card_instance_id"))))
	if mode == "ClassLoadout":
		for value: Variant in SafeTypeUtils.array(loadout.get("available_cards")):
			var card: Dictionary = SafeTypeUtils.dict(value)
			_add_card_widgets(available_grid, card, SafeTypeUtils.bool_val(card.get("selected")), _toggle_class_card.bind(SafeTypeUtils.string(card.get("card_instance_id"))))
	elif mode == "Owned":
		var active_id: String = DecksApi.get_active_deck_id()
		for value: Variant in DecksApi.list_decks_for_summoner_dict(SummonerSelectionApi.get_active_summoner_id()):
			var deck: Dictionary = SafeTypeUtils.dict(value)
			deck_selector.add_item(
				SafeTypeUtils.string(deck.get("name"), Loc.t("academy.flow.deck"))
			)
			deck_selector.set_item_metadata(deck_selector.item_count - 1, SafeTypeUtils.string(deck.get("id")))
			if SafeTypeUtils.string(deck.get("id")) == active_id:
				deck_selector.select(deck_selector.item_count - 1)
	if mode == "Fixed":
		edit_panel.visible = false

func _add_card_widgets(parent: Control, card: Dictionary, locked: bool, action: Callable) -> void:
	var card_id: String = SafeTypeUtils.string(card.get("card_id", card.get("catalog_id")))
	var count: int = SafeTypeUtils.int_val(card.get("count"), 1)
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(card_id)
	var card_data: Dictionary = {}
	var instance_id: String = SafeTypeUtils.string(card.get("card_instance_id"))
	if not instance_id.is_empty():
		card_data = CardServiceApi.get_card_dict(instance_id)
	if card_data.is_empty():
		card_data = {"catalog_id": card_id}
	for _copy_index: int in range(maxi(count, 1)):
		var widget: CardWidget = CardWidgetScene.instantiate()
		parent.add_child(widget)
		widget.set_card(card_data, catalog_data)
		widget.set_draggable(false)
		widget.custom_minimum_size = Vector2(160.0, 240.0)
		widget.tooltip_text = SafeTypeUtils.string(catalog_data.get("card_name"), card_id)
		if locked:
			widget.tooltip_text = "%s • %s" % [widget.tooltip_text, Loc.t("academy.flow.class_supplied")]
		if action.is_valid() and not locked:
			widget.card_clicked.connect(func(_card_data: Dictionary) -> void: action.call())


func _show_deck_editor() -> void:
	edit_panel.visible = true
	close_edit_button.grab_focus()


func _hide_deck_editor() -> void:
	edit_panel.visible = false
	edit_deck_button.grab_focus()

func _toggle_class_card(instance_id: String) -> void:
	var loadout: Dictionary = SafeTypeUtils.dict(_state.get("loadout"))
	var selected: Array[Dictionary] = []
	var found: bool = false
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(card.get("card_instance_id")) == instance_id:
			found = true
		else:
			selected.append({"card_instance_id": SafeTypeUtils.string(card.get("card_instance_id"))})
	if not found:
		selected.append({"card_instance_id": instance_id})
	if CampaignApi.update_academy_activity_loadout(_course_id, _activity_id, selected):
		_refresh()

func _select_owned_deck(index: int) -> void:
	var deck_id: String = SafeTypeUtils.string(deck_selector.get_item_metadata(index))
	if not deck_id.is_empty() and DecksApi.set_active_deck(deck_id):
		_refresh()
		edit_panel.visible = true

func _save_as_deck() -> void:
	CampaignApi.save_academy_activity_loadout_as_deck(_course_id, _activity_id)
	_refresh()

func _start() -> void:
	var config: Dictionary = CampaignApi.resolve_academy_activity_battle_config(_course_id, _activity_id)
	if config.is_empty():
		_refresh()
		return
	BattleContext.configure_academy_battle(_course_id, _activity_id, config)
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)

func _go_back() -> void:
	if edit_panel.visible:
		_hide_deck_editor()
		return
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_FLOW)


func _reward_summary(previews: Array) -> String:
	if previews.is_empty():
		return Loc.t("academy.flow.none")
	var labels: Array[String] = []
	for value: Variant in previews:
		var preview: Dictionary = SafeTypeUtils.dict(value)
		var status: String = Loc.t(
			"academy.flow.reward_status_%s" % SafeTypeUtils.string(preview.get("status"), "preview").to_lower()
		)
		var options: Array = SafeTypeUtils.array(preview.get("options"))
		var label: String = Loc.t("academy.flow.reward")
		if not options.is_empty():
			var option: Dictionary = SafeTypeUtils.dict(options[0])
			var grants: Array = SafeTypeUtils.array(option.get("grants"))
			if not grants.is_empty():
				label = _grant_label(SafeTypeUtils.dict(grants[0]))
		labels.append("• %s (%s)" % [label, status])
	return "\n".join(labels)


func _grant_label(grant: Dictionary) -> String:
	if SafeTypeUtils.string(grant.get("kind")) == "card":
		var card_id: String = SafeTypeUtils.string(grant.get("id"))
		var card: Dictionary = CardCatalogApi.get_card_as_dict(card_id)
		return SafeTypeUtils.string(card.get("card_name"), card_id)
	var grant_id: String = SafeTypeUtils.string(grant.get("id"))
	return Loc.t("academy.reward.%s" % grant_id) if not grant_id.is_empty() else Loc.t("academy.flow.reward")

func _rule_summary(loadout: Dictionary) -> String:
	var rules: Dictionary = SafeTypeUtils.dict(loadout.get("rules"))
	if not SafeTypeUtils.bool_val(rules.get("has_rules")):
		return Loc.t("academy.flow.no_special_rules")
	var parts: Array[String] = []
	var allowed_types: Array = SafeTypeUtils.array(rules.get("allowed_card_types"))
	if not allowed_types.is_empty():
		parts.append(Loc.t("academy.flow.allowed_types", {"values": ", ".join(allowed_types)}))
	var allowed_elements: Array = SafeTypeUtils.array(rules.get("allowed_elements"))
	if not allowed_elements.is_empty():
		parts.append(Loc.t("academy.flow.allowed_elements", {"values": ", ".join(allowed_elements)}))
	var min_summons: int = SafeTypeUtils.int_val(rules.get("min_summons"))
	if min_summons > 0:
		parts.append(Loc.t("academy.flow.min_summons", {"count": min_summons}))
	var min_spells: int = SafeTypeUtils.int_val(rules.get("min_spells"))
	if min_spells > 0:
		parts.append(Loc.t("academy.flow.min_spells", {"count": min_spells}))
	var max_cards: int = SafeTypeUtils.int_val(rules.get("max_deck_size"))
	if max_cards > 0:
		parts.append(Loc.t("academy.flow.max_cards", {"count": max_cards}))
	return "; ".join(parts)

func _validation_issue_summary(issues: Array) -> String:
	var messages: Array[String] = []
	for value: Variant in issues:
		var issue: Dictionary = SafeTypeUtils.dict(value)
		var code: String = SafeTypeUtils.string(issue.get("code"))
		var arguments: Dictionary = SafeTypeUtils.dict(issue.get("arguments"))
		var card_id: String = SafeTypeUtils.string(arguments.get("card_id"))
		if not card_id.is_empty():
			arguments["card"] = SafeTypeUtils.string(
				CardCatalogApi.get_card_as_dict(card_id).get("card_name"), card_id
			)
		messages.append(Loc.t("academy.flow.validation_%s" % code, arguments))
	return "; ".join(messages)

func _clear(parent: Control) -> void:
	for child: Node in parent.get_children():
		parent.remove_child(child)
		child.queue_free()
