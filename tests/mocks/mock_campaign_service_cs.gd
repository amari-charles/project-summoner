extends Node
class_name MockCampaignServiceCS

## Mock C# Campaign Service for Unit Testing
##
## GDScript implementation that mimics CampaignServiceCS behavior.
## Allows CampaignService unit tests to run without the C# autoload.
##
## This mock auto-initializes with battle data matching C# EventCatalog.

## Signals (match C# service)
signal BattleCompleted(battle_id: String)
signal BattleUnlocked(battle_id: String)
signal CampaignProgressChanged()
signal CampaignChanged(old_campaign_id: String, new_campaign_id: String)

## Internal state
var _campaigns: Dictionary = {}  # campaign_id -> campaign data
var _battles: Dictionary = {}    # battle_id -> battle data with campaign_id
var _edges: Array = []           # edges for current campaign (graph-based unlock)
var _completed_battles: Array = []
var _current_campaign_id: String = ""
var _pending_reward: Dictionary = {}

## Callback references (injected by GDScript wrapper)
var _grant_card: Callable
var _get_active_summoner: Callable

## Profile repo reference (for loading/saving progress)
var _profile_repo: IProfileRepo

## Call tracking for assertions
var _calls: Dictionary = {}


func _init() -> void:
	_initialize_catalog_data()


## =============================================================================
## CATALOG DATA INITIALIZATION
## =============================================================================

## Initialize mock with battle data matching C# EventCatalog
## This allows tests to work without the real C# service
func _initialize_catalog_data() -> void:
	# Summoner's Path campaign (matches C# CampaignCatalog)
	var summoners_path: Dictionary = {
		"campaign_id": String(CampaignIDs.SUMMONERS_PATH),
		"name_key": "campaign.summoners_path.name",
		"battles": [
			_create_battle(String(BattleIDs.FIRST_TRIAL), true, 30, "fire_wisp"),
			_create_battle(String(BattleIDs.SECOND_CHALLENGE), true, 40, "earth_sprite"),
			_create_caravan(String(BattleIDs.CARAVAN_01)),
			_create_battle(String(BattleIDs.THIRD_TRIAL), false, 50, "fire_wisp"),
			_create_choice(String(BattleIDs.PATH_FORK)),
			_create_battle(String(BattleIDs.ELITE_BATTLE_01), false, 80, "fire_wisp"),
			_create_battle(String(BattleIDs.STANDARD_BATTLE_01), false, 50, "puff"),
			_create_boss(String(BattleIDs.ACT1_BOSS), 100, "mana_bolt"),
		],
		"edges": [
			{"from": String(BattleIDs.FIRST_TRIAL), "to": String(BattleIDs.SECOND_CHALLENGE)},
			{"from": String(BattleIDs.SECOND_CHALLENGE), "to": String(BattleIDs.CARAVAN_01)},
			{"from": String(BattleIDs.CARAVAN_01), "to": String(BattleIDs.THIRD_TRIAL)},
			{"from": String(BattleIDs.THIRD_TRIAL), "to": String(BattleIDs.PATH_FORK)},
			{"from": String(BattleIDs.PATH_FORK), "to": String(BattleIDs.ELITE_BATTLE_01), "condition": {"choice": "elite"}},
			{"from": String(BattleIDs.PATH_FORK), "to": String(BattleIDs.STANDARD_BATTLE_01), "condition": {"choice": "standard"}},
			{"from": String(BattleIDs.ELITE_BATTLE_01), "to": String(BattleIDs.ACT1_BOSS)},
			{"from": String(BattleIDs.STANDARD_BATTLE_01), "to": String(BattleIDs.ACT1_BOSS)},
		]
	}

	# Test Arena campaign (matches C# CampaignCatalog)
	var test_arena: Dictionary = {
		"campaign_id": String(CampaignIDs.TEST_ARENA),
		"name_key": "campaign.test_arena.name",
		"battles": [
			_create_arena_battle(String(BattleIDs.ARENA_EARTH_SPRITE)),
			_create_arena_battle(String(BattleIDs.ARENA_PUFF)),
			_create_arena_battle(String(BattleIDs.ARENA_FIRE_WISP)),
			_create_arena_battle(String(BattleIDs.ARENA_CLOUD_SWARM)),
			_create_arena_battle(String(BattleIDs.ARENA_MANA_BOLT)),
			_create_arena_battle(String(BattleIDs.DEBUG_ARENA)),
		],
		"edges": []  # Test Arena battles have no edges (all unlocked)
	}

	_load_campaign_data([summoners_path, test_arena])
	SetCurrentCampaign(String(CampaignIDs.SUMMONERS_PATH))


