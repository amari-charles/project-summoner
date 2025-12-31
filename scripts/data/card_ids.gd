class_name CardIDs

## Card ID Constants - Type-Safe Card References
##
## Use these constants instead of string literals when referencing cards in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   var card = CardCatalog.create_card_resource(CardIDs.FIREBALL)
##   var test_deck = [CardIDs.NEADE, CardIDs.FIRE_RECRUIT, CardIDs.EMBER_SLINGER]
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

# ============================================================================
# SPECIAL UNITS
# ============================================================================

const NEADE: StringName = &"neade"
const DEMON_IMP: StringName = &"demon_imp"

# ============================================================================
# SLIMES (By Size and Color)
# ============================================================================

# Small slimes (fast, fragile)
const SLIME_GREEN: StringName = &"slime_green"
const SLIME_PINK: StringName = &"slime_pink"
const SLIME_VIOLET: StringName = &"slime_violet"

# Medium slimes (balanced)
const SLIME_BLUE: StringName = &"slime_blue"
const SLIME_ORANGE: StringName = &"slime_orange"
const SLIME_YELLOW: StringName = &"slime_yellow"

# Large slimes (tanky, slow)
const SLIME_GREY: StringName = &"slime_grey"
const SLIME_PURPLE: StringName = &"slime_purple"
const SLIME_RED: StringName = &"slime_red"

# ============================================================================
# FIRE ELEMENT UNITS
# ============================================================================

const FIRE_RECRUIT: StringName = &"fire_recruit"
const FIRE_ELEMENTAL: StringName = &"fire_elemental"
const FIRE_TITAN: StringName = &"fire_titan"
const FIRE_ELEMENTAL_SWARM: StringName = &"fire_elemental_swarm"
const EMBER_SLINGER: StringName = &"ember_slinger"
const BLAZE_RIDER: StringName = &"blaze_rider"
const ASH_VANGUARD: StringName = &"ash_vanguard"
const EMBER_GUARD: StringName = &"ember_guard"

# ============================================================================
# EARTH ELEMENT UNITS
# ============================================================================

const EARTH_SPRITE: StringName = &"earth_sprite"

# ============================================================================
# WIND ELEMENT UNITS
# ============================================================================

const PUFF: StringName = &"puff"
