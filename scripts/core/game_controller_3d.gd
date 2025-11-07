extends Node3D
class_name GameController3D

## 3D Game Controller for 2.5D battlefield
## Manages match flow, timers, victory conditions

enum GameState { SETUP, PLAYING, PAUSED, GAME_OVER }

@export var match_duration: float = 180.0
@export var overtime_duration: float = 60.0

@export var battlefield: Node3D
@export var player_summoner: Summoner3D
@export var enemy_summoner: Summoner3D

var player_base: Node3D
var enemy_base: Node3D

var current_state: GameState = GameState.SETUP
var match_time: float = 0.0
var is_overtime: bool = false

signal game_started()
signal game_ended(winner: Unit3D.Team)
signal time_updated(remaining: float)
signal state_changed(new_state: GameState)

func _ready() -> void:
	add_to_group("game_controller")

	if battlefield == null:
		battlefield = get_node_or_null("Battlefield3D")

	if player_summoner == null:
		player_summoner = get_tree().get_first_node_in_group("player_summoners")
	if enemy_summoner == null:
		enemy_summoner = get_tree().get_first_node_in_group("enemy_summoners")

	if player_summoner and player_summoner.has_signal("summoner_died"):
		player_summoner.summoner_died.connect(_on_summoner_died)
	if enemy_summoner and enemy_summoner.has_signal("summoner_died"):
		enemy_summoner.summoner_died.connect(_on_summoner_died)

	await get_tree().process_frame
	var player_bases = get_tree().get_nodes_in_group("player_base")
	var enemy_bases = get_tree().get_nodes_in_group("enemy_base")

	if player_bases.size() > 0:
		player_base = player_bases[0]
		if player_base.has_signal("base_damaged"):
			player_base.base_damaged.connect(_on_base_damaged)
		if player_base.has_signal("base_destroyed"):
			player_base.base_destroyed.connect(_on_base_destroyed)
		print("Found player base")

	if enemy_bases.size() > 0:
		enemy_base = enemy_bases[0]
		if enemy_base.has_signal("base_damaged"):
			enemy_base.base_damaged.connect(_on_base_damaged)
		if enemy_base.has_signal("base_destroyed"):
			enemy_base.base_destroyed.connect(_on_base_destroyed)
		print("Found enemy base")

	# Load AI for enemy summoner from campaign config
	_load_ai_for_enemy()

	call_deferred("start_game")

func _process(delta: float) -> void:
	if current_state != GameState.PLAYING:
		return

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

func start_game() -> void:
	current_state = GameState.PLAYING
	match_time = 0.0
	game_started.emit()
	state_changed.emit(current_state)
	print("3D Match started! Duration: %d seconds" % match_duration)

func pause_game() -> void:
	if current_state == GameState.PLAYING:
		current_state = GameState.PAUSED
		get_tree().paused = true
		state_changed.emit(current_state)

func resume_game() -> void:
	if current_state == GameState.PAUSED:
		current_state = GameState.PLAYING
		get_tree().paused = false
		state_changed.emit(current_state)

func restart_game() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

func end_game(winner: Unit3D.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)
	get_tree().paused = true

	var winner_text = "PLAYER" if winner == Unit3D.Team.PLAYER else "ENEMY"
	print("Game Over! Winner: %s" % winner_text)

	# Check if this is a campaign battle
	var profile_repo = get_node_or_null("/root/ProfileRepo")
	if profile_repo:
		var profile = profile_repo.get_active_profile()
		var current_battle_id = profile.get("campaign_progress", {}).get("current_battle", "")

		if current_battle_id != "":
			# This is a campaign battle - transition to appropriate screen
			if winner == Unit3D.Team.PLAYER:
				print("GameController3D: Campaign battle won! Transitioning to reward screen...")
				await get_tree().create_timer(2.0).timeout
				get_tree().paused = false
				get_tree().change_scene_to_file("res://scenes/ui/reward_screen.tscn")
			else:
				print("GameController3D: Campaign battle lost. Returning to campaign...")
				await get_tree().create_timer(2.0).timeout
				get_tree().paused = false
				get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")

func _on_summoner_died(summoner: Summoner3D) -> void:
	if summoner == player_summoner:
		end_game(Unit3D.Team.ENEMY)
	elif summoner == enemy_summoner:
		end_game(Unit3D.Team.PLAYER)

func _check_timeout_victory() -> void:
	# Simplified: player wins on timeout for now
	end_game(Unit3D.Team.PLAYER)

func _check_overtime_victory() -> void:
	end_game(Unit3D.Team.PLAYER)

func get_time_remaining() -> float:
	if is_overtime:
		return overtime_duration - (match_time - match_duration)
	return match_duration - match_time

func get_time_string() -> String:
	var remaining = get_time_remaining()
	var minutes = int(remaining) / 60
	var seconds = int(remaining) % 60
	return "%02d:%02d" % [minutes, seconds]

func _on_time_updated(time_remaining: float) -> void:
	var time_label = get_node_or_null("UI/TimerLabel")
	if time_label:
		time_label.text = get_time_string()

func _on_game_ended(winner: Unit3D.Team) -> void:
	var game_over_label = get_node_or_null("UI/GameOverLabel")
	if game_over_label:
		game_over_label.visible = true
		if winner == Unit3D.Team.PLAYER:
			game_over_label.text = "VICTORY!"
		else:
			game_over_label.text = "DEFEAT"

func _on_base_damaged(_base, _damage: float) -> void:
	_update_hp_labels()

func _on_base_destroyed(base) -> void:
	if base == player_base:
		end_game(Unit3D.Team.ENEMY)
	elif base == enemy_base:
		end_game(Unit3D.Team.PLAYER)

func _update_hp_labels() -> void:
	var player_hp_label = get_node_or_null("UI/PlayerHPLabel")
	if player_hp_label and player_base:
		player_hp_label.text = "Player Base: %d/%d" % [player_base.current_hp, player_base.max_hp]

	var enemy_hp_label = get_node_or_null("UI/EnemyHPLabel")
	if enemy_hp_label and enemy_base:
		enemy_hp_label.text = "Enemy Base: %d/%d" % [enemy_base.current_hp, enemy_base.max_hp]

func _load_ai_for_enemy() -> void:
	if not enemy_summoner:
		return

	# Get battle config from profile
	var profile_repo = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		print("GameController3D: No ProfileRepo found, skipping AI load")
		return

	var profile = profile_repo.get_active_profile()
	var current_battle_id = profile.get("campaign_progress", {}).get("current_battle", "")
	if current_battle_id == "":
		print("GameController3D: No current battle set, skipping AI load")
		return

	var campaign = get_node_or_null("/root/CampaignService")
	if not campaign:
		print("GameController3D: No CampaignService found, skipping AI load")
		return

	var battle_config = campaign.get_battle(current_battle_id)
	if not battle_config:
		print("GameController3D: No battle config for '%s', skipping AI load" % current_battle_id)
		return

	# Remove existing AI (if any)
	for child in enemy_summoner.get_children():
		if child.has_method("decide_next_play"):  # Duck-type check for AI
			print("GameController3D: Removing old AI: %s" % child.name)
			child.queue_free()

	# Create and attach new AI
	const AILoader = preload("res://scripts/ai/ai_loader.gd")
	var ai = AILoader.create_ai_for_battle(battle_config, enemy_summoner)
	if ai:
		enemy_summoner.add_child(ai)
		print("GameController3D: Loaded %s AI for battle '%s'" % [battle_config.get("ai_type", "unknown"), current_battle_id])
