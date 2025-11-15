extends Node
class_name CampaignService

## CampaignService - Manages campaign progression and battle rewards
##
## Tracks which battles have been completed and handles reward distribution.
## Battle definitions and progression are managed here.

## Signals
signal battle_completed(battle_id: String)
signal battle_unlocked(battle_id: String)
signal campaign_progress_changed()

## Dependencies
var _profile_repo: Node = null
var _collection: Node = null

## Battle data structure
const BattleData: Dictionary = {
	"id": "",
	"name": "",
	"description": "",
	"difficulty": 1,  # 1-5
	"reward_type": "",  # "fixed", "choice", "random"
	"reward_cards": [],  # Array of {catalog_id, rarity, count}
	"enemy_deck": [],  # For now, placeholder
	"unlock_requirements": []  # Array of battle_ids that must be completed
}

## Campaign battles (placeholder data)
var _battles: Dictionary = {}

## Current profile's campaign progress
var _completed_battles: Array[String] = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignService: Initializing...")

	# Initialize dependencies (autoloads are always available in _ready)
	_profile_repo = get_node("/root/ProfileRepo")
	_collection = get_node("/root/Collection")

	_init_battles()
	_load_progress()

	# Reload progress when profile changes (e.g., on reset)
	if _profile_repo and _profile_repo.has_signal("data_changed"):
		var data_changed_signal: Signal = _profile_repo.get("data_changed")
		data_changed_signal.connect(_on_profile_data_changed)

func _on_profile_data_changed() -> void:
	print("CampaignService: Profile data changed - reloading progress...")
	_load_progress()

## =============================================================================
## BATTLE DEFINITIONS
## =============================================================================

func _init_battles() -> void:
	# TODO: Ensure reward_cards here stay in sync with what's displayed in the campaign menu UI
	# When updating battle rewards, also update the corresponding UI display in campaign_screen.gd
	# to prevent divergence between advertised and actual rewards

	# Battle 0: Tutorial - First card
	_battles["battle_00"] = {
		"id": "battle_00",
		"biome_id": "summer_plains",
		"name": "First Summons",
		"description": "Learn the basics of summoning. Win to earn your first card!",
		"difficulty": 1,
		"is_tutorial": true,  # Tutorial battle - deck editing locked
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "fire_recruit", "rarity": "common", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "fire_recruit", "count": 1}
		],
		"enemy_hp": 30.0,  # Very low HP for tutorial (2 hits)
		"unlock_requirements": [],
		# AI Configuration
		"ai_type": "scripted",
		"ai_script": [
			{"delay": 0.0, "card_name": "Fire Recruit", "position": {"x": 1400, "y": 540}}
		]
	}

	# Battle 1: Building army
	_battles["battle_01"] = {
		"id": "battle_01",
		"biome_id": "summer_plains",
		"name": "Building Your Army",
		"description": "Expand your forces. Choose your reward.",
		"difficulty": 1,
		"is_tutorial": true,  # Tutorial battle - deck editing locked
		"reward_type": "choice",
		"reward_cards": [
			{"catalog_id": "fire_recruit", "rarity": "common", "count": 1},
			{"catalog_id": "ember_slinger", "rarity": "common", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "fire_recruit", "count": 2}
		],
		"enemy_hp": 100.0,
		"unlock_requirements": ["battle_00"],
		# AI Configuration
		"ai_type": "heuristic",
		"ai_personality": "defensive",
		"ai_difficulty": 1,
		"ai_config": {
			"play_interval_min": 4.0,
			"play_interval_max": 7.0
		}
	}

	# Battle 2: Fortification
	_battles["battle_02"] = {
		"id": "battle_02",
		"biome_id": "summer_plains",
		"name": "Flames Rising",
		"description": "Face mixed fire forces. Earn a swift charger.",
		"difficulty": 2,
		"is_tutorial": true,  # Last tutorial battle - deck editing unlocks after this
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "blaze_rider", "rarity": "common", "count": 2}
		],
		"enemy_deck": [
			{"catalog_id": "fire_recruit", "count": 2},
			{"catalog_id": "ember_slinger", "count": 1}
		],
		"enemy_hp": 250.0,
		"unlock_requirements": ["battle_01"],
		# AI Configuration
		"ai_type": "heuristic",
		"ai_personality": "balanced",
		"ai_difficulty": 2,
		"ai_config": {
			"play_interval_min": 3.0,
			"play_interval_max": 6.0
		}
	}

	# Battle 3: Random reward
	_battles["battle_03"] = {
		"id": "battle_03",
		"biome_id": "summer_plains",
		"name": "Growing Power",
		"description": "Test your strength. Random reward awaits.",
		"difficulty": 2,
		"reward_type": "random",
		"reward_cards": [
			{"catalog_id": "fire_recruit", "rarity": "common", "count": 2},
			{"catalog_id": "ember_slinger", "rarity": "common", "count": 2},
			{"catalog_id": "blaze_rider", "rarity": "common", "count": 2},
			{"catalog_id": "ash_vanguard", "rarity": "rare", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "fire_recruit", "count": 3},
			{"catalog_id": "ember_slinger", "count": 2},
			{"catalog_id": "blaze_rider", "count": 1},
			{"catalog_id": "ash_vanguard", "count": 1}
		],
		"enemy_hp": 400.0,
		"unlock_requirements": ["battle_02"],
		# AI Configuration
		"ai_type": "heuristic",
		"ai_personality": "aggressive",
		"ai_difficulty": 3,
		"ai_config": {
			"play_interval_min": 2.5,
			"play_interval_max": 5.0
		}
	}

	# Battle 4: Fire onslaught
	_battles["battle_04"] = {
		"id": "battle_04",
		"biome_id": "summer_plains",
		"name": "Inferno Assault",
		"description": "Face the full fury of fire! Rare units await.",
		"difficulty": 3,
		"reward_type": "choice",
		"reward_count": 1,
		"reward_cards": [
			{"catalog_id": "ash_vanguard", "rarity": "rare", "count": 1},
			{"catalog_id": "ember_guard", "rarity": "rare", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "fire_recruit", "count": 4},
			{"catalog_id": "ember_slinger", "count": 3},
			{"catalog_id": "blaze_rider", "count": 2},
			{"catalog_id": "ash_vanguard", "count": 1},
			{"catalog_id": "ember_guard", "count": 1}
		],
		"enemy_hp": 600.0,
		"unlock_requirements": ["battle_03"],
		# AI Configuration
		"ai_type": "heuristic",
		"ai_personality": "aggressive",
		"ai_difficulty": 4,
		"ai_config": {
			"play_interval_min": 2.0,
			"play_interval_max": 4.0
		}
	}

	print("CampaignService: Loaded %d battles" % _battles.size())

