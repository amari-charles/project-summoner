extends BackNavigableScreen
class_name SnapshotManager

## Snapshot Manager - UI for managing profile snapshots
##
## Allows users to save, load, and delete profile state snapshots.
## Used from both pause menu and main menu.

## Node references
@onready var snapshot_list: ItemList = %SnapshotList
@onready var save_button: Button = %SaveButton
@onready var load_button: Button = %LoadButton
@onready var delete_button: Button = %DeleteButton
@onready var refresh_button: Button = %RefreshButton
@onready var close_button: Button = %CloseButton
@onready var reset_profile_button: Button = %ResetProfileButton
@onready var unlock_summoners_button: Button = %UnlockSummonersButton

## Dialogs
@onready var name_dialog: AcceptDialog = %NameDialog
@onready var name_input: LineEdit = %NameInput
@onready var confirm_delete_dialog: ConfirmationDialog = %ConfirmDeleteDialog
@onready var confirm_reset_dialog: ConfirmationDialog = %ConfirmResetDialog

## Service references
var _snapshots: Node = null  # DebugSnapshots autoload
var _dev_console: Node = null  # DevConsole autoload

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Get autoload references
	_snapshots = DebugSnapshots
	_dev_console = DevConsole

	# Connect signals
	save_button.pressed.connect(_on_save_pressed)
	load_button.pressed.connect(_on_load_pressed)
	delete_button.pressed.connect(_on_delete_pressed)
	refresh_button.pressed.connect(_on_refresh_pressed)
	close_button.pressed.connect(_on_close_pressed)
	reset_profile_button.pressed.connect(_on_reset_profile_pressed)
	unlock_summoners_button.pressed.connect(_on_unlock_summoners_pressed)

	name_dialog.confirmed.connect(_on_name_confirmed)
	confirm_delete_dialog.confirmed.connect(_on_delete_confirmed)
	confirm_reset_dialog.confirmed.connect(_on_reset_confirmed)

	snapshot_list.item_selected.connect(_on_snapshot_selected)

	# Initial state
	load_button.disabled = true
	delete_button.disabled = true

	# Load snapshots
	_refresh_snapshot_list()

func _input(event: InputEvent) -> void:
	if not visible:
		return

	# ESC to close
	if event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_on_close_pressed()
			get_viewport().set_input_as_handled()

## =============================================================================
## PUBLIC API
## =============================================================================

func show_manager() -> void:
	visible = true
	_refresh_snapshot_list()

## =============================================================================
## BUTTON HANDLERS
## =============================================================================

func _on_save_pressed() -> void:
	# Show name input dialog
	name_input.text = ""
	name_input.placeholder_text = "Enter snapshot name..."
	name_dialog.popup_centered(Vector2i(400, 150))
	# Focus the input field
	await get_tree().process_frame
	name_input.grab_focus()

func _on_load_pressed() -> void:
	var selected_indices: PackedInt32Array = snapshot_list.get_selected_items()
	if selected_indices.size() == 0:
		return

	var selected_index: int = selected_indices[0]
	var item_text: String = snapshot_list.get_item_text(selected_index)
	var snapshot_name: String = _extract_name_from_display(item_text)

	if _dev_console:
		var success: bool = _dev_console.call("execute_command", "/snapshot_load " + snapshot_name)
		if success:
			print("SnapshotManager: Loaded snapshot '%s'" % snapshot_name)
			_on_close_pressed()  # Close after successful load
		else:
			push_error("SnapshotManager: Failed to load snapshot '%s'" % snapshot_name)

func _on_delete_pressed() -> void:
	var selected_indices: PackedInt32Array = snapshot_list.get_selected_items()
	if selected_indices.size() == 0:
		return

	var selected_index: int = selected_indices[0]
	var item_text: String = snapshot_list.get_item_text(selected_index)
	var snapshot_name: String = _extract_name_from_display(item_text)

	# Show confirmation
	confirm_delete_dialog.dialog_text = "Delete snapshot '%s'?\nThis cannot be undone." % snapshot_name
	confirm_delete_dialog.popup_centered(Vector2i(400, 150))

func _on_refresh_pressed() -> void:
	_refresh_snapshot_list()

func _on_close_pressed() -> void:
	visible = false

func _on_reset_profile_pressed() -> void:
	# Show confirmation
	confirm_reset_dialog.dialog_text = "Reset profile to fresh state?\nThis will delete all progress!\n\nThis cannot be undone."
	confirm_reset_dialog.popup_centered(Vector2i(400, 180))

func _on_unlock_summoners_pressed() -> void:
	if _dev_console:
		_dev_console.call("execute_command", "/unlock_all_summoners")
		print("SnapshotManager: Unlocked all summoners")

## =============================================================================
## DIALOG HANDLERS
## =============================================================================

func _on_name_confirmed() -> void:
	var snapshot_name: String = name_input.text.strip_edges()

	if snapshot_name.is_empty():
		push_warning("SnapshotManager: Snapshot name cannot be empty")
		return

	if _dev_console:
		var success: bool = _dev_console.call("execute_command", "/snapshot_save " + snapshot_name)
		if success:
			print("SnapshotManager: Saved snapshot '%s'" % snapshot_name)
			_refresh_snapshot_list()
		else:
			push_error("SnapshotManager: Failed to save snapshot '%s'" % snapshot_name)

func _on_delete_confirmed() -> void:
	var selected_indices: PackedInt32Array = snapshot_list.get_selected_items()
	if selected_indices.size() == 0:
		return

	var selected_index: int = selected_indices[0]
	var item_text: String = snapshot_list.get_item_text(selected_index)
	var snapshot_name: String = _extract_name_from_display(item_text)

	if _dev_console:
		var success: bool = _dev_console.call("execute_command", "/snapshot_delete " + snapshot_name)
		if success:
			print("SnapshotManager: Deleted snapshot '%s'" % snapshot_name)
			_refresh_snapshot_list()
		else:
			push_error("SnapshotManager: Failed to delete snapshot '%s'" % snapshot_name)

func _on_reset_confirmed() -> void:
	if _dev_console:
		var success: bool = _dev_console.call("execute_command", "/save_wipe")
		if success:
			print("SnapshotManager: Profile reset successfully")
			_on_close_pressed()  # Close after reset
		else:
			push_error("SnapshotManager: Failed to reset profile")

## =============================================================================
## SNAPSHOT LIST
## =============================================================================

func _on_snapshot_selected(_index: int) -> void:
	# Enable load/delete buttons when something is selected
	load_button.disabled = false
	delete_button.disabled = false

func _refresh_snapshot_list() -> void:
	snapshot_list.clear()

	if not _snapshots:
		return

	var snapshots: Array = _snapshots.call("list_snapshots")

	if snapshots.size() == 0:
		snapshot_list.add_item("(No snapshots found)")
		load_button.disabled = true
		delete_button.disabled = true
		return

	for snapshot: Dictionary in snapshots:
		var snapshot_name: String = snapshot.get("name", "unknown")
		var created_at: String = snapshot.get("created_at", "unknown")
		var display_text: String = "%s (created: %s)" % [snapshot_name, created_at]
		snapshot_list.add_item(display_text)

	# Clear selection
	load_button.disabled = true
	delete_button.disabled = true

func _extract_name_from_display(display_text: String) -> String:
	# Extract "name" from "name (created: timestamp)"
	var parts: PackedStringArray = display_text.split(" (created:", false)
	if parts.size() > 0:
		return parts[0].strip_edges()
	return display_text


func _request_back_navigation() -> void:
	_on_close_pressed()
