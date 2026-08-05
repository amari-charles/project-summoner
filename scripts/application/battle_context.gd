extends Node

## Global battle configuration and state tracking.
## Thin data bag — holds config, state, and data for the current battle.
## Business logic (XP granting, match reporting, scene transitions) lives in BattleScene.
##
## Usage:
##   1. Before loading battle, configure this singleton with mode-specific data
##   2. BattleScene reads configuration via BattleSessionConfig.FromBattleContext()
##   3. BattleScene owns post-battle completion logic

enum BattleMode {
	CAMPAIGN,   ## Story progression battles
	ARENA,      ## Random battles for rewards
	ENDLESS,    ## Wave-based survival mode
	TUTORIAL,   ## Guided learning battles
	PRACTICE,   ## Free play / testing
	MULTIPLAYER, ## P2P or online multiplayer
	ACADEMY ## Academy class activity battle
}

## Battle lifecycle state machine
## Tracks the current state of the battle for proper cleanup and validation
enum BattleState {
	NONE,        ## No battle configured
	CONFIGURED,  ## Battle configured, ready to start
	IN_PROGRESS, ## Battle actively running
	VICTORY,     ## Player won, awaiting rewards
	DEFEAT,      ## Player lost
	ABANDONED    ## Player quit mid-battle
}

## Practice battle defaults
const PRACTICE_ENEMY_CATALOG_ID: String = "pebbloom"
const PRACTICE_ENEMY_HP: float = 300.0
const PRACTICE_AI_TYPE: String = "scripted"

## Current battle mode
var current_mode: BattleMode = BattleMode.PRACTICE

## Current battle state (lifecycle tracking)
var battle_state: BattleState = BattleState.NONE

## Battle configuration (enemy deck, HP, AI, etc.)
var battle_config: Dictionary = {}

## The battle ID for the current configuration
var _battle_id: String = ""

## Authority-created occurrence identity for campaign progression.
var _battle_attempt_id: String = ""

## Academy course/activity IDs for academy battle completion.
var academy_course_id: String = ""
var academy_activity_id: String = ""

## Typed event accessor for battle_config
## Provides type-safe access to event properties
var battle_event: TypedEventData:
	get:
		if battle_config.is_empty():
			return TypedEventData.new({}, "")
		return TypedEventData.new(battle_config, _battle_id)

## Biome ID for visual theme
var biome_id: StringName = BiomeIDs.SUMMER_PLAINS

## Track if battle was configured (for debugging)
var was_configured: bool = false

## Set to true to enable verbose logging
var debug_mode: bool = false

## Scene to return to after battle (campaign map, arena menu, etc.)
var origin_scene: String = ""

## Optional legacy authority provider bridge for multiplayer.
## Session architecture no longer owns authority here, but C# interop still
## reads this property during migration.
var authority_provider: Object = null

## Level cap for this battle (0 = uncapped)
## When set, player cards are normalized to this level maximum
var _level_cap: int = 0

## Ranked match info for match reporting
## Contains: match_id, opponent_user_id, opponent_username, opponent_rating, is_ranked
var _ranked_match_info: Dictionary = {}

## Player summoner's computed stats (cached at battle start for damage calculations)
## Set by Summoner._apply_summoner_bonuses(), read by DamageSystem
var _player_summoner_stats: Dictionary = {}

## Configure for campaign battle
func configure_campaign_battle(battle_id: String) -> void:
	current_mode = BattleMode.CAMPAIGN
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = _get_campaign_battle_origin_scene()
	_battle_id = battle_id

	if debug_mode: print("BattleContext: configure_campaign_battle() called with battle_id='%s'" % battle_id)

	# Campaign is an autoload, access it directly
	battle_config = CampaignApi.get_battle(battle_id)

	if battle_config.is_empty():
		push_error("BattleContext: CRITICAL - Cannot configure battle '%s', battle_config is empty!" % battle_id)
		push_error("BattleContext: This will cause enemy deck loading to fail")

	biome_id = StringName(battle_event.biome_id) if not battle_event.biome_id.is_empty() else BiomeIDs.SUMMER_PLAINS

	# Set level cap if configured (use typed accessor)
	_level_cap = battle_event.level_cap
	if _level_cap > 0:
		if debug_mode: print("BattleContext: Level cap set to %d" % _level_cap)

	# Get enemy deck size safely
	var enemy_side: Dictionary = battle_config.get("enemy_side", {})
	var enemy_deck: Dictionary = enemy_side.get("deck", {})
	var enemy_deck_variant: Variant = enemy_deck.get("cards", [])
	var enemy_deck_size: int = 0
	if enemy_deck_variant is Array:
		var enemy_deck_array: Array = enemy_deck_variant
		enemy_deck_size = enemy_deck_array.size()

	if debug_mode: print("BattleContext: Configured campaign battle '%s' (has enemy_deck: %s, enemy_deck size: %d)" % [
		battle_id,
		enemy_side.has("deck"),
		enemy_deck_size
	])

