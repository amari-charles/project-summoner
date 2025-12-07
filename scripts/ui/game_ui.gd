extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

@export var timer_label: Label = null
@export var player_mana_bar: ManaBar = null
@export var game_over_label: Label = null
@export var restart_button: Button = null

var game_controller: Node = null
var player_summoner: Summoner = null
var _initialized: bool = false  # Track initialization state

## Player team value that works for both Unit.Team.PLAYER (2D) and Unit3D.Team.PLAYER (3D)
const PLAYER_TEAM_VALUE: int = 0

func _ready() -> void:
	# Minimal setup - find child nodes if not assigned via @export
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if player_mana_bar == null:
		player_mana_bar = get_node_or_null("PlayerManaBar")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")
	if restart_button == null:
		restart_button = get_node_or_null("RestartButton")

	# Connect restart button (always safe to do in _ready)
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		restart_button.visible = false  # Hidden until game over

## Initialize GameUI with controller and summoner references
## Called by BattleCoordinator after summoners are ready
func init(controller: Node, summoner: Node) -> void:
	if _initialized:
		return
	_initialized = true

	game_controller = controller
	player_summoner = summoner

	# Connect to game controller signals
	if game_controller:
		if game_controller.has_signal("time_updated"):
			var time_updated_signal: Signal = game_controller.get("time_updated")
			time_updated_signal.connect(_on_time_updated)
		if game_controller.has_signal("game_ended"):
			var game_ended_signal: Signal = game_controller.get("game_ended")
			game_ended_signal.connect(_on_game_ended)
	else:
		push_error("GameUI: init() called with null game_controller!")

	# Connect to summoner signals
	if player_summoner:
		_connect_to_summoner(player_summoner)
	else:
		push_error("GameUI: init() called with null player_summoner!")

## Connect to summoner signals
func _connect_to_summoner(summoner: Node) -> void:
	if summoner.has_signal("mana_changed"):
		var mana_changed_signal: Signal = summoner.get("mana_changed")
		mana_changed_signal.connect(_on_mana_changed)
		print("GameUI: Connected to PlayerSummoner mana_changed signal")
	else:
		push_warning("GameUI: PlayerSummoner found but has no mana_changed signal")

func _on_time_updated(remaining: float) -> void:
	if timer_label:
		var minutes: int = floori(remaining / 60.0)
		var seconds: int = int(remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_bar:
		player_mana_bar.update_mana(current, maximum)

func _on_game_ended(winner: Unit.Team) -> void:
	if game_over_label:
		var winner_text: String = Loc.t("ui.battle.player_wins") if winner == Unit.Team.PLAYER else Loc.t("ui.battle.enemy_wins")
		game_over_label.text = winner_text
		game_over_label.visible = true

	# Show restart button
	if restart_button:
		restart_button.visible = true

func _on_restart_pressed() -> void:
	if game_controller and game_controller.has_method("restart_game"):
		game_controller.call("restart_game")
