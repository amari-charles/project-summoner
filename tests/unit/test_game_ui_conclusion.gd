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


func test_match_timer_occupies_prep_countdown_space_during_battle() -> void:
	var packed_scene: PackedScene = load("res://scenes/battle/ui/battle_hud.tscn")
	var game_ui: GameUI = packed_scene.instantiate() as GameUI
	game_ui.get_node("SpeedButton").free()
	game_ui.get_node("PauseButton").free()
	game_ui.get_node("PauseMenu").free()
	add_child_autofree(game_ui)
	await get_tree().process_frame

	var timer: Label = game_ui.get_node("TimerLabel") as Label
	var phase_label: Label = game_ui.get_node("PhaseLabel") as Label
	var prep_timer: Label = game_ui.get_node("PrepTimerLabel") as Label
	assert_false(timer.visible)
	game_ui._on_phase_changed(UnitConstants.BattlePhase.PREPARATION)
	game_ui._on_prep_timer_updated(12.4)
	assert_true(phase_label.visible)
	assert_eq(phase_label.text, Loc.t("ui.battle.prepare_your_field"))
	assert_true(prep_timer.visible)
	assert_eq(prep_timer.text, "13")

	game_ui._on_phase_changed(UnitConstants.BattlePhase.BATTLE)
	game_ui._on_time_updated(125.0)
	assert_true(timer.visible)
	assert_eq(timer.text, "02:05")
	assert_false(prep_timer.visible)
	assert_eq(phase_label.text, Loc.t("ui.battle.phase_battle"))

	game_ui._on_phase_changed(UnitConstants.BattlePhase.PREPARATION)
	assert_false(timer.visible)


func test_battle_hud_presents_mirrored_summoner_identity_groups() -> void:
	var packed_scene: PackedScene = load("res://scenes/battle/ui/battle_hud.tscn")
	var game_ui: GameUI = packed_scene.instantiate() as GameUI
	game_ui.get_node("SpeedButton").free()
	game_ui.get_node("PauseButton").free()
	game_ui.get_node("PauseMenu").free()
	add_child_autofree(game_ui)
	await get_tree().process_frame

	var player_status: BattleSummonerStatus = game_ui.get_node(
		"HUDContainer/PlayerStatus"
	) as BattleSummonerStatus
	var enemy_status: BattleSummonerStatus = game_ui.get_node(
		"HUDContainer/EnemyStatus"
	) as BattleSummonerStatus
	assert_false(player_status.mirrored)
	assert_true(enemy_status.mirrored)
	assert_eq(player_status.get_child(0), player_status.portrait_frame)
	assert_eq(enemy_status.get_child(enemy_status.get_child_count() - 1), enemy_status.portrait_frame)

	var summoner_id: String = String(SummonerIDs.COLE)
	var config: SummonerConfig = SummonerConfig.from_dict(
		SummonerCatalogApi.get_summoner(summoner_id)
	)
	player_status.configure(summoner_id, "Fallback Player")
	enemy_status.configure("", "Practice Opponent")
	assert_eq(player_status.name_label.text, config.summoner_name)
	assert_eq(enemy_status.name_label.text, "Practice Opponent")
	assert_not_null(player_status.hp_bar)
	assert_not_null(enemy_status.mana_bar)
