extends BackNavigableScreen
class_name EncounterPreparation

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")
@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var details_label: Label = %DetailsLabel
@onready var rewards_label: Label = %RewardsLabel
@onready var validation_label: Label = %ValidationLabel
@onready var loadout_grid: HBoxContainer = %LoadoutGrid
@onready var loadout_label: Label = %LoadoutLabel
@onready var save_button: Button = %SaveButton
@onready var start_button: Button = %StartButton
@onready var edit_deck_button: Button = %EditDeckButton
@onready var collection_overlay: CollectionScreen = %CollectionOverlay
@onready var save_choice_dialog: ConfirmationDialog = %SaveChoiceDialog
@onready var new_deck_dialog: ConfirmationDialog = %NewDeckDialog
@onready var new_deck_name: LineEdit = %NewDeckName
@onready var replace_deck_dialog: ConfirmationDialog = %ReplaceDeckDialog
@onready var replace_deck_selector: OptionButton = %ReplaceDeckSelector
@onready var save_result_dialog: AcceptDialog = %SaveResultDialog

var _state: Dictionary = {}
var _encounter_id: String = ""
var _saved_decks: Array = []

func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.hub.title")
	back_button.accessibility_name = Loc.t("academy.hub.title")
	back_button.pressed.connect(_go_back)
	start_button.text = Loc.t("academy.flow.start")
	save_button.text = Loc.t("academy.flow.save_to_my_decks")
	loadout_label.text = Loc.t("academy.flow.active_deck")
	edit_deck_button.tooltip_text = Loc.t("academy.flow.edit_deck")
	edit_deck_button.accessibility_name = Loc.t("academy.flow.edit_deck")
	start_button.pressed.connect(_start)
	save_button.pressed.connect(_open_save_choice)
	edit_deck_button.pressed.connect(_show_deck_editor)
	collection_overlay.closed.connect(_on_collection_overlay_closed)
	_configure_save_dialogs()
	_encounter_id = BattleContext.encounter_id
	BattleContext.set_battle_attempt_id(NarrativeDirectorApi.begin_attempt())
	_refresh()
	if _state.is_empty():
		_go_back()
		return
	NarrativeDirectorApi.publish_event(
		NarrativeDirectorApi.EventType.PREPARATION_OPENED,
		_encounter_id,
		{"encounter_id": _encounter_id}
	)

