extends GutTest

## Unit Tests for Shop Offering Ownership Validation
##
## Tests the is_offering_owned() and _check_already_owned() logic in ShopService.
## Uses the actual autoloads (ProfileRepo, Shop) for integration-style testing.

var _original_profile_id: String = ""


func before_all() -> void:
	# Store original profile ID to restore after tests
	_original_profile_id = ProfileRepo.get_current_profile_id()


func before_each() -> void:
	# Load a test profile to avoid modifying real data
	ProfileRepo.load_profile("test_shop_ownership")
	ProfileRepo.reset_profile()
	# Wait for Shop to be ready after profile change
	await get_tree().process_frame


func after_all() -> void:
	# Restore original profile
	if not _original_profile_id.is_empty():
		ProfileRepo.load_profile(_original_profile_id)


## =============================================================================
## SUMMONER OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_summoner() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var summoner_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.SUMMONER:
			summoner_offering = offering
			break

	if summoner_offering == null:
		pending("No summoner offerings found in premium_store")
		return

	# By default, no premium summoners are unlocked
	assert_false(Shop.is_offering_owned(summoner_offering), "Unowned summoner should return false")


func test_is_offering_owned_returns_true_for_owned_summoner() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var summoner_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.SUMMONER:
			summoner_offering = offering
			break

	if summoner_offering == null:
		pending("No summoner offerings found in premium_store")
		return

	# Unlock the summoner
	ProfileRepo.unlock_summoner(summoner_offering.summoner_id)
	await get_tree().process_frame

	assert_true(Shop.is_offering_owned(summoner_offering), "Owned summoner should return true")


## =============================================================================
## COSMETIC OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_cosmetic() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var cosmetic_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.COSMETIC:
			cosmetic_offering = offering
			break

	if cosmetic_offering == null:
		pending("No cosmetic offerings found in premium_store")
		return

	assert_false(Shop.is_offering_owned(cosmetic_offering), "Unowned cosmetic should return false")


func test_is_offering_owned_returns_true_for_owned_cosmetic() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var cosmetic_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.COSMETIC:
			cosmetic_offering = offering
			break

	if cosmetic_offering == null:
		pending("No cosmetic offerings found in premium_store")
		return

	# Grant the cosmetic
	ProfileRepo.grant_cosmetic(cosmetic_offering.cosmetic_id)
	await get_tree().process_frame

	assert_true(Shop.is_offering_owned(cosmetic_offering), "Owned cosmetic should return true")


## =============================================================================
## EMOTE OWNERSHIP TESTS
## =============================================================================

func test_is_offering_owned_returns_false_for_unowned_emote() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var emote_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.EMOTE:
			emote_offering = offering
			break

	if emote_offering == null:
		pending("No emote offerings found in premium_store")
		return

	assert_false(Shop.is_offering_owned(emote_offering), "Unowned emote should return false")


func test_is_offering_owned_returns_true_for_owned_emote() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var emote_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.EMOTE:
			emote_offering = offering
			break

	if emote_offering == null:
		pending("No emote offerings found in premium_store")
		return

	# Grant the emote
	ProfileRepo.grant_emote(emote_offering.emote_id)
	await get_tree().process_frame

	assert_true(Shop.is_offering_owned(emote_offering), "Owned emote should return true")


## =============================================================================
## CARD OFFERINGS (NOT ONE-TIME PURCHASES)
## =============================================================================

func test_is_offering_owned_returns_false_for_card_offerings() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("general")
	var card_offering: ShopOffering = null

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.CARD:
			card_offering = offering
			break

	if card_offering == null:
		pending("No card offerings found in general shop")
		return

	# Card offerings are not one-time purchases, should always return false
	assert_false(Shop.is_offering_owned(card_offering), "Card offerings are never 'owned'")


## =============================================================================
## PREMIUM STORE STRUCTURE TESTS
## =============================================================================

func test_premium_store_has_summoner_offerings() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var summoner_count: int = 0

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.SUMMONER:
			summoner_count += 1

	assert_gt(summoner_count, 0, "Premium store should have summoner offerings")


func test_premium_store_has_cosmetic_offerings() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var cosmetic_count: int = 0

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.COSMETIC:
			cosmetic_count += 1

	assert_gt(cosmetic_count, 0, "Premium store should have cosmetic offerings")


func test_premium_store_has_emote_offerings() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")
	var emote_count: int = 0

	for offering: ShopOffering in offerings:
		if offering.offering_type == ShopOffering.OfferingType.EMOTE:
			emote_count += 1

	assert_gt(emote_count, 0, "Premium store should have emote offerings")


func test_all_premium_offerings_have_account_purchase_limit() -> void:
	var offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")

	for offering: ShopOffering in offerings:
		assert_eq(offering.purchase_limit_type, "account", "Premium offerings should have account purchase limits")
		assert_eq(offering.purchase_limit, 1, "Premium offerings should have purchase limit of 1")
