extends Control
class_name MultiplayerLobby

## Multiplayer lobby screen for creating and joining P2P matches
##
## Supports two modes:
## - Host: Create a room and wait for opponent
## - Join: Enter IP address to connect to host

const P2PHostScript: GDScript = preload("res://scripts/multiplayer/connection/p2p_host.gd")
const P2PClientScript: GDScript = preload("res://scripts/multiplayer/connection/p2p_client.gd")
const RoomCodeServiceScript: GDScript = preload("res://scripts/multiplayer/connection/room_code_service.gd")

enum LobbyState { MENU, HOSTING, JOINING, WAITING_FOR_OPPONENT, CONNECTED, STARTING }

## Brief delay before transitioning to battle scene (allows UI feedback to be visible)
const MATCH_START_DELAY: float = 1.0

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

## P2P instances
var _p2p_host: P2PHost = null
var _p2p_client: P2PClient = null

## Placeholder player info
## TODO: Replace with actual player profile data from profile service
var _player_name: String = "Player"
var _summoner_id: String = "ignis"


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
	# Menu buttons
	host_button.pressed.connect(_on_host_pressed)
	join_button.pressed.connect(_on_join_pressed)
	back_button.pressed.connect(_on_back_pressed)

	# Host buttons
	host_ready_button.pressed.connect(_on_host_ready_pressed)
	host_cancel_button.pressed.connect(_on_cancel_pressed)

	# Join buttons
	connect_button.pressed.connect(_on_connect_pressed)
	join_cancel_button.pressed.connect(_on_cancel_pressed)

	# Waiting buttons
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
## HOST FLOW
## =============================================================================

func _on_host_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_start_hosting()


func _start_hosting() -> void:
	_set_state(LobbyState.HOSTING)
	host_status_label.text = Loc.t("ui.multiplayer.creating_room")
	host_ready_button.disabled = true

	# Create host instance
	_p2p_host = P2PHostScript.new()
	add_child(_p2p_host)

	# Connect signals
	_p2p_host.server_started.connect(_on_server_started)
	_p2p_host.server_failed.connect(_on_server_failed)
	_p2p_host.client_connected.connect(_on_client_connected)
	_p2p_host.client_disconnected.connect(_on_client_disconnected)
	_p2p_host.client_info_received.connect(_on_client_info_received)
	_p2p_host.match_ready.connect(_on_match_ready)

	# Start server
	var error: Error = _p2p_host.start_server(_player_name, _summoner_id)
	if error != OK:
		host_status_label.text = Loc.t("ui.multiplayer.error_creating_room")


func _on_server_started(room_code: String) -> void:
	room_code_label.text = room_code
	host_status_label.text = Loc.t("ui.multiplayer.waiting_for_opponent")
	_set_state(LobbyState.WAITING_FOR_OPPONENT)


func _on_server_failed(error: String) -> void:
	host_status_label.text = error
	_cleanup_host()
	_set_state(LobbyState.MENU)


func _on_client_connected(_peer_info: RefCounted) -> void:
	host_status_label.text = Loc.t("ui.multiplayer.player_connecting")


func _on_client_disconnected(_peer_id: int) -> void:
	host_status_label.text = Loc.t("ui.multiplayer.player_disconnected")
	host_ready_button.disabled = true


func _on_client_info_received(peer_info: RefCounted) -> void:
	host_status_label.text = Loc.t("ui.multiplayer.player_joined").format({"name": peer_info.display_name})
	host_ready_button.disabled = false


func _on_host_ready_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if _p2p_host:
		_p2p_host.set_host_ready(true)
		host_ready_button.disabled = true
		host_status_label.text = Loc.t("ui.multiplayer.waiting_for_ready")


## =============================================================================
## JOIN FLOW
## =============================================================================

func _on_join_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_set_state(LobbyState.JOINING)
	join_status_label.text = ""
	ip_input.text = ""


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

	# Create client instance
	_p2p_client = P2PClientScript.new()
	add_child(_p2p_client)

	# Connect signals
	_p2p_client.connected_to_host.connect(_on_connected_to_host)
	_p2p_client.connection_failed.connect(_on_connection_failed)
	_p2p_client.disconnected_from_host.connect(_on_disconnected_from_host)
	_p2p_client.match_config_received.connect(_on_match_config_received)
	_p2p_client.match_starting.connect(_on_client_match_starting)

	# Connect
	var error: Error = _p2p_client.connect_with_string(connection_string, _player_name, _summoner_id)
	if error != OK:
		join_status_label.text = Loc.t("ui.multiplayer.invalid_address")
		connect_button.disabled = false
		_cleanup_client()


func _on_connected_to_host() -> void:
	_set_state(LobbyState.CONNECTED)
	waiting_status_label.text = Loc.t("ui.multiplayer.connected_waiting")
	client_ready_button.disabled = true


func _on_connection_failed(error: String) -> void:
	join_status_label.text = error
	connect_button.disabled = false
	_cleanup_client()


func _on_disconnected_from_host() -> void:
	waiting_status_label.text = Loc.t("ui.multiplayer.disconnected")
	_cleanup_client()
	_set_state(LobbyState.MENU)


func _on_match_config_received(_config: RefCounted) -> void:
	waiting_status_label.text = Loc.t("ui.multiplayer.ready_to_play")
	client_ready_button.disabled = false


func _on_client_ready_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if _p2p_client:
		_p2p_client.set_ready(true)
		client_ready_button.disabled = true
		waiting_status_label.text = Loc.t("ui.multiplayer.waiting_for_host")


func _on_client_match_starting(config: RefCounted) -> void:
	_set_state(LobbyState.STARTING)
	waiting_status_label.text = Loc.t("ui.multiplayer.match_starting")
	_start_match(config)


## =============================================================================
## MATCH START
## =============================================================================

func _on_match_ready(config: RefCounted) -> void:
	_set_state(LobbyState.STARTING)
	host_status_label.text = Loc.t("ui.multiplayer.match_starting")
	_start_match(config)


func _start_match(config: RefCounted) -> void:
	# Set up BattleRNG with shared seed
	BattleRNG.set_battle_seed(config.battle_seed)

	# Store config in NetworkState
	NetworkState.set_match_config(config)
	NetworkState.start_match()

	# TODO: Transition to battle scene
	# For now, just print and wait
	print("MultiplayerLobby: Match starting with seed %d" % config.battle_seed)

	# Transition to battle after brief delay
	await get_tree().create_timer(MATCH_START_DELAY).timeout
	# SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)


## =============================================================================
## CANCEL / BACK
## =============================================================================

func _on_cancel_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_cleanup_host()
	_cleanup_client()
	_show_menu()


func _on_back_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)


## =============================================================================
## CLEANUP
## =============================================================================

func _cleanup_host() -> void:
	if _p2p_host:
		_p2p_host.stop_server()
		_p2p_host.queue_free()
		_p2p_host = null


func _cleanup_client() -> void:
	if _p2p_client:
		_p2p_client.disconnect_from_host()
		_p2p_client.queue_free()
		_p2p_client = null


func _exit_tree() -> void:
	_cleanup_host()
	_cleanup_client()
