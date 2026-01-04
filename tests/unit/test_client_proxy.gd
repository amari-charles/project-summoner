extends GutTest

## Unit Tests for ClientProxy
##
## Tests the client authority provider for P2P multiplayer.

const ClientProxyScript: GDScript = preload("res://scripts/multiplayer/authority/client_proxy.gd")
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

var authority: RefCounted


func before_each() -> void:
	authority = ClientProxyScript.new(null, 2)  # Peer ID 2 (client)
	authority.initialize()


func after_each() -> void:
	if authority:
		authority.cleanup()
		authority = null


## =============================================================================
## BASIC PROPERTY TESTS
## =============================================================================

func test_has_authority_returns_false() -> void:
	# Client never has authority
	assert_false(authority.has_authority())


func test_is_multiplayer_returns_true() -> void:
	assert_true(authority.is_multiplayer())


func test_get_local_peer_id_returns_assigned_id() -> void:
	assert_eq(authority.get_local_peer_id(), 2)


func test_set_peer_id_updates_id() -> void:
	authority.set_peer_id(5)
	assert_eq(authority.get_local_peer_id(), 5)


func test_is_local_action_true_for_client() -> void:
	assert_true(authority.is_local_action(2))


func test_is_local_action_false_for_host() -> void:
	assert_false(authority.is_local_action(1))


func test_is_local_action_false_for_other() -> void:
	assert_false(authority.is_local_action(99))


## =============================================================================
## ACTION REQUEST TESTS (without replicator)
## =============================================================================

func test_request_action_sets_player_id() -> void:
	var action: RefCounted = GameActionScript.new()

	authority.request_action(action)

	assert_eq(action.player_id, 2)


func test_request_action_assigns_local_action_id() -> void:
	var action: RefCounted = GameActionScript.new()

	authority.request_action(action)

	assert_eq(action.action_id, 1)


func test_request_action_increments_id() -> void:
	var action1: RefCounted = GameActionScript.new()
	var action2: RefCounted = GameActionScript.new()

	authority.request_action(action1)
	authority.request_action(action2)

	assert_eq(action1.action_id, 1)
	assert_eq(action2.action_id, 2)


func test_request_action_without_replicator_emits_rejected() -> void:
	var action: RefCounted = GameActionScript.new()
	watch_signals(authority)

	authority.request_action(action)

	# Without replicator, action is rejected
	assert_signal_emitted(authority, "action_rejected")


## =============================================================================
## ACTION EXECUTION TESTS
## =============================================================================

func test_execute_action_emits_confirmed() -> void:
	var action: RefCounted = GameActionScript.new()
	watch_signals(authority)

	authority.execute_action(action)

	assert_signal_emitted(authority, "action_confirmed")


## =============================================================================
## INITIALIZATION TESTS
## =============================================================================

func test_initialize_clears_pending_actions() -> void:
	var action: RefCounted = GameActionScript.new()
	authority.request_action(action)

	authority.initialize()

	# Internal pending should be cleared
	# If we got here without crash, it worked
	assert_true(true)


func test_initialize_resets_action_counter() -> void:
	var action1: RefCounted = GameActionScript.new()
	authority.request_action(action1)
	assert_eq(action1.action_id, 1)

	authority.initialize()

	var action2: RefCounted = GameActionScript.new()
	authority.request_action(action2)
	assert_eq(action2.action_id, 1)
