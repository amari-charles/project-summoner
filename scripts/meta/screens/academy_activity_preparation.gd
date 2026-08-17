extends Control
class_name EncounterPreparation

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")
const CardDetailModalScene: PackedScene = preload("res://scenes/meta/modals/card_detail_modal.tscn")
const LevelUpPanelScene: PackedScene = preload("res://scenes/meta/modals/card_level_up_panel.tscn")

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var details_label: Label = %DetailsLabel
@onready var rewards_label: Label = %RewardsLabel
@onready var validation_label: Label = %ValidationLabel
@onready var loadout_grid: HBoxContainer = %LoadoutGrid
@onready var loadout_label: Label = %LoadoutLabel
@onready var deck_selector: OptionButton = %DeckSelector
@onready var saved_deck_count: Label = %SavedDeckCount
@onready var save_button: Button = %SaveButton
@onready var start_button: Button = %StartButton
@onready var edit_deck_button: Button = %EditDeckButton
@onready var info_panel: PanelContainer = %InfoPanel
@onready var deck_header: HBoxContainer = %DeckHeader
@onready var loadout_scroll: ScrollContainer = %LoadoutScroll
@onready var footer: HBoxContainer = %Footer
@onready var editor_toolbar: HBoxContainer = %EditorToolbar
@onready var editor_footer: HBoxContainer = %EditorFooter
@onready var deck_editor: DeckEditorPanel = %DeckEditorPanel
@onready var save_choice_dialog: ConfirmationDialog = %SaveChoiceDialog
@onready var new_deck_dialog: ConfirmationDialog = %NewDeckDialog
@onready var new_deck_name: LineEdit = %NewDeckName
@onready var replace_deck_dialog: ConfirmationDialog = %ReplaceDeckDialog
@onready var replace_deck_selector: OptionButton = %ReplaceDeckSelector
@onready var save_result_dialog: AcceptDialog = %SaveResultDialog

