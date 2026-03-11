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


func test_case_c02_trait_button_badge_reflects_unspent_points() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))
	await get_tree().process_frame

	var modal: CardDetailModal = CARD_DETAIL_MODAL_SCENE.instantiate() as CardDetailModal
	assert_true(modal != null, "Expected card detail modal scene")
	add_child(modal)
	modal.open_for_card(card_instance_id, "fire_wisp")
	await get_tree().process_frame

	assert_true(modal.traits_button.visible, "Traits button should be visible when progression data exists")
	assert_true(modal.traits_button.text.contains("!"), "Traits button should show spend-available badge when exactly one point is unspent")
	assert_true(modal.trait_points_label.visible, "Trait points label should be visible")
	assert_true(modal.trait_points_label.text.contains("1"), "Trait points label should show unspent point count")
