extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

@export var timer_label: Label
@export var player_mana_label: Label
@export var game_over_label: Label

var game_controller: GameController
var player_summoner: Summoner

func _ready() -> void:
	# Find nodes if not assigned
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if player_mana_label == null:
		player_mana_label = get_node_or_null("PlayerManaLabel")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")

	# Find game controller and summoner
	game_controller = get_tree().get_first_node_in_group("game_controller")
	player_summoner = get_tree().get_first_node_in_group("player_summoners")

	# Connect signals
	if game_controller:
		game_controller.time_updated.connect(_on_time_updated)
		game_controller.game_ended.connect(_on_game_ended)

	if player_summoner:
		player_summoner.mana_changed.connect(_on_mana_changed)

func _on_time_updated(remaining: float) -> void:
	if timer_label:
		var minutes = int(remaining) / 60
		var seconds = int(remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_label:
		player_mana_label.text = "Mana: %d/%d" % [int(current), int(maximum)]

func _on_game_ended(winner: int) -> void:
	if game_over_label:
		var winner_text = "PLAYER WINS!" if winner == Unit.Team.PLAYER else "ENEMY WINS!"
		game_over_label.text = winner_text
		game_over_label.visible = true
