extends GutTest

## Unit Tests for EconomyService
##
## Tests resource management: adding, spending, checking affordability.
## Uses MockProfileRepo for isolation from disk I/O.

var economy: Node  # EconomyService instance
var mock_repo: MockProfileRepo

# Signal capture variables (class-level for lambda access)
var _signal_received: bool = false
var _received_delta: Dictionary = {}
var _received_gold: int = 0
var _received_reason: String = ""
var _signal_count: int = 0


func before_each() -> void:
	# Reset signal capture state
	_signal_received = false
	_received_delta = {}
	_received_gold = 0
	_received_reason = ""
	_signal_count = 0

	# Create fresh mock and service for each test
	mock_repo = MockProfileRepo.new()
	mock_repo.set_resources({"gold": 100, "essence": 50, "fragments": 10})

	# Load the EconomyService script and create instance
	var EconomyServiceScript: GDScript = load("res://scripts/services/economy_service.gd")
	economy = EconomyServiceScript.new()
	economy.init_for_testing(mock_repo)


func after_each() -> void:
	if economy:
		economy.free()
	if mock_repo:
		mock_repo.free()


## =============================================================================
## RESOURCE QUERY TESTS
## =============================================================================

func test_get_resources_returns_current_values() -> void:
	var resources: Dictionary = economy.get_resources()

	assert_eq(resources.get("gold"), 100)
	assert_eq(resources.get("essence"), 50)
	assert_eq(resources.get("fragments"), 10)


func test_get_gold_returns_gold_amount() -> void:
	assert_eq(economy.get_gold(), 100)


func test_get_essence_returns_essence_amount() -> void:
	assert_eq(economy.get_essence(), 50)


func test_get_fragments_returns_fragments_amount() -> void:
	assert_eq(economy.get_fragments(), 10)


func test_get_gold_returns_zero_when_not_set() -> void:
	mock_repo.set_resources({})
	assert_eq(economy.get_gold(), 0)


## =============================================================================
## CAN_AFFORD TESTS
## =============================================================================

func test_can_afford_returns_true_when_sufficient() -> void:
	assert_true(economy.can_afford({"gold": 50}))
	assert_true(economy.can_afford({"gold": 100}))  # exactly enough
	assert_true(economy.can_afford({"gold": 50, "essence": 50}))


func test_can_afford_returns_false_when_insufficient() -> void:
	assert_false(economy.can_afford({"gold": 150}))
	assert_false(economy.can_afford({"gold": 100, "essence": 100}))


func test_can_afford_returns_true_for_empty_cost() -> void:
	assert_true(economy.can_afford({}))


func test_can_afford_handles_missing_resource_type() -> void:
	# Player has no "gems" resource
	assert_false(economy.can_afford({"gems": 10}))


## =============================================================================
## ADD RESOURCE TESTS
## =============================================================================

func test_add_gold_increases_gold() -> void:
	economy.add_gold(50)

	assert_eq(economy.get_gold(), 150)


func test_add_gold_calls_profile_repo_update() -> void:
	economy.add_gold(50)

	assert_eq(mock_repo.get_call_count("update_resources"), 1)
	var args: Array = mock_repo.get_call_args("update_resources")
	assert_eq(args[0][0], {"gold": 50})


func test_add_gold_ignores_zero_or_negative() -> void:
	economy.add_gold(0)
	economy.add_gold(-10)

	assert_eq(economy.get_gold(), 100)  # unchanged
	assert_eq(mock_repo.get_call_count("update_resources"), 0)
	# Expect 2 push_warnings about non-positive amounts
	assert_engine_error(2)


func test_add_essence_increases_essence() -> void:
	economy.add_essence(25)

	assert_eq(economy.get_essence(), 75)


func test_add_fragments_increases_fragments() -> void:
	economy.add_fragments(5)

	assert_eq(economy.get_fragments(), 15)


## =============================================================================
## SPEND TESTS
## =============================================================================

func test_spend_decreases_resources() -> void:
	var result: bool = economy.spend({"gold": 30})

	assert_true(result)
	assert_eq(economy.get_gold(), 70)


func test_spend_returns_false_when_cannot_afford() -> void:
	var result: bool = economy.spend({"gold": 500})

	assert_false(result)
	assert_eq(economy.get_gold(), 100)  # unchanged
	# Expect the push_warning about cannot afford
	assert_engine_error("Cannot afford")


func test_spend_multiple_resources() -> void:
	var result: bool = economy.spend({"gold": 50, "essence": 25})

	assert_true(result)
	assert_eq(economy.get_gold(), 50)
	assert_eq(economy.get_essence(), 25)


func test_spend_calls_profile_repo_with_negative_delta() -> void:
	economy.spend({"gold": 30})

	var args: Array = mock_repo.get_call_args("update_resources")
	assert_eq(args[0][0], {"gold": -30})


## =============================================================================
## GRANT_REWARDS TESTS
## =============================================================================

func test_grant_rewards_adds_multiple_resources() -> void:
	economy.grant_rewards({"gold": 100, "essence": 50})

	assert_eq(economy.get_gold(), 200)
	assert_eq(economy.get_essence(), 100)


## =============================================================================
## SIGNAL TESTS
## =============================================================================

func _on_transaction_completed(delta: Dictionary) -> void:
	_signal_received = true
	_received_delta = delta


func test_add_gold_emits_transaction_completed() -> void:
	economy.transaction_completed.connect(_on_transaction_completed)

	economy.add_gold(50)

	assert_true(_signal_received)
	assert_eq(_received_delta, {"gold": 50})


func _on_resources_changed(gold: int, _gems: int, _essence: int, _fragments: int) -> void:
	_signal_received = true
	_received_gold = gold
	_signal_count += 1


func test_add_gold_emits_resources_changed() -> void:
	economy.resources_changed.connect(_on_resources_changed)

	economy.add_gold(50)

	assert_true(_signal_received)
	assert_eq(_received_gold, 150)


func _on_transaction_failed(reason: String) -> void:
	_signal_received = true
	_received_reason = reason


func test_spend_failure_emits_transaction_failed() -> void:
	economy.transaction_failed.connect(_on_transaction_failed)

	economy.spend({"gold": 500})

	assert_true(_signal_received)
	assert_true(_received_reason.contains("Cannot afford"))
	# Expect the push_warning about cannot afford
	assert_engine_error("Cannot afford")


func test_repo_data_changed_triggers_resources_changed_signal() -> void:
	economy.resources_changed.connect(_on_resources_changed)

	# Simulate external data change
	mock_repo.data_changed.emit()

	assert_eq(_signal_count, 1)
