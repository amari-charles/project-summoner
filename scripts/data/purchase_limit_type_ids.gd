class_name PurchaseLimitTypeIds

## Purchase Limit Type ID Constants - Type-Safe Shop Purchase Limit References
##
## Mirrors C# PurchaseLimitType enum in scripts/csharp/Services/Shop/PurchaseLimitType.cs
## Keep these in sync when adding new purchase limit types.
##
## Use these constants instead of string literals when configuring shop offerings.
##
## Usage:
##   offering.purchase_limit_type = PurchaseLimitTypeIds.ACCOUNT
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# PURCHASE LIMIT TYPES
# ============================================================================

## No purchase limit - unlimited purchases allowed
const NONE: StringName = &"none"

## Limit resets when shop refreshes
const PER_REFRESH: StringName = &"per_refresh"

## Account-wide permanent limit (one-time purchase)
const ACCOUNT: StringName = &"account"

# ============================================================================
# UTILITY
# ============================================================================

## All purchase limit types
const ALL_TYPES: Array[StringName] = [NONE, PER_REFRESH, ACCOUNT]

## Default purchase limit type
const DEFAULT: StringName = NONE

## Check if a purchase limit type string is valid
static func is_valid(limit_type: String) -> bool:
	return StringName(limit_type) in ALL_TYPES

## Check if the limit type means unlimited purchases
static func is_unlimited(limit_type: StringName) -> bool:
	return limit_type == NONE
