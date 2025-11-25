extends Node
# EconomyService is registered as autoload "Economy", no class_name needed

## Economy Service - Resource Management
##
## Handles all resource operations (gold, essence, fragments).
## UI and gameplay code should call this, never the repository directly.
##
## Usage:
##   Economy.add_gold(50)
##   if Economy.can_afford({"gold": 100}):
##       Economy.spend({"gold": 100})
##
## Emits signals for reactive UI updates.

## Signals
signal resources_changed(gold: int, essence: int, fragments: int)
signal transaction_completed(delta: Dictionary)
signal transaction_failed(reason: String)

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("EconomyService: Initializing...")

	# Connect to repo signals for reactive updates
	ProfileRepo.data_changed.connect(_on_repo_data_changed)

	print("EconomyService: Ready")

	# Emit initial state
	_emit_current_resources()

## =============================================================================
## RESOURCE QUERIES
## =============================================================================

## Get current resource values
func get_resources() -> Dictionary:
	return ProfileRepo.get_resources()

## Get specific resource amount
func get_gold() -> int:
	return get_resources().get("gold", 0)

func get_essence() -> int:
	return get_resources().get("essence", 0)

func get_fragments() -> int:
	return get_resources().get("fragments", 0)

## Check if player can afford a cost
## cost: Dictionary like {"gold": 100, "essence": 50}
func can_afford(cost: Dictionary) -> bool:
	var resources: Dictionary = get_resources()

	for key: String in cost:
		var required: int = cost[key] if cost[key] is int else 0
		if resources.get(key, 0) < required:
			return false

	return true

## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

## Add gold (positive amount only)
func add_gold(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_gold called with non-positive amount: %d" % amount)
		return

	_update_resources({"gold": amount})
	print("EconomyService: Added %d gold" % amount)

## Add essence (positive amount only)
func add_essence(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_essence called with non-positive amount: %d" % amount)
		return

	_update_resources({"essence": amount})
	print("EconomyService: Added %d essence" % amount)

## Add fragments (positive amount only)
func add_fragments(amount: int) -> void:
	if amount <= 0:
		push_warning("EconomyService: add_fragments called with non-positive amount: %d" % amount)
		return

	_update_resources({"fragments": amount})
	print("EconomyService: Added %d fragments" % amount)

## Spend resources (negative delta)
## Returns true if successful, false if can't afford
func spend(cost: Dictionary) -> bool:
	if not can_afford(cost):
		var reason: String = "Cannot afford: " + str(cost)
		push_warning("EconomyService: " + reason)
		transaction_failed.emit(reason)
		return false

	# Convert to negative delta
	var delta: Dictionary = {}
	for key: String in cost:
		var amount: int = cost[key] if cost[key] is int else 0
		delta[key] = -amount

	_update_resources(delta)
	print("EconomyService: Spent %s" % str(cost))
	return true

## Grant multiple resources at once (for rewards, etc.)
## rewards: Dictionary like {"gold": 100, "essence": 50}
func grant_rewards(rewards: Dictionary) -> void:
	_update_resources(rewards)
	print("EconomyService: Granted rewards: %s" % str(rewards))

## =============================================================================
## INTERNAL
## =============================================================================

func _update_resources(delta: Dictionary) -> void:
	ProfileRepo.update_resources(delta)
	transaction_completed.emit(delta)
	_emit_current_resources()

func _emit_current_resources() -> void:
	var resources: Dictionary = get_resources()
	var gold: int = resources.get("gold", 0)
	var essence: int = resources.get("essence", 0)
	var fragments: int = resources.get("fragments", 0)
	resources_changed.emit(gold, essence, fragments)

func _on_repo_data_changed() -> void:
	# Repo data changed (from external source or load)
	_emit_current_resources()
