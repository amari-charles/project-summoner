extends Control
class_name TitleScreen

## Simple title screen - tap/click to start
## Entry point for the game, transitions to Campaign Map

## Time to wait before allowing interaction
## Allows UI elements to fully render and any intro animations to settle
const INITIAL_DELAY_SECONDS: float = 0.5

## Max time to wait for fade_out animation before proceeding anyway
const FADE_OUT_TIMEOUT_SECONDS: float = 2.0

@onready var title_label: Label = $CenterContainer/VBoxContainer/Title
@onready var tap_prompt: Label = $CenterContainer/VBoxContainer/TapPrompt
@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var quit_button: Button = $QuitButton

var _can_proceed: bool = false

func _ready() -> void:
	# Set localized text
	title_label.text = Loc.t("ui.title.game_name")
	tap_prompt.text = Loc.t("ui.title.tap_prompt")
	quit_button.text = Loc.t("menu.quit")
	quit_button.pressed.connect(_on_quit_pressed)

	# Start fade-in and prompt animation after brief delay
	await get_tree().create_timer(INITIAL_DELAY_SECONDS).timeout
	_can_proceed = true
	animation_player.play("pulse_prompt")

func _input(event: InputEvent) -> void:
	if not _can_proceed:
		return

	# Handle tap/click to proceed
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_proceed_to_campaign()

	# Also handle touch
	if event is InputEventScreenTouch:
		var touch_event: InputEventScreenTouch = event as InputEventScreenTouch
		if touch_event.pressed:
			_proceed_to_campaign()

	# Handle keyboard (space/enter)
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and not key_event.is_echo():
			if key_event.keycode == KEY_SPACE or key_event.keycode == KEY_ENTER:
				_proceed_to_campaign()
			# Debug: F11 to reset profile
			elif key_event.keycode == KEY_F11:
				_debug_reset_profile()

func _proceed_to_campaign() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_can_proceed = false
	animation_player.play("fade_out")
	await _await_animation_with_timeout(animation_player, FADE_OUT_TIMEOUT_SECONDS)
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Await animation completion with timeout protection to prevent hangs
## Uses is_playing() check to avoid race conditions with signal connection
func _await_animation_with_timeout(anim_player: AnimationPlayer, timeout: float) -> void:
	var elapsed: float = 0.0
	while anim_player.is_playing() and elapsed < timeout:
		await get_tree().process_frame
		elapsed += get_process_delta_time()

	if elapsed >= timeout:
		push_warning("TitleScreen: Animation timed out after %.1fs" % timeout)

func _debug_reset_profile() -> void:
	print("TitleScreen: F11 pressed - resetting profile...")
	if DevConsole:
		DevConsole.execute_command("/save_wipe")
		get_tree().reload_current_scene()

func _on_quit_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	get_tree().quit()
