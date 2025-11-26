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

## Campaign battles
var _battles: Dictionary = {}

## Current profile's campaign progress
var _completed_battles: Array[String] = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignService: Initializing...")

	_init_battles()
	_load_progress()

	# Reload progress when profile changes (e.g., on reset)
	ProfileRepo.data_changed.connect(_on_profile_data_changed)

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
	_battles[BattleIDs.EVENT_AFFINITY] = {
		"id": BattleIDs.EVENT_AFFINITY,
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
	_battles[BattleIDs.EVENT_FIRST_SUMMON] = {
		"id": BattleIDs.EVENT_FIRST_SUMMON,
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
		"unlock_requirements": [BattleIDs.EVENT_AFFINITY],  # Requires completing affinity selection
	}

	# Battle 0: The First Trial
	_battles[BattleIDs.FIRST_TRIAL] = {
		"id": BattleIDs.FIRST_TRIAL,
		"biome_id": BiomeIDs.SUMMER_PLAINS,
		"name": Loc.t("campaign.battle.first_trial.name"),
		"description": Loc.t("campaign.battle.first_trial.description"),
		"difficulty": 1,
		"event_type": "battle",
		"repeatable": false,  # One-time tutorial battle
		"requires_deck": true,  # Requires deck selection
		"is_tutorial": true,  # Tutorial battle - deck editing locked
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "charge", "rarity": RarityIDs.COMMON, "count": 1}
		],
		"enemy_deck": [
			{"catalog_id": "slime_green", "count": 1}
		],
		"enemy_hp": 30.0,  # Very low HP for tutorial (3 hits × 10 damage)
		"unlock_requirements": [BattleIDs.EVENT_FIRST_SUMMON],
		# Tutorial Event Sequence (Phase 3: Event System)
		"event_sequence": "res://resources/sequences/first_trial_tutorial.tres",
		# AI Configuration (disabled for tutorial - manual spawn via dialogue system)
		"ai_type": "scripted",
		"ai_script": []
	}

	# Tutorial: Charge Card Introduction
	_battles[BattleIDs.CHARGE_TUTORIAL] = {
		"id": BattleIDs.CHARGE_TUTORIAL,
		"biome_id": BiomeIDs.SUMMER_PLAINS,
		"name": Loc.t("campaign.battle.charge_tutorial.name"),
		"description": Loc.t("campaign.battle.charge_tutorial.description"),
		"difficulty": 1,
		"event_type": "battle",
		"repeatable": false,  # One-time tutorial battle
		"requires_deck": true,
		"is_tutorial": true,  # Last tutorial battle - deck editing unlocks after this
		"reward_type": "fixed",
		"reward_cards": [
			{"catalog_id": "fire_recruit", "rarity": RarityIDs.COMMON, "count": 1},
			{"catalog_id": "ember_slinger", "rarity": RarityIDs.COMMON, "count": 1}
		],
		"enemy_deck": [],  # Spawned via event sequence
		"enemy_hp": 50.0,
		"unlock_requirements": [BattleIDs.FIRST_TRIAL],
		# Tutorial Event Sequence
		"event_sequence": "res://resources/sequences/charge_tutorial.tres",
		# AI Configuration (disabled for tutorial)
		"ai_type": "scripted",
		"ai_script": []
	}

	# Caravan Event: Mr. Merriweather's Trading Post
	_battles[BattleIDs.EVENT_CARAVAN_TUTORIAL] = {
		"id": BattleIDs.EVENT_CARAVAN_TUTORIAL,
		"event_type": "caravan",
		"name": Loc.t("campaign.event.caravan_tutorial.name"),
		"description": Loc.t("campaign.event.caravan_tutorial.description"),
		"difficulty": 1,
		"gold_reward": 0,  # Handled by shop purchases
		"unlock_requirements": [BattleIDs.CHARGE_TUTORIAL],
		"requires_deck": false,
		"repeatable": false,
		"reward_type": "none",  # Rewards from shop
		"reward_cards": [],
		# Caravan shop ID
		"shop_id": "caravan_tutorial",
		# Event Sequence (dialogue + shop opening)
		"event_sequence": "res://resources/sequences/caravan_tutorial.tres"
	}

	print("CampaignService: Loaded %d battles" % _battles.size())

	# Validate all battle rewards exist in card catalog
	_validate_battle_rewards()

