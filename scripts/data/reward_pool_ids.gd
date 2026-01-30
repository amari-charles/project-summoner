class_name RewardPoolIDs

## Reward Pool ID Constants - Type-Safe Pool References
##
## Use these constants when referencing reward pools in battle configs.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   "reward_pool": RewardPoolIDs.FIRE_CARDS
##   "reward_pool": RewardPoolIDs.COMMON_SPELLS

# ============================================================================
# ELEMENT-BASED POOLS (Auto-populated from CardCatalog by element)
# ============================================================================

const FIRE_CARDS: StringName = &"fire_cards"
const WATER_CARDS: StringName = &"water_cards"
const WIND_CARDS: StringName = &"wind_cards"
const EARTH_CARDS: StringName = &"earth_cards"
const LIGHTNING_CARDS: StringName = &"lightning_cards"
const LIFE_CARDS: StringName = &"life_cards"
const DEATH_CARDS: StringName = &"death_cards"
const SHADOW_CARDS: StringName = &"shadow_cards"

# ============================================================================
# RARITY-BASED POOLS
# ============================================================================

const COMMON_CARDS: StringName = &"common_cards"
const RARE_CARDS: StringName = &"rare_cards"
const EPIC_CARDS: StringName = &"epic_cards"
const LEGENDARY_CARDS: StringName = &"legendary_cards"

# ============================================================================
# TYPE-BASED POOLS
# ============================================================================

const ALL_SPELLS: StringName = &"all_spells"
const ALL_UNITS: StringName = &"all_units"

# ============================================================================
# CUSTOM POOLS (Manually defined card lists)
# ============================================================================

const STARTER_REWARDS: StringName = &"starter_rewards"
const TUTORIAL_REWARDS: StringName = &"tutorial_rewards"
const BOSS_LOOT: StringName = &"boss_loot"

# ============================================================================
# SPECIAL POOLS
# ============================================================================

const ALL_CARDS: StringName = &"all_cards"
