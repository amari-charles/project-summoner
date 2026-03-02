extends Node3D
class_name GameController3D

## 3D Game Controller for 2.5D battlefield
## Manages match flow, timers, victory conditions

enum GameState { SETUP, PLAYING, PAUSED, GAME_OVER }
enum BattlePhase { PREPARATION, BATTLE }  ## Two-phase battle system

@export var match_duration: float = 180.0
@export var overtime_duration: float = 60.0
@export var preparation_duration: float = 30.0  ## Duration of PREPARATION phase (seconds)

@export var battlefield: Node3D
@export var player_summoner: Summoner
@export var enemy_summoner: Summoner

## Max frames to wait for a single scene to load (~5 seconds at 60fps)
const SCENE_LOAD_TIMEOUT_FRAMES: int = 300

var current_state: GameState = GameState.SETUP
var current_phase: BattlePhase = BattlePhase.PREPARATION
var match_time: float = 0.0
var prep_time_remaining: float = 0.0
var is_overtime: bool = false

## =============================================================================
## WIN CONDITION SYSTEM
## =============================================================================

## Current win condition type (from battle config)
var win_condition: StringName = WinConditionIDs.DEFAULT

## Time limit for timed win conditions (seconds, 0 = no limit)
var win_condition_time_limit: float = 0.0

## Kill target for KILL_COUNT win condition
var win_condition_kill_target: int = 0

## Current kill count for KILL_COUNT tracking
var _enemy_kill_count: int = 0

signal game_started()
signal game_ended(winner: UnitConstants.Team)
signal time_updated(remaining: float)
signal state_changed(new_state: GameState)
signal initialization_complete()  ## Emitted when all battle systems are ready
signal objective_progress(current: int, target: int)  ## For kill count objectives

## Battle phase signals (two-phase system)
signal phase_changed(new_phase: BattlePhase)
signal prep_timer_updated(remaining: float)

func _ready() -> void:

	# Add to groups for discovery
	add_to_group(GroupIDs.GAME_CONTROLLER)
	add_to_group("battle_coordinator")  # For SceneCoordinator to find us

	# Validate BattleContext
	if not BattleContext.is_configured():
		push_error("BattleCoordinator: BattleContext was NEVER configured!")
		push_error("BattleCoordinator: Did you run the battle scene directly (F6)?")
		push_error("BattleCoordinator: Configuring with practice mode defaults...")
		BattleContext.configure_practice_battle()

	# Reset all battle state before initialization
	reset_battle_state()

	# Wait one frame for all scene nodes to be in tree
	await get_tree().process_frame

	# =============================================================================
	# EXPLICIT INITIALIZATION PHASES
	# =============================================================================

	# Phase 1: Initialize battlefield
	if battlefield == null:
		battlefield = get_node_or_null("Battlefield3D")

	# Phase 1.5: Preload unit scenes asynchronously to prevent first-spawn delays
	await _preload_unit_scenes()

	# Phase 1.75: Initialize win conditions (must happen before SimulationNode so params are available)
	_init_win_conditions()

	# Phase 1.8: Initialize SimulationNode (must exist before summoner init)
	# Seed comes from BattleContext — no MatchSession dependency
	_init_simulation_node()

	# Phase 1.9: Initialize EntityManager (view layer wiring)
	_init_entity_manager()

	# Phase 2: Initialize summoners (summoners are now the attack targets)
	_init_summoners()
	_connect_summoner_combat_signals()

	# Phase 4: Initialize AI
	_load_ai_for_enemy()

	# Phase 6: Initialize UI components
	_init_ui()

	# Phase 6.5: Set up multiplayer transport and MatchSession (if multiplayer)
	# SimulationNode already exists (Phase 1.8), so runners wire up normally in Initialize()
	_setup_multiplayer()

	# =============================================================================
	# INITIALIZATION COMPLETE
	# =============================================================================

	initialization_complete.emit()

	# Start the game — client waits for first snapshot from host
	var is_mp_client: bool = BattleContext.is_multiplayer_battle() and not BattleContext.has_authority()
	if is_mp_client:
		var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
		if sim_node and sim_node.has_signal("FirstSnapshotApplied"):
			await sim_node.FirstSnapshotApplied
	start_game()

