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
signal game_ended(winner: Unit3D.Team)
signal time_updated(remaining: float)
signal state_changed(new_state: GameState)
signal initialization_complete()  ## Emitted when all battle systems are ready
signal objective_progress(current: int, target: int)  ## For kill count objectives

## Battle phase signals (two-phase system)
signal phase_changed(new_phase: BattlePhase)
signal prep_timer_updated(remaining: float)

func _ready() -> void:
	print("BattleCoordinator: Starting battle initialization...")

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
	print("BattleCoordinator: Phase 1 - Battlefield...")
	if battlefield == null:
		battlefield = get_node_or_null("Battlefield3D")
	print("BattleCoordinator: Phase 1 complete - Battlefield ready")

	# Phase 1.5: Preload unit scenes to prevent first-spawn delays
	print("BattleCoordinator: Phase 1.5 - Preloading unit scenes...")
	_preload_unit_scenes()
	print("BattleCoordinator: Phase 1.5 complete - Unit scenes cached")

	# Phase 2: Initialize summoners (summoners are now the attack targets)
	print("BattleCoordinator: Phase 2 - Summoners...")
	_init_summoners()
	_connect_summoner_combat_signals()
	print("BattleCoordinator: Phase 2 complete - Summoners ready")

	# Phase 3: Initialize win conditions
	print("BattleCoordinator: Phase 3 - Win conditions...")
	_init_win_conditions()
	print("BattleCoordinator: Phase 3 complete - Win conditions ready")

	# Phase 4: Initialize AI
	print("BattleCoordinator: Phase 4 - AI...")
	_load_ai_for_enemy()
	print("BattleCoordinator: Phase 4 complete - AI ready")

	# Phase 5: Initialize summoner modifiers
	print("BattleCoordinator: Phase 5 - Summoner modifiers...")
	_register_summoner_provider()
	print("BattleCoordinator: Phase 5 complete - Summoner modifiers ready")

	# Phase 6: Initialize UI components
	print("BattleCoordinator: Phase 6 - UI...")
	_init_ui()
	print("BattleCoordinator: Phase 6 complete - UI ready")

	# =============================================================================
	# INITIALIZATION COMPLETE
	# =============================================================================

	print("BattleCoordinator: All phases complete, emitting initialization_complete")
	initialization_complete.emit()

	# Start the game
	start_game()

## Preload all unit scenes to prevent first-spawn initialization delays
## Instantiates and immediately frees each unit scene to force Godot to cache resources
## NOTE: This is a synchronous stopgap that may cause brief stutter during battle load.
## See docs/todos.md "Add Loading Screen with Asset Preloading" for the async solution.
func _preload_unit_scenes() -> void:
	var preloaded_count: int = 0
	for card_id: String in CardCatalog.get_all_card_ids():
		var card_def: Dictionary = CardCatalog.get_card(card_id)
		if card_def.get("type") == "summon":
			var unit_scene_path: String = card_def.get("unit_scene", "")
			if unit_scene_path != "":
				var scene: PackedScene = load(unit_scene_path)
				if scene:
					var instance: Node = scene.instantiate()
					instance.queue_free()
					preloaded_count += 1
	print("BattleCoordinator: Preloaded %d unit scenes" % preloaded_count)

## Initialize summoners and connect their signals
func _init_summoners() -> void:
	if player_summoner == null:
		player_summoner = get_tree().get_first_node_in_group(GroupIDs.PLAYER_SUMMONERS)
	if enemy_summoner == null:
		enemy_summoner = get_tree().get_first_node_in_group(GroupIDs.ENEMY_SUMMONERS)

	# Call init() on summoners (synchronous - no need to await since signal emits during init())
	if player_summoner and player_summoner.has_method("init"):
		player_summoner.init()
		print("BattleCoordinator: Player summoner initialized")

	if enemy_summoner and enemy_summoner.has_method("init"):
		enemy_summoner.init()
		print("BattleCoordinator: Enemy summoner initialized")

