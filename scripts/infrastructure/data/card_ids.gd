class_name CardIDs

## Card ID Constants - Type-Safe Card References
##
## Use these constants instead of string literals when referencing cards in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   var card = CardCatalogApi.create_card(CardIDs.FIREBALL)
##   var test_deck = [CardIDs.FIRE_WISP, CardIDs.PEBBLOOM, CardIDs.PUFF]
##
## When adding new cards:
##   1. Add card definition in C# CardCatalog
##   2. Add corresponding constant here (const CARD_NAME: StringName = &"card_id")
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
# WISPS (Basic starter units for each element)
# ============================================================================

const FIRE_WISP: StringName = &"fire_wisp"
const WATER_WISP: StringName = &"water_wisp"
const WIND_WISP: StringName = &"wind_wisp"
const EARTH_WISP: StringName = &"earth_wisp"
const LIGHTNING_WISP: StringName = &"lightning_wisp"
const LIFE_WISP: StringName = &"life_wisp"
const DEATH_WISP: StringName = &"death_wisp"
const SHADOW_WISP: StringName = &"shadow_wisp"
const FIRE_WISP_SWARM: StringName = &"fire_wisp_swarm"

# ============================================================================
# FIRE ELEMENT UNITS
# ============================================================================

const FIRE_TITAN: StringName = &"fire_titan"
const FIRE_ANT: StringName = &"fire_ant"
const FIRE_ANT_SWARM: StringName = &"fire_ant_swarm"
const FIRE_BOAR: StringName = &"fire_boar"
const FIRE_WOLF: StringName = &"fire_wolf"
const FIRE_SPIDER: StringName = &"fire_spider"

# ============================================================================
# EARTH ELEMENT UNITS
# ============================================================================

const PEBBLOOM: StringName = &"pebbloom"
const EARTH_KOMODO_DRAGON: StringName = &"earth_komodo_dragon"
const ROCK: StringName = &"rock"  # Stationary test dummy
const STONE_APE: StringName = &"stone_ape"
const EARTH_ROCK_THROWER: StringName = &"earth_rock_thrower"

# ============================================================================
# WIND ELEMENT UNITS
# ============================================================================

const PUFF: StringName = &"puff"
const CLOUD_SWARM: StringName = &"cloud_swarm"

# ============================================================================
# WATER ELEMENT UNITS
# ============================================================================

const WATER_FROG: StringName = &"water_frog"
const MAMA_DUCK: StringName = &"mama_duck"
# NOTE: duckling is a unit spawned by mama_duck, not a playable card
