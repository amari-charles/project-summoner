extends Node
# CampaignService is registered as autoload "Campaign", no class_name needed

## CampaignService - Manages campaign progression and battle rewards (GDScript wrapper for C#)
##
## Tracks which battles have been completed and handles reward distribution.
## Battle definitions are loaded from GDScript data files.
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
var economy_service: Node = null  # EconomyService
var collection_service: Node = null  # CollectionService

## C# service reference
var _cs_service: Node

## Campaign metadata cache (for data that stays in GDScript)
var _campaigns: Dictionary = {}

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
	if economy_service == null:
		economy_service = Economy
	if collection_service == null:
		collection_service = Collection

	# Connect to C# service signals
	_cs_service.BattleCompleted.connect(_on_cs_battle_completed)
	_cs_service.BattleUnlocked.connect(_on_cs_battle_unlocked)
	_cs_service.CampaignProgressChanged.connect(_on_cs_progress_changed)
	_cs_service.CampaignChanged.connect(_on_cs_campaign_changed)

	# Inject Economy callbacks
	_cs_service.SetEconomyCallbacks(_get_campaign_gold, _add_campaign_gold, _clear_campaign_gold)

	# Inject Collection callbacks
	_cs_service.SetCollectionCallbacks(_grant_card)

	# Inject active summoner getter
	_cs_service.SetActiveSummonerGetter(_get_active_summoner)

	# Load campaigns from GDScript data files and pass to C#
	_load_campaigns()

	# Load selected campaign from profile meta (default to first campaign)
	var meta: Dictionary = profile_repo.get_profile_meta()
	var selected_campaign: String = meta.get("selected_campaign", String(CampaignIDs.DEFAULT))

	# Ensure selected campaign is valid
	if not _campaigns.has(selected_campaign):
		selected_campaign = String(CampaignIDs.DEFAULT)

	_cs_service.SetCurrentCampaign(selected_campaign)

	# Reload progress when profile changes (e.g., on reset)
	profile_repo.data_changed.connect(_on_profile_data_changed)

	# Reload progress when active summoner changes
	SummonerSelection.SummonerChanged.connect(_on_summoner_changed)

	print("CampaignService: Ready")


## Initialize for unit testing with mock dependencies
## Call this instead of relying on _ready() in tests
## Pass a MockCampaignServiceCS instance to enable full testing without C# autoload
func init_for_testing(repo: IProfileRepo, economy: Node = null, collection: Node = null, cs_service_mock: Node = null) -> void:
	profile_repo = repo
	economy_service = economy
	collection_service = collection

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

		# Inject callbacks
		_cs_service.SetEconomyCallbacks(_get_campaign_gold, _add_campaign_gold, _clear_campaign_gold)
		_cs_service.SetCollectionCallbacks(_grant_card)
		if _cs_service.has_method("SetActiveSummonerGetter"):
			_cs_service.SetActiveSummonerGetter(_get_active_summoner)

	_load_campaigns(true)  # Skip validation in tests (no scene tree access)
	if _cs_service != null:
		_cs_service.SetCurrentCampaign(String(CampaignIDs.DEFAULT))


## =============================================================================
## CALLBACK INJECTORS
## =============================================================================

func _get_campaign_gold() -> int:
	if economy_service == null:
		return 0
	return economy_service.get_campaign_gold()

func _add_campaign_gold(amount: int) -> void:
	if economy_service != null:
		economy_service.add_campaign_gold(amount)

func _clear_campaign_gold(summoner_id: String) -> void:
	if economy_service != null:
		economy_service.clear_campaign_gold(summoner_id)

func _grant_card(catalog_id: String, rarity: String) -> String:
	if collection_service == null:
		return ""
	return collection_service.GrantCard(catalog_id, rarity)

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
## CAMPAIGN LOADING (GDScript-specific)
## =============================================================================

## Load all campaigns from GDScript data files
## Campaign data uses CardIDs constants for compile-time validation
func _load_campaigns(skip_validation: bool = false) -> void:
	_campaigns.clear()

	# Load campaigns from GDScript data files (compile-time validated CardIDs)
	var campaign_data_sources: Array[Callable] = [
		OnboardingData.get_campaign,
		CombatArenaData.get_campaign,
		AcademyTrialsData.get_campaign,
	]

	var campaigns_for_cs: Array[Dictionary] = []

	for get_campaign: Callable in campaign_data_sources:
		var campaign_data: Dictionary = get_campaign.call()
		var campaign_id: String = String(campaign_data.get("campaign_id", ""))
		if not campaign_id.is_empty():
			# Convert campaign_id to String for consistency
			campaign_data["campaign_id"] = campaign_id

			# Convert unlock_requirements from StringName to String
			var raw_reqs: Variant = campaign_data.get("unlock_requirements", [])
			if raw_reqs is Array:
				var string_reqs: Array[String] = []
				for req: Variant in raw_reqs:
					string_reqs.append(String(req))
				campaign_data["unlock_requirements"] = string_reqs

			# Process battles
			_load_battles_from_campaign(campaign_data)

			_campaigns[campaign_id] = campaign_data
			campaigns_for_cs.append(campaign_data)

	print("CampaignService: Loaded %d campaigns" % _campaigns.size())

	# Pass campaign data to C# service
	if _cs_service != null:
		_cs_service.LoadCampaignsFromGDScript(campaigns_for_cs)

	# Validate all battle rewards exist in card catalog (skip in tests)
	if not skip_validation:
		_validate_battle_rewards()

