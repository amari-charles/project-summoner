extends GutTest


func test_speed_button_is_enabled_for_academy_battles() -> void:
	var original_mode: BattleContext.BattleMode = BattleContext.current_mode
	BattleContext.current_mode = BattleContext.BattleMode.ENCOUNTER

	var button: SpeedButton = SpeedButton.new()
	button._check_battle_mode()

	assert_false(button.disabled, "Academy class battles should allow 2x speed")

	button.free()
	BattleContext.current_mode = original_mode
