extends BillingProvider
class_name StubBillingProvider

## StubBillingProvider - Development/testing stub for billing
##
## Simulates platform purchases without real transactions.
## Configurable behavior for testing different scenarios.

enum Mode {
	AUTO_SUCCESS,   ## All purchases succeed after delay
	AUTO_FAIL,      ## All purchases fail with error
	AUTO_CANCEL,    ## All purchases are cancelled by user
	INTERACTIVE,    ## Show debug popup to choose outcome (future)
}

## Current simulation mode
var mode: Mode = Mode.AUTO_SUCCESS

## Delay before purchase completes (simulates network/UI)
var simulate_delay: float = 1.0

## Error message to use in AUTO_FAIL mode
var fail_message: String = "Simulated billing failure"

## Cached products
var _products: Array[BillingProduct] = []

## Simulated owned products (for non-consumables)
var _owned_products: Dictionary = {}  # product_id -> bool

## Transaction counter for generating IDs
var _transaction_counter: int = 0


func initialize() -> void:
	_load_stub_products()
	print("[StubBilling] Initialized with %d products" % _products.size())


func is_available() -> bool:
	return true


func purchase(product_id: String) -> void:
	print("[StubBilling] Purchase requested: %s (mode: %s)" % [product_id, Mode.keys()[mode]])

	# Simulate async behavior
	await get_tree().create_timer(simulate_delay).timeout

	match mode:
		Mode.AUTO_SUCCESS:
			_complete_purchase(product_id)
		Mode.AUTO_FAIL:
			purchase_failed.emit(product_id, fail_message)
		Mode.AUTO_CANCEL:
			purchase_cancelled.emit(product_id)
		Mode.INTERACTIVE:
			# For now, default to success
			_complete_purchase(product_id)


func _complete_purchase(product_id: String) -> void:
	_transaction_counter += 1
	var transaction_id: String = "stub_txn_%d_%d" % [Time.get_unix_time_from_system(), _transaction_counter]

	# Mark non-consumables as owned
	var product: BillingProduct = _find_product(product_id)
	if product and product.product_type == BillingProduct.ProductType.NON_CONSUMABLE:
		_owned_products[product_id] = true

	print("[StubBilling] Purchase completed: %s -> %s" % [product_id, transaction_id])
	purchase_completed.emit(product_id, transaction_id)


func restore_purchases() -> void:
	print("[StubBilling] Restore requested")
	await get_tree().create_timer(0.5).timeout

	var restored: Array[String] = []
	for product_id: String in _owned_products.keys():
		if _owned_products[product_id]:
			restored.append(product_id)

	print("[StubBilling] Restored %d purchases" % restored.size())
	restore_completed.emit(restored)


func get_products() -> Array[BillingProduct]:
	return _products


func get_localized_price(product_id: String) -> String:
	var product: BillingProduct = _find_product(product_id)
	if product:
		return "$%.2f" % product.price_usd
	return ""


func is_product_owned(product_id: String) -> bool:
	return _owned_products.get(product_id, false)


func get_provider_name() -> String:
	return "StubBilling"


## =============================================================================
## STUB CONFIGURATION (for testing)
## =============================================================================

## Set the simulation mode
func set_mode(new_mode: Mode) -> void:
	mode = new_mode
	print("[StubBilling] Mode set to: %s" % Mode.keys()[mode])


## Set the simulation delay
func set_delay(delay: float) -> void:
	simulate_delay = delay


## Force a specific failure message
func set_fail_message(message: String) -> void:
	fail_message = message


## Simulate owning a product (for testing restore flow)
func simulate_own_product(product_id: String) -> void:
	_owned_products[product_id] = true


## Clear all simulated ownership
func clear_owned_products() -> void:
	_owned_products.clear()


## =============================================================================
## INTERNAL
## =============================================================================

func _find_product(product_id: String) -> BillingProduct:
	for product: BillingProduct in _products:
		if product.product_id == product_id:
			return product
	return null


func _load_stub_products() -> void:
	# Create stub products matching BillingCatalog
	_products = [
		# Gem packs
		BillingProduct.create_gem_pack("gems_100", 100, 0.99),
		BillingProduct.create_gem_pack("gems_500", 550, 4.99, 10),  # 10% bonus
		BillingProduct.create_gem_pack("gems_1200", 1320, 9.99, 10),  # 10% bonus
		BillingProduct.create_gem_pack("gems_2500", 2750, 19.99, 10),  # 10% bonus

		# Bundles
		BillingProduct.create_bundle(
			"starter_pack",
			"Starter Pack",
			"200 Gems + Lightning Adept Summoner",
			4.99,
			{"summoners": ["lightning_adept"]},
			200
		),
	]

	products_loaded.emit(_products)