## Preload all unit scenes asynchronously to prevent first-spawn initialization delays.
## Uses ResourceLoader.load_threaded_request() to load in background without blocking.
## Instantiates and immediately frees each scene to force Godot to cache resources.
func _preload_unit_scenes() -> void:
	# Gather all unique unit scene paths
	var scene_paths: Array[String] = []
	for card_id: String in CardCatalog.get_all_card_ids():
		var card_def: Dictionary = CardCatalog.get_card(card_id)
		if card_def.get("type") == "summon":
			var unit_scene_path: String = card_def.get("unit_scene", "")
			if unit_scene_path != "" and not scene_paths.has(unit_scene_path):
				scene_paths.append(unit_scene_path)

	if scene_paths.is_empty():
		return

	# Start async loading for all scenes
	for path: String in scene_paths:
		ResourceLoader.load_threaded_request(path, "PackedScene")

	# Wait for all scenes to finish loading
	var preloaded_count: int = 0
	for path: String in scene_paths:
		# Wait for this scene to finish loading (with timeout protection)
		var frames_waited: int = 0
		while ResourceLoader.load_threaded_get_status(path) == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
			await get_tree().process_frame
			frames_waited += 1
			if frames_waited >= SCENE_LOAD_TIMEOUT_FRAMES:
				push_warning("BattleCoordinator: Timeout loading unit scene: %s" % path)
				break

		var status: ResourceLoader.ThreadLoadStatus = ResourceLoader.load_threaded_get_status(path)
		if status == ResourceLoader.THREAD_LOAD_LOADED:
			var scene: PackedScene = ResourceLoader.load_threaded_get(path)
			if scene:
				# Instantiate and free to cache resources (sub-resources, scripts, etc.)
				var instance: Node = scene.instantiate()
				instance.queue_free()
				preloaded_count += 1
		elif status == ResourceLoader.THREAD_LOAD_FAILED:
			push_warning("BattleCoordinator: Failed to load unit scene: %s" % path)


## Create and initialize the SimulationNode (must happen before summoner init)
func _init_simulation_node() -> void:
	var sim_script: Script = load("res://scripts/csharp/Simulation/SimulationNode.cs")
	if sim_script == null:
		push_error("BattleCoordinator: Failed to load SimulationNode script!")
		return

	var sim_node: Node = sim_script.new()
	add_child(sim_node)

	# Pass seed directly from BattleContext (no MatchSession dependency)
	var battle_seed: int = BattleContext.battle_config.get("battle_seed", 0)
	sim_node.Initialize(
		preparation_duration, match_duration, win_condition,
		win_condition_time_limit, win_condition_kill_target, battle_seed
	)

	# Belt-and-suspenders: set IsHost=false for client right at creation
	if BattleContext.is_multiplayer_battle() and not BattleContext.has_authority():
		sim_node.IsHost = false
		sim_node.LocalPlayerIndex = 1


## Initialize EntityManager (view layer bridge)
## Creates an EntityManager as a child and wires it to SimulationNode as IGameSession.
## EntityManager tracks unit/projectile shells but does NOT render them yet —
## the existing Unit3D/Projectile3D pipeline still handles rendering.
## Enable rendering via dev console when ready to test.
func _init_entity_manager() -> void:
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node == null:
		push_warning("BattleCoordinator: No SimulationNode found, skipping EntityManager init")
		return

	var em_script: Script = load("res://scripts/csharp/View/EntityManager.cs")
	if em_script == null:
		push_error("BattleCoordinator: Failed to load EntityManager script!")
		return

	var em: Node = em_script.new()
	em.name = "EntityManager"
	add_child(em)
	em.Initialize(sim_node)
	print("[GameController3D] EntityManager initialized with SimulationNode as IGameSession")


## Initialize summoners and connect their signals
func _init_summoners() -> void:
	if player_summoner == null:
		player_summoner = get_tree().get_first_node_in_group(GroupIDs.PLAYER_SUMMONERS)
	if enemy_summoner == null:
		enemy_summoner = get_tree().get_first_node_in_group(GroupIDs.ENEMY_SUMMONERS)

	var is_mp_client: bool = BattleContext.is_multiplayer_battle() and not BattleContext.has_authority()

	if is_mp_client:
		# Client path: lightweight init — no deck loading, no RegisterSummoner
		# Client receives all state from host snapshots
		if player_summoner and player_summoner.has_method("init_as_client"):
			player_summoner.init_as_client()
		if enemy_summoner and enemy_summoner.has_method("init_as_client"):
			enemy_summoner.init_as_client()
	else:
		# Host / single-player path: full init with deck loading and sim registration
		# Apply enemy stats BEFORE init() so _apply_summoner_bonuses() runs inside init()
		if enemy_summoner:
			if BattleContext.is_multiplayer_battle():
				# Multiplayer: reconstruct opponent's SummonerInstance from exchanged data
				var opponent_data: Dictionary = BattleContext.battle_config.get("opponent_summoner_data", {})
				if not opponent_data.is_empty():
					var opponent_instance: SummonerInstance = SummonerInstance.from_dict(opponent_data)
					if opponent_instance:
						enemy_summoner.set_summoner_instance(opponent_instance)
			elif BattleContext.battle_config.has("enemy_hp"):
				# Single-player: use configured enemy HP
				var custom_hp: float = BattleContext.battle_config.get("enemy_hp", 300.0)
				enemy_summoner.max_hp = custom_hp
				enemy_summoner.current_hp = custom_hp

		if player_summoner and player_summoner.has_method("init"):
			player_summoner.init()

		if enemy_summoner and enemy_summoner.has_method("init"):
			enemy_summoner.init()

		# Populate SimulationNode card data after both summoners registered
		var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
		if sim_node:
			sim_node.PopulateCardData()

