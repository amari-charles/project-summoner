extends Node
# CampaignService is registered as autoload "Campaign", no class_name needed

## CampaignService - Manages campaign progression and battle rewards (GDScript wrapper for C#)
##
## Tracks which battles have been completed and handles reward distribution.
## Campaign/event data is now defined in C# (EventCatalog, CampaignCatalog).
## Delegates to C# CampaignServiceCS for core operations.


## Signals (forwarded from C#)
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
var collection_service: Node = null  # CollectionService
var deck_service: Node = null  # DeckService (for starter deck auto-add)

## C# service reference
var _cs_service: Node

## Deck constants (preloaded for class_name compatibility in headless mode)
const _DeckConstants: GDScript = preload("res://scripts/data/deck_constants.gd")


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/CampaignServiceCS")
	if _cs_service == null:
		push_error("CampaignService: CampaignServiceCS autoload not found")
		return

	# Use injected dependencies or fall back to autoloads
	if profile_repo == null:
		profile_repo = ProfileRepo
	if collection_service == null:
		collection_service = CardServiceCS
	if deck_service == null:
		deck_service = Decks

	# Connect to C# service signals
	_cs_service.BattleCompleted.connect(_on_cs_battle_completed)
	_cs_service.BattleUnlocked.connect(_on_cs_battle_unlocked)
	_cs_service.CampaignProgressChanged.connect(_on_cs_progress_changed)
	_cs_service.CampaignChanged.connect(_on_cs_campaign_changed)

	# Inject Collection callbacks (economy uses EconomyService.Instance directly in C#)
	_cs_service.SetCollectionCallbacks(_grant_card)

	# Inject active summoner getter
	_cs_service.SetActiveSummonerGetter(_get_active_summoner)

	# Initialize campaign data from C# catalogs (EventCatalog, CampaignCatalog)
	_cs_service.InitializeCatalogs()

	# Load selected campaign from profile meta (default to first campaign)
	var meta: Dictionary = profile_repo.get_profile_meta()
	var selected_campaign: String = meta.get("selected_campaign", String(CampaignIDs.DEFAULT))

	_cs_service.SetCurrentCampaign(selected_campaign)

	# Reload progress when profile changes (e.g., on reset)
	profile_repo.data_changed.connect(_on_profile_data_changed)

	# Reload progress when active summoner changes
	SummonerSelection.SummonerChanged.connect(_on_summoner_changed)

	print("CampaignService: Ready")


## Initialize for unit testing with mock dependencies
## Call this instead of relying on _ready() in tests
## Pass a MockCampaignServiceCS instance to enable full testing without C# autoload
## Pass deck = null to disable starter deck auto-add in tests
func init_for_testing(repo: IProfileRepo, collection: Node = null, cs_service_mock: Node = null, deck: Node = null) -> void:
	profile_repo = repo
	collection_service = collection
	deck_service = deck  # null in tests disables auto-add to starter deck

	# Use provided mock or try to find real autoload
	if cs_service_mock != null:
		_cs_service = cs_service_mock
		# Set up the mock with profile repo reference
		if _cs_service.has_method("set_profile_repo"):
			_cs_service.set_profile_repo(repo)
	elif _cs_service == null:
		_cs_service = get_node_or_null("/root/CampaignServiceCS")

	# Disconnect previous connections if any
	if profile_repo.data_changed.is_connected(_on_profile_data_changed):
		profile_repo.data_changed.disconnect(_on_profile_data_changed)
	profile_repo.data_changed.connect(_on_profile_data_changed)

	# Set up callbacks if we have a C# service
	if _cs_service != null:
		# Connect signals (check if not already connected)
		if _cs_service.has_signal("BattleCompleted") and not _cs_service.BattleCompleted.is_connected(_on_cs_battle_completed):
			_cs_service.BattleCompleted.connect(_on_cs_battle_completed)
		if _cs_service.has_signal("BattleUnlocked") and not _cs_service.BattleUnlocked.is_connected(_on_cs_battle_unlocked):
			_cs_service.BattleUnlocked.connect(_on_cs_battle_unlocked)
		if _cs_service.has_signal("CampaignProgressChanged") and not _cs_service.CampaignProgressChanged.is_connected(_on_cs_progress_changed):
			_cs_service.CampaignProgressChanged.connect(_on_cs_progress_changed)
		if _cs_service.has_signal("CampaignChanged") and not _cs_service.CampaignChanged.is_connected(_on_cs_campaign_changed):
			_cs_service.CampaignChanged.connect(_on_cs_campaign_changed)

		# Inject callbacks (economy uses EconomyService.Instance directly in C#)
		_cs_service.SetCollectionCallbacks(_grant_card)
		if _cs_service.has_method("SetActiveSummonerGetter"):
			_cs_service.SetActiveSummonerGetter(_get_active_summoner)

		# Initialize from C# catalogs
		if _cs_service.has_method("InitializeCatalogs"):
			_cs_service.InitializeCatalogs()

	if _cs_service != null:
		_cs_service.SetCurrentCampaign(String(CampaignIDs.DEFAULT))


