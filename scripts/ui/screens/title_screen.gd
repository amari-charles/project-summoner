extends Control
class_name TitleScreen

## Loading splash screen - shows game title then auto-transitions to Campaign Map

## Time to display the splash before transitioning
const SPLASH_DISPLAY_SECONDS: float = 2.0

## Max time to wait for fade_out animation before proceeding anyway
const FADE_OUT_TIMEOUT_SECONDS: float = 2.0

@onready var title_label: Label = $CenterContainer/VBoxContainer/Title
@onready var loading_bar: ProgressBar = $CenterContainer/VBoxContainer/LoadingBar
@onready var animation_player: AnimationPlayer = $AnimationPlayer

func _ready() -> void:
	title_label.text = Loc.t("ui.title.game_name")
	loading_bar.value = 0.0

	# Animate loading bar filling up
	var tween: Tween = create_tween()
	tween.tween_property(loading_bar, "value", 100.0, SPLASH_DISPLAY_SECONDS)
	await tween.finished
	_proceed_to_campaign()

func _input(event: InputEvent) -> void:
	# Debug: F11 to reset profile
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and not key_event.is_echo() and key_event.keycode == KEY_F11:
			_debug_reset_profile()

func _proceed_to_campaign() -> void:
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