## Connect summoner combat signals and sim game-over signal
func _connect_summoner_combat_signals() -> void:
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node:
		# GameOver is an ephemeral event — always use signal
		sim_node.connect("GameOver", _on_sim_game_over)

		# Host/single-player: use signals for phase/timer sync
		# Client: polls in _process() instead (more reliable than 10Hz snapshot signals)
		if not BattleContext.is_multiplayer_battle() or BattleContext.has_authority():
			sim_node.connect("PhaseChanged", _on_sim_phase_changed)
			sim_node.connect("PrepTimerUpdated", _on_sim_prep_timer_updated)
			sim_node.connect("MatchTimeUpdated", _on_sim_match_time_updated)
	else:
		push_warning("BattleCoordinator: No SimulationNode found for GameOver signal")

	# Legacy summoner_destroyed as fallback (for non-Battle phase or edge cases)
	if player_summoner:
		player_summoner.summoner_destroyed.connect(_on_summoner_destroyed)
	else:
		push_warning("BattleCoordinator: Could not find player_summoner")

	if enemy_summoner:
		enemy_summoner.summoner_destroyed.connect(_on_summoner_destroyed)
	else:
		push_warning("BattleCoordinator: Could not find enemy_summoner")

func _exit_tree() -> void:
	# Cleanup: disconnect kill tracking signal to prevent memory leak
	if get_tree().node_added.is_connected(_on_node_added_for_kill_tracking):
		get_tree().node_added.disconnect(_on_node_added_for_kill_tracking)

	# Cleanup: disconnect sim signals (only the ones that were connected)
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node and is_instance_valid(sim_node):
		if sim_node.is_connected("GameOver", _on_sim_game_over):
			sim_node.disconnect("GameOver", _on_sim_game_over)
		if sim_node.is_connected("PhaseChanged", _on_sim_phase_changed):
			sim_node.disconnect("PhaseChanged", _on_sim_phase_changed)
		if sim_node.is_connected("PrepTimerUpdated", _on_sim_prep_timer_updated):
			sim_node.disconnect("PrepTimerUpdated", _on_sim_prep_timer_updated)
		if sim_node.is_connected("MatchTimeUpdated", _on_sim_match_time_updated):
			sim_node.disconnect("MatchTimeUpdated", _on_sim_match_time_updated)


## Comprehensive battle state reset
## Clears all units, projectiles, HP bars from the scene
## Note: Autoload resets (EventSequencer, DialogueManager, etc.) are handled by SceneCoordinator
func reset_battle_state() -> void:
	# Clear all units from the battlefield
	_clear_all_units()

	# Reset game state
	current_state = GameState.SETUP
	current_phase = BattlePhase.PREPARATION
	match_time = 0.0
	prep_time_remaining = 0.0
	is_overtime = false

## Clear all unit instances from the battlefield
func _clear_all_units() -> void:
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	var cleared_count: int = 0

	for node: Node in units:
		if is_instance_valid(node):
			node.queue_free()
			cleared_count += 1

func _process(delta: float) -> void:
	if current_state != GameState.PLAYING:
		return

	# Multiplayer client: poll MatchState every frame (like Unit3D.ReadFromSimulation)
	if BattleContext.is_multiplayer_battle() and not BattleContext.has_authority():
		_poll_match_state()
		return

	# Host uses signals for phase/timer (connected in _connect_summoner_combat_signals)
	if BattleContext.is_multiplayer_battle():
		return

	# Single-player: local timers (SimulationNode also emits, but local timers
	# provide smoother per-frame updates without waiting for Tick intervals)
	if current_phase == BattlePhase.PREPARATION:
		_update_preparation_phase(delta)
		return

	# BATTLE phase - normal game flow
	match_time += delta

	# Handle timed win conditions
	if WinConditionIDs.has_time_limit(win_condition) and win_condition_time_limit > 0:
		var remaining: float = win_condition_time_limit - match_time
		time_updated.emit(remaining)

		if remaining <= 0:
			_handle_win_condition_timeout()
	else:
		# Default timer behavior (no time limit or DESTROY_BASE)
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


