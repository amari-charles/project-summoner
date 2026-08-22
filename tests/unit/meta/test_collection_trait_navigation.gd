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
