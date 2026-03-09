# Unified Trait System Validation Cases

**Status:** PASS 1 BASELINE  
**Last Updated:** 2026-03-08  
**Companion Doc:** `unified-trait-system-plan.md`

## 1. How To Use This Document

This matrix is used in two ways:
1. Pass 1 design validation: verify architecture can express each case.
2. Pass 3 implementation validation: verify behavior with tests.

Case status tags:
1. `Design-Covered`: schema/runtime can represent this case.
2. `Implemented`: code path exists and passes tests.
3. `Deferred`: intentionally out of current pass scope.

## 2. Baseline Case Matrix (Required)

| ID | Case | Category | Expected Result | Status |
|---|---|---|---|---|
| C01 | Summoner trait point grant on level-up | Progression | Summoner unspent points increment deterministically | Design-Covered |
| C02 | Card trait point grant on card level-up | Progression | Target card's unspent points increment only for that card | Design-Covered |
| C03 | Trait point grant from reward/mission/condition | Progression | Configured target entity receives point grant | Design-Covered |
| C04 | Deferred spend with multiple queued points | Progression | Multiple unspent points persist and remain spendable | Design-Covered |
| C05 | Offer roll at spend time | Offer Engine | Offers generated from current state when point is spent | Design-Covered |
| C06 | Weighted pool behavior | Offer Engine | Offer frequencies follow configured weights deterministically by seed | Design-Covered |
| C07 | Guaranteed slot behavior | Offer Engine | Guaranteed entries always appear when eligibility allows | Design-Covered |
| C08 | Prerequisite/exclusion enforcement | Offer Engine | Ineligible traits never offered or spendable | Design-Covered |
| C09 | Duplicate prevention and uniqueness | Offer Engine | Duplicate trait options and illegal re-acquisition blocked | Design-Covered |
| C10 | Spend failure paths | Progression | Invalid spend fails safely with deterministic reason | Design-Covered |
| C11 | Summoner stat effects at registration | Runtime Application | Summoner combat stats reflect compiled effects | Design-Covered |
| C12 | Summoner-owned unit stat effects on spawn | Runtime Application | Spawned units receive applicable summoner effects | Design-Covered |
| C13 | Card-owned unit stat effects on spawn | Runtime Application | Spawned units receive applicable card-instance effects | Design-Covered |
| C14 | Time-window effects use global battle timer | Runtime Timing | Activation reflects battle time condition | Design-Covered |
| C15 | First 15s rule for late spawn at 20s | Runtime Timing | Late spawn receives no first-15s bonus | Design-Covered |
| C16 | Triggered effects default per-unit scope | Trigger Engine | Each unit tracks its own trigger state | Design-Covered |
| C17 | Triggered effects per-team override | Trigger Engine | Team-shared trigger state applies when configured | Design-Covered |
| C18 | Triggered effects per-card-cast override | Trigger Engine | Cast-scoped trigger state applies when configured | Design-Covered |
| C19 | Trigger cooldown behavior across scopes | Trigger Engine | Cooldowns apply according to declared scope | Design-Covered |
| C20 | Typed predicate matching (`All/Any/Not`) | Predicate DSL | Conditions evaluate correctly and deterministically | Design-Covered |
| C21 | Host/client deterministic parity | Determinism | Same seed/input -> same trait outcomes and events | Design-Covered |
| C22 | Snapshot/protocol with card-instance identity | Multiplayer Data | Card instance identity preserved through runtime/network path | Design-Covered |
| C23 | Invalid config deterministic handling | Reliability | Invalid content is skipped/fails by deterministic policy | Design-Covered |
| C24 | Authoring validation error surfacing | Tooling/Content | Clear validation errors for bad trait/pool definitions | Design-Covered |

## 3. Future Accommodation Cases (Pressure Tests)

These are not required for Pass 3 acceptance, but architecture should remain open for them.

| ID | Case | Why It Matters | Status |
|---|---|---|---|
| F01 | Per-stat stacking mode override | Balance flexibility | Design-Covered |
| F02 | Trait respec and refund ledger | Economy and UX | Design-Covered |
| F03 | Locked offers at point grant mode | Alternative progression style | Design-Covered |
| F04 | Seasonal pool overlays | Live-ops content rotation | Design-Covered |
| F05 | Composite cross-owner conditions | Advanced build interactions | Design-Covered |
| F06 | AI deterministic auto-spend policies | PvE automation and bots | Design-Covered |
| F07 | Server-authored trait packs | Externalized content control | Design-Covered |
| F08 | Telemetry for pick rates and offer entropy | Balance iteration | Design-Covered |
| F09 | Versioned trait definitions | Hotfix safety and auditability | Design-Covered |
| F10 | Rule-pack switching by game mode | Campaign vs arena separation | Design-Covered |

