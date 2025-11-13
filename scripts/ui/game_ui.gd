extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

@export var timer_label: Label = null
@export var player_mana_label: Label = null
@export var game_over_label: Label = null
@export var restart_button: Button = null

var game_controller: GameController = null
var player_summoner: Summoner = null

func _ready() -> void:
	# Find nodes if not assigned
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if player_mana_label == null:
		player_mana_label = get_node_or_null("PlayerManaLabel")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")
	if restart_button == null:
		restart_button = get_node_or_null("RestartButton")

	# Connect restart button
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		restart_button.visible = false  # Hidden until game over

	# Wait a frame for all nodes to be ready
	await get_tree().process_frame

	# Find game controller and summoner
	game_controller = get_tree().get_first_node_in_group("game_controller")

	# Find the actual Summoner node (not Base) in player_summoners group
	var summoners: Array[Node] = get_tree().get_nodes_in_group("summoners")
	for node: Node in summoners:
		if node is Summoner:
			var summoner: Summoner = node as Summoner
			if summoner.team == Unit.Team.PLAYER:
				player_summoner = summoner
				break

	# Connect signals
	if game_controller:
		game_controller.time_updated.connect(_on_time_updated)
		game_controller.game_ended.connect(_on_game_ended)
		print("GameUI: Connected to GameController")
	else:
		push_error("GameUI: Could not find game_controller!")

	if player_summoner:
		player_summoner.mana_changed.connect(_on_mana_changed)
		print("GameUI: Connected to PlayerSummoner")
	else:
		push_error("GameUI: Could not find player Summoner!")

func _on_time_updated(remaining: float) -> void:
	if timer_label:
		var minutes = int(remaining) // 60
		var seconds = int(remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_label:
		player_mana_label.text = "Mana: %d/%d" % [int(current), int(maximum)]

func _on_game_ended(winner: Unit.Team) -> void:
	if game_over_label:
		var winner_text = "PLAYER WINS!" if winner == Unit.Team.PLAYER else "ENEMY WINS!"
		game_over_label.text = winner_text
		game_over_label.visible = true

	# Show restart button
	if restart_button:
		restart_button.visible = true

func _on_restart_pressed() -> void:
	if game_controller:
		game_controller.restart_game()