## =============================================================================
## CALLBACK INJECTORS
## =============================================================================

func _grant_card(catalog_id: String, rarity: String) -> String:
	if collection_service == null:
		return ""
	var instance_id: String = collection_service.GrantCard(catalog_id, rarity)

	# Auto-add to Starter Deck if under threshold
	if not instance_id.is_empty():
		_try_auto_add_to_starter_deck(instance_id)

	return instance_id


func _try_auto_add_to_starter_deck(card_instance_id: String) -> void:
	# Skip if no deck service (e.g., in tests)
	if deck_service == null:
		return

	# Find the Starter Deck by name
	var decks: Array = deck_service.list_decks()
	for deck: Variant in decks:
		if not deck is Dictionary:
			continue
		var deck_dict: Dictionary = deck
		if deck_dict.get("name", "") == _DeckConstants.STARTER_DECK_NAME:
			var card_ids: Array = deck_dict.get("card_instance_ids", [])
			if card_ids.size() < _DeckConstants.STARTER_DECK_AUTO_ADD_THRESHOLD:
				var deck_id: String = deck_dict.get("id", "")
				if not deck_id.is_empty():
					deck_service.add_card_to_deck(deck_id, card_instance_id)
			return  # Only one Starter Deck expected


func _get_active_summoner() -> String:
	return SummonerSelection.GetActiveSummonerId()

## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_battle_completed(battle_id: String) -> void:
	battle_completed.emit(battle_id)

func _on_cs_battle_unlocked(battle_id: String) -> void:
	battle_unlocked.emit(battle_id)

func _on_cs_progress_changed() -> void:
	campaign_progress_changed.emit()

func _on_cs_campaign_changed(old_campaign_id: String, new_campaign_id: String) -> void:
	campaign_changed.emit(old_campaign_id, new_campaign_id)

func _on_profile_data_changed() -> void:
	print("CampaignService: Profile data changed - reloading progress...")
	if _cs_service != null:
		_cs_service.LoadProgress()
		_cs_service.NotifyProgressChanged()

func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	print("CampaignService: Summoner changed to '%s' - reloading progress..." % new_summoner_id)
	if _cs_service != null:
		_cs_service.LoadProgress()
		_cs_service.NotifyProgressChanged()

## =============================================================================
## CAMPAIGN QUERIES (delegated to C#)
## =============================================================================

## Get all campaigns with unlock status
func get_all_campaigns() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetAllCampaigns()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## Get a specific campaign's metadata
func get_campaign(campaign_id: String) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetCampaign(campaign_id)

## Get the currently selected campaign ID
func get_current_campaign_id() -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GetCurrentCampaignId()

## Set the current campaign and save to profile
func set_current_campaign(campaign_id: String) -> bool:
	if _cs_service == null:
		return false

	if not _cs_service.HasCampaign(campaign_id):
		push_warning("CampaignService: Invalid campaign ID: %s" % campaign_id)
		return false

	if not is_campaign_unlocked(campaign_id):
		push_warning("CampaignService: Campaign '%s' is locked" % campaign_id)
		return false

	_cs_service.SetCurrentCampaign(campaign_id)

	# Save to profile meta
	profile_repo.update_profile_meta({"selected_campaign": campaign_id})

	print("CampaignService: Switched to campaign '%s'" % campaign_id)
	return true

