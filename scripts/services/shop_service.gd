extends Node
# ShopService is registered as autoload "Shop", no class_name needed

## ShopService - Manages shop offerings and purchases (GDScript wrapper for C#)
##
## Handles both Caravan (campaign event) shops and General UI shop.
## Data layer: ProfileRepo owns all persistent state
## Service layer: ShopService orchestrates purchases and validates
## Delegates to C# ShopServiceCS for core operations.
##
## Usage:
##   var offerings = Shop.get_shop_offerings("caravan_tutorial")
##   var success = Shop.purchase_offering(offering_id, "caravan_tutorial")

## Signals (forwarded from C#)
signal purchase_completed(offering_id: String, shop_id: String)
signal purchase_failed(offering_id: String, reason: String)
signal shop_refreshed(shop_id: String)

## C# service reference
var _cs_service: Node

## Shop catalog (_init_shops pattern like CampaignService._init_battles)
var _shops: Dictionary = {}  # shop_id -> shop_definition

## Pending real-money purchases (product_id -> offering_id, shop_id)
var _pending_billing_purchases: Dictionary = {}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("ShopService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/ShopServiceCS")
	if _cs_service == null:
		push_error("ShopService: ShopServiceCS autoload not found")
		return

	# Wait for dependencies
	await get_tree().process_frame

	# Connect to C# service signals
	_cs_service.PurchaseCompleted.connect(_on_cs_purchase_completed)
	_cs_service.PurchaseFailed.connect(_on_cs_purchase_failed)
	_cs_service.ShopRefreshed.connect(_on_cs_shop_refreshed)

	# Inject ProfileRepo callbacks
	_cs_service.SetProfileCallbacks(
		_get_resources,
		_update_resources,
		_is_summoner_unlocked,
		_is_cosmetic_owned,
		_is_emote_owned,
		_get_shop_refresh_state
	)

	# Inject RewardService callback
	_cs_service.SetRewardCallbacks(_grant_rewards)

	# Inject PlatformBilling callbacks
	_cs_service.SetBillingCallbacks(_initiate_billing_purchase, _add_gems)

	# Initialize shop catalog
	_init_shops()

	# Pass shops to C# service
	_cs_service.LoadShopsFromGDScript(_shops)

	# Connect to PlatformBilling signals for real-money purchases
	PlatformBilling.purchase_completed.connect(_on_billing_purchase_completed)
	PlatformBilling.purchase_failed.connect(_on_billing_purchase_failed)
	PlatformBilling.purchase_cancelled.connect(_on_billing_purchase_cancelled)

	print("ShopService: Ready (%d shops loaded)" % _shops.size())

## =============================================================================
## CALLBACK INJECTORS
## =============================================================================

func _get_resources() -> Dictionary:
	return ProfileRepo.get_resources()

func _update_resources(delta: Dictionary) -> void:
	ProfileRepo.update_resources(delta)

func _is_summoner_unlocked(summoner_id: String) -> bool:
	return ProfileRepo.is_summoner_unlocked(summoner_id)

func _is_cosmetic_owned(cosmetic_id: String) -> bool:
	return ProfileRepo.is_cosmetic_owned(cosmetic_id)

func _is_emote_owned(emote_id: String) -> bool:
	return ProfileRepo.is_emote_owned(emote_id)

func _get_shop_refresh_state(shop_id: String) -> Dictionary:
	return ProfileRepo.get_shop_refresh_state(shop_id)

func _grant_rewards(rewards: Dictionary) -> bool:
	return RewardService.grant_rewards(rewards)

func _initiate_billing_purchase(product_id: String) -> void:
	PlatformBilling.purchase(product_id)

func _add_gems() -> int:
	return Economy.get_gems()

## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_purchase_completed(offering_id: String, shop_id: String) -> void:
	purchase_completed.emit(offering_id, shop_id)

func _on_cs_purchase_failed(offering_id: String, reason: String) -> void:
	purchase_failed.emit(offering_id, reason)

func _on_cs_shop_refreshed(shop_id: String) -> void:
	shop_refreshed.emit(shop_id)

## =============================================================================
## SHOP CATALOG (GDScript-specific for localization)
## =============================================================================