## Connect summoner combat signals (summoner is now the attack target)
func _connect_summoner_combat_signals() -> void:
	if player_summoner:
		player_summoner.summoner_destroyed.connect(_on_summoner_destroyed)
	else:
		push_warning("BattleCoordinator: Could not find player_summoner")

	if enemy_summoner:
		enemy_summoner.summoner_destroyed.connect(_on_summoner_destroyed)
		# Apply enemy HP override from battle config (for tutorial/special battles)
		if BattleContext.battle_config.has("enemy_hp"):
			var custom_hp: float = BattleContext.battle_config.get("enemy_hp", 300.0)
			enemy_summoner.max_hp = custom_hp
			enemy_summoner.current_hp = custom_hp
			enemy_summoner.hp_changed.emit(custom_hp, custom_hp)
			print("BattleCoordinator: Overrode enemy summoner HP to %s" % custom_hp)
	else:
		push_warning("BattleCoordinator: Could not find enemy_summoner")

func _exit_tree() -> void:
	# Cleanup: unregister summoner provider to prevent memory leak
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	if modifier_system and modifier_system.has_method("unregister_provider"):
		modifier_system.call("unregister_provider", "summoner")

	# Cleanup: disconnect kill tracking signal to prevent memory leak
	if get_tree().node_added.is_connected(_on_node_added_for_kill_tracking):
		get_tree().node_added.disconnect(_on_node_added_for_kill_tracking)

## Comprehensive battle state reset
## Clears all units, projectiles, HP bars from the scene
## Note: Autoload resets (EventSequencer, DialogueManager, etc.) are handled by SceneCoordinator
func reset_battle_state() -> void:
	# Clear all active projectiles
	if ProjectileManager:
		ProjectileManager.clear_all_projectiles()

	# Clear all HP bars
	if HPBarManager:
		HPBarManager.clear_all_bars()

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

	if cleared_count > 0:
		print("  - Cleared %d units" % cleared_count)

func _process(delta: float) -> void:
	if current_state != GameState.PLAYING:
		return

	# Handle PREPARATION phase
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

func start_game() -> void:
	current_state = GameState.PLAYING
	current_phase = BattlePhase.PREPARATION
	match_time = 0.0
	prep_time_remaining = preparation_duration

	# Mark battle as in progress in BattleContext
	BattleContext.start_battle()

	# Start battle music
	AudioManager.play_music("res://resources/audio/bgm/battle.mp3")

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

func end_game(winner: Unit3D.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)
	get_tree().paused = true

	# Stop battle music
	AudioManager.stop_music()

	# Update BattleContext state based on winner
	if winner == Unit3D.Team.PLAYER:
		BattleContext.end_battle_victory()
	else:
		BattleContext.end_battle_defeat()

	# Delegate to BattleContext for mode-specific completion handling
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context:
		var callback_variant: Variant = battle_context.get("completion_callback")
		if callback_variant is Callable:
			var callback: Callable = callback_variant
			if callback.is_valid():
				await get_tree().create_timer(2.0, true).timeout  # process_always=true to run while paused
				get_tree().paused = false
				callback.call(winner as int)

func _check_timeout_victory() -> void:
	# Simplified: player wins on timeout for now
	end_game(Unit3D.Team.PLAYER)

func _check_overtime_victory() -> void:
	end_game(Unit3D.Team.PLAYER)

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
		if node is Unit3D:
			var unit: Unit3D = node
			if unit.activation_state == Unit3D.ActivationState.INACTIVE:
				unit.activate()
				activated_count += 1

	print("BattleCoordinator: Battle phase started - activated %d units" % activated_count)

func get_time_remaining() -> float:
	if is_overtime:
		return overtime_duration - (match_time - match_duration)
	return match_duration - match_time

func get_time_string() -> String:
	var remaining: float = get_time_remaining()
	var minutes: int = floori(remaining / 60.0)
	var seconds: int = int(remaining) % 60
	return "%02d:%02d" % [minutes, seconds]

