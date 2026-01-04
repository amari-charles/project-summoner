extends "res://scripts/multiplayer/authority/authority_provider.gd"
class_name HostAuthority

## Authority provider for P2P host in multiplayer.
##
## The host:
## - Has authority over game state
## - Validates all actions from clients
## - Executes actions and broadcasts results to clients
## - Generates and shares the battle seed

const ActionReplicatorScript: GDScript = preload("res://scripts/multiplayer/sync/action_replicator.gd")

## Reference to the battle context
var _battle_context: Node = null

## Local peer ID (host is always peer 1 in Godot's multiplayer)
var _local_peer_id: int = 1

## Action sequence counter
var _next_action_id: int = 1

## Reference to the action replicator node (for RPCs)
var _action_replicator: Node = null


func _init(battle_context: Node = null) -> void:
	_battle_context = battle_context


## Host always has authority.
func has_authority() -> bool:
	return true


## This is multiplayer mode.
func is_multiplayer() -> bool:
	return true


## Check if action is from local player (the host).
func is_local_action(player_id: int) -> bool:
	return player_id == _local_peer_id


## Host's peer ID.
func get_local_peer_id() -> int:
	return _local_peer_id


## Set the action replicator node (must be in scene tree for RPCs)
func set_action_replicator(replicator: Node) -> void:
	_action_replicator = replicator

	# Connect to action received signal to handle client requests
	if _action_replicator and not _action_replicator.action_received.is_connected(_on_client_action_received):
		_action_replicator.action_received.connect(_on_client_action_received)


## Execute action with validation and broadcast.
## action should be a GameAction instance (RefCounted).
func execute_action(action: RefCounted) -> void:
	# Assign action ID if not set
	if action.action_id == 0:
		action.action_id = _next_action_id
		_next_action_id += 1

	# Validate action
	if not action.validate(_battle_context):
		action_rejected.emit(action, "Action validation failed")
		# Notify originating client of rejection if from network
		if _action_replicator and action.player_id != _local_peer_id:
			_action_replicator.send_rejection(action.player_id, action.action_id, "Validation failed")
		return

	# Execute action locally
	action.execute(_battle_context)

	# Confirm action
	action_confirmed.emit(action)

	# Broadcast action to all clients via RPC
	if _action_replicator:
		_action_replicator.broadcast_action(action)


## Request action (for host, same as execute since we have authority).
## action should be a GameAction instance (RefCounted).
func request_action(action: RefCounted) -> void:
	action.player_id = _local_peer_id
	execute_action(action)


## Initialize the authority provider.
func initialize() -> void:
	_next_action_id = 1


## Cleanup the authority provider.
func cleanup() -> void:
	if _action_replicator and _action_replicator.action_received.is_connected(_on_client_action_received):
		_action_replicator.action_received.disconnect(_on_client_action_received)
	_action_replicator = null
	_battle_context = null


## =============================================================================
## NETWORK HANDLERS
## =============================================================================

## Handle action request received from a client via ActionReplicator
func _on_client_action_received(action: RefCounted, sender_peer_id: int) -> void:
	action.player_id = sender_peer_id
	execute_action(action)