## Load battles from a campaign data dictionary
func _load_battles_from_campaign(campaign_data: Dictionary) -> void:
	var battles_array: Variant = campaign_data.get("battles", [])
	if not battles_array is Array:
		return

	for battle_variant: Variant in battles_array:
		if not battle_variant is Dictionary:
			continue
		var battle: Dictionary = battle_variant
		var battle_id: String = String(battle.get("id", ""))
		if battle_id.is_empty():
			push_warning("CampaignService: Battle missing 'id' field, skipping")
			continue

		# Convert StringName id to String for compatibility with UI code
		battle["id"] = battle_id

		# Convert unlock_requirements from StringName to String
		var raw_reqs: Variant = battle.get("unlock_requirements", [])
		if raw_reqs is Array:
			var string_reqs: Array[String] = []
			for req: Variant in raw_reqs:
				string_reqs.append(String(req))
			battle["unlock_requirements"] = string_reqs

		# Convert deck entries (enemy_deck, dev_player_deck, reward_cards) - catalog_id from StringName to String
		_convert_deck_entries(battle, "enemy_deck")
		_convert_deck_entries(battle, "dev_player_deck")
		_convert_deck_entries(battle, "reward_cards")

		# Convert localization keys to localized strings
		var name_key: String = battle.get("name_key", "")
		var desc_key: String = battle.get("description_key", "")
		battle["name"] = Loc.t(name_key) if not name_key.is_empty() else ""
		battle["description"] = Loc.t(desc_key) if not desc_key.is_empty() else ""

## Convert deck entry arrays to use String catalog_ids (from StringName constants)
func _convert_deck_entries(battle: Dictionary, key: String) -> void:
	var raw_deck: Variant = battle.get(key, [])
	if not raw_deck is Array:
		return

	var converted: Array[Dictionary] = []
	for entry_variant: Variant in raw_deck:
		if not entry_variant is Dictionary:
			continue
		var entry: Dictionary = entry_variant.duplicate()
		# Convert catalog_id from StringName to String
		var catalog_id: Variant = entry.get("catalog_id", "")
		entry["catalog_id"] = String(catalog_id)
		# Convert rarity if present (for reward_cards)
		if entry.has("rarity"):
			entry["rarity"] = String(entry.get("rarity", ""))
		converted.append(entry)

	battle[key] = converted

## Validate that all reward cards in battle configs exist in the card catalog
func _validate_battle_rewards() -> void:
	var invalid_count: int = 0
	for campaign_id: String in _campaigns.keys():
		var campaign: Dictionary = _campaigns[campaign_id]
		var battles: Array = campaign.get("battles", [])
		for battle_variant: Variant in battles:
			if not battle_variant is Dictionary:
				continue
			var battle: Dictionary = battle_variant
			var battle_id: String = battle.get("id", "")
			var reward_cards: Array = battle.get("reward_cards", [])

			for reward_variant: Variant in reward_cards:
				if not reward_variant is Dictionary:
					continue
				var reward: Dictionary = reward_variant
				var catalog_id: String = reward.get("catalog_id", "")

				if catalog_id.is_empty():
					continue

				if not CardCatalog.has_card(catalog_id):
					push_error("CampaignService: INVALID REWARD - Battle '%s' has reward card '%s' which doesn't exist in CardCatalog!" % [battle_id, catalog_id])
					invalid_count += 1

	if invalid_count > 0:
		push_error("CampaignService: Found %d invalid reward card references! Fix these before shipping." % invalid_count)
	else:
		var total_battles: int = 0
		for c: Dictionary in _campaigns.values():
			total_battles += c.get("battles", []).size()
		print("CampaignService: All %d battles validated - rewards are properly configured" % total_battles)

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

	if not _campaigns.has(campaign_id):
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

## Check if onboarding is complete (convenience method)
func is_onboarding_complete() -> bool:
	if _cs_service == null:
		return false
	return _cs_service.IsOnboardingComplete()

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
## CAMPAIGN ECONOMY (delegated to C#)
## =============================================================================

## Get current campaign gold for a summoner
func get_campaign_gold(summoner_id: String = "") -> int:
	if economy_service == null:
		return 0
	return economy_service.get_campaign_gold(summoner_id)

## End a campaign (victory or defeat)
## This clears all campaign-scoped resources (gold)
func end_campaign(summoner_id: String = "", victory: bool = false) -> void:
	if _cs_service != null:
		_cs_service.EndCampaign(summoner_id, victory)
