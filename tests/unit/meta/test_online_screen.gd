extends GutTest

const ONLINE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/online_screen.tscn")


func test_competitive_mode_is_selected_while_casual_is_disabled() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	assert_true(screen.casual_button.disabled)
	assert_false(screen.casual_button.button_pressed)
	assert_true(screen.competitive_button.button_pressed)
	assert_eq(screen.casual_button.text, Loc.t("ui.ranked.casual"))
	assert_eq(screen.competitive_button.text, Loc.t("ui.ranked.competitive"))

	screen.queue_free()
	await get_tree().process_frame


func test_back_button_is_above_full_screen_layout_input_layer() -> void:
	var screen: OnlineScreen = ONLINE_SCREEN_SCENE.instantiate() as OnlineScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	var margin_container: MarginContainer = screen.get_node("MarginContainer") as MarginContainer
	assert_gt(
		screen.back_button.get_index(),
		margin_container.get_index(),
		"Back button must be later in the scene tree so the full-screen layout cannot intercept its clicks"
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
