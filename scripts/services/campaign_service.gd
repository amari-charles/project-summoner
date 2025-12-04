extends Node
class_name CampaignService

## CampaignService - Manages campaign progression and battle rewards
##
## Tracks which battles have been completed and handles reward distribution.
## Battle definitions and progression are managed here.

## Preload CampaignIDs to ensure class is available
const _CampaignIDs = preload("res://scripts/data/campaign_ids.gd")

## Signals
signal battle_completed(battle_id: String)
signal battle_unlocked(battle_id: String)
signal campaign_progress_changed()
signal campaign_changed(old_campaign_id: String, new_campaign_id: String)

## =============================================================================
## DEPENDENCIES
## =============================================================================

## Injectable dependencies - defaults to global autoloads
## For testing: set these before calling _ready() or use init_for_testing()
var profile_repo: IProfileRepo = null
var economy_service: Node = null  # EconomyService
var collection_service: Node = null  # CollectionService

## Campaign battles
var _battles: Dictionary = {}

## Campaign metadata
var _campaigns: Dictionary = {}  # campaign_id -> campaign metadata
var _current_campaign_id: String = ""

## Current profile's campaign progress
var _completed_battles: Array[String] = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignService: Initializing...")

	# Use injected dependencies or fall back to autoloads
	if profile_repo == null:
		profile_repo = ProfileRepo
	if economy_service == null:
		economy_service = Economy
	if collection_service == null:
		collection_service = Collection

	_load_campaigns()

	# Load selected campaign from profile meta (default to first campaign)
	var meta: Dictionary = profile_repo.get_profile_meta()
	_current_campaign_id = meta.get("selected_campaign", String(_CampaignIDs.DEFAULT))

	# Ensure current campaign is valid
	if not _campaigns.has(_current_campaign_id):
		_current_campaign_id = String(_CampaignIDs.DEFAULT)

	_load_progress()

	# Reload progress when profile changes (e.g., on reset)
	profile_repo.data_changed.connect(_on_profile_data_changed)

	# Reload progress when active summoner changes
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_changed)


## Initialize for unit testing with mock dependencies
## Call this instead of relying on _ready() in tests
func init_for_testing(repo: IProfileRepo, economy: Node = null, collection: Node = null) -> void:
	profile_repo = repo
	economy_service = economy
	collection_service = collection

	# Disconnect previous connections if any
	if profile_repo.data_changed.is_connected(_on_profile_data_changed):
		profile_repo.data_changed.disconnect(_on_profile_data_changed)
	profile_repo.data_changed.connect(_on_profile_data_changed)

	_load_campaigns(true)  # Skip validation in tests (no scene tree access)
	_current_campaign_id = String(_CampaignIDs.DEFAULT)
	_load_progress()

func _on_profile_data_changed() -> void:
	print("CampaignService: Profile data changed - reloading progress...")
	_load_progress()
	campaign_progress_changed.emit()

func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	print("CampaignService: Summoner changed to '%s' - reloading progress..." % new_summoner_id)
	_load_progress()
	campaign_progress_changed.emit()

## =============================================================================
## CAMPAIGN LOADING
## =============================================================================

## Load all campaigns from JSON files in data/campaigns/
func _load_campaigns(skip_validation: bool = false) -> void:
	_campaigns.clear()
	_battles.clear()

	var campaign_dir: String = "res://data/campaigns/"
	var dir: DirAccess = DirAccess.open(campaign_dir)

	if not dir:
		push_warning("CampaignService: campaigns directory not found: " + campaign_dir)
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".json"):
			var file_path: String = campaign_dir + file_name
			var campaign_data: Dictionary = _load_campaign_from_file(file_path)
			if not campaign_data.is_empty():
				var campaign_id: String = campaign_data.get("campaign_id", "")
				if not campaign_id.is_empty():
					_campaigns[campaign_id] = campaign_data
					_load_battles_from_campaign(campaign_data)
		file_name = dir.get_next()

	dir.list_dir_end()
	print("CampaignService: Loaded %d campaigns with %d total battles" % [_campaigns.size(), _battles.size()])

	# Validate all battle rewards exist in card catalog (skip in tests)
	if not skip_validation:
		_validate_battle_rewards()

