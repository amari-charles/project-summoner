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
var biome_id: StringName = BiomeIDs.SUMMER_PLAINS

## Track if battle was configured (for debugging)
var was_configured: bool = false

## Callback to execute when battle ends
## Signature: func(winner: int) where 0 = player, 1 = enemy
var completion_callback: Callable

## Cards played during this battle (for XP rewards)
## Array of card instance IDs
var _cards_played: Array[String] = []

## Player hero's computed stats (cached at battle start for damage calculations)
## Set by Summoner._apply_hero_bonuses(), read by DamageSystem
var _player_hero_stats: Dictionary = {}

## Configure for campaign battle
func configure_campaign_battle(battle_id: String) -> void:
	current_mode = BattleMode.CAMPAIGN
	was_configured = true

	print("BattleContext: configure_campaign_battle() called with battle_id='%s'" % battle_id)

	# Campaign is an autoload, access it directly
	battle_config = Campaign.get_battle(battle_id)

	if battle_config.is_empty():
		push_error("BattleContext: CRITICAL - Cannot configure battle '%s', battle_config is empty!" % battle_id)
		push_error("BattleContext: This will cause enemy deck loading to fail")

	biome_id = battle_config.get("biome_id", BiomeIDs.SUMMER_PLAINS)
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
	was_configured = true

	# Use provided config or defaults
	battle_config = config if not config.is_empty() else {
		"enemy_deck": [{"catalog_id": "slime_green", "count": 1}],
		"enemy_hp": 300.0,
		"ai_type": "scripted"
	}

	biome_id = config.get("biome_id", BiomeIDs.SUMMER_PLAINS)
	completion_callback = _handle_practice_completion

	print("BattleContext: Configured practice battle")

## Configure for arena battle (future)
func configure_arena_battle(_difficulty: int) -> void:
	current_mode = BattleMode.ARENA

	# TODO: ArenaService would generate random battle config
	push_warning("BattleContext: Arena mode not yet implemented")

	biome_id = BiomeIDs.SUMMER_PLAINS  # Random biome selection later
	completion_callback = _handle_arena_completion

## Configure for endless mode (future)
func configure_endless_wave(_wave_number: int) -> void:
	current_mode = BattleMode.ENDLESS

	# TODO: EndlessService would provide wave config
	push_warning("BattleContext: Endless mode not yet implemented")

	biome_id = BiomeIDs.SUMMER_PLAINS
	completion_callback = _handle_endless_completion

## Check if battle context has been configured
func is_configured() -> bool:
	return was_configured and not battle_config.is_empty()

## Clear battle context
func clear() -> void:
	battle_config = {}
	biome_id = BiomeIDs.SUMMER_PLAINS
	completion_callback = Callable()
	was_configured = false
	_cards_played.clear()
	_player_hero_stats.clear()
	print("BattleContext: Cleared")

## =============================================================================
## PLAYER HERO STATS (for DamageSystem)
## =============================================================================

## Set player hero stats (called by Summoner when hero is loaded)
func set_player_hero_stats(stats: Dictionary) -> void:
	_player_hero_stats = stats.duplicate()
	print("BattleContext: Cached player hero stats - damage_bonus: %.0f%%, damage_reduction: %.0f" % [
		_player_hero_stats.get("damage_bonus", 0.0),
		_player_hero_stats.get("damage_reduction", 0.0)
	])

## Get player hero stats (called by DamageSystem)
func get_player_hero_stats() -> Dictionary:
	return _player_hero_stats

## Get a specific player hero stat
func get_player_hero_stat(stat_name: String, default_value: float = 0.0) -> float:
	return _player_hero_stats.get(stat_name, default_value)

## Reset battle context (alias for clear, called between battles)
func reset() -> void:
	clear()

## =============================================================================
## CARD XP TRACKING
## =============================================================================

## Register a card as played during this battle (for XP rewards)
## Called by card hand manager when a card is successfully played
func register_card_played(card_instance_id: String) -> void:
	if card_instance_id.is_empty():
		return
	# Only track unique plays (don't double-count if same card played twice)
	if card_instance_id not in _cards_played:
		_cards_played.append(card_instance_id)
		print("BattleContext: Registered card played: %s (total: %d)" % [card_instance_id, _cards_played.size()])

## Get list of cards played this battle
func get_cards_played() -> Array[String]:
	return _cards_played.duplicate()

## Grant XP to all cards played in this battle
## Called on battle victory
func grant_xp_to_played_cards() -> void:
	var card_xp: int = battle_config.get("card_xp_reward", 0)
	if card_xp <= 0:
		print("BattleContext: No card XP reward configured for this battle")
		return

	if _cards_played.is_empty():
		print("BattleContext: No cards were played this battle")
		return

	print("BattleContext: Granting %d XP to %d played cards" % [card_xp, _cards_played.size()])
	var progression_node: Node = get_node_or_null("/root/CardProgression")
	if progression_node:
		progression_node.call("grant_xp_to_cards", _cards_played, card_xp)
	else:
		push_warning("BattleContext: CardProgression autoload not found")

## Grant XP to the active hero
## Called on battle victory
func grant_xp_to_active_hero() -> void:
	var hero_xp: int = battle_config.get("hero_xp_reward", 0)
	if hero_xp <= 0:
		print("BattleContext: No hero XP reward configured for this battle")
		return

	print("BattleContext: Granting %d XP to active hero" % hero_xp)
	var hero_progression: Node = get_node_or_null("/root/HeroProgression")
	if hero_progression:
		var new_xp: int = hero_progression.call("grant_active_hero_xp", hero_xp)
		print("BattleContext: Hero now has %d XP" % new_xp)
	else:
		push_warning("BattleContext: HeroProgression autoload not found")

## Handle campaign battle completion
func _handle_campaign_completion(winner: int) -> void:
	if winner == 0:  # Player won
		# Grant XP to cards played during battle
		grant_xp_to_played_cards()
		# Grant XP to the active hero
		grant_xp_to_active_hero()
		# Transition to reward screen (it will handle completion and rewards)
		SceneManager.transition_to(SceneManager.SCENE_REWARD_SCREEN)
	else:  # Player lost
		# Return to campaign screen
		# TODO: Track origin screen to return to correct location (arena, practice, etc.)
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

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
