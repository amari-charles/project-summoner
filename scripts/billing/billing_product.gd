extends Resource
class_name BillingProduct

## BillingProduct - Data model for real-money purchasable products
##
## Represents items that can be bought with real money through platform stores.
## Products can grant gems (consumable) or direct content (non-consumable).

enum ProductType {
	CONSUMABLE,      ## Can be purchased multiple times (gem packs)
	NON_CONSUMABLE,  ## One-time purchase (starter packs, unlocks)
	SUBSCRIPTION,    ## Recurring (future: battle pass)
}

## Platform-specific product ID (e.g., "com.game.gems_100" for iOS)
@export var product_id: String

## Internal reference ID used in our systems
@export var internal_id: String

## Display name shown to players
@export var display_name: String

## Description shown in store
@export var description: String

## Type of product
@export var product_type: ProductType = ProductType.CONSUMABLE

## Base price in USD (for reference/fallback)
@export var price_usd: float = 0.99

## Amount of gems granted (for gem packs)
@export var gems_amount: int = 0

## Direct rewards granted (for bundles)
## Format: { "summoners": ["id"], "cosmetics": ["id"], "cards": {"id": count}, "gold": amount }
@export var rewards: Dictionary = {}

## Optional: bonus percentage for display ("20% bonus!")
@export var bonus_percent: int = 0

## Optional: is this a limited-time offer?
@export var is_limited_time: bool = false


## Create a gem pack product
static func create_gem_pack(id: String, gems: int, price: float, bonus: int = 0) -> BillingProduct:
	var product: BillingProduct = BillingProduct.new()
	product.product_id = id
	product.internal_id = id
	product.display_name = "%d Gems" % gems
	product.description = "A pack of %d gems" % gems
	product.product_type = ProductType.CONSUMABLE
	product.price_usd = price
	product.gems_amount = gems
	product.bonus_percent = bonus
	return product


## Create a bundle product with direct rewards
static func create_bundle(id: String, name: String, desc: String, price: float, reward_dict: Dictionary, gems: int = 0) -> BillingProduct:
	var product: BillingProduct = BillingProduct.new()
	product.product_id = id
	product.internal_id = id
	product.display_name = name
	product.description = desc
	product.product_type = ProductType.NON_CONSUMABLE
	product.price_usd = price
	product.gems_amount = gems
	product.rewards = reward_dict
	return product
