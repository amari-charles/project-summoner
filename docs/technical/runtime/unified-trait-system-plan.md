# Unified Trait System Plan (No Backward Compatibility)

**Status:** PASS 1 COMPLETE (Design + Validation Plan)  
**Last Updated:** 2026-03-08  
**Owner:** Gameplay Systems

## 1. Purpose

Replace all existing trait implementations (summoner traits, card traits, item-driven trait modifiers) with one unified schema and one deterministic runtime engine.

This is a full replacement, not an incremental compatibility migration.

## 2. Hard Constraints

1. No backward compatibility is required.
2. Existing save/profile trait shape can be reset.
3. Determinism is mandatory in simulation and multiplayer.
4. Percent stacking defaults to multiplicative product.
5. Time-window effects default to global battle-time evaluation.
6. Trigger scope defaults to per-unit, with explicit override support.
7. Condition matching must use a typed predicate DSL, not script expressions.

## 2.1 Locked Policy Defaults (Pass 1 Addendum)

These defaults are now fixed unless explicitly changed in a later approved pass.

1. Time-window boundaries:
- Start boundary is inclusive.
- End boundary is exclusive.
- Example: `first 5s` means `0.0 <= battle_time < 5.0`.
- Boundary payout effects (refund/award at cutoff) resolve once at the first simulation tick where `battle_time >= end`.

2. Initial stat clamps (runtime safety rails):
- `move_speed`: min `0.10`, max `10.00`
- `cast_speed`: min `0.10`, max `5.00`
- `attack_speed`: min `0.10`, max `10.00`
- `damage_reduction_flat`: min `0.00`, max `1000.00`
- `crit_chance`: min `0.00`, max `1.00`
- `lifesteal`: min `0.00`, max `1.00`
- If a stat is not listed, default policy is no hard clamp in Pass 2 stubs and explicit clamp definition in Pass 3 implementation.

3. Aura cadence and evaluation:
- Aura membership is evaluated every simulation tick (`FixedDeltaSeconds` cadence).
- Aura entry/exit takes effect on the same tick evaluation pass.
- Deterministic target ordering is required before truncation or tie resolution.

## 3. Three-Pass Delivery Model (Approval-Gated)

## Pass 1: Use Cases + Validation Design

Scope:
1. Author plan and validation docs.
2. Lock data model and runtime flow.
3. Lock API contracts and deletion scope.
4. Define acceptance checklists for Pass 2 and Pass 3.

Output:
1. `unified-trait-system-plan.md` (this file)
2. `unified-trait-system-validation-cases.md`

Gate:
1. Stop and request explicit approval before Pass 2.

## Pass 2: Scaffolding/Stubs + Legacy Removal

Scope:
1. Remove legacy trait systems and conflicting code paths.
2. Add new namespaces, core types, interfaces, and runtime state stubs.
3. Wire battle/session/simulation to new entry points.
4. Keep deterministic compile-safe stub behavior.
5. Add test skeletons for all validation categories.

Gate:
1. Stop and request explicit approval before Pass 3.

## Pass 3: Full Implementation

Scope:
1. Implement progression point ledgers and point grants.
2. Implement pool rolling and spend flows.
3. Implement predicate evaluator.
4. Implement runtime compiler and simulation integration.
5. Implement trigger engine (per-unit default + scope overrides).
6. Complete tests and scenario matrix verification.
7. Final cleanup of dead code and outdated docs.

## 4. Target Architecture

## 4.1 Unified Authoring Schema

Core entities:
1. `TraitDefinition`
2. `TraitEffectDefinition`
3. `TraitPoolDefinition`
4. `TraitPredicate` (typed DSL)
5. `TraitPointLedger`

Owner model:
1. Summoner-owned traits
2. Card-owned traits
3. Item-provided traits/effects
4. Reward/event-granted traits

## 4.2 Runtime Model

