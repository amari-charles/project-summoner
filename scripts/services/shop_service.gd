extends Node
# ShopService is registered as autoload "Shop", no class_name needed

## ShopService - Manages shop offerings and purchases
##
## Handles both Caravan (campaign event) shops and General UI shop.
## Tracks purchase history, manages currency, validates purchases.
##
## Usage:
##   var offerings = Shop.get_caravan_offerings("caravan_tutorial")
##   var success = Shop.purchase_offering(offering_id, "caravan_tutorial")
##   var gold = Shop.get_player_gold()

## Signals
signal purchase_completed(offering_id: String, shop_id: String)
signal purchase_failed(offering_id: String, reason: String)
signal gold_changed(new_amount: int)
signal shop_refreshed(shop_id: String)

## Dependencies
var _profile_repo: Node = null
var _collection: Node = null

## Shop data
var _caravan_shops: Dictionary = {}  # shop_id -> Array[ShopOffering]
var _general_shop_offerings: Array[ShopOffering] = []
var _purchase_history: Dictionary = {}  # offering_id -> purchase_count

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("ShopService: Initializing...")

	# Wait for dependencies
	await get_tree().process_frame

	_profile_repo = get_node("/root/ProfileRepo")
	_collection = get_node("/root/Collection")

	if _profile_repo == null:
		push_error("ShopService: ProfileRepo not found!")
		return

	if _collection == null:
		push_error("ShopService: Collection not found!")
		return

	_load_purchase_history()
	_init_caravan_shops()

	print("ShopService: Ready")

## =============================================================================
## CURRENCY MANAGEMENT
## =============================================================================

## Get player's current gold
func get_player_gold() -> int:
	if not _profile_repo or not _profile_repo.has_method("get_active_profile"):
		return 0

	var profile: Variant = _profile_repo.call("get_active_profile")
	if not profile is Dictionary:
		return 0

	var profile_dict: Dictionary = profile
	var resources: Variant = profile_dict.get("resources", {})
	if not resources is Dictionary:
		return 0

	var resources_dict: Dictionary = resources
	var gold_val: Variant = resources_dict.get("gold", 0)
	return gold_val if gold_val is int else 0

## Add gold to player's account
func add_gold(amount: int) -> bool:
	if amount <= 0:
		return false

	var current_gold: int = get_player_gold()
	return _set_player_gold(current_gold + amount)

## Subtract gold from player's account
func subtract_gold(amount: int) -> bool:
	if amount <= 0:
		return false

	var current_gold: int = get_player_gold()
	if current_gold < amount:
		return false

	return _set_player_gold(current_gold - amount)

## Internal: Set player gold
func _set_player_gold(new_amount: int) -> bool:
	if not _profile_repo or not _profile_repo.has_method("get_active_profile"):
		return false

	var profile: Variant = _profile_repo.call("get_active_profile")
	if not profile is Dictionary:
		return false

	var profile_dict: Dictionary = profile
	if not profile_dict.has("resources"):
		profile_dict["resources"] = {}

	var resources: Variant = profile_dict["resources"]
	if not resources is Dictionary:
		return false

	var resources_dict: Dictionary = resources
	resources_dict["gold"] = new_amount
	resources_dict["updated_at"] = Time.get_unix_time_from_system()

	# Save profile
	if _profile_repo.has_method("save_profile"):
		_profile_repo.call("save_profile", true)

	gold_changed.emit(new_amount)
	return true

## =============================================================================
## CARAVAN SHOP MANAGEMENT
## =============================================================================

func _init_caravan_shops() -> void:
	# TODO: Load caravan shop definitions from resources
	# For now, create placeholder tutorial caravan
	_caravan_shops["caravan_tutorial"] = []

	print("ShopService: Initialized %d caravan shops" % _caravan_shops.size())

## Get offerings for a specific caravan shop
func get_caravan_offerings(shop_id: String) -> Array[ShopOffering]:
	var offerings_variant: Variant = _caravan_shops.get(shop_id, [])
	if offerings_variant is Array:
		var offerings_array: Array = offerings_variant
		var typed_offerings: Array[ShopOffering] = []
		for offering: Variant in offerings_array:
			if offering is ShopOffering:
				typed_offerings.append(offering)
		return typed_offerings
	return []

## Register a caravan shop (called by campaign events)
func register_caravan_shop(shop_id: String, offerings: Array[ShopOffering]) -> void:
	_caravan_shops[shop_id] = offerings
	print("ShopService: Registered caravan shop '%s' with %d offerings" % [shop_id, offerings.size()])

## =============================================================================
## GENERAL SHOP MANAGEMENT
## =============================================================================

