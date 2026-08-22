extends GutTest


func test_battle_conclusion_is_a_timed_overlay_without_a_continue_step() -> void:
	var packed_scene: PackedScene = load("res://scenes/battle/ui/battle_hud.tscn")
	var game_ui: GameUI = packed_scene.instantiate() as GameUI
	game_ui.get_node("SpeedButton").free()
	game_ui.get_node("PauseButton").free()
	game_ui.get_node("PauseMenu").free()
	add_child_autofree(game_ui)
	await get_tree().process_frame

	var overlay: PanelContainer = game_ui.get_node("GameOverModal") as PanelContainer
	var label: Label = game_ui.get_node("GameOverModal/Content/GameOverLabel") as Label
	assert_null(game_ui.get_node_or_null("GameOverModal/Content/ContinueButton"))

	var conclusion_finished: Array[bool] = [false]
	game_ui.battle_conclusion_finished.connect(
		func() -> void: conclusion_finished[0] = true
	)
	var was_paused: bool = get_tree().paused
	get_tree().paused = true
	game_ui._on_game_ended(UnitConstants.Team.PLAYER)
	assert_true(overlay.visible)
	assert_eq(label.text, Loc.t("ui.post_battle.victory"))

	await get_tree().create_timer(
		GameUI.GAME_OVER_DISPLAY_DURATION + 0.1,
		true
	).timeout
	get_tree().paused = was_paused
	assert_true(conclusion_finished[0])
	assert_false(overlay.visible)
