class_name ContentBinding

## Content Binding Constants - Mirrors C# ContentBinding enum
##
## These values MUST match the C# enum in scripts/csharp/Data/Profile/ContentBinding.cs
##
## Used for ownership filtering of items, cards, and other content:
## - ACCOUNT_WIDE: Any summoner can access/use (cosmetics, premium items)
## - SUMMONER_BOUND: Only the bound summoner can access (progression rewards)
##
## Usage:
##   var binding: int = ContentBinding.ACCOUNT_WIDE
##   if card.get("binding", 0) == ContentBinding.SUMMONER_BOUND:
##       # This card is bound to a specific summoner

# ============================================================================
# BINDING TYPES (must match C# ContentBinding enum values)
# ============================================================================

## Content can be accessed by any summoner on the account
const ACCOUNT_WIDE: int = 0

## Content is bound to a specific summoner and only they can access it
const SUMMONER_BOUND: int = 1
