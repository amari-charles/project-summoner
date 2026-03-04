extends PanelContainer
class_name DeckDropZone

## A drop zone for cards that emits a signal when a valid card is dropped.

signal card_dropped(instance_id: String)

## Callback to check if a drop is valid (set by parent)
var can_drop_callback: Callable = Callable()


func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	if not data is Dictionary:
		return false
	if data.get("type") != "card":
		return false

	# Use callback if provided
	if can_drop_callback.is_valid():
		var card_data: Dictionary = data.get("card_data", {})
		var instance_id: String = card_data.get("id", "")
		return can_drop_callback.call(instance_id)

	return true


func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if not data is Dictionary:
		return

	var card_data: Dictionary = data.get("card_data", {})
	var instance_id: String = card_data.get("id", "")

	if instance_id.is_empty():
		return

	card_dropped.emit(instance_id)