## =============================================================================
## PROGRESS MANAGEMENT
## =============================================================================

func _load_progress() -> void:
	if not _profile_repo:
		push_error("CampaignService: ProfileRepository not found!")
		return

	var profile: Dictionary = {}
	if _profile_repo.has_method("get_active_profile"):
		var result: Variant = _profile_repo.call("get_active_profile")
		if result is Dictionary:
			profile = result
	if profile.is_empty():
		push_warning("CampaignService: No active profile")
		return

	var empty_progress: Dictionary = {}
	var campaign_progress: Dictionary = profile.get("campaign_progress", empty_progress)
	var completed_battles_raw: Array = campaign_progress.get("completed_battles", [])
	_completed_battles.clear()
	for battle_id: Variant in completed_battles_raw:
		if battle_id is String:
			_completed_battles.append(battle_id)
	print("CampaignService: Loaded progress - %d battles completed" % _completed_battles.size())

func save_progress() -> void:
	if not _profile_repo:
		return

	var profile: Dictionary = {}
	if _profile_repo.has_method("get_active_profile"):
		var result: Variant = _profile_repo.call("get_active_profile")
		if result is Dictionary:
			profile = result
	if profile.is_empty():
		return

	if not profile.has("campaign_progress"):
		profile["campaign_progress"] = {}

	var campaign_progress_variant: Variant = profile["campaign_progress"]
	if not campaign_progress_variant is Dictionary:
		push_error("CampaignService: profile['campaign_progress'] is not a Dictionary")
		return
	var campaign_progress: Dictionary = campaign_progress_variant
	campaign_progress["completed_battles"] = _completed_battles.duplicate()

	if _profile_repo.has_method("save_profile"):
		_profile_repo.call("save_profile", true)  # Force immediate save
	campaign_progress_changed.emit()
	print("CampaignService: Saved progress - %d battles completed" % _completed_battles.size())

## =============================================================================
## BATTLE QUERIES
## =============================================================================

func get_all_battles() -> Array[Dictionary]:
	var battles: Array[Dictionary] = []
	for battle_id: String in _battles.keys():
		battles.append(_battles[battle_id])
	return battles

func get_battle(battle_id: String) -> Dictionary:
	var empty_battle: Dictionary = {}
	return _battles.get(battle_id, empty_battle)

func is_battle_completed(battle_id: String) -> bool:
	return battle_id in _completed_battles

func is_battle_unlocked(battle_id: String) -> bool:
	var battle: Dictionary = get_battle(battle_id)
	if battle.is_empty():
		return false

	# Check if all required battles are completed
	var requirements: Array = battle.get("unlock_requirements", [])
	for req_id: Variant in requirements:
		if req_id is String:
			var req_id_str: String = req_id
			if not is_battle_completed(req_id_str):
				return false

	return true

func get_available_battles() -> Array[Dictionary]:
	var available: Array[Dictionary] = []
	for battle: Dictionary in get_all_battles():
		var battle_id: String = battle.get("id", "")
		if is_battle_unlocked(battle_id) and not is_battle_completed(battle_id):
			available.append(battle)
	return available

func get_completed_battles() -> Array[Dictionary]:
	var completed: Array[Dictionary] = []
	for battle_id: String in _completed_battles:
		var battle: Dictionary = get_battle(battle_id)
		if not battle.is_empty():
			completed.append(battle)
	return completed

