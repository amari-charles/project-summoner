extends "res://scripts/multiplayer/authority/authority_provider.gd"
class_name ClientProxy

## Authority provider for P2P client in multiplayer.
##
## The client:
## - Does NOT have authority over game state
## - Sends action requests to host for validation
## - Waits for host confirmation before applying actions
## - Receives state updates from host
##
## STUB: Full implementation in Phase 1.4 (P2P Connection)

## Preload GameAction for type hints
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

## Reference to the battle context
var _battle_context: Node = null

## Local peer ID (assigned by host during connection)
var _local_peer_id: int = 0

## Pending actions waiting for host confirmation
## Maps action_id -> GameAction
var _pending_actions: Dictionary = {}


func _init(battle_context: Node = null, peer_id: int = 0) -> void:
	_battle_context = battle_context
	_local_peer_id = peer_id


## Client never has authority (host does).
func has_authority() -> bool:
	return false


## This is multiplayer mode.
func is_multiplayer() -> bool:
	return true


## Check if action is from local player.
func is_local_action(player_id: int) -> bool:
	return player_id == _local_peer_id


## Client's assigned peer ID.
func get_local_peer_id() -> int:
	return _local_peer_id


## Set peer ID (called after connecting to host).
func set_peer_id(peer_id: int) -> void:
	_local_peer_id = peer_id


## Clients cannot execute actions directly.
## This should only be called when host broadcasts confirmed actions.
## action should be a GameAction instance (RefCounted).
func execute_action(action: RefCounted) -> void:
	# Only execute actions that came from host (via RPC)
	# This is called when host broadcasts a confirmed action
	action.execute(_battle_context)
	action_confirmed.emit(action)


## Request action from host.
## STUB: Currently does nothing.
## TODO (Phase 1.4): Send action to host via RPC.
## action should be a GameAction instance (RefCounted).
func request_action(action: RefCounted) -> void:
	action.player_id = _local_peer_id

	# Generate local action ID for tracking
	var local_id: int = _pending_actions.size() + 1
	action.action_id = local_id

	# Store as pending
	_pending_actions[local_id] = action

	# TODO (Phase 1.4): Send action request to host via RPC
	# _send_action_to_host(action)
	push_warning("ClientProxy.request_action() not yet implemented - action not sent to host")


## Initialize the authority provider.
func initialize() -> void:
	_pending_actions.clear()
	# TODO (Phase 1.4): Set up RPC handlers


## Cleanup the authority provider.
func cleanup() -> void:
	_battle_context = null
	_pending_actions.clear()
	# TODO (Phase 1.4): Clean up network connections


## =============================================================================
## NETWORK METHODS (STUBS - Implement in Phase 1.4)
## =============================================================================

## Send action request to host via RPC.
## action should be a GameAction instance (RefCounted).
func _send_action_to_host(_action: RefCounted) -> void:
	# TODO (Phase 1.4): Serialize action and send via RPC to host
	push_warning("ClientProxy._send_action_to_host() not yet implemented")


## Handle action confirmation from host via RPC.
## Called when host broadcasts a confirmed action.
func _on_action_confirmed_from_host(action_data: Dictionary) -> void:
	var action: RefCounted = GameActionScript.deserialize(action_data)
	if action == null:
		push_error("ClientProxy: Failed to deserialize confirmed action")
		return

	# Remove from pending if it was our action
	if action.player_id == _local_peer_id:
		_pending_actions.erase(action.action_id)

	# Execute the confirmed action
	execute_action(action)


## Handle action rejection from host via RPC.
func _on_action_rejected_from_host(action_id: int, reason: String) -> void:
	var action: RefCounted = _pending_actions.get(action_id)
	if action:
		_pending_actions.erase(action_id)
		action_rejected.emit(action, reason)
	else:
		push_warning("ClientProxy: Received rejection for unknown action %d" % action_id)


## Handle state update from host (for resync).
func _on_state_update_from_host(state_data: Dictionary) -> void:
	state_update_received.emit(state_data)
