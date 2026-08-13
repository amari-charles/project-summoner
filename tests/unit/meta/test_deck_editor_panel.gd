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
