extends GutTest

## Unit Tests for EmotesCatalog
##
## Tests emote data lookup and filtering methods.

func test_get_emote_returns_valid_data() -> void:
	var emote: Dictionary = EmotesCatalog.get_emote("emote_laugh")

	assert_false(emote.is_empty(), "Should return emote data")
	assert_eq(emote.get("id"), &"emote_laugh")
	assert_gt(emote.get("price", 0), 0, "Purchasable emote should have a price")


func test_get_emote_returns_empty_for_unknown_id() -> void:
	var emote: Dictionary = EmotesCatalog.get_emote("nonexistent_emote")

	assert_true(emote.is_empty(), "Should return empty for unknown ID")
	# Expect the push_warning about emote not found
	assert_engine_error("Emote not found")


func test_has_emote_returns_true_for_existing() -> void:
	assert_true(EmotesCatalog.has_emote("emote_hello"))
	assert_true(EmotesCatalog.has_emote("emote_laugh"))


func test_has_emote_returns_false_for_unknown() -> void:
	assert_false(EmotesCatalog.has_emote("fake_emote"))


func test_get_all_emotes_returns_array() -> void:
	var all_emotes: Array[Dictionary] = EmotesCatalog.get_all_emotes()

	assert_gt(all_emotes.size(), 0, "Should have at least one emote")


func test_get_emotes_by_category_filters_correctly() -> void:
	var reactions: Array[Dictionary] = EmotesCatalog.get_emotes_by_category("reaction")
	var taunts: Array[Dictionary] = EmotesCatalog.get_emotes_by_category("taunt")

	# Verify all returned items match the requested category
	for emote: Dictionary in reactions:
		assert_eq(emote.get("category"), "reaction")

	for emote: Dictionary in taunts:
		assert_eq(emote.get("category"), "taunt")


func test_get_starter_emotes_returns_free_emotes() -> void:
	var starters: Array[Dictionary] = EmotesCatalog.get_starter_emotes()

	assert_gt(starters.size(), 0, "Should have starter emotes")

	for emote: Dictionary in starters:
		assert_eq(emote.get("price", -1), 0, "Starter emotes should be free")


func test_get_purchasable_emotes_excludes_free_items() -> void:
	var purchasable: Array[Dictionary] = EmotesCatalog.get_purchasable_emotes()

	for emote: Dictionary in purchasable:
		assert_gt(emote.get("price", 0), 0, "Purchasable emotes should have price > 0")


func test_get_emote_name_returns_display_name() -> void:
	var name: String = EmotesCatalog.get_emote_name("emote_hello")

	assert_false(name.is_empty(), "Should return display name")


func test_get_emote_price_returns_price() -> void:
	var price: int = EmotesCatalog.get_emote_price("emote_laugh")

	assert_gt(price, 0, "Purchasable emote should have a price")


func test_is_starter_emote_returns_true_for_starters() -> void:
	assert_true(EmotesCatalog.is_starter_emote("emote_hello"))
	assert_true(EmotesCatalog.is_starter_emote("emote_good_game"))


func test_is_starter_emote_returns_false_for_purchasable() -> void:
	assert_false(EmotesCatalog.is_starter_emote("emote_laugh"))
	assert_false(EmotesCatalog.is_starter_emote("emote_taunt"))


func test_max_equipped_emotes_constant() -> void:
	assert_eq(EmotesCatalog.MAX_EQUIPPED_EMOTES, 4)


func test_starter_emotes_array_matches_is_starter_emote() -> void:
	for emote_id: StringName in EmotesCatalog.STARTER_EMOTES:
		assert_true(EmotesCatalog.is_starter_emote(emote_id), "STARTER_EMOTES entry should be recognized as starter")
