class_name PeerInfo extends RefCounted

## Data class for connected peer information
##
## Stores information about each connected player in a multiplayer match,
## including their network ID, display name, and connection quality metrics.

## Unique peer ID assigned by the multiplayer system
var peer_id: int = 0

## Display name for the peer (player name)
var display_name: String = ""

## Whether this peer is the host of the match
var is_host: bool = false

## Summoner ID being used by this peer
var summoner_id: String = ""

## Current latency to this peer in milliseconds
var latency_ms: int = 0

## Timestamp of last successful communication
var last_seen: float = 0.0

## Connection state
enum ConnectionState { CONNECTING, CONNECTED, DISCONNECTED }
var connection_state: ConnectionState = ConnectionState.CONNECTING


func _init(p_peer_id: int = 0, p_display_name: String = "") -> void:
	peer_id = p_peer_id
	display_name = p_display_name
	last_seen = Time.get_unix_time_from_system()


## Check if peer is currently connected
func is_connected_peer() -> bool:
	return connection_state == ConnectionState.CONNECTED


## Update the last seen timestamp
func update_last_seen() -> void:
	last_seen = Time.get_unix_time_from_system()


## Serialize peer info for network transmission
func serialize() -> Dictionary:
	return {
		"peer_id": peer_id,
		"display_name": display_name,
		"is_host": is_host,
		"summoner_id": summoner_id,
		"latency_ms": latency_ms,
	}


## Deserialize peer info from network data
static func deserialize(data: Dictionary) -> RefCounted:
	var script: GDScript = load("res://scripts/multiplayer/core/peer_info.gd")
	var info: RefCounted = script.new()
	info.peer_id = data.get("peer_id", 0)
	info.display_name = data.get("display_name", "")
	info.is_host = data.get("is_host", false)
	info.summoner_id = data.get("summoner_id", "")
	info.latency_ms = data.get("latency_ms", 0)
	info.connection_state = ConnectionState.CONNECTED
	return info
