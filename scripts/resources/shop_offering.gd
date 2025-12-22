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
	SPECIAL,        # Special items (legacy - use COSMETIC/EMOTE instead)
	SUMMONER,       # Unlock a new summoner
	COSMETIC,       # Skins, card backs, UI themes
	EMOTE           # Battle emotes/reactions
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

## Summoner-specific (for SUMMONER type)
@export var summoner_id: String = ""  # References SummonerCatalog

## Cosmetic-specific (for COSMETIC type)
@export var cosmetic_type: String = ""  # "summoner_skin", "card_back", "ui_theme"
@export var cosmetic_id: String = ""

## Emote-specific (for EMOTE type)
@export var emote_id: String = ""

## Pricing
@export var currency_type: String = "gold"  # "gold", "gems" (future)
@export var base_price: int = 10
@export var price_formula: String = "base"  # "base", "rarity", "power", "custom"
@export var discount_percent: int = 0  # 0-100

## Purchase limits
@export var purchase_limit_type: String = "none"  # "none", "per_refresh", "account"
@export var purchase_limit: int = 0  # 0 = unlimited

## Availability conditions
@export var required_battles: Array[String] = []  # Battle IDs that must be completed
@export var required_affinity: String = ""  # "fire", "ice", "nature", "" = any
@export var available_once: bool = false  # Disappears after purchase

## Calculated price (context-aware for future affinity/event pricing)
func get_price(context: ShopPurchaseContext = null) -> int:
	var price: int = base_price

	# When we implement affinity/event pricing, callers should pass context
	# so price reflects discounts/bonuses.
	# if context and context.summoner_affinity == required_affinity:
	#     price = int(price * 0.9)  # 10% discount for matching affinity

	# Apply discount
	if discount_percent > 0:
		price = int(price * (100 - discount_percent) / 100.0)

	return max(1, price)  # Minimum price of 1

## Check if can be purchased (uses context for all validation data)
func can_purchase(context: ShopPurchaseContext) -> bool:
	# Check gold
	var price: int = get_price(context)
	if context.player_gold < price:
		return false

	# Check purchase limits
	if purchase_limit_type != "none" and purchase_limit > 0:
		if context.purchase_count >= purchase_limit:
			return false

	# Future: Check affinity discounts, event bonuses, etc.
	# All data available in context object

	return true

## Get remaining stock
func get_remaining_stock(context: ShopPurchaseContext) -> int:
	if purchase_limit_type == "none":
		return -1  # Unlimited

	return max(0, purchase_limit - context.purchase_count)
