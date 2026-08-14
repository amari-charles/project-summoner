extends GutTest

const HUB_SCENE_PATH: String = "res://scenes/meta/screens/walkable_academy_hub.tscn"
const MENU_HUB_SCENE_PATH: String = "res://scenes/meta/screens/academy_hub.tscn"
const CUTOUT_RENDER_ORDER: Script = preload("res://scripts/meta/components/academy_cutout_render_order.gd")


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
	var ground: MeshInstance3D = hub.get_node("Ground") as MeshInstance3D
	var ground_material: StandardMaterial3D = ground.material_override as StandardMaterial3D
	assert_not_null(ground_material)
	assert_true(ground_material.albedo_texture.resource_path.contains("/placeholders/tiny_swords/terrain/"))
	assert_gt(ground_material.uv1_scale.x, 1.0)
	assert_gt(ground_material.uv1_scale.y, 1.0)
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
			assert_true(absf(position.x) <= 13.0)
			assert_true(absf(position.z) <= 11.0)
			assert_true(destination.has("placeholder_texture"))
			var placeholder_texture: Texture2D = destination["placeholder_texture"]
			assert_not_null(placeholder_texture)
			var placeholder_art_path: String = placeholder_texture.resource_path
			assert_true(placeholder_art_path.contains("/placeholders/"))
			assert_true(ResourceLoader.exists(placeholder_art_path))

	assert_eq(building_count, 5)
	assert_eq(hub._scene_for_destination(WalkableAcademyHub.DESTINATION_SUMMONER), SceneManager.SCENE_SUMMONER_SCREEN)
	assert_eq(hub._scene_for_destination(WalkableAcademyHub.DESTINATION_SETTINGS), SceneManager.SCENE_SETTINGS)
	hub.free()


func test_placeholder_ground_tile_scale_and_tint_are_configurable() -> void:
	var ground_textures: Array[Texture2D] = [
		WalkableAcademyHub.PLACEHOLDER_GROUND_CENTER,
		WalkableAcademyHub.PLACEHOLDER_GROUND_TOP_LEFT,
		WalkableAcademyHub.PLACEHOLDER_GROUND_TOP,
		WalkableAcademyHub.PLACEHOLDER_GROUND_TOP_RIGHT,
		WalkableAcademyHub.PLACEHOLDER_GROUND_LEFT,
		WalkableAcademyHub.PLACEHOLDER_GROUND_RIGHT,
		WalkableAcademyHub.PLACEHOLDER_GROUND_BOTTOM_LEFT,
		WalkableAcademyHub.PLACEHOLDER_GROUND_BOTTOM,
		WalkableAcademyHub.PLACEHOLDER_GROUND_BOTTOM_RIGHT,
		WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_LEFT,
		WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE,
		WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_RIGHT,
	]
	for texture: Texture2D in ground_textures:
		assert_eq(texture.get_size(), Vector2(64.0, 64.0), "Ground regions must share the source pack's 64px grid")

	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	hub.ground_tile_world_size = 9.0
	hub.ground_tint = Color(0.5, 0.6, 0.7, 1.0)
	add_child_autofree(hub)
	await get_tree().process_frame

	var ground: MeshInstance3D = hub.get_node("Ground") as MeshInstance3D
	var ground_plane: PlaneMesh = ground.mesh as PlaneMesh
	var ground_material: StandardMaterial3D = ground.material_override as StandardMaterial3D
	assert_almost_eq(ground_material.uv1_scale.x, ground_plane.size.x / 9.0, 0.0001)
	assert_almost_eq(ground_material.uv1_scale.y, ground_plane.size.y / 9.0, 0.0001)
	assert_eq(ground_material.albedo_color, hub.ground_tint)
	assert_eq(ground_material.transparency, BaseMaterial3D.TRANSPARENCY_DISABLED)
	assert_eq(ground.get_child_count(), 11, "The ground should include its perimeter and one front cliff row")
	var top_edge: MeshInstance3D = ground.get_node_or_null("TopEdge") as MeshInstance3D
	assert_not_null(top_edge)
	var top_edge_material: StandardMaterial3D = top_edge.material_override as StandardMaterial3D
	assert_eq(top_edge_material.transparency, BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR)
	assert_not_null(ground.get_node_or_null("BottomRightCorner"))
	var cliff_middle: MeshInstance3D = ground.get_node_or_null("CliffMiddleCenter") as MeshInstance3D
	var cliff_left: MeshInstance3D = ground.get_node_or_null("CliffMiddleLeft") as MeshInstance3D
	var cliff_right: MeshInstance3D = ground.get_node_or_null("CliffMiddleRight") as MeshInstance3D
	assert_not_null(cliff_middle)
	assert_not_null(cliff_left)
	assert_not_null(cliff_right)
	assert_true(cliff_middle.mesh is QuadMesh, "The cliff must descend on vertical geometry")
	assert_lt(cliff_middle.position.y, 0.0)
	assert_eq((cliff_left.material_override as StandardMaterial3D).albedo_texture, WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_LEFT)
	assert_eq((cliff_right.material_override as StandardMaterial3D).albedo_texture, WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_RIGHT)
	assert_null(ground.get_node_or_null("CliffBottomCenter"))