func _refresh() -> void:
	_state = CampaignApi.get_encounter_preparation_state(_encounter_id)
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
	_saved_decks = DecksApi.list_decks_for_summoner_dict(
		SummonerSelectionApi.get_active_summoner_id()
	)
	var loadout: Dictionary = SafeTypeUtils.dict(_state.get("loadout"))
	var mode: String = SafeTypeUtils.string(loadout.get("mode"))
	loadout_label.text = Loc.t("academy.flow.lesson_loadout") \
		if mode == "ClassLoadout" else Loc.t("academy.flow.active_deck")
	edit_deck_button.visible = mode != "Fixed"
	save_button.visible = mode == "ClassLoadout"
	for value: Variant in SafeTypeUtils.array(loadout.get("supplied_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, true)
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, false)

func _add_card_widgets(parent: Control, card: Dictionary, locked: bool) -> void:
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
		widget.set_display_size(CardVisualHelper.CARD_SIZE_LARGE)
		widget.tooltip_text = SafeTypeUtils.string(catalog_data.get("card_name"), card_id)
		if locked:
			widget.tooltip_text = "%s • %s" % [widget.tooltip_text, Loc.t("academy.flow.class_supplied")]


func _show_deck_editor() -> void:
	var mode: String = SafeTypeUtils.string(SafeTypeUtils.dict(_state.get("loadout")).get("mode"))
	if mode == "ClassLoadout":
		collection_overlay.open_encounter_loadout(_encounter_id)
	else:
		collection_overlay.open_collection()


func _on_collection_overlay_closed() -> void:
	_refresh()
	edit_deck_button.grab_focus()


func _configure_save_dialogs() -> void:
	save_choice_dialog.title = Loc.t("academy.flow.save_to_my_decks")
	save_choice_dialog.dialog_text = Loc.t("academy.flow.save_choice_prompt")
	save_choice_dialog.ok_button_text = Loc.t("academy.flow.create_new_deck")
	save_choice_dialog.add_button(Loc.t("academy.flow.replace_existing_deck"), true, "replace")
	save_choice_dialog.custom_action.connect(_on_save_choice_action)
	save_choice_dialog.confirmed.connect(_open_new_deck_dialog)
	new_deck_dialog.title = Loc.t("academy.flow.create_new_deck")
	new_deck_dialog.dialog_text = Loc.t("academy.flow.deck_name_prompt")
	new_deck_dialog.ok_button_text = Loc.t("academy.flow.create_deck")
	new_deck_dialog.confirmed.connect(_create_new_deck)
	new_deck_name.text_changed.connect(
		func(value: String) -> void: new_deck_dialog.get_ok_button().disabled = value.strip_edges().is_empty()
	)
	replace_deck_dialog.title = Loc.t("academy.flow.replace_existing_deck")
	replace_deck_dialog.dialog_text = Loc.t("academy.flow.replace_deck_warning")
	replace_deck_dialog.ok_button_text = Loc.t("academy.flow.replace_deck")
	replace_deck_dialog.confirmed.connect(_replace_existing_deck)
	save_result_dialog.title = Loc.t("academy.flow.save_to_my_decks")


func _open_save_choice() -> void:
	save_choice_dialog.popup_centered()


func _on_save_choice_action(action: StringName) -> void:
	if action == &"replace":
		save_choice_dialog.hide()
		_open_replace_deck_dialog()


func _open_new_deck_dialog() -> void:
	new_deck_name.text = Loc.t(
		"academy.flow.default_lesson_deck_name",
		{"activity": title_label.text}
	)
	new_deck_dialog.get_ok_button().disabled = new_deck_name.text.strip_edges().is_empty()
	new_deck_dialog.popup_centered()
	new_deck_name.grab_focus()
	new_deck_name.select_all()


func _open_replace_deck_dialog() -> void:
	replace_deck_selector.clear()
	for value: Variant in _saved_decks:
		var deck: Dictionary = SafeTypeUtils.dict(value)
		replace_deck_selector.add_item(SafeTypeUtils.string(deck.get("name"), Loc.t("academy.flow.deck")))
		replace_deck_selector.set_item_metadata(
			replace_deck_selector.item_count - 1,
			SafeTypeUtils.string(deck.get("id"))
		)
	replace_deck_dialog.get_ok_button().disabled = replace_deck_selector.item_count == 0
	replace_deck_dialog.popup_centered()


func _create_new_deck() -> void:
	_save_lesson_loadout("", new_deck_name.text.strip_edges())


func _replace_existing_deck() -> void:
	if replace_deck_selector.item_count == 0:
		return
	var deck_id: String = SafeTypeUtils.string(
		replace_deck_selector.get_item_metadata(replace_deck_selector.selected)
	)
	_save_lesson_loadout(deck_id, "")


func _save_lesson_loadout(target_deck_id: String, new_name: String) -> void:
	var result: Dictionary = CampaignApi.save_encounter_loadout_to_deck(
		_encounter_id, target_deck_id, new_name
	)
	if not SafeTypeUtils.bool_val(result.get("success")):
		_show_save_result(Loc.t("academy.flow.save_deck_failed"))
		return
	var omitted: Array = SafeTypeUtils.array(result.get("omitted_supplied_card_ids"))
	var message: String = Loc.t("academy.flow.deck_created") \
		if SafeTypeUtils.bool_val(result.get("created")) else Loc.t("academy.flow.deck_replaced")
	if not omitted.is_empty():
		var card_names: Array[String] = []
		for value: Variant in omitted:
			var card_id: String = SafeTypeUtils.string(value)
			card_names.append(SafeTypeUtils.string(
				CardCatalogApi.get_card_as_dict(card_id).get("card_name"), card_id
			))
		message += "\n\n" + Loc.t(
			"academy.flow.supplied_cards_omitted",
			{"cards": ", ".join(card_names)}
		)
	_show_save_result(message)
	_refresh()


func _show_save_result(message: String) -> void:
	save_result_dialog.dialog_text = message
	save_result_dialog.popup_centered()

func _start() -> void:
	var config: Dictionary = CampaignApi.resolve_encounter_battle_config(_encounter_id)
	if config.is_empty():
		_refresh()
		return
	BattleContext.configure_encounter_battle(_encounter_id, config)
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)

func _go_back() -> void:
	SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)


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


func _request_back_navigation() -> void:
	_go_back()
