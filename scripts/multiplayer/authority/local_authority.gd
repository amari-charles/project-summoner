extends "res://scripts/multiplayer/authority/authority_provider.gd"
class_name LocalAuthority

## Authority provider for single-player (offline) mode.
##
## In single-player:
## - Local player always has authority
## - All actions execute immediately
## - No network validation or replication needed
## - This is the current behavior wrapped in the authority abstraction

## Preload GameAction for type hints
const GameActionScript: GDScript = preload("res://scripts/multiplayer/actions/game_action.gd")

## Constants for single-player
const LOCAL_PLAYER_ID: int = 0

## Reference to the battle context (for action execution)
var _battle_context: Node = null

## Action sequence counter
var _next_action_id: int = 1


func _init(battle_context: Node = null) -> void:
	_battle_context = battle_context


## In single-player, local always has authority.
func has_authority() -> bool:
	return true


## Single-player is not multiplayer.
func is_multiplayer() -> bool:
	return false


## In single-player, all actions are local.
func is_local_action(_player_id: int) -> bool:
	return true


## In single-player, we use a constant peer ID.
func get_local_peer_id() -> int:
	return LOCAL_PLAYER_ID


## Execute action immediately (single-player has no validation delay).
## action should be a GameAction instance (RefCounted).
func execute_action(action: RefCounted) -> void:
	# Assign action ID if not set
	if action.action_id == 0:
		action.action_id = _next_action_id
		_next_action_id += 1

	# Assign player ID
	action.player_id = LOCAL_PLAYER_ID

	# Validate action
	if not action.validate(_battle_context):
		action_rejected.emit(action, "Action validation failed")
		return

	# Execute action
	action.execute(_battle_context)

	# Confirm action (for consistency with multiplayer flow)
	action_confirmed.emit(action)


## In single-player, request_action just calls execute_action directly.
## action should be a GameAction instance (RefCounted).
func request_action(action: RefCounted) -> void:
	# No network delay - execute immediately
	execute_action(action)


## Initialize the authority provider.
func initialize() -> void:
	_next_action_id = 1


## Cleanup the authority provider.
func cleanup() -> void:
	_battle_context = null