## 4. Determinism Validation Requirements

Each runtime-affecting case must define:
1. Seed and initial state snapshot.
2. Input command stream.
3. Expected event/state hash at checkpoints.
4. Failure output with diff details.

Minimum checkpoints:
1. Post-battle initialization.
2. First trait-trigger activation.
3. Mid-battle random offer/trigger heavy frame.
4. End-of-match summary hash.

## 5. Test Mapping Template (for Pass 2/Pass 3)

Use this template per case:

```text
Case ID:
Test Type: unit | integration | simulation | multiplayer
Test File:
Fixture:
Seed:
Inputs:
Assertions:
Determinism Check:
Notes:
```

## 6. Pass Exit Criteria Mapping

## Pass 2
1. Each baseline case has a planned test file placeholder.
2. Case IDs are referenced by test skeleton names/comments.

## Pass 3
1. Each baseline case marked `Implemented` or `Deferred`.
2. Any `Deferred` case must include explicit rationale and target pass.
3. Determinism cases (C21, C22) must have replay/hash checks.

## 7. Candidate Gameplay Authoring Cases (From Design Discussion)

These cases are concrete trait definitions we should be able to author with the unified system.
They are intended as additional validation pressure tests and implementation examples.

Determinism note for selection-based cases:
1. `nearest N` must use stable tie-breaks: `distance`, then `unit_id` ascending.
2. Radius queries must use deterministic ordering before truncation.

