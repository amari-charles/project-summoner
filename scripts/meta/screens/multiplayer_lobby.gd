extends Control
class_name MultiplayerLobby

## Multiplayer lobby screen for creating and joining P2P matches
##
## Uses C# P2PTransport for networking.
## Supports two modes:
## - Host: Create a room and wait for opponent
## - Join: Enter IP address to connect to host

const P2PTransportScene: PackedScene = preload("res://scripts/csharp/Battle/Session/Transport/P2PTransport.tscn")

enum LobbyState { MENU, HOSTING, JOINING, WAITING_FOR_OPPONENT, CONNECTED, STARTING }

## Brief delay before transitioning to battle scene
const MATCH_START_DELAY: float = 1.0

## Default port for P2P connections
const DEFAULT_PORT: int = 7777

## UI References
@onready var menu_panel: Control = $MenuPanel
@onready var host_panel: Control = $HostPanel
@onready var join_panel: Control = $JoinPanel
@onready var waiting_panel: Control = $WaitingPanel

## Menu panel
@onready var title_label: Label = $MenuPanel/VBoxContainer/Title
@onready var host_button: Button = $MenuPanel/VBoxContainer/HostButton
@onready var join_button: Button = $MenuPanel/VBoxContainer/JoinButton
@onready var back_button: Button = $MenuPanel/VBoxContainer/BackButton

## Host panel
@onready var host_title: Label = $HostPanel/VBoxContainer/Title
@onready var room_code_label: Label = $HostPanel/VBoxContainer/RoomCodeContainer/RoomCodeLabel
@onready var host_status_label: Label = $HostPanel/VBoxContainer/StatusLabel
@onready var host_ready_button: Button = $HostPanel/VBoxContainer/ReadyButton
@onready var host_cancel_button: Button = $HostPanel/VBoxContainer/CancelButton

## Join panel
@onready var join_title: Label = $JoinPanel/VBoxContainer/Title
@onready var ip_input: LineEdit = $JoinPanel/VBoxContainer/IPContainer/IPInput
@onready var connect_button: Button = $JoinPanel/VBoxContainer/ConnectButton
@onready var join_status_label: Label = $JoinPanel/VBoxContainer/StatusLabel
@onready var join_cancel_button: Button = $JoinPanel/VBoxContainer/CancelButton

## Waiting panel
@onready var waiting_title: Label = $WaitingPanel/VBoxContainer/Title
@onready var waiting_status_label: Label = $WaitingPanel/VBoxContainer/StatusLabel
@onready var client_ready_button: Button = $WaitingPanel/VBoxContainer/ReadyButton
@onready var waiting_cancel_button: Button = $WaitingPanel/VBoxContainer/CancelButton

## Current state
var _state: LobbyState = LobbyState.MENU

## C# Transport instance
var _transport: Node = null

## Whether we're hosting
var _is_host: bool = false

## Whether opponent is connected
var _opponent_connected: bool = false

## Whether host is ready
var _host_ready: bool = false

## Whether client is ready
var _client_ready: bool = false

## Player info
var _player_name: String = "Player"
var _summoner_id: String = "ignis"

## Opponent info (received via network)
var _opponent_name: String = ""
var _opponent_summoner_id: String = ""


func _ready() -> void:
	_setup_localization()
	_setup_signals()
	_show_menu()


func _setup_localization() -> void:
	title_label.text = Loc.t("ui.multiplayer.title")
	host_button.text = Loc.t("ui.multiplayer.host_game")
	join_button.text = Loc.t("ui.multiplayer.join_game")
	back_button.text = Loc.t("ui.common.back")

	host_title.text = Loc.t("ui.multiplayer.hosting")
	host_ready_button.text = Loc.t("ui.multiplayer.ready")
	host_cancel_button.text = Loc.t("ui.common.cancel")

	join_title.text = Loc.t("ui.multiplayer.joining")
	ip_input.placeholder_text = Loc.t("ui.multiplayer.enter_ip")
	connect_button.text = Loc.t("ui.multiplayer.connect")
	join_cancel_button.text = Loc.t("ui.common.cancel")

	waiting_title.text = Loc.t("ui.multiplayer.connected")
	client_ready_button.text = Loc.t("ui.multiplayer.ready")
	waiting_cancel_button.text = Loc.t("ui.common.cancel")


func _setup_signals() -> void:
	host_button.pressed.connect(_on_host_pressed)
	join_button.pressed.connect(_on_join_pressed)
	back_button.pressed.connect(_on_back_pressed)
	host_ready_button.pressed.connect(_on_host_ready_pressed)
	host_cancel_button.pressed.connect(_on_cancel_pressed)
	connect_button.pressed.connect(_on_connect_pressed)
	join_cancel_button.pressed.connect(_on_cancel_pressed)
	client_ready_button.pressed.connect(_on_client_ready_pressed)
	waiting_cancel_button.pressed.connect(_on_cancel_pressed)


