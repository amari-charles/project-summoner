extends GutTest

## Unit Tests for HostAuthority
##
## Tests the host authority provider for P2P multiplayer.

const HostAuthorityScript: GDScript = preload("res://scripts/multiplayer/authority/host_authority.gd")
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

var authority: RefCounted


func before_each() -> void:
	authority = HostAuthorityScript.new(null)
	authority.initialize()


func after_each() -> void:
	if authority:
		authority.cleanup()
		authority = null


## =============================================================================
## BASIC PROPERTY TESTS
## =============================================================================

func test_has_authority_returns_true() -> void:
	assert_true(authority.has_authority())


func test_is_multiplayer_returns_true() -> void:
	assert_true(authority.is_multiplayer())


func test_get_local_peer_id_returns_one() -> void:
	# Host is always peer 1 in Godot multiplayer
	assert_eq(authority.get_local_peer_id(), 1)


func test_is_local_action_true_for_host() -> void:
	assert_true(authority.is_local_action(1))


func test_is_local_action_false_for_client() -> void:
	assert_false(authority.is_local_action(2))
	assert_false(authority.is_local_action(99))


## =============================================================================
## ACTION EXECUTION TESTS
## =============================================================================

func test_execute_action_assigns_action_id() -> void:
	var action: RefCounted = GameActionScript.new()
	assert_eq(action.action_id, 0)

	authority.execute_action(action)

	assert_eq(action.action_id, 1)


func test_execute_action_increments_action_id() -> void:
	var action1: RefCounted = GameActionScript.new()
	var action2: RefCounted = GameActionScript.new()
	var action3: RefCounted = GameActionScript.new()

	authority.execute_action(action1)
	authority.execute_action(action2)
	authority.execute_action(action3)

	assert_eq(action1.action_id, 1)
	assert_eq(action2.action_id, 2)
	assert_eq(action3.action_id, 3)


func test_execute_action_emits_action_confirmed() -> void:
	var action: RefCounted = GameActionScript.new()
	watch_signals(authority)

	authority.execute_action(action)

	assert_signal_emitted(authority, "action_confirmed")


func test_execute_action_preserves_existing_action_id() -> void:
	var action: RefCounted = GameActionScript.new()
	action.action_id = 42

	authority.execute_action(action)

	assert_eq(action.action_id, 42)


## =============================================================================
## REQUEST ACTION TESTS
## =============================================================================

func test_request_action_sets_player_id_to_host() -> void:
	var action: RefCounted = GameActionScript.new()

	authority.request_action(action)

	assert_eq(action.player_id, 1)  # Host peer ID


func test_request_action_executes_action() -> void:
	var action: RefCounted = GameActionScript.new()
	watch_signals(authority)

	authority.request_action(action)

	assert_signal_emitted(authority, "action_confirmed")
	assert_eq(action.action_id, 1)


## =============================================================================
## INITIALIZATION TESTS
## =============================================================================

func test_initialize_resets_action_counter() -> void:
	var action1: RefCounted = GameActionScript.new()
	authority.execute_action(action1)
	assert_eq(action1.action_id, 1)

	authority.initialize()

	var action2: RefCounted = GameActionScript.new()
	authority.execute_action(action2)
	assert_eq(action2.action_id, 1)


## =============================================================================
## ACTION REPLICATOR TESTS
## =============================================================================

func test_set_action_replicator_without_replicator() -> void:
	# Should not crash when no replicator is set
	var action: RefCounted = GameActionScript.new()
	authority.execute_action(action)

	# Action still executed locally
	assert_eq(action.action_id, 1)
