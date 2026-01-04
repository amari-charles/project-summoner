extends Node
class_name ActionReplicator

## Handles RPC replication of game actions between peers
##
## This node must be added to the scene tree for RPCs to work.
## Authority providers use this to send/receive actions over the network.

const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

## Pending action callbacks (for clients awaiting host confirmation)
## Maps action_id -> Callable
var _pending_callbacks: Dictionary = {}


## Emitted when an action is received from a remote peer
signal action_received(action: RefCounted, sender_peer_id: int)

## Emitted when host confirms an action
signal action_confirmed(action: RefCounted)

## Emitted when host rejects an action
signal action_rejected(action_id: int, reason: String)


## =============================================================================
## HOST METHODS (called by HostAuthority)
## =============================================================================

## Broadcast a confirmed action to all clients
func broadcast_action(action: RefCounted) -> void:
	if not multiplayer.is_server():
		push_error("ActionReplicator.broadcast_action() called on non-host")
		return

	var action_data: Dictionary = action.serialize()
	_receive_confirmed_action.rpc(action_data)


## Send rejection to a specific client
func send_rejection(peer_id: int, action_id: int, reason: String) -> void:
	if not multiplayer.is_server():
		push_error("ActionReplicator.send_rejection() called on non-host")
		return

	_receive_rejection.rpc_id(peer_id, action_id, reason)


## =============================================================================
## CLIENT METHODS (called by ClientProxy)
## =============================================================================

## Send an action request to the host
func send_action_to_host(action: RefCounted) -> void:
	if multiplayer.is_server():
		push_error("ActionReplicator.send_action_to_host() called on host")
		return

	var action_data: Dictionary = action.serialize()
	_receive_action_request.rpc_id(1, action_data)


## =============================================================================
## RPC METHODS
## =============================================================================

## Client -> Host: Request to execute an action
@rpc("any_peer", "call_remote", "reliable")
func _receive_action_request(action_data: Dictionary) -> void:
	var sender_id: int = multiplayer.get_remote_sender_id()
	print("ActionReplicator: Received action request from peer %d" % sender_id)

	var action: RefCounted = GameActionScript.deserialize(action_data)
	if action == null:
		push_error("ActionReplicator: Failed to deserialize action from peer %d" % sender_id)
		send_rejection(sender_id, action_data.get("action_id", 0), "Failed to deserialize action")
		return

	action.player_id = sender_id
	action_received.emit(action, sender_id)


## Host -> Client: Confirmed action broadcast
@rpc("authority", "call_local", "reliable")
func _receive_confirmed_action(action_data: Dictionary) -> void:
	var action: RefCounted = GameActionScript.deserialize(action_data)
	if action == null:
		push_error("ActionReplicator: Failed to deserialize confirmed action")
		return

	action_confirmed.emit(action)


## Host -> Client: Action rejection
@rpc("authority", "call_remote", "reliable")
func _receive_rejection(action_id: int, reason: String) -> void:
	print("ActionReplicator: Action %d rejected: %s" % [action_id, reason])
	action_rejected.emit(action_id, reason)
