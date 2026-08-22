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
	assert_not_null(hub.get_node_or_null("Interface/RightActionRail"))
	assert_not_null(hub.get_node_or_null("Interface/RightActionRail/ShortcutButton"))
	assert_not_null(hub.get_node_or_null("Interface/ShortcutPanel"))
	assert_not_null(hub.get_node_or_null("Interface/RightActionRail/JournalButton"))
	assert_not_null(hub.get_node_or_null("Interface/RightActionRail/InventoryButton"))
	assert_not_null(hub.get_node_or_null("Interface/RightActionRail/SpellbookButton"))
	assert_not_null(hub.get_node_or_null("Interface/TrackedQuestBanner/TrackedQuestButton"))
	assert_not_null(hub.get_node_or_null("Interface/NpcDialogueBox"))
	assert_null(hub.get_node_or_null("Interface/ProfessorDialog"))
	assert_not_null(hub.get_node_or_null("Professors"))
	assert_not_null(hub.get_node_or_null("QuestTargets"))
	assert_not_null(hub.get_node_or_null("PlaceholderCrowd"))
	assert_not_null(hub.get_node_or_null("PlaceholderScenery"))
	assert_not_null(hub.get_node_or_null("PlaceholderWater"))
	var ground: MeshInstance3D = hub.get_node("Ground") as MeshInstance3D
	var ground_material: StandardMaterial3D = ground.material_override as StandardMaterial3D
	assert_not_null(ground_material)
	assert_true(ground_material.albedo_texture.resource_path.contains("/placeholders/tiny_swords/terrain/"))
	assert_gt(ground_material.uv1_scale.x, 1.0)
	assert_gt(ground_material.uv1_scale.y, 1.0)
	hub.free()


func test_hub_uses_a_vertical_icon_action_rail() -> void:
	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	var rail: VBoxContainer = hub.get_node("Interface/RightActionRail") as VBoxContainer
	var journal: Button = rail.get_node("JournalButton") as Button
	var spellbook: Button = rail.get_node("SpellbookButton") as Button
	var inventory: Button = rail.get_node("InventoryButton") as Button
	var shortcuts: Button = rail.get_node("ShortcutButton") as Button
	assert_eq(rail.get_child_count(), 4)
	assert_eq(rail.anchor_top, 0.5)
	assert_eq(rail.anchor_bottom, 0.5)
	assert_almost_eq(absf(rail.offset_top), rail.offset_bottom, 0.01)
	assert_not_null(journal.icon)
	assert_not_null(spellbook.icon)
	assert_not_null(inventory.icon)
	assert_not_null(shortcuts.icon)
	assert_true(journal.text.is_empty())
	assert_true(spellbook.text.is_empty())
	assert_true(inventory.text.is_empty())
	assert_true(shortcuts.text.is_empty())
	assert_false(inventory.disabled)
	hub.free()


func test_spellbook_is_a_persistent_right_side_action_instead_of_a_building() -> void:
	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	var rail: VBoxContainer = hub.get_node("Interface/RightActionRail") as VBoxContainer
	var button: Button = rail.get_node("SpellbookButton") as Button
	assert_eq(rail.anchor_top, 0.5)
	assert_eq(rail.anchor_bottom, 0.5)
	assert_lt(rail.offset_left, 0.0)
	assert_not_null(button.icon)
	assert_true(button.text.is_empty())
	assert_eq(
		hub._scene_for_destination(WalkableAcademyHub.DESTINATION_SPELLBOOK),
		SceneManager.SCENE_COLLECTION_SCREEN
	)
	var spellbook_destination: Dictionary = {}
	for destination: Dictionary in WalkableAcademyHub.DESTINATIONS:
		if destination["id"] == WalkableAcademyHub.DESTINATION_SPELLBOOK:
			spellbook_destination = destination
			break
	assert_false(spellbook_destination.is_empty())
	assert_false(spellbook_destination.has("position"))
	hub.free()


func test_inventory_action_opens_the_combined_summoner_build_screen() -> void:
	var source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/walkable_academy_hub.gd"
	)
	assert_true(
		source.contains("inventory_button.pressed.connect(_route_to.bind(DESTINATION_SUMMONER))")
	)


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

	assert_eq(building_count, 3)
	assert_eq(hub._scene_for_destination(WalkableAcademyHub.DESTINATION_SUMMONER), SceneManager.SCENE_SUMMONER_SCREEN)
	assert_eq(hub._scene_for_destination(WalkableAcademyHub.DESTINATION_JOURNAL), SceneManager.SCENE_QUEST_JOURNAL)
	assert_eq(hub._scene_for_destination(WalkableAcademyHub.DESTINATION_SETTINGS), SceneManager.SCENE_SETTINGS)
	hub.free()