## Poll phase, prep timer, and match time from SimulationNode (multiplayer client only).
func _poll_match_state() -> void:
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if not sim_node:
		return

	# Poll phase
	var sim_phase: int = sim_node.GetPhase()
	# 0 = Preparation, 1 = Battle
	if sim_phase == 1 and current_phase == BattlePhase.PREPARATION:
		_start_battle_phase()

	# Poll prep timer (only during Preparation — hide is handled by phase_changed signal)
	if current_phase == BattlePhase.PREPARATION:
		prep_time_remaining = sim_node.GetPrepTimeRemaining()
		prep_timer_updated.emit(prep_time_remaining)

	# Poll match time and emit for UI
	match_time = sim_node.GetMatchTime()
	var remaining: float = match_duration - match_time
	time_updated.emit(remaining)

func start_game() -> void:
	current_state = GameState.PLAYING
	current_phase = BattlePhase.PREPARATION
	match_time = 0.0
	prep_time_remaining = preparation_duration

	# Mark battle as in progress in BattleContext
	BattleContext.start_battle()

	# Start battle music
	AudioManager.play_music(AudioManager.MUSIC_BATTLE)

	game_started.emit()
	state_changed.emit(current_state)
	phase_changed.emit(current_phase)
	prep_timer_updated.emit(prep_time_remaining)

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

func freeze_game() -> void:
	"""Freeze gameplay without activating pause state (for dialogues, cutscenes, etc)"""
	# Use tree pause instead of time_scale so UI with PROCESS_MODE_ALWAYS still works
	get_tree().paused = true

func unfreeze_game() -> void:
	"""Unfreeze gameplay and restore normal gameplay"""
	# Only unpause if we're not in PAUSED state (don't interfere with pause menu)
	if current_state != GameState.PAUSED:
		get_tree().paused = false

func restart_game() -> void:
	get_tree().paused = false
	# Reset battle state to CONFIGURED so start_battle() works correctly after reload
	BattleContext.battle_state = BattleContext.BattleState.CONFIGURED
	get_tree().reload_current_scene()

func end_game(winner: UnitConstants.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)
	get_tree().paused = true

	# Stop battle music
	AudioManager.stop_music()

	# In multiplayer, only authority broadcasts match end to clients
	if BattleContext.is_multiplayer_battle() and BattleContext.has_authority():
		_broadcast_match_end(winner)

	# Update BattleContext state based on winner
	if winner == UnitConstants.Team.PLAYER:
		BattleContext.end_battle_victory()
	else:
		BattleContext.end_battle_defeat()

	# Delegate to BattleContext for mode-specific completion handling
	var callback_variant: Variant = BattleContext.completion_callback
	if callback_variant is Callable:
		var callback: Callable = callback_variant
		if callback.is_valid():
			await get_tree().create_timer(2.0, true).timeout  # process_always=true to run while paused
			get_tree().paused = false
			callback.call(winner as int)


## Broadcast match end to clients via MatchSession (multiplayer only)
func _broadcast_match_end(winner: UnitConstants.Team) -> void:
	# Get MatchSession from authority provider
	if BattleContext.authority_provider == null:
		return

	var match_session: Node = BattleContext.authority_provider.get_match_session()
	if match_session == null:
		push_warning("GameController3D: No MatchSession found for multiplayer broadcast")
		return

	# Convert winner team to player index
	# Team.PLAYER = 0 means local player won (host is index 0)
	# Team.ENEMY = 1 means opponent won
	var winner_index: int = 0 if winner == UnitConstants.Team.PLAYER else 1

	# Determine reason based on how game ended
	var reason: String = "Summoner destroyed"

	# Call C# MatchSession.BroadcastMatchEnd()
	if match_session.has_method("BroadcastMatchEnd"):
		match_session.BroadcastMatchEnd(winner_index, reason)

func _check_timeout_victory() -> void:
	# Simplified: player wins on timeout for now
	end_game(UnitConstants.Team.PLAYER)

func _check_overtime_victory() -> void:
	end_game(UnitConstants.Team.PLAYER)

## =============================================================================
## TWO-PHASE BATTLE SYSTEM
## =============================================================================