## =============================================================================
## BATTLE COMPLETION & REWARDS
## =============================================================================

func complete_battle(battle_id: String) -> void:
	if is_battle_completed(battle_id):
		push_warning("CampaignService: Battle '%s' already completed" % battle_id)
		return

	_completed_battles.append(battle_id)
	save_progress()
	battle_completed.emit(battle_id)

	# Check for newly unlocked battles
	_check_unlocked_battles()

	print("CampaignService: Battle '%s' completed" % battle_id)

func _check_unlocked_battles() -> void:
	for battle: Dictionary in get_all_battles():
		var battle_id: String = battle.get("id", "")
		if is_battle_unlocked(battle_id) and not is_battle_completed(battle_id):
			# Check if it was just unlocked (not in previous available list)
			battle_unlocked.emit(battle_id)

func grant_battle_reward(battle_id: String, chosen_index: int = 0) -> Dictionary:
	var battle: Dictionary = get_battle(battle_id)
	if battle.is_empty():
		push_error("CampaignService: Battle not found: %s" % battle_id)
		var empty_result: Dictionary = {}
		return empty_result

	var reward_type: String = battle.get("reward_type", "fixed")
	var reward_cards: Array = battle.get("reward_cards", [])

	if reward_cards.is_empty():
		push_warning("CampaignService: No rewards defined for battle '%s'" % battle_id)
		var empty_rewards: Dictionary = {}
		return empty_rewards

	var granted_card: Dictionary = {}
	var granted_instance_ids: Array[String] = []  # Track actual card instance IDs

	match reward_type:
		"fixed":
			# Grant all reward cards
			for reward: Variant in reward_cards:
				if reward is Dictionary:
					var reward_dict: Dictionary = reward
					var ids: Array[String] = _grant_reward_card(reward_dict)
					granted_instance_ids.append_array(ids)
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				granted_card = reward_cards[0]  # Return first for display

		"choice":
			# Player chooses one from the list
			if chosen_index >= 0 and chosen_index < reward_cards.size():
				var chosen_reward_variant: Variant = reward_cards[chosen_index]
				if not chosen_reward_variant is Dictionary:
					push_error("CampaignService: reward_cards[%d] is not a Dictionary" % chosen_index)
					return {}
				var chosen_reward: Dictionary = chosen_reward_variant
				var ids: Array[String] = _grant_reward_card(chosen_reward)
				granted_instance_ids.append_array(ids)
				granted_card = chosen_reward
			else:
				push_error("CampaignService: Invalid choice index %d" % chosen_index)

		"random":
			# Pick random card from pool
			var random_reward_variant: Variant = reward_cards[randi() % reward_cards.size()]
			if not random_reward_variant is Dictionary:
				push_error("CampaignService: random reward_cards entry is not a Dictionary")
				return {}
			var random_reward: Dictionary = random_reward_variant
			var ids: Array[String] = _grant_reward_card(random_reward)
			granted_instance_ids.append_array(ids)
			granted_card = random_reward

	# Add instance IDs to return value
	granted_card["instance_ids"] = granted_instance_ids
	return granted_card

func _grant_reward_card(reward: Dictionary) -> Array[String]:
	var instance_ids: Array[String] = []

	if not _collection:
		push_error("CampaignService: Collection service not found!")
		return instance_ids

	var catalog_id: String = reward.get("catalog_id", "")
	var rarity: String = reward.get("rarity", "common")
	var count: int = reward.get("count", 1)

	for i: int in range(count):
		var instance_id: String = ""
		if _collection.has_method("grant_card"):
			var result: Variant = _collection.call("grant_card", catalog_id, rarity)
			if result is String:
				instance_id = result
		instance_ids.append(instance_id)

	print("CampaignService: Granted %dx %s (%s)" % [count, catalog_id, rarity])
	return instance_ids

## =============================================================================
## TUTORIAL HELPERS
## =============================================================================

## Check if a specific battle is a tutorial battle
func is_battle_tutorial(battle_id: String) -> bool:
	var battle: Dictionary = get_battle(battle_id)
	return battle.get("is_tutorial", false)

## Check if all tutorial battles have been completed
func is_tutorial_complete() -> bool:
	# Get all tutorial battles
	var tutorial_battles: Array[String] = []
	for battle: Dictionary in get_all_battles():
		if battle.get("is_tutorial", false):
			var battle_id: String = battle.get("id", "")
			tutorial_battles.append(battle_id)

	# Check if all are completed
	for battle_id: String in tutorial_battles:
		if not is_battle_completed(battle_id):
			return false

	return true

## Get list of all tutorial battle IDs
func get_tutorial_battles() -> Array[String]:
	var tutorial_battles: Array[String] = []
	for battle: Dictionary in get_all_battles():
		if battle.get("is_tutorial", false):
			var battle_id: String = battle.get("id", "")
			tutorial_battles.append(battle_id)
	return tutorial_battles
