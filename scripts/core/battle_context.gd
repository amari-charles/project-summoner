extends Node

## Global battle configuration system
## Decouples battle scene from specific modes (campaign, arena, endless, etc.)
##
## Usage:
##   1. Before loading battle, configure this singleton with mode-specific data
##   2. Battle scene reads configuration from here
##   3. After battle, this calls the appropriate completion handler

enum BattleMode {
	CAMPAIGN,   ## Story progression battles
	ARENA,      ## Random battles for rewards
	ENDLESS,    ## Wave-based survival mode
	TUTORIAL,   ## Guided learning battles
	PRACTICE    ## Free play / testing
}

## Current battle mode
var current_mode: BattleMode = BattleMode.PRACTICE

## Battle configuration (enemy deck, HP, AI, etc.)
var battle_config: Dictionary = {}

## Biome ID for visual theme
var biome_id: String = "summer_plains"

## Callback to execute when battle ends
## Signature: func(winner: int) where 0 = player, 1 = enemy
var completion_callback: Callable

## Configure for campaign battle
func configure_campaign_battle(battle_id: String) -> void:
	current_mode = BattleMode.CAMPAIGN

	print("BattleContext: configure_campaign_battle() called with battle_id='%s'" % battle_id)

	var campaign: Node = get_node_or_null("/root/Campaign")
	if not campaign:
		push_error("BattleContext: Campaign service not found")
		return

	print("BattleContext: Campaign service found, calling get_battle('%s')..." % battle_id)

	if campaign.has_method("get_battle"):
		var result: Variant = campaign.call("get_battle", battle_id)
		print("BattleContext: get_battle() returned type: %s, is_empty: %s" % [type_string(typeof(result)), result.is_empty() if result is Dictionary else "N/A"])

		if result is Dictionary:
			battle_config = result
			if battle_config.is_empty():
				push_error("BattleContext: Battle config is empty for battle '%s'" % battle_id)
		else:
			push_error("BattleContext: get_battle() did not return a Dictionary for battle '%s'" % battle_id)
	else:
		push_error("BattleContext: Campaign service does not have get_battle() method")

	if battle_config.is_empty():
		push_error("BattleContext: CRITICAL - Cannot configure battle '%s', battle_config is empty!" % battle_id)
		push_error("BattleContext: This will cause enemy deck loading to fail")

	biome_id = battle_config.get("biome_id", "summer_plains")
	completion_callback = _handle_campaign_completion

	# Get enemy deck size safely
	var enemy_deck_variant: Variant = battle_config.get("enemy_deck", [])
	var enemy_deck_size: int = 0
	if enemy_deck_variant is Array:
		var enemy_deck_array: Array = enemy_deck_variant
		enemy_deck_size = enemy_deck_array.size()

	print("BattleContext: Configured campaign battle '%s' (has enemy_deck: %s, enemy_deck size: %d)" % [
		battle_id,
		battle_config.has("enemy_deck"),
		enemy_deck_size
	])

## Configure for practice/test battle
func configure_practice_battle(config: Dictionary = {}) -> void:
	current_mode = BattleMode.PRACTICE

	# Use provided config or defaults
	battle_config = config if not config.is_empty() else {
		"enemy_deck": [{"catalog_id": "slime_green", "count": 1}],
		"enemy_hp": 300.0,
		"ai_type": "scripted"
	}

	biome_id = config.get("biome_id", "summer_plains")
	completion_callback = _handle_practice_completion

	print("BattleContext: Configured practice battle")

## Configure for arena battle (future)
func configure_arena_battle(_difficulty: int) -> void:
	current_mode = BattleMode.ARENA

	# TODO: ArenaService would generate random battle config
	push_warning("BattleContext: Arena mode not yet implemented")

	biome_id = "summer_plains"  # Random biome selection later
	completion_callback = _handle_arena_completion

## Configure for endless mode (future)
func configure_endless_wave(_wave_number: int) -> void:
	current_mode = BattleMode.ENDLESS

	# TODO: EndlessService would provide wave config
	push_warning("BattleContext: Endless mode not yet implemented")

	biome_id = "summer_plains"
	completion_callback = _handle_endless_completion

## Clear battle context
func clear() -> void:
	battle_config = {}
	biome_id = "summer_plains"
	completion_callback = Callable()
	print("BattleContext: Cleared")

## Handle campaign battle completion
func _handle_campaign_completion(winner: int) -> void:
	var campaign: Node = get_node_or_null("/root/Campaign")
	if not campaign:
		push_error("BattleContext: Campaign service not found for completion")
		return

	if winner == 0:  # Player won
		# Transition to reward screen (it will handle completion and rewards)
		SceneManager.change_scene(SceneManager.SCENE_REWARD_SCREEN)
	else:  # Player lost
		# Return to campaign screen
		# TODO: Track origin screen to return to correct location (arena, practice, etc.)
		SceneManager.change_scene(SceneManager.SCENE_CAMPAIGN_MAP)

## Handle practice battle completion
func _handle_practice_completion(winner: int) -> void:
	print("BattleContext: Practice battle ended, winner: %d" % winner)

	# For practice mode, just show result and stay in scene
	# Or return to main menu
	# TODO: Implement practice mode UI
	print("Practice battle complete - no progression")

## Handle arena battle completion (future)
func _handle_arena_completion(winner: int) -> void:
	print("BattleContext: Arena battle ended, winner: %d" % winner)
	# TODO: Update leaderboard, grant arena rewards, show result screen

## Handle endless wave completion (future)
func _handle_endless_completion(winner: int) -> void:
	print("BattleContext: Endless wave ended, winner: %d" % winner)

	if winner == 0:  # Player won wave
		# Increment wave, reload battle
		# TODO: Implement endless progression
		pass
	else:  # Player lost
		# Show endless result screen with score
		# TODO: Implement endless result screen
		pass