## Update preparation phase timer
func _update_preparation_phase(delta: float) -> void:
	prep_time_remaining -= delta
	prep_timer_updated.emit(prep_time_remaining)

	if prep_time_remaining <= 0.0:
		_start_battle_phase()

## Transition from PREPARATION to BATTLE phase
func _start_battle_phase() -> void:
	current_phase = BattlePhase.BATTLE
	phase_changed.emit(current_phase)

	# Activate all units that were spawned during PREPARATION
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	var activated_count: int = 0

	for node: Node in units:
		# C# units use PascalCase properties
		if "IsAlive" in node and "Team" in node:
			var unit: Node3D = node
			# ActivationState enum: 0 = Inactive, 1 = Active
			if unit.get("ActivationState") == 0:
				unit.Activate()
				activated_count += 1



## Skip preparation phase immediately (debug)
func skip_prep_phase() -> void:
	if current_phase == BattlePhase.PREPARATION:
		prep_time_remaining = 0.0
		_start_battle_phase()
		var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
		if sim_node:
			sim_node.SkipPreparation()

func get_time_remaining() -> float:
	if is_overtime:
		return overtime_duration - (match_time - match_duration)
	return match_duration - match_time

func get_time_string() -> String:
	var remaining: float = get_time_remaining()
	var minutes: int = floori(remaining / 60.0)
	var seconds: int = int(remaining) % 60
	return "%02d:%02d" % [minutes, seconds]

func _on_game_ended(winner: UnitConstants.Team) -> void:
	# Show game over label
	var game_over_label: Node = get_node_or_null("UI/GameOverLabel")
	if game_over_label:
		if winner == UnitConstants.Team.PLAYER:
			game_over_label.set("text", "VICTORY!")
			if game_over_label.has_method("add_theme_color_override"):
				game_over_label.call("add_theme_color_override", "font_color", Color(0.3, 1.0, 0.3))
		else:
			game_over_label.set("text", "DEFEAT")
			if game_over_label.has_method("add_theme_color_override"):
				game_over_label.call("add_theme_color_override", "font_color", Color(1.0, 0.3, 0.3))
		game_over_label.set("visible", true)

## Sim emitted GameOver — the simulation determined a winner via IWinCondition.
## winner_team is already in local team space (SimulationNode.ToLocalTeam remaps).
func _on_sim_game_over(winner_team: int, reason: String) -> void:
	end_game(winner_team as UnitConstants.Team)

## Sim phase changed — update local phase from SimulationNode signal.
## Works for both host (EmitEvents) and client (ApplySnapshot).
func _on_sim_phase_changed(new_phase: int) -> void:
	# 1 = GamePhase.Battle
	if new_phase == 1 and current_phase == BattlePhase.PREPARATION:
		_start_battle_phase()

## Sim prep timer updated — sync local prep_time_remaining from SimulationNode.
func _on_sim_prep_timer_updated(remaining: float) -> void:
	prep_time_remaining = remaining
	prep_timer_updated.emit(remaining)

## Sim match time updated — sync local match_time from SimulationNode.
func _on_sim_match_time_updated(time: float) -> void:
	match_time = time

func _on_summoner_destroyed(summoner: Summoner) -> void:
	# During Battle, the sim handles win conditions via GameOver signal.
	# This legacy path remains for edge cases outside Battle phase.
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node and sim_node.GetPhase() == 1:  # 1 = GamePhase.Battle
		return

	# Only authority evaluates win conditions
	if not BattleContext.has_authority():
		return

	if summoner == player_summoner:
		end_game(UnitConstants.Team.ENEMY)
	elif summoner == enemy_summoner:
		end_game(UnitConstants.Team.PLAYER)

func _load_ai_for_enemy() -> void:
	if not enemy_summoner:
		return

	if BattleContext.is_multiplayer_battle():
		return

	# Get battle config from BattleContext
	var battle_config: Dictionary = BattleContext.battle_config
	if battle_config.is_empty():
		push_error("GameController3D: Battle config is empty")
		return

	# Remove existing AI (if any)
	for child: Node in enemy_summoner.get_children():
		if child.has_method("decide_next_play"):  # Duck-type check for AI
			child.queue_free()

	# Create and attach new AI
	const AILoaderScript: GDScript = preload("res://scripts/ai/ai_loader.gd")
	var ai: Node = AILoaderScript.call("create_ai_for_battle", battle_config, enemy_summoner)
	if ai:
		enemy_summoner.add_child(ai)
	else:
		push_error("GameController3D: Failed to create AI!")

