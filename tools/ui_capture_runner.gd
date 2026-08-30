extends Node

const OUTPUT_DIR: String = "res://docs/art/commissions/ui-design-handoff/screenshots"
var _capture_viewport: SubViewport


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_capture_viewport = SubViewport.new()
	_capture_viewport.size = Vector2i(1920, 1080)
	_capture_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	_capture_viewport.transparent_bg = false
	_capture_viewport.gui_embed_subwindows = true
	add_child(_capture_viewport)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT_DIR))
	await get_tree().process_frame
	ProfileRepoApi.load_profile("ui_capture")
	if "--welcome-only" in OS.get_cmdline_user_args():
		_seed_capture_profile()
		WalkableAcademyHub.reset_showcase_welcome_for_run()
		var welcome_screen: Node = _add_scene(
			"res://scenes/meta/screens/walkable_academy_hub.tscn"
		)
		await _settle(8)
		await _save_viewport("walkthrough-welcome.png")
		await _remove_scene(welcome_screen)
		get_tree().quit()
		return
	if "--summoner-switch-only" in OS.get_cmdline_user_args():
		await _settle()
		await _capture_summoner_switch_carousel()
		get_tree().quit()
		return
	if "--journal-only" in OS.get_cmdline_user_args():
		await _capture_journal()
		get_tree().quit()
		return
	if "--dialogue-only" in OS.get_cmdline_user_args():
		await _capture_dialogue()
		get_tree().quit()
		return
	if "--results-only" in OS.get_cmdline_user_args():
		_seed_capture_profile()
		await _settle()
		await _capture_results()
		get_tree().quit()
		return
	if "--battle-overlay-only" in OS.get_cmdline_user_args():
		await _capture_battle_victory_overlay()
		get_tree().quit()
		return
	ProfileRepoApi.reset_profile()
	await get_tree().process_frame
	await _capture_title()
	await _capture_summoner_selection()
	_seed_capture_profile()
	await _settle()
	await _capture_summoner_reveal()
	await _capture_basic_screen("res://scenes/meta/screens/summoner_screen.tscn", "summoner-profile.png", "open_profile", [String(SummonerIDs.TEO)])
	await _capture_summoner_switch_carousel()
	await _capture_trait_development()
	await _capture_shop()
	await _capture_collection()
	await _capture_inventory()
	await _capture_journal()
	await _capture_dialogue()
	await _capture_activity_preparation()
	await _capture_results()
	await _capture_basic_screen("res://scenes/meta/screens/settings_screen.tscn", "settings.png")
	if not "--skip-battle" in OS.get_cmdline_user_args():
		await _capture_battle()
	get_tree().quit()


func _capture_title() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/title_screen.tscn")
	await get_tree().process_frame
	await _save_viewport("title-loading.png")
	await _remove_scene(screen)


func _capture_summoner_selection() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/summoner_selection.tscn")
	await get_tree().process_frame
	if screen.has_method("_show_summoner_selection"):
		screen.call("_show_summoner_selection")
	await _settle()
	var presenter: CanvasItem = screen.get_node_or_null("NarrativePresenter") as CanvasItem
	if presenter:
		presenter.visible = false
	await _settle(2)
	await _save_viewport("summoner-selection.png")
	await _remove_scene(screen)


func _seed_capture_profile() -> void:
	DevConsole.execute_command("/unlock_all_summoners")
	SummonerSelectionApi.set_active_summoner(String(SummonerIDs.TEO), {})
	DevConsole.execute_command("/save_grant_cards 45")
	var profile: Dictionary = ProfileRepoApi.get_profile_data()
	var resources: Dictionary = SafeTypeUtils.dict(profile.get("resources"))
	resources["gold"] = 2500
	profile["resources"] = resources
	profile["items"] = [
		{"id": "capture-training-blade", "catalog_id": "item_training_blade", "equipped_by": null, "bound_to": null, "slot": null},
		{"id": "capture-simple-ring", "catalog_id": "item_simple_ring", "equipped_by": null, "bound_to": null, "slot": null},
		{"id": "capture-lucky-band", "catalog_id": "item_lucky_band", "equipped_by": null, "bound_to": null, "slot": null},
		{"id": "capture-travelers-cloak", "catalog_id": "item_travelers_cloak", "equipped_by": null, "bound_to": null, "slot": null},
		{"id": "capture-veterans-medal", "catalog_id": "item_veterans_medal", "equipped_by": null, "bound_to": null, "slot": null},
	]
	var summoner_instances: Array = SafeTypeUtils.array(profile.get("summoner_instances"))
	for index: int in range(summoner_instances.size()):
		var summoner: Dictionary = SafeTypeUtils.dict(summoner_instances[index])
		if SafeTypeUtils.string(summoner.get("summoner_id")) == String(SummonerIDs.TEO):
			summoner["level"] = 2
			summoner_instances[index] = summoner
	profile["summoner_instances"] = summoner_instances
	ProfileRepoApi.load_profile_data(profile)
	SummonerSelectionApi.set_active_summoner(String(SummonerIDs.TEO), {})
	var card_ids: Array = []
	for value: Variant in CardServiceApi.list_cards_dict().slice(0, DeckConstants.MAX_DECK_SIZE):
		card_ids.append(SafeTypeUtils.string(SafeTypeUtils.dict(value).get("id")))
	var deck_id: String = DecksApi.create_deck_from_dict("Ember Practicum", card_ids, String(SummonerIDs.TEO))
	if not deck_id.is_empty():
		DecksApi.set_active_deck(deck_id)
	DevConsole.execute_command("/traits_grant_summoner_points 4 %s" % String(SummonerIDs.TEO))


