extends Node
class_name PlayerInput

## Handles player input for card playing and unit summoning

@export var summoner: Summoner
@export var test_card_scene: PackedScene  # For testing without a full deck

var current_selected_card: int = 0

func _ready() -> void:
	if summoner == null:
		summoner = get_tree().get_first_node_in_group("player_summoners")

func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			_handle_spawn_click(event.position)
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			_cycle_card()

	# Number keys to select card in hand (1-4)
	if event is InputEventKey and event.pressed:
		if event.keycode >= KEY_1 and event.keycode <= KEY_4:
			var card_index = event.keycode - KEY_1
			if card_index < summoner.hand.size():
				current_selected_card = card_index
				print("Selected card: ", summoner.hand[card_index].card_name)

## Handle left click to spawn unit
func _handle_spawn_click(screen_pos: Vector2) -> void:
	if summoner == null or not summoner.is_alive:
		return

	# Get world position from screen position
	var camera = get_viewport().get_camera_2d()
	var world_pos = screen_pos
	if camera:
		world_pos = camera.get_screen_center_position() + (screen_pos - get_viewport().get_visible_rect().size / 2)

	# Try to play the currently selected card
	if summoner.hand.size() > current_selected_card:
		var success = summoner.play_card(current_selected_card, world_pos)
		if success:
			print("Played card at: ", world_pos)
			# Reset to first card
			current_selected_card = 0
		else:
			print("Cannot play card - not enough mana")
	else:
		print("No cards in hand!")

## Cycle through cards with right click
func _cycle_card() -> void:
	if summoner == null or summoner.hand.is_empty():
		return

	current_selected_card = (current_selected_card + 1) % summoner.hand.size()
	print("Selected card: ", summoner.hand[current_selected_card].card_name)
