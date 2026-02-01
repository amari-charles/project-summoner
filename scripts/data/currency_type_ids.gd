class_name CurrencyTypeIds

## Currency Type ID Constants - Type-Safe Currency References
##
## Mirrors C# CurrencyType enum in scripts/csharp/Services/Shop/CurrencyType.cs
## Keep these in sync when adding new currency types.
##
## Use these constants instead of string literals when configuring shop offerings.
##
## Usage:
##   offering.currency_type = CurrencyTypeIds.GOLD
##   if currency == CurrencyTypeIds.GEMS:
##       _show_premium_ui()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# CURRENCY TYPES
# ============================================================================

## In-game gold earned from battles
const GOLD: StringName = &"gold"

## Premium currency (purchasable with real money)
const GEMS: StringName = &"gems"

## Real-money purchases via platform billing
const REAL_MONEY: StringName = &"real_money"

# ============================================================================
# UTILITY
# ============================================================================

## All currency types
const ALL_TYPES: Array[StringName] = [GOLD, GEMS, REAL_MONEY]

## Default currency type
const DEFAULT: StringName = GOLD

## Check if a currency type string is valid
static func is_valid(currency: String) -> bool:
	return StringName(currency) in ALL_TYPES

## Check if the currency is premium (gems or real money)
static func is_premium(currency: StringName) -> bool:
	return currency in [GEMS, REAL_MONEY]