## Helper to create battle definition matching EventCatalog
func _create_battle(id: String, is_tutorial: bool, gold: int, reward_card: String) -> Dictionary:
	return {
		"id": id,
		"type": String(NodeTypeIDs.BATTLE),
		"is_tutorial": is_tutorial,
		"gold_reward": gold,
		"reward_type": String(RewardTypeIDs.FLEXIBLE),
		"reward_options": [reward_card, "fire_wisp", "puff"],
		"player_selects": true,
	}


## Helper to create caravan event
func _create_caravan(id: String) -> Dictionary:
	return {
		"id": id,
		"type": String(NodeTypeIDs.CARAVAN),
		"gold_reward": 0,
	}


## Helper to create choice event
func _create_choice(id: String) -> Dictionary:
	return {
		"id": id,
		"type": String(NodeTypeIDs.CHOICE),
		"options": [
			{"id": "elite", "label_key": "campaign.path.elite.label"},
			{"id": "standard", "label_key": "campaign.path.standard.label"},
		]
	}


## Helper to create boss battle with fixed reward
func _create_boss(id: String, gold: int, reward_card: String) -> Dictionary:
	return {
		"id": id,
		"type": String(NodeTypeIDs.BOSS),
		"gold_reward": gold,
		"reward_type": String(RewardTypeIDs.FIXED),
		"reward_cards": [{"catalog_id": reward_card, "rarity": "rare", "count": 1}],
	}


## Helper to create test arena battle (no rewards, repeatable)
func _create_arena_battle(id: String) -> Dictionary:
	return {
		"id": id,
		"type": String(NodeTypeIDs.BATTLE),
		"is_tutorial": false,
		"gold_reward": 0,
		"reward_type": String(RewardTypeIDs.NONE),
		"repeatable": true,
	}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
	_campaigns.clear()
	_battles.clear()
	_edges.clear()
	_completed_battles.clear()
	_current_campaign_id = ""
	_pending_reward = {}
	_calls = {}
	_initialize_catalog_data()  # Re-initialize with catalog data


func get_call_count(method_name: String) -> int:
	return _calls.get(method_name, 0)


func get_call_args(method_name: String) -> Array:
	return _calls.get(method_name + "_args", [])


func _record_call(method_name: String, args: Array = []) -> void:
	_calls[method_name] = _calls.get(method_name, 0) + 1
	if not _calls.has(method_name + "_args"):
		_calls[method_name + "_args"] = []
	_calls[method_name + "_args"].append(args)


func set_profile_repo(repo: IProfileRepo) -> void:
	_profile_repo = repo


## =============================================================================
## CALLBACK INJECTION (called by GDScript wrapper)
## =============================================================================

func SetCollectionCallbacks(grant_card: Callable) -> void:
	_grant_card = grant_card


func SetActiveSummonerGetter(getter: Callable) -> void:
	_get_active_summoner = getter


## =============================================================================
## CAMPAIGN LOADING
## =============================================================================

## Initialize catalogs (matches CampaignServiceCS.InitializeCatalogs())
func InitializeCatalogs() -> void:
	_record_call("InitializeCatalogs", [])
	# Re-initialize from catalog data (already done in _init)
	_initialize_catalog_data()


## Internal helper to load campaign data into mock state
func _load_campaign_data(campaigns_array: Array) -> void:
	_campaigns.clear()
	_battles.clear()

	for campaign_data: Variant in campaigns_array:
		if not campaign_data is Dictionary:
			continue
		var campaign: Dictionary = campaign_data
		var campaign_id: String = campaign.get("campaign_id", "")
		if campaign_id.is_empty():
			continue

		_campaigns[campaign_id] = campaign

		# Index battles
		var battles_array: Array = campaign.get("battles", [])
		for battle_variant: Variant in battles_array:
			if not battle_variant is Dictionary:
				continue
			var battle: Dictionary = battle_variant
			var battle_id: String = battle.get("id", "")
			if battle_id.is_empty():
				continue

			var battle_copy: Dictionary = battle.duplicate(true)
			battle_copy["campaign_id"] = campaign_id
			_battles[battle_id] = battle_copy


