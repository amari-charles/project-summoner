extends GutTest

const CARD_TRAIT_TREE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/card_trait_tree_screen.tscn")

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


func test_case_c03_collection_opens_card_instance_trait_tree() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")
	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1
	}))

	NavigationContext.set_value("trait_tree_card_instance_id", card_instance_id)

	var screen: CardTraitTreeScreen = CARD_TRAIT_TREE_SCREEN_SCENE.instantiate() as CardTraitTreeScreen
	assert_true(screen != null, "Expected card trait tree screen scene")
	add_child(screen)
	await get_tree().process_frame
	await get_tree().process_frame

	assert_eq(screen._card_instance_id, card_instance_id, "Card trait tree should resolve navigation context card instance id")
	assert_false(screen.card_subtitle_label.text == Loc.t("ui.trait_tree.no_card_selected"), "Card subtitle should resolve selected card metadata")
	assert_true(screen._progression_nodes.size() > 0, "Card trait tree should render progression node data")
