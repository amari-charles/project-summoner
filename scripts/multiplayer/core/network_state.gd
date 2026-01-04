extends Node
class_name NetworkStateClass

## Autoload singleton tracking multiplayer connection state
##
## Manages the current connection status, connected peers, and match configuration.
## Acts as a central hub for multiplayer state that other systems can query.

const PeerInfoScript: GDScript = preload("res://scripts/multiplayer/core/peer_info.gd")
const MatchConfigScript: GDScript = preload("res://scripts/multiplayer/core/match_config.gd")

## Connection state enum
enum ConnectionState {
	OFFLINE,       ## Not connected to any network
	CONNECTING,    ## Attempting to connect
	CONNECTED,     ## Connected to a peer/server
	DISCONNECTED,  ## Was connected but lost connection
}

## Current connection state
var state: ConnectionState = ConnectionState.OFFLINE

## Local peer ID (assigned by multiplayer system)
var local_peer_id: int = 0

## Connected peers (peer_id -> PeerInfo)
var peers: Dictionary = {}

## Current match configuration (null if not in a match)
var match_config: RefCounted = null

## Room code for P2P connection
var room_code: String = ""

## Average latency to host/server in milliseconds
var latency_ms: int = 0


## Emitted when connection state changes
signal connection_state_changed(new_state: ConnectionState)

## Emitted when a peer connects
signal peer_connected(peer_info: RefCounted)

## Emitted when a peer disconnects
signal peer_disconnected(peer_id: int)

## Emitted when match is about to start
signal match_starting(config: RefCounted)

## Emitted when match has started
signal match_started(config: RefCounted)

## Emitted when match ends
signal match_ended(winner_peer_id: int, reason: String)

## Emitted when latency measurement updates
signal latency_updated(peer_id: int, latency_ms: int)


func _ready() -> void:
	# Connect to Godot's multiplayer signals
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)


## Check if we are the host
func is_host() -> bool:
	return multiplayer.is_server()


## Check if we are online (connected)
func is_online() -> bool:
	return state == ConnectionState.CONNECTED


## Check if we are currently in a match
func in_match() -> bool:
	return match_config != null


## Get our local peer info
func get_local_peer_info() -> RefCounted:
	return peers.get(local_peer_id)


## Get peer info by ID
func get_peer_info(peer_id: int) -> RefCounted:
	return peers.get(peer_id)


## Get opponent's peer info (in a 2-player match)
func get_opponent_peer_info() -> RefCounted:
	for peer_id: int in peers:
		if peer_id != local_peer_id:
			return peers[peer_id]
	return null


## Get all connected peer IDs
func get_connected_peer_ids() -> Array[int]:
	var result: Array[int] = []
	for peer_id: int in peers:
		var info: RefCounted = peers[peer_id]
		if info.is_connected_peer():
			result.append(peer_id)
	return result


## =============================================================================
## STATE MANAGEMENT
## =============================================================================

## Set connection state and emit signal
func set_state(new_state: ConnectionState) -> void:
	if state != new_state:
		state = new_state
		connection_state_changed.emit(new_state)


## Register a peer (called when connection is established)
func register_peer(peer_info: RefCounted) -> void:
	peers[peer_info.peer_id] = peer_info
	peer_connected.emit(peer_info)


## Unregister a peer (called when disconnected)
func unregister_peer(peer_id: int) -> void:
	if peer_id in peers:
		peers.erase(peer_id)
		peer_disconnected.emit(peer_id)


## Set match configuration (called by host when starting match)
func set_match_config(config: RefCounted) -> void:
	match_config = config
	match_starting.emit(config)


## Start the match (called after all peers are ready)
func start_match() -> void:
	if match_config:
		match_started.emit(match_config)


## End the match
func end_match(winner_peer_id: int, reason: String) -> void:
	match_ended.emit(winner_peer_id, reason)
	match_config = null


## Reset all state (called when leaving multiplayer)
func reset() -> void:
	set_state(ConnectionState.OFFLINE)
	local_peer_id = 0
	peers.clear()
	match_config = null
	room_code = ""
	latency_ms = 0


## =============================================================================
## MULTIPLAYER SIGNAL HANDLERS
## =============================================================================

func _on_peer_connected(id: int) -> void:
	# Create placeholder peer info - will be updated with handshake data
	var peer_info: RefCounted = PeerInfoScript.new(id, "Player %d" % id)
	peer_info.connection_state = PeerInfoScript.ConnectionState.CONNECTED
	register_peer(peer_info)


func _on_peer_disconnected(id: int) -> void:
	unregister_peer(id)


func _on_connected_to_server() -> void:
	local_peer_id = multiplayer.get_unique_id()
	set_state(ConnectionState.CONNECTED)

	# Create local peer info
	var local_info: RefCounted = PeerInfoScript.new(local_peer_id, "Player")
	local_info.connection_state = PeerInfoScript.ConnectionState.CONNECTED
	register_peer(local_info)


func _on_connection_failed() -> void:
	set_state(ConnectionState.DISCONNECTED)


func _on_server_disconnected() -> void:
	set_state(ConnectionState.DISCONNECTED)
