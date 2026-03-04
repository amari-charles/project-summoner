extends Node
class_name BillingProvider

## BillingProvider - Abstract base class for platform-specific billing implementations
##
## Subclasses implement platform-specific purchase logic:
## - StubBillingProvider (dev/testing)
## - SteamBillingProvider (future)
## - iOSBillingProvider (future)
## - AndroidBillingProvider (future)

## Emitted when a purchase completes successfully
signal purchase_completed(product_id: String, transaction_id: String)

## Emitted when a purchase fails
signal purchase_failed(product_id: String, error: String)

## Emitted when user cancels a purchase
signal purchase_cancelled(product_id: String)

## Emitted when product list is loaded from platform
signal products_loaded(products: Array[BillingProduct])

## Emitted when purchases are restored (iOS requirement)
signal restore_completed(restored_product_ids: Array[String])

## Emitted when restore fails
signal restore_failed(error: String)


## Initialize the billing provider (connect to platform SDK, etc.)
func initialize() -> void:
	push_warning("BillingProvider.initialize() not implemented in %s" % get_class())


## Check if billing is available on this platform
func is_available() -> bool:
	return false


## Start a purchase flow for the given product
func purchase(product_id: String) -> void:
	push_warning("BillingProvider.purchase() not implemented in %s" % get_class())
	purchase_failed.emit(product_id, "Not implemented")


## Restore previous purchases (required for iOS non-consumables)
func restore_purchases() -> void:
	push_warning("BillingProvider.restore_purchases() not implemented in %s" % get_class())
	restore_failed.emit("Not implemented")


## Get cached list of available products
func get_products() -> Array[BillingProduct]:
	return []


## Get localized price string from platform (e.g., "$0.99", "€0,99")
func get_localized_price(product_id: String) -> String:
	# Default fallback - subclasses should override with platform-specific pricing
	var products: Array[BillingProduct] = get_products()
	for product: BillingProduct in products:
		if product.product_id == product_id:
			return "$%.2f" % product.price_usd
	return ""


## Check if a specific product has been purchased (for non-consumables)
func is_product_owned(product_id: String) -> bool:
	return false


## Get the provider name for logging/debugging
func get_provider_name() -> String:
	return "BaseProvider"