func test_quest_journal_scene_exposes_three_authoritative_sections() -> void:
	assert_true(ResourceLoader.exists(SceneManager.SCENE_QUEST_JOURNAL))
	var packed_scene: PackedScene = load(SceneManager.SCENE_QUEST_JOURNAL) as PackedScene
	var journal: QuestJournal = packed_scene.instantiate() as QuestJournal
	assert_not_null(journal)
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/CategoryPanel/CategoryMargin/Categories/ActiveButton"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/CategoryPanel/CategoryMargin/Categories/OpenButton"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/CategoryPanel/CategoryMargin/Categories/CompletedButton"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/ListPanel/ListMargin/ListRoot/QuestScroll/QuestList"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/DetailPanel"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/DetailPanel/DetailMargin/DetailLayout/QuestDetailPanel/DetailContent/ProfessorRow/ProfessorPortrait"))
	assert_not_null(journal.get_node_or_null("Margin/Root/Body/DetailPanel/DetailMargin/DetailLayout/QuestDetailPanel/DetailContent/RewardsScroll/RewardsList"))
	journal.free()


func test_quest_journal_uses_the_shared_card_widget_for_card_rewards() -> void:
	assert_not_null(QuestDetailPanel.CardWidgetScene)
	var card_widget: CardWidget = QuestDetailPanel.CardWidgetScene.instantiate() as CardWidget
	assert_not_null(card_widget)
	assert_not_null(card_widget.get_node_or_null("CardPanel/ContentContainer/ArtContainer"))
	card_widget.free()


func test_interactive_npc_component_is_role_agnostic() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/interactive_npc.tscn") as PackedScene
	var npc: InteractiveNpc = packed_scene.instantiate() as InteractiveNpc
	assert_not_null(npc)
	assert_not_null(npc.get_node_or_null("CharacterVisual"))
	assert_not_null(npc.get_node_or_null("MarkerLabel"))
	assert_not_null(npc.get_node_or_null("InteractionArea/CollisionShape3D"))
	npc.free()


func test_quest_world_target_component_is_generic_and_interactable() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/quest_world_target.tscn")
	var target: QuestWorldTarget = packed_scene.instantiate() as QuestWorldTarget
	assert_not_null(target)
	target.configure("practice_grounds", "Practice Grounds")
	target.set_current_objective(true)
	assert_eq(target.target_id, "practice_grounds")
	assert_true((target.get_node("MarkerLabel") as Label3D).visible)
	assert_not_null(target.get_node_or_null("InteractionArea/CollisionShape3D"))
	target.free()


func test_npc_dialogue_uses_bottom_screen_rich_text_and_inline_choices() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/npc_dialogue_box.tscn") as PackedScene
	var dialogue: NpcDialogueBox = packed_scene.instantiate() as NpcDialogueBox
	assert_not_null(dialogue)
	var panel: PanelContainer = dialogue.get_node("Panel") as PanelContainer
	var line: RichTextLabel = dialogue.get_node("Panel/Margin/Content/LineLabel") as RichTextLabel
	assert_eq(panel.anchor_top, 1.0)
	assert_true(line.bbcode_enabled)
	assert_not_null(dialogue.get_node_or_null("Panel/Margin/Content/Choices"))
	assert_not_null(dialogue.get_node_or_null("Panel/Margin/Content/AdvanceIndicator"))
	dialogue.free()


func test_npc_dialogue_renders_player_responses_as_full_width_spoken_lines() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/npc_dialogue_box.tscn")
	var dialogue: NpcDialogueBox = packed_scene.instantiate() as NpcDialogueBox
	add_child_autofree(dialogue)
	await get_tree().process_frame
	dialogue.present("Professor", [], [{"id": "accept", "text": "I'm ready."}])
	var responses: VBoxContainer = dialogue.get_node("Panel/Margin/Content/Choices")
	assert_eq(responses.get_child_count(), 1)
	var response: Button = responses.get_child(0) as Button
	assert_true(response.text.contains("I'm ready."))
	assert_eq(response.alignment, HORIZONTAL_ALIGNMENT_LEFT)
	assert_eq(response.size_flags_horizontal, Control.SIZE_EXPAND_FILL)


