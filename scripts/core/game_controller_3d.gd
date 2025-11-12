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
	var player_bases: Array = get_tree().get_nodes_in_group("player_base")
	var enemy_bases: Array = get_tree().get_nodes_in_group("enemy_base")

	if player_bases.size() > 0:
		player_base = player_bases[0]
		if player_base.has_signal("base_damaged"):
			player_base.base_damaged.connect(_on_base_damaged)
		if player_base.has_signal("base_destroyed"):
			player_base.base_destroyed.connect(_on_base_destroyed)

	if enemy_bases.size() > 0:
		enemy_base = enemy_bases[0]
		if enemy_base.has_signal("base_damaged"):
			enemy_base.base_damaged.connect(_on_base_damaged)
		if enemy_base.has_signal("base_destroyed"):
			enemy_base.base_destroyed.connect(_on_base_destroyed)

	# Load AI for enemy summoner from campaign config
	_load_ai_for_enemy()

	# Register hero modifier provider
	_register_hero_provider()

	call_deferred("start_game")

func _exit_tree() -> void:
	# Cleanup: unregister hero provider to prevent memory leak
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	if modifier_system:
		modifier_system.unregister_provider("hero")

func _process(delta: float) -> void:
	if current_state != GameState.PLAYING:
		return

	match_time += delta
	var remaining: float = match_duration - match_time

	if not is_overtime:
		time_updated.emit(remaining)
		if remaining <= 0:
			_check_timeout_victory()
	else:
		var overtime_remaining: float = overtime_duration - (match_time - match_duration)
		time_updated.emit(overtime_remaining)
		if overtime_remaining <= 0:
			_check_overtime_victory()

func start_game() -> void:
	current_state = GameState.PLAYING
	match_time = 0.0
	game_started.emit()
	state_changed.emit(current_state)

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

	# Delegate to BattleContext for mode-specific completion handling
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.completion_callback.is_valid():
		await get_tree().create_timer(2.0, true).timeout  # process_always=true to run while paused
		get_tree().paused = false
		battle_context.completion_callback.call(winner as int)

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
	var remaining: float = get_time_remaining()
	var minutes: int = int(remaining) / 60
	var seconds: int = int(remaining) % 60
	return "%02d:%02d" % [minutes, seconds]

func _on_time_updated(_time_remaining: float) -> void:
	var time_label: Node = get_node_or_null("UI/TimerLabel")
	if time_label:
		time_label.text = get_time_string()

func _on_game_ended(winner: Unit3D.Team) -> void:
	# Show game over label
	var game_over_label: Node = get_node_or_null("UI/GameOverLabel")
	if game_over_label:
		if winner == Unit3D.Team.PLAYER:
			game_over_label.text = "VICTORY!"
			game_over_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
		else:
			game_over_label.text = "DEFEAT"
			game_over_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))
		game_over_label.visible = true

func _on_base_damaged(_base: Variant, _damage: float) -> void:
	_update_hp_labels()

func _on_base_destroyed(base: Variant) -> void:
	if base == player_base:
		end_game(Unit3D.Team.ENEMY)
	elif base == enemy_base:
		end_game(Unit3D.Team.PLAYER)

func _update_hp_labels() -> void:
	var player_hp_label: Node = get_node_or_null("UI/PlayerHPLabel")
	if player_hp_label and player_base:
		player_hp_label.text = "Player Base: %d/%d" % [player_base.current_hp, player_base.max_hp]

	var enemy_hp_label: Node = get_node_or_null("UI/EnemyHPLabel")
	if enemy_hp_label and enemy_base:
		enemy_hp_label.text = "Enemy Base: %d/%d" % [enemy_base.current_hp, enemy_base.max_hp]

func _load_ai_for_enemy() -> void:
	if not enemy_summoner:
		return

	# Get battle config from BattleContext
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if not battle_context:
		push_error("GameController3D: BattleContext not found")
		return

	var battle_config: Dictionary = battle_context.battle_config
	if battle_config.is_empty():
		push_error("GameController3D: Battle config is empty")
		return

	# Remove existing AI (if any)
	for child: Node in enemy_summoner.get_children():
		if child.has_method("decide_next_play"):  # Duck-type check for AI
			child.queue_free()

	# Create and attach new AI
	const AILoaderScript: GDScript = preload("res://scripts/ai/ai_loader.gd")
	var ai: Node = AILoaderScript.create_ai_for_battle(battle_config, enemy_summoner)
	if ai:
		enemy_summoner.add_child(ai)
	else:
		push_error("GameController3D: Failed to create AI!")

func _register_hero_provider() -> void:
	# Get hero from profile
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		push_warning("GameController3D: ProfileRepo not found, no hero bonuses will apply")
		return

	var profile: Dictionary = profile_repo.get_active_profile()
	if profile.is_empty():
		push_warning("GameController3D: No active profile, no hero bonuses will apply")
		return

	var hero_id: String = profile.get("meta", {}).get("selected_hero", "")
	if hero_id.is_empty():
		push_warning("GameController3D: No hero selected, no hero bonuses will apply")
		return

	# Register hero modifier provider
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	if not modifier_system:
		push_error("GameController3D: ModifierSystem not found!")
		return

	# Create and register hero provider
	var hero_provider: HeroModifierProvider = HeroModifierProvider.new(hero_id)
	modifier_system.register_provider("hero", hero_provider)