## Load a single campaign from a JSON file
func _load_campaign_from_file(file_path: String) -> Dictionary:
	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("CampaignService: Failed to open file: " + file_path)
		return {}

	var json_text: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var parse_result: Error = json.parse(json_text)

	if parse_result != OK:
		push_error("CampaignService: JSON parse error in %s: %s" % [file_path, json.get_error_message()])
		return {}

	var data: Variant = json.get_data()
	if not data is Dictionary:
		push_error("CampaignService: JSON root is not a dictionary: " + file_path)
		return {}

	return data

## Load battles from a campaign data dictionary into _battles
func _load_battles_from_campaign(campaign_data: Dictionary) -> void:
	var battles_array: Array = campaign_data.get("battles", [])

	for battle_variant: Variant in battles_array:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = battle.get("id", "")
		if battle_id.is_empty():
			push_warning("CampaignService: Battle missing 'id' field, skipping")
			continue

		# Convert localization keys to localized strings
		var name_key: String = battle.get("name_key", "")
		var desc_key: String = battle.get("description_key", "")
		battle["name"] = Loc.t(name_key) if not name_key.is_empty() else ""
		battle["description"] = Loc.t(desc_key) if not desc_key.is_empty() else ""

		# Convert event_type string to StringName
		var event_type_str: String = battle.get("event_type", "battle")
		battle["event_type"] = _string_to_event_type(event_type_str)

		# Convert reward_type string to StringName
		var reward_type_str: String = battle.get("reward_type", "fixed")
		battle["reward_type"] = _string_to_reward_type(reward_type_str)

		# Store battle
		_battles[battle_id] = battle

## Convert event type string to EventTypeIDs constant
func _string_to_event_type(type_str: String) -> StringName:
	match type_str.to_lower():
		"battle": return EventTypeIDs.BATTLE
		"affinity": return EventTypeIDs.AFFINITY
		"first_summon": return EventTypeIDs.FIRST_SUMMON
		"caravan": return EventTypeIDs.CARAVAN
		"onboarding": return EventTypeIDs.ONBOARDING
		_: return EventTypeIDs.BATTLE

## Convert reward type string to RewardTypeIDs constant
func _string_to_reward_type(type_str: String) -> StringName:
	match type_str.to_lower():
		"fixed": return RewardTypeIDs.FIXED
		"choice": return RewardTypeIDs.CHOICE
		"random": return RewardTypeIDs.RANDOM
		"none": return RewardTypeIDs.NONE
		_: return RewardTypeIDs.FIXED

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
## CAMPAIGN QUERIES
## =============================================================================

## Get all campaigns with unlock status
func get_all_campaigns() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for campaign_id: String in _campaigns.keys():
		var campaign: Dictionary = _campaigns[campaign_id].duplicate()
		campaign["is_unlocked"] = is_campaign_unlocked(campaign_id)
		result.append(campaign)
	# Sort by sort_order
	result.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return a.get("sort_order", 999) < b.get("sort_order", 999)
	)
	return result

## Get a specific campaign's metadata
func get_campaign(campaign_id: String) -> Dictionary:
	return _campaigns.get(campaign_id, {})

## Get the currently selected campaign ID
func get_current_campaign_id() -> String:
	return _current_campaign_id

## Set the current campaign and save to profile
func set_current_campaign(campaign_id: String) -> bool:
	if not _campaigns.has(campaign_id):
		push_warning("CampaignService: Invalid campaign ID: %s" % campaign_id)
		return false

	if not is_campaign_unlocked(campaign_id):
		push_warning("CampaignService: Campaign '%s' is locked" % campaign_id)
		return false

	var old_id: String = _current_campaign_id
	_current_campaign_id = campaign_id

	# Save to profile meta
	profile_repo.update_profile_meta({"selected_campaign": campaign_id})

	campaign_changed.emit(old_id, campaign_id)
	print("CampaignService: Switched to campaign '%s'" % campaign_id)
	return true

## Check if a campaign is unlocked
func is_campaign_unlocked(campaign_id: String) -> bool:
	var campaign: Dictionary = _campaigns.get(campaign_id, {})
	var requirements: Array = campaign.get("unlock_requirements", [])

	if requirements.is_empty():
		return true

	# Future: implement requirement checking (e.g., completed other campaigns)
	return true