var _state: Dictionary = {}
var _encounter_id: String = ""
var _editing_deck: bool = false
var _saved_decks: Array = []
var _populating_deck_selector: bool = false

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
	deck_selector.item_selected.connect(_select_owned_deck)
	_configure_save_dialogs()
	deck_editor.set_available_columns(7)
	deck_editor.add_card_requested.connect(_add_editor_card)
	deck_editor.remove_card_requested.connect(_remove_editor_card)
	deck_editor.card_info_requested.connect(_open_card_detail_modal)
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
	_populating_deck_selector = true
	deck_selector.clear()
	_saved_decks = DecksApi.list_decks_for_summoner_dict(
		SummonerSelectionApi.get_active_summoner_id()
	)
	var loadout: Dictionary = SafeTypeUtils.dict(_state.get("loadout"))
	var mode: String = SafeTypeUtils.string(loadout.get("mode"))
	loadout_label.text = Loc.t("academy.flow.lesson_loadout") \
		if mode == "ClassLoadout" else Loc.t("academy.flow.active_deck")
	edit_deck_button.visible = mode != "Fixed"
	deck_selector.visible = mode != "Fixed"
	saved_deck_count.visible = mode != "Fixed"
	save_button.visible = mode == "ClassLoadout"
	saved_deck_count.text = Loc.t("academy.flow.saved_deck_count", {"count": _saved_decks.size()})
	for value: Variant in SafeTypeUtils.array(loadout.get("supplied_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, true)
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		_add_card_widgets(loadout_grid, card, false)
	if mode == "Owned":
		var active_id: String = DecksApi.get_active_deck_id()
		for value: Variant in _saved_decks:
			var deck: Dictionary = SafeTypeUtils.dict(value)
			deck_selector.add_item(
				SafeTypeUtils.string(deck.get("name"), Loc.t("academy.flow.deck"))
			)
			deck_selector.set_item_metadata(deck_selector.item_count - 1, SafeTypeUtils.string(deck.get("id")))
			if SafeTypeUtils.string(deck.get("id")) == active_id:
				deck_selector.select(deck_selector.item_count - 1)
	elif mode == "ClassLoadout":
		deck_selector.add_item(Loc.t("academy.flow.fill_from_deck"))
		deck_selector.set_item_metadata(0, "")
		for value: Variant in _saved_decks:
			var deck: Dictionary = SafeTypeUtils.dict(value)
			deck_selector.add_item(SafeTypeUtils.string(deck.get("name"), Loc.t("academy.flow.deck")))
			deck_selector.set_item_metadata(
				deck_selector.item_count - 1,
				SafeTypeUtils.string(deck.get("id"))
			)
		deck_selector.select(0)
	_populating_deck_selector = false
	if mode == "Fixed":
		_editing_deck = false
	_render_editor(loadout, mode)
	_set_editor_visibility()


func _render_editor(loadout: Dictionary, mode: String) -> void:
	var active_entries: Array[Dictionary] = []
	for value: Variant in SafeTypeUtils.array(loadout.get("supplied_cards")):
		var supplied: Dictionary = SafeTypeUtils.dict(value)
		var count: int = maxi(SafeTypeUtils.int_val(supplied.get("count"), 1), 1)
		for _copy_index: int in range(count):
			active_entries.append(_to_editor_entry(supplied, true, active_entries.size()))
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		active_entries.append(_to_editor_entry(SafeTypeUtils.dict(value), false))

	var available_entries: Array[Dictionary] = []
	for value: Variant in SafeTypeUtils.array(loadout.get("available_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		if not SafeTypeUtils.bool_val(card.get("selected")):
			available_entries.append(_to_editor_entry(card, false))

	var editor_title: String = Loc.t("academy.flow.lesson_loadout") \
		if mode == "ClassLoadout" else _selected_owned_deck_name()
	deck_editor.set_active_deck(
		editor_title,
		active_entries,
		_editor_max_deck_size(loadout),
		mode != "Fixed"
	)
	deck_editor.set_available_cards(available_entries)


func _to_editor_entry(card: Dictionary, locked: bool, copy_index: int = 0) -> Dictionary:
	var catalog_id: String = SafeTypeUtils.string(card.get("card_id", card.get("catalog_id")))
	var instance_id: String = SafeTypeUtils.string(card.get("card_instance_id"))
	var detail_instance_id: String = instance_id
	var card_data: Dictionary = CardServiceApi.get_card_dict(instance_id) if not instance_id.is_empty() else {}
	if instance_id.is_empty():
		instance_id = "__encounter_supplied_%s_%d" % [catalog_id, copy_index]
		card_data = {"id": instance_id, "catalog_id": catalog_id}
	var tooltip: String = SafeTypeUtils.string(
		CardCatalogApi.get_card_as_dict(catalog_id).get("card_name"),
		catalog_id
	)
	if locked:
		tooltip = "%s • %s" % [tooltip, Loc.t("academy.flow.class_supplied")]
	return {
		"instance_id": instance_id,
		"detail_instance_id": detail_instance_id,
		"catalog_id": catalog_id,
		"card_data": card_data,
		"locked": locked,
		"tooltip": tooltip,
	}


func _editor_max_deck_size(loadout: Dictionary) -> int:
	var rules: Dictionary = SafeTypeUtils.dict(loadout.get("rules"))
	var authored_max: int = SafeTypeUtils.int_val(rules.get("max_deck_size"))
	return mini(authored_max, DeckConstants.MAX_DECK_SIZE) if authored_max > 0 else DeckConstants.MAX_DECK_SIZE

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
		widget.custom_minimum_size = Vector2(160.0, 240.0)
		widget.tooltip_text = SafeTypeUtils.string(catalog_data.get("card_name"), card_id)
		if locked:
			widget.tooltip_text = "%s • %s" % [widget.tooltip_text, Loc.t("academy.flow.class_supplied")]


func _show_deck_editor() -> void:
	_editing_deck = true
	_set_editor_visibility()
	back_button.grab_focus()


func _hide_deck_editor() -> void:
	_editing_deck = false
	deck_editor.dismiss_popup()
	_set_editor_visibility()
	edit_deck_button.grab_focus()


func _set_editor_visibility() -> void:
	var showing_editor: bool = _editing_deck and SafeTypeUtils.string(
		SafeTypeUtils.dict(_state.get("loadout")).get("mode")
	) != "Fixed"
	info_panel.visible = not showing_editor
	deck_header.visible = not showing_editor
	loadout_scroll.visible = not showing_editor
	validation_label.visible = not showing_editor
	footer.visible = not showing_editor
	editor_toolbar.visible = showing_editor
	deck_editor.visible = showing_editor
	editor_footer.visible = showing_editor and SafeTypeUtils.string(
		SafeTypeUtils.dict(_state.get("loadout")).get("mode")
	) == "ClassLoadout"


func _add_editor_card(instance_id: String) -> void:
	var mode: String = SafeTypeUtils.string(SafeTypeUtils.dict(_state.get("loadout")).get("mode"))
	if mode == "Owned":
		var deck_id: String = DecksApi.get_active_deck_id()
		if not deck_id.is_empty() and DecksApi.add_card_to_deck(deck_id, instance_id):
			_refresh()
	elif mode == "ClassLoadout":
		_toggle_class_card(instance_id)


func _remove_editor_card(instance_id: String) -> void:
	var mode: String = SafeTypeUtils.string(SafeTypeUtils.dict(_state.get("loadout")).get("mode"))
	if mode == "Owned":
		var deck_id: String = DecksApi.get_active_deck_id()
		if not deck_id.is_empty() and DecksApi.remove_card_from_deck(deck_id, instance_id):
			_refresh()
	elif mode == "ClassLoadout":
		_toggle_class_card(instance_id)

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
	if CampaignApi.update_encounter_loadout(_encounter_id, selected):
		_refresh()

func _select_owned_deck(index: int) -> void:
	if _populating_deck_selector:
		return
	var deck_id: String = SafeTypeUtils.string(deck_selector.get_item_metadata(index))
	if deck_id.is_empty():
		return
	var mode: String = SafeTypeUtils.string(SafeTypeUtils.dict(_state.get("loadout")).get("mode"))
	if mode == "Owned" and DecksApi.set_active_deck(deck_id):
		_refresh()
	elif mode == "ClassLoadout":
		var result: Dictionary = CampaignApi.fill_encounter_loadout_from_deck(
			_encounter_id, deck_id
		)
		if SafeTypeUtils.bool_val(result.get("success")):
			_refresh()
		else:
			_show_save_result(Loc.t("academy.flow.fill_failed"))


func _selected_owned_deck_name() -> String:
	var active_id: String = DecksApi.get_active_deck_id()
	for value: Variant in _saved_decks:
		var deck: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(deck.get("id")) == active_id:
			return SafeTypeUtils.string(deck.get("name"), Loc.t("academy.flow.active_deck"))
	return Loc.t("academy.flow.active_deck")


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
	if _editing_deck:
		_hide_deck_editor()
		return
	SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)


func _open_card_detail_modal(instance_id: String, catalog_id: String) -> void:
	var modal: CardDetailModal = CardDetailModalScene.instantiate()
	add_child(modal)
	modal.open_for_card(instance_id, catalog_id)
	var loadout: Dictionary = SafeTypeUtils.dict(_state.get("loadout"))
	var selected: bool = false
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		if SafeTypeUtils.string(SafeTypeUtils.dict(value).get("card_instance_id")) == instance_id:
			selected = true
			break
	modal.set_deck_context("encounter" if not instance_id.is_empty() else "", selected)
	modal.deck_action_requested.connect(_on_card_detail_deck_action)
	modal.level_up_requested.connect(_on_card_detail_level_up)
	modal.traits_requested.connect(_on_card_detail_traits)
	modal.closed.connect(func() -> void: modal.queue_free())


func _on_card_detail_deck_action(instance_id: String, action: String) -> void:
	if action == "add":
		_add_editor_card(instance_id)
	elif action == "remove":
		_remove_editor_card(instance_id)


func _on_card_detail_level_up(instance_id: String) -> void:
	var panel: CardLevelUpPanel = LevelUpPanelScene.instantiate()
	add_child(panel)
	panel.open_for_card(instance_id)
	panel.level_up_completed.connect(func(_card_id: String) -> void: _refresh())


func _on_card_detail_traits(instance_id: String) -> void:
	NavigationContext.set_value("trait_tree_card_instance_id", instance_id)
	NavigationContext.push_return(SceneManager.SCENE_ENCOUNTER_PREPARATION)
	SceneManager.transition_to(SceneManager.SCENE_CARD_TRAIT_TREE_SCREEN)


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
