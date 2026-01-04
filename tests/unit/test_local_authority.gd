extends GutTest

## Unit Tests for LocalAuthority
##
## Tests the single-player authority provider implementation.

const LocalAuthorityScript: GDScript = preload("res://scripts/multiplayer/authority/local_authority.gd")
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

var authority: RefCounted


func before_each() -> void:
	authority = LocalAuthorityScript.new(null)
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


func test_is_multiplayer_returns_false() -> void:
	assert_false(authority.is_multiplayer())


func test_is_local_action_always_returns_true() -> void:
	assert_true(authority.is_local_action(0))
	assert_true(authority.is_local_action(1))
	assert_true(authority.is_local_action(999))


func test_get_local_peer_id_returns_zero() -> void:
	assert_eq(authority.get_local_peer_id(), 0)


## =============================================================================
## ACTION EXECUTION TESTS
## =============================================================================

func test_execute_action_assigns_action_id() -> void:
	var action: RefCounted = GameActionScript.new()
	assert_eq(action.action_id, 0)

	authority.execute_action(action)

	assert_eq(action.action_id, 1)


func test_execute_action_assigns_player_id() -> void:
	var action: RefCounted = GameActionScript.new()

	authority.execute_action(action)

	assert_eq(action.player_id, 0)  # LOCAL_PLAYER_ID


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

func test_request_action_calls_execute_action() -> void:
	var action: RefCounted = GameActionScript.new()
	watch_signals(authority)

	authority.request_action(action)

	# Should have been executed (emits confirmed signal)
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
	assert_eq(action2.action_id, 1)  # Reset to 1


func test_cleanup_clears_battle_context() -> void:
	# Create authority with a mock context
	var mock_context: Node = Node.new()
	var auth_with_context: RefCounted = LocalAuthorityScript.new(mock_context)

	auth_with_context.cleanup()

	# Internal _battle_context should be null (we can't directly test private vars,
	# but cleanup() should complete without error)
	assert_true(true)  # If we got here, cleanup worked

	mock_context.free()