func _on_game_ended(winner: Unit3D.Team) -> void:
	# Show game over label
	var game_over_label: Node = get_node_or_null("UI/GameOverLabel")
	if game_over_label:
		if winner == Unit3D.Team.PLAYER:
			game_over_label.set("text", "VICTORY!")
			if game_over_label.has_method("add_theme_color_override"):
				game_over_label.call("add_theme_color_override", "font_color", Color(0.3, 1.0, 0.3))
		else:
			game_over_label.set("text", "DEFEAT")
			if game_over_label.has_method("add_theme_color_override"):
				game_over_label.call("add_theme_color_override", "font_color", Color(1.0, 0.3, 0.3))
		game_over_label.set("visible", true)

func _on_summoner_destroyed(summoner: Summoner) -> void:
	if summoner == player_summoner:
		end_game(Unit3D.Team.ENEMY)
	elif summoner == enemy_summoner:
		end_game(Unit3D.Team.PLAYER)

func _load_ai_for_enemy() -> void:
	if not enemy_summoner:
		return

	# Get battle config from BattleContext
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if not battle_context:
		push_error("GameController3D: BattleContext not found")
		return

	var battle_config_variant: Variant = battle_context.get("battle_config")
	var battle_config: Dictionary = battle_config_variant if battle_config_variant is Dictionary else {}
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

func _register_summoner_provider() -> void:
	# Get active summoner using SummonerSelection service (handles fallbacks)
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		push_warning("GameController3D: SummonerSelection not found, no summoner bonuses will apply")
		return

	var summoner_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		summoner_id = summoner_selection.call("get_active_summoner_id")

	if summoner_id.is_empty():
		push_warning("GameController3D: No summoner selected, no summoner bonuses will apply")
		return

	# Get summoner instance data and create SummonerInstance
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	var summoner_instance: SummonerInstance = null
	if profile_repo and profile_repo.has_method("get_summoner_instance"):
		var summoner_data: Variant = profile_repo.call("get_summoner_instance", summoner_id)
		if summoner_data is Dictionary and not summoner_data.is_empty():
			summoner_instance = SummonerInstance.from_dict(summoner_data)

	if not summoner_instance:
		push_warning("GameController3D: Failed to load SummonerInstance for '%s', no summoner bonuses will apply" % summoner_id)
		return

	# Register summoner modifier provider
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	if not modifier_system:
		push_error("GameController3D: ModifierSystem not found!")
		return

	# Create and register summoner provider with SummonerInstance
	var summoner_provider: SummonerModifierProvider = SummonerModifierProvider.new(summoner_instance)
	if modifier_system.has_method("register_provider"):
		modifier_system.call("register_provider", "summoner", summoner_provider)

## =============================================================================
## REDIRECT INPUT HANDLING
## =============================================================================

## State for redirect drag operation
var _redirect_drag_active: bool = false
var _redirect_start_point: Vector3 = Vector3.ZERO
var _redirect_selected_units: Array[Unit3D] = []

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
			print("GameController3D: Mouse click detected in redirect mode")
			var click_point: Vector3 = _get_battlefield_point_from_mouse(mouse_event.position)
			print("GameController3D: Click point = ", click_point)
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
				print("GameController3D: Showing indicator at ", click_point, " with color ", indicator_color)
				_redirect_indicator.show_selection_circle(click_point, indicator_color)

				# Tint selected units
				_apply_unit_tint(indicator_color)

				print("GameController3D: Redirect started, selected %d units" % _redirect_selected_units.size())
			else:
				print("GameController3D: Click point was ZERO, raycast failed")

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
	print("GameController3D: Redirect release at ", release_point)
	print("GameController3D: Selected units count: ", _redirect_selected_units.size())

	if _redirect_selected_units.is_empty():
		print("GameController3D: No units selected for redirect")
		RedirectManager.cancel_redirect()
		return

	# Find nearest enemy at release point
	print("GameController3D: Searching for target at ", release_point, " with radius ", RedirectManager.TARGET_SEARCH_RADIUS)
	var target: Node3D = RedirectManager.find_nearest_enemy(
		release_point,
		player_summoner.team,
		RedirectManager.TARGET_SEARCH_RADIUS
	)

	if not target:
		print("GameController3D: No valid target found at release point")
		RedirectManager.cancel_redirect()
		return

	print("GameController3D: Found target: ", target.name, " at ", target.global_position)

	# Apply forced targets
	RedirectManager.apply_forced_targets(
		_redirect_selected_units,
		target,
		RedirectManager.FORCED_TARGET_DURATION,
		release_point
	)

	print("GameController3D: Redirect applied to %d units targeting %s" % [_redirect_selected_units.size(), target.name])

