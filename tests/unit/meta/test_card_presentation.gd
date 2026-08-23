extends GutTest


func test_full_card_sizes_share_the_gameplay_three_to_four_ratio() -> void:
	for display_size: Vector2 in [
		CardVisualHelper.CARD_SIZE_COMPACT,
		CardVisualHelper.CARD_SIZE_STANDARD,
		CardVisualHelper.CARD_SIZE_LARGE,
	]:
		assert_almost_eq(
			display_size.x / display_size.y,
			CardVisualHelper.CARD_ASPECT_RATIO,
			0.001
		)
	assert_eq(CardVisualHelper.CARD_SIZE_STANDARD, Vector2(120, 160))


func test_shared_card_components_use_named_default_sizes() -> void:
	var visual_scene: PackedScene = load("res://scenes/shared/card_visual.tscn") as PackedScene
	var card_visual: CardVisual = visual_scene.instantiate() as CardVisual
	add_child_autofree(card_visual)
	await get_tree().process_frame
	assert_eq(card_visual.custom_minimum_size, CardVisualHelper.CARD_SIZE_STANDARD)

	var widget_scene: PackedScene = load(
		"res://scenes/meta/components/card_widget.tscn"
	) as PackedScene
	var card_widget: CardWidget = widget_scene.instantiate() as CardWidget
	add_child_autofree(card_widget)
	await get_tree().process_frame
	var card_panel: PanelContainer = card_widget.get_node("CardPanel") as PanelContainer
	assert_eq(card_panel.custom_minimum_size, CardVisualHelper.CARD_SIZE_LARGE)


func test_collection_uses_standard_browsing_cards() -> void:
	var collection_scene: PackedScene = load(
		"res://scenes/meta/screens/collection_screen.tscn"
	) as PackedScene
	var collection: Control = collection_scene.instantiate() as Control
	var deck_editor: DeckEditorPanel = collection.find_child("DeckEditorPanel") as DeckEditorPanel
	var card_browser: PanelContainer = collection.find_child("LeftPanel") as PanelContainer
	var deck_list: PanelContainer = collection.find_child("RightPanel") as PanelContainer
	assert_eq(deck_editor.card_size_preset, CardVisualHelper.CardSize.STANDARD)
	assert_eq(card_browser.size_flags_stretch_ratio, 3.0)
	assert_eq(deck_list.size_flags_stretch_ratio, 1.0)
	collection.free()
