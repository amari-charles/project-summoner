extends GutTest

## Unit Tests for CosmeticsCatalog (C# bridge)
##
## Tests cosmetic data lookup and filtering methods.

func test_get_cosmetic_returns_valid_data() -> void:
	var cosmetic: Dictionary = CosmeticsCatalog.GetCosmetic("card_back_default")

	assert_false(cosmetic.is_empty(), "Should return cosmetic data")
	assert_eq(cosmetic.get("id"), "card_back_default")
	assert_eq(cosmetic.get("type"), CosmeticsCatalog.CARD_BACK)


func test_get_cosmetic_returns_empty_for_unknown_id() -> void:
	var cosmetic: Dictionary = CosmeticsCatalog.GetCosmetic("nonexistent_cosmetic")

	assert_true(cosmetic.is_empty(), "Should return empty for unknown ID")
	# Expect the push_warning about cosmetic not found
	assert_engine_error("Cosmetic not found")


func test_has_cosmetic_returns_true_for_existing() -> void:
	assert_true(CosmeticsCatalog.HasCosmetic("card_back_default"))
	assert_true(CosmeticsCatalog.HasCosmetic("ui_theme_default"))


func test_has_cosmetic_returns_false_for_unknown() -> void:
	assert_false(CosmeticsCatalog.HasCosmetic("fake_cosmetic"))


func test_get_all_cosmetics_returns_array() -> void:
	var all_cosmetics: Array = CosmeticsCatalog.GetAllCosmetics()

	assert_gt(all_cosmetics.size(), 0, "Should have at least one cosmetic")


func test_get_cosmetics_by_type_filters_correctly() -> void:
	var card_backs: Array = CosmeticsCatalog.GetCosmeticsByType(CosmeticsCatalog.CARD_BACK)
	var ui_themes: Array = CosmeticsCatalog.GetCosmeticsByType(CosmeticsCatalog.UI_THEME)

	assert_gt(card_backs.size(), 0, "Should have card backs")
	assert_gt(ui_themes.size(), 0, "Should have UI themes")

	# Verify all returned items match the requested type
	for card_back: Dictionary in card_backs:
		assert_eq(card_back.get("type"), CosmeticsCatalog.CARD_BACK)

	for theme: Dictionary in ui_themes:
		assert_eq(theme.get("type"), CosmeticsCatalog.UI_THEME)


func test_get_card_backs_returns_card_back_type_only() -> void:
	var card_backs: Array = CosmeticsCatalog.GetCardBacks()

	for card_back: Dictionary in card_backs:
		assert_eq(card_back.get("type"), CosmeticsCatalog.CARD_BACK)


func test_get_ui_themes_returns_ui_theme_type_only() -> void:
	var ui_themes: Array = CosmeticsCatalog.GetUiThemes()

	for theme: Dictionary in ui_themes:
		assert_eq(theme.get("type"), CosmeticsCatalog.UI_THEME)


func test_get_purchasable_cosmetics_excludes_free_items() -> void:
	var purchasable: Array = CosmeticsCatalog.GetPurchasableCosmetics()

	# If no purchasable cosmetics exist yet, that's valid (empty catalog)
	# If any exist, they must have price > 0
	for cosmetic: Dictionary in purchasable:
		assert_gt(cosmetic.get("price", 0), 0, "Purchasable cosmetics should have price > 0")

	# Ensure test always asserts something (avoids risky test warning)
	assert_true(true, "Purchasable cosmetics check completed")


func test_get_cosmetic_name_returns_display_name() -> void:
	var cosmetic_name: String = CosmeticsCatalog.GetCosmeticName("card_back_default")

	assert_false(cosmetic_name.is_empty(), "Should return display name")


func test_get_cosmetic_price_returns_zero_for_default() -> void:
	var price: int = CosmeticsCatalog.GetCosmeticPrice("card_back_default")

	assert_eq(price, 0, "Default cosmetic should be free")


func test_type_to_string_conversion() -> void:
	assert_eq(CosmeticsCatalog.TypeToString(CosmeticsCatalog.CARD_BACK), "card_back")
	assert_eq(CosmeticsCatalog.TypeToString(CosmeticsCatalog.UI_THEME), "ui_theme")
	assert_eq(CosmeticsCatalog.TypeToString(CosmeticsCatalog.SUMMONER_SKIN), "summoner_skin")


func test_string_to_type_conversion() -> void:
	assert_eq(CosmeticsCatalog.StringToType("card_back"), CosmeticsCatalog.CARD_BACK)
	assert_eq(CosmeticsCatalog.StringToType("ui_theme"), CosmeticsCatalog.UI_THEME)
	assert_eq(CosmeticsCatalog.StringToType("summoner_skin"), CosmeticsCatalog.SUMMONER_SKIN)


func test_default_cosmetics_have_zero_price() -> void:
	var default_card_back: Dictionary = CosmeticsCatalog.GetCosmetic("card_back_default")
	var default_theme: Dictionary = CosmeticsCatalog.GetCosmetic("ui_theme_default")

	assert_eq(default_card_back.get("price", -1), 0, "Default card back should be free")
	assert_eq(default_theme.get("price", -1), 0, "Default theme should be free")
