extends Node2D
class_name GameController

## Manages match flow, timers, victory conditions, and game state
## This is the main game manager for a 3-5 minute match

enum GameState { SETUP, PLAYING, PAUSED, GAME_OVER }

## Match settings
@export var match_duration: float = 180.0  # 3 minutes in seconds
@export var overtime_duration: float = 60.0  # Extra time if tied

## References
@export var battlefield: Node2D
@export var player_summoner: Summoner
@export var enemy_summoner: Summoner

## State
var current_state: GameState = GameState.SETUP
var match_time: float = 0.0
var is_overtime: bool = false

## Signals
signal game_started()
signal game_ended(winner: Unit.Team)
signal time_updated(remaining: float)
signal state_changed(new_state: GameState)

func _ready() -> void:
	add_to_group("game_controller")

	# Find or create battlefield
	if battlefield == null:
		battlefield = get_node_or_null("Battlefield")

	# Find summoners
	if player_summoner == null:
		player_summoner = get_tree().get_first_node_in_group("player_summoners")
	if enemy_summoner == null:
		enemy_summoner = get_tree().get_first_node_in_group("enemy_summoners")

	# Connect summoner death signals
	if player_summoner:
		player_summoner.summoner_died.connect(_on_summoner_died)
	if enemy_summoner:
		enemy_summoner.summoner_died.connect(_on_summoner_died)

	# Start the match
	call_deferred("start_game")

func _process(delta: float) -> void:
	if current_state != GameState.PLAYING:
		return

	# Update match timer
	match_time += delta
	var remaining = match_duration - match_time

	if not is_overtime:
		time_updated.emit(remaining)

		if remaining <= 0:
			_check_timeout_victory()
	else:
		var overtime_remaining = overtime_duration - (match_time - match_duration)
		time_updated.emit(overtime_remaining)

		if overtime_remaining <= 0:
			_check_overtime_victory()

## Start the match
func start_game() -> void:
	current_state = GameState.PLAYING
	match_time = 0.0
	game_started.emit()
	state_changed.emit(current_state)
	print("Match started! Duration: %d seconds" % match_duration)

## Pause the game
func pause_game() -> void:
	if current_state == GameState.PLAYING:
		current_state = GameState.PAUSED
		get_tree().paused = true
		state_changed.emit(current_state)

## Resume the game
func resume_game() -> void:
	if current_state == GameState.PAUSED:
		current_state = GameState.PLAYING
		get_tree().paused = false
		state_changed.emit(current_state)

## End the game with a winner
func end_game(winner: Unit.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)

	var winner_text = "PLAYER" if winner == Unit.Team.PLAYER else "ENEMY"
	print("Game Over! Winner: %s" % winner_text)

## Handle summoner death
func _on_summoner_died(summoner: Summoner) -> void:
	if summoner == player_summoner:
		end_game(Unit.Team.ENEMY)
	elif summoner == enemy_summoner:
		end_game(Unit.Team.PLAYER)

## Check victory when time runs out
func _check_timeout_victory() -> void:
	if player_summoner.current_hp > enemy_summoner.current_hp:
		end_game(Unit.Team.PLAYER)
	elif enemy_summoner.current_hp > player_summoner.current_hp:
		end_game(Unit.Team.ENEMY)
	else:
		# Tied - enter overtime
		is_overtime = true
		print("Overtime! First blood wins!")

## Check victory in overtime
func _check_overtime_victory() -> void:
	# If still tied after overtime, higher HP wins
	if player_summoner.current_hp > enemy_summoner.current_hp:
		end_game(Unit.Team.PLAYER)
	elif enemy_summoner.current_hp > player_summoner.current_hp:
		end_game(Unit.Team.ENEMY)
	else:
		# Still tied? Player wins by default
		end_game(Unit.Team.PLAYER)

## Helper: Get current match time remaining
func get_time_remaining() -> float:
	if is_overtime:
		return overtime_duration - (match_time - match_duration)
	return match_duration - match_time

## Helper: Format time as MM:SS
func get_time_string() -> String:
	var remaining = get_time_remaining()
	var minutes = int(remaining) / 60
	var seconds = int(remaining) % 60
	return "%02d:%02d" % [minutes, seconds]
