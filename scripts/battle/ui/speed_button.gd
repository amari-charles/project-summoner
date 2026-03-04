extends Button
class_name SpeedButton

## Speed control button for battle UI
## Toggles between 1x and 2x game speed
## Only enabled for campaign mode (disabled for online/arena)

var game_controller: Node = null
var current_speed: float = 1.0

func _ready() -> void:
	# Always process input (not affected by pause state)
	process_mode = PROCESS_MODE_ALWAYS

	# Connect button press
	pressed.connect(_on_pressed)

	# Find game controller and check battle mode
	call_deferred("_setup")

func _exit_tree() -> void:
	# Clean up signal connections to prevent memory leaks
	if game_controller and game_controller.is_connected("GameEnded", _on_game_ended):
		game_controller.disconnect("GameEnded", _on_game_ended)

	# CRITICAL: Reset time scale when leaving battle
	Engine.time_scale = 1.0

func _setup() -> void:
	game_controller = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)

	if not game_controller:
		push_error("SpeedButton: Could not find game controller")
		return

	# Hide button when game ends
	game_controller.connect("GameEnded", _on_game_ended)

	# Check battle mode to enable/disable button
	_check_battle_mode()

func _check_battle_mode() -> void:
	var current_mode: BattleContext.BattleMode = BattleContext.current_mode
	var is_speed_enabled: bool = current_mode in [
		BattleContext.BattleMode.CAMPAIGN,
		BattleContext.BattleMode.TUTORIAL,
		BattleContext.BattleMode.PRACTICE  # Enable for debug arena
	]

	disabled = not is_speed_enabled

	if disabled:
		text = "▶ 1x"
		tooltip_text = Loc.t("ui.speed.disabled_tooltip")
	else:
		tooltip_text = Loc.t("ui.speed.enabled_tooltip")

func _on_game_ended(_winner: UnitConstants.Team) -> void:
	visible = false

func _on_pressed() -> void:
	_toggle_speed()

func _toggle_speed() -> void:
	if current_speed == 1.0:
		_set_speed(2.0)
	else:
		_set_speed(1.0)

func _set_speed(speed: float) -> void:
	# Clamp to reasonable bounds for safety
	speed = clampf(speed, 0.1, 5.0)
	current_speed = speed
	Engine.time_scale = speed

	# Update button text
	if speed == 1.0:
		text = "▶ 1x"
	else:
		text = "▶▶ 2x"
