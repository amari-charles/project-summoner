class_name VFXIDs

## VFX ID Constants - Type-Safe VFX Effect References
##
## Use these constants instead of string literals when referencing VFX effects in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   VFXManager.play_effect(VFXIDs.FIREBALL_EXPLOSION, position)
##   if VFXManager.has_effect(VFXIDs.SPELL_FIZZLE):
##       VFXManager.play_effect(VFXIDs.SPELL_FIZZLE, pos)
##
## When adding new VFX:
##   1. Create .tres file in resources/vfx/ with effect_id matching the constant
##   2. Add constant here (const VFX_NAME: StringName = &"vfx_id")
##   3. Add to IMPLEMENTED array (or PLACEHOLDERS if .tres doesn't exist yet)
##   4. Validation in VFXManager._ready() will catch any mismatches
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# FIREBALL EFFECTS
# ============================================================================

## Fireball explosion on impact
const FIREBALL_EXPLOSION: StringName = &"fireball_explosion"

## Fireball trail while projectile is in flight
const FIREBALL_TRAIL: StringName = &"fireball_trail"

## Fireball spell cast effect (AOE indicator + impact)
const FIREBALL_SPELL: StringName = &"fireball_spell"

# ============================================================================
# LIGHTNING EFFECTS
# ============================================================================

## Lightning strike bolt from attacker to target (used by storm cloud, etc.)
const LIGHTNING_STRIKE: StringName = &"lightning_strike"

# ============================================================================
# SPELL COMMAND EFFECTS (used by card.gd for tactical commands)
# Note: These are placeholders - VFX definitions may not exist yet
# ============================================================================

## Effect when a spell fails to cast (fizzle)
const SPELL_FIZZLE: StringName = &"spell_fizzle"

## Rally command circle marker
const RALLY_CIRCLE: StringName = &"rally_circle"

## Guard command position marker
const GUARD_MARKER: StringName = &"guard_marker"

## Charge command target marker
const CHARGE_MARKER: StringName = &"charge_marker"

# ============================================================================
# UTILITY
# ============================================================================

## All implemented VFX IDs (have .tres definitions)
const IMPLEMENTED: Array[StringName] = [
	FIREBALL_EXPLOSION,
	FIREBALL_TRAIL,
	FIREBALL_SPELL,
	LIGHTNING_STRIKE,
]

## All placeholder VFX IDs (used in code but no .tres yet)
const PLACEHOLDERS: Array[StringName] = [
	SPELL_FIZZLE,
	RALLY_CIRCLE,
	GUARD_MARKER,
	CHARGE_MARKER,
]

## All VFX IDs (implemented + placeholders)
static func all_ids() -> Array[StringName]:
	var result: Array[StringName] = []
	result.append_array(IMPLEMENTED)
	result.append_array(PLACEHOLDERS)
	return result

## Check if a VFX ID is a known constant (implemented or placeholder)
## Accepts String or StringName
static func is_known(vfx_id: String) -> bool:
	var id: StringName = StringName(vfx_id)
	return id in IMPLEMENTED or id in PLACEHOLDERS

## Check if a VFX ID has an implemented .tres definition
## Accepts String or StringName
static func is_implemented(vfx_id: String) -> bool:
	return StringName(vfx_id) in IMPLEMENTED
