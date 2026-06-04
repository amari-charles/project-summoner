# Remaining Work Scoping Roadmap

This document is a living scoping checklist for production work that cuts across content, VFX, academy classes, items, upgrades, and rewards.

Design intent still lives in `docs/design/` and element docs. This file is for counting, grouping, estimating, and converting fuzzy remaining work into tracked implementation tasks.

## Source References

- `docs/tracking/todos.md`
- `docs/tracking/bugs.md`
- `docs/design/academy-forging-model.md`
- `docs/design/academy-forging-implementation-spec.md`
- `docs/design/fire-content-working-notes.md`
- `docs/design/water-content-working-notes.md`
- `docs/design/earth-content-working-notes.md`
- `docs/design/wind-content-working-notes.md`
- `docs/technical/spell-system-audit.md`
- `docs/features/equipment-system.md`

## Current Read

- Active bug tracker has no open bugs.
- Active TODOs now treat the current spell roster as enough for this slice; remaining spell work is production VFX, balance, presentation, academy placement, and reward/loot integration.
- First-pass Fire/Water/Earth/Wind spell runtime coverage exists with placeholder/readability VFX, but production-quality VFX, art direction, and audio pairing are still open.
- Academy design targets a Year 1 MVP first, with Semester 1 and Semester 2 course content proving the loop before later-year expansion.
- Equipment slots and runtime item modifiers exist; remaining scoping is mostly catalog breadth, reward placement, shop placement, and art/icon needs.

## Spell VFX Count Model

### Naive Bespoke Count

If every spell gets a unique finished VFX, the first-pass four-element roster implies:

| Scope | Estimate | Notes |
| --- | ---: | --- |
| Year 1 immediate academy spell needs | 5-6 spell VFX | Magic 101 neutral/basic spell, Practical Spellcraft spell, and one spell from each Intro Element class. Some may reuse the same neutral/basic presentation. |
| First-pass Fire/Water/Earth/Wind spell roster | 24 spell VFX | Working target is 6 spells per element across 4 elements. Existing runtime effects still need production polish. |
| Current element-doc spell library | 44 spell docs | Includes future/expanded elements beyond the initial Fire/Water/Earth/Wind academy slice. |

The naive count is useful for upper-bound production planning, but it is probably too expensive and too brittle for this project.

### Kit-Based Count

A better production model is to commission/make/find reusable VFX kits, then tune them per spell.

| Reuse Layer | Estimate | What It Covers |
| --- | ---: | --- |
| Element material/palette kits | 4 initial kits | Fire, Water, Earth, Wind. Later elements can repeat this model. |
| Shape templates | 5-6 templates | Projectile/bolt, single-target strike, area burst, persistent field, buff/shield aura, line/cone sweep. |
| Spell-specific tuning presets | 24 initial presets | Radius, color, timing, particles, decals, scale, and sound pairing per spell. |
| Bespoke hero effects | 3-6 effects | For visually important spells that should not feel like recolors. |

This means the initial four-element roster should not require 24 wholly bespoke commissions. A realistic first production pass is closer to 10-14 authored bases plus per-spell tuning, with hero effects added only where reuse fails.

### Suggested Archetypes

| Archetype | Example Spells | Reuse Notes |
| --- | --- | --- |
| Projectile / bolt | Fireball, Water Jet, Air Bullet, Stone Spike | Shared travel core with element material, trail, and impact swap. |
| Single-target strike / mark | Ignition Mark, Earthen Grip, Gust, Cleanse target moments | Small burst, reticle, or above-target flash. |
| Area burst | Fire Area Burn, Burn Cashout, Quake | Ground indicator plus timed burst. |
| Persistent field / pulse | Rain Field, Whirlpool, Gravity Well, Tornado, Crosswind, Tail Wind | Looping zone effect with pulse timing and readable radius. |
| Buff / shield aura | Bubble Shield, Flow, Overheat, Flare Shield, Fortify, Reform Earth | Attachment or area aura, usually lower particle count and longer lifetime. |
| Line / cone sweep | Wind Shear, possible flamethrower-style future fire spell | Directional effect with strong shape readability. |

## Work Groups

### Spell VFX Inventory & Reuse Plan

**Urgency:** High  
**Ease:** Medium  
**Scope:** Medium

**Included work:**
- Build a table of every runtime spell and its current VFX state.
- Mark each spell as `production-ready`, `placeholder-readable`, `missing`, or `needs replacement`.
- Assign each spell to a VFX archetype.
- Decide which effects need bespoke art versus kit tuning.

**Likely files:**
- `docs/tracking/todos.md`
- `docs/tracking/remaining-work-scope.md`
- `docs/technical/spell-system-audit.md`
- `scenes/battle/vfx/`
- `scripts/battle/vfx/`

### Academy Year 1 Content Matrix

**Urgency:** High  
**Ease:** Medium  
**Scope:** Large

**Included work:**
- Turn Year 1 Semester 1 and Semester 2 into a course matrix.
- List each course's activities, reward preview, reward payload, prerequisites, and Honors hooks.
- Identify exactly which cards, traits, equipment, gold, and consistency tools each class needs.
- Convert the matrix into course catalog data tasks.

