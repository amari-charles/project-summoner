extends Node
class_name NetworkStateClass

## Autoload singleton tracking multiplayer connection state
##
## Simplified state tracker for multiplayer. The core multiplayer logic
## has been migrated to C# (HostSession/ClientSession, P2PTransport, etc.).
## This GDScript autoload provides basic state that UI can query.

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

## Whether we are the host/server
var is_host: bool = false

## Room code for P2P connection
var room_code: String = ""

## Average latency to host/server in milliseconds
var latency_ms: int = 0


## Emitted when connection state changes
signal connection_state_changed(new_state: ConnectionState)

## Emitted when a peer connects
signal peer_connected(peer_id: int)

## Emitted when a peer disconnects
signal peer_disconnected(peer_id: int)

## Emitted when match ends
signal match_ended(winner_peer_id: int, reason: String)


func _ready() -> void:
	# Connect to Godot's multiplayer signals
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)


## Check if we are online (connected)
func is_online() -> bool:
	return state == ConnectionState.CONNECTED


## =============================================================================
## STATE MANAGEMENT
## =============================================================================

## Set connection state and emit signal
func set_state(new_state: ConnectionState) -> void:
	if state != new_state:
		state = new_state
		connection_state_changed.emit(new_state)


## End the match
func end_match(winner_peer_id: int, reason: String) -> void:
	match_ended.emit(winner_peer_id, reason)


## Reset all state (called when leaving multiplayer)
func reset() -> void:
	set_state(ConnectionState.OFFLINE)
	local_peer_id = 0
	is_host = false
	room_code = ""
	latency_ms = 0


## =============================================================================
## MULTIPLAYER SIGNAL HANDLERS
## =============================================================================

func _on_peer_connected(id: int) -> void:
	peer_connected.emit(id)


func _on_peer_disconnected(id: int) -> void:
	peer_disconnected.emit(id)


func _on_connected_to_server() -> void:
	local_peer_id = multiplayer.get_unique_id()
	set_state(ConnectionState.CONNECTED)


func _on_connection_failed() -> void:
	set_state(ConnectionState.DISCONNECTED)


func _on_server_disconnected() -> void:
	set_state(ConnectionState.DISCONNECTED)