## Configure for practice/test battle
func configure_practice_battle(config: Dictionary = {}) -> void:
	current_mode = BattleMode.PRACTICE
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = SceneManager.SCENE_CAMPAIGN_MAP

	# Use provided config or defaults
	battle_config = config.duplicate(true) if not config.is_empty() else {
		"enemy_side": _build_authored_enemy_side(
			PRACTICE_ENEMY_HP,
			[{"catalog_id": PRACTICE_ENEMY_CATALOG_ID, "count": 1}],
			PRACTICE_AI_TYPE
		)
	}

	biome_id = config.get("biome_id", BiomeIDs.SUMMER_PLAINS)

	if debug_mode: print("BattleContext: Configured practice battle")

## Configure for academy course activity battle
func select_academy_course(course_id: String) -> void:
	academy_course_id = course_id
	academy_activity_id = ""

func configure_academy_battle(course_id: String, activity_id: String, config: Dictionary = {}) -> void:
	current_mode = BattleMode.ACADEMY
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = SceneManager.SCENE_ACADEMY_COURSE_PATH
	_battle_id = activity_id
	academy_course_id = course_id
	academy_activity_id = activity_id

	battle_config = config.duplicate(true) if not config.is_empty() else {
		"enemy_side": _build_authored_enemy_side(
			20.0,
			[{"catalog_id": PRACTICE_ENEMY_CATALOG_ID, "count": 1}],
			"passive",
			0,
			999.0,
			999.0
		)
	}

	biome_id = config.get("biome_id", BiomeIDs.SUMMER_PLAINS)

	if debug_mode: print("BattleContext: Configured academy battle '%s' for course '%s'" % [activity_id, course_id])

func _build_authored_enemy_side(
	enemy_hp: float,
	enemy_deck: Array,
	ai_type: String,
	ai_difficulty: int = 3,
	play_interval_min: float = 3.0,
	play_interval_max: float = 6.0
) -> Dictionary:
	return {
		"team": 1,
		"source": "authored",
		"summoner": {
			"source": "authored",
			"id": "authored_enemy",
			"display_name": "Authored Enemy",
			"hp": enemy_hp,
			"max_hp": enemy_hp,
			"mana": 100.0,
			"max_mana": 100.0,
			"cast_speed": 1.0,
			"damage_bonus": 0.0,
			"damage_reduction": 0.0,
			"soul_strength": 0.0
		},
		"deck": {
			"source": "authored",
			"cards": enemy_deck
		},
		"controller": {
			"kind": "trainer_ai",
			"ai_type": ai_type,
			"ai_difficulty": ai_difficulty,
			"ai_config": {
				"play_interval_min": play_interval_min,
				"play_interval_max": play_interval_max
			}
		}
	}

## Configure for arena battle (future)
func configure_arena_battle(_difficulty: int) -> void:
	current_mode = BattleMode.ARENA
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = SceneManager.SCENE_CAMPAIGN_MAP

	# TODO: ArenaService would generate random battle config
	push_warning("BattleContext: Arena mode not yet implemented")

	biome_id = BiomeIDs.SUMMER_PLAINS  # Random biome selection later

## Configure for endless mode (future)
func configure_endless_wave(_wave_number: int) -> void:
	current_mode = BattleMode.ENDLESS
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = SceneManager.SCENE_CAMPAIGN_MAP

	# TODO: EndlessService would provide wave config
	push_warning("BattleContext: Endless mode not yet implemented")

	biome_id = BiomeIDs.SUMMER_PLAINS