| ID | Owner | Target | Selection | Effect | Timing | Scope | Duration/Cooldown | Stacking |
|---|---|---|---|---|---|---|---|---|
| G01 | Summoner trait | Spawned units | All owned spawned units | `move_speed` x1.10 | Always | Per-unit | Permanent | Multiplicative |
| G02 | Summoner trait | Summoner | Self | `cast_speed` x1.15 (faster cast time) | Always | Per-team | Permanent | Multiplicative |
| G03 | Summoner trait | Spawned units and nearby allies | Self unit + nearest 3 allies | Apply `attack_speed` buff x1.20 | Triggered `OnHit` | Per-unit | 4s / 8s | Multiplicative |
| G04 | Summoner trait | Allies | Allies within radius 3 | Apply `damage_reduction` +5 | Triggered `OnTakeHit` | Per-unit | 3s / 10s | Add then multiply phase rules |
| G05 | Card trait | Spawned units | Units spawned by this card cast | `attack_damage` x1.25 | First 15s of battle | Per-card-cast | Window-gated | Multiplicative |
| G06 | Card trait | Spawned units | Units spawned by this card cast | `max_hp` x1.18 | Always | Per-card-cast | Permanent | Multiplicative |
| G07 | Summoner trait | Allies | Nearest 2 allies | Grant shield +80 | Triggered `OnKill` | Per-unit | Instant / 12s | N/A |
| G08 | Summoner trait | Enemies | Enemies within radius 3 | Apply slow x0.85 move speed | Triggered `OnHit` | Per-unit | 2s / 6s | Multiplicative |
| G09 | Summoner trait | Allies | All allies in radius 4 around source | `crit_chance` +0.10 | Always (aura) | Per-unit | Active while in radius | Additive |
| G10 | Item trait | Summoner | Self | `max_mana` x1.20 | Always | Per-team | Permanent | Multiplicative |
| G11 | Item trait | Spawned units | All owned spawned units with tag `beast` | `attack_speed` x1.08 | Always | Per-unit | Permanent | Multiplicative |
| G12 | Summoner trait | Spawned units | Lowest HP ally in radius 3 | Heal +40 | Triggered `Periodic` | Per-unit | every 5s | N/A |
| G13 | Summoner trait | Spawned units | Self unit only | Berserk: `attack_damage` x1.35 when HP < 40% | Condition (`HpBelow`) | Per-unit | while condition true | Multiplicative |
| G14 | Card trait | Spawned units | Nearest 3 allies around spawned leader | `move_speed` x1.12 | Triggered `OnSpawn` | Per-card-cast | 6s / none | Multiplicative |
| G15 | Summoner trait | Summoner and spawned units | Self and all owned spawned units | `cast_speed` x1.10 and `attack_speed` x1.05 | First 20s of battle | Mixed (`team` + `unit`) | Window-gated | Multiplicative |
| G16 | Summoner trait | Allies | Allies within radius 3 excluding source | `lifesteal` +0.05 flag/value | Always (aura) | Per-unit | Active while in radius | Additive flag merge |
| G17 | Summoner trait | Summoner resource economy | Self | Refund 100% of mana spent during first 5s (one-time at t=5.0s) | First 5s tracked, payout at boundary | Per-team | One-time / none | N/A |
| G18 | Summoner trait | Summoner resource economy | Self | Boundary check: first-5s refund includes/excludes spend at exactly `t=5.0s` by explicit rule | Time boundary | Per-team | One-time / none | N/A |
| G19 | Summoner trait | Allies | Nearest 3 allies but only 2 exist | Applies to available allies only; no null/phantom targets | Triggered `OnHit` | Per-unit | 4s / 8s | Multiplicative |
| G20 | Summoner trait | Allies | Nearest N with equal-distance ties | Stable tie-break by `distance` then `unit_id` | Always | Per-unit | Permanent | N/A |
| G21 | Summoner trait | Spawned units | Self unit | On-hit buff must not recursively retrigger itself in same resolution chain | Triggered `OnHit` | Per-unit | 3s / 5s | Multiplicative |
| G22 | Summoner trait | Allies | Radius aura from source unit | Aura deactivates immediately when source dies | Always (aura) | Per-unit | Active while source alive | N/A |
| G23 | Summoner trait | Allies | Radius aura from source unit | Inactive/spawning unit does not emit aura before activation | Always (aura) | Per-unit | Active while source active | N/A |
| G24 | Summoner trait | Allies and enemies | Team-relative radius or nearest selection | If source switches team, effect ownership and valid target set re-evaluate deterministically | Always | Per-unit | Continuous | N/A |
| G25 | Summoner trait | Spawned units | All owned spawned units | Duplicate effect from same `source_id` merges or stacks by explicit deterministic policy | Always | Per-unit | Permanent | Policy-defined |
| G26 | Summoner trait | Spawned units | All owned spawned units | Buff pushes stat above cap; clamp behavior is deterministic and documented | Always | Per-unit | Permanent | Multiplicative + clamp |
| G27 | Summoner trait | Spawned units | Units with zero base for target stat | Multiplicative on zero-value stats follows explicit rule (usually remains zero) | Always | Per-unit | Permanent | Multiplicative |
| G28 | Summoner trait | Summoner resource economy | Self | Mana refund trait with mana-cost reduction trait active in same window | Refund computed from actual mana spent after discounts | First 5s | Per-team | One-time / none | N/A |
| G29 | Summoner trait | Summoner resource economy | Self | Two refund traits in same window | Over-refund handling (cap vs stack) follows explicit deterministic policy | First 5s | Per-team | One-time / none | Policy-defined |
| G30 | Summoner trait | Allies | Radius aura with rapid in/out movement | Re-entry flicker around radius edge resolves deterministically by tick rules | Always (aura) | Per-unit | Continuous | N/A |
| G31 | Summoner trait | Spawned units | Mixed scopes | Per-unit and per-team triggers of same type coexist without state corruption | Triggered mixed | Mixed | Configured | N/A |
| G32 | Summoner trait | Trigger state | N/A | Mid-cooldown and mid-duration trigger state survives snapshot/reconnect exactly | Triggered | Per-unit default | Configured | N/A |
| G33 | Any trait | Any | Any | Invalid predicate/effect config fails by deterministic skip/fail policy without crash | Authoring/runtime safety | Any | N/A | N/A |

## 8. Availability and Distribution Gating Cases

These validate that traits can be gated by campaign, mission, character, and external entitlement rules.