func test_clicking_visible_dialogue_panel_advances_to_next_line() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/npc_dialogue_box.tscn")
	var dialogue: NpcDialogueBox = packed_scene.instantiate() as NpcDialogueBox
	add_child_autofree(dialogue)
	await get_tree().process_frame
	dialogue.present("Professor", ["First line", "Second line"])
	var panel: PanelContainer = dialogue.get_node("Panel") as PanelContainer
	var line: RichTextLabel = dialogue.get_node("Panel/Margin/Content/LineLabel")
	assert_eq(line.text, "First line")
	var click: InputEventMouseButton = InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_RIGHT
	click.pressed = true
	panel.gui_input.emit(click)
	assert_eq(line.text, "First line")
	click = InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	panel.gui_input.emit(click)
	assert_eq(line.text, "Second line")


func test_skipping_dialogue_stops_at_required_player_response() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/components/npc_dialogue_box.tscn")
	var dialogue: NpcDialogueBox = packed_scene.instantiate() as NpcDialogueBox
	add_child_autofree(dialogue)
	await get_tree().process_frame
	dialogue.present(
		"Professor",
		["First line", "Second line"],
		[{"id": "accept", "text": "I'm ready."}]
	)
	dialogue._skip_to_choices_or_dismiss()
	var responses: VBoxContainer = dialogue.get_node("Panel/Margin/Content/Choices")
	assert_true(dialogue.visible)
	assert_eq(responses.get_child_count(), 1)
	await get_tree().process_frame
	assert_eq(get_viewport().gui_get_focus_owner(), responses.get_child(0))


func test_intro_offer_uses_authored_player_response_instead_of_rule_callouts() -> void:
	var script_text: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	var quest_file: FileAccess = FileAccess.open("res://data/quests/quests.json", FileAccess.READ)
	assert_not_null(quest_file)
	var quest_text: String = quest_file.get_as_text()
	quest_file.close()
	assert_true(script_text.contains('opportunity.get("response_choices")'))
	assert_false(script_text.contains('Loc.t("academy.quest.permanent_cost"'))
	assert_false(script_text.contains('Loc.t("academy.quest.assignment_callout"'))
	assert_true(quest_text.contains('"action": "accept_quest"'))
	assert_false(quest_text.contains('"action": "decline_quest"'))


func test_quest_turn_in_opens_generic_reward_modal() -> void:
	var script_text: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	assert_false(script_text.contains("consume_last_academy_completion_summary"))
	assert_true(script_text.contains('result.get("completion_summary")'))
	assert_true(script_text.contains("reward_modal.present"))
	assert_true(script_text.contains("academy.quest.complete"))
	var packed_scene: PackedScene = load(HUB_SCENE_PATH) as PackedScene
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	assert_not_null(hub.get_node_or_null("Interface/RewardGrantModal"))
	hub.free()


func test_quest_offer_and_journal_share_the_same_detail_component() -> void:
	var hub_script: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	assert_true(hub_script.contains("quest_offer_modal.present"))
	assert_true(hub_script.contains("_on_quest_offer_backed"))
	assert_true(hub_script.contains("_on_quest_offer_accepted"))
	var journal_scene: PackedScene = load(SceneManager.SCENE_QUEST_JOURNAL) as PackedScene
	var journal: QuestJournal = journal_scene.instantiate() as QuestJournal
	var journal_detail: QuestDetailPanel = journal.get_node(
		"Margin/Root/Body/DetailPanel/DetailMargin/DetailLayout/QuestDetailPanel"
	) as QuestDetailPanel
	var offer_scene: PackedScene = load(
		"res://scenes/meta/components/quest_offer_modal.tscn"
	) as PackedScene
	var offer: QuestOfferModal = offer_scene.instantiate() as QuestOfferModal
	var offer_detail: QuestDetailPanel = offer.get_node(
		"Center/Panel/Margin/Content/QuestDetailPanel"
	) as QuestDetailPanel
	assert_not_null(journal_detail)
	assert_not_null(offer_detail)
	assert_not_null(offer.get_node_or_null("Center/Panel/Margin/Content/Actions/BackButton"))
	assert_not_null(offer.get_node_or_null("Center/Panel/Margin/Content/Actions/AcceptButton"))
	journal.free()
	offer.free()