## =============================================================================
## REDIRECT INPUT HANDLING
## =============================================================================

## State for redirect drag operation
var _redirect_drag_active: bool = false
var _redirect_start_point: Vector3 = Vector3.ZERO
var _redirect_selected_units: Array[Node3D] = []

## Store original modulate values for tinting
var _unit_original_modulates: Dictionary = {}

## Camera for raycasting (cached)
var _camera: Camera3D = null

## Visual indicator for redirect
var _redirect_indicator: RedirectIndicator = null

func _unhandled_input(event: InputEvent) -> void:
	# Only handle redirect input during active gameplay
	if current_state != GameState.PLAYING:
		return

	# Only process if redirect mode is active
	if RedirectManager.current_mode == RedirectManager.RedirectMode.NORMAL:
		return

	# Ensure we have camera for raycasting
	if not _camera:
		_camera = get_viewport().get_camera_3d()
		if not _camera:
			return

	# Handle mouse button press (start of redirect)
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event

		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			# Click detected - select units in radius
			var click_point: Vector3 = _get_battlefield_point_from_mouse(mouse_event.position)
			if click_point != Vector3.ZERO:
				_redirect_start_point = click_point
				_redirect_selected_units = RedirectManager.select_units_in_radius(
					click_point,
					RedirectManager.REDIRECT_RADIUS,
					player_summoner.team
				)
				_redirect_drag_active = true

				# Show visual indicator
				var indicator_color: Color = RedirectManager.get_current_mode_color()
				_redirect_indicator.show_selection_circle(click_point, indicator_color)

				# Tint selected units
				_apply_unit_tint(indicator_color)

		elif mouse_event.button_index == MOUSE_BUTTON_LEFT and not mouse_event.pressed and _redirect_drag_active:
			# Release detected - apply redirect
			var release_point: Vector3 = _get_battlefield_point_from_mouse(mouse_event.position)
			if release_point != Vector3.ZERO:
				_on_redirect_release(release_point)

			# Hide visual indicator
			_redirect_indicator.hide_selection_circle()

			# Restore unit colors
			_restore_unit_tint()

			_redirect_drag_active = false

	# Handle mouse motion (update drag arrow)
	if event is InputEventMouseMotion and _redirect_drag_active:
		var mouse_motion: InputEventMouseMotion = event
		var current_point: Vector3 = _get_battlefield_point_from_mouse(mouse_motion.position)
		if current_point != Vector3.ZERO:
			_redirect_indicator.update_drag(_redirect_start_point, current_point)

## Convert 2D screen position to 3D battlefield point via plane intersection
## Uses mathematical plane intersection instead of physics raycasting
func _get_battlefield_point_from_mouse(screen_pos: Vector2) -> Vector3:
	if not _camera:
		return Vector3.ZERO

	# Get ray from camera through mouse position
	var ray_origin: Vector3 = _camera.project_ray_origin(screen_pos)
	var ray_direction: Vector3 = _camera.project_ray_normal(screen_pos)

	# Intersect with Y=0 plane (battlefield ground level)
	# Formula: intersection_point = ray_origin + ray_direction * t
	# where t = (plane_y - ray_origin.y) / ray_direction.y
	var plane_y: float = 0.0

	# Check if ray is parallel to ground plane
	if abs(ray_direction.y) < 0.0001:
		return Vector3.ZERO

	# Calculate intersection distance
	var t: float = (plane_y - ray_origin.y) / ray_direction.y

	# Check if intersection is behind camera
	if t < 0:
		return Vector3.ZERO

	# Calculate and return intersection point
	return ray_origin + ray_direction * t

## Handle redirect release (apply forced targets)
func _on_redirect_release(release_point: Vector3) -> void:

	if _redirect_selected_units.is_empty():
		RedirectManager.cancel_redirect()
		return

	# Find nearest enemy at release point
	var target: Node3D = RedirectManager.find_nearest_enemy(
		release_point,
		player_summoner.team,
		RedirectManager.TARGET_SEARCH_RADIUS
	)

	if not target:
		RedirectManager.cancel_redirect()
		return


	# Apply forced targets
	RedirectManager.apply_forced_targets(
		_redirect_selected_units,
		target,
		RedirectManager.FORCED_TARGET_DURATION,
		release_point
	)