func SetCurrentCampaign(campaign_id: String) -> void:
	_record_call("SetCurrentCampaign", [campaign_id])
	var old_id: String = _current_campaign_id
	_current_campaign_id = campaign_id

	# Load edges for current campaign (graph-based unlock logic)
	_edges.clear()
	var campaign: Dictionary = _campaigns.get(campaign_id, {})
	var edges_array: Variant = campaign.get("edges", [])
	if edges_array is Array:
		_edges = edges_array.duplicate()

	LoadProgress()
	if old_id != campaign_id:
		CampaignChanged.emit(old_id, campaign_id)


func LoadProgress() -> void:
	_record_call("LoadProgress", [])
	if _profile_repo == null:
		return

	var progress: Dictionary = _profile_repo.get_campaign_progress()

	_completed_battles = progress.get("completed_battles", []).duplicate()
	_pending_reward = progress.get("pending_reward", {}).duplicate()


func NotifyProgressChanged() -> void:
	CampaignProgressChanged.emit()


func SaveProgress() -> void:
	_record_call("SaveProgress", [])
	if _profile_repo == null:
		return

	var progress: Dictionary = {
		"completed_battles": _completed_battles.duplicate(),
		"pending_reward": _pending_reward.duplicate() if not _pending_reward.is_empty() else {}
	}

	_profile_repo.update_campaign_progress(progress)

	# Note: Signal is emitted via data_changed -> _on_profile_data_changed -> NotifyProgressChanged
	# Don't emit here to avoid double emission


## =============================================================================
## CAMPAIGN QUERIES
## =============================================================================

func GetAllCampaigns() -> Array:
	var result: Array = []
	for campaign_id: String in _campaigns.keys():
		var campaign: Dictionary = _campaigns[campaign_id].duplicate()
		campaign["is_unlocked"] = IsCampaignUnlocked(campaign_id)
		result.append(campaign)
	return result


func GetCampaign(campaign_id: String) -> Dictionary:
	return _campaigns.get(campaign_id, {}).duplicate()


func GetCurrentCampaignId() -> String:
	return _current_campaign_id


func IsCampaignUnlocked(campaign_id: String) -> bool:
	var campaign: Dictionary = _campaigns.get(campaign_id, {})
	var unlock_requirements: Array = campaign.get("unlock_requirements", [])

	if unlock_requirements.is_empty():
		return true

	# Check if all required campaigns are completed
	for req_campaign_id: Variant in unlock_requirements:
		if not _is_campaign_completed(String(req_campaign_id)):
			return false

	return true


func HasCampaign(campaign_id: String) -> bool:
	return _campaigns.has(campaign_id)


func _is_campaign_completed(campaign_id: String) -> bool:
	var campaign: Dictionary = _campaigns.get(campaign_id, {})
	var battles: Array = campaign.get("battles", [])

	for battle_variant: Variant in battles:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = battle.get("id", "")
		if not battle_id.is_empty() and battle_id not in _completed_battles:
			return false

	return battles.size() > 0


## =============================================================================
## BATTLE QUERIES
## =============================================================================

func GetAllBattles() -> Array:
	var result: Array = []
	var campaign: Dictionary = _campaigns.get(_current_campaign_id, {})
	var battles: Array = campaign.get("battles", [])

	for battle_variant: Variant in battles:
		if battle_variant is Dictionary:
			result.append(battle_variant.duplicate())

	return result


func GetBattle(battle_id: String) -> Dictionary:
	return _battles.get(battle_id, {}).duplicate()


func IsBattleCompleted(battle_id: String) -> bool:
	return battle_id in _completed_battles


func IsBattleUnlocked(battle_id: String) -> bool:
	# Graph-based unlock logic (matches NodeUnlockHandler in C#)
	# A node is unlocked if ANY incoming edge is satisfied (OR logic)
	# Start nodes (no incoming edges) are always unlocked

	# Get incoming edges for this node
	var incoming_edges: Array = _get_incoming_edges(battle_id)

	# No incoming edges = start node, always unlocked
	if incoming_edges.is_empty():
		return true

	# Check if ANY incoming edge is satisfied
	for edge: Variant in incoming_edges:
		if edge is Dictionary and _is_edge_satisfied(edge):
			return true

	return false


func _get_incoming_edges(node_id: String) -> Array:
	var result: Array = []
	for edge: Variant in _edges:
		if edge is Dictionary:
			var to_id: String = edge.get("to", "")
			if to_id == node_id:
				result.append(edge)
	return result


