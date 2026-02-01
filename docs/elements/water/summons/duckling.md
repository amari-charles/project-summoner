# Duckling

**Element:** Water
**Status:** Placeholder - needs real art

## Overview

A baby duck that follows mama into battle. Ducklings are fragile ranged attackers that coordinate their fire with mama's target. They don't use normal targeting—they always attack whatever mama is fighting.

## Visual Description

**Current:** Small yellow oval/sphere mesh
**Target:** Fluffy yellow duckling. Big eyes, small body. Spits water bullets at mama's enemies.

## Gameplay Role

- **Type:** Ranged
- **Rarity:** N/A (spawned by Mama Duck card, not a standalone card)
- **Archetype:** Coordinated Attacker
- Low HP, moderate damage, fast attack speed
- Uses `DucklingUnit3D` script which overrides target acquisition
- Always targets mama's current target
- Falls back to normal targeting if mama dies or has no target

## Technical Notes

Implemented via `DucklingUnit3D.cs` which extends `RangedUnit3D` and overrides `AcquireTarget()` to use the mama's target.

---

*See also: [Mama Duck](mama-duck.md) | [Water Element](../overview.md)*
