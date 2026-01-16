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
	_cs_service = get_node_or_null("/root/EconomyServiceCS")
	if _cs_service == null:
		push_error("EconomyService: EconomyServiceCS autoload not found - C# service unavailable")
		return

	# Forward C# signals to GDScript signals
	_cs_service.ResourcesChanged.connect(_on_cs_resources_changed)
	_cs_service.TransactionCompleted.connect(_on_cs_transaction_completed)
	_cs_service.TransactionFailed.connect(_on_cs_transaction_failed)

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
