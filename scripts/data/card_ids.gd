class_name CardIDs

## Card ID Constants - Type-Safe Card References
##
## Use these constants instead of string literals when referencing cards in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   var card = CardCatalog.create_card_resource(CardIDs.FIREBALL)
##   var test_deck = [CardIDs.FIRE_ELEMENTAL, CardIDs.EARTH_SPRITE, CardIDs.PUFF]
##
## When adding new cards:
##   1. Add card to CardCatalog._init_catalog()
##   2. Add corresponding constant here (const CARD_NAME: StringName = &"card_id")
##   3. Validation in CardCatalog._ready() will catch any mismatches
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# SPELLS
# ============================================================================

const FIREBALL: StringName = &"fireball"
const RALLY: StringName = &"rally"
const GUARD: StringName = &"guard"
const CHARGE: StringName = &"charge"
const MANA_BOLT: StringName = &"mana_bolt"

# ============================================================================
# FIRE ELEMENT UNITS
# ============================================================================

const FIRE_ELEMENTAL: StringName = &"fire_elemental"
const FIRE_TITAN: StringName = &"fire_titan"
const FIRE_ELEMENTAL_SWARM: StringName = &"fire_elemental_swarm"
const FIRE_ANT: StringName = &"fire_ant"
const FIRE_ANT_SWARM: StringName = &"fire_ant_swarm"

# ============================================================================
# EARTH ELEMENT UNITS
# ============================================================================

const EARTH_SPRITE: StringName = &"earth_sprite"
const ROCK: StringName = &"rock"  # Stationary test dummy

# ============================================================================
# WIND ELEMENT UNITS
# ============================================================================

const PUFF: StringName = &"puff"
const CLOUD_SWARM: StringName = &"cloud_swarm"

# ============================================================================
# WATER ELEMENT UNITS
# ============================================================================

const WATER_FROG: StringName = &"water_frog"
