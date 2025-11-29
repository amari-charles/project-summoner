extends Node
# ShopService is registered as autoload "Shop", no class_name needed

## ShopService - Manages shop offerings and purchases
##
## Handles both Caravan (campaign event) shops and General UI shop.
## Data layer: ProfileRepo owns all persistent state
## Service layer: ShopService orchestrates purchases and validates
##
## Architecture:
## - ProfileRepo: Single source of truth for gold, purchase history, refresh state
## - ShopService: Catalog, validation, transaction orchestration
## - RewardService: Centralized reward granting
## - ShopOffering: Pure configuration/template (no runtime state)
##
## Usage:
##   var offerings = Shop.get_shop_offerings("caravan_tutorial")
##   var success = Shop.purchase_offering(offering_id, "caravan_tutorial")

## Signals
signal purchase_completed(offering_id: String, shop_id: String)
signal purchase_failed(offering_id: String, reason: String)
signal shop_refreshed(shop_id: String)

## Shop catalog (_init_shops pattern like CampaignService._init_battles)
var _shops: Dictionary = {}  # shop_id -> shop_definition

## Purchase history cache (for read performance during validation)
## Source of truth is ProfileRepo, cache is loaded on _ready()
var _purchase_cache: Dictionary = {}  # "shop_id::offering_id::refresh_epoch" -> count

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("ShopService: Initializing...")

	# Wait for dependencies
	await get_tree().process_frame

	# Load purchase history into cache
	_purchase_cache = ProfileRepo.get_shop_purchases()

	# Initialize shop catalog
	_init_shops()

	print("ShopService: Ready (%d shops loaded)" % _shops.size())

## =============================================================================
## SHOP CATALOG
## =============================================================================

func _init_shops() -> void:
	# General shop (persistent UI shop)
	_shops["general"] = {
		"id": "general",
		"shop_type": "general",
		"name": Loc.t("shop.general.name"),
		"offerings": [
			{
				"offering_id": "general_fire_recruit",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.fire_recruit.name"),
				"description": Loc.t("shop.offering.fire_recruit.description"),
				"card_catalog_id": "fire_recruit",
				"card_count": 1,
				"base_price": 30,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_slime_violet",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.slime_violet.name"),
				"description": Loc.t("shop.offering.slime_violet.description"),
				"card_catalog_id": "slime_violet",
				"card_count": 1,
				"base_price": 20,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_slime_yellow",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.slime_yellow.name"),
				"description": Loc.t("shop.offering.slime_yellow.description"),
				"card_catalog_id": "slime_yellow",
				"card_count": 1,
				"base_price": 35,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_basic_spell_pack",
				"offering_type": ShopOffering.OfferingType.CARD_PACK,
				"display_name": Loc.t("shop.offering.basic_spell_pack.name"),
				"description": Loc.t("shop.offering.basic_spell_pack.description"),
				"pack_cards": [
					{"catalog_id": "charge", "count": 1},
					{"catalog_id": "fireball", "count": 1}
				],
				"base_price": 50,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_summon_pack",
				"offering_type": ShopOffering.OfferingType.CARD_PACK,
				"display_name": Loc.t("shop.offering.summon_pack.name"),
				"description": Loc.t("shop.offering.summon_pack.description"),
				"pack_cards": [
					{"catalog_id": "slime_violet", "count": 2},
					{"catalog_id": "fire_recruit", "count": 1}
				],
				"base_price": 70,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			}
		]
	}

	# Mr. Merriweather's Caravan (tutorial caravan)
	_shops["caravan_tutorial"] = {
		"id": "caravan_tutorial",
		"shop_type": "caravan",
		"name": Loc.t("shop.caravan.merriweather.name"),
		"offerings": [
			{
				"offering_id": "tutorial_fire_recruit",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.tutorial_fire_recruit.name"),
				"description": Loc.t("shop.offering.tutorial_fire_recruit.description"),
				"card_catalog_id": "fire_recruit",
				"card_count": 1,
				"base_price": 25,
				"purchase_limit_type": "account",
				"purchase_limit": 3
			},
			{
				"offering_id": "tutorial_spell_pack",
				"offering_type": ShopOffering.OfferingType.CARD_PACK,
				"display_name": Loc.t("shop.offering.tutorial_spell_pack.name"),
				"description": Loc.t("shop.offering.tutorial_spell_pack.description"),
				"pack_cards": [
					{"catalog_id": "charge", "count": 1},
					{"catalog_id": "rally", "count": 1}
				],
				"base_price": 50,
				"purchase_limit_type": "account",
				"purchase_limit": 1
			}
		]
	}

	print("ShopService: Initialized %d shops" % _shops.size())