func _get_outgoing_edges(node_id: String) -> Array:
	var result: Array = []
	for edge: Variant in _edges:
		if edge is Dictionary:
			var from_id: String = edge.get("from", "")
			if from_id == node_id:
				result.append(edge)
	return result


func _is_edge_satisfied(edge: Dictionary) -> bool:
	# Source node must be completed
	var from_id: String = edge.get("from", "")
	if from_id not in _completed_battles:
		return false

	# Check edge condition (if any)
	var condition: Variant = edge.get("condition")
	if condition is Dictionary:
		return _evaluate_condition(condition, from_id)

	# No condition, just requires source completion
	return true


func _evaluate_condition(condition: Dictionary, source_node_id: String) -> bool:
	# Handle shorthand format: {"choice": "elite"}
	if condition.has("choice"):
		var required_value: String = condition.get("choice", "")
		var choice_made: String = _choices.get(source_node_id, "")
		return choice_made == required_value

	# Handle full format: {"type": "choice", "value": "elite", "node_id": "optional"}
	var condition_type: String = condition.get("type", "")
	var required_value: String = condition.get("value", "")
	var choice_node_id: String = condition.get("node_id", source_node_id)

	match condition_type:
		"choice":
			var choice_made: String = _choices.get(choice_node_id, "")
			return choice_made == required_value
		"completed":
			return choice_node_id in _completed_battles
		_:
			# Unknown condition type = pass
			return true


func GetAvailableBattles() -> Array:
	var result: Array = []
	var campaign: Dictionary = _campaigns.get(_current_campaign_id, {})
	var battles: Array = campaign.get("battles", [])

	for battle_variant: Variant in battles:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = battle.get("id", "")

		if IsBattleUnlocked(battle_id) and not IsBattleCompleted(battle_id):
			result.append(battle.duplicate())

	return result


func GetCompletedBattles() -> Array:
	var result: Array = []
	var campaign: Dictionary = _campaigns.get(_current_campaign_id, {})
	var battles: Array = campaign.get("battles", [])

	for battle_variant: Variant in battles:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = battle.get("id", "")

		if IsBattleCompleted(battle_id):
			result.append(battle.duplicate())

	return result


## =============================================================================
## BATTLE COMPLETION
## =============================================================================

func CompleteBattle(battle_id: String) -> void:
	_record_call("CompleteBattle", [battle_id])

	if battle_id in _completed_battles:
		push_warning("CampaignService: Battle '%s' already completed" % battle_id)
		return

	_completed_battles.append(battle_id)
	SaveProgress()
	BattleCompleted.emit(battle_id)

	# Check for newly unlocked battles
	_check_unlocked_battles()


func CompleteBattleWithoutReward(battle_id: String) -> void:
	_record_call("CompleteBattleWithoutReward", [battle_id])
	CompleteBattle(battle_id)


func _check_unlocked_battles() -> void:
	# Graph-based unlock checking (matches NodeUnlockHandler.GetNewlyUnlockedNodes)
	# Find nodes that were just unlocked by the last completion
	var last_completed: String = ""
	if _completed_battles.size() > 0:
		last_completed = _completed_battles[_completed_battles.size() - 1]

	if last_completed.is_empty():
		return

	# Get all nodes that have edges from the completed node
	var outgoing_edges: Array = _get_outgoing_edges(last_completed)
	for edge: Variant in outgoing_edges:
		if not edge is Dictionary:
			continue
		var to_id: String = edge.get("to", "")

		# Check if this node is now unlocked and not completed
		if IsBattleUnlocked(to_id) and not IsBattleCompleted(to_id):
			BattleUnlocked.emit(to_id)


## =============================================================================
## PENDING REWARD
## =============================================================================

func SetPendingReward(battle_id: String, reward_type: String, choice_index: int = -1) -> void:
	_record_call("SetPendingReward", [battle_id, reward_type, choice_index])
	_pending_reward = {
		"battle_id": battle_id,
		"reward_type": reward_type,
		"choice_index": choice_index
	}
	SaveProgress()


func GetPendingReward() -> Dictionary:
	return _pending_reward.duplicate()


func UpdatePendingChoice(choice_index: int) -> void:
	_record_call("UpdatePendingChoice", [choice_index])
	if not _pending_reward.is_empty():
		_pending_reward["choice_index"] = choice_index
		SaveProgress()


func ClearPendingReward() -> void:
	_record_call("ClearPendingReward", [])
	_pending_reward = {}
	SaveProgress()


