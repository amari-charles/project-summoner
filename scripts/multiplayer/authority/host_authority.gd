extends "res://scripts/multiplayer/authority/authority_provider.gd"
class_name HostAuthority

## Authority provider for P2P host in multiplayer.
##
## The host:
## - Has authority over game state
## - Validates all actions from clients
## - Executes actions and broadcasts results to clients
## - Generates and shares the battle seed
##
## STUB: Full implementation in Phase 1.4 (P2P Connection)

## Preload GameAction for type hints
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

## Reference to the battle context
var _battle_context: Node = null

## Local peer ID (host is always peer 1 in Godot's multiplayer)
var _local_peer_id: int = 1

## Action sequence counter
var _next_action_id: int = 1


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


## Execute action with validation and broadcast.
## STUB: Currently just executes locally like LocalAuthority.
## TODO (Phase 1.4): Add RPC broadcast to clients.
## action should be a GameAction instance (RefCounted).
func execute_action(action: RefCounted) -> void:
	# Assign action ID if not set
	if action.action_id == 0:
		action.action_id = _next_action_id
		_next_action_id += 1

	# Validate action
	if not action.validate(_battle_context):
		action_rejected.emit(action, "Action validation failed")
		# TODO (Phase 1.4): Notify originating client of rejection
		return

	# Execute action locally
	action.execute(_battle_context)

	# Confirm action
	action_confirmed.emit(action)

	# TODO (Phase 1.4): Broadcast action to all clients via RPC
	# _broadcast_action_to_clients(action)


## Request action (for host, same as execute since we have authority).
## action should be a GameAction instance (RefCounted).
func request_action(action: RefCounted) -> void:
	action.player_id = _local_peer_id
	execute_action(action)


## Initialize the authority provider.
func initialize() -> void:
	_next_action_id = 1
	# TODO (Phase 1.4): Set up RPC handlers


## Cleanup the authority provider.
func cleanup() -> void:
	_battle_context = null
	# TODO (Phase 1.4): Clean up network connections


## =============================================================================
## NETWORK METHODS (STUBS - Implement in Phase 1.4)
## =============================================================================

## Handle action request from a client via RPC.
## Called when client sends an action for validation.
func _on_client_action_request(_action_data: Dictionary, _sender_peer_id: int) -> void:
	# TODO (Phase 1.4): Deserialize action, validate, execute, broadcast
	push_warning("HostAuthority._on_client_action_request() not yet implemented")


## Broadcast an executed action to all clients via RPC.
## action should be a GameAction instance (RefCounted).
func _broadcast_action_to_clients(_action: RefCounted) -> void:
	# TODO (Phase 1.4): Serialize action and send via RPC
	push_warning("HostAuthority._broadcast_action_to_clients() not yet implemented")