func _init_shops() -> void:
	# General shop (persistent UI shop)
	_shops["general"] = {
		"id": "general",
		"shop_type": "general",
		"name": Loc.t("shop.general.name"),
		"offerings": [
			{
				"offering_id": "general_fire_wisp",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.fire_wisp.name"),
				"description": Loc.t("shop.offering.fire_wisp.description"),
				"card_catalog_id": "fire_wisp",
				"card_count": 1,
				"base_price": 30,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_pebbloom",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.pebbloom.name"),
				"description": Loc.t("shop.offering.pebbloom.description"),
				"card_catalog_id": "pebbloom",
				"card_count": 1,
				"base_price": 20,
				"purchase_limit_type": "none",
				"purchase_limit": 0
			},
			{
				"offering_id": "general_puff",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.puff.name"),
				"description": Loc.t("shop.offering.puff.description"),
				"card_catalog_id": "puff",
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
					{"catalog_id": "mana_bolt", "count": 2}
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
					{"catalog_id": "pebbloom", "count": 2},
					{"catalog_id": "fire_wisp", "count": 1}
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
				"offering_id": "caravan_fire_wisp",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.fire_wisp.name"),
				"description": Loc.t("shop.offering.fire_wisp.description"),
				"card_catalog_id": "fire_wisp",
				"card_count": 1,
				"base_price": 25,
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "caravan_pebbloom",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.pebbloom.name"),
				"description": Loc.t("shop.offering.pebbloom.description"),
				"card_catalog_id": "pebbloom",
				"card_count": 1,
				"base_price": 20,
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "caravan_puff",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.puff.name"),
				"description": Loc.t("shop.offering.puff.description"),
				"card_catalog_id": "puff",
				"card_count": 1,
				"base_price": 30,
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "caravan_cloud_swarm",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.cloud_swarm.name"),
				"description": Loc.t("shop.offering.cloud_swarm.description"),
				"card_catalog_id": "cloud_swarm",
				"card_count": 1,
				"base_price": 45,
				"purchase_limit_type": "account",
				"purchase_limit": 1
			},
			{
				"offering_id": "caravan_mana_bolt",
				"offering_type": ShopOffering.OfferingType.CARD,
				"display_name": Loc.t("shop.offering.mana_bolt.name"),
				"description": Loc.t("shop.offering.mana_bolt.description"),
				"card_catalog_id": "mana_bolt",
				"card_count": 1,
				"base_price": 35,
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
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
				"currency_type": "gems",
				"purchase_limit_type": "account",
				"purchase_limit": 1
			}
		]
	}

	print("ShopService: Initialized %d shops" % _shops.size())

## =============================================================================
## SHOP QUERIES (delegated to C#)
## =============================================================================

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

	# Filter for caravan shops - hide purchased offerings
	if shop_id.begins_with("caravan"):
		var purchased_ids: Array = ProfileRepo.get_caravan_purchases()
		result = result.filter(func(o: ShopOffering) -> bool: return o.offering_id not in purchased_ids)

	return result

## =============================================================================
## PURCHASE LOGIC (delegated to C#)
## =============================================================================

