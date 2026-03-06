extends Control
class_name TitleScreen

## Loading splash screen - preloads target scene then transitions

## Minimum time to display splash (prevents flash when resources are cached)
const MIN_DISPLAY_SECONDS: float = 0.5

## Max time to wait for fade_out animation before proceeding anyway
const FADE_OUT_TIMEOUT_SECONDS: float = 2.0

@onready var title_label: Label = $CenterContainer/VBoxContainer/Title
@onready var loading_bar: ProgressBar = $CenterContainer/VBoxContainer/LoadingBar
@onready var animation_player: AnimationPlayer = $AnimationPlayer

var _start_time_ms: int = 0

func _ready() -> void:
	title_label.text = Loc.t("ui.title.game_name")
	loading_bar.value = 0.0
	_start_time_ms = Time.get_ticks_msec()

	# Preload the target scene with real resource loading
	var preloader: ResourcePreloader = ResourcePreloader.new()
	add_child(preloader)
	preloader.ProgressUpdated.connect(_on_progress_updated)
	preloader.LoadAll(_get_preload_paths())
	await preloader.Completed

	# Enforce minimum display time
	var elapsed_ms: int = Time.get_ticks_msec() - _start_time_ms
	var remaining_ms: float = (MIN_DISPLAY_SECONDS * 1000.0) - elapsed_ms
	if remaining_ms > 0:
		await get_tree().create_timer(remaining_ms / 1000.0).timeout

	loading_bar.value = 100.0

	if _should_goto_online():
		_proceed_to_online()
	else:
		_proceed_to_campaign()

func _on_progress_updated(progress: float) -> void:
	loading_bar.value = progress * 100.0

func _get_preload_paths() -> PackedStringArray:
	if _should_goto_online():
		return PackedStringArray([SceneManager.SCENE_ONLINE])
	return PackedStringArray([SceneManager.SCENE_CAMPAIGN_MAP])

func _input(event: InputEvent) -> void:
	# Debug: F11 to reset profile
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and not key_event.is_echo() and key_event.keycode == KEY_F11:
			_debug_reset_profile()

func _should_goto_online() -> bool:
	return "--goto-online" in OS.get_cmdline_user_args()

func _proceed_to_online() -> void:
	animation_player.play("fade_out")
	await _await_animation_with_timeout(animation_player, FADE_OUT_TIMEOUT_SECONDS)
	SceneManager.transition_to(SceneManager.SCENE_ONLINE)

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