1. Compile all relevant trait data at battle setup into `MatchTraitRuntimeState`.
2. Store compiled state in `MatchState`.
3. Apply summoner effects at summoner registration.
4. Apply unit-targeted effects at unit spawn and trigger execution points.
5. Evaluate time windows against global match time by default.
6. Track trigger state deterministically.

## 4.3 Deterministic Execution Rules

1. No runtime calls to non-deterministic services during simulation ticks.
2. Trait offer roll and trigger order are deterministic and stable.
3. Predicate evaluation order is deterministic.
4. Floating-point operations follow consistent stat application order.

## 5. API Contract Changes (Target State)

## 5.1 Summoner Progression API

Required operations:
1. `GetUnspentTraitPoints(summonerId)`
2. `RollTraitOffers(summonerId, count)`
3. `SpendTraitPoint(summonerId, traitId)`
4. `GrantTraitPoints(summonerId, amount, source)`

Behavior:
1. Level-up grants trait points.
2. Spending can be deferred.
3. Offers are rolled at spend time.

## 5.2 Card Progression API

Required operations:
1. `GetCardUnspentTraitPoints(cardInstanceId)`
2. `RollCardTraitOffers(cardInstanceId, count)`
3. `SpendCardTraitPoint(cardInstanceId, traitId)`
4. `GrantCardTraitPoints(cardInstanceId, amount, source)`

Behavior:
1. Cards keep independent trait points.
2. Card level-ups can grant card trait points.
3. Reward/events can grant card trait points.

## 5.3 Battle/Runtime Interfaces

Required runtime changes:
1. `MatchState` stores compiled trait runtime state.
2. Deck/hand/discard paths carry card instance identity (not only catalog id).
3. Spawn and trigger hooks consume compiled trait state, not legacy catalogs.

## 6. Legacy Deletion Scope

Remove in Pass 2+Pass 3:
1. Old split trait catalogs/services where behavior overlaps with unified system.
2. Progression APIs requiring immediate trait selection on level-up.
3. Compatibility adapters and migration shims.
4. Obsolete runtime trait provider paths that conflict with unified engine.
5. Outdated docs that describe removed architecture as current behavior.

## 7. Acceptance Checklist: Pass 2

Pass 2 is accepted when all are true:
1. Legacy trait execution paths are removed or disconnected.
2. Unified core namespaces/types/interfaces exist and compile.
3. Battle/session/simulation call chain points to new stub entry points.
4. No hidden fallback path to old trait behavior remains.
5. Test skeletons exist for all categories in validation doc.
6. Build compiles successfully.

## 8. Acceptance Checklist: Pass 3

Pass 3 is accepted when all are true:
1. All required APIs are implemented and wired.
2. Validation matrix scenarios pass (or are explicitly documented as deferred).
3. Multiplayer determinism checks pass for trait-driven scenarios.
4. Card-instance identity works end-to-end in battle runtime.
5. Legacy/obsolete docs are either updated or moved to archive.
6. Final architecture doc matches actual implementation.

## 9. Future Accommodation List (Design Pressure Tests)

These are not required for V1 implementation, but the design should accommodate them without structural rewrite:
1. Per-stat stacking policy override (instead of global multiplicative default).
2. Trait respec tokens and rollback-safe spend history.
3. Multi-offer persistence modes (roll-now vs lock-at-point-grant).
4. Trait quality tiers/rarities and dynamic weighting by season.
5. Cross-entity conditions (`if summoner has X and card has Y`).
6. Mode-specific pool overlays (campaign/arena/event ladders).
7. Server-authoritative remote trait content loading.
8. Telemetry hooks for offer rates and spend decisions.
9. AI policy integration for deterministic auto-spend.
10. Live balance patch toggles by trait version key.

## 10. Pass Control Rule

Execution policy for this overhaul:
1. Complete one pass.
2. Report outcomes and unresolved items.
3. Wait for explicit approval before starting next pass.