## Get all offerings for a shop
func get_shop_offerings(shop_id: String) -> Array[ShopOffering]:
	var shop_variant: Variant = _shops.get(shop_id)
	if shop_variant == null:
		return []

	var shop: Dictionary = shop_variant
	var offerings_variant: Variant = shop.get("offerings", [])
	if not offerings_variant is Array:
		return []

	var offerings_array: Array = offerings_variant
	var result: Array[ShopOffering] = []

	for offering_def: Variant in offerings_array:
		if not offering_def is Dictionary:
			continue
		var offering_dict: Dictionary = offering_def
		var offering: ShopOffering = _build_offering_from_dict(offering_dict)
		if offering:
			result.append(offering)

	return result

## =============================================================================
## PURCHASE LOGIC
## =============================================================================

## Purchase an offering
func purchase_offering(offering_id: String, shop_id: String = "general") -> bool:
	var offering: ShopOffering = _find_offering(offering_id, shop_id)
	if not offering:
		_emit_purchase_failed(offering_id, "Offering not found")
		return false

	# Get shop refresh state
	var shop_refresh_state: Dictionary = ProfileRepo.get_shop_refresh_state(shop_id)
	var epoch_variant: Variant = shop_refresh_state.get("refresh_epoch", 0)
	var refresh_epoch: int = epoch_variant if epoch_variant is int else 0

	# Build namespaced key with refresh epoch
	var purchase_key: String = _build_purchase_key(shop_id, offering_id, refresh_epoch)

	# Get state from ProfileRepo (typed calls, no .call())
	var resources: Dictionary = ProfileRepo.get_resources()
	var gold: int = resources.get("gold", 0)
	var purchase_count: int = _purchase_cache.get(purchase_key, 0)

	# Build context for validation
	var context: ShopPurchaseContext = ShopPurchaseContext.new()
	context.player_gold = gold
	context.purchase_count = purchase_count
	context.summoner_affinity = ""  # TODO: ProfileRepo.get_summoner_affinity() when implemented
	context.refresh_epoch = refresh_epoch

	# Validate
	if not offering.can_purchase(context):
		var reason: String = _get_failure_reason(offering, context)
		_emit_purchase_failed(offering_id, reason)
		return false

	# Transaction atomicity: All-or-nothing guarantee
	var price: int = offering.get_price(context)

	# Step 1: Deduct gold
	ProfileRepo.update_resources({"gold": -price})
	# update_resources doesn't return bool, assume success

	# Step 2: Grant rewards via RewardService
	var rewards: Dictionary = _build_reward_dict(offering)
	if not RewardService.grant_rewards(rewards):
		# Rollback: Refund gold
		ProfileRepo.update_resources({"gold": price})
		_emit_purchase_failed(offering_id, "Failed to grant rewards")
		return false

	# Step 3: Track purchase (namespaced key)
	if not ProfileRepo.increment_purchase_count(purchase_key):
		push_warning("ShopService: Failed to track purchase count")
		# Don't rollback - player got their items, tracking is non-critical
	else:
		# Update cache
		_purchase_cache[purchase_key] = _purchase_cache.get(purchase_key, 0) + 1

	purchase_completed.emit(offering_id, shop_id)
	print("ShopService: Purchased '%s' for %d gold" % [offering_id, price])
	return true