## =============================================================================
## PROGRESS MANAGEMENT
## =============================================================================

func _load_progress() -> void:
	var campaign_progress: Dictionary = profile_repo.get_campaign_progress()
	var completed_battles_raw: Array = campaign_progress.get("completed_battles", [])
	_completed_battles.clear()
	for battle_id: Variant in completed_battles_raw:
		if battle_id is String:
			_completed_battles.append(battle_id)
	print("CampaignService: Loaded progress - %d battles completed" % _completed_battles.size())

func save_progress() -> void:
	profile_repo.update_campaign_progress({
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
	profile_repo.update_campaign_progress({"pending_reward": pending})
	print("CampaignService: Set pending reward for battle '%s' (type: %s)" % [battle_id, reward_type])

## Get the current pending reward (null if none)
func get_pending_reward() -> Variant:
	var campaign_progress: Dictionary = profile_repo.get_campaign_progress()
	return campaign_progress.get("pending_reward", null)

## Update choice index for a pending choice reward
func update_pending_choice(choice_index: int) -> void:
	var pending: Variant = get_pending_reward()
	if pending == null or not pending is Dictionary:
		push_warning("CampaignService: No pending reward to update choice for")
		return

	var pending_dict: Dictionary = pending
	pending_dict["choice_index"] = choice_index
	profile_repo.update_campaign_progress({"pending_reward": pending_dict})
	print("CampaignService: Updated pending choice to index %d" % choice_index)

## Clear the pending reward (called after reward is claimed)
func clear_pending_reward() -> void:
	profile_repo.update_campaign_progress({"pending_reward": null})
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
	if reward_type == RewardTypeIDs.CHOICE and choice_index < 0:
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

	var reward_type: StringName = StringName(battle.get("reward_type", RewardTypeIDs.FIXED))
	var reward_cards: Array = battle.get("reward_cards", [])

	# Grant gold reward
	var gold_reward: int = battle.get("gold_reward", 0)
	if gold_reward > 0 and economy_service != null:
		economy_service.add_gold(gold_reward)
		print("CampaignService: Granted %d gold for battle '%s'" % [gold_reward, battle_id])

	# Handle case where there are no card rewards but there is gold
	if reward_cards.is_empty():
		if gold_reward > 0:
			return {"gold_reward": gold_reward, "instance_ids": []}
		push_warning("CampaignService: No rewards defined for battle '%s'" % battle_id)
		var empty_rewards: Dictionary = {}
		return empty_rewards

	var granted_card: Dictionary = {}
	var granted_instance_ids: Array[String] = []  # Track actual card instance IDs

	match reward_type:
		RewardTypeIDs.FIXED:
			# Grant all reward cards
			for reward: Variant in reward_cards:
				if reward is Dictionary:
					var reward_dict: Dictionary = reward
					var ids: Array[String] = _grant_reward_card(reward_dict)
					granted_instance_ids.append_array(ids)
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				granted_card = reward_cards[0]  # Return first for display

		RewardTypeIDs.CHOICE:
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

		RewardTypeIDs.RANDOM:
			# Pick random card from pool
			var random_reward_variant: Variant = reward_cards[randi() % reward_cards.size()]
			if not random_reward_variant is Dictionary:
				push_error("CampaignService: random reward_cards entry is not a Dictionary")
				return {}
			var random_reward: Dictionary = random_reward_variant
			var ids: Array[String] = _grant_reward_card(random_reward)
			granted_instance_ids.append_array(ids)
			granted_card = random_reward

	# Add instance IDs and gold to return value
	granted_card["instance_ids"] = granted_instance_ids
	granted_card["gold_reward"] = gold_reward
	return granted_card

func _grant_reward_card(reward: Dictionary) -> Array[String]:
	var instance_ids: Array[String] = []

	var catalog_id: String = reward.get("catalog_id", "")
	var rarity: String = reward.get("rarity", RarityIDs.COMMON)
	var count: int = reward.get("count", 1)

	if collection_service == null:
		push_warning("CampaignService: No collection service - skipping card grant")
		return instance_ids

	for i: int in range(count):
		var instance_id: String = collection_service.grant_card(catalog_id, rarity)
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
