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

var player_base: Base  # Base object
var enemy_base: Base   # Base object

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

	# Connect summoner death signals (for backward compatibility)
	if player_summoner and player_summoner.has_signal("summoner_died"):
		player_summoner.summoner_died.connect(_on_summoner_died)
	if enemy_summoner and enemy_summoner.has_signal("summoner_died"):
		enemy_summoner.summoner_died.connect(_on_summoner_died)

	# Connect base destruction signals
	await get_tree().process_frame  # Wait for bases to be ready
	var player_bases = get_tree().get_nodes_in_group("player_bases")
	var enemy_bases = get_tree().get_nodes_in_group("enemy_bases")

	for base in player_bases:
		if base.has_signal("base_destroyed"):
			base.base_destroyed.connect(_on_base_destroyed)
			player_base = base
			print("Connected to player base")

	for base in enemy_bases:
		if base.has_signal("base_destroyed"):
			base.base_destroyed.connect(_on_base_destroyed)
			enemy_base = base
			print("Connected to enemy base")

	# Setup AI for campaign battles
	_setup_campaign_ai()

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

## Restart the current scene
func restart_game() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

## End the game with a winner
func end_game(winner: Unit.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)

	# Pause the game
	get_tree().paused = true

	var winner_text = "PLAYER" if winner == Unit.Team.PLAYER else "ENEMY"
	print("Game Over! Winner: %s" % winner_text)

	# Check if this is a campaign battle
	_handle_campaign_victory(winner)

## Handle summoner death (backward compatibility)
func _on_summoner_died(summoner: Summoner) -> void:
	if summoner == player_summoner:
		end_game(Unit.Team.ENEMY)
	elif summoner == enemy_summoner:
		end_game(Unit.Team.PLAYER)

## Handle base destruction
func _on_base_destroyed(base: Base) -> void:
	print("Base destroyed! Team: ", base.team)
	if base.team == Base.Team.PLAYER:
		end_game(Unit.Team.ENEMY)
	else:
		end_game(Unit.Team.PLAYER)

## Check victory when time runs out
func _check_timeout_victory() -> void:
	var player_hp = player_base.current_hp if player_base else 0
	var enemy_hp = enemy_base.current_hp if enemy_base else 0

	if player_hp > enemy_hp:
		end_game(Unit.Team.PLAYER)
	elif enemy_hp > player_hp:
		end_game(Unit.Team.ENEMY)
	else:
		# Tied - enter overtime
		is_overtime = true
		print("Overtime! First blood wins!")

## Check victory in overtime
func _check_overtime_victory() -> void:
	# If still tied after overtime, higher HP wins
	var player_hp = player_base.current_hp if player_base else 0
	var enemy_hp = enemy_base.current_hp if enemy_base else 0

	if player_hp > enemy_hp:
		end_game(Unit.Team.PLAYER)
	elif enemy_hp > player_hp:
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

## Handle campaign battle victory
func _handle_campaign_victory(winner: Unit.Team) -> void:
	# Check if this is a campaign battle
	var profile_repo = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		return

	var profile = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	var current_battle = profile.get("campaign_progress", {}).get("current_battle", "")
	if current_battle == "":
		# Not a campaign battle, no special handling
		return

	# This is a campaign battle!
	if winner == Unit.Team.PLAYER:
		# Player won - transition to reward screen after a delay
		print("GameController: Campaign battle won! Transitioning to reward screen...")
		await get_tree().create_timer(2.0).timeout  # Show victory for 2 seconds
		get_tree().paused = false
		get_tree().change_scene_to_file("res://scenes/ui/reward_screen.tscn")
	else:
		# Player lost - return to campaign screen
		print("GameController: Campaign battle lost. Returning to campaign...")
		await get_tree().create_timer(2.0).timeout
		get_tree().paused = false
		get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")

## Setup AI for campaign battles
func _setup_campaign_ai() -> void:
	if not enemy_summoner:
		return

	# Check if this is a campaign battle
	var profile_repo = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		return

	var profile = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	var current_battle_id = profile.get("campaign_progress", {}).get("current_battle", "")
	if current_battle_id == "":
		return  # Not a campaign battle

	# Load battle config
	var campaign = get_node_or_null("/root/Campaign")
	if not campaign:
		return

	var battle_config = campaign.get_battle(current_battle_id)
	if battle_config.is_empty():
		return

	# Remove existing AI (SimpleAI from scene)
	for child in enemy_summoner.get_children():
		if child is AIController or child.get_script() == preload("res://scripts/core/simple_ai.gd"):
			print("GameController: Removing old AI: %s" % child.name)
			child.queue_free()

	# Create and attach new AI
	var ai = AILoader.create_ai_for_battle(battle_config, enemy_summoner)
	if ai:
		enemy_summoner.add_child(ai)
		print("GameController: Loaded %s AI for battle '%s'" % [battle_config.get("ai_type", "unknown"), current_battle_id])