## Validate that all reward cards in battle configs exist in the card catalog
func _validate_battle_rewards() -> void:
	var catalog: Node = get_node_or_null("/root/CardCatalog")
	if not catalog:
		push_warning("CampaignService: CardCatalog not found - skipping reward validation")
		return

	var invalid_count: int = 0
	for battle_id: String in _battles.keys():
		var battle: Dictionary = _battles[battle_id]
		var reward_cards: Array = battle.get("reward_cards", [])

		for reward_variant: Variant in reward_cards:
			if not reward_variant is Dictionary:
				continue
			var reward: Dictionary = reward_variant
			var catalog_id: String = reward.get("catalog_id", "")

			if catalog_id.is_empty():
				continue

			if not catalog.call("has_card", catalog_id):
				push_error("CampaignService: INVALID REWARD - Battle '%s' has reward card '%s' which doesn't exist in CardCatalog!" % [battle_id, catalog_id])
				invalid_count += 1

	if invalid_count > 0:
		push_error("CampaignService: Found %d invalid reward card references! Fix these before shipping." % invalid_count)
	else:
		print("CampaignService: All %d battles validated - rewards are properly configured" % _battles.size())

## =============================================================================
## PROGRESS MANAGEMENT
## =============================================================================

func _load_progress() -> void:
	var campaign_progress: Dictionary = ProfileRepo.get_campaign_progress()
	var completed_battles_raw: Array = campaign_progress.get("completed_battles", [])
	_completed_battles.clear()
	for battle_id: Variant in completed_battles_raw:
		if battle_id is String:
			_completed_battles.append(battle_id)
	print("CampaignService: Loaded progress - %d battles completed" % _completed_battles.size())

func save_progress() -> void:
	ProfileRepo.update_campaign_progress({
		"completed_battles": _completed_battles.duplicate()
	})
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
## PENDING REWARD MANAGEMENT
## =============================================================================

## Set a pending reward for a battle (called when player wins but hasn't claimed yet)
func set_pending_reward(battle_id: String, reward_type: String, choice_index: int = -1) -> void:
	var pending: Dictionary = {
		"battle_id": battle_id,
		"reward_type": reward_type,
		"choice_index": choice_index  # -1 = not chosen yet (for choice rewards)
	}
	ProfileRepo.update_campaign_progress({"pending_reward": pending})
	print("CampaignService: Set pending reward for battle '%s' (type: %s)" % [battle_id, reward_type])

## Get the current pending reward (null if none)
func get_pending_reward() -> Variant:
	var campaign_progress: Dictionary = ProfileRepo.get_campaign_progress()
	return campaign_progress.get("pending_reward", null)

## Update choice index for a pending choice reward
func update_pending_choice(choice_index: int) -> void:
	var pending: Variant = get_pending_reward()
	if pending == null or not pending is Dictionary:
		push_warning("CampaignService: No pending reward to update choice for")
		return

	var pending_dict: Dictionary = pending
	pending_dict["choice_index"] = choice_index
	ProfileRepo.update_campaign_progress({"pending_reward": pending_dict})
	print("CampaignService: Updated pending choice to index %d" % choice_index)

## Clear the pending reward (called after reward is claimed)
func clear_pending_reward() -> void:
	ProfileRepo.update_campaign_progress({"pending_reward": null})
	print("CampaignService: Cleared pending reward")

## Claim the pending reward (grants cards and marks battle complete)
## Returns the granted card info or empty dict if failed
func claim_pending_reward() -> Dictionary:
	var pending: Variant = get_pending_reward()
	if pending == null or not pending is Dictionary:
		push_warning("CampaignService: No pending reward to claim")
		return {}

	var pending_dict: Dictionary = pending
	var battle_id: String = pending_dict.get("battle_id", "")
	var reward_type: String = pending_dict.get("reward_type", "")
	var choice_index: int = pending_dict.get("choice_index", 0)

	if battle_id.is_empty():
		push_error("CampaignService: Invalid pending reward - no battle_id")
		return {}

	# For choice rewards, ensure a choice was made
	if reward_type == "choice" and choice_index < 0:
		push_error("CampaignService: Cannot claim choice reward without making a choice")
		return {}

	# Grant the reward
	var granted_card: Dictionary = grant_battle_reward(battle_id, choice_index)

	# Mark battle as completed
	complete_battle(battle_id)

	# Clear the pending reward
	clear_pending_reward()

	print("CampaignService: Claimed reward for battle '%s'" % battle_id)
	return granted_card

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

	var catalog_id: String = reward.get("catalog_id", "")
	var rarity: String = reward.get("rarity", RarityIDs.COMMON)
	var count: int = reward.get("count", 1)

	for i: int in range(count):
		var instance_id: String = Collection.grant_card(catalog_id, rarity)
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
