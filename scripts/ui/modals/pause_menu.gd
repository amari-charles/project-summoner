extends Control
class_name PauseMenu

## Pause menu overlay for battles
## Shows when game is paused, allows resume or quit to menu

var game_controller: Node = null

@onready var resume_button: Button = %ResumeButton
@onready var settings_button: Button = %SettingsButton
@onready var quit_button: Button = %QuitButton
@onready var settings_panel: PauseSettingsPanel = $SettingsPanel

func _ready() -> void:
	# CRITICAL: Process input even when game is paused
	process_mode = PROCESS_MODE_WHEN_PAUSED

	# Start hidden (also set in scene, but enforce here)
	visible = false

	# Prevent clicks from passing through overlay
	var overlay: Control = get_node_or_null("BackgroundOverlay")
	if overlay:
		overlay.mouse_filter = Control.MOUSE_FILTER_STOP

	# Set up panel border using ButtonStyleFactory variant system
	_setup_panel_border()

	# Set localized text
	resume_button.text = Loc.t("ui.pause_menu.resume")
	settings_button.text = Loc.t("ui.pause_menu.settings")
	quit_button.text = Loc.t("ui.pause_menu.quit")

	# Connect button signals first
	resume_button.pressed.connect(_on_resume_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	quit_button.pressed.connect(_on_quit_pressed)

	# Find game controller (deferred to ensure it's ready)
	call_deferred("_find_game_controller")

func _exit_tree() -> void:
	# Clean up signal connections to prevent memory leaks
	if game_controller and game_controller.is_connected("StateChanged", _on_game_state_changed):
		game_controller.disconnect("StateChanged", _on_game_state_changed)

func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)

	if not game_controller:
		push_error("PauseMenu: Could not find game controller in scene")
		return

	# Listen for pause state changes
	game_controller.connect("StateChanged", _on_game_state_changed)

	# Sync initial state
	_on_game_state_changed(game_controller.CurrentState)

## Show/hide based on game state
## BattleScene.GameState: Setup=0, Playing=1, Paused=2, GameOver=3
func _on_game_state_changed(new_state: int) -> void:
	visible = (new_state == 2)  # Paused

## Resume button - unpause game
func _on_resume_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if not game_controller:
		push_error("PauseMenu: Cannot resume - no game controller")
		return
	game_controller.ResumeGame()

## Settings button - show settings panel
func _on_settings_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	settings_panel.show_panel()

## Set up the panel border texture and margins from ButtonStyleFactory
func _setup_panel_border() -> void:
	var panel: NinePatchRect = get_node_or_null("PausePanel")
	if not panel:
		return
	ButtonStyleFactory.apply_panel_border(panel)


## Quit button - abandon battle and return to origin screen
func _on_quit_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	# CRITICAL: Unpause before changing scenes
	get_tree().paused = false

	# Stop battle music immediately (no fade) since we're transitioning scenes
	AudioManager.stop_music(0.0)

	# Get return destination before clearing context
	var return_scene: String = BattleContext.get_origin_scene()

	# Abandon via BattleScene (handles service cleanup + BattleContext state)
	if game_controller and game_controller.has_method("AbandonBattle"):
		game_controller.AbandonBattle()
	else:
		# Fallback if BattleScene not available
		BattleContext.abandon_battle()

	# Clear battle context
	BattleContext.clear()

	# Return to origin screen
	SceneManager.transition_to(return_scene)