## =============================================================================
## STATE MANAGEMENT
## =============================================================================

func _set_state(new_state: LobbyState) -> void:
	_state = new_state
	_update_ui()


func _update_ui() -> void:
	menu_panel.visible = _state == LobbyState.MENU
	host_panel.visible = _state in [LobbyState.HOSTING, LobbyState.WAITING_FOR_OPPONENT]
	join_panel.visible = _state == LobbyState.JOINING
	waiting_panel.visible = _state in [LobbyState.CONNECTED, LobbyState.STARTING]


func _show_menu() -> void:
	_set_state(LobbyState.MENU)


## =============================================================================
## TRANSPORT MANAGEMENT
## =============================================================================

func _create_transport() -> void:
	if _transport != null:
		return

	_transport = P2PTransportScene.instantiate()
	add_child(_transport)

	# Connect C# signals
	if _transport.has_signal("PeerConnected"):
		_transport.PeerConnected.connect(_on_peer_connected)
	if _transport.has_signal("PeerDisconnected"):
		_transport.PeerDisconnected.connect(_on_peer_disconnected)
	if _transport.has_signal("Connected"):
		_transport.Connected.connect(_on_connected_to_server)
	if _transport.has_signal("Disconnected"):
		_transport.Disconnected.connect(_on_disconnected)
	if _transport.has_signal("MessageReceived"):
		_transport.MessageReceived.connect(_on_message_received)


func _cleanup_transport() -> void:
	if _transport != null:
		if _transport.has_method("Disconnect"):
			_transport.Disconnect()
		_transport.queue_free()
		_transport = null

	_opponent_connected = false
	_host_ready = false
	_client_ready = false
	_opponent_name = ""
	_opponent_summoner_id = ""


## =============================================================================
## HOST FLOW
## =============================================================================

func _on_host_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_start_hosting()


func _start_hosting() -> void:
	_set_state(LobbyState.HOSTING)
	host_status_label.text = Loc.t("ui.multiplayer.creating_room")
	host_ready_button.disabled = true
	_is_host = true

	_create_transport()

	if _transport.has_method("Host"):
		_transport.Host(DEFAULT_PORT)

		# Generate room code from local IP
		var room_code: String = _generate_room_code()
		room_code_label.text = room_code
		host_status_label.text = Loc.t("ui.multiplayer.waiting_for_opponent")
		_set_state(LobbyState.WAITING_FOR_OPPONENT)
	else:
		host_status_label.text = Loc.t("ui.multiplayer.error_creating_room")
		_cleanup_transport()
		_set_state(LobbyState.MENU)


func _generate_room_code() -> String:
	# Get local IP address and format as room code
	var ip_addresses: PackedStringArray = IP.get_local_addresses()
	for ip in ip_addresses:
		# Skip loopback and IPv6
		if ip.begins_with("127.") or ip.contains(":"):
			continue
		# Return first valid IPv4 address with port
		return "%s:%d" % [ip, DEFAULT_PORT]
	return "127.0.0.1:%d" % DEFAULT_PORT


func _on_host_ready_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_host_ready = true
	host_ready_button.disabled = true
	host_status_label.text = Loc.t("ui.multiplayer.waiting_for_ready")

	# Send ready status to client
	_send_message({"type": "ready", "ready": true})

	# Check if both ready
	_check_both_ready()


## =============================================================================
## JOIN FLOW
## =============================================================================

func _on_join_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_set_state(LobbyState.JOINING)
	join_status_label.text = ""
	ip_input.text = ""
	_is_host = false


func _on_connect_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	var connection_string: String = ip_input.text.strip_edges()
	if connection_string.is_empty():
		join_status_label.text = Loc.t("ui.multiplayer.enter_ip")
		return

	_start_joining(connection_string)


func _start_joining(connection_string: String) -> void:
	join_status_label.text = Loc.t("ui.multiplayer.connecting")
	connect_button.disabled = true

	# Parse IP:port
	var parts: PackedStringArray = connection_string.split(":")
	var ip: String = parts[0]
	var port: int = DEFAULT_PORT
	if parts.size() > 1:
		port = int(parts[1])

	_create_transport()

	if _transport.has_method("Connect"):
		_transport.Connect(ip, port)
	else:
		join_status_label.text = Loc.t("ui.multiplayer.invalid_address")
		connect_button.disabled = false
		_cleanup_transport()


func _on_client_ready_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_client_ready = true
	client_ready_button.disabled = true
	waiting_status_label.text = Loc.t("ui.multiplayer.waiting_for_host")

	# Send ready status to host
	_send_message({"type": "ready", "ready": true})

	# Check if both ready
	_check_both_ready()


## =============================================================================
## NETWORK CALLBACKS
## =============================================================================

func _on_peer_connected(peer_id: int) -> void:
	print("MultiplayerLobby: Peer connected: %d" % peer_id)
	_opponent_connected = true

	if _is_host:
		host_status_label.text = Loc.t("ui.multiplayer.player_connecting")
		# Send our info to client
		_send_player_info()
	else:
		# Client connected to host
		_send_player_info()


