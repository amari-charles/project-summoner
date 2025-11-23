extends Resource
class_name ShopOffering

## ShopOffering - Defines a purchasable item in a shop
##
## Used by both Caravan (campaign events) and General Shop (UI)
## Configurable pricing, limits, and conditions

## Item type
enum OfferingType {
	CARD,           # Single card purchase
	CARD_PACK,      # Multiple cards in a bundle
	CURRENCY,       # Gold or other currency
	SPECIAL         # Special items (cosmetics, etc.)
}

## Offering metadata
@export var offering_id: String = ""
@export var offering_type: OfferingType = OfferingType.CARD
@export var display_name: String = ""
@export var description: String = ""

## Card-specific (for CARD type)
@export var card_catalog_id: String = ""
@export var card_count: int = 1

## Pack-specific (for CARD_PACK type)
@export var pack_cards: Array[Dictionary] = []  # Array of {catalog_id: String, count: int}

## Pricing
@export var base_price: int = 10
@export var price_formula: String = "base"  # "base", "rarity", "power", "custom"
@export var discount_percent: int = 0  # 0-100

## Purchase limits
@export var purchase_limit_type: String = "none"  # "none", "per_refresh", "account"
@export var purchase_limit: int = 0  # 0 = unlimited
@export var purchases_made: int = 0  # Tracked at runtime

## Availability conditions
@export var required_battles: Array[String] = []  # Battle IDs that must be completed
@export var required_affinity: String = ""  # "fire", "ice", "nature", "" = any
@export var available_once: bool = false  # Disappears after purchase

## Calculated price
func get_price() -> int:
	var price: int = base_price

	# Apply discount
	if discount_percent > 0:
		price = int(price * (100 - discount_percent) / 100.0)

	return max(1, price)  # Minimum price of 1

## Check if can be purchased
func can_purchase(player_gold: int, purchases_count: int = 0) -> bool:
	# Check price
	if player_gold < get_price():
		return false

	# Check purchase limits
	if purchase_limit_type != "none" and purchase_limit > 0:
		if purchases_count >= purchase_limit:
			return false

	# Available
	return true

## Get remaining stock
func get_remaining_stock() -> int:
	if purchase_limit_type == "none":
		return -1  # Unlimited

	return max(0, purchase_limit - purchases_made)