func test_quest_offer_previews_card_and_back_closes_without_accepting() -> void:
	var offer_scene: PackedScene = load(
		"res://scenes/meta/components/quest_offer_modal.tscn"
	) as PackedScene
	var offer: QuestOfferModal = offer_scene.instantiate() as QuestOfferModal
	add_child_autofree(offer)
	await get_tree().process_frame
	offer.present({
		"id": "introduction_to_magic",
		"title_key": "academy.course.introduction_to_magic_101.name",
		"description_key": "academy.course.introduction_to_magic_101.description",
		"source_name_key": "academy.professor.general_magic.name",
		"location_key": "academy.location.general_grounds",
		"curriculum_cost": 1,
		"reward_previews": [{
			"options": [{
				"grants": [{"kind": "card", "card_id": "magic_bolt", "amount": 1}],
			}],
		}],
	})
	assert_true(offer.visible)
	var detail: QuestDetailPanel = offer.get_node(
		"Center/Panel/Margin/Content/QuestDetailPanel"
	) as QuestDetailPanel
	assert_eq(detail.rewards_list.get_child_count(), 1)
	assert_true(detail.rewards_list.get_child(0) is CardWidget)
	offer._back()
	assert_false(offer.visible)


func test_non_academic_quest_offer_does_not_invent_curriculum_cost() -> void:
	var offer_scene: PackedScene = load(
		"res://scenes/meta/components/quest_offer_modal.tscn"
	) as PackedScene
	var offer: QuestOfferModal = offer_scene.instantiate() as QuestOfferModal
	add_child_autofree(offer)
	await get_tree().process_frame
	offer.present({
		"id": "side_quest",
		"title_key": "academy.journal.title",
		"description_key": "academy.journal.description",
	})
	var detail: QuestDetailPanel = offer.get_node(
		"Center/Panel/Margin/Content/QuestDetailPanel"
	) as QuestDetailPanel
	assert_false(detail.detail_status.visible)


func test_tracked_quest_is_a_wide_semitransparent_banner() -> void:
	var packed_scene: PackedScene = load(HUB_SCENE_PATH)
	var hub: WalkableAcademyHub = packed_scene.instantiate() as WalkableAcademyHub
	var banner: Control = hub.get_node("Interface/TrackedQuestBanner") as Control
	var background: ColorRect = banner.get_node("Background") as ColorRect
	var button: Button = banner.get_node("TrackedQuestButton") as Button
	assert_true(background.material is ShaderMaterial)
	assert_gt(banner.size.x, banner.size.y * 5.0)
	assert_gt(banner.position.y, 168.0)
	assert_true(button.flat)
	hub.free()