## Apply color tint to selected units
func _apply_unit_tint(tint_color: Color) -> void:
	_unit_original_modulates.clear()

	for unit: Node3D in _redirect_selected_units:
		if is_instance_valid(unit) and "visual_component" in unit and unit.visual_component:
			# Access sprite via duck typing (works with both GDScript and C# visual components)
			var visual: Variant = unit.visual_component
			if "character_sprite" in visual and visual.character_sprite != null:
				var sprite: Variant = visual.character_sprite
				# Store original modulate
				_unit_original_modulates[unit] = sprite.modulate

				# Apply tinted color (lighter version of the redirect color)
				sprite.modulate = tint_color.lightened(0.3)

## Restore original colors to selected units
func _restore_unit_tint() -> void:
	for unit: Node3D in _unit_original_modulates.keys():
		if is_instance_valid(unit) and "visual_component" in unit and unit.visual_component:
			# Access sprite via duck typing (works with both GDScript and C# visual components)
			var visual: Variant = unit.visual_component
			if "character_sprite" in visual and visual.character_sprite != null:
				var sprite: Variant = visual.character_sprite
				sprite.modulate = _unit_original_modulates[unit]

	_unit_original_modulates.clear()

## =============================================================================
## WIN CONDITION INITIALIZATION
## =============================================================================

## Initialize win conditions from battle config
func _init_win_conditions() -> void:
	# Reset state
	win_condition = WinConditionIDs.DEFAULT
	win_condition_time_limit = 0.0
	win_condition_kill_target = 0
	_enemy_kill_count = 0

	# Get win condition from battle config
	var config: Dictionary = BattleContext.battle_config
	if config.is_empty():
		return

	# Read win condition type
	var condition_str: String = config.get(&"win_condition", "")
	if not condition_str.is_empty() and WinConditionIDs.is_valid(condition_str):
		win_condition = StringName(condition_str)
	else:
		win_condition = WinConditionIDs.DEFAULT

	# Read time limit for timed conditions
	win_condition_time_limit = config.get(&"time_limit", 0.0)

	# Read kill target for KILL_COUNT
	win_condition_kill_target = config.get(&"kill_target", 0)

	# Validate configuration
	if win_condition == WinConditionIDs.KILL_COUNT and win_condition_kill_target <= 0:
		push_warning("BattleCoordinator: KILL_COUNT win condition has no kill_target set!")

	if WinConditionIDs.has_time_limit(win_condition) and win_condition_time_limit <= 0:
		push_warning("BattleCoordinator: Timed win condition has no time_limit set!")

	# Connect to unit death signal for kill counting
	if win_condition == WinConditionIDs.KILL_COUNT:
		_connect_unit_death_tracking()

## Handle timeout based on win condition type
func _handle_win_condition_timeout() -> void:
	if WinConditionIDs.timeout_is_win(win_condition):
		# Player wins by surviving the time limit (SURVIVE_TIME)
		end_game(UnitConstants.Team.PLAYER)
	elif WinConditionIDs.timeout_is_loss(win_condition):
		# Player loses if they didn't complete objective in time (TIMED_DESTROY)
		end_game(UnitConstants.Team.ENEMY)
	else:
		# Default: player wins on timeout
		_check_timeout_victory()

## Connect to unit death signals for kill count tracking
func _connect_unit_death_tracking() -> void:
	# Connect to any existing units
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	for node: Node in units:
		if _has_combat_properties(node):
			var unit: Node3D = node
			if not unit.unit_died.is_connected(_on_unit_died_for_kill_count):
				unit.unit_died.connect(_on_unit_died_for_kill_count)

	# Connect to future units via tree signal
	get_tree().node_added.connect(_on_node_added_for_kill_tracking)

## Track newly added units for kill counting
func _on_node_added_for_kill_tracking(node: Node) -> void:
	if win_condition != WinConditionIDs.KILL_COUNT:
		return

	if _has_combat_properties(node):
		var unit: Node3D = node
		if not unit.unit_died.is_connected(_on_unit_died_for_kill_count):
			unit.unit_died.connect(_on_unit_died_for_kill_count)

## Helper for duck typing (C# uses PascalCase, GDScript uses snake_case)
func _has_combat_properties(node: Node) -> bool:
	var has_alive: bool = "is_alive" in node or "IsAlive" in node
	var has_team: bool = "team" in node or "Team" in node
	return has_alive and has_team

## Handle unit death for kill count objective
func _on_unit_died_for_kill_count(unit: Node3D) -> void:
	if win_condition != WinConditionIDs.KILL_COUNT:
		return

	# Only authority tracks kill counts and evaluates win conditions
	if not BattleContext.has_authority():
		return

	# Only count enemy kills
	if unit.team == UnitConstants.Team.ENEMY:
		_enemy_kill_count += 1
		objective_progress.emit(_enemy_kill_count, win_condition_kill_target)

		# Check if objective met
		if _enemy_kill_count >= win_condition_kill_target:
			end_game(UnitConstants.Team.PLAYER)

