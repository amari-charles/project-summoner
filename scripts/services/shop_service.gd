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

## Pending real-money purchases (product_id -> offering_id, shop_id)
var _pending_billing_purchases: Dictionary = {}

func _ready() -> void:
	print("ShopService: Initializing...")

	# Wait for dependencies
	await get_tree().process_frame

	# Load purchase history into cache
	_purchase_cache = ProfileRepo.get_shop_purchases()

	# Initialize shop catalog
	_init_shops()

	# Connect to PlatformBilling signals for real-money purchases
	PlatformBilling.purchase_completed.connect(_on_billing_purchase_completed)
	PlatformBilling.purchase_failed.connect(_on_billing_purchase_failed)
	PlatformBilling.purchase_cancelled.connect(_on_billing_purchase_cancelled)

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

	# Premium Store (account-level meta-progression purchases)
	# Summoners, cosmetics, emotes - accessed from campaign map
	_shops["premium_store"] = {
		"id": "premium_store",
		"shop_type": "premium",
		"name": Loc.t("shop.premium.name"),
		"offerings": [
			# =====================================================================
			# SUMMONER OFFERINGS
			# =====================================================================
			{
				"offering_id": "summoner_lightning_adept",
				"offering_type": ShopOffering.OfferingType.SUMMONER,
				"display_name": Loc.t("shop.offering.lightning_adept.name"),
				"description": Loc.t("shop.offering.lightning_adept.description"),
				"summoner_id": "summoner_lightning_adept",
				"base_price": 750,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "summoner_verdant_sage",
				"offering_type": ShopOffering.OfferingType.SUMMONER,
				"display_name": Loc.t("shop.offering.verdant_sage.name"),
				"description": Loc.t("shop.offering.verdant_sage.description"),
				"summoner_id": "summoner_verdant_sage",
				"base_price": 750,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "summoner_void_walker",
				"offering_type": ShopOffering.OfferingType.SUMMONER,
				"display_name": Loc.t("shop.offering.void_walker.name"),
				"description": Loc.t("shop.offering.void_walker.description"),
				"summoner_id": "summoner_void_walker",
				"base_price": 750,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			# =====================================================================
			# COSMETIC OFFERINGS
			# =====================================================================
			{
				"offering_id": "cosmetic_card_back_gold",
				"offering_type": ShopOffering.OfferingType.COSMETIC,
				"display_name": Loc.t("shop.offering.card_back_gold.name"),
				"description": Loc.t("shop.offering.card_back_gold.description"),
				"cosmetic_type": "card_back",
				"cosmetic_id": "card_back_gold",
				"base_price": 300,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "cosmetic_card_back_obsidian",
				"offering_type": ShopOffering.OfferingType.COSMETIC,
				"display_name": Loc.t("shop.offering.card_back_obsidian.name"),
				"description": Loc.t("shop.offering.card_back_obsidian.description"),
				"cosmetic_type": "card_back",
				"cosmetic_id": "card_back_obsidian",
				"base_price": 500,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "cosmetic_ui_theme_crimson",
				"offering_type": ShopOffering.OfferingType.COSMETIC,
				"display_name": Loc.t("shop.offering.ui_theme_crimson.name"),
				"description": Loc.t("shop.offering.ui_theme_crimson.description"),
				"cosmetic_type": "ui_theme",
				"cosmetic_id": "ui_theme_crimson",
				"base_price": 400,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "cosmetic_ui_theme_void",
				"offering_type": ShopOffering.OfferingType.COSMETIC,
				"display_name": Loc.t("shop.offering.ui_theme_void.name"),
				"description": Loc.t("shop.offering.ui_theme_void.description"),
				"cosmetic_type": "ui_theme",
				"cosmetic_id": "ui_theme_void",
				"base_price": 600,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			# =====================================================================
			# EMOTE OFFERINGS
			# =====================================================================
			{
				"offering_id": "emote_laugh",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_laugh.name"),
				"description": Loc.t("shop.offering.emote_laugh.description"),
				"emote_id": "emote_laugh",
				"base_price": 150,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "emote_shocked",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_shocked.name"),
				"description": Loc.t("shop.offering.emote_shocked.description"),
				"emote_id": "emote_shocked",
				"base_price": 150,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "emote_thinking",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_thinking.name"),
				"description": Loc.t("shop.offering.emote_thinking.description"),
				"emote_id": "emote_thinking",
				"base_price": 200,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "emote_taunt",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_taunt.name"),
				"description": Loc.t("shop.offering.emote_taunt.description"),
				"emote_id": "emote_taunt",
				"base_price": 250,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "emote_confident",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_confident.name"),
				"description": Loc.t("shop.offering.emote_confident.description"),
				"emote_id": "emote_confident",
				"base_price": 300,
				"currency_type": "gold",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "emote_victory",
				"offering_type": ShopOffering.OfferingType.EMOTE,
				"display_name": Loc.t("shop.offering.emote_victory.name"),
				"description": Loc.t("shop.offering.emote_victory.description"),
				"emote_id": "emote_victory",
				"base_price": 350,
				"currency_type": "gold",
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

	# Check if already owned (for one-time purchases like summoners/cosmetics/emotes)
	var already_owned_reason: String = _check_already_owned(offering)
	if not already_owned_reason.is_empty():
		_emit_purchase_failed(offering_id, already_owned_reason)
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
	var gems: int = resources.get("gems", 0)
	var purchase_count: int = _purchase_cache.get(purchase_key, 0)

	# Build context for validation
	var context: ShopPurchaseContext = ShopPurchaseContext.new()
	context.player_gold = gold
	context.player_gems = gems
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
	var currency: String = offering.currency_type

	# Handle different currency types
	match currency:
		"gold":
			return _complete_currency_purchase(offering, offering_id, shop_id, purchase_key, price, "gold")
		"gems":
			return _complete_currency_purchase(offering, offering_id, shop_id, purchase_key, price, "gems")
		"real_money":
			# Delegate to PlatformBilling (async)
			var product_id: String = offering.product_id if offering.product_id else offering_id
			_pending_billing_purchases[product_id] = {
				"offering_id": offering_id,
				"shop_id": shop_id,
				"purchase_key": purchase_key,
				"offering": offering
			}
			PlatformBilling.purchase(product_id)
			print("ShopService: Initiated real-money purchase for '%s'" % offering_id)
			return true  # Async - result comes via billing signals
		_:
			_emit_purchase_failed(offering_id, "Unknown currency type: %s" % currency)
			return false


## Complete a purchase using in-game currency (gold or gems)
func _complete_currency_purchase(offering: ShopOffering, offering_id: String, shop_id: String, purchase_key: String, price: int, currency: String) -> bool:
	# Step 1: Deduct currency
	ProfileRepo.update_resources({currency: -price})

	# Step 2: Grant rewards via RewardService
	var rewards: Dictionary = _build_reward_dict(offering)
	if not RewardService.grant_rewards(rewards):
		# Rollback: Refund currency
		ProfileRepo.update_resources({currency: price})
		_emit_purchase_failed(offering_id, "Failed to grant rewards")
		return false

	# Step 3: Track purchase (namespaced key)
	if not ProfileRepo.increment_purchase_count(purchase_key):
		push_warning("ShopService: Failed to track purchase count")
	else:
		_purchase_cache[purchase_key] = _purchase_cache.get(purchase_key, 0) + 1

	purchase_completed.emit(offering_id, shop_id)
	print("ShopService: Purchased '%s' for %d %s" % [offering_id, price, currency])
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
	offering.currency_type = def.get("currency_type", "gold")
	offering.product_id = def.get("product_id", "")
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

	# For SUMMONER types
	offering.summoner_id = def.get("summoner_id", "")

	# For COSMETIC types
	offering.cosmetic_type = def.get("cosmetic_type", "")
	offering.cosmetic_id = def.get("cosmetic_id", "")

	# For EMOTE types
	offering.emote_id = def.get("emote_id", "")

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
			# Legacy - use COSMETIC/EMOTE instead
			pass

		ShopOffering.OfferingType.SUMMONER:
			rewards["summoner"] = offering.summoner_id

		ShopOffering.OfferingType.COSMETIC:
			rewards["cosmetic"] = offering.cosmetic_id

		ShopOffering.OfferingType.EMOTE:
			rewards["emote"] = offering.emote_id

	return rewards

## Get human-readable failure reason
func _get_failure_reason(offering: ShopOffering, context: ShopPurchaseContext) -> String:
	var price: int = offering.get_price(context)

	# Check currency
	match offering.currency_type:
		"gold":
			if context.player_gold < price:
				return "Not enough gold (need %d, have %d)" % [price, context.player_gold]
		"gems":
			if context.player_gems < price:
				return "Not enough gems (need %d, have %d)" % [price, context.player_gems]

	if offering.purchase_limit_type != "none" and offering.purchase_limit > 0:
		if context.purchase_count >= offering.purchase_limit:
			return "Purchase limit reached"

	return "Unknown error"

## Emit purchase failed signal
func _emit_purchase_failed(offering_id: String, reason: String) -> void:
	push_warning("ShopService: Purchase failed for '%s': %s" % [offering_id, reason])
	purchase_failed.emit(offering_id, reason)

## Check if an offering is already owned (for one-time purchases)
## Returns empty string if not owned, or failure reason if already owned
func _check_already_owned(offering: ShopOffering) -> String:
	match offering.offering_type:
		ShopOffering.OfferingType.SUMMONER:
			if ProfileRepo.is_summoner_unlocked(offering.summoner_id):
				return Loc.t("shop.error.already_owned")

		ShopOffering.OfferingType.COSMETIC:
			if ProfileRepo.is_cosmetic_owned(offering.cosmetic_id):
				return Loc.t("shop.error.already_owned")

		ShopOffering.OfferingType.EMOTE:
			if ProfileRepo.is_emote_owned(offering.emote_id):
				return Loc.t("shop.error.already_owned")

	return ""  # Not owned, can purchase

## Check if an offering is already owned (public API for UI)
func is_offering_owned(offering: ShopOffering) -> bool:
	return not _check_already_owned(offering).is_empty()


## =============================================================================
## PLATFORM BILLING HANDLERS
## =============================================================================

func _on_billing_purchase_completed(product_id: String, transaction_id: String) -> void:
	print("ShopService: Billing purchase completed - product: %s, txn: %s" % [product_id, transaction_id])

	# Check if this was a shop offering purchase
	if _pending_billing_purchases.has(product_id):
		var pending: Dictionary = _pending_billing_purchases[product_id]
		_pending_billing_purchases.erase(product_id)

		var offering: ShopOffering = pending.offering
		var offering_id: String = pending.offering_id
		var shop_id: String = pending.shop_id
		var purchase_key: String = pending.purchase_key

		# Grant the offering rewards
		var rewards: Dictionary = _build_reward_dict(offering)
		if RewardService.grant_rewards(rewards):
			# Track purchase
			if ProfileRepo.increment_purchase_count(purchase_key):
				_purchase_cache[purchase_key] = _purchase_cache.get(purchase_key, 0) + 1
			purchase_completed.emit(offering_id, shop_id)
			print("ShopService: Real-money purchase completed for '%s'" % offering_id)
		else:
			# Failed to grant rewards - this is a problem (payment went through)
			push_error("ShopService: CRITICAL - Payment completed but failed to grant rewards for '%s'" % offering_id)
			_emit_purchase_failed(offering_id, "Failed to grant rewards after payment")
	else:
		# Direct billing product (gem pack, not a shop offering)
		var product: BillingProduct = BillingCatalog.get_product(product_id)
		if product:
			# Grant gems
			if product.gems_amount > 0:
				Economy.add_gems(product.gems_amount)
				print("ShopService: Granted %d gems from billing purchase" % product.gems_amount)

			# Grant direct rewards
			if not product.rewards.is_empty():
				RewardService.grant_rewards(product.rewards)
				print("ShopService: Granted direct rewards from billing purchase")
		else:
			push_warning("ShopService: Unknown billing product: %s" % product_id)


func _on_billing_purchase_failed(product_id: String, error: String) -> void:
	print("ShopService: Billing purchase failed - product: %s, error: %s" % [product_id, error])

	if _pending_billing_purchases.has(product_id):
		var pending: Dictionary = _pending_billing_purchases[product_id]
		_pending_billing_purchases.erase(product_id)

		var offering_id: String = pending.offering_id
		_emit_purchase_failed(offering_id, "Payment failed: %s" % error)


func _on_billing_purchase_cancelled(product_id: String) -> void:
	print("ShopService: Billing purchase cancelled - product: %s" % product_id)

	if _pending_billing_purchases.has(product_id):
		var pending: Dictionary = _pending_billing_purchases[product_id]
		_pending_billing_purchases.erase(product_id)

		var offering_id: String = pending.offering_id
		_emit_purchase_failed(offering_id, "Purchase cancelled")