## Configure for multiplayer battle
func configure_multiplayer_battle(
	player_summoner_id: String,
	opponent_summoner_id: String,
	player_deck: Array,
	opponent_deck: Array,
	is_host: bool,
	battle_seed: int,
	opponent_summoner_data: Dictionary = {}
) -> void:
	current_mode = BattleMode.MULTIPLAYER
	battle_state = BattleState.CONFIGURED
	was_configured = true
	origin_scene = SceneManager.SCENE_MULTIPLAYER_LOBBY

	# Build battle config with both players' data
	battle_config = {
		"is_multiplayer": true,
		"is_host": is_host,
		"battle_seed": battle_seed,
		"player_summoner_id": player_summoner_id,
		"opponent_summoner_id": opponent_summoner_id,
		"player_side": {
			"team": 0,
			"source": "profile",
			"summoner": {"source": "profile"},
			"deck": {"source": "authored", "cards": player_deck},
			"controller": {"kind": "player"}
		},
		"enemy_side": {
			"team": 1,
			"source": "multiplayer_opponent",
			"summoner": {"source": "multiplayer_opponent"},
			"deck": {"source": "authored", "cards": opponent_deck},
			"controller": {"kind": "network"}
		},
		# Standard battle settings for multiplayer
		"win_condition": "SUMMONER_DESTROYED",
		"opponent_summoner_data": opponent_summoner_data,
	}

	biome_id = BiomeIDs.SUMMER_PLAINS  # Could randomize or let host choose

	if debug_mode: print("BattleContext: Configured multiplayer battle (host: %s, seed: %d)" % [is_host, battle_seed])

## Set ranked match info for match reporting after battle ends
func set_ranked_match_info(info: Dictionary) -> void:
	_ranked_match_info = info


## Get ranked match info
func get_ranked_match_info() -> Dictionary:
	return _ranked_match_info


## Check if this is a ranked match
func is_ranked_match() -> bool:
	return _ranked_match_info.get("is_ranked", false)


## Check if battle context has been configured
func is_configured() -> bool:
	return was_configured and not battle_config.is_empty()

## Clear battle context
func clear() -> void:
	_cleanup_authority_provider()
	battle_config = {}
	_battle_id = ""
	_battle_attempt_id = ""
	academy_course_id = ""
	academy_activity_id = ""
	biome_id = BiomeIDs.SUMMER_PLAINS
	_ranked_match_info = {}
	was_configured = false
	battle_state = BattleState.NONE
	origin_scene = ""
	_player_summoner_stats.clear()
	_level_cap = 0

	if debug_mode: print("BattleContext: Cleared")

## Get the scene to return to after battle
func get_origin_scene() -> String:
	if origin_scene.is_empty():
		return SceneManager.SCENE_CAMPAIGN_MAP
	return origin_scene


func _get_campaign_battle_origin_scene() -> String:
	var current_campaign_id: String = CampaignApi.get_current_campaign_id()
	if StringName(current_campaign_id) == CampaignIDs.TEST_ARENA:
		return SceneManager.SCENE_LEGACY_CAMPAIGN_MAP
	return SceneManager.SCENE_CAMPAIGN_MAP

## Mark battle as started (called by GameController when battle begins)
func start_battle() -> void:
	if battle_state != BattleState.CONFIGURED:
		push_warning("BattleContext: start_battle() called in invalid state: %d" % battle_state)
		return

	battle_state = BattleState.IN_PROGRESS
	if debug_mode: print("BattleContext: Battle started")

## Mark battle as victory (called by GameController on player win)
func end_battle_victory() -> void:
	if battle_state != BattleState.IN_PROGRESS:
		push_warning("BattleContext: end_battle_victory() called in invalid state: %d" % battle_state)
		return
	battle_state = BattleState.VICTORY
	if debug_mode: print("BattleContext: Battle ended - VICTORY")

## Mark battle as defeat (called by GameController on player loss)
func end_battle_defeat() -> void:
	if battle_state != BattleState.IN_PROGRESS:
		push_warning("BattleContext: end_battle_defeat() called in invalid state: %d" % battle_state)
		return
	battle_state = BattleState.DEFEAT
	if debug_mode: print("BattleContext: Battle ended - DEFEAT")

## Abandon battle (called when player quits mid-battle)
## Sets state and clears local tracking. Service cleanup (profile, campaign)
## is handled by BattleScene.AbandonBattle().
func abandon_battle() -> void:
	if battle_state == BattleState.NONE:
		return

	if debug_mode: print("BattleContext: Battle abandoned")
	battle_state = BattleState.ABANDONED

## =============================================================================
## PLAYER SUMMONER STATS (for DamageSystem)
## =============================================================================

