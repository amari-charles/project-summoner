extends GutTest

const SHOP_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/shop_screen.tscn")


func test_offerings_page_six_at_a_time_without_wrapping() -> void:
	var screen: ShopScreen = SHOP_SCREEN_SCENE.instantiate() as ShopScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	screen.current_offerings = _make_offerings(7)
	screen.current_page = 0
	screen._render_current_page()

	assert_eq(screen.offering_list.get_child_count(), 6)
	assert_true(screen.previous_page_button.disabled)
	assert_false(screen.next_page_button.disabled)
	assert_eq(screen.page_label.text, Loc.t("shop.campus.page", {"current": 1, "total": 2}))

	screen._on_next_page_pressed()

	assert_eq(screen.current_page, 1)
	assert_eq(screen.offering_list.get_child_count(), 1)
	assert_false(screen.previous_page_button.disabled)
	assert_true(screen.next_page_button.disabled)
	var final_card: OfferingCard = screen.offering_list.get_child(0) as OfferingCard
	assert_eq(final_card.offering.get("offering_id", ""), "test_offering_6")

	screen._on_next_page_pressed()
	assert_eq(screen.current_page, 1, "The last page must not wrap to the first page")
	screen.queue_free()
	await get_tree().process_frame


func test_selecting_offering_opens_and_closes_detail_modal() -> void:
	var screen: ShopScreen = SHOP_SCREEN_SCENE.instantiate() as ShopScreen
	add_child_autofree(screen)
	await get_tree().process_frame
	assert_false(screen.current_offerings.is_empty(), "General shop needs an offering for the modal test")
	if screen.current_offerings.is_empty():
		return

	var offering: Dictionary = screen.current_offerings[0]
	screen._on_offering_card_clicked(offering)

	assert_true(screen.detail_modal.visible)
	assert_eq(screen.selected_offering.get("offering_id", ""), offering.get("offering_id", ""))
	assert_eq(screen.offering_name_label.text, offering.get("display_name", ""))
	assert_eq(screen.description_label.text, offering.get("description", ""))

	screen._close_detail_modal()

	assert_false(screen.detail_modal.visible)
	assert_true(screen.selected_offering.is_empty())
	assert_true(screen.purchase_button.disabled)
	screen.queue_free()
	await get_tree().process_frame


func _make_offerings(count: int) -> Array:
	var offerings: Array = []
	for index: int in count:
		offerings.append({
			"offering_id": "test_offering_%d" % index,
			"display_name": "TEST OFFERING %d" % index,
			"offering_type_name": "card",
			"base_price": 10,
			"description": "Test offering for pagination coverage."
		})
	return offerings
