# Combat Primitives V2 Plan

**Status:** Implementation planned  
**Initiative:** `combat-primitives-v2`  
**Domain:** `runtime`  
**Last Updated:** 2026-05-28  
**Owner:** Codex + Gameplay Engineering

## Summary

Combat Primitives V2 replaces content-specific unit ability dispatch with composable simulation-owned primitives. Unit abilities and spell effects should describe when they run, who they target, how they deliver effects, and what gameplay mutation they apply. The simulation remains authoritative; view-side ability nodes are not revived.

## Goals

1. Replace `UnitAbilityKind` custom behavior branches with stable primitive specs.
2. Keep gameplay mutations centralized in simulation systems, especially `SimEffects`.
3. Re-author existing healer, taunt, cleanse, passive self-effect, and knockback abilities through primitives.
4. Add the primitives needed for the planned Fire, Water, Earth, and Wind unit/spell rosters.
5. Keep placeholder visuals shape-based until a final art pass.

## Primitive Model

Abilities are composed from:

1. **Trigger** - when the ability runs: on spawn, periodic, on hit, on damaged, on death, or on buff removed.
2. **Targeting** - who receives the effect: self, hit target, current target, allies/enemies in radius, lowest-HP ally, health redistribution pool, or cast area.
3. **Delivery** - how the effect is delivered: instant, projectile, pulse, aura, delayed, or repeated area.
4. **Effect** - what changes: damage, heal, shield, stat modifier, status apply/consume, transfer health, revive-on-death, displacement, root, accuracy modifier, or ranged damage modifier.

Switch dispatch is acceptable only at this primitive layer. It should not grow branches for individual units or named content.

## Content Scope

This initiative implements Fire, Water, Earth, and Wind planned rosters from the working notes in `docs/design/*-content-working-notes.md`.

Neutral content, course loot pools, and difficulty structure are explicitly deferred.

## Validation

1. Existing ability behavior must pass after migration.
2. Existing spell behavior for Cleanse, Water Jet, Rain Field, Tail Wind, and Fortify must remain stable.
3. New tests must cover aura/pulse, status apply/consume, transfer health, death effects, shield break or expire hooks, revive-on-death, center displacement, accuracy modifier, ranged damage modifier, and line/cone spell selection.
4. Content tests must prove new cards resolve to valid units/spells, valid scenes, and correct element affinity.

## Commit Plan

1. Save current content-planning state before architecture changes.
2. Commit this plan separately.
3. Commit primitive specs and migration tests.
4. Commit migrated existing abilities.
5. Commit spell/status primitives.
6. Commit Fire/Water content.
7. Commit Earth/Wind content.