**Likely files:**
- `docs/design/academy-forging-model.md`
- `docs/design/academy-forging-implementation-spec.md`
- `docs/technical/meta/academy-forging-plan.md`
- `docs/technical/meta/academy-forging-validation-cases.md`
- academy course catalog/data files once implementation resumes

### Element Roster Production Readiness

**Urgency:** High  
**Ease:** Hard  
**Scope:** Large

**Included work:**
- Reconcile working-note targets against runtime/card data.
- Treat the current spell mechanics count as sufficient unless product direction changes.
- Focus remaining spell work on presentation quality, VFX/audio readiness, balance, and academy/loot placement.
- Inventory summon-unit breadth separately from spell-count expansion.

**Current working-note gaps:**
- Summon-unit breadth and visual production gaps still need a separate inventory.
- Spell mechanics expansion is no longer an active first-slice goal.
- Working notes may still contain future spell ideas, but those should not automatically become active TODOs.

**Likely files:**
- `docs/design/fire-content-working-notes.md`
- `docs/design/water-content-working-notes.md`
- `docs/design/earth-content-working-notes.md`
- `docs/design/wind-content-working-notes.md`
- card catalog/data files
- battle simulation spell/unit files

### Items, Equipment, and Shop Rewards

**Urgency:** Medium  
**Ease:** Medium  
**Scope:** Medium

**Included work:**
- Inventory current item catalog by slot: Wand, Ring1, Ring2, Robes.
- Decide which items are starter, shop, class reward, Honors reward, or event reward.
- Identify missing icons/art and missing item descriptions.
- Decide whether early academy classes should grant equipment directly or unlock shop stock.

**Likely files:**
- `docs/features/equipment-system.md`
- item catalog/data files
- academy reward catalog/data files
- shop/caravan data files

### Upgrades, Traits, and Resource Costs

**Urgency:** Medium  
**Ease:** Medium  
**Scope:** Large

**Included work:**
- Inventory existing card upgrades and summon/summoner traits.
- Decide which upgrades are normal XP-only progression and which need special resource costs.
- Map upgrades and traits to class rewards, Honors rewards, shop unlocks, and events.
- Finish UI affordability display for upgrade-specific costs if special resources become real.

**Likely files:**
- `scripts/infrastructure/data/card_upgrade_catalog.gd`
- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`
- card progression UI files
- academy reward catalog/data files

### Production Asset Acquisition Plan

**Urgency:** High  
**Ease:** Medium  
**Scope:** Large

**Included work:**
- Decide what we commission, what we make procedurally, what we buy/find, and what stays placeholder.
- Create a VFX style guide before commissioning too much.
- Define acceptance criteria for spell VFX: readable target area, readable impact timing, element identity, performance budget, and camera-scale legibility.
- Pair VFX scoping with spell audio scoping where useful.

**Likely files:**
- `docs/tracking/remaining-work-scope.md`
- possible future VFX style guide doc
- `scenes/battle/vfx/`
- audio asset folders

### Tracking Hygiene

**Urgency:** High  
**Ease:** Easy  
**Scope:** Small

**Included work:**
- Keep this scoping doc updated as decisions harden.
- Promote scoped groups into concrete TODO entries when they are ready for implementation.
- Mark stale TODOs when they are superseded by better scoped work.

**Likely files:**
- `docs/tracking/todos.md`
- `docs/tracking/remaining-work-scope.md`

## Running Checklist

- [ ] Build current runtime spell/VFX inventory table.
- [ ] Decide Year 1 spell VFX minimum count and first-pass four-element VFX count.
- [ ] Assign each initial spell to one VFX archetype.
- [ ] Decide which spell VFX are kit-tuned versus bespoke.
- [ ] Draft VFX style guide and production acceptance criteria.
- [ ] Build Academy Year 1 course matrix.
- [ ] List every Year 1 course reward and whether it is card, trait, equipment, consistency tool, gold, or eligibility.
- [ ] Inventory current item catalog by equipment slot.
- [ ] Decide starter/shop/class/Honors placement for items.
- [ ] Inventory current upgrade catalog and trait catalog.
- [ ] Decide special-resource upgrade cost policy.
- [ ] Convert scoped groups into implementation TODOs.

## Recommended Execution Order

1. Spell VFX inventory and archetype taxonomy.
2. Academy Year 1 course and reward matrix.
3. Initial Fire/Water/Earth/Wind roster completion inventory.
4. Item/equipment reward inventory.
5. Upgrade/trait/resource-cost inventory.
6. Production asset acquisition plan.
7. Convert each scoped slice into small TODO entries.

## Open Questions

- Is the first playable academy slice only Year 1 Semester 1, or should Semester 2 be content-complete before the next milestone?
- Should Intro Element classes always grant one summon plus one spell, or can some elements grant a choice between multiple cards?
- Which spell VFX should be hero-quality first: intro spells, high-frequency spells, or visually central capstone-style spells?
- Should early equipment come mostly from classes, campus shop, or events?
- Are special upgrade resources part of the academy MVP, or should upgrade-specific costs remain scaffolded but unused?