## Check if a campaign is unlocked
func is_campaign_unlocked(campaign_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsCampaignUnlocked(campaign_id)


## =============================================================================
## PROGRESS MANAGEMENT (delegated to C#)
## =============================================================================

func save_progress() -> void:
	if _cs_service != null:
		_cs_service.SaveProgress()

## =============================================================================
## BATTLE QUERIES (delegated to C#)
## =============================================================================

func get_all_battles() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetAllBattles()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

func get_battle(battle_id: String) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetBattle(battle_id)

func is_battle_completed(battle_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsBattleCompleted(battle_id)

func is_battle_unlocked(battle_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsBattleUnlocked(battle_id)

func get_available_battles() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetAvailableBattles()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

func get_completed_battles() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetCompletedBattles()
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## PENDING REWARD MANAGEMENT (delegated to C#)
## =============================================================================

## Set a pending reward for a battle (called when player wins but hasn't claimed yet)
func set_pending_reward(battle_id: String, reward_type: String, choice_index: int = -1) -> void:
	if _cs_service != null:
		_cs_service.SetPendingReward(battle_id, reward_type, choice_index)

## Get the current pending reward (null if none)
func get_pending_reward() -> Variant:
	if _cs_service == null:
		return null
	var result: Dictionary = _cs_service.GetPendingReward()
	if result.is_empty():
		return null
	return result

## Update choice index for a pending choice reward
func update_pending_choice(choice_index: int) -> void:
	if _cs_service != null:
		_cs_service.UpdatePendingChoice(choice_index)

## Clear the pending reward (called after reward is claimed)
func clear_pending_reward() -> void:
	if _cs_service != null:
		_cs_service.ClearPendingReward()

## Claim the pending reward (grants cards and marks battle complete)
## Returns the granted card info or empty dict if failed
func claim_pending_reward() -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.ClaimPendingReward()

## =============================================================================
## BATTLE COMPLETION & REWARDS (delegated to C#)
## =============================================================================

## Claim all rewards for a battle and mark it complete.
## This is the single entry point for reward claiming - replaces both
## claim_pending_reward() and complete_battle_without_reward() patterns.
## @param battle_id: The battle to claim rewards for
## @param card_reward: The card reward to grant (from flexible selection or fixed battle config)
## @return Dictionary with granted rewards {gold: int, cards: Array, instance_ids: Array}
func claim_battle_rewards(battle_id: String, card_reward: Dictionary = {}) -> Dictionary:
	# Guard against replay - don't grant rewards for completed battles
	if is_battle_completed(battle_id):
		print("CampaignService: Battle '%s' already completed, skipping rewards" % battle_id)
		return {}

	var battle: Dictionary = get_battle(battle_id)
	if battle.is_empty():
		push_error("CampaignService: Battle not found: %s" % battle_id)
		return {}

	# Grant all rewards via RewardService
	var granted: Dictionary = RewardService.grant_battle_rewards(battle, card_reward)

	# Mark battle complete and clear pending
	if _cs_service != null:
		_cs_service.CompleteBattle(battle_id)
		_cs_service.ClearPendingReward()

	print("CampaignService: Claimed rewards for battle '%s': %s" % [battle_id, granted])
	return granted

func complete_battle(battle_id: String) -> void:
	if _cs_service != null:
		_cs_service.CompleteBattle(battle_id)

## Complete a battle without granting rewards (used when rewards are granted externally, e.g., via RewardService)
func complete_battle_without_reward(battle_id: String) -> void:
	if _cs_service != null:
		_cs_service.CompleteBattleWithoutReward(battle_id)

func grant_battle_reward(battle_id: String, chosen_index: int = 0) -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GrantBattleReward(battle_id, chosen_index)

## =============================================================================
## TUTORIAL HELPERS (delegated to C#)
## =============================================================================

## Check if a specific battle is a tutorial battle
func is_battle_tutorial(battle_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsBattleTutorial(battle_id)

## Check if all tutorial battles have been completed
func is_tutorial_complete() -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsTutorialComplete()

## Get list of all tutorial battle IDs
func get_tutorial_battles() -> Array[String]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetTutorialBattles()
	var typed_result: Array[String] = []
	typed_result.assign(result)
	return typed_result

## =============================================================================
## GRAPH DATA ACCESS
## =============================================================================

## Get nodes for the current campaign (graph format)
func get_current_campaign_nodes() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var campaign: Dictionary = _cs_service.GetCampaign(get_current_campaign_id())
	if campaign.is_empty():
		return []
	var nodes: Variant = campaign.get("nodes", [])
	if not nodes is Array:
		return []
	var typed_result: Array[Dictionary] = []
	typed_result.assign(nodes)
	return typed_result

## Get edges for the current campaign (graph format)
func get_current_campaign_edges() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var campaign: Dictionary = _cs_service.GetCampaign(get_current_campaign_id())
	if campaign.is_empty():
		return []
	var edges: Variant = campaign.get("edges", [])
	if not edges is Array:
		return []
	var typed_result: Array[Dictionary] = []
	typed_result.assign(edges)
	return typed_result

## Get the start node ID for the current campaign
func get_current_campaign_start_node() -> String:
	if _cs_service == null:
		return ""
	var campaign: Dictionary = _cs_service.GetCampaign(get_current_campaign_id())
	if campaign.is_empty():
		return ""
	return String(campaign.get("start_node", ""))

## =============================================================================
## CAMPAIGN ECONOMY (delegated to C#)
## =============================================================================

## Get current campaign gold for a summoner
func get_campaign_gold(summoner_id: String = "") -> int:
	return Economy.get_campaign_gold(summoner_id)

## End a campaign (victory or defeat)
## This clears all campaign-scoped resources (gold, caravan purchases)
func end_campaign(summoner_id: String = "", victory: bool = false) -> void:
	if _cs_service != null:
		_cs_service.EndCampaign(summoner_id, victory)

	# Clear campaign-scoped shop state
	ProfileRepo.clear_caravan_purchases(summoner_id)

## =============================================================================
## CHOICE RECORDING (for branching paths)
## =============================================================================

## Record a choice made at a choice node
func record_choice(node_id: String, choice_id: String) -> void:
	if _cs_service != null:
		_cs_service.RecordChoice(node_id, choice_id)

## Get the choice made at a specific node (empty string if none)
func get_choice(node_id: String) -> String:
	if _cs_service == null:
		return ""
	return _cs_service.GetChoice(node_id)

## Check if a choice has been made at a specific node
func has_choice(node_id: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.HasChoice(node_id)

## Get all choices as a dictionary (node_id -> choice_id)
func get_all_choices() -> Dictionary:
	if _cs_service == null:
		return {}
	return _cs_service.GetAllChoices()

## =============================================================================
## PROGRESS RESET
## =============================================================================

## Reset all campaign progress for the current summoner
func reset_progress() -> void:
	if _cs_service != null:
		_cs_service.ResetProgress()
