extends Node
class_name MockCampaignServiceCS

## Mock C# Campaign Service for Unit Testing
##
## GDScript implementation that mimics CampaignServiceCS behavior.
## Allows CampaignService unit tests to run without the C# autoload.

## Signals (match C# service)
signal BattleCompleted(battle_id: String)
signal BattleUnlocked(battle_id: String)
signal CampaignProgressChanged()
signal CampaignChanged(old_campaign_id: String, new_campaign_id: String)

## Internal state
var _campaigns: Dictionary = {}  # campaign_id -> campaign data
var _battles: Dictionary = {}    # battle_id -> battle data with campaign_id
var _completed_battles: Array = []
var _current_campaign_id: String = ""
var _pending_reward: Dictionary = {}

## Callback references (injected by GDScript wrapper)
var _get_campaign_gold: Callable
var _add_campaign_gold: Callable
var _clear_campaign_gold: Callable
var _grant_card: Callable
var _get_active_summoner: Callable

## Profile repo reference (for loading/saving progress)
var _profile_repo: IProfileRepo

## Call tracking for assertions
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
	_campaigns.clear()
	_battles.clear()
	_completed_battles.clear()
	_current_campaign_id = ""
	_pending_reward = {}
	_calls = {}


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

func SetEconomyCallbacks(get_gold: Callable, add_gold: Callable, clear_gold: Callable) -> void:
	_get_campaign_gold = get_gold
	_add_campaign_gold = add_gold
	_clear_campaign_gold = clear_gold


func SetCollectionCallbacks(grant_card: Callable) -> void:
	_grant_card = grant_card


func SetActiveSummonerGetter(getter: Callable) -> void:
	_get_active_summoner = getter


## =============================================================================
## CAMPAIGN LOADING
## =============================================================================

func LoadCampaignsFromGDScript(campaigns_array: Array) -> void:
	_record_call("LoadCampaignsFromGDScript", [campaigns_array])
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
	var battle: Dictionary = _battles.get(battle_id, {})
	var unlock_requirements: Array = battle.get("unlock_requirements", [])

	if unlock_requirements.is_empty():
		return true

	for req_id: Variant in unlock_requirements:
		if String(req_id) not in _completed_battles:
			return false

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
	var campaign: Dictionary = _campaigns.get(_current_campaign_id, {})
	var battles: Array = campaign.get("battles", [])

	for battle_variant: Variant in battles:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = battle.get("id", "")

		if not IsBattleCompleted(battle_id) and IsBattleUnlocked(battle_id):
			# Check if it was just unlocked (all requirements now met)
			var unlock_reqs: Array = battle.get("unlock_requirements", [])
			if unlock_reqs.size() > 0:
				var all_just_completed: bool = true
				for req_id: Variant in unlock_reqs:
					if String(req_id) not in _completed_battles:
						all_just_completed = false
						break
				if all_just_completed:
					BattleUnlocked.emit(battle_id)


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

	# Grant gold
	var gold_reward: int = battle.get("gold_reward", 0)
	if gold_reward > 0 and _add_campaign_gold.is_valid():
		_add_campaign_gold.call(gold_reward)
		result["gold"] = gold_reward

	# Grant cards
	var reward_cards: Array = battle.get("reward_cards", [])
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
	if _clear_campaign_gold.is_valid():
		_clear_campaign_gold.call(summoner_id)