func _capture_summoner_reveal() -> void:
	NavigationContext.set_value("summoner_reveal.result", {
		"summoner_id": String(SummonerIDs.TEO),
		"was_random": false,
	})
	await _capture_basic_screen("res://scenes/meta/modals/summoner_reveal.tscn", "summoner-reveal.png")


func _capture_summoner_switch_carousel() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/summoner_switch_screen.tscn")
	await _settle(6)
	if screen.has_method("_on_right_arrow_pressed"):
		screen.call("_on_right_arrow_pressed")
	await _settle(30)
	await _save_viewport("summoner-switch-carousel-sprites.png")
	await _remove_scene(screen)


func _capture_trait_development() -> void:
	var view_model: Dictionary = TraitTreeApi.get_summoner_tree_view_model(String(SummonerIDs.TEO))
	var anchor_id: String = _first_trait_id(view_model)
	if anchor_id.is_empty():
		push_warning("UICaptureRunner: no summoner trait anchor found")
		return
	var overlay: Node = _add_scene("res://scenes/meta/components/trait_development_overlay.tscn")
	await get_tree().process_frame
	overlay.call("open_for_summoner", String(SummonerIDs.TEO), anchor_id)
	await _settle()
	await _save_viewport("trait-development.png")
	if overlay.has_method("_show_node_detail"):
		overlay.call("_show_node_detail", anchor_id)
	await _settle(2)
	await _save_viewport("trait-development-node-detail.png")
	if overlay.has_method("_on_action_pressed"):
		overlay.call("_on_action_pressed")
		await _settle(2)
		await _save_viewport("trait-development-confirmation.png")
	await _remove_scene(overlay)


func _capture_shop() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/shop_screen.tscn")
	await _settle()
	await _save_viewport("shop.png")
	var offerings: Array = screen.get("current_offerings")
	if not offerings.is_empty():
		screen.call("_open_detail_modal", offerings[0])
		await _settle(2)
		await _save_viewport("shop-item-detail.png")
	await _remove_scene(screen)


func _capture_collection() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/collection_screen.tscn")
	await get_tree().process_frame
	screen.call("open_collection")
	await _settle(6)
	await _save_viewport("collection-decks.png")
	var cards: Array = CardServiceApi.list_cards_dict()
	if not cards.is_empty():
		var card: Dictionary = SafeTypeUtils.dict(cards[0])
		screen.call("_open_card_detail_modal", SafeTypeUtils.string(card.get("id")), SafeTypeUtils.string(card.get("catalog_id")))
		await _settle(3)
		await _save_viewport("collection-card-detail.png")
		var modal: Node = screen.get_node_or_null("CardDetailModal")
		if modal:
			modal.queue_free()
			await _settle(2)
	if screen.has_method("_on_new_deck_pressed"):
		screen.call("_on_new_deck_pressed")
		await _settle(2)
		await _save_viewport("collection-new-deck-dialog.png")
	await _remove_scene(screen)


func _capture_inventory() -> void:
	var overlay: Node = _add_scene("res://scenes/meta/components/inventory_overlay.tscn")
	await get_tree().process_frame
	overlay.call("open_inventory", String(SummonerIDs.TEO))
	await _settle()
	await _save_viewport("inventory.png")
	var items: Array = ItemsApi.get_owned_items_dict(String(SummonerIDs.TEO))
	if not items.is_empty():
		overlay.call("_on_item_selected", SafeTypeUtils.dict(items[0]))
		await _settle(2)
		await _save_viewport("inventory-item-detail.png")
	await _remove_scene(overlay)