## =============================================================================
## INITIALIZATION HELPERS
## =============================================================================

## Initialize UI components with proper dependencies
func _init_ui() -> void:
	# Create redirect indicator
	_redirect_indicator = RedirectIndicator.new()
	add_child(_redirect_indicator)

	# Find and initialize HandUI
	var hand_ui: Node = get_tree().get_first_node_in_group(GroupIDs.HAND_UI)
	if hand_ui and hand_ui.has_method("init"):
		hand_ui.init(player_summoner)
	else:
		push_warning("BattleCoordinator: HandUI not found or has no init() method")

	# Find and initialize GameUI
	var game_ui: Node = get_node_or_null("UI")
	if game_ui and game_ui.has_method("init"):
		game_ui.init(self, player_summoner, enemy_summoner)
	else:
		push_warning("BattleCoordinator: GameUI not found or has no init() method")

	# Find and initialize BattlefieldDropZone
	var drop_zone: Node = get_node_or_null("UI/BattlefieldDropZone")
	if drop_zone and drop_zone.has_method("init"):
		drop_zone.init(player_summoner)
	else:
		push_warning("BattleCoordinator: BattlefieldDropZone not found or has no init() method")


## Set up multiplayer transport and MatchSession (if this is a multiplayer battle)
func _setup_multiplayer() -> void:
	if not BattleContext.is_multiplayer_battle():
		return

	var config_dict: Dictionary = BattleContext.battle_config
	var is_host: bool = BattleContext.has_authority()
	var local_peer_id: int = 1 if is_host else 2
	var local_player_index: int = 0 if is_host else 1

	# Create NakamaMatchTransport
	var transport_script: Script = load("res://scripts/csharp/Multiplayer/Transport/NakamaMatchTransport.cs")
	if transport_script == null:
		push_error("BattleCoordinator: Failed to load NakamaMatchTransport script!")
		return

	var transport: Node = transport_script.new()
	add_child(transport)

	# Get match ID from NakamaGameClient
	var nakama: Node = get_tree().root.get_node_or_null("NakamaGameClient")
	var match_id: String = ""
	if nakama:
		var active_match_id: Variant = nakama.get("ActiveMatchId")
		if active_match_id != null:
			match_id = str(active_match_id)

	transport.Initialize(match_id, is_host, local_peer_id)

	# Create MatchSession
	var match_session_script: Script = load("res://scripts/csharp/Multiplayer/Core/MatchSession.cs")
	if match_session_script == null:
		push_error("BattleCoordinator: Failed to load MatchSession script!")
		return

	var match_session: Node = match_session_script.new()
	add_child(match_session)

	# Build match config
	var seed: int = config_dict.get("battle_seed", 0)
	var ranked_info: Dictionary = BattleContext.get_ranked_match_info()
	var local_user_id: String = ""
	var opponent_user_id: String = ranked_info.get("opponent_user_id", "")
	if nakama:
		var uid: Variant = nakama.get("UserId")
		if uid != null:
			local_user_id = str(uid)

	# Build player IDs array (index 0 = host, index 1 = client)
	var player_ids: Array = []
	if is_host:
		player_ids = [local_user_id, opponent_user_id]
	else:
		player_ids = [opponent_user_id, local_user_id]

	var summoner_ids: Array = [
		config_dict.get("player_summoner_id", "ignis"),
		config_dict.get("opponent_summoner_id", "ignis")
	]
	# If we're client, swap so index 0 = host's summoner
	if not is_host:
		summoner_ids = [summoner_ids[1], summoner_ids[0]]

	# Start the match (GDScript-callable wrapper)
	match_session.StartMatchFromGDScript(
		seed, match_id, player_ids, summoner_ids,
		transport, is_host, local_player_index
	)

	# Wire MatchSession into authority provider
	if BattleContext.authority_provider:
		BattleContext.authority_provider._match_session = match_session

	# Client: connect RemoteUnitSpawned signal for visual unit spawning
	if not is_host:
		var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
		if sim_node and sim_node.has_signal("RemoteUnitSpawned"):
			sim_node.RemoteUnitSpawned.connect(_on_remote_unit_spawned)



## Handle remote unit spawn from host (client-side only).
## EntityManager handles all unit rendering via MatchState diffing.
func _on_remote_unit_spawned(_catalog_id: String, _local_team: int, _local_position: Vector3, _network_id: int, _spawn_duration: float) -> void:
	pass