## Apply color tint to selected units
func _apply_unit_tint(tint_color: Color) -> void:
	_unit_original_modulates.clear()

	for unit: Unit3D in _redirect_selected_units:
		if is_instance_valid(unit) and unit.visual_component:
			# Access sprite via Variant to avoid type system issues
			var visual: Variant = unit.visual_component
			if visual is SpriteCharacter2D5Component:
				var sprite: Variant = visual.character_sprite
				if sprite != null:
					# Store original modulate
					_unit_original_modulates[unit] = sprite.modulate

					# Apply tinted color (lighter version of the redirect color)
					sprite.modulate = tint_color.lightened(0.3)

## Restore original colors to selected units
func _restore_unit_tint() -> void:
	for unit: Unit3D in _unit_original_modulates.keys():
		if is_instance_valid(unit) and unit.visual_component:
			# Access sprite via Variant to avoid type system issues
			var visual: Variant = unit.visual_component
			if visual is SpriteCharacter2D5Component:
				var sprite: Variant = visual.character_sprite
				if sprite != null:
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
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if not battle_context:
		print("BattleCoordinator: No BattleContext, using default win condition")
		return

	var config_variant: Variant = battle_context.get("battle_config")
	var config: Dictionary = config_variant if config_variant is Dictionary else {}
	if config.is_empty():
		print("BattleCoordinator: Empty battle config, using default win condition")
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

	print("BattleCoordinator: Win condition = %s, time_limit = %.1fs, kill_target = %d" % [
		win_condition, win_condition_time_limit, win_condition_kill_target
	])

## Handle timeout based on win condition type
func _handle_win_condition_timeout() -> void:
	if WinConditionIDs.timeout_is_win(win_condition):
		# Player wins by surviving the time limit (SURVIVE_TIME)
		end_game(Unit3D.Team.PLAYER)
	elif WinConditionIDs.timeout_is_loss(win_condition):
		# Player loses if they didn't complete objective in time (TIMED_DESTROY)
		end_game(Unit3D.Team.ENEMY)
	else:
		# Default: player wins on timeout
		_check_timeout_victory()

## Connect to unit death signals for kill count tracking
func _connect_unit_death_tracking() -> void:
	# Connect to any existing units
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	for node: Node in units:
		if node is Unit3D:
			var unit: Unit3D = node
			if not unit.unit_died.is_connected(_on_unit_died_for_kill_count):
				unit.unit_died.connect(_on_unit_died_for_kill_count)

	# Connect to future units via tree signal
	get_tree().node_added.connect(_on_node_added_for_kill_tracking)

## Track newly added units for kill counting
func _on_node_added_for_kill_tracking(node: Node) -> void:
	if win_condition != WinConditionIDs.KILL_COUNT:
		return

	if node is Unit3D:
		var unit: Unit3D = node
		if not unit.unit_died.is_connected(_on_unit_died_for_kill_count):
			unit.unit_died.connect(_on_unit_died_for_kill_count)

## Handle unit death for kill count objective
func _on_unit_died_for_kill_count(unit: Unit3D) -> void:
	if win_condition != WinConditionIDs.KILL_COUNT:
		return

	# Only count enemy kills
	if unit.team == Unit3D.Team.ENEMY:
		_enemy_kill_count += 1
		objective_progress.emit(_enemy_kill_count, win_condition_kill_target)

		# Check if objective met
		if _enemy_kill_count >= win_condition_kill_target:
			end_game(Unit3D.Team.PLAYER)

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