func _capture_results() -> void:
	var cards: Array = CardServiceApi.list_cards_dict()
	var card_instance_id: String = ""
	if not cards.is_empty():
		card_instance_id = SafeTypeUtils.string(SafeTypeUtils.dict(cards[0]).get("id"))
	var grants: Array[Dictionary] = [
		{"kind": "summoner_xp", "amount": 75},
		{"kind": "currency", "content_id": "gold", "amount": 180},
		{"kind": "card", "content_id": "fireball", "amount": 1},
	]
	if not card_instance_id.is_empty():
		grants.append({"kind": "card_xp", "target_id": card_instance_id, "amount": 35})
	var report: Dictionary = {
		"outcome": "victory",
		"grants": grants,
	}
	var screen: Node = _add_scene("res://scenes/meta/screens/post_battle_results.tscn")
	await get_tree().process_frame
	screen.call("present", report)
	await _settle(5)
	await _save_viewport("post-battle-results-rewards.png")
	await _remove_scene(screen)


func _capture_journal() -> void:
	var screen: Node = _add_scene("res://scenes/meta/screens/quest_journal.tscn")
	await get_tree().process_frame
	screen.call("open_journal")
	var journal_state: Dictionary = SafeTypeUtils.dict(screen.get("_journal_state"))
	var open_entries: Array = SafeTypeUtils.array(journal_state.get("opportunities"))
	if not open_entries.is_empty():
		var entry: Dictionary = SafeTypeUtils.dict(open_entries[0])
		var quest_id: String = SafeTypeUtils.string(entry.get("id"))
		screen.set("_section", "open")
		screen.set("_section_entries", open_entries)
		screen.set("_selected_quest_id", quest_id)
		screen.call("_refresh_category_buttons")
		var quest_list: VBoxContainer = screen.get_node("%QuestList") as VBoxContainer
		for child: Node in quest_list.get_children():
			child.queue_free()
		var button: Button = Button.new()
		button.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size = Vector2(0.0, 68.0)
		button.button_pressed = true
		quest_list.add_child(button)
		screen.get_node("%ListEmpty").visible = false
		screen.call("_render_detail", entry)
	await _settle(4)
	await _save_viewport("journal-open-quest.png")
	await _remove_scene(screen)


func _capture_dialogue() -> void:
	var backdrop: ColorRect = ColorRect.new()
	backdrop.position = Vector2.ZERO
	backdrop.size = Vector2(1920, 1080)
	backdrop.color = Color(0.24, 0.22, 0.20, 1.0)
	_capture_viewport.add_child(backdrop)
	var dialogue: NpcDialogueBox = _add_scene(
		"res://scenes/meta/components/npc_dialogue_box.tscn"
	) as NpcDialogueBox
	await get_tree().process_frame
	var lines: Array[String] = [
		"Your first lesson begins when you are ready."
	]
	var choices: Array[Dictionary] = [
		{"id": "begin", "text": "Begin the lesson."},
		{"id": "questions", "text": "I have a few questions first."},
	]
	dialogue.present("Professor Merriweather", lines, choices)
	await _settle(3)
	await _save_viewport("dialogue-line.png")
	dialogue.call("_skip_to_choices_or_dismiss")
	await _settle(3)
	await _save_viewport("dialogue-choices.png")
	await _remove_scene(dialogue)
	await _remove_scene(backdrop)


func _capture_activity_preparation() -> void:
	BattleContext.select_encounter("intro_summoning_practice")
	var screen: Node = _add_scene("res://scenes/meta/screens/academy_activity_preparation.tscn")
	await _settle(8)
	if is_instance_valid(screen) and screen.is_inside_tree():
		await _save_viewport("activity-preparation-context.png")
	await _remove_scene(screen)


