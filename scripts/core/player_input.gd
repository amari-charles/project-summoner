extends Node
class_name PlayerInput

## Handles player input for card playing and unit summoning

@export var summoner: Summoner
@export var test_card_scene: PackedScene  # For testing without a full deck

var hand_ui: HandUI

func _ready() -> void:
	if summoner == null:
		# PlayerInput is a child of PlayerSummoner node
		summoner = get_parent() as Summoner
		if summoner == null:
			push_error("PlayerInput: Could not find parent Summoner!")

	# Find HandUI
	await get_tree().process_frame  # Wait for UI to be ready
	hand_ui = get_tree().get_first_node_in_group("hand_ui")
	if not hand_ui:
		# Try to find it manually
		var ui_layer = get_tree().get_first_node_in_group("game_ui")
		if ui_layer:
			hand_ui = ui_layer.get_node_or_null("HandUI")

	if hand_ui:
		hand_ui.card_selected.connect(_on_hand_card_selected)
		print("PlayerInput: Connected to HandUI")
	else:
		push_warning("PlayerInput: Could not find HandUI")

func _input(event: InputEvent) -> void:
	# Number keys to select card in hand (1-4) for visual highlighting
	if event is InputEventKey and event.pressed:
		if event.keycode >= KEY_1 and event.keycode <= KEY_4:
			var card_index = event.keycode - KEY_1
			if hand_ui and card_index < summoner.hand.size():
				hand_ui.select_card_by_index(card_index)

## Called when card is selected in HandUI
func _on_hand_card_selected(index: int) -> void:
	print("PlayerInput: Card %d selected from HandUI" % index)