func test_walkable_controls_are_project_actions() -> void:
	for action: StringName in [&"move_left", &"move_right", &"move_up", &"move_down", &"interact"]:
		assert_true(InputMap.has_action(action), "%s must be configured in project.godot" % action)
		assert_false(InputMap.action_get_events(action).is_empty(), "%s must have an input binding" % action)


func test_placeholder_crowd_is_visual_only_and_deterministic() -> void:
	assert_eq(PlaceholderCampusCrowd.PLACEMENTS.size(), 8)
	for placement: Dictionary in PlaceholderCampusCrowd.PLACEMENTS:
		assert_not_null(placement["texture"])
		assert_gt(int(placement["frames"]), 1)
		assert_gt(float(placement["pixel_size"]), 0.0)
		var position: Vector3 = placement["position"]
		assert_eq(position.y, 0.0)


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
	assert_almost_eq(visual.position.y, -1.2, 0.0001)
	assert_gt(visual.offset.y, 0.0)
	assert_false(visual.flip_h)
	player._update_animation(0.0, Vector3.LEFT)
	assert_true(visual.flip_h)
	player._update_animation(0.0, Vector3.RIGHT)
	assert_false(visual.flip_h)


func test_cutout_order_uses_feet_depth_instead_of_sprite_center() -> void:
	assert_lt(CUTOUT_RENDER_ORDER.priority_for_feet(-10.0), CUTOUT_RENDER_ORDER.priority_for_feet(10.0))
	var sprite: Sprite3D = Sprite3D.new()
	CUTOUT_RENDER_ORDER.apply_from_feet(sprite, 7.0)
	assert_eq(sprite.render_priority, CUTOUT_RENDER_ORDER.priority_for_feet(7.0))
	assert_eq(sprite.alpha_cut, SpriteBase3D.ALPHA_CUT_DISABLED)
	sprite.free()


func test_building_displays_explicit_placeholder_art() -> void:
	var destination: Dictionary = WalkableAcademyHub.DESTINATIONS[0]
	var texture: Texture2D = destination["placeholder_texture"]
	var packed_building: PackedScene = load("res://scenes/meta/components/walkable_academy_building.tscn") as PackedScene
	var building: WalkableAcademyBuilding = packed_building.instantiate() as WalkableAcademyBuilding
	var campus_camera: Camera3D = Camera3D.new()
	campus_camera.rotation_degrees.x = -45.0
	add_child_autofree(campus_camera)
	building.configure(
		destination["name_key"],
		SceneManager.SCENE_ACADEMY_CLASS_HALL,
		SceneManager.SCENE_WALKABLE_ACADEMY_HUB,
		texture,
		campus_camera
	)
	add_child_autofree(building)
	await get_tree().process_frame

	var art: Sprite3D = building.get_node("PlaceholderBuildingArt") as Sprite3D
	var collision_shape: CollisionShape3D = building.get_node("CollisionBody/BuildingCollisionShape") as CollisionShape3D
	var collision_box: BoxShape3D = collision_shape.shape as BoxShape3D
	var placeholder_label: Label3D = building.get_node("PlaceholderLabel") as Label3D
	var name_label: Label3D = building.get_node("NameLabel") as Label3D
	assert_eq(art.texture, texture)
	assert_eq(art.position.y, 0.0)
	assert_gt(art.offset.y, 0.0)
	var visible_width: float = texture.get_image().get_used_rect().size.x * art.pixel_size
	assert_almost_eq(collision_box.size.x, visible_width * building.collision_width_ratio, 0.0001)
	assert_almost_eq(collision_box.size.z, building.collision_depth, 0.0001)
	assert_lt(collision_box.size.x, visible_width)
	assert_true(placeholder_label.text.begins_with("PLACEHOLDER"))
	var screen_up: Vector3 = campus_camera.global_basis.y.normalized()
	var placeholder_offset: Vector3 = placeholder_label.global_position - building.global_position
	var name_offset: Vector3 = name_label.global_position - building.global_position
	assert_gt(placeholder_offset.dot(screen_up), building._placeholder_art_height)
	assert_gt(name_offset.dot(screen_up), placeholder_offset.dot(screen_up))
	assert_ne(placeholder_label.global_position.z, building.global_position.z)
	assert_eq(placeholder_label.render_priority, art.render_priority + 1)
	assert_eq(name_label.render_priority, art.render_priority + 1)
