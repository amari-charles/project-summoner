extends CanvasLayer
class_name ReconnectionOverlay

## Overlay shown when connection is lost during multiplayer battles.
## Displays reconnection status and allows player to cancel/return to menu.

@onready var status_label: Label = %StatusLabel
@onready var attempt_label: Label = %AttemptLabel
@onready var cancel_button: Button = %CancelButton

var _reconnection_handler: Node = null


func _ready() -> void:
	visible = false
	cancel_button.pressed.connect(_on_cancel_pressed)

	# Connect to ReconnectionHandler signals
	_reconnection_handler = get_node_or_null("/root/ReconnectionHandler")
	if _reconnection_handler:
		if _reconnection_handler.has_signal("ConnectionLost"):
			_reconnection_handler.ConnectionLost.connect(_on_connection_lost)
		if _reconnection_handler.has_signal("Reconnecting"):
			_reconnection_handler.Reconnecting.connect(_on_reconnecting)
		if _reconnection_handler.has_signal("Reconnected"):
			_reconnection_handler.Reconnected.connect(_on_reconnected)
		if _reconnection_handler.has_signal("ReconnectionFailed"):
			_reconnection_handler.ReconnectionFailed.connect(_on_reconnection_failed)


func _on_connection_lost(reason: String) -> void:
	visible = true
	status_label.text = Loc.t("ui.reconnection.connection_lost")
	attempt_label.text = reason
	cancel_button.text = Loc.t("ui.reconnection.return_to_menu")


func _on_reconnecting(attempt: int, max_attempts: int) -> void:
	visible = true
	status_label.text = Loc.t("ui.reconnection.reconnecting")
	attempt_label.text = Loc.t("ui.reconnection.attempt").format({"current": attempt, "max": max_attempts})


func _on_reconnected() -> void:
	visible = false
	status_label.text = ""
	attempt_label.text = ""


func _on_reconnection_failed() -> void:
	visible = true
	status_label.text = Loc.t("ui.reconnection.failed")
	attempt_label.text = Loc.t("ui.reconnection.failed_description")
	cancel_button.text = Loc.t("ui.reconnection.return_to_menu")


func _on_cancel_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	# Cancel any reconnection attempts
	if _reconnection_handler and _reconnection_handler.has_method("CancelReconnection"):
		_reconnection_handler.CancelReconnection()

	# End the match and return to menu
	visible = false

	# Signal battle to end
	if BattleContext.battle_state == BattleContext.BattleState.IN_PROGRESS:
		BattleContext.battle_state = BattleContext.BattleState.ABANDONED

	# Return to appropriate screen
	if BattleContext.is_ranked_match():
		SceneManager.transition_to(SceneManager.SCENE_ONLINE)
	else:
		SceneManager.transition_to(SceneManager.SCENE_MULTIPLAYER_LOBBY)


## Call this to manually show the overlay (e.g., from MatchSession)
func show_connection_lost(reason: String) -> void:
	_on_connection_lost(reason)


## Call this to manually hide the overlay
func hide_overlay() -> void:
	visible = false
