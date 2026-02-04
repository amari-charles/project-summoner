class_name AuthorityProvider extends RefCounted

## Abstract base class for authority management in multiplayer
##
## Provides an abstraction that allows the same game code to work under
## different authority models:
## - LocalAuthority: Single-player (all actions immediate, no network)
## - MultiplayerAuthority: Online multiplayer (bridges to C# MatchSession)

## Emitted when authority confirms an action was executed
## action is a GameAction (RefCounted)
signal action_confirmed(action: RefCounted)

## Emitted when authority rejects an action (with reason)
## action is a GameAction (RefCounted)
signal action_rejected(action: RefCounted, reason: String)

## Emitted when a state change is received from authority (for clients)
signal state_update_received(state_data: Dictionary)


## Returns true if this peer has authority over game state changes.
## Only the authority (host/server in MP, always true in SP) can make state changes.
func has_authority() -> bool:
	return false  # Override in subclasses


## Returns true if this is a multiplayer context (vs single-player).
func is_multiplayer() -> bool:
	return false  # Override in subclasses


## Returns true if this action belongs to the local player.
## Used to determine if we should process input for this action.
func is_local_action(_player_id: int) -> bool:
	return false  # Override in subclasses


## Returns the local player's peer ID.
## In single-player, this is always 0 (or a constant).
func get_local_peer_id() -> int:
	return 0  # Override in subclasses


## Execute an action with authority (host/server only).
## This is called by the authority to actually perform the action.
## In single-player, this executes immediately.
## In multiplayer, the host validates and broadcasts.
## action should be a GameAction instance (RefCounted).
func execute_action(_action: RefCounted) -> void:
	push_error("AuthorityProvider.execute_action() must be overridden")


## Request an action (client sends to authority for validation).
## In single-player, this directly calls execute_action.
## In multiplayer, client sends request to host and waits for confirmation.
## action should be a GameAction instance (RefCounted).
func request_action(_action: RefCounted) -> void:
	push_error("AuthorityProvider.request_action() must be overridden")


## Initialize the authority provider.
## Called when battle starts.
func initialize() -> void:
	pass  # Override in subclasses if needed


## Cleanup the authority provider.
## Called when battle ends.
func cleanup() -> void:
	pass  # Override in subclasses if needed


## Get the C# MatchSession node (multiplayer only).
## Returns null for single-player (LocalAuthority).
func get_match_session() -> Node:
	return null  # Override in MultiplayerAuthority