| ID | Gate Type | Example Rule | Expected Result | Status |
|---|---|---|---|---|
| A01 | Campaign gate | Trait is only eligible in campaign `campaign_main_01` | Trait never appears outside allowed campaign | Design-Covered |
| A02 | Mission gate | Trait can only be offered in mission `mission_03_boss` | Offer/spend path blocks trait in other missions | Design-Covered |
| A03 | Character gate | Trait only valid for summoner `Cole` | Non-Cole summoners cannot roll or spend this trait | Design-Covered |
| A04 | Character blacklist | Trait valid for all except `Mei` | Mei is excluded while others remain eligible | Design-Covered |
| A05 | Promo code entitlement | Trait unlocks when promo code `FOUNDER2026` is redeemed | Trait pool includes it only after entitlement is present | Design-Covered |
| A06 | Promo expiration | Promo trait entitlement expires at configured date/time | Trait becomes unavailable after expiry by deterministic rule | Design-Covered |
| A07 | Campaign branch gate | Trait available only on specific narrative branch flag | Branch mismatch blocks offer/spend deterministically | Design-Covered |
| A08 | Mission completion prerequisite | Trait unlocks only after mission `M12` completed | Trait unavailable before completion and available after | Design-Covered |

## 9. Offer-Time vs Spend-Time Gating Conflict Cases

These cases validate deterministic behavior when trait eligibility changes after an offer is rolled but before the point is spent.
Current design default is roll-at-spend-time, but conflict policy still matters for any cached/previewed offers.

| ID | Conflict Type | Example | Deterministic Resolution | Status |
|---|---|---|---|---|
| V01 | Campaign changed | Trait previewed in Campaign A, player enters Campaign B before spending | Re-evaluate at spend; if now ineligible, spend fails with reason `campaign_gate` | Design-Covered |
| V02 | Mission changed | Trait shown in mission lobby, mission switched before confirm | Re-evaluate at spend; ineligible trait rejected | Design-Covered |
| V03 | Character switched | Trait previewed for Cole, player switches active summoner to Mei | Re-evaluate at spend using current owner identity | Design-Covered |
| V04 | Promo expired | Promo trait was visible while entitlement active, expires before spend | Spend-time entitlement check fails with reason `entitlement_expired` | Design-Covered |
| V05 | Promo redeemed late | Trait initially unavailable, promo redeemed before spend flow | Re-roll/refresh offer source at spend; trait can become newly eligible | Design-Covered |
| V06 | Branch flag changed | Narrative branch state changed between preview and spend | Spend uses current branch flags; deterministic rejection if mismatch | Design-Covered |
| V07 | Prereq newly met | Trait unavailable at preview, prerequisite met before spend | Spend-time roll can include trait if now eligible | Design-Covered |
| V08 | Prereq lost | Trait previewed, prerequisite trait respec/removed before spend | Spend-time validation rejects trait with reason `missing_prerequisite` | Design-Covered |
| V09 | Pool weights changed | Live config updates weights mid-session | Deterministic behavior uses pinned ruleset version for active battle/session | Design-Covered |
| V10 | Trait disabled | Trait hard-disabled by balance flag after preview | Spend-time validation rejects with reason `trait_disabled` | Design-Covered |
| V11 | Point count changed | Spend attempted after another action consumed the point | Atomic spend check fails with reason `insufficient_points` | Design-Covered |
| V12 | Multi-client race | Two clients attempt spend against same point in close succession | Authoritative side accepts first deterministic winner, second fails safely | Design-Covered |

## 10. Locked Runtime Policy Cases

These cases validate the three newly locked defaults: boundary semantics, clamps, and aura cadence.

| ID | Policy Type | Example | Expected Result | Status |
|---|---|---|---|---|
| P01 | Time boundary | Effect window `0-5s` with action at exactly `t=5.0` | Action is outside window (`end` exclusive) | Design-Covered |
| P02 | Boundary payout | Refund-at-5s window closes between ticks | Refund resolves once on first tick where `time >= 5.0` | Design-Covered |
| P03 | Move speed clamp | Multipliers push move speed over max | Final speed is clamped to configured max | Design-Covered |
| P04 | Cast speed clamp | Debuffs push cast speed below min | Final cast speed is clamped to configured min | Design-Covered |
| P05 | Crit/lifesteal clamp | Combined effects exceed 1.0 | Final value is clamped to 1.0 | Design-Covered |
| P06 | Aura cadence | Unit crosses aura edge mid-frame | Membership updates deterministically on next fixed tick | Design-Covered |
| P07 | Aura tie resolution | Multiple same-distance candidates in aura cap set | Stable ordering by deterministic tie-break | Design-Covered |

## 11. Open Gameplay Context Needed (Optional)

If we want this list to be closer to final shipping content before Pass 2, additional context would help on:
1. Expected max ally count in dense fights (affects `nearest N` defaults).
2. Preferred aura update cadence (continuous vs interval).
3. Which stats are intended to be hard-capped (if any).
