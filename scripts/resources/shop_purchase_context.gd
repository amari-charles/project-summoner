class_name ShopPurchaseContext
extends RefCounted

## Context object for shop purchase validation
##
## Provides all data needed to validate a purchase and calculate prices
## Future-proof: Add new fields without breaking existing code

var player_gold: int = 0
var player_gems: int = 0  # Premium currency for gems purchases
var purchase_count: int = 0
var summoner_affinity: String = ""  # For future affinity discounts
var active_bonuses: Array[String] = []  # For future event bonuses ("fire_week", etc.)
var refresh_epoch: int = 0  # For per-refresh limits
