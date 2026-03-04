extends GutTest

## Unit Tests for Shop Offering Ownership Validation
##
## Tests the IsOfferingOwned() logic in ShopService.
## Uses the actual autoloads (ProfileRepo, Shop) for integration-style testing.

var _original_profile_id: String = ""


func before_all() -> void:
	# Store original profile ID to restore after tests
	_original_profile_id = ProfileRepo.GetActiveProfileDict().get("profile_id", "")


func before_each() -> void:
	# Load a test profile to avoid modifying real data
	ProfileRepo.LoadProfile("test_shop_ownership")
	ProfileRepo.ResetProfile()
	# Wait for Shop to be ready after profile change
	await get_tree().process_frame


func after_all() -> void:
	# Restore original profile
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


## =============================================================================
## SUMMONER OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_summoner() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var summoner_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "summoner":
			summoner_offering = offering
			break

	if summoner_offering.is_empty():
		pending("No summoner offerings found in premium_store")
		return

	# By default, no premium summoners are unlocked
	assert_false(Shop.IsOfferingOwned(summoner_offering.get("offering_id", ""), "premium_store"), "Unowned summoner should return false")


func test_is_offering_owned_returns_true_for_owned_summoner() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var summoner_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "summoner":
			summoner_offering = offering
			break

	if summoner_offering.is_empty():
		pending("No summoner offerings found in premium_store")
		return

	# Unlock the summoner
	ProfileRepo.UnlockSummoner(summoner_offering.get("summoner_id", ""))
	await get_tree().process_frame

	assert_true(Shop.IsOfferingOwned(summoner_offering.get("offering_id", ""), "premium_store"), "Owned summoner should return true")


## =============================================================================
## COSMETIC OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_cosmetic() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var cosmetic_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "cosmetic":
			cosmetic_offering = offering
			break

	if cosmetic_offering.is_empty():
		pending("No cosmetic offerings found in premium_store")
		return

	assert_false(Shop.IsOfferingOwned(cosmetic_offering.get("offering_id", ""), "premium_store"), "Unowned cosmetic should return false")


func test_is_offering_owned_returns_true_for_owned_cosmetic() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var cosmetic_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "cosmetic":
			cosmetic_offering = offering
			break

	if cosmetic_offering.is_empty():
		pending("No cosmetic offerings found in premium_store")
		return

	# Grant the cosmetic
	ProfileRepo.GrantCosmetic(cosmetic_offering.get("cosmetic_id", ""))
	await get_tree().process_frame

	assert_true(Shop.IsOfferingOwned(cosmetic_offering.get("offering_id", ""), "premium_store"), "Owned cosmetic should return true")


## =============================================================================
## EMOTE OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_emote() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var emote_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "emote":
			emote_offering = offering
			break

	if emote_offering.is_empty():
		pending("No emote offerings found in premium_store")
		return

	assert_false(Shop.IsOfferingOwned(emote_offering.get("offering_id", ""), "premium_store"), "Unowned emote should return false")


func test_is_offering_owned_returns_true_for_owned_emote() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var emote_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "emote":
			emote_offering = offering
			break

	if emote_offering.is_empty():
		pending("No emote offerings found in premium_store")
		return

	# Grant the emote
	ProfileRepo.GrantEmote(emote_offering.get("emote_id", ""))
	await get_tree().process_frame

	assert_true(Shop.IsOfferingOwned(emote_offering.get("offering_id", ""), "premium_store"), "Owned emote should return true")


## =============================================================================
## CARD OFFERINGS (NOT ONE-TIME PURCHASES)
## =============================================================================

func test_is_offering_owned_returns_false_for_card_offerings() -> void:
	var offerings: Array = Shop.GetShopOfferings("general")
	var card_offering: Dictionary = {}

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "card":
			card_offering = offering
			break

	if card_offering.is_empty():
		pending("No card offerings found in general shop")
		return

	# Card offerings are not one-time purchases, should always return false
	assert_false(Shop.IsOfferingOwned(card_offering.get("offering_id", ""), "general"), "Card offerings are never 'owned'")


## =============================================================================
## PREMIUM STORE STRUCTURE TESTS
## =============================================================================

func test_premium_store_has_summoner_offerings() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var summoner_count: int = 0

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "summoner":
			summoner_count += 1

	assert_gt(summoner_count, 0, "Premium store should have summoner offerings")


func test_premium_store_has_cosmetic_offerings() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var cosmetic_count: int = 0

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "cosmetic":
			cosmetic_count += 1

	assert_gt(cosmetic_count, 0, "Premium store should have cosmetic offerings")


func test_premium_store_has_emote_offerings() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")
	var emote_count: int = 0

	for offering: Dictionary in offerings:
		if offering.get("offering_type_name") == "emote":
			emote_count += 1

	assert_gt(emote_count, 0, "Premium store should have emote offerings")


func test_all_premium_offerings_have_account_purchase_limit() -> void:
	var offerings: Array = Shop.GetShopOfferings("premium_store")

	for offering: Dictionary in offerings:
		assert_eq(offering.get("purchase_limit_type", ""), "account", "Premium offerings should have account purchase limits")
		assert_eq(offering.get("purchase_limit", 0), 1, "Premium offerings should have purchase limit of 1")