## Purchase an offering
func purchase_offering(offering_id: String, shop_id: String = "general") -> bool:
	if _cs_service == null:
		_emit_purchase_failed(offering_id, "Service not available")
		return false

	var offering_dict: Dictionary = _cs_service.FindOffering(offering_id, shop_id)
	if offering_dict.is_empty():
		_emit_purchase_failed(offering_id, "Offering not found")
		return false

	# Check if already owned
	var already_owned_reason: String = _cs_service.CheckAlreadyOwned(offering_dict)
	if not already_owned_reason.is_empty():
		_emit_purchase_failed(offering_id, already_owned_reason)
		return false

	# Get shop refresh state
	var refresh_epoch: int = _cs_service.GetRefreshEpoch(shop_id)
	var purchase_key: String = _cs_service.BuildPurchaseKey(shop_id, offering_id, refresh_epoch)
	var purchase_count: int = _cs_service.GetPurchaseCount(purchase_key)

	# Determine if this is a caravan (campaign) shop - uses campaign gold
	var is_caravan: bool = shop_id.begins_with("caravan")

	# Get current resources - use campaign gold for caravan shops
	var player_gold: int = Economy.get_campaign_gold() if is_caravan else _cs_service.GetPlayerGold()
	var player_gems: int = _cs_service.GetPlayerGems()

	# Validate purchase
	var failure_reason: String = _cs_service.ValidatePurchase(offering_dict, player_gold, player_gems, purchase_count)
	if not failure_reason.is_empty():
		_emit_purchase_failed(offering_id, failure_reason)
		return false

	# Execute purchase based on currency type
	var price: int = offering_dict.get("base_price", 0)
	var currency: String = offering_dict.get("currency_type", "gold")

	match currency:
		"gold":
			if is_caravan:
				# Use campaign gold for caravan purchases
				return _complete_caravan_purchase(offering_dict, offering_id, shop_id, purchase_key, price)
			else:
				return _cs_service.CompleteCurrencyPurchase(offering_dict, offering_id, shop_id, purchase_key, price, currency)
		"gems":
			return _cs_service.CompleteCurrencyPurchase(offering_dict, offering_id, shop_id, purchase_key, price, currency)
		"real_money":
			# Delegate to PlatformBilling (async)
			var product_id: String = offering_dict.get("product_id", offering_id)
			_pending_billing_purchases[product_id] = {
				"offering_id": offering_id,
				"shop_id": shop_id,
				"purchase_key": purchase_key,
				"offering_dict": offering_dict
			}
			_cs_service.InitiateBillingPurchase(product_id)
			print("ShopService: Initiated real-money purchase for '%s'" % offering_id)
			return true  # Async - result comes via billing signals
		_:
			_emit_purchase_failed(offering_id, "Unknown currency type: %s" % currency)
			return false

## Complete a caravan purchase using campaign gold
func _complete_caravan_purchase(offering_dict: Dictionary, offering_id: String, shop_id: String, purchase_key: String, price: int) -> bool:
	# Spend campaign gold
	if not Economy.spend_campaign_gold(price):
		_emit_purchase_failed(offering_id, "Cannot afford %d campaign gold" % price)
		return false

	# Grant rewards via RewardService
	var rewards: Dictionary = _cs_service.BuildRewardDict(offering_dict, shop_id)
	if not RewardService.grant_rewards(rewards):
		# Rollback: Refund campaign gold
		Economy.add_campaign_gold(price)
		_emit_purchase_failed(offering_id, "Failed to grant rewards")
		return false

	# Record purchase via profile (account-wide tracking)
	ProfileRepo.increment_purchase_count(purchase_key)

	# Record as caravan purchase (campaign-scoped for UI filtering)
	ProfileRepo.add_caravan_purchase(offering_id)

	# Emit success
	purchase_completed.emit(offering_id, shop_id)
	print("ShopService: Completed caravan purchase '%s' for %d campaign gold" % [offering_id, price])
	return true

## =============================================================================
## INTERNAL HELPERS
## =============================================================================

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

## Emit purchase failed signal
func _emit_purchase_failed(offering_id: String, reason: String) -> void:
	push_warning("ShopService: Purchase failed for '%s': %s" % [offering_id, reason])
	purchase_failed.emit(offering_id, reason)

## Check if an offering is already owned (public API for UI)
func is_offering_owned(offering: ShopOffering) -> bool:
	if _cs_service == null:
		return false
	# Build dict from offering for C# service
	var offering_dict: Dictionary = {
		"offering_type": offering.offering_type,
		"summoner_id": offering.summoner_id,
		"cosmetic_id": offering.cosmetic_id,
		"emote_id": offering.emote_id
	}
	return _cs_service.IsOfferingOwned(offering_dict)

## =============================================================================
## PLATFORM BILLING HANDLERS
## =============================================================================

func _on_billing_purchase_completed(product_id: String, transaction_id: String) -> void:
	print("ShopService: Billing purchase completed - product: %s, txn: %s" % [product_id, transaction_id])

	# Check if this was a shop offering purchase
	if _pending_billing_purchases.has(product_id):
		var pending: Dictionary = _pending_billing_purchases[product_id]
		_pending_billing_purchases.erase(product_id)

		var offering_dict: Dictionary = pending.offering_dict
		var offering_id: String = pending.offering_id
		var shop_id: String = pending.shop_id
		var purchase_key: String = pending.purchase_key

		# Delegate to C# service
		_cs_service.OnBillingPurchaseCompleted(offering_dict, offering_id, shop_id, purchase_key)
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
