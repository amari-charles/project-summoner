class_name GroupIDs

## Group ID Constants - Type-Safe Group Name References
##
## Use these constants instead of string literals when adding/checking groups.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   add_to_group(GroupIDs.UNITS)
##   get_tree().get_nodes_in_group(GroupIDs.PLAYER_UNITS)
##   var target_group = GroupIDs.enemy_units_for(team)
##
## Note: StringName (&"text") is faster than String ("text") for group operations

# ============================================================================
# UNIT GROUPS
# ============================================================================

## All units (both teams)
const UNITS: StringName = &"units"

## Player team units only
const PLAYER_UNITS: StringName = &"player_units"

## Enemy team units only
const ENEMY_UNITS: StringName = &"enemy_units"

# ============================================================================
# BASE/STRUCTURE GROUPS
# ============================================================================

## All bases (both teams)
const BASES: StringName = &"bases"

## Player team bases only
const PLAYER_BASES: StringName = &"player_bases"

## Enemy team bases only
const ENEMY_BASES: StringName = &"enemy_bases"

# ============================================================================
# CHARACTER GROUPS
# ============================================================================

## All summoners/heroes (both teams)
const SUMMONERS: StringName = &"summoners"

## Player summoners only
const PLAYER_SUMMONERS: StringName = &"player_summoners"

## Enemy summoners only
const ENEMY_SUMMONERS: StringName = &"enemy_summoners"

# ============================================================================
# PROJECTILE GROUPS
# ============================================================================

## All projectiles
const PROJECTILES: StringName = &"projectiles"

# ============================================================================
# STRUCTURE GROUPS
# ============================================================================

## All structures (towers, walls, etc.)
const STRUCTURES: StringName = &"structures"

# ============================================================================
# UI GROUPS
# ============================================================================

## Hand UI elements
const HAND_UI: StringName = &"hand_ui"

## Game UI layer
const GAME_UI: StringName = &"game_ui"

## Game controller references
const GAME_CONTROLLER: StringName = &"game_controller"

# ============================================================================
# UTILITY METHODS
# ============================================================================

## Get the enemy units group for a given team
## Usage: GroupIDs.enemy_units_for(Unit3D.Team.PLAYER) -> &"enemy_units"
static func enemy_units_for(team: int) -> StringName:
	# Team.PLAYER = 0, Team.ENEMY = 1
	return ENEMY_UNITS if team == 0 else PLAYER_UNITS

## Get the ally units group for a given team
## Usage: GroupIDs.ally_units_for(Unit3D.Team.PLAYER) -> &"player_units"
static func ally_units_for(team: int) -> StringName:
	# Team.PLAYER = 0, Team.ENEMY = 1
	return PLAYER_UNITS if team == 0 else ENEMY_UNITS

## Get the enemy bases group for a given team
static func enemy_bases_for(team: int) -> StringName:
	return ENEMY_BASES if team == 0 else PLAYER_BASES

## Get the ally bases group for a given team
static func ally_bases_for(team: int) -> StringName:
	return PLAYER_BASES if team == 0 else ENEMY_BASES
