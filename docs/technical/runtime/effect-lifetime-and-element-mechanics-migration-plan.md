# Effect Lifetime And Element Mechanics Migration Plan

**Status:** PASS 3 IMPLEMENTATION  
**Initiative:** `effect-lifetime-and-element-mechanics-migration`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-19`  
**Owner:** `Codex + Gameplay Engineering`

## Summary

This initiative replaces sentinel lifetime semantics (`duration = -1`, `0 means permanent`) with explicit typed effect lifetime modeling in simulation effect domains. It also introduces formal mechanics support needed by upcoming Wind and Earth content: attack speed modifier effects, flat damage reduction effects, and spell area shape typing (circle/square). Persistent bonuses are ability-granted through the buff pipeline, not hidden config magic. Placeholder names remain descriptive until a naming pass.

## Goals

1. Eliminate sentinel duration semantics from combat effect carriers with a typed model.
2. Add runtime support for `AttackSpeedModifier`, `FlatDamageReduction`, and `SpellAreaShape`.
3. Route persistent bonuses through formal ability/buff application paths.
4. Ship deterministic implementation with reusable architecture nodes.

## Non-Goals

1. Final gameplay tuning for all new Wind/Earth cards.
2. Full PASS 3 mechanic balancing and animation polish.
3. Lore/final naming of new cards or units.

## Architecture Decisions

1. Effect lifetimes use explicit `EffectLifetimeKind` + `EffectLifetime` instead of magic numeric values.
2. `Evasion` remains a stat; `FlatDamageReduction` is a dedicated effect/mechanic.
3. Persistent bonuses are ability-granted (`ApplySelfEffect`) and represented as persistent buffs.
4. Spell area is typed (`Circle`, `Square`) and carried from authoring payload to simulation/runtime payload.
5. Cross-cutting runtime concerns are centralized into dedicated simulation nodes:
1. `EffectLifetimeResolver`
2. `SpellAreaResolver`
3. `EffectStatResolver`

## Public API / Interface / Type Changes

1. Add `EffectLifetimeKind` and `EffectLifetime` in simulation enums/types.
2. Add lifetime fields on `ActiveBuff`, `TriggerConfig`, `DelayedEffect`, `SimSpellEffect`, and authoring `SpellEffectDefinition`.
3. Add `SpellAreaShape` on spell effect payloads.
4. Add `EffectType.EvasionModifier`, `EffectType.AttackSpeedModifier`, and `EffectType.FlatDamageReduction`.
5. Add `UnitAbilityKind.ApplySelfEffect` plus supporting ability payload fields.
6. Add reusable effect resolver nodes under `scripts/csharp/Battle/Simulation/Effects/`.

## Legacy Removal Scope

1. Remove direct dependency on `Duration == -1` for permanent buff behavior.
2. Remove implicit `Duration > 0` logic as the only timed lifetime signal.
3. Replace shield/trigger permanence sentinel usage with typed lifetime values.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Validation matrix defines explicit scenarios and test mapping IDs.
2. Core architecture decisions and defaults are locked.

### PASS 2: STUBS + WIRING

1. New types and enum values compile and are wired end-to-end.
2. Legacy behavior stays stable through compatibility bridges.
3. Stub checklist maps wiring and test skeletons against case IDs.

### PASS 3: IMPLEMENTATION + TESTS

1. Typed lifetime behavior fully replaces sentinel logic in effect domains.
2. New mechanics (`AttackSpeedModifier`, `FlatDamageReduction`, square spell area) are implemented and tested.
3. Wind/Earth authored content behavior is implemented and covered.

### PR REVIEW: READY

1. Required artifacts exist and pass order is preserved with approval gate evidence.
2. Validation cases are marked `Implemented` or `Deferred` with rationale.

## Open Risks

1. Partial migration could leave mixed sentinel/lifetime logic in edge paths.
2. Attack cadence changes from attack speed modifier wiring may regress determinism.
3. Delayed spell effects with typed area/lifetime may diverge from immediate cast behavior.

## Assumptions and Defaults

1. Migration scope includes all combat effect lifetime semantics now, not unrelated timers (AI, movement, projectile flight lifetime).
2. Placeholder naming is descriptive-only until naming pass.
3. Wind/Earth content is in scope; Fire remains follow-up.
4. PASS 2 prioritizes compile-safe wiring and deterministic no-op stubs where behavior is deferred to PASS 3.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` - completed
2. `PASS 2: STUBS + WIRING` - completed
3. `PASS 3: IMPLEMENTATION + TESTS` - completed
4. `PR REVIEW: READY` - pending

Gate note:
1. Use explicit approval text to advance.
2. If waiting, state: `blocked waiting approval`.
