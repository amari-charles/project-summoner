extends GutTest

## Unit Tests for CosmeticsCatalog
##
## Tests cosmetic data lookup and filtering methods.

func test_get_cosmetic_returns_valid_data() -> void:
	var cosmetic: Dictionary = CosmeticsCatalog.get_cosmetic("card_back_gold")

	assert_false(cosmetic.is_empty(), "Should return cosmetic data")
	assert_eq(cosmetic.get("id"), &"card_back_gold")
	assert_eq(cosmetic.get("type"), CosmeticsCatalog.CosmeticType.CARD_BACK)
	assert_gt(cosmetic.get("price", 0), 0, "Premium cosmetics should have a price")


func test_get_cosmetic_returns_empty_for_unknown_id() -> void:
	var cosmetic: Dictionary = CosmeticsCatalog.get_cosmetic("nonexistent_cosmetic")

	assert_true(cosmetic.is_empty(), "Should return empty for unknown ID")
	# Expect the push_warning about cosmetic not found
	assert_engine_error("Cosmetic not found")


func test_has_cosmetic_returns_true_for_existing() -> void:
	assert_true(CosmeticsCatalog.has_cosmetic("card_back_gold"))
	assert_true(CosmeticsCatalog.has_cosmetic("ui_theme_crimson"))


func test_has_cosmetic_returns_false_for_unknown() -> void:
	assert_false(CosmeticsCatalog.has_cosmetic("fake_cosmetic"))


func test_get_all_cosmetics_returns_array() -> void:
	var all_cosmetics: Array[Dictionary] = CosmeticsCatalog.get_all_cosmetics()

	assert_gt(all_cosmetics.size(), 0, "Should have at least one cosmetic")


func test_get_cosmetics_by_type_filters_correctly() -> void:
	var card_backs: Array[Dictionary] = CosmeticsCatalog.get_cosmetics_by_type(CosmeticsCatalog.CosmeticType.CARD_BACK)
	var ui_themes: Array[Dictionary] = CosmeticsCatalog.get_cosmetics_by_type(CosmeticsCatalog.CosmeticType.UI_THEME)

	assert_gt(card_backs.size(), 0, "Should have card backs")
	assert_gt(ui_themes.size(), 0, "Should have UI themes")

	# Verify all returned items match the requested type
	for card_back: Dictionary in card_backs:
		assert_eq(card_back.get("type"), CosmeticsCatalog.CosmeticType.CARD_BACK)

	for theme: Dictionary in ui_themes:
		assert_eq(theme.get("type"), CosmeticsCatalog.CosmeticType.UI_THEME)


func test_get_card_backs_returns_card_back_type_only() -> void:
	var card_backs: Array[Dictionary] = CosmeticsCatalog.get_card_backs()

	for card_back: Dictionary in card_backs:
		assert_eq(card_back.get("type"), CosmeticsCatalog.CosmeticType.CARD_BACK)


func test_get_ui_themes_returns_ui_theme_type_only() -> void:
	var ui_themes: Array[Dictionary] = CosmeticsCatalog.get_ui_themes()

	for theme: Dictionary in ui_themes:
		assert_eq(theme.get("type"), CosmeticsCatalog.CosmeticType.UI_THEME)


func test_get_purchasable_cosmetics_excludes_free_items() -> void:
	var purchasable: Array[Dictionary] = CosmeticsCatalog.get_purchasable_cosmetics()

	for cosmetic: Dictionary in purchasable:
		assert_gt(cosmetic.get("price", 0), 0, "Purchasable cosmetics should have price > 0")


func test_get_cosmetic_name_returns_display_name() -> void:
	var name: String = CosmeticsCatalog.get_cosmetic_name("card_back_gold")

	assert_false(name.is_empty(), "Should return display name")


func test_get_cosmetic_price_returns_price() -> void:
	var price: int = CosmeticsCatalog.get_cosmetic_price("card_back_gold")

	assert_gt(price, 0, "Premium cosmetic should have a price")


func test_type_to_string_conversion() -> void:
	assert_eq(CosmeticsCatalog.type_to_string(CosmeticsCatalog.CosmeticType.CARD_BACK), "card_back")
	assert_eq(CosmeticsCatalog.type_to_string(CosmeticsCatalog.CosmeticType.UI_THEME), "ui_theme")
	assert_eq(CosmeticsCatalog.type_to_string(CosmeticsCatalog.CosmeticType.SUMMONER_SKIN), "summoner_skin")


func test_string_to_type_conversion() -> void:
	assert_eq(CosmeticsCatalog.string_to_type("card_back"), CosmeticsCatalog.CosmeticType.CARD_BACK)
	assert_eq(CosmeticsCatalog.string_to_type("ui_theme"), CosmeticsCatalog.CosmeticType.UI_THEME)
	assert_eq(CosmeticsCatalog.string_to_type("summoner_skin"), CosmeticsCatalog.CosmeticType.SUMMONER_SKIN)


func test_default_cosmetics_have_zero_price() -> void:
	var default_card_back: Dictionary = CosmeticsCatalog.get_cosmetic("card_back_default")
	var default_theme: Dictionary = CosmeticsCatalog.get_cosmetic("ui_theme_default")

	assert_eq(default_card_back.get("price", -1), 0, "Default card back should be free")
	assert_eq(default_theme.get("price", -1), 0, "Default theme should be free")
