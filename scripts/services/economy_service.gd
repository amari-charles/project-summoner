extends Node
# EconomyService is registered as autoload "Economy", no class_name needed
# This GDScript wrapper delegates to C# EconomyServiceCS for implementation.

## Economy Service - Resource Management
##
## Handles all resource operations (gold, gems, essence, fragments).
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   Economy.add_gold(50)
##   Economy.add_gems(100)  # From real-money purchase
##   if Economy.can_afford({"gold": 100}):
##       Economy.spend({"gold": 100})
##
## Emits signals for reactive UI updates.

## Signals
signal resources_changed(gold: int, gems: int, essence: int, fragments: int)
signal transaction_completed(delta: Dictionary)
signal transaction_failed(reason: String)

## =============================================================================
## C# BRIDGE / TEST MODE
## =============================================================================

var _cs_service: Node = null
var _test_mode: bool = false
var _test_repo: Object = null  # IProfileRepo for testing

func _ready() -> void:
	if _test_mode:
		return  # Skip C# connection in test mode
	print("EconomyService (GD): Initializing as thin wrapper...")
	call_deferred("_connect_to_cs")


## Initialize for unit testing with mock dependencies
## Call this instead of relying on _ready() in tests
func init_for_testing(repo: Object) -> void:
	_test_mode = true
	_test_repo = repo
	if _test_repo.has_signal("data_changed"):
		if _test_repo.data_changed.is_connected(_on_repo_data_changed):
			_test_repo.data_changed.disconnect(_on_repo_data_changed)
		_test_repo.data_changed.connect(_on_repo_data_changed)


func _connect_to_cs() -> void:
	_cs_service = EconomyServiceCS

	# Forward C# signals to GDScript signals
	_cs_service.ResourcesChanged.connect(_on_cs_resources_changed)
	_cs_service.TransactionCompleted.connect(_on_cs_transaction_completed)
	_cs_service.TransactionFailed.connect(_on_cs_transaction_failed)
	_connect_campaign_gold_signal()

	print("EconomyService (GD): Connected to C# EconomyServiceCS")


func _on_repo_data_changed() -> void:
	# Test mode: repo data changed
	_emit_current_resources()


func _on_cs_resources_changed(gold: int, gems: int, essence: int, fragments: int) -> void:
	resources_changed.emit(gold, gems, essence, fragments)


func _on_cs_transaction_completed(delta: Dictionary) -> void:
	transaction_completed.emit(delta)


func _on_cs_transaction_failed(reason: String) -> void:
	transaction_failed.emit(reason)

## =============================================================================
## RESOURCE QUERIES
## =============================================================================

## Get current resource values
func get_resources() -> Dictionary:
	if _test_mode and _test_repo:
		var result: Variant = _test_repo.get_resources()
		return result if result is Dictionary else {"gold": 0, "gems": 0, "essence": 0, "fragments": 0}
	if _cs_service:
		var result: Variant = _cs_service.GetResourcesDict()
		return result if result is Dictionary else {"gold": 0, "gems": 0, "essence": 0, "fragments": 0}
	push_warning("EconomyService.get_resources: C# service not available")
	return {"gold": 0, "gems": 0, "essence": 0, "fragments": 0}

## Get specific resource amount
func get_gold() -> int:
	return get_resources().get("gold", 0)

func get_gems() -> int:
	return get_resources().get("gems", 0)

func get_essence() -> int:
	return get_resources().get("essence", 0)

func get_fragments() -> int:
	return get_resources().get("fragments", 0)

## Check if player can afford a cost
## cost: Dictionary like {"gold": 100, "essence": 50}
func can_afford(cost: Dictionary) -> bool:
	if _test_mode:
		var resources: Dictionary = get_resources()
		for key: String in cost:
			var required: int = cost[key] if cost[key] is int else 0
			if resources.get(key, 0) < required:
				return false
		return true
	if _cs_service:
		return _cs_service.CanAffordDict(cost)
	push_warning("EconomyService.can_afford: C# service not available")
	return false

## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

