extends GutTest


func test_string_accepts_stringname_values() -> void:
	var value: Variant = StringName("summer_plains")
	var parsed: String = SafeTypeUtils.string(value, "")
	assert_eq(parsed, "summer_plains")


func test_string_returns_default_for_non_string_values() -> void:
	var value: Variant = 42
	var parsed: String = SafeTypeUtils.string(value, "fallback")
	assert_eq(parsed, "fallback")
