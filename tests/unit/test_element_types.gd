extends GutTest


func test_element_types_accept_stringname_inputs() -> void:
	assert_true(ElementTypes.is_core(StringName("fire")))
	assert_eq(ElementTypes.get_display_name(StringName("water")), "Water")