## Add gold (positive amount only)
func add_gold(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_gold called with non-positive amount: %d" % amount)
		return
	if _test_mode:
		_update_resources({"gold": amount})
		print("EconomyService: Added %d gold" % amount)
		return
	if _cs_service:
		_cs_service.AddGold(amount)
	else:
		push_warning("EconomyService.add_gold: C# service not available")

## Add gems (positive amount only) - typically from real-money purchases
func add_gems(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_gems called with non-positive amount: %d" % amount)
		return
	if _test_mode:
		_update_resources({"gems": amount})
		print("EconomyService: Added %d gems" % amount)
		return
	if _cs_service:
		_cs_service.AddGems(amount)
	else:
		push_warning("EconomyService.add_gems: C# service not available")

## Add essence (positive amount only)
func add_essence(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_essence called with non-positive amount: %d" % amount)
		return
	if _test_mode:
		_update_resources({"essence": amount})
		print("EconomyService: Added %d essence" % amount)
		return
	if _cs_service:
		_cs_service.AddEssence(amount)
	else:
		push_warning("EconomyService.add_essence: C# service not available")

## Add fragments (positive amount only)
func add_fragments(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_fragments called with non-positive amount: %d" % amount)
		return
	if _test_mode:
		_update_resources({"fragments": amount})
		print("EconomyService: Added %d fragments" % amount)
		return
	if _cs_service:
		_cs_service.AddFragments(amount)
	else:
		push_warning("EconomyService.add_fragments: C# service not available")

## Spend resources (negative delta)
## Returns true if successful, false if can't afford
func spend(cost: Dictionary) -> bool:
	if _test_mode:
		if not can_afford(cost):
			var reason: String = "Cannot afford: " + str(cost)
			push_warning("EconomyService: " + reason)
			transaction_failed.emit(reason)
			return false
		var delta: Dictionary = {}
		for key: String in cost:
			var amount: int = cost[key] if cost[key] is int else 0
			delta[key] = -amount
		_update_resources(delta)
		print("EconomyService: Spent %s" % str(cost))
		return true
	if _cs_service:
		return _cs_service.SpendDict(cost)
	push_warning("EconomyService.spend: C# service not available")
	return false

## Grant multiple resources at once (for rewards, etc.)
## rewards: Dictionary like {"gold": 100, "essence": 50}
func grant_rewards(rewards: Dictionary) -> void:
	if _test_mode:
		_update_resources(rewards)
		print("EconomyService: Granted rewards: %s" % str(rewards))
		return
	if _cs_service:
		_cs_service.GrantRewardsDict(rewards)
	else:
		push_warning("EconomyService.grant_rewards: C# service not available")

## =============================================================================
## CAMPAIGN-SCOPED GOLD
## =============================================================================
## Campaign gold is tied to a specific summoner's campaign progress.
## It is lost when the campaign ends (win or lose).
## Use these methods for in-campaign purchases (caravan) and battle rewards.
## Delegates to C# EconomyService for actual implementation.

signal campaign_gold_changed(summoner_id: String, gold: int)

func _connect_campaign_gold_signal() -> void:
	if _cs_service and _cs_service.has_signal("CampaignGoldChanged"):
		if not _cs_service.CampaignGoldChanged.is_connected(_on_cs_campaign_gold_changed):
			_cs_service.CampaignGoldChanged.connect(_on_cs_campaign_gold_changed)

func _on_cs_campaign_gold_changed(summoner_id: String, gold: int) -> void:
	campaign_gold_changed.emit(summoner_id, gold)

## Get campaign gold for a summoner (or active summoner if not specified)
func get_campaign_gold(summoner_id: String = "") -> int:
	if _test_mode and _test_repo:
		var progress: Dictionary = _test_repo.get_campaign_progress(summoner_id)
		return progress.get("gold", 0)
	if _cs_service:
		return _cs_service.GetCampaignGold(summoner_id)
	push_warning("EconomyService.get_campaign_gold: C# service not available")
	return 0

## Add campaign gold (positive amount only)
func add_campaign_gold(amount: int, summoner_id: String = "") -> void:
	print("EconomyService: add_campaign_gold called with amount=%d, summoner_id='%s'" % [amount, summoner_id])
	if _test_mode and _test_repo:
		var progress: Dictionary = _test_repo.get_campaign_progress(summoner_id)
		var new_gold: int = progress.get("gold", 0) + amount
		_test_repo.update_campaign_progress({"gold": new_gold}, summoner_id)
		var target_id: String = summoner_id if not summoner_id.is_empty() else SummonerSelection.GetActiveSummonerId()
		campaign_gold_changed.emit(target_id, new_gold)
		print("EconomyService: Test mode - updated campaign gold to %d" % new_gold)
		return
	if _cs_service:
		print("EconomyService: Calling C# AddCampaignGold(%d, '%s')" % [amount, summoner_id])
		_cs_service.AddCampaignGold(amount, summoner_id)
	else:
		push_warning("EconomyService.add_campaign_gold: C# service not available")

## Spend campaign gold
## Returns true if successful, false if can't afford
func spend_campaign_gold(amount: int, summoner_id: String = "") -> bool:
	if _test_mode and _test_repo:
		var current_gold: int = get_campaign_gold(summoner_id)
		if current_gold < amount:
			transaction_failed.emit("Cannot afford %d campaign gold" % amount)
			return false
		var new_gold: int = current_gold - amount
		_test_repo.update_campaign_progress({"gold": new_gold}, summoner_id)
		var target_id: String = summoner_id if not summoner_id.is_empty() else SummonerSelection.GetActiveSummonerId()
		campaign_gold_changed.emit(target_id, new_gold)
		return true
	if _cs_service:
		return _cs_service.SpendCampaignGold(amount, summoner_id)
	push_warning("EconomyService.spend_campaign_gold: C# service not available")
	return false

## Check if player can afford a campaign gold cost
func can_afford_campaign_gold(amount: int, summoner_id: String = "") -> bool:
	if _test_mode:
		return get_campaign_gold(summoner_id) >= amount
	if _cs_service:
		return _cs_service.CanAffordCampaignGold(amount, summoner_id)
	return false

## Clear all campaign gold (called when campaign ends)
func clear_campaign_gold(summoner_id: String = "") -> void:
	if _test_mode and _test_repo:
		_test_repo.update_campaign_progress({"gold": 0}, summoner_id)
		var target_id: String = summoner_id if not summoner_id.is_empty() else SummonerSelection.GetActiveSummonerId()
		campaign_gold_changed.emit(target_id, 0)
		return
	if _cs_service:
		_cs_service.ClearCampaignGold(summoner_id)
	else:
		push_warning("EconomyService.clear_campaign_gold: C# service not available")

## =============================================================================
## INTERNAL (TEST MODE)
## =============================================================================

func _update_resources(delta: Dictionary) -> void:
	if _test_repo:
		_test_repo.update_resources(delta)
	transaction_completed.emit(delta)
	_emit_current_resources()

func _emit_current_resources() -> void:
	var resources: Dictionary = get_resources()
	var gold: int = resources.get("gold", 0)
	var gems: int = resources.get("gems", 0)
	var essence: int = resources.get("essence", 0)
	var fragments: int = resources.get("fragments", 0)
	resources_changed.emit(gold, gems, essence, fragments)
