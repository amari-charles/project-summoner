extends GutTest

const CARD_DETAIL_MODAL_SCENE: PackedScene = preload("res://scenes/meta/modals/card_detail_modal.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c03")
	ProfileRepo.ResetProfile()
	NavigationContext.clear()
	await get_tree().process_frame


func after_all() -> void:
	NavigationContext.clear()
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c03_card_development_stays_in_the_card_detail_surface() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))

	var modal: CardDetailModal = CARD_DETAIL_MODAL_SCENE.instantiate() as CardDetailModal
	assert_true(modal != null, "Expected card detail modal scene")
	add_child_autofree(modal)
	modal.open_for_card(card_instance_id, "fire_wisp")
	await get_tree().process_frame

	var core_circle: Button = modal.traits_container.get_child(0) as Button
	core_circle.pressed.emit()
	await get_tree().process_frame

	assert_true(modal.visible, "Opening development should not navigate away from card details")
	assert_true(modal.trait_development_overlay.visible, "Selected card path should open as an overlay")
	assert_false(NavigationContext.has_return(), "Card development should not push a separate screen route")


func test_trait_unlock_confirmation_stays_in_the_node_popover() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))

	var modal: CardDetailModal = CARD_DETAIL_MODAL_SCENE.instantiate() as CardDetailModal
	add_child_autofree(modal)
	modal.open_for_card(card_instance_id, "fire_wisp")
	await get_tree().process_frame
	var core_circle: Button = modal.traits_container.get_child(0) as Button
	core_circle.pressed.emit()
	await get_tree().process_frame

	var overlay: TraitDevelopmentOverlay = modal.trait_development_overlay
	var available_trait_id: String = ""
	for node_value: Variant in overlay._visible_nodes:
		var node_data: Dictionary = SafeTypeUtils.dict(node_value)
		var trait_id: String = SafeTypeUtils.string(node_data.get("id"))
		var detail: Dictionary = TraitTreeApi.get_trait_node_detail("card", card_instance_id, trait_id)
		if SafeTypeUtils.bool_val(detail.get("unlock_button_enabled")):
			available_trait_id = trait_id
			break
	assert_false(available_trait_id.is_empty(), "Expected an available card trait")

	overlay._show_node_detail(available_trait_id)
	overlay._on_action_pressed()
	assert_true(overlay._is_confirming_unlock)
	assert_true(overlay.cancel_button.visible)
	assert_eq(overlay.action_button.text, Loc.t("ui.common.confirm"))
	assert_null(overlay.find_child("UnlockConfirmation", true, false))

	overlay._on_cancel_unlock_pressed()
	assert_false(overlay._is_confirming_unlock)
	assert_false(overlay.cancel_button.visible)
	assert_eq(overlay.action_button.text, Loc.t("ui.trait_tree.unlock_button"))


func test_collection_screen_does_not_show_gold() -> void:
	var scene: PackedScene = load("res://scenes/meta/screens/collection_screen.tscn")
	var screen: Control = scene.instantiate() as Control
	add_child_autofree(screen)
	await get_tree().process_frame

	assert_null(screen.find_child("GoldContainer", true, false))
	assert_null(screen.find_child("GoldLabel", true, false))