## Set player summoner stats (called by Summoner when summoner is loaded)
func set_player_summoner_stats(stats: Dictionary) -> void:
	_player_summoner_stats = stats.duplicate()
	if debug_mode: print("BattleContext: Cached player summoner stats - damage_bonus: %.0f%%, damage_reduction: %.0f" % [
		_player_summoner_stats.get("damage_bonus", 0.0),
		_player_summoner_stats.get("damage_reduction", 0.0)
	])

## Get player summoner stats (called by DamageSystem)
func get_player_summoner_stats() -> Dictionary:
	return _player_summoner_stats

## Get a specific player summoner stat
func get_player_summoner_stat(stat_name: String, default_value: float = 0.0) -> float:
	return _player_summoner_stats.get(stat_name, default_value)

## Reset battle context (alias for clear, called between battles)
func reset() -> void:
	clear()


## Set by the progression authority before campaign scene navigation.
func set_battle_attempt_id(attempt_id: String) -> void:
	_battle_attempt_id = attempt_id


func get_battle_attempt_id() -> String:
	return _battle_attempt_id

## =============================================================================
## AUTHORITY ACCESS (C# INTEROP COMPATIBILITY)
## =============================================================================

## Returns true for single-player, and host-only in multiplayer.
func has_authority() -> bool:
	if authority_provider is Object and is_instance_valid(authority_provider) and authority_provider.has_method("has_authority"):
		return authority_provider.call("has_authority")

	if current_mode != BattleMode.MULTIPLAYER:
		return true

	return battle_config.get("is_host", true)


## Returns true when current battle is multiplayer.
func is_multiplayer_battle() -> bool:
	if authority_provider is Object and is_instance_valid(authority_provider) and authority_provider.has_method("is_multiplayer"):
		return authority_provider.call("is_multiplayer")

	if current_mode == BattleMode.MULTIPLAYER:
		return true

	return battle_config.get("is_multiplayer", false)


## Local peer ID if provided by authority provider (default 0).
func get_local_peer_id() -> int:
	if authority_provider is Object and is_instance_valid(authority_provider) and authority_provider.has_method("get_local_peer_id"):
		return authority_provider.call("get_local_peer_id")
	return 0


## Optional compatibility setter for legacy multiplayer glue.
func set_authority_provider(provider: Object) -> void:
	_cleanup_authority_provider()
	authority_provider = provider

	if authority_provider is Object and is_instance_valid(authority_provider) and authority_provider.has_method("initialize"):
		authority_provider.call("initialize")


func get_authority_provider() -> Object:
	return authority_provider


func _cleanup_authority_provider() -> void:
	if authority_provider is Object and is_instance_valid(authority_provider) and authority_provider.has_method("cleanup"):
		authority_provider.call("cleanup")
	authority_provider = null

## =============================================================================
## LEVEL CAP
## =============================================================================

## Get the level cap for this battle (0 = uncapped)
func get_level_cap() -> int:
	return _level_cap


## Check if this battle has a level cap
func has_level_cap() -> bool:
	return _level_cap > 0


## Get effective level for a card in this battle
func get_effective_card_level(card_level: int) -> int:
	var level_cap_service: Node = get_node_or_null(CSharpAutoloads.LEVEL_CAP_SERVICE)
	if level_cap_service and level_cap_service.has_method("GetEffectiveLevel"):
		var level_val: Variant = level_cap_service.call("GetEffectiveLevel", card_level, _level_cap)
		return SafeTypeUtils.int_val(level_val, card_level)
	# Fallback if service not available
	push_warning("BattleContext: LevelCapService unavailable, using inline fallback for get_effective_card_level")
	if _level_cap <= 0:
		return card_level
	return mini(card_level, _level_cap)


## Get effective upgrades for a card in this battle
func get_effective_card_upgrades(upgrades: Array) -> Array:
	var level_cap_service: Node = get_node_or_null(CSharpAutoloads.LEVEL_CAP_SERVICE)
	if level_cap_service and level_cap_service.has_method("GetEffectiveUpgradesArray"):
		var upgrades_val: Variant = level_cap_service.call("GetEffectiveUpgradesArray", upgrades, _level_cap)
		return SafeTypeUtils.array(upgrades_val)
	# Fallback if service not available
	push_warning("BattleContext: LevelCapService unavailable, using inline fallback for get_effective_card_upgrades")
	if _level_cap <= 0:
		return upgrades.duplicate()
	var max_upgrades: int = _level_cap - 1
	if max_upgrades <= 0:
		return []
	if upgrades.size() <= max_upgrades:
		return upgrades.duplicate()
	return upgrades.slice(0, max_upgrades)