func ClaimPendingReward() -> Dictionary:
	_record_call("ClaimPendingReward", [])

	if _pending_reward.is_empty():
		push_warning("CampaignService: No pending reward to claim")
		return {}

	var battle_id: String = _pending_reward.get("battle_id", "")
	var choice_index: int = _pending_reward.get("choice_index", 0)

	var result: Dictionary = GrantBattleReward(battle_id, choice_index)
	CompleteBattle(battle_id)
	ClearPendingReward()

	return result


func GrantBattleReward(battle_id: String, chosen_index: int = 0) -> Dictionary:
	_record_call("GrantBattleReward", [battle_id, chosen_index])

	var battle: Dictionary = _battles.get(battle_id, {})
	if battle.is_empty():
		return {}

	var result: Dictionary = {"cards": [], "gold": 0}

	# Record gold reward in result (in production, C# grants via EconomyService.Instance directly)
	var gold_reward: int = battle.get("gold_reward", 0)
	result["gold"] = gold_reward

	# Grant cards - handle both fixed (reward_cards) and flexible (reward_options)
	var reward_cards: Array = battle.get("reward_cards", [])
	var reward_options: Array = battle.get("reward_options", [])

	if reward_cards.size() > 0:
		# Fixed reward - grant all cards
		for card_variant: Variant in reward_cards:
			if not card_variant is Dictionary:
				continue
			var card: Dictionary = card_variant
			var catalog_id: String = card.get("catalog_id", "")
			var rarity: String = card.get("rarity", "common")

			if not catalog_id.is_empty() and _grant_card.is_valid():
				var instance_id: String = _grant_card.call(catalog_id, rarity)
				result["cards"].append({
					"catalog_id": catalog_id,
					"instance_id": instance_id,
					"rarity": rarity
				})
	elif reward_options.size() > 0:
		# Flexible reward - grant the chosen option
		var safe_index: int = clampi(chosen_index, 0, reward_options.size() - 1)
		var chosen_option: Variant = reward_options[safe_index]

		# reward_options can be array of card IDs (String or StringName) or dictionaries
		var catalog_id: String = ""
		var rarity: String = "common"

		if chosen_option is String or chosen_option is StringName:
			catalog_id = String(chosen_option)
		elif chosen_option is Dictionary:
			catalog_id = String(chosen_option.get("catalog_id", ""))
			rarity = String(chosen_option.get("rarity", "common"))

		if not catalog_id.is_empty() and _grant_card.is_valid():
			var instance_id: String = _grant_card.call(catalog_id, rarity)
			result["cards"].append({
				"catalog_id": catalog_id,
				"instance_id": instance_id,
				"rarity": rarity
			})

	return result


## =============================================================================
## TUTORIAL HELPERS
## =============================================================================

func IsBattleTutorial(battle_id: String) -> bool:
	var battle: Dictionary = _battles.get(battle_id, {})
	return battle.get("is_tutorial", false)


func IsTutorialComplete() -> bool:
	var tutorials: Array = GetTutorialBattles()
	for tutorial_id: String in tutorials:
		if not IsBattleCompleted(tutorial_id):
			return false
	return tutorials.size() > 0


func GetTutorialBattles() -> Array:
	var result: Array = []
	for battle_id: String in _battles.keys():
		if IsBattleTutorial(battle_id):
			result.append(battle_id)
	return result


## =============================================================================
## CAMPAIGN ECONOMY
## =============================================================================

func EndCampaign(summoner_id: String, _victory: bool) -> void:
	_record_call("EndCampaign", [summoner_id, _victory])
	# C# clears gold via EconomyService.Instance.ClearCampaignGold() directly


## =============================================================================
## CHOICE TRACKING
## =============================================================================

var _choices: Dictionary = {}  # node_id -> choice_id

func RecordChoice(node_id: String, choice_id: String) -> void:
	_record_call("RecordChoice", [node_id, choice_id])
	_choices[node_id] = choice_id
	SaveProgress()


func GetChoice(node_id: String) -> String:
	return _choices.get(node_id, "")


func HasChoice(node_id: String) -> bool:
	return _choices.has(node_id)


func GetAllChoices() -> Dictionary:
	return _choices.duplicate()


## =============================================================================
## PROGRESS RESET
## =============================================================================

func ResetProgress() -> void:
	_record_call("ResetProgress", [])
	_completed_battles.clear()
	_choices.clear()
	_pending_reward = {}
	CampaignProgressChanged.emit()