func _capture_battle() -> void:
	BattleContext.configure_practice_battle({
		"player_summoner_id": String(SummonerIDs.TEO),
		"opponent_summoner_id": String(SummonerIDs.COLE),
		"enemy_side": {
			"summoner": {"id": String(SummonerIDs.COLE), "display_name": "Cole", "hp": 300.0, "max_hp": 300.0},
			"deck": {"cards": [{"catalog_id": "pebbloom", "count": 6}]},
			"ai_type": "basic",
		},
	})
	var battle: Node = _add_scene("res://scenes/battle/battlefield/battle_3d.tscn")
	await _settle(90)
	if is_instance_valid(battle) and battle.is_inside_tree():
		await _save_viewport("battle-hud.png")
		var hud: Node = battle.get_node_or_null("UI")
		var game_over_modal: CanvasItem = hud.get_node_or_null("GameOverModal") as CanvasItem if hud else null
		var game_over_label: Label = hud.get_node_or_null("GameOverModal/Content/GameOverLabel") as Label if hud else null
		if game_over_modal and game_over_label:
			game_over_label.text = Loc.t("ui.post_battle.victory")
			game_over_modal.visible = true
			await _settle(3)
			await _save_viewport("battle-victory-overlay.png")
			game_over_modal.visible = false
		var pause_menu: CanvasItem = hud.get_node_or_null("PauseMenu") as CanvasItem if hud else null
		if pause_menu:
			pause_menu.visible = true
			await _settle(3)
			await _save_viewport("battle-pause-menu.png")
			if pause_menu.has_method("_on_quit_pressed"):
				pause_menu.call("_on_quit_pressed")
				await _settle(2)
				await _save_viewport("battle-forfeit-confirmation.png")
				var forfeit_dialog: Window = pause_menu.get_node_or_null("ForfeitConfirmation") as Window
				if forfeit_dialog:
					forfeit_dialog.hide()
			if pause_menu.has_method("_on_settings_pressed"):
				pause_menu.call("_on_settings_pressed")
				await _settle(3)
				await _save_viewport("battle-pause-settings.png")
	await _remove_scene(battle)
	BattleContext.clear()


func _capture_battle_victory_overlay() -> void:
	var backdrop: TextureRect = TextureRect.new()
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.texture = load("%s/battle-hud.png" % OUTPUT_DIR)
	backdrop.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_capture_viewport.add_child(backdrop)
	var hud: Node = _add_scene("res://scenes/battle/ui/battle_hud.tscn")
	await _settle(2)
	for node_path: String in ["InputCollector", "HUDContainer", "SpeedButton", "PauseButton", "PauseMenu"]:
		var item: CanvasItem = hud.get_node_or_null(node_path) as CanvasItem
		if item:
			item.visible = false
	var game_over_label: Label = hud.get_node_or_null("GameOverModal/Content/GameOverLabel") as Label
	var game_over_modal: CanvasItem = hud.get_node_or_null("GameOverModal") as CanvasItem
	if game_over_label and game_over_modal:
		game_over_label.text = Loc.t("ui.post_battle.victory")
		game_over_modal.visible = true
		await _settle(3)
		await _save_viewport("battle-victory-overlay.png")
	await _remove_scene(hud)
	await _remove_scene(backdrop)


func _capture_basic_screen(scene_path: String, file_name: String, open_method: String = "", args: Array = []) -> void:
	var screen: Node = _add_scene(scene_path)
	await get_tree().process_frame
	if not open_method.is_empty() and screen.has_method(open_method):
		screen.callv(open_method, args)
	await _settle()
	await _save_viewport(file_name)
	await _remove_scene(screen)


func _add_scene(scene_path: String) -> Node:
	var scene: PackedScene = load(scene_path)
	var instance: Node = scene.instantiate()
	_capture_viewport.add_child(instance)
	return instance


func _remove_scene(instance: Node) -> void:
	if is_instance_valid(instance):
		instance.queue_free()
	await get_tree().process_frame
	await get_tree().process_frame


func _first_trait_id(view_model: Dictionary) -> String:
	for key: String in ["nodes", "trait_nodes", "progression_nodes"]:
		for value: Variant in SafeTypeUtils.array(view_model.get(key)):
			var node: Dictionary = SafeTypeUtils.dict(value)
			var trait_id: String = SafeTypeUtils.string(node.get("id"))
			if not trait_id.is_empty():
				return trait_id
	return ""


func _settle(frames: int = 4) -> void:
	for _frame: int in range(frames):
		await get_tree().process_frame


func _save_viewport(file_name: String) -> void:
	await RenderingServer.frame_post_draw
	var image: Image = _capture_viewport.get_texture().get_image()
	var path: String = "%s/%s" % [OUTPUT_DIR, file_name]
	var error: Error = image.save_png(path)
	if error != OK:
		push_error("UICaptureRunner: failed to save %s (%s)" % [path, error])
	else:
		print("UICaptureRunner: saved %s" % path)
