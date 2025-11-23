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

## Campaign battles
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
	# When updating battle rewards, also update the corresponding UI displays in:
	#   - campaign_map.gd (visual node-based map)
	#   - campaign_screen.gd (list-based screen)
	# to prevent divergence between advertised and actual rewards

	# IMPORTANT: Battles using event_sequence system
	# If a battle uses "event_sequence" for spawning enemies via dialogue/events:
	# - Set "enemy_deck": [] (empty array, NOT omit the key)
	# - Summoner3D will auto-detect this and use DEFERRED deck loading strategy
	# - Enemies are spawned manually via BattleDialogueController or EventSequencer

	# Onboarding Event 1: Hero/Affinity selection
	_battles["event_affinity"] = {
		"id": "event_affinity",
		"biome_id": "",  # No biome, not a battle
		"name": Loc.t("campaign.event.affinity.name"),
		"description": Loc.t("campaign.event.affinity.description"),
		"difficulty": 0,
		"event_type": "affinity",
		"requires_deck": false,  # No deck selection needed
		"repeatable": false,  # One-time event
		"reward_type": "fixed",
		"reward_cards": [],  # Reward handled by hero_selection flow
		"enemy_deck": [],  # Not a battle
		"unlock_requirements": [],  # First event, always available
	}

	# Onboarding Event 2: First summon selection
	_battles["event_first_summon"] = {
		"id": "event_first_summon",
		"biome_id": "",  # No biome, not a battle
		"name": Loc.t("campaign.event.first_summon.name"),
		"description": Loc.t("campaign.event.first_summon.description"),
		"difficulty": 0,
		"event_type": "first_summon",
		"requires_deck": false,  # No deck selection needed
		"repeatable": false,  # One-time event
		"reward_type": "fixed",
		"reward_cards": [],  # Reward handled by first_card_selection flow
		"enemy_deck": [],  # Not a battle
		"unlock_requirements": ["event_affinity"],  # Requires completing affinity selection
	}

	# Battle 0: The First Trial
	_battles["first_trial"] = {
		"id": "first_trial",
		"biome_id": "summer_plains",
		"name": Loc.t("campaign.battle.first_trial.name"),
		"description": Loc.t("campaign.battle.first_trial.description"),
		"difficulty": 1,
		"event_type": "battle",
		"repeatable": false,  # One-time tutorial battle
		"requires_deck": true,  # Requires deck selection
		"is_tutorial": true,  # Tutorial battle - deck editing locked
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "charge", "rarity": "common", "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "slime_green", "count": 1}
		],
		"enemy_hp": 30.0,  # Very low HP for tutorial (3 hits × 10 damage)
		"unlock_requirements": ["event_first_summon"],
		# Tutorial Event Sequence (Phase 3: Event System)
		"event_sequence": "res://resources/sequences/first_trial_tutorial.tres",
		# AI Configuration (disabled for tutorial - manual spawn via dialogue system)
		"ai_type": "scripted",
		"ai_script": []
	}

	# Tutorial: Charge Card Introduction
	_battles["charge_tutorial"] = {
		"id": "charge_tutorial",
		"biome_id": "summer_plains",
		"name": Loc.t("campaign.battle.charge_tutorial.name"),
		"description": Loc.t("campaign.battle.charge_tutorial.description"),
		"difficulty": 1,
		"event_type": "battle",
		"repeatable": false,  # One-time tutorial battle
		"requires_deck": true,
		"is_tutorial": true,  # Last tutorial battle - deck editing unlocks after this
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "fire_recruit", "rarity": "common", "count": 1},
			{"catalog_id": "ember_slinger", "rarity": "common", "count": 1}
		],
		"enemy_deck": [],  # Spawned via event sequence
		"enemy_hp": 50.0,
		"unlock_requirements": ["first_trial"],
		# Tutorial Event Sequence
		"event_sequence": "res://resources/sequences/charge_tutorial.tres",
		# AI Configuration (disabled for tutorial)
		"ai_type": "scripted",
		"ai_script": []
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
	var battle: Dictionary = _battles.get(battle_id, empty_battle)

	# Get enemy deck size safely
	var enemy_deck_variant: Variant = battle.get("enemy_deck", [])
	var enemy_deck_size: int = 0
	if enemy_deck_variant is Array:
		var enemy_deck_array: Array = enemy_deck_variant
		enemy_deck_size = enemy_deck_array.size()

	print("CampaignService: get_battle('%s') - found: %s, has_enemy_deck: %s, enemy_deck_size: %d" % [
		battle_id,
		not battle.is_empty(),
		battle.has("enemy_deck"),
		enemy_deck_size
	])
	return battle

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
