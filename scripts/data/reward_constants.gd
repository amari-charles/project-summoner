class_name RewardConstants

## Reward Constants - Type-Safe Reward Pool References
##
## This file mirrors C# enums for GDScript type safety.
## Keep in sync with scripts/csharp/Services/Rewards/RewardPoolId.cs
##
## Usage:
##   "reward_pool": RewardConstants.PoolId.FIRE_COMMON_UNITS

## Mirror of C# RewardPoolId enum
## MUST match values in scripts/csharp/Services/Rewards/RewardPoolId.cs
enum PoolId {
	# Curated pools (explicit card lists)
	TUTORIAL_REWARDS = 0,
	STARTER_REWARDS = 1,
	BOSS_LOOT = 2,

	# Filter-based pools (element + rarity + type)
	FIRE_COMMON_UNITS = 3,
	WATER_COMMON_UNITS = 4,
	WIND_COMMON_UNITS = 5,
	EARTH_COMMON_UNITS = 6,
	ALL_SPELLS = 7,
	ALL_COMMON = 8,
	ALL_RARE = 9,

	# Composite pools (unions)
	ELEMENTAL_STARTERS = 10,
}

## Mirror of C# Element enum for inline filters
## MUST match values in scripts/csharp/Cards/Element.cs
enum Element {
	NEUTRAL = 0,
	FIRE = 1,
	WATER = 2,
	WIND = 3,
	EARTH = 4,
	LIGHTNING = 5,
	SHADOW = 6,
	POISON = 7,
	LIFE = 8,
	DEATH = 9,
	OCCULTIST = 10,
	HOLY = 11,
	ICE = 12,
	METAL = 13,
	SPIRIT = 14,
}

## Mirror of C# Rarity enum for inline filters
## MUST match values in scripts/csharp/Cards/Rarity.cs
enum Rarity {
	COMMON = 0,
	RARE = 1,
	EPIC = 2,
	LEGENDARY = 3,
}

## Mirror of C# CardType enum for inline filters
## MUST match values in scripts/csharp/Cards/CardType.cs
enum CardType {
	SUMMON = 0,
	SPELL = 1,
}
