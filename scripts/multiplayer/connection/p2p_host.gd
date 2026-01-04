extends Node
class_name P2PHost

## P2P Host for multiplayer matches
##
## Manages hosting a P2P game session using ENetMultiplayerPeer.
## Handles client connections, handshakes, and match initialization.

const PeerInfoScript: GDScript = preload("res://scripts/multiplayer/core/peer_info.gd")
const MatchConfigScript: GDScript = preload("res://scripts/multiplayer/core/match_config.gd")
const RoomCodeServiceScript: GDScript = preload("res://scripts/multiplayer/connection/room_code_service.gd")

## Maximum number of clients (1v1 = 1 client)
const MAX_CLIENTS: int = 1

## Timeout for client handshake in seconds
const HANDSHAKE_TIMEOUT: float = 10.0

## Current room code
var room_code: String = ""

## Port the server is listening on
var port: int = RoomCodeServiceScript.DEFAULT_PORT

## Peer instance
var peer: ENetMultiplayerPeer = null

## Host's player info
var host_info: RefCounted = null

## Connected client info (awaiting or ready)
var client_info: RefCounted = null

## Match configuration
var match_config: RefCounted = null

## Is the host ready to start
var host_ready: bool = false

## Is the client ready to start
var client_ready: bool = false


## Emitted when server starts successfully
signal server_started(room_code: String)

## Emitted when server fails to start
signal server_failed(error: String)

## Emitted when a client connects
signal client_connected(peer_info: RefCounted)

## Emitted when a client disconnects
signal client_disconnected(peer_id: int)

## Emitted when client sends their player info
signal client_info_received(peer_info: RefCounted)

## Emitted when both players are ready and match can start
signal match_ready(config: RefCounted)


## Start hosting a P2P session
func start_server(host_name: String, summoner_id: String) -> Error:
	# Generate room code
	room_code = RoomCodeServiceScript.generate_code()

	# Create peer
	peer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_server(port, MAX_CLIENTS)

	if error != OK:
		server_failed.emit("Failed to create server: %s" % error_string(error))
		return error

	# Set multiplayer peer
	multiplayer.multiplayer_peer = peer

	# Create host info
	host_info = PeerInfoScript.new(1, host_name)
	host_info.is_host = true
	host_info.summoner_id = summoner_id
	var connected_state: int = PeerInfoScript.ConnectionState.CONNECTED
	host_info.connection_state = connected_state

	# Register with NetworkState
	NetworkState.local_peer_id = 1
	NetworkState.room_code = room_code
	NetworkState.set_state(NetworkState.ConnectionState.CONNECTED)
	NetworkState.register_peer(host_info)

	# Create initial match config
	match_config = MatchConfigScript.create_new(1)
	var deck_hash: int = _compute_deck_hash()
	match_config.add_player(1, summoner_id, deck_hash)

	server_started.emit(room_code)
	return OK


## Stop the server and disconnect all clients
func stop_server() -> void:
	if peer:
		peer.close()
		peer = null

	multiplayer.multiplayer_peer = null
	NetworkState.reset()

	room_code = ""
	host_info = null
	client_info = null
	match_config = null
	host_ready = false
	client_ready = false


## Set host as ready
func set_host_ready(ready: bool) -> void:
	host_ready = ready
	_check_match_ready()


## Called when a peer connects
func _on_peer_connected(id: int) -> void:
	print("P2PHost: Peer %d connected" % id)

	# Create placeholder info
	client_info = PeerInfoScript.new(id, "Connecting...")
	var connecting_state: int = PeerInfoScript.ConnectionState.CONNECTING
	client_info.connection_state = connecting_state

	client_connected.emit(client_info)

	# Send host info to client
	_send_host_info.rpc_id(id)

	# Start handshake timeout
	var timeout_connecting_state: int = PeerInfoScript.ConnectionState.CONNECTING
	get_tree().create_timer(HANDSHAKE_TIMEOUT).timeout.connect(
		func() -> void:
			if client_info and client_info.connection_state == timeout_connecting_state:
				print("P2PHost: Client %d handshake timeout" % id)
				peer.disconnect_peer(id)
	)


## Called when a peer disconnects
func _on_peer_disconnected(id: int) -> void:
	print("P2PHost: Peer %d disconnected" % id)

	if client_info and client_info.peer_id == id:
		NetworkState.unregister_peer(id)
		client_disconnected.emit(id)
		client_info = null
		client_ready = false

		# Remove from match config
		if match_config and match_config.player_peer_ids.size() > 1:
			match_config.player_peer_ids.resize(1)
			match_config.player_summoner_ids.resize(1)
			match_config.player_deck_hashes.resize(1)


## Send host info to newly connected client
@rpc("authority", "call_remote", "reliable")
func _send_host_info() -> void:
	# This is called ON the client (sent from host)
	pass  # Implementation is on client side


## Receive client info during handshake
@rpc("any_peer", "call_remote", "reliable")
func _receive_client_info(data: Dictionary) -> void:
	var sender_id: int = multiplayer.get_remote_sender_id()
	print("P2PHost: Received client info from peer %d" % sender_id)

	if client_info and client_info.peer_id == sender_id:
		client_info = PeerInfoScript.deserialize(data)
		client_info.peer_id = sender_id
		var client_connected_state: int = PeerInfoScript.ConnectionState.CONNECTED
		client_info.connection_state = client_connected_state

		# Add to match config
		match_config.add_player(
			sender_id,
			client_info.summoner_id,
			data.get("deck_hash", 0)
		)

		# Register with NetworkState
		NetworkState.register_peer(client_info)

		# Send match config to client
		_send_match_config.rpc_id(sender_id, match_config.serialize())

		client_info_received.emit(client_info)


## Send match config to client
@rpc("authority", "call_remote", "reliable")
func _send_match_config(_config_data: Dictionary) -> void:
	# This is called ON the client (sent from host)
	pass  # Implementation is on client side


## Receive client ready signal
@rpc("any_peer", "call_remote", "reliable")
func _client_set_ready(ready: bool) -> void:
	var sender_id: int = multiplayer.get_remote_sender_id()
	if client_info and client_info.peer_id == sender_id:
		client_ready = ready
		print("P2PHost: Client %d ready = %s" % [sender_id, ready])
		_check_match_ready()


## Check if both players are ready and emit match_ready
func _check_match_ready() -> void:
	if host_ready and client_ready and match_config:
		print("P2PHost: Both players ready, starting match")
		# Notify client that match is starting
		_match_starting.rpc(match_config.serialize())
		match_ready.emit(match_config)


## Broadcast match starting to all clients
@rpc("authority", "call_remote", "reliable")
func _match_starting(_config_data: Dictionary) -> void:
	# This is called ON the client (sent from host)
	pass  # Implementation is on client side


## Compute deck hash for the local player (for validation)
func _compute_deck_hash() -> int:
	# Simple hash of deck card IDs
	# TODO: Get actual deck from Decks service when integrating
	return hash("placeholder_deck")


func _ready() -> void:
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
