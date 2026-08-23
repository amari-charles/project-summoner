extends GutTest

const ONLINE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/online_screen.tscn")


func test_ranked_layout_is_one_centered_composition_without_mode_tabs() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame
	screen._set_state(OnlineScreen.ScreenState.READY)

	assert_true(screen.mode_title_button.text.contains(Loc.t("ui.ranked.mode_ranked_1v1")))
	assert_true(screen.mode_title_button.text.contains("▾"))
	assert_null(screen.find_child("ModePanel", true, false))
	assert_null(screen.find_child("RankPanel", true, false))
	assert_null(screen.find_child("FriendlyBattleTabButton", true, false))
	assert_null(screen.find_child("MatchmakingTabButton", true, false))
	assert_not_null(screen.find_child("CompositionCenter", true, false))

	screen.queue_free()
	await get_tree().process_frame


func test_back_button_is_above_full_screen_layout_input_layer() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	var margin_container: MarginContainer = screen.get_node("MarginContainer") as MarginContainer
	assert_gt(
		screen.back_button.z_index,
		margin_container.z_index,
		"Back button must have explicit input priority over the full-screen layout"
	)

	screen.queue_free()
	await get_tree().process_frame


func test_rating_panel_shows_tier_relative_league_points() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame
	screen._leaderboard_service = null

	screen._update_rating_display(925)

	assert_eq(screen.tier_label.text, Loc.t("ui.ranked.tier_apprentice"))
	assert_eq(screen.league_points_label.text, Loc.t("ui.ranked.league_points", {"amount": 125}))

	screen.queue_free()
	await get_tree().process_frame


func test_queue_state_replaces_rank_content_and_start_with_cancel() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	screen._is_authenticated = true
	screen._set_state(OnlineScreen.ScreenState.READY)
	assert_true(screen.rank_content.visible)
	assert_eq(screen.queue_button.text, Loc.t("ui.ranked.find_match"))

	screen._set_state(OnlineScreen.ScreenState.IN_QUEUE)
	assert_false(screen.rank_content.visible)
	assert_eq(screen.queue_button.text, Loc.t("ui.ranked.cancel_queue"))
	assert_false(screen.queue_button.disabled)

	screen.queue_free()
	await get_tree().process_frame


func test_loadout_uses_character_art_slot_instead_of_summoner_name_text() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	assert_not_null(screen.character_art)
	assert_eq(screen.character_art.text, Loc.t("ui.ranked.character_art_placeholder"))
	assert_null(screen.find_child("SummonerLabel", true, false))
	var art_slot: Button = screen.find_child("CharacterArt", true, false) as Button
	assert_gt(art_slot.custom_minimum_size.y, art_slot.custom_minimum_size.x)
	assert_eq(art_slot.size_flags_horizontal, Control.SIZE_SHRINK_CENTER)
	assert_eq(art_slot.size_flags_vertical, Control.SIZE_SHRINK_CENTER)
	assert_gt(screen.deck_rail_button.custom_minimum_size.y, 0.0)

	screen.queue_free()
	await get_tree().process_frame


func test_deck_rail_renders_fixed_size_card_art_without_resizing_the_source_card() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict("fire_wisp")
	if catalog_data.is_empty():
		var all_cards: Array = CardCatalogApi.get_all_cards_as_dict()
		catalog_data = SafeTypeUtils.dict(all_cards[0])

	screen._clear_deck_rail()
	screen._add_deck_card(catalog_data)

	assert_eq(screen.deck_rail.get_child_count(), 1)
	var slot: Control = screen.deck_rail.get_child(0) as Control
	var card_visual: CardVisual = slot.get_child(0) as CardVisual
	assert_eq(slot.custom_minimum_size, OnlineScreen.DECK_CARD_SIZE)
	assert_eq(card_visual.custom_minimum_size, OnlineScreen.DECK_CARD_SIZE)
	assert_eq(card_visual.size, OnlineScreen.DECK_CARD_SIZE)
	assert_eq(card_visual.scale, Vector2.ONE)

	screen.queue_free()
	await get_tree().process_frame


func test_empty_deck_is_a_visible_selector_and_mode_menu_uses_noninteractive_placeholders() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	screen._clear_deck_rail()
	screen._show_empty_deck_state()
	assert_false(screen.deck_rail_button.flat)
	assert_eq(screen.deck_rail_button.text, Loc.t("ui.ranked.choose_ranked_deck"))

	screen._on_mode_title_pressed()
	assert_true(screen.mode_selector.visible)
	assert_true(screen.current_mode_label.text.contains(Loc.t("ui.ranked.mode_ranked_1v1")))
	var placeholder_a: Node = screen.find_child("PlaceholderModeA", true, false)
	var placeholder_b: Node = screen.find_child("PlaceholderModeB", true, false)
	assert_true(placeholder_a is Label)
	assert_true(placeholder_b is Label)
	assert_false(placeholder_a is BaseButton)
	assert_false(placeholder_b is BaseButton)

	screen.queue_free()
	await get_tree().process_frame


func test_ranked_deck_selection_opens_the_shared_collection_overlay() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	assert_true(screen.collection_overlay.embedded_overlay)
	assert_false(screen.collection_overlay.visible)
	screen._on_change_deck_pressed()
	assert_true(screen.collection_overlay.visible)
	assert_true(screen.collection_overlay._ranked_selection_mode)
	assert_eq(
		screen.collection_overlay._ranked_summoner_id,
		SummonerSelectionApi.get_active_summoner_id()
	)
	screen.collection_overlay._on_close_pressed()
	assert_false(screen.collection_overlay.visible)

	screen.queue_free()
	await get_tree().process_frame


func test_authentication_failure_shows_only_actionable_loadout_guidance() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	screen._on_authentication_failed("internal provider detail")

	assert_eq(screen.status_label.text, screen._get_loadout_issue())
	assert_false(screen.status_label.text.to_lower().contains("auth"))
	assert_false(screen.status_label.text.to_lower().contains("connect"))
	assert_false(screen.status_label.text.contains("internal provider detail"))

	screen.queue_free()
	await get_tree().process_frame