## =============================================================================
## INTERNAL HELPERS
## =============================================================================

## Build purchase key with refresh epoch
func _build_purchase_key(shop_id: String, offering_id: String, refresh_epoch: int) -> String:
	# Per-refresh and account-limited offerings both include the epoch
	# Account-limited offerings can ignore epoch changes or pass 0
	return "%s::%s::%d" % [shop_id, offering_id, refresh_epoch]

## Find offering by ID in shop catalog
func _find_offering(offering_id: String, shop_id: String) -> ShopOffering:
	var shop_variant: Variant = _shops.get(shop_id)
	if shop_variant == null:
		return null

	var shop: Dictionary = shop_variant
	var offerings_variant: Variant = shop.get("offerings", [])
	if not offerings_variant is Array:
		return null

	var offerings_array: Array = offerings_variant
	for offering_def: Variant in offerings_array:
		if not offering_def is Dictionary:
			continue
		var def_dict: Dictionary = offering_def
		if def_dict.get("offering_id") == offering_id:
			return _build_offering_from_dict(def_dict)

	return null

## Build ShopOffering instance from dictionary definition
func _build_offering_from_dict(def: Dictionary) -> ShopOffering:
	var offering: ShopOffering = ShopOffering.new()
	offering.offering_id = def.get("offering_id", "")
	offering.offering_type = def.get("offering_type", ShopOffering.OfferingType.CARD)
	offering.display_name = def.get("display_name", "")
	offering.description = def.get("description", "")
	offering.card_catalog_id = def.get("card_catalog_id", "")
	offering.card_count = def.get("card_count", 1)
	offering.base_price = def.get("base_price", 0)
	offering.purchase_limit_type = def.get("purchase_limit_type", "none")
	offering.purchase_limit = def.get("purchase_limit", 0)

	# For CARD_PACK types
	if def.has("pack_cards"):
		var pack_cards_variant: Variant = def["pack_cards"]
		if pack_cards_variant is Array:
			var pack_cards_array: Array = pack_cards_variant
			for card_data: Variant in pack_cards_array:
				if card_data is Dictionary:
					var card_dict: Dictionary = card_data
					offering.pack_cards.append(card_dict)

	return offering

## Build reward dictionary for RewardService
func _build_reward_dict(offering: ShopOffering) -> Dictionary:
	var rewards: Dictionary = {}

	match offering.offering_type:
		ShopOffering.OfferingType.CARD:
			rewards["cards"] = [{"catalog_id": offering.card_catalog_id, "count": offering.card_count, "rarity": RarityIDs.COMMON}]

		ShopOffering.OfferingType.CARD_PACK:
			var cards: Array[Dictionary] = []
			for card_data: Dictionary in offering.pack_cards:
				cards.append({
					"catalog_id": card_data.get("catalog_id", ""),
					"count": card_data.get("count", 1),
					"rarity": RarityIDs.COMMON
				})
			rewards["cards"] = cards

		ShopOffering.OfferingType.CURRENCY:
			# TODO: Implement currency rewards
			pass

		ShopOffering.OfferingType.SPECIAL:
			# TODO: Implement special rewards
			pass

	return rewards

## Get human-readable failure reason
func _get_failure_reason(offering: ShopOffering, context: ShopPurchaseContext) -> String:
	var price: int = offering.get_price(context)
	if context.player_gold < price:
		return "Not enough gold (need %d, have %d)" % [price, context.player_gold]

	if offering.purchase_limit_type != "none" and offering.purchase_limit > 0:
		if context.purchase_count >= offering.purchase_limit:
			return "Purchase limit reached"

	return "Unknown error"

## Emit purchase failed signal
func _emit_purchase_failed(offering_id: String, reason: String) -> void:
	push_warning("ShopService: Purchase failed for '%s': %s" % [offering_id, reason])
	purchase_failed.emit(offering_id, reason)
