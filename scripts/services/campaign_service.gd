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
var collection_service: Node = null  # CollectionService
var deck_service: Node = null  # DeckService (for starter deck auto-add)

## C# service reference
var _cs_service: Node

## Deck constants (preloaded for class_name compatibility in headless mode)
const _DeckConstants: GDScript = preload("res://scripts/data/deck_constants.gd")


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

	_load_campaigns(true)  # Skip validation in tests (no scene tree access)
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
## CAMPAIGN LOADING (GDScript-specific)
## =============================================================================

## Load all campaigns from GDScript data files
## Campaign data uses CardIDs constants for compile-time validation
func _load_campaigns(skip_validation: bool = false) -> void:
	_campaigns.clear()

	# Load campaigns from GDScript data files (compile-time validated CardIDs)
	var campaign_data_sources: Array[Callable] = [
		SummonersPathData.get_campaign,
		TestArenaData.get_campaign,
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

			# Process nodes (graph format)
			_load_nodes_from_campaign(campaign_data)

			_campaigns[campaign_id] = campaign_data
			campaigns_for_cs.append(campaign_data)

	print("CampaignService: Loaded %d campaigns" % _campaigns.size())

	# Pass campaign data to C# service
	if _cs_service != null:
		_cs_service.LoadCampaignsFromGDScript(campaigns_for_cs)

	# Validate all battle rewards exist in card catalog (skip in tests)
	if not skip_validation:
		_validate_battle_rewards()

## Load nodes from a campaign data dictionary (graph format)
## Creates a flattened 'battles' array that merges node metadata (id, type, position)
## with node.data for convenient access via get_battle() API
func _load_nodes_from_campaign(campaign_data: Dictionary) -> void:
	var nodes_array: Variant = campaign_data.get("nodes", [])
	if not nodes_array is Array:
		return

	# Create a flattened battles array for backwards compatibility
	var battles: Array[Dictionary] = []

	for node_variant: Variant in nodes_array:
		if not node_variant is Dictionary:
			continue
		var node: Dictionary = node_variant
		var node_id: String = String(node.get("id", ""))
		if node_id.is_empty():
			push_warning("CampaignService: Node missing 'id' field, skipping")
			continue

		# Convert StringName id to String for compatibility with UI code
		node["id"] = node_id

		# Convert type from StringName to String
		if node.has("type"):
			node["type"] = String(node.get("type", ""))

		# Get the nested data dictionary and process it
		var node_data: Variant = node.get("data", {})
		if node_data is Dictionary:
			var data: Dictionary = node_data

			# Convert deck entries within data
			_convert_deck_entries(data, "enemy_deck")
			_convert_deck_entries(data, "dev_player_deck")
			_convert_deck_entries(data, "reward_cards")
			_convert_deck_entries(data, "reward_options")

			# Convert localization keys to localized strings
			var name_key: String = data.get("name_key", "")
			var desc_key: String = data.get("description_key", "")
			data["name"] = Loc.t(name_key) if not name_key.is_empty() else ""
			data["description"] = Loc.t(desc_key) if not desc_key.is_empty() else ""

			node["data"] = data

			# Create a merged entry combining node metadata with node.data
			var battle: Dictionary = data.duplicate()
			battle["id"] = node_id
			battle["type"] = node.get("type", "")
			battle["position"] = node.get("position", Vector2.ZERO)
			battle["event_type"] = _node_type_to_event_type(String(node.get("type", "")))
			battles.append(battle)

	# Process edges (convert StringName to String)
	var edges_array: Variant = campaign_data.get("edges", [])
	if edges_array is Array:
		var processed_edges: Array[Dictionary] = []
		for edge_variant: Variant in edges_array:
			if not edge_variant is Dictionary:
				continue
			var edge: Dictionary = edge_variant
			edge["from"] = String(edge.get("from", ""))
			edge["to"] = String(edge.get("to", ""))
			processed_edges.append(edge)
		campaign_data["edges"] = processed_edges

	# Store merged battles for get_battle() API access
	campaign_data["battles"] = battles


## Map node type to event type for UI type checking
func _node_type_to_event_type(node_type: String) -> String:
	match node_type:
		"battle", "elite", "boss":
			return "battle"
		"caravan":
			return "caravan"
		"choice":
			return "choice"
		"rest":
			return "rest"
		"story":
			return "story"
		_:
			return "battle"

## Convert deck entry arrays to use String catalog_ids (from StringName constants)
## Handles both dictionary entries (with catalog_id) and raw StringName IDs
func _convert_deck_entries(battle: Dictionary, key: String) -> void:
	if not battle.has(key):
		return  # Don't create empty arrays for missing keys

	var raw_deck: Variant = battle.get(key, [])
	if not raw_deck is Array:
		return

	var converted: Array = []
	for entry_variant: Variant in raw_deck:
		if entry_variant is Dictionary:
			var entry: Dictionary = entry_variant.duplicate()
			# Convert catalog_id from StringName to String
			var catalog_id: Variant = entry.get("catalog_id", "")
			entry["catalog_id"] = String(catalog_id)
			# Convert rarity if present (for reward_cards)
			if entry.has("rarity"):
				entry["rarity"] = String(entry.get("rarity", ""))
			converted.append(entry)
		elif entry_variant is StringName or entry_variant is String:
			# Handle raw card IDs (e.g., reward_options: [CardIDs.CHARGE, ...])
			converted.append(String(entry_variant))

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
## GRAPH DATA ACCESS
## =============================================================================

## Get nodes for the current campaign (graph format)
func get_current_campaign_nodes() -> Array[Dictionary]:
	var campaign_id: String = get_current_campaign_id()
	if not _campaigns.has(campaign_id):
		return []
	var campaign: Dictionary = _campaigns[campaign_id]
	var nodes: Variant = campaign.get("nodes", [])
	if not nodes is Array:
		return []
	var typed_result: Array[Dictionary] = []
	typed_result.assign(nodes)
	return typed_result

## Get edges for the current campaign (graph format)
func get_current_campaign_edges() -> Array[Dictionary]:
	var campaign_id: String = get_current_campaign_id()
	if not _campaigns.has(campaign_id):
		return []
	var campaign: Dictionary = _campaigns[campaign_id]
	var edges: Variant = campaign.get("edges", [])
	if not edges is Array:
		return []
	var typed_result: Array[Dictionary] = []
	typed_result.assign(edges)
	return typed_result

## Get the start node ID for the current campaign
func get_current_campaign_start_node() -> String:
	var campaign_id: String = get_current_campaign_id()
	if not _campaigns.has(campaign_id):
		return ""
	var campaign: Dictionary = _campaigns[campaign_id]
	return String(campaign.get("start_node", ""))

## =============================================================================
## CAMPAIGN ECONOMY (delegated to C#)
## =============================================================================

## Get current campaign gold for a summoner
func get_campaign_gold(summoner_id: String = "") -> int:
	return Economy.get_campaign_gold(summoner_id)

## End a campaign (victory or defeat)
## This clears all campaign-scoped resources (gold)
func end_campaign(summoner_id: String = "", victory: bool = false) -> void:
	if _cs_service != null:
		_cs_service.EndCampaign(summoner_id, victory)

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
