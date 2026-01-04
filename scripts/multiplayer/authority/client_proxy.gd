extends "res://scripts/multiplayer/authority/authority_provider.gd"
class_name ClientProxy

## Authority provider for P2P client in multiplayer.
##
## The client:
## - Does NOT have authority over game state
## - Sends action requests to host for validation
## - Waits for host confirmation before applying actions
## - Receives state updates from host

const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")
const ActionReplicatorScript: GDScript = preload("res://scripts/multiplayer/sync/action_replicator.gd")

## Reference to the battle context
var _battle_context: Node = null

## Local peer ID (assigned by host during connection)
var _local_peer_id: int = 0

## Pending actions waiting for host confirmation
## Maps action_id -> GameAction
var _pending_actions: Dictionary = {}

## Local action ID counter
var _next_local_id: int = 1

## Reference to the action replicator node (for RPCs)
var _action_replicator: Node = null


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


## Set the action replicator node (must be in scene tree for RPCs)
func set_action_replicator(replicator: Node) -> void:
	_action_replicator = replicator

	# Connect to signals
	if _action_replicator:
		if not _action_replicator.action_confirmed.is_connected(_on_action_confirmed):
			_action_replicator.action_confirmed.connect(_on_action_confirmed)
		if not _action_replicator.action_rejected.is_connected(_on_action_rejected):
			_action_replicator.action_rejected.connect(_on_action_rejected)


## Clients cannot execute actions directly.
## This should only be called when host broadcasts confirmed actions.
## action should be a GameAction instance (RefCounted).
func execute_action(action: RefCounted) -> void:
	# Only execute actions that came from host (via RPC)
	# This is called when host broadcasts a confirmed action
	action.execute(_battle_context)
	action_confirmed.emit(action)


## Request action from host.
## action should be a GameAction instance (RefCounted).
func request_action(action: RefCounted) -> void:
	action.player_id = _local_peer_id

	# Generate local action ID for tracking
	action.action_id = _next_local_id
	_next_local_id += 1

	# Store as pending
	_pending_actions[action.action_id] = action

	# Send action request to host via RPC
	if _action_replicator:
		_action_replicator.send_action_to_host(action)
	else:
		# No replicator means we can't send - reject gracefully via signal
		_pending_actions.erase(action.action_id)
		action_rejected.emit(action, "No network connection")


## Initialize the authority provider.
func initialize() -> void:
	_pending_actions.clear()
	_next_local_id = 1


## Cleanup the authority provider.
func cleanup() -> void:
	if _action_replicator:
		if _action_replicator.action_confirmed.is_connected(_on_action_confirmed):
			_action_replicator.action_confirmed.disconnect(_on_action_confirmed)
		if _action_replicator.action_rejected.is_connected(_on_action_rejected):
			_action_replicator.action_rejected.disconnect(_on_action_rejected)
	_action_replicator = null
	_battle_context = null
	_pending_actions.clear()


## =============================================================================
## NETWORK HANDLERS
## =============================================================================

## Handle action confirmation from host via ActionReplicator
func _on_action_confirmed(action: RefCounted) -> void:
	# Remove from pending if it was our action
	if action.player_id == _local_peer_id:
		_pending_actions.erase(action.action_id)

	# Execute the confirmed action
	execute_action(action)


## Handle action rejection from host via ActionReplicator
func _on_action_rejected(action_id: int, reason: String) -> void:
	var action: RefCounted = _pending_actions.get(action_id)
	if action:
		_pending_actions.erase(action_id)
		action_rejected.emit(action, reason)
	else:
		push_warning("ClientProxy: Received rejection for unknown action %d" % action_id)
