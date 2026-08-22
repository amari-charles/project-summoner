extends Control
class_name BackNavigableScreen

## Shared keyboard behavior for full-screen menus that expose a Back/Close button.
## Concrete screens retain ownership of their existing navigation destination.


func _unhandled_key_input(event: InputEvent) -> void:
	if not is_visible_in_tree() or not event.is_action_pressed("ui_cancel") or event.is_echo():
		return

	get_viewport().set_input_as_handled()
	_request_back_navigation()


func _request_back_navigation() -> void:
	push_warning("BackNavigableScreen requires _request_back_navigation()")
