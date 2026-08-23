extends GutTest


class FakeGameController:
	extends Node

	signal GameEnded(winner: int)
	signal StateChanged(state: int)

	var CurrentState: int = int(UnitConstants.GameState.PLAYING)

	func PauseGame() -> void:
		CurrentState = int(UnitConstants.GameState.PAUSED)
		get_tree().paused = true
		StateChanged.emit(CurrentState)

	func ResumeGame() -> void:
		CurrentState = int(UnitConstants.GameState.PLAYING)
		get_tree().paused = false
		StateChanged.emit(CurrentState)


var _original_mode: BattleContext.BattleMode


func before_each() -> void:
	_original_mode = BattleContext.current_mode
	get_tree().paused = false


func after_each() -> void:
	get_tree().paused = false
	BattleContext.current_mode = _original_mode


func test_offline_menu_pauses_and_offers_confirmed_forfeit() -> void:
	BattleContext.current_mode = BattleContext.BattleMode.ENCOUNTER
	var controller: FakeGameController = FakeGameController.new()
	controller.add_to_group(GroupIDs.GAME_CONTROLLER)
	add_child_autofree(controller)
	var menu: PauseMenu = _instantiate_menu()
	await get_tree().process_frame

	controller.PauseGame()
	assert_true(get_tree().paused)
	assert_true(menu.visible)
	assert_eq(menu.title_label.text, Loc.t("ui.pause_menu.battle_paused"))
	assert_eq(menu.quit_button.text, Loc.t("ui.pause_menu.forfeit"))
	menu._on_quit_pressed()
	assert_true(menu.forfeit_confirmation.visible)
	menu.forfeit_confirmation.hide()
	controller.ResumeGame()


func test_online_menu_opens_without_pausing_and_offers_confirmed_forfeit() -> void:
	BattleContext.current_mode = BattleContext.BattleMode.MULTIPLAYER
	var controller: FakeGameController = FakeGameController.new()
	controller.add_to_group(GroupIDs.GAME_CONTROLLER)
	add_child_autofree(controller)
	var menu: PauseMenu = _instantiate_menu()
	await get_tree().process_frame

	menu.toggle_menu()
	assert_true(menu.visible)
	assert_false(get_tree().paused)
	assert_eq(menu.title_label.text, Loc.t("ui.pause_menu.battle_menu"))
	menu._on_quit_pressed()
	assert_true(menu.forfeit_confirmation.visible)
	menu.forfeit_confirmation.hide()
	menu._on_resume_pressed()
	assert_false(menu.visible)
	assert_false(get_tree().paused)


func _instantiate_menu() -> PauseMenu:
	var scene: PackedScene = load("res://scenes/battle/ui/pause_menu.tscn")
	var menu: PauseMenu = scene.instantiate() as PauseMenu
	add_child_autofree(menu)
	return menu
