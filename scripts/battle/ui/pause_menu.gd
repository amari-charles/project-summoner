extends Control
class_name PauseMenu

## Pause menu overlay for battles
## Shows when game is paused, allows resume or quit to menu

var game_controller: Node = null

@onready var resume_button: Button = %ResumeButton
@onready var settings_button: Button = %SettingsButton
@onready var quit_button: Button = %QuitButton
@onready var title_label: Label = %TitleLabel
@onready var forfeit_confirmation: ConfirmationDialog = %ForfeitConfirmation
@onready var settings_panel: PauseSettingsPanel = $SettingsPanel
@onready var pause_panel: NinePatchRect = $PausePanel
@onready var panel_fill: ColorRect = $PanelFill

var _is_online_menu: bool = false

func _ready() -> void:
	# CRITICAL: Process input even when game is paused
	process_mode = PROCESS_MODE_ALWAYS

	# Start hidden (also set in scene, but enforce here)
	visible = false

	# Prevent clicks from passing through overlay
	var overlay: Control = get_node_or_null("BackgroundOverlay")
	if overlay:
		overlay.mouse_filter = Control.MOUSE_FILTER_STOP

	# Set up panel border using ButtonStyleFactory variant system
	_setup_panel_border()

	# Set localized text
	_is_online_menu = BattleContext.is_multiplayer_battle()
	title_label.text = Loc.t("ui.pause_menu.battle_menu") if _is_online_menu else Loc.t("ui.pause_menu.battle_paused")
	resume_button.text = Loc.t("ui.pause_menu.return_to_battle")
	settings_button.text = Loc.t("ui.pause_menu.settings")
	quit_button.text = Loc.t("ui.pause_menu.forfeit")
	forfeit_confirmation.title = Loc.t("ui.pause_menu.forfeit_confirm_title")
	forfeit_confirmation.dialog_text = Loc.t("ui.pause_menu.forfeit_confirm_message")
	forfeit_confirmation.ok_button_text = Loc.t("ui.pause_menu.forfeit")
	forfeit_confirmation.cancel_button_text = Loc.t("ui.common.cancel")

	# Connect button signals first
	resume_button.pressed.connect(_on_resume_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	quit_button.pressed.connect(_on_quit_pressed)
	forfeit_confirmation.confirmed.connect(_confirm_forfeit)
	settings_panel.closed.connect(_on_settings_closed)

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
	_on_game_state_changed(game_controller.get("CurrentState"))

## Show/hide based on game state
func _on_game_state_changed(new_state: Variant) -> void:
	if _is_online_menu:
		return
	var state: int = SafeTypeUtils.int_val(new_state, int(UnitConstants.GameState.PLAYING))
	visible = (state == int(UnitConstants.GameState.PAUSED))


func toggle_menu() -> void:
	if not _is_online_menu:
		return
	visible = not visible
	if not visible:
		settings_panel.hide_panel()


func close_settings_if_open() -> bool:
	if not settings_panel.visible:
		return false
	settings_panel.hide_panel()
	return true

## Resume button - unpause game
func _on_resume_pressed() -> void:
	if _is_online_menu:
		visible = false
		settings_panel.hide_panel()
		return
	if not game_controller:
		push_error("PauseMenu: Cannot resume - no game controller")
		return
	if game_controller.has_method("ResumeGame"):
		game_controller.call("ResumeGame")

## Settings button - show settings panel
func _on_settings_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	pause_panel.visible = false
	panel_fill.visible = false
	settings_panel.show_panel()


func _on_settings_closed() -> void:
	pause_panel.visible = true
	panel_fill.visible = true

## Set up the panel border texture and margins from ButtonStyleFactory
func _setup_panel_border() -> void:
	var panel: NinePatchRect = get_node_or_null("PausePanel")
	if not panel:
		return
	ButtonStyleFactory.apply_panel_border(panel)


## Quit button - abandon battle and return to origin screen
func _on_quit_pressed() -> void:
	forfeit_confirmation.popup_centered()


func _confirm_forfeit() -> void:
	# CRITICAL: Unpause before changing scenes
	get_tree().paused = false

	# Stop battle music immediately (no fade) since we're transitioning scenes
	AudioManager.stop_music(0.0)

	# Get return destination before clearing context
	var return_scene: String = BattleContext.get_origin_scene()

	# Abandon via BattleScene (handles service cleanup + BattleContext state)
	if game_controller and game_controller.has_method("AbandonBattle"):
		game_controller.call("AbandonBattle")
	else:
		# Fallback if BattleScene not available
		BattleContext.abandon_battle()

	# Clear battle context
	BattleContext.clear()

	# Return to origin screen
	SceneManager.transition_to(return_scene)
