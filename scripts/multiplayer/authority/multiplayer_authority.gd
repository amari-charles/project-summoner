class_name MultiplayerAuthority extends RefCounted

## Authority provider for multiplayer battles.
## Bridges BattleContext authority checks with the C# MatchSession system.
##
## In multiplayer:
## - Host has authority (validates actions, runs authoritative sim)
## - Client does not have authority (predicts, waits for confirmation)

## Reference to the MatchSession node (C#)
var _match_session: Node = null

## Whether this player is the host
var _is_host: bool = false

## Local player's peer ID
var _local_peer_id: int = 0

## Local player's index (0 or 1)
var _local_player_index: int = 0


func _init(match_session: Node, is_host: bool, local_peer_id: int, local_player_index: int) -> void:
	_match_session = match_session
	_is_host = is_host
	_local_peer_id = local_peer_id
	_local_player_index = local_player_index


## Initialize the authority provider
func initialize() -> void:
	print("MultiplayerAuthority: Initialized (host: %s, peer: %d, player: %d)" % [
		_is_host, _local_peer_id, _local_player_index
	])


## Cleanup when battle ends
func cleanup() -> void:
	_match_session = null
	print("MultiplayerAuthority: Cleaned up")


## Check if this peer has authority over game state.
## Only the host has authority in multiplayer.
func has_authority() -> bool:
	return _is_host


## Check if this is a multiplayer battle.
func is_multiplayer() -> bool:
	return true


## Get the local peer ID.
func get_local_peer_id() -> int:
	return _local_peer_id


## Get the local player index (0 or 1).
func get_local_player_index() -> int:
	return _local_player_index


## Get the MatchSession node.
func get_match_session() -> Node:
	return _match_session


## Request to play a card (routes through MatchSession).
## Returns true if request was sent, false if invalid.
func request_card_play(card_index: int, position: Vector3) -> bool:
	if _match_session == null:
		push_error("MultiplayerAuthority: MatchSession not available")
		return false

	# Call MatchSession.RequestCardPlay() in C#
	_match_session.RequestCardPlay(card_index, position)
	return true


## Request to forfeit the match.
func request_forfeit() -> void:
	if _match_session == null:
		push_error("MultiplayerAuthority: MatchSession not available")
		return

	_match_session.RequestForfeit()