func test_quest_journal_uses_readable_neutral_background() -> void:
	var packed_scene: PackedScene = load(SceneManager.SCENE_QUEST_JOURNAL) as PackedScene
	var journal: QuestJournal = packed_scene.instantiate() as QuestJournal
	var background: ColorRect = journal.get_node("Background") as ColorRect
	assert_gt(background.color.r, background.color.b)
	assert_gt(background.color.get_luminance(), 0.5)
	journal.free()


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents


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
	assert_eq(ground_plane.size.x, 6.0 * hub.ground_tile_world_size)
	assert_eq(ground_plane.size.y, 3.0 * hub.ground_tile_world_size)
	var top_edge: MeshInstance3D = ground.get_node_or_null("TopEdge") as MeshInstance3D
	assert_not_null(top_edge)
	var top_edge_material: StandardMaterial3D = top_edge.material_override as StandardMaterial3D
	assert_eq(top_edge_material.transparency, BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR)
	assert_not_null(ground.get_node_or_null("BottomRightCorner"))
	var cliff_middle: MeshInstance3D = ground.get_node_or_null("FrontCliffCenter") as MeshInstance3D
	var cliff_left: MeshInstance3D = ground.get_node_or_null("FrontCliffLeft") as MeshInstance3D
	var cliff_right: MeshInstance3D = ground.get_node_or_null("FrontCliffRight") as MeshInstance3D
	assert_not_null(cliff_middle)
	assert_not_null(cliff_left)
	assert_not_null(cliff_right)
	assert_true(cliff_middle.mesh is QuadMesh, "The illustrated stone should retain its vertical presentation")
	assert_lt(cliff_middle.position.y, 0.0)
	var bottom_edge: MeshInstance3D = ground.get_node("BottomEdge") as MeshInstance3D
	var bottom_edge_plane: PlaneMesh = bottom_edge.mesh as PlaneMesh
	var grass_front: float = bottom_edge.position.z + bottom_edge_plane.size.y * 0.5
	assert_almost_eq(cliff_middle.position.z, grass_front, 0.0001, "The cliff must hang directly beneath the front grass edge")
	assert_eq((cliff_left.material_override as StandardMaterial3D).albedo_texture, WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_LEFT)
	assert_eq((cliff_right.material_override as StandardMaterial3D).albedo_texture, WalkableAcademyHub.PLACEHOLDER_CLIFF_MIDDLE_RIGHT)
	assert_null(ground.get_node_or_null("CliffBottomCenter"))
	var water_surface: MeshInstance3D = hub.get_node("PlaceholderWater/Surface") as MeshInstance3D
	var water_material: StandardMaterial3D = water_surface.material_override as StandardMaterial3D
	assert_eq(water_material.albedo_texture, WalkableAcademyHub.PLACEHOLDER_WATER_BACKGROUND)
	assert_eq(water_material.albedo_color, hub.water_tint)
	assert_lt(water_surface.position.y, cliff_middle.position.y)
	var foam: Node3D = hub.get_node("PlaceholderWater/Foam") as Node3D
	assert_eq(foam.get_child_count(), 22, "Every shoreline cell should receive one foam animation")
	var foam_piece: MeshInstance3D = foam.get_child(0) as MeshInstance3D
	var foam_mesh: PlaneMesh = foam_piece.mesh as PlaneMesh
	assert_eq(
		foam_mesh.size,
		Vector2.ONE * hub.ground_tile_world_size * WalkableAcademyHub.WATER_FOAM_TILE_SPAN
	)
	var foam_material: ShaderMaterial = foam_piece.material_override as ShaderMaterial
	assert_eq(foam_material.get_shader_parameter("foam_texture"), WalkableAcademyHub.PLACEHOLDER_WATER_FOAM)
	var next_foam_piece: MeshInstance3D = foam.get_child(1) as MeshInstance3D
	var next_foam_material: ShaderMaterial = next_foam_piece.material_override as ShaderMaterial
	assert_ne(
		foam_material.get_shader_parameter("animation_offset"),
		next_foam_material.get_shader_parameter("animation_offset"),
		"Neighboring foam animations should start on different frames"
	)


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


func test_placeholder_scenery_frames_the_island_and_keeps_water_props_off_land() -> void:
	assert_eq(PlaceholderCampusScenery.LAND_PLACEMENTS.size(), 28)
	assert_eq(PlaceholderCampusScenery.WATER_PLACEMENTS.size(), 12)
	for placement: Dictionary in PlaceholderCampusScenery.LAND_PLACEMENTS:
		assert_not_null(placement["texture"])
		assert_gt(int(placement["frames"]), 1)
		var position: Vector3 = placement["position"]
		assert_eq(position.y, 0.0)
		assert_true(
			absf(position.x) >= 18.0 or absf(position.z) >= 16.0,
			"Land scenery should leave the central campus open"
		)
		assert_lt(absf(position.x), 36.0)
		assert_lt(absf(position.z), 22.5)
	for placement: Dictionary in PlaceholderCampusScenery.WATER_PLACEMENTS:
		assert_not_null(placement["texture"])
		assert_gt(int(placement["frames"]), 1)
		var position: Vector3 = placement["position"]
		assert_true(
			absf(position.x) > 36.0 or absf(position.z) > 22.5,
			"Water scenery must remain outside the island shoreline"
		)

	var scenery: PlaceholderCampusScenery = PlaceholderCampusScenery.new()
	add_child(scenery)
	await get_tree().process_frame
	assert_eq(scenery.get_child_count(), 40)
	var conifer: Sprite3D = scenery.get_node("NorthwestConifer") as Sprite3D
	var duck: Sprite3D = scenery.get_node("FrontLeftDuck") as Sprite3D
	assert_eq(conifer.hframes, 8, "Conifer sheets use eight 192px-wide frames")
	assert_eq(duck.hframes, 3)
	assert_eq(conifer.billboard, BaseMaterial3D.BILLBOARD_ENABLED)
	assert_eq(duck.billboard, BaseMaterial3D.BILLBOARD_ENABLED)
	remove_child(scenery)
	scenery.free()


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
		SafeTypeUtils.string(destination["target_scene"]),
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
