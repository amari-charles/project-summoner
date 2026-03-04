extends Node
class_name PlatformBillingService

## PlatformBilling - Main entry point for real-money purchases
##
## Detects the current platform and routes purchases to the appropriate
## billing provider (Steam, iOS, Android, or Stub for development).
##
## NOTE: This is scaffolding for future IAP support. Currently all Premium Store
## offerings use in-game gold. When real-money purchases are added, this system
## will handle platform-specific billing (App Store, Google Play, Steam).
##
## Usage:
##   PlatformBilling.purchase("gems_100")
##   PlatformBilling.purchase_completed.connect(_on_purchase_completed)

## Emitted when any purchase completes successfully
signal purchase_completed(product_id: String, transaction_id: String)

## Emitted when any purchase fails
signal purchase_failed(product_id: String, error: String)

## Emitted when user cancels a purchase
signal purchase_cancelled(product_id: String)

## Emitted when products are loaded
signal products_loaded(products: Array[BillingProduct])

## Emitted when restore completes
signal restore_completed(restored_product_ids: Array[String])

## Emitted when restore fails
signal restore_failed(error: String)


enum Platform {
	UNKNOWN,
	EDITOR,    ## Running in Godot editor
	STEAM,     ## Steam build
	IOS,       ## iOS App Store
	ANDROID,   ## Google Play
	WEB,       ## Web/HTML5 build
}

## Current detected platform
var current_platform: Platform = Platform.UNKNOWN

## Active billing provider
var _provider: BillingProvider


func _ready() -> void:
	current_platform = _detect_platform()
	_provider = _create_provider()
	_connect_provider_signals()
	_provider.initialize()

	print("[PlatformBilling] Platform: %s, Provider: %s" % [
		Platform.keys()[current_platform],
		_provider.get_provider_name()
	])


## =============================================================================
## PUBLIC API
## =============================================================================

## Check if billing is available on this platform
func is_available() -> bool:
	return _provider.is_available()


## Start a purchase flow
func purchase(product_id: String) -> void:
	if not is_available():
		purchase_failed.emit(product_id, "Billing not available on this platform")
		return
	_provider.purchase(product_id)


## Restore previous purchases (required for iOS)
func restore_purchases() -> void:
	_provider.restore_purchases()


## Get list of available products
func get_products() -> Array[BillingProduct]:
	return _provider.get_products()


## Get localized price string for a product
func get_localized_price(product_id: String) -> String:
	return _provider.get_localized_price(product_id)


## Check if a non-consumable product is owned
func is_product_owned(product_id: String) -> bool:
	return _provider.is_product_owned(product_id)


## Get the current platform
func get_platform() -> Platform:
	return current_platform


## Get the active provider (for debugging)
func get_provider() -> BillingProvider:
	return _provider


## =============================================================================
## STUB CONTROLS (only work when using StubBillingProvider)
## =============================================================================

## Set stub mode for testing
func stub_set_mode(mode: StubBillingProvider.Mode) -> void:
	if _provider is StubBillingProvider:
		_provider.set_mode(mode)
	else:
		push_warning("stub_set_mode only works with StubBillingProvider")


## Set stub delay for testing
func stub_set_delay(delay: float) -> void:
	if _provider is StubBillingProvider:
		_provider.set_delay(delay)
	else:
		push_warning("stub_set_delay only works with StubBillingProvider")


## Force stub to fail with specific message
func stub_set_fail_message(message: String) -> void:
	if _provider is StubBillingProvider:
		_provider.set_fail_message(message)
	else:
		push_warning("stub_set_fail_message only works with StubBillingProvider")


## =============================================================================
## INTERNAL
## =============================================================================

func _detect_platform() -> Platform:
	if OS.has_feature("editor"):
		return Platform.EDITOR

	if OS.has_feature("steam"):
		return Platform.STEAM

	if OS.has_feature("ios"):
		return Platform.IOS

	if OS.has_feature("android"):
		return Platform.ANDROID

	if OS.has_feature("web"):
		return Platform.WEB

	return Platform.UNKNOWN


func _create_provider() -> BillingProvider:
	match current_platform:
		Platform.EDITOR:
			# Always use stub in editor
			return _create_stub_provider()

		Platform.STEAM:
			# TODO: Return SteamBillingProvider when implemented
			push_warning("Steam billing not implemented, using stub")
			return _create_stub_provider()

		Platform.IOS:
			# TODO: Return iOSBillingProvider when implemented
			push_warning("iOS billing not implemented, using stub")
			return _create_stub_provider()

		Platform.ANDROID:
			# TODO: Return AndroidBillingProvider when implemented
			push_warning("Android billing not implemented, using stub")
			return _create_stub_provider()

		_:
			return _create_stub_provider()


func _create_stub_provider() -> StubBillingProvider:
	var provider: StubBillingProvider = StubBillingProvider.new()
	provider.name = "StubBillingProvider"
	add_child(provider)
	return provider


func _connect_provider_signals() -> void:
	_provider.purchase_completed.connect(_on_provider_purchase_completed)
	_provider.purchase_failed.connect(_on_provider_purchase_failed)
	_provider.purchase_cancelled.connect(_on_provider_purchase_cancelled)
	_provider.products_loaded.connect(_on_provider_products_loaded)
	_provider.restore_completed.connect(_on_provider_restore_completed)
	_provider.restore_failed.connect(_on_provider_restore_failed)


func _on_provider_purchase_completed(product_id: String, transaction_id: String) -> void:
	purchase_completed.emit(product_id, transaction_id)


func _on_provider_purchase_failed(product_id: String, error: String) -> void:
	purchase_failed.emit(product_id, error)


func _on_provider_purchase_cancelled(product_id: String) -> void:
	purchase_cancelled.emit(product_id)


func _on_provider_products_loaded(products: Array[BillingProduct]) -> void:
	products_loaded.emit(products)


func _on_provider_restore_completed(restored_product_ids: Array[String]) -> void:
	restore_completed.emit(restored_product_ids)


func _on_provider_restore_failed(error: String) -> void:
	restore_failed.emit(error)