func _on_peer_disconnected(peer_id: int) -> void:
	print("MultiplayerLobby: Peer disconnected: %d" % peer_id)
	_opponent_connected = false

	if _is_host:
		host_status_label.text = Loc.t("ui.multiplayer.player_disconnected")
		host_ready_button.disabled = true
	else:
		waiting_status_label.text = Loc.t("ui.multiplayer.disconnected")
		_cleanup_transport()
		_set_state(LobbyState.MENU)


func _on_connected_to_server() -> void:
	print("MultiplayerLobby: Connected to server")
	_set_state(LobbyState.CONNECTED)
	waiting_status_label.text = Loc.t("ui.multiplayer.connected_waiting")
	client_ready_button.disabled = true


func _on_disconnected(reason: String) -> void:
	print("MultiplayerLobby: Disconnected: %s" % reason)
	if _state == LobbyState.JOINING:
		join_status_label.text = reason
		connect_button.disabled = false
	else:
		_cleanup_transport()
		_set_state(LobbyState.MENU)


func _on_message_received(sender_id: int, message: Dictionary) -> void:
	var msg_type: String = message.get("type", "")

	match msg_type:
		"player_info":
			_handle_player_info(message)
		"ready":
			_handle_ready_message(message)
		"start_match":
			_handle_start_match(message)


## =============================================================================
## MESSAGE HANDLING
## =============================================================================

func _send_message(data: Dictionary) -> void:
	if _transport == null:
		return
	if _transport.has_method("Send"):
		_transport.Send(data)


func _send_player_info() -> void:
	_send_message({
		"type": "player_info",
		"name": _player_name,
		"summoner_id": _get_active_summoner_id()
	})


func _handle_player_info(message: Dictionary) -> void:
	_opponent_name = message.get("name", "Opponent")
	_opponent_summoner_id = message.get("summoner_id", "ignis")

	if _is_host:
		host_status_label.text = Loc.t("ui.multiplayer.player_joined").format({"name": _opponent_name})
		host_ready_button.disabled = false
	else:
		waiting_status_label.text = Loc.t("ui.multiplayer.ready_to_play")
		client_ready_button.disabled = false


func _handle_ready_message(message: Dictionary) -> void:
	var is_ready: bool = message.get("ready", false)

	if _is_host:
		_client_ready = is_ready
	else:
		_host_ready = is_ready

	_check_both_ready()


func _check_both_ready() -> void:
	if not _is_host:
		return  # Only host initiates match start

	if _host_ready and _client_ready:
		_initiate_match_start()


func _initiate_match_start() -> void:
	# Generate shared battle seed
	var battle_seed: int = randi()

	# Send start message to client
	_send_message({
		"type": "start_match",
		"seed": battle_seed,
		"host_summoner": _get_active_summoner_id(),
		"client_summoner": _opponent_summoner_id
	})

	# Start match locally
	_start_match(battle_seed, _get_active_summoner_id(), _opponent_summoner_id, true)


func _handle_start_match(message: Dictionary) -> void:
	var battle_seed: int = message.get("seed", 0)
	var host_summoner: String = message.get("host_summoner", "ignis")
	var client_summoner: String = message.get("client_summoner", "ignis")

	# Client starts match with reversed summoner order
	_start_match(battle_seed, client_summoner, host_summoner, false)


## =============================================================================
## MATCH START
## =============================================================================

func _start_match(battle_seed: int, player_summoner: String, opponent_summoner: String, is_host: bool) -> void:
	_set_state(LobbyState.STARTING)

	if is_host:
		host_status_label.text = Loc.t("ui.multiplayer.match_starting")
	else:
		waiting_status_label.text = Loc.t("ui.multiplayer.match_starting")

	# Get player deck
	var player_deck: Array = _get_player_deck()

	# Configure BattleContext for multiplayer
	BattleContext.configure_multiplayer_battle(
		player_summoner,
		opponent_summoner,
		player_deck,
		[],  # Opponent deck synced during battle
		is_host,
		battle_seed
	)

	# TODO: Wire multiplayer authority via HostSession/ClientSession
	# LocalPlayer.NetworkIndex should be set when sessions are created

	# Brief delay for UI feedback
	await get_tree().create_timer(MATCH_START_DELAY).timeout

	# Transition to battle (transport stays alive for the match)
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)


func _get_active_summoner_id() -> String:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("get_selected_summoner_id"):
		var selected: String = summoner_selection.get_selected_summoner_id()
		if not selected.is_empty():
			return selected
	return "ignis"


func _get_player_deck() -> Array:
	return ProfileRepo.GetActiveDeckArray()


## =============================================================================
## CANCEL / BACK
## =============================================================================

func _on_cancel_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_cleanup_transport()
	_show_menu()


func _on_back_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)


func _exit_tree() -> void:
	_cleanup_transport()
