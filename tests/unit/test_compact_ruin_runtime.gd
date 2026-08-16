extends GutTest

const SCENE_PATH: String = "res://scenes/battle/battlefield/dev/compact_ruin_skirmish.tscn"


func after_each() -> void:
	Input.action_release(&"move_right")


func test_movement_toggle_controls_live_summoner_movement() -> void:
	var packed: PackedScene = load(SCENE_PATH)
	var scene: Node3D = packed.instantiate()
	add_child_autofree(scene)
	for _frame: int in 30:
		await get_tree().process_frame
	await get_tree().physics_frame
	assert_engine_error("No explicit seed provided")

	var summoner: Node3D = scene.get_node("PlayerSummoner")
	var summoner_visual: Sprite3D = scene.get_node("PlayerSummoner/Visual")
	var camera: Camera3D = scene.get_node("Battlefield3D/Camera3D")
	var simulation: Node = scene.get_tree().get_first_node_in_group("simulation_node")
	var hand_ui: Control = scene.get_node("UI/HandUI")
	var toggle: CheckButton = scene.get_node("UI/MovementPanel/VBox/MovementToggle")
	var start_position: Vector3 = summoner.global_position
	var card_display_count: int = 0
	for child: Node in hand_ui.get_children():
		if child.name.begins_with("CardDisplay"):
			card_display_count += 1
	assert_eq(card_display_count, 4, "the opening hand should render four playable cards")
	assert_eq(
		int(simulation.call("GetSummonPlacementMode")),
		1,
		"the compact room should replace team-half placement with card-specific range"
	)
	assert_true(camera.is_processing_input(), "the room camera should receive live input events")

	var starting_fov: float = camera.fov
	var zoom_event: InputEventMouseButton = InputEventMouseButton.new()
	zoom_event.button_index = MOUSE_BUTTON_WHEEL_UP
	zoom_event.pressed = true
	camera._input(zoom_event)
	assert_lt(camera.fov, starting_fov, "mouse wheel up should zoom the fixed camera in")
	var zoomed_in_fov: float = camera.fov
	var zoom_out_event: InputEventMouseButton = InputEventMouseButton.new()
	zoom_out_event.button_index = MOUSE_BUTTON_WHEEL_DOWN
	zoom_out_event.pressed = true
	camera._input(zoom_out_event)
	assert_gt(camera.fov, zoomed_in_fov, "mouse wheel down should zoom the fixed camera out")

	Input.action_press(&"move_right")
	for _frame: int in 6:
		await get_tree().physics_frame
	Input.action_release(&"move_right")
	assert_gt(summoner.global_position.x, start_position.x, "enabled movement should move right")
	assert_eq(
		summoner_visual.texture.resource_path,
		"res://assets/placeholders/tiny_swords/characters/placeholder_player_pawn_run.png",
		"moving should use the campus run animation"
	)
	assert_eq(summoner_visual.hframes, 6)

	toggle.button_pressed = false
	await get_tree().process_frame
	var disabled_position: Vector3 = summoner.global_position
	Input.action_press(&"move_right")
	for _frame: int in 6:
		await get_tree().physics_frame
	Input.action_release(&"move_right")
	assert_almost_eq(
		summoner.global_position.x,
		disabled_position.x,
		0.001,
		"disabled movement should hold the summoner still"
	)
	assert_eq(
		summoner_visual.texture.resource_path,
		"res://assets/placeholders/tiny_swords/characters/placeholder_player_pawn_idle.png",
		"standing still should return to the campus idle animation"
	)
	assert_eq(summoner_visual.hframes, 8)
