extends GutTest

const CARD_DETAIL_MODAL_SCENE: PackedScene = preload("res://scenes/meta/modals/card_detail_modal.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c02")
	ProfileRepo.ResetProfile()
	await get_tree().process_frame


func after_all() -> void:
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c02_card_points_and_core_path_are_visible() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))
	await get_tree().process_frame

	var modal: CardDetailModal = CARD_DETAIL_MODAL_SCENE.instantiate() as CardDetailModal
	assert_true(modal != null, "Expected card detail modal scene")
	add_child_autofree(modal)
	modal.open_for_card(card_instance_id, "fire_wisp")
	await get_tree().process_frame

	assert_true(modal.trait_points_label.visible, "Trait points label should be visible")
	assert_true(modal.trait_points_label.text.contains("1 Card Points"), "Card Points should be named in the Traits header")
	assert_true(modal.traits_section.visible, "Card development list should be visible")
	assert_true(modal.traits_container.get_child_count() >= 1, "Core should always be the first card development entry")
	var core_circle: Button = modal.traits_container.get_child(0) as Button
	assert_not_null(core_circle, "Core development entry should be interactive")
	assert_true(core_circle.tooltip_text.contains("Core"), "Core should identify the card's native path")
	assert_true(modal.description_label.visible, "Card rules text should be visible in the detail view")


func test_core_circle_opens_card_path_in_shared_overlay() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))
	await get_tree().process_frame

	var modal: CardDetailModal = CARD_DETAIL_MODAL_SCENE.instantiate() as CardDetailModal
	add_child_autofree(modal)
	modal.open_for_card(card_instance_id, "fire_wisp")
	await get_tree().process_frame

	var core_circle: Button = modal.traits_container.get_child(0) as Button
	core_circle.pressed.emit()
	await get_tree().process_frame

	assert_true(modal.trait_development_overlay.visible, "Core should open the shared development overlay")
	assert_eq(modal.trait_development_overlay._owner_type, "card")
	assert_eq(modal.trait_development_overlay._owner_id, card_instance_id)
	assert_eq(modal.trait_development_overlay._anchor_trait_id, TraitDevelopmentOverlay.CARD_CORE_PATH_ID)
	assert_true(modal.trait_development_overlay._visible_nodes.size() > 0, "Core should render current native development nodes")
	assert_true(_visible_core_has(modal, "__card_core_root__:fire_wisp"), "Core should begin at one inherent Fire Wisp root")
	assert_true(_visible_core_has(modal, "trait_fire_wisp_twin_flame"), "Core should contain Fire Wisp's authored behavior branch")
	assert_true(_visible_core_has(modal, "trait_fire_wisp_condensed_flame"), "Core should expose both permanent branch choices")
	assert_false(_visible_core_has(modal, "trait_power"), "Global stat offers must not leak into Card Core")


func _visible_core_has(modal: CardDetailModal, trait_id: String) -> bool:
	for node_value: Variant in modal.trait_development_overlay._visible_nodes:
		if node_value is Dictionary and SafeTypeUtils.string(node_value.get("id", ""), "") == trait_id:
			return true
	return false