## Get general shop offerings
func get_general_shop_offerings() -> Array[ShopOffering]:
	return _general_shop_offerings

## Refresh general shop (for rotating shops)
func refresh_general_shop() -> void:
	# TODO: Implement rotation logic
	_general_shop_offerings.clear()
	shop_refreshed.emit("general")
	print("ShopService: General shop refreshed")

## =============================================================================
## PURCHASE LOGIC
## =============================================================================

## Purchase an offering
func purchase_offering(offering_id: String, shop_id: String = "general") -> bool:
	# Find the offering
	var offering: ShopOffering = _find_offering(offering_id, shop_id)
	if not offering:
		_emit_purchase_failed(offering_id, "Offering not found")
		return false

	# Get purchase count
	var purchase_count: int = _get_purchase_count(offering_id)

	# Check if can purchase
	var player_gold: int = get_player_gold()
	if not offering.can_purchase(player_gold, purchase_count):
		var reason: String = "Insufficient gold or purchase limit reached"
		if player_gold < offering.get_price():
			reason = "Not enough gold (need %d, have %d)" % [offering.get_price(), player_gold]
		elif offering.get_remaining_stock() == 0:
			reason = "Out of stock"
		_emit_purchase_failed(offering_id, reason)
		return false

	# Deduct gold
	if not subtract_gold(offering.get_price()):
		_emit_purchase_failed(offering_id, "Failed to deduct gold")
		return false

	# Grant rewards
	if not _grant_offering_rewards(offering):
		# Refund gold
		add_gold(offering.get_price())
		_emit_purchase_failed(offering_id, "Failed to grant rewards")
		return false

	# Track purchase
	_increment_purchase_count(offering_id)
	offering.purchases_made += 1

	purchase_completed.emit(offering_id, shop_id)
	print("ShopService: Purchased '%s' for %d gold" % [offering_id, offering.get_price()])
	return true

## Internal: Find offering by ID
func _find_offering(offering_id: String, shop_id: String) -> ShopOffering:
	# Check caravan shops
	if shop_id != "general":
		var offerings: Array[ShopOffering] = get_caravan_offerings(shop_id)
		for offering: ShopOffering in offerings:
			if offering.offering_id == offering_id:
				return offering

	# Check general shop
	for offering: ShopOffering in _general_shop_offerings:
		if offering.offering_id == offering_id:
			return offering

	return null

## Internal: Grant offering rewards
func _grant_offering_rewards(offering: ShopOffering) -> bool:
	if not _collection:
		return false

	match offering.offering_type:
		ShopOffering.OfferingType.CARD:
			# Add card to collection
			if offering.card_catalog_id.is_empty():
				push_error("ShopService: Card offering has no catalog_id")
				return false

			for _i in range(offering.card_count):
				if _collection.has_method("add_card"):
					var result: Variant = _collection.call("add_card", offering.card_catalog_id, "common")
					if not result is String or (result as String).is_empty():
						push_error("ShopService: Failed to add card '%s'" % offering.card_catalog_id)
						return false

			return true

		ShopOffering.OfferingType.CARD_PACK:
			# Add multiple cards
			for card_data: Dictionary in offering.pack_cards:
				var catalog_id: String = card_data.get("catalog_id", "")
				var count: int = card_data.get("count", 1)
				if catalog_id.is_empty():
					continue

				for _i in range(count):
					if _collection.has_method("add_card"):
						var result: Variant = _collection.call("add_card", catalog_id, "common")
						if not result is String or (result as String).is_empty():
							push_error("ShopService: Failed to add pack card '%s'" % catalog_id)
							return false

			return true

		ShopOffering.OfferingType.CURRENCY:
			# Add currency
			# TODO: Implement currency rewards
			return true

		ShopOffering.OfferingType.SPECIAL:
			# Special items
			# TODO: Implement special item grants
			return true

	return false

## =============================================================================
## PURCHASE HISTORY
## =============================================================================

func _load_purchase_history() -> void:
	# TODO: Load from profile
	_purchase_history.clear()

func _get_purchase_count(offering_id: String) -> int:
	return _purchase_history.get(offering_id, 0)

func _increment_purchase_count(offering_id: String) -> void:
	var count: int = _get_purchase_count(offering_id)
	_purchase_history[offering_id] = count + 1
	# TODO: Save to profile

## =============================================================================
## HELPERS
## =============================================================================

func _emit_purchase_failed(offering_id: String, reason: String) -> void:
	push_warning("ShopService: Purchase failed for '%s': %s" % [offering_id, reason])
	purchase_failed.emit(offering_id, reason)
