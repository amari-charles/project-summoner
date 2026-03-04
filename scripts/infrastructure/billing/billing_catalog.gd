extends Node
class_name BillingCatalogService

## BillingCatalog - Registry of all real-money purchasable products
##
## Defines gem packs, bundles, and other IAP products.
## Platform-specific product IDs should match store configurations.

## =============================================================================
## GEM PACKS (Consumable - can buy multiple times)
## =============================================================================

const GEM_PACKS: Dictionary = {
	"gems_100": {
		"gems": 100,
		"price_usd": 0.99,
		"bonus_percent": 0,
	},
	"gems_500": {
		"gems": 550,  # 500 + 10% bonus
		"price_usd": 4.99,
		"bonus_percent": 10,
	},
	"gems_1200": {
		"gems": 1320,  # 1200 + 10% bonus
		"price_usd": 9.99,
		"bonus_percent": 10,
	},
	"gems_2500": {
		"gems": 2750,  # 2500 + 10% bonus
		"price_usd": 19.99,
		"bonus_percent": 10,
	},
	"gems_6500": {
		"gems": 7150,  # 6500 + 10% bonus
		"price_usd": 49.99,
		"bonus_percent": 10,
	},
}

## =============================================================================
## BUNDLES (Non-consumable - one-time purchase)
## =============================================================================

const BUNDLES: Dictionary = {
	"starter_pack": {
		"display_name": "Starter Pack",
		"description": "200 Gems + Lightning Adept Summoner - Great value for new players!",
		"gems": 200,
		"rewards": {
			"summoners": ["lightning_adept"],
		},
		"price_usd": 4.99,
	},
	"summoner_pack_verdant": {
		"display_name": "Verdant Sage Pack",
		"description": "100 Gems + Verdant Sage Summoner",
		"gems": 100,
		"rewards": {
			"summoners": ["verdant_sage"],
		},
		"price_usd": 3.99,
	},
	"cosmetic_bundle_fire": {
		"display_name": "Fire Cosmetics Bundle",
		"description": "Fire-themed card back and UI theme",
		"gems": 0,
		"rewards": {
			"cosmetics": ["card_back_fire", "ui_theme_fire"],
		},
		"price_usd": 2.99,
	},
}

## =============================================================================
## CACHED DATA
## =============================================================================

var _products_cache: Dictionary = {}  # product_id -> BillingProduct


func _ready() -> void:
	_build_products_cache()


## =============================================================================
## PUBLIC API
## =============================================================================

## Get a product by ID
func get_product(product_id: String) -> BillingProduct:
	return _products_cache.get(product_id)


## Get all gem pack products
func get_gem_packs() -> Array[BillingProduct]:
	var packs: Array[BillingProduct] = []
	for pack_id: String in GEM_PACKS.keys():
		var product: BillingProduct = get_product(pack_id)
		if product:
			packs.append(product)
	return packs


## Get all bundle products
func get_bundles() -> Array[BillingProduct]:
	var bundles: Array[BillingProduct] = []
	for bundle_id: String in BUNDLES.keys():
		var product: BillingProduct = get_product(bundle_id)
		if product:
			bundles.append(product)
	return bundles


## Get all products
func get_all_products() -> Array[BillingProduct]:
	var all: Array[BillingProduct] = []
	for product: BillingProduct in _products_cache.values():
		all.append(product)
	return all


## Check if a product exists
func has_product(product_id: String) -> bool:
	return _products_cache.has(product_id)


## Check if a product is a gem pack
func is_gem_pack(product_id: String) -> bool:
	return GEM_PACKS.has(product_id)


## Check if a product is a bundle
func is_bundle(product_id: String) -> bool:
	return BUNDLES.has(product_id)


## =============================================================================
## INTERNAL
## =============================================================================

func _build_products_cache() -> void:
	_products_cache.clear()

	# Build gem pack products
	for pack_id: String in GEM_PACKS.keys():
		var pack_data: Dictionary = GEM_PACKS[pack_id]
		var product: BillingProduct = BillingProduct.create_gem_pack(
			pack_id,
			pack_data.gems,
			pack_data.price_usd,
			pack_data.get("bonus_percent", 0)
		)
		_products_cache[pack_id] = product

	# Build bundle products
	for bundle_id: String in BUNDLES.keys():
		var bundle_data: Dictionary = BUNDLES[bundle_id]
		var product: BillingProduct = BillingProduct.create_bundle(
			bundle_id,
			bundle_data.display_name,
			bundle_data.description,
			bundle_data.price_usd,
			bundle_data.get("rewards", {}),
			bundle_data.get("gems", 0)
		)
		_products_cache[bundle_id] = product

	print("[BillingCatalog] Loaded %d products (%d gem packs, %d bundles)" % [
		_products_cache.size(),
		GEM_PACKS.size(),
		BUNDLES.size()
	])
