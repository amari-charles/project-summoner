extends Node
class_name P2PClient

## P2P Client for multiplayer matches
##
## Manages connecting to a P2P host using ENetMultiplayerPeer.
## Handles connection, handshake, and match initialization.

const PeerInfoScript: GDScript = preload("res://scripts/multiplayer/core/peer_info.gd")
const MatchConfigScript: GDScript = preload("res://scripts/multiplayer/core/match_config.gd")
const RoomCodeServiceScript: GDScript = preload("res://scripts/multiplayer/connection/room_code_service.gd")

## Connection timeout in seconds
const CONNECTION_TIMEOUT: float = 10.0

## Peer instance
var peer: ENetMultiplayerPeer = null

## Client's player info
var client_info: RefCounted = null

## Host's player info
var host_info: RefCounted = null

## Match configuration received from host
var match_config: RefCounted = null

## Is the client ready to start
var client_ready: bool = false


## Emitted when connection succeeds
signal connected_to_host()

## Emitted when connection fails
signal connection_failed(error: String)

## Emitted when disconnected from host
signal disconnected_from_host()

## Emitted when host info is received
signal host_info_received(peer_info: RefCounted)

## Emitted when match config is received
signal match_config_received(config: RefCounted)

## Emitted when match is starting
signal match_starting(config: RefCounted)


## Connect to a host by IP address
func connect_to_host(ip: String, port: int, player_name: String, summoner_id: String) -> Error:
	# Create client info
	client_info = PeerInfoScript.new(0, player_name)  # ID will be assigned on connect
	client_info.summoner_id = summoner_id
	client_info.connection_state = PeerInfoScript.ConnectionState.CONNECTING

	# Create peer
	peer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_client(ip, port)

	if error != OK:
		connection_failed.emit("Failed to create client: %s" % error_string(error))
		return error

	# Set multiplayer peer
	multiplayer.multiplayer_peer = peer

	# Update NetworkState
	NetworkState.set_state(NetworkState.ConnectionState.CONNECTING)

	# Start connection timeout
	get_tree().create_timer(CONNECTION_TIMEOUT).timeout.connect(
		func() -> void:
			if NetworkState.state == NetworkState.ConnectionState.CONNECTING:
				print("P2PClient: Connection timeout")
				disconnect_from_host()
				connection_failed.emit("Connection timed out")
	)

	return OK


## Connect using a connection string (IP:PORT or just IP)
func connect_with_string(connection_string: String, player_name: String, summoner_id: String) -> Error:
	var parsed: Dictionary = RoomCodeServiceScript.parse_connection_string(connection_string)

	if not parsed.valid:
		connection_failed.emit("Invalid connection string: %s" % connection_string)
		return ERR_INVALID_PARAMETER

	return connect_to_host(parsed.ip, parsed.port, player_name, summoner_id)


## Disconnect from the host
func disconnect_from_host() -> void:
	if peer:
		peer.close()
		peer = null

	multiplayer.multiplayer_peer = null
	NetworkState.reset()

	client_info = null
	host_info = null
	match_config = null
	client_ready = false


## Set client as ready
func set_ready(ready: bool) -> void:
	client_ready = ready
	if NetworkState.is_online():
		_client_set_ready.rpc_id(1, ready)


## Called when connected to server
func _on_connected_to_server() -> void:
	print("P2PClient: Connected to server")

	# Update local peer ID
	client_info.peer_id = multiplayer.get_unique_id()
	var connected_state: int = PeerInfoScript.ConnectionState.CONNECTED
	client_info.connection_state = connected_state

	NetworkState.local_peer_id = client_info.peer_id
	NetworkState.set_state(NetworkState.ConnectionState.CONNECTED)
	NetworkState.register_peer(client_info)

	connected_to_host.emit()


## Called when connection fails
func _on_connection_failed() -> void:
	print("P2PClient: Connection failed")
	NetworkState.set_state(NetworkState.ConnectionState.DISCONNECTED)
	connection_failed.emit("Failed to connect to host")


## Called when disconnected from server
func _on_server_disconnected() -> void:
	print("P2PClient: Disconnected from server")
	NetworkState.set_state(NetworkState.ConnectionState.DISCONNECTED)
	disconnected_from_host.emit()


## Receive host info from host (sent via RPC)
@rpc("authority", "call_remote", "reliable")
func _send_host_info() -> void:
	# We received host info request - send our info back
	print("P2PClient: Received host info request, sending client info")

	var data: Dictionary = client_info.serialize()
	data["deck_hash"] = _compute_deck_hash()
	_receive_client_info.rpc_id(1, data)


## Send client info to host (this is the actual RPC target on host)
@rpc("any_peer", "call_remote", "reliable")
func _receive_client_info(_data: Dictionary) -> void:
	# This is called ON the host (sent from client)
	pass  # Implementation is on host side


## Receive match config from host
@rpc("authority", "call_remote", "reliable")
func _send_match_config(config_data: Dictionary) -> void:
	print("P2PClient: Received match config")
	match_config = MatchConfigScript.deserialize(config_data)
	NetworkState.set_match_config(match_config)
	match_config_received.emit(match_config)


## Receive client ready state (RPC stub - actual implementation is on P2PHost)
@rpc("any_peer", "call_remote", "reliable")
func _client_set_ready(_ready: bool) -> void:
	pass


## Receive match starting notification from host
@rpc("authority", "call_remote", "reliable")
func _match_starting(config_data: Dictionary) -> void:
	print("P2PClient: Match is starting!")
	match_config = MatchConfigScript.deserialize(config_data)
	match_starting.emit(match_config)


## Compute deck hash for validation
func _compute_deck_hash() -> int:
	# Placeholder: Returns fixed hash for Phase 1 testing
	# Phase 2.1 (Action System): Connect to Decks service to get actual deck card IDs
	return hash("placeholder_deck")


func _ready() -> void:
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)
