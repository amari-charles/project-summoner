extends ScrollContainer
class_name CollectionDropZone

## A scroll container that can receive card drops for removing from deck.

signal card_dropped_to_remove(instance_id: String)

## Callback to check if a card can be removed (should return true if card is in deck)
var can_remove_callback: Callable = Callable()


func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	if not data is Dictionary:
		return false
	if data.get("type") != "card":
		return false

	# Use callback to check if this is a deck card
	if can_remove_callback.is_valid():
		var card_data: Dictionary = data.get("card_data", {})
		var instance_id: String = card_data.get("id", "")
		return can_remove_callback.call(instance_id)

	return false


func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if not data is Dictionary:
		return

	var card_data: Dictionary = data.get("card_data", {})
	var instance_id: String = card_data.get("id", "")

	if instance_id.is_empty():
		return

	card_dropped_to_remove.emit(instance_id)
