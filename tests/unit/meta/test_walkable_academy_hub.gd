extends GutTest

const HUB_SCENE_PATH: String = "res://scenes/meta/screens/walkable_academy_hub.tscn"
const MENU_HUB_SCENE_PATH: String = "res://scenes/meta/screens/academy_hub.tscn"


func test_walkable_hub_is_primary_route_and_menu_hub_remains_available() -> void:
	assert_eq(SceneManager.SCENE_CAMPAIGN_MAP, HUB_SCENE_PATH)
	assert_eq(SceneManager.SCENE_WALKABLE_ACADEMY_HUB, HUB_SCENE_PATH)
	assert_eq(SceneManager.SCENE_ACADEMY_MENU_HUB, MENU_HUB_SCENE_PATH)
	assert_true(ResourceLoader.exists(SceneManager.SCENE_WALKABLE_ACADEMY_HUB))
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ACADEMY_MENU_HUB))


func test_hub_scene_contains_player_boundaries_and_shortcut_interface() -> void:
	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	assert_not_null(packed_scene)
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	assert_not_null(hub)
	assert_not_null(hub.get_node_or_null("Player"))
	assert_not_null(hub.get_node_or_null("Boundaries/Top/CollisionShape3D"))
	assert_not_null(hub.get_node_or_null("Boundaries/Bottom/CollisionShape3D"))
	assert_not_null(hub.get_node_or_null("Boundaries/Left/CollisionShape3D"))
	assert_not_null(hub.get_node_or_null("Boundaries/Right/CollisionShape3D"))
	assert_not_null(hub.get_node_or_null("Interface/ShortcutButton"))
	assert_not_null(hub.get_node_or_null("Interface/ShortcutPanel"))
	assert_not_null(hub.get_node_or_null("PlaceholderCrowd"))
	hub.free()


func test_every_building_destination_has_a_shortcut_and_current_route() -> void:
	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	var building_count: int = 0
	assert_eq(WalkableAcademyHub.DESTINATIONS.size(), 7)
	for destination: Dictionary in WalkableAcademyHub.DESTINATIONS:
		var destination_id: StringName = destination["id"]
		assert_false(hub._scene_for_destination(destination_id).is_empty())
		assert_true(ResourceLoader.exists(hub._scene_for_destination(destination_id)))
		if destination.has("position"):
			building_count += 1
			var position: Vector3 = destination["position"]
			assert_true(absf(position.x) <= 18.0)
			assert_true(absf(position.z) <= 15.0)
			assert_true(destination.has("placeholder_art_path"))
			var placeholder_art_path: String = destination["placeholder_art_path"]
			assert_true(placeholder_art_path.contains("/placeholders/"))
			assert_true(ResourceLoader.exists(placeholder_art_path))

	assert_eq(building_count, 5)
	assert_eq(hub._scene_for_destination(&"summoner"), SceneManager.SCENE_SUMMONER_SCREEN)
	assert_eq(hub._scene_for_destination(&"settings"), SceneManager.SCENE_SETTINGS)
	hub.free()


func test_walkable_controls_are_project_actions() -> void:
	for action: StringName in [&"move_left", &"move_right", &"move_up", &"move_down", &"interact"]:
		assert_true(InputMap.has_action(action), "%s must be configured in project.godot" % action)
		assert_false(InputMap.action_get_events(action).is_empty(), "%s must have an input binding" % action)


func test_placeholder_crowd_is_visual_only_and_deterministic() -> void:
	assert_eq(PlaceholderCampusCrowd.PLACEMENTS.size(), 8)
	for placement: Dictionary in PlaceholderCampusCrowd.PLACEMENTS:
		assert_not_null(placement["texture"])
		assert_gt(int(placement["frames"]), 1)


func test_player_switches_to_visible_run_cycle_during_movement() -> void:
	var packed_hub: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_hub.instantiate() as WalkableAcademyHub
	var player: WalkableAcademyPlayer = hub.get_node("Player") as WalkableAcademyPlayer
	add_child_autofree(hub)
	await get_tree().process_frame

	player._set_animation(true)
	player._update_animation(0.2, Vector3.RIGHT)
	var visual: Sprite3D = player.get_node("PlayerVisual") as Sprite3D
	assert_true(visual.texture.resource_path.ends_with("placeholder_player_pawn_run.png"))
	assert_eq(visual.hframes, WalkableAcademyPlayer.RUN_FRAME_COUNT)
	assert_gt(visual.frame, 0)
	assert_false(visual.flip_h)
	player._update_animation(0.0, Vector3.LEFT)
	assert_true(visual.flip_h)
	player._update_animation(0.0, Vector3.RIGHT)
	assert_false(visual.flip_h)


func test_cutout_order_uses_feet_depth_instead_of_sprite_center() -> void:
	assert_lt(CutoutRenderOrder.priority_for_feet(-10.0), CutoutRenderOrder.priority_for_feet(10.0))
	var sprite: Sprite3D = Sprite3D.new()
	CutoutRenderOrder.apply_from_feet(sprite, 7.0)
	assert_eq(sprite.render_priority, CutoutRenderOrder.priority_for_feet(7.0))
	sprite.free()


func test_building_displays_explicit_placeholder_art() -> void:
	var destination: Dictionary = WalkableAcademyHub.DESTINATIONS[0]
	var texture: Texture2D = load(destination["placeholder_art_path"]) as Texture2D
	var packed_building: PackedScene = load("res://scenes/meta/components/walkable_academy_building.tscn") as PackedScene
	var building: WalkableAcademyBuilding = packed_building.instantiate() as WalkableAcademyBuilding
	building.configure(
		destination["name_key"],
		SceneManager.SCENE_ACADEMY_CLASS_HALL,
		SceneManager.SCENE_WALKABLE_ACADEMY_HUB,
		texture
	)
	add_child_autofree(building)
	await get_tree().process_frame

	var art: Sprite3D = building.get_node("PlaceholderBuildingArt") as Sprite3D
	var placeholder_label: Label3D = building.get_node("PlaceholderLabel") as Label3D
	assert_eq(art.texture, texture)
	assert_eq(art.offset, Vector2.ZERO)
	assert_true(placeholder_label.text.begins_with("PLACEHOLDER"))
