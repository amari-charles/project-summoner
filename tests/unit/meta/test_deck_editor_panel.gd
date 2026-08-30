extends GutTest


func test_membership_refresh_reuses_widgets_without_reconfiguring_content() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	var first_entry: Dictionary = _test_entry("instance-one", "First")
	var second_entry: Dictionary = _test_entry("instance-two", "Second")
	var updated_second_entry: Dictionary = _test_entry("instance-two", "Updated Second")

	editor.set_active_deck("Active Deck", [], DeckConstants.MAX_DECK_SIZE, true)
	editor.set_available_cards([first_entry, second_entry])
	var first_widget: CardWidget = editor.available_cards.get_child(0) as CardWidget
	var second_widget: CardWidget = editor.available_cards.get_child(1) as CardWidget

	editor.set_available_cards([first_entry, updated_second_entry], false)
	assert_same(editor.available_cards.get_child(1), second_widget)
	assert_eq(second_widget.card_name.text, "Second")

	editor.set_available_cards([first_entry, updated_second_entry], true)
	assert_same(editor.available_cards.get_child(1), second_widget)
	assert_eq(second_widget.card_name.text, "Updated Second")

	editor.set_available_cards([updated_second_entry], false)
	assert_eq(editor.available_cards.get_child_count(), 1)
	assert_same(editor.available_cards.get_child(0), second_widget)
	assert_false(first_widget.is_inside_tree())
	await get_tree().process_frame
	assert_false(is_instance_valid(first_widget))


func test_left_click_adds_available_card_without_opening_details() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_active_deck("Active Deck", [], DeckConstants.MAX_DECK_SIZE, true)
	editor.set_available_cards([_test_entry("instance-one", "First")])

	var state: Dictionary = {"added_id": ""}
	var request: Dictionary = {"instance_id": "", "catalog_id": ""}
	editor.add_card_requested.connect(
		func(instance_id: String) -> void: state["added_id"] = instance_id
	)
	editor.card_info_requested.connect(
		func(instance_id: String, catalog_id: String) -> void:
			request["instance_id"] = instance_id
			request["catalog_id"] = catalog_id
	)

	var widget: CardWidget = editor.available_cards.get_child(0) as CardWidget
	widget.card_clicked.emit({})

	assert_eq(state["added_id"], "instance-one")
	assert_eq(request["instance_id"], "")


func test_left_click_removes_active_card() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_active_deck("Active Deck", [_test_entry("instance-one", "First")], DeckConstants.MAX_DECK_SIZE, true)
	editor.set_available_cards([])

	var state: Dictionary = {"removed_id": ""}
	editor.remove_card_requested.connect(
		func(instance_id: String) -> void: state["removed_id"] = instance_id
	)
	var widget: CardWidget = editor.active_cards.get_child(0) as CardWidget
	widget.card_clicked.emit({})

	assert_eq(state["removed_id"], "instance-one")


func test_right_click_inspects_without_changing_deck() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_active_deck("Active Deck", [], DeckConstants.MAX_DECK_SIZE, true)
	editor.set_available_cards([_test_entry("instance-one", "First")])

	var state: Dictionary = {"added_id": ""}
	var request: Dictionary = {"instance_id": "", "catalog_id": ""}
	editor.add_card_requested.connect(
		func(instance_id: String) -> void: state["added_id"] = instance_id
	)
	editor.card_info_requested.connect(
		func(instance_id: String, catalog_id: String) -> void:
			request["instance_id"] = instance_id
			request["catalog_id"] = catalog_id
	)

	var widget: CardWidget = editor.available_cards.get_child(0) as CardWidget
	var right_click: InputEventMouseButton = InputEventMouseButton.new()
	right_click.button_index = MOUSE_BUTTON_RIGHT
	right_click.pressed = true
	widget._gui_input(right_click)

	assert_eq(request["instance_id"], "instance-one")
	assert_eq(request["catalog_id"], "test-card")
	assert_eq(state["added_id"], "")


func test_showcase_guidance_marks_the_exact_right_click_card_yellow() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_active_deck("Active Deck", [], DeckConstants.MAX_DECK_SIZE, true)
	editor.set_available_cards([_test_entry("instance-one", "First")])
	var marker_targets: Array[Control] = []
	editor.card_inspection_guidance_target_changed.connect(
		func(target: Control) -> void: marker_targets.append(target)
	)

	editor.set_card_inspection_guidance(true)

	var widget: CardWidget = editor.available_cards.get_child(0) as CardWidget
	var style: StyleBoxFlat = widget.card_panel.get_theme_stylebox("panel") as StyleBoxFlat
	assert_true(widget._quest_highlighted)
	assert_eq(style.border_color, Color(1.0, 0.78, 0.16, 1.0))
	assert_eq(style.border_width_left, 8)
	assert_eq(style.shadow_color, Color(1.0, 0.78, 0.16, 0.9))
	assert_eq(style.shadow_size, 18)
	assert_eq(marker_targets.back(), widget.card_panel)

	# Service-driven deck refreshes rebuild the active row. The quest highlight
	# must follow the replacement widget instead of disappearing with the old one.
	editor.set_active_deck(
		"Active Deck",
		[_test_entry("instance-two", "Replacement")],
		DeckConstants.MAX_DECK_SIZE,
		true
	)
	await get_tree().process_frame
	var replacement: CardWidget = editor.active_cards.get_child(0) as CardWidget
	assert_true(replacement._quest_highlighted)
	assert_eq(marker_targets.back(), replacement.card_panel)
	var replacement_style: StyleBoxFlat = replacement.card_panel.get_theme_stylebox(
		"panel"
	) as StyleBoxFlat
	assert_eq(replacement_style.border_color, Color(1.0, 0.78, 0.16, 1.0))


func test_left_click_inspects_when_deck_is_read_only() -> void:
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_active_deck("Deck", [], DeckConstants.MAX_DECK_SIZE, false)
	editor.set_available_cards([_test_entry("instance-one", "First")])

	var state: Dictionary = {"inspected_id": ""}
	editor.card_info_requested.connect(
		func(instance_id: String, _catalog_id: String) -> void: state["inspected_id"] = instance_id
	)
	var widget: CardWidget = editor.available_cards.get_child(0) as CardWidget
	widget.card_clicked.emit({})

	assert_eq(state["inspected_id"], "instance-one")


func _test_entry(instance_id: String, card_name: String) -> Dictionary:
	return {
		"instance_id": instance_id,
		"catalog_id": "test-card",
		"card_data": {
			"id": instance_id,
			"catalog_id": "test-card",
		},
		"catalog_data": {
			"catalog_id": "test-card",
			"card_name": card_name,
			"mana_cost": 1,
			"card_type": UnitConstants.CardType.SUMMON,
			"unit_type": UnitTypeIDs.MELEE,
			"categories": {"elemental_affinity": "neutral"},
		},
		"progression": {},
	}
