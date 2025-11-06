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
@onready var _profile_repo = get_node("/root/ProfileRepository")
@onready var _collection = get_node("/root/Collection")

## Battle data structure
const BattleData = {
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
var _completed_battles: Array = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignService: Initializing...")
	_init_battles()
	_load_progress()

## =============================================================================
## BATTLE DEFINITIONS
## =============================================================================

func _init_battles() -> void:
	# Battle 0: Tutorial - First card
	_battles["battle_00"] = {
		"id": "battle_00",
		"name": "First Summons",
		"description": "Learn the basics of summoning. Win to earn your first card!",
		"difficulty": 1,
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "warrior", "rarity": "common", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "training_dummy", "count": 1}
		],
		"enemy_hp": 30.0,  # Very low HP for tutorial (2 hits)
		"unlock_requirements": [],
		# AI Configuration
		"ai_type": "scripted",
		"ai_script": [
			{"delay": 0.0, "card_name": "Training Dummy", "position": {"x": 1400, "y": 540}}
		]
	}

	# Battle 1: Building army
	_battles["battle_01"] = {
		"id": "battle_01",
		"name": "Building Your Army",
		"description": "Expand your forces. Choose your reward.",
		"difficulty": 1,
		"reward_type": "choice",
		"reward_cards": [
			{"catalog_id": "warrior", "rarity": "common", "count": 1},
			{"catalog_id": "archer", "rarity": "common", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "warrior", "count": 2}
		],
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
		"name": "Fortify Your Position",
		"description": "Defense is key. Earn defensive cards.",
		"difficulty": 2,
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "wall", "rarity": "common", "count": 2}
		],
		"enemy_deck": [
			{"catalog_id": "warrior", "count": 2},
			{"catalog_id": "archer", "count": 1}
		],
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
		"name": "Growing Power",
		"description": "Test your strength. Random reward awaits.",
		"difficulty": 2,
		"reward_type": "random",
		"reward_cards": [
			{"catalog_id": "warrior", "rarity": "common", "count": 2},
			{"catalog_id": "archer", "rarity": "common", "count": 2},
			{"catalog_id": "wall", "rarity": "common", "count": 2}
		],
		"enemy_deck": [
			{"catalog_id": "warrior", "count": 3},
			{"catalog_id": "archer", "count": 2},
			{"catalog_id": "wall", "count": 1}
		],
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

	# Battle 4: Rare reward
	_battles["battle_04"] = {
		"id": "battle_04",
		"name": "Arcane Knowledge",
		"description": "Master advanced tactics. Rare card awaits!",
		"difficulty": 3,
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "fireball", "rarity": "rare", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "warrior", "count": 4},
			{"catalog_id": "archer", "count": 3},
			{"catalog_id": "wall", "count": 2}
		],
		"unlock_requirements": ["battle_03"],
		# AI Configuration
		"ai_type": "heuristic",
		"ai_personality": "spell_focused",
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

	var profile = _profile_repo.get_active_profile()
	if profile.is_empty():
		push_warning("CampaignService: No active profile")
		return

	_completed_battles = profile.get("campaign_progress", {}).get("completed_battles", [])
	print("CampaignService: Loaded progress - %d battles completed" % _completed_battles.size())

func save_progress() -> void:
	if not _profile_repo:
		return

	var profile = _profile_repo.get_active_profile()
	if profile.is_empty():
		return

	if not profile.has("campaign_progress"):
		profile["campaign_progress"] = {}

	profile["campaign_progress"]["completed_battles"] = _completed_battles.duplicate()

	_profile_repo.save_profile(true)  # Force immediate save
	campaign_progress_changed.emit()
	print("CampaignService: Saved progress - %d battles completed" % _completed_battles.size())

## =============================================================================
## BATTLE QUERIES
## =============================================================================

func get_all_battles() -> Array:
	var battles = []
	for battle_id in _battles.keys():
		battles.append(_battles[battle_id])
	return battles

func get_battle(battle_id: String) -> Dictionary:
	return _battles.get(battle_id, {})

func is_battle_completed(battle_id: String) -> bool:
	return battle_id in _completed_battles

func is_battle_unlocked(battle_id: String) -> bool:
	var battle = get_battle(battle_id)
	if battle.is_empty():
		return false

	# Check if all required battles are completed
	var requirements = battle.get("unlock_requirements", [])
	for req_id in requirements:
		if not is_battle_completed(req_id):
			return false

	return true

func get_available_battles() -> Array:
	var available = []
	for battle in get_all_battles():
		var battle_id = battle.get("id", "")
		if is_battle_unlocked(battle_id) and not is_battle_completed(battle_id):
			available.append(battle)
	return available

func get_completed_battles() -> Array:
	var completed = []
	for battle_id in _completed_battles:
		var battle = get_battle(battle_id)
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
	for battle in get_all_battles():
		var battle_id = battle.get("id", "")
		if is_battle_unlocked(battle_id) and not is_battle_completed(battle_id):
			# Check if it was just unlocked (not in previous available list)
			battle_unlocked.emit(battle_id)

func grant_battle_reward(battle_id: String, chosen_index: int = 0) -> Dictionary:
	var battle = get_battle(battle_id)
	if battle.is_empty():
		push_error("CampaignService: Battle not found: %s" % battle_id)
		return {}

	var reward_type = battle.get("reward_type", "fixed")
	var reward_cards = battle.get("reward_cards", [])

	if reward_cards.is_empty():
		push_warning("CampaignService: No rewards defined for battle '%s'" % battle_id)
		return {}

	var granted_card: Dictionary = {}

	match reward_type:
		"fixed":
			# Grant all reward cards
			for reward in reward_cards:
				_grant_reward_card(reward)
			granted_card = reward_cards[0]  # Return first for display

		"choice":
			# Player chooses one from the list
			if chosen_index >= 0 and chosen_index < reward_cards.size():
				var chosen_reward = reward_cards[chosen_index]
				_grant_reward_card(chosen_reward)
				granted_card = chosen_reward
			else:
				push_error("CampaignService: Invalid choice index %d" % chosen_index)

		"random":
			# Pick random card from pool
			var random_reward = reward_cards[randi() % reward_cards.size()]
			_grant_reward_card(random_reward)
			granted_card = random_reward

	return granted_card

func _grant_reward_card(reward: Dictionary) -> void:
	if not _collection:
		push_error("CampaignService: Collection service not found!")
		return

	var catalog_id = reward.get("catalog_id", "")
	var rarity = reward.get("rarity", "common")
	var count = reward.get("count", 1)

	for i in range(count):
		_collection.grant_card(catalog_id, rarity)

	print("CampaignService: Granted %dx %s (%s)" % [count, catalog_id, rarity])
