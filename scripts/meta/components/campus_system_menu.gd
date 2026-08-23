extends Control
class_name CampusSystemMenu

@onready var title_label: Label = %TitleLabel
@onready var menu_center: CenterContainer = %MenuCenter
@onready var resume_button: Button = %ResumeButton
@onready var settings_button: Button = %SettingsButton
@onready var quit_button: Button = %QuitButton
@onready var settings_overlay: Control = %SettingsOverlay
@onready var settings_back_button: Button = %SettingsBackButton
@onready var quit_confirmation: ConfirmationDialog = %QuitConfirmation


func _ready() -> void:
	process_mode = PROCESS_MODE_ALWAYS
	visible = false
	settings_overlay.visible = false
	title_label.text = Loc.t("ui.system_menu.title")
	resume_button.text = Loc.t("ui.system_menu.resume")
	settings_button.text = Loc.t("ui.system_menu.settings")
	quit_button.text = Loc.t("ui.system_menu.quit_game")
	settings_back_button.text = Loc.t("ui.system_menu.back_to_menu")
	quit_confirmation.title = Loc.t("ui.system_menu.quit_confirm_title")
	quit_confirmation.dialog_text = Loc.t("ui.system_menu.quit_confirm_message")
	quit_confirmation.ok_button_text = Loc.t("ui.system_menu.quit_game")
	quit_confirmation.cancel_button_text = Loc.t("ui.common.cancel")
	resume_button.pressed.connect(close_menu)
	settings_button.pressed.connect(open_settings)
	quit_button.pressed.connect(_show_quit_confirmation)
	settings_back_button.pressed.connect(_close_settings)
	quit_confirmation.confirmed.connect(_quit_game)


func _exit_tree() -> void:
	if get_tree() != null:
		get_tree().paused = false


func _unhandled_key_input(event: InputEvent) -> void:
	if not visible or not event.is_action_pressed("ui_cancel") or event.is_echo():
		return
	get_viewport().set_input_as_handled()
	if settings_overlay.visible:
		_close_settings()
	else:
		close_menu()


func open_menu() -> void:
	settings_overlay.visible = false
	menu_center.visible = true
	visible = true
	get_tree().paused = true
	resume_button.grab_focus()


func close_menu() -> void:
	settings_overlay.visible = false
	visible = false
	get_tree().paused = false


func open_settings() -> void:
	if not visible:
		open_menu()
	menu_center.visible = false
	settings_overlay.visible = true
	settings_back_button.grab_focus()


func _close_settings() -> void:
	settings_overlay.visible = false
	menu_center.visible = true
	settings_button.grab_focus()


func _show_quit_confirmation() -> void:
	quit_confirmation.popup_centered()


func _quit_game() -> void:
	get_tree().paused = false
	get_tree().quit()
