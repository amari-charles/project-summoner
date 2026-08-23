# Project TODOs

This document tracks planned features, improvements, and tasks for Fateforged.

For completed tasks, see [todos-completed.md](todos-completed.md).

**Status Legend:**
- ⬜ Not Started
- 🔄 In Progress
- ✅ Completed
- 🚫 Blocked

**Priority Levels:**
- 🔴 High Priority
- 🟡 Medium Priority
- 🟢 Low Priority

**Tracker Sync (2026-03-05):** Removed completed `Replace /root/VFXManager Lookup in ProjectileVisual`, moved Puff target-stickiness work to completed (PR `#270`), and removed Wisp single-target verification from active queue after post-refactor validation.
**Audit Sync (2026-03-05, evening):** Moved completed camera boundary/pan task to `todos-completed.md` based on merged camera bounds fixes (`#267`) and unit tests; updated directional/cone attack TODO to reflect partial completion (cone gating in targeting is shipped, hitbox-shape work remains).
**Tracker Sync (2026-03-08):** Moved completed typed-internal service handler refactor and loading-screen preloading work to `todos-completed.md`; updated blocked-idle investigation and `_PhysicsProcess` throttling entries to reflect merged movement/perf commits and remaining verification scope.
**Tracker Sync (2026-03-08, late):** Added missing completion records for GDScript typed-API safety migration (`#288`) and StringName-safe coercion sweep (`#290`) to `todos-completed.md`; active queue unchanged aside from status notes.
**Tracker Sync (2026-03-08, final):** Closed blocked-unit idle freeze item after manual signoff; moved remaining notes to `todos-completed.md` and refreshed AI priority queue to only open items.
**Tracker Sync (2026-03-08, desync pass):** Closed sim/visual state desync audit task after phase sync hardening, summoner destroy signal dedupe, activation-state visual alignment fix, and regression coverage updates.
**Tracker Sync (2026-03-09, combat correctness):** Moved completed DamageProfile armor/magic-resist integration + summoner combat-modifier wiring to `todos-completed.md`; removed UI damage-type card indicator from this task per product direction.
**Tracker Sync (2026-03-10, attack vectors):** Updated `Implement Single Target vs Multi Target Attack System` to partial after runtime V1 delivery (vector recipient resolution + tests); visual telegraphs and balance pass remain.
**Tracker Sync (2026-03-11, attack vector target-limit semantics):** Recorded follow-up fix preserving explicit `TargetLimit` values across presets (`1` single-target, `0` unlimited) while keeping preset defaults when unset; remaining scope for this initiative is still visual telegraphs + balance tuning.
**Tracker Sync (2026-03-10, summoner design):** Added Summoner Oaths planning item (trait-backed permanent choices) and split trait work to prioritize curated, intentional trait design over placeholder AI-generated traits.
**Tracker Sync (2026-03-11, summon traits runtime):** Updated trait-curation item to reflect shipped summon stat-tree runtime (shared trait IDs, per-card/per-rarity overrides, additive + spawn-count hooks, rarity-gated Legion tiers, coverage); remaining scope narrowed to per-summoner identity lines and campaign-level ultimate/oath design validation.
**Tracker Sync (2026-03-11, per-summoner lines):** Simplified per-summoner identity lines to summoner-stat-only V1 (no unit modifiers/triggers) for Cole/Selene/Mei/Teo in `docs/design/summon-traits-v1.md`; remaining trait-curation scope is campaign-facing Ultimate/Oath candidate pass and permanence validation.
**Tracker Sync (2026-03-11, combat spatial v2):** Updated directional attack, multi-target, and hitbox tracker entries to reflect runtime geometry-channel split + debug overlay progress; engage-shape startup alignment remains open.
**Tracker Sync (2026-03-12, quick-win wave):** Closed targeted combat redirect/retarget robustness scope; completed simulation spatial namespace + spawn-rule ownership alignment; completed UI async timeout guards, Puff cone-center offset tuning, and large-unit hit-flash throttling; updated summoner stat audit status and recorded upgrade-cost scaffolding progress.
**Tracker Sync (2026-06-04, production scoping):** Added `docs/tracking/remaining-work-scope.md` as the running scoping roadmap for spell VFX, academy classes, items, upgrades, rewards, and production asset planning.
**Tracker Sync (2026-06-04, active-tracker cleanup):** Moved completed Puff angle, pathfinding robustness, large-unit hit-flash, summoner secondary-stat audit, simulation spatial-domain, spawn-rule source-of-truth, and UI async timeout entries to `todos-completed.md`.
**Tracker Sync (2026-06-04, spell roster scope):** Closed `Add More Spell Cards` as an active expansion item; current spell count is sufficient. Remaining spell-related work stays under VFX polish, balance, presentation, academy/course integration, and production scoping.
**Tracker Sync (2026-06-04, backlog cleanup):** Consolidated premature per-sound audio tasks into one production-audio scoping item, closed stale Puff lateral-movement follow-up by product review, closed completed portrait-cropping and campaign-data migration items, and refreshed stale interop/performance/root-path TODO wording.
**Tracker Sync (2026-08-05, authority-boundary audit):** Started the gated battle-progression-authority initiative and added concrete follow-ups for permanent progression commands, atomic commerce, and authoritative competitive results/loadout validation. Backend provider selection remains intentionally undecided.
**Tracker Sync (2026-08-05, battle authority completion):** Moved the completed battle-progression-authority initiative to `todos-completed.md` after PR `#352` merged; provider-neutral security follow-ups remain active as separate tasks.
**Tracker Sync (2026-08-13, Campus Shop):** Added high-priority changelog infrastructure work and a scoped deprecation plan for the legacy linear Caravan campaign flow.
**Tracker Sync (2026-08-13, Online deck selection):** Added a matchmaking-screen follow-up for selecting and confirming the deck used before queueing.
**Tracker Sync (2026-08-13, Academy hub):** Restored the bounded walkable Academy hub direction with permanent shortcut access; Phase 1 recovery is in progress.
**Tracker Sync (2026-08-14, product direction history):** Reframed the ambiguous changelog task as a curated product direction log, added its inclusion and authority framework, and left historical backfill pending user review.
**Tracker Sync (2026-08-16, designer readiness):** Added an immediate high-priority UI readiness queue covering the summoner progression pass, automatic-level/XP presentation contract, reusable reward acquisition flow, first-quest reward slice, and canonical screen/state inventory needed before external UI design begins.
**Tracker Sync (2026-08-18, discovery-driven development):** Replaced the fully exposed default-tree assumption with shared summoner/card opportunity states, configurable costs, and world actions that can reveal, unlock, acquire, or transform; added the representative design slice and UI/implementation work plan required before replacement progression UI is committed.
**Tracker Sync (2026-08-22, UI flow consolidation):** Replaced the redundant click-through battlefield victory/defeat modal with a brief automatic conclusion overlay before the combined Results screen. Began the canonical screen inventory by resolving this duplicate outcome surface; the broader reward, level-up, course, and navigation inventory remains open.
**Tracker Sync (2026-08-23, battle HUD and settings foundations):** Recorded the functional battle HUD scaffold, mode-aware pause/forfeit flow, shared categorized settings surface, and campus Escape menu. These surfaces are ready for designer treatment, while final visual language, responsive layout validation, and the broader canonical screen inventory remain open.
**Tracker Sync (2026-08-23, ranked loadouts):** Implemented the Online loadout scaffold, separate per-summoner ranked-deck persistence, contextual selection through the collection screen, queue validation, and ranked battle wiring.
**Tracker Sync (2026-08-23, item developer tooling):** Added a bounded audit of the broken item debug-command/service contract after `/items_grant` exposed a nonexistent runtime method call. Repair versus replacement remains an audit outcome rather than a preselected implementation.
**Tracker Sync (2026-08-23, utility overlays):** Standardized Summoner Profile, Spellbook/Deck, Journal, and Inventory as distinct reusable overlays over their invoking context. Online remains a destination and now hosts the shared Spellbook overlay for ranked-deck changes; encounter preparation retains its activity-specific loadout editor.
**Tracker Sync (2026-08-23, designer-readiness reconciliation):** Reconciled the active UI queue after PR `#376`. Summoner Profile, Inventory, Collection/Decks, Journal, quest offers, dialogue, campus HUD/Travel, battle HUD, Settings, Online loadout, summoner selection, and the combined Results direction now have accepted structural scaffolds. Removed stale Inventory and XP assumptions, recorded the remaining state matrices and legacy cleanup, and split generic Activity Preparation into the next explicit core-loop UI review.

---

## Designer Readiness — Immediate Queue

### 🔴 HIGH PRIORITY

#### Remove or Rebuild the Legacy First-Card Choice Screen
**Status:** ⬜ Not Started
**Category:** Onboarding / Cards / UI/UX
**Urgency:** High — canonical card-presentation follow-up
**Ease:** Medium
**Scope:** Medium

The superseded `first_card_selection` route is a bespoke button-based choice
screen rather than a shared full-card presentation. Remove the route with the
legacy onboarding path or rebuild it from the canonical 3:4 card surface; do
not treat a ratio-only change to its outer buttons as a completed migration.

#### Review the Generic Activity Preparation Screen
**Status:** 🟡 Partial (Functional Scaffold)
**Category:** Quests / Activities / Decks / UI/UX
**Urgency:** High — remaining core quest-to-battle handoff
**Ease:** Easy
**Scope:** Small

The generic Activity Preparation surface is reachable and supports encounter
rules, deck validation, fixed or editable loadouts, reward preview, and battle
launch. It has not received the same product/information-architecture review as
the quest offer, Journal, battle HUD, and Results surfaces.

**Tasks:**
- [ ] Confirm the minimum hierarchy: activity identity and objective, authored
  rules, loadout/deck validity, reward expectation, and Start/Back actions.
- [ ] Review owned-deck, class-loadout, and fixed-loadout variants without
  restoring course-specific UI ownership.
- [ ] Define valid, invalid, missing-deck, fixed-loadout, loading, and blocked
  states for designer handoff.
- [ ] Confirm the screen remains an activity-specific preparation surface rather
  than another general-purpose Collection/Deck overlay.

**Likely Files:**
- `scenes/meta/screens/academy_activity_preparation.tscn`
- `scripts/meta/screens/academy_activity_preparation.gd`
- `docs/design/quest-system.md`

#### Review the Campus Shop and Purchase Surface After Economy Scope
**Status:** 🟡 Partial (Functional Scaffold; Product Dependencies Open)
**Category:** Shop / Items / Economy / UI/UX
**Urgency:** Medium — designer handoff needs the states, but product rules lead
**Ease:** Medium
**Scope:** Medium

The Campus Shop has a reachable offer grid and purchase-detail scaffold, but its
final information hierarchy depends on the accepted jobs for gold, item types,
ownership, purchase limits, and universal reward grants. Do not polish around
legacy Caravan assumptions.

**Tasks:**
- [ ] Define the purchase-state contract after gold and item-system planning:
  balance, price, affordability, ownership, purchase limit, confirmation,
  success, and actionable failure.
- [ ] Reuse canonical Card and item inspection surfaces rather than inventing
  shop-only reward visuals.
- [ ] Confirm how services such as card cracking or ritual costs differ from
  ordinary purchasable offerings and keep their world interactions distinct.
- [ ] Give the designer the minimum empty, loading, unavailable, affordable,
  unaffordable, owned, and sold-out states.

**Dependencies:**
- Gold's defined economic job
- Gameplay item ownership and catalog scope
- Universal reward/commerce contracts

#### Rework the Summoner Screen Around Automatic Levels and Banked Upgrade Choices
**Status:** 🔄 In Progress
**Category:** Meta Progression / UI/UX
**Urgency:** High — designer handoff dependency
**Ease:** Medium
**Scope:** Medium

**Description:**
Run a second product and information-architecture pass on the summoner screen. Treat the current screen as functional prototype evidence, not a visual constraint. Remove the manual-level-up assumption and clearly separate automatic numerical growth, banked summoner upgrade choices, permanent traits, equipment, and descriptive identity.

**Tasks:**
- [x] Confirm automatic summoner leveling as product direction; apply every affordable level and carry remaining XP forward.
- [x] Replace the persistent `Level Up` action with owned-trait development entry points and an unspent-point state.
- [x] Establish the current information hierarchy: portrait and equipped items;
  level and XP; compact identity and stats; and owned-trait summary plus Upgrades.
- [x] Remove the owned-item grid from Summoner and route the campus bag action to
  a reusable Inventory prototype while retaining equipped slots on Summoner.
- [x] Convert campus Summoner Profile access to a fixed centered overlay that
  retains the dimmed campus context and pauses traversal while open.
- [x] Accept Summoner Profile and Inventory as distinct fixed overlays over the
  invoking context; do not merge Inventory into the profile or route it through
  a separate full-screen destination.
- [ ] Migrate gameplay-item definitions, grant call sites, saved instances, and
  ownership queries from the legacy account-wide path to summoner ownership.
- [ ] Define the minimum states the designer must cover: ordinary progress, newly leveled, unspent points, max level, locked upgrade, and permanent branch confirmation.
- [x] Use `Level`, `XP`, `Trait Points`, `Card Points`, and `Traits` as the current
  structural terminology; final copy and authored trait names may still change.
- [x] Treat the current portrait, stat icons, stat ladders, typography, and panel
  styling as replaceable designer-facing scaffolding.

**Progress (2026-08-17):** Automatic multi-level processing and banked points are
implemented. The accepted profile is a fixed overlay containing identity, Level
and XP, stats, equipped items, and clickable owned Traits. Inventory is a separate
summoner-scoped overlay, and equipped slots reopen it in a compatible-item
context. Remaining work is the gameplay-item ownership migration, explicit
designer state coverage, and final visual design.

**Likely Files:**
- `docs/design/trait-tree-screen-flow-spec.md`
- `docs/design/discovery-driven-development.md`
- `docs/design/academy-forging-model.md`
- `scenes/meta/screens/summoner_screen.tscn`
- `scripts/meta/screens/summoner_screen.gd`
- `scenes/meta/screens/trait_tree_screen.tscn`

#### Define the Discovery-Driven Summoner and Card Development Slice
**Status:** 🔄 In Progress
**Category:** Summoner Progression / Card Progression / World Systems
**Urgency:** High — replaces the default-tree assumptions used by progression UI
**Ease:** Hard
**Scope:** Large

**Description:**
Prove the shared discovery-driven development model with one representative
summoner trait and one representative card. Levels provide banked, entity-bound
points; quests, rituals, and other world actions configurably reveal, unlock,
acquire, or transform with optional points, materials, or other costs.

**Tasks:**
- [ ] Author one summoner trait with ordinary and ritual-driven development.
- [ ] Author one card with mutually exclusive behavioral branches and at least
  one world-gated opportunity.
- [ ] Walk both examples through hidden, known-locked, available, acquired, and
  permanently closed states.
- [ ] Store per-upgrade acquisition metadata on each owner, including path,
  acquisition source/occurrence, card or summoner level at acquisition,
  acquisition sequence, and configured cost snapshot.
- [ ] Define a non-destructive effective-state projection for capped battles;
  cap effective Card level and automatic level-scaled stats while preserving all
  acquired upgrades, and never downgrade or rewrite the permanent Card instance.
- [ ] Define configurable acquisition costs, including free, point-only,
  material-only, and combined transactions.
- [x] Confirm one banked owner-bound point per level plus modest automatic base
  stat growth; exact curves and affected stats remain to be tuned.
- [x] Keep Summoner health and maximum mana in automatic growth; configure Card
  growth per Card and defer exact growth/mana-cost tuning.
- [x] Confirm no separate Summoner Core tree; Summoner points are spent across
  acquired traits, while Cards expose Core plus acquired trait paths.
- [x] Confirm owned trait circles as the primary Summoner development entry;
  remove the separate global `Upgrades` button.
- [x] Give the Traits section more vertical height, wrap icons across multiple
  rows, and scroll after the visible grid is full; do not add `+N / View All`.
- [x] Confirm a large selected-trait overlay, no embedded trait switcher for the
  first version, and one-node trees for atomic traits.
- [x] Show Trait Points in both the Summoner Traits header and open overlay; show
  Card `Core` as the first circle beside acquired Card traits and use the same
  overlay interaction for every selection.
- [x] Hide permanently closed Card Core paths by default.
- [ ] Add optional closed-path inspection, preserve available-but-unaffordable
  state, and mark newly revealed or unlocked nodes until inspected.
- [ ] Use toast notifications for reveal/unlock feedback and the generic reward
  presentation for acquisition/transformation.
- [x] Keep the tree dominant and use a contextual node detail/action popover on
  hover, focus, or pinned selection; communicate branch closure through the tree
  instead of repeating alternatives in confirmation, and keep physical ritual
  acquisition outside the tree overlay.
- [ ] Exercise both preferred content patterns and exceptions: quest unlocks,
  ritual acquisition, quest acquisition, and ritual unlocks.
- [x] Prototype the selected-path development interface for Summoner traits,
  Card Core, and acquired Card traits.

**Progress (2026-08-22):** Automatic levels and banked owner-bound points are
implemented for Summoners and Cards. The shared selected-path overlay is wired
into both owner surfaces. Fire Wisp proves explicit Card Core membership, a
single inherent root, a behavioral permanent fork, supporting stat nodes, and
authoritative closure. Summoner representative world-driven development,
durable discovery/provenance state, configurable material transactions,
closed-path inspection, notification feedback, and capped-battle projection
remain incomplete.

**Related Doc:**
- `docs/design/discovery-driven-development.md`
- `docs/tracking/discovery-driven-development-work-plan.md`

#### Define Summoner XP Visibility Across the Persistent HUD and Progression Surfaces
**Status:** 🟡 Partial (Placement Contract Accepted)
**Category:** Meta Progression / HUD / UI/UX
**Urgency:** High — designer handoff dependency
**Ease:** Easy
**Scope:** Small

**Description:**
Establish one explicit XP presentation contract so the designer does not omit progression state or add redundant bars to every screen. Decide where summoner level, XP progress, and unspent upgrade points are persistent, contextual, or intentionally hidden.

**Tasks:**
- [x] Keep a persistent XP bar/ring out of the walkable-campus profile widget;
  the compact widget may show Level and an unspent-choice badge without becoming
  a second progression screen.
- [x] Keep summoner XP out of the battle HUD unless a separate in-battle use is explicitly approved.
- [ ] Specify the exact before/after, rollover, multi-level, and max-level states
  the designer must support in the combined Results surface.
- [x] Show current Summoner Level and XP progress prominently on the Summoner Profile.
- [x] Show participating-card XP in Results and Card progression/details rather
  than in the global HUD.

**Remaining Scope:** This is no longer a placement decision. Only the Results
animation/state contract and the optional unspent-choice badge treatment remain.

**Likely Files:**
- `scenes/meta/components/summoner_icon_widget.tscn`
- `scenes/meta/screens/walkable_academy_hub.tscn`
- `scenes/meta/screens/summoner_screen.tscn`
- `docs/technical/meta/unified-post-battle-flow-proposal.md`

#### Build One Reusable Reward and Progression Reveal Flow
**Status:** 🔄 In Progress
**Category:** Rewards / Progression / UI Architecture
**Urgency:** High — major missing player-feedback screen
**Ease:** Hard
**Scope:** Large

**Description:**
Replace the fragmented campaign reward screen, encounter results screen, and optional level-up modals with one clear post-battle flow backed by reusable acquisition/reveal components. The same acquisition presentation must also support rewards granted outside battle, including quest turn-ins.

**Progress:** Quest turn-ins use a reusable reward-grant modal fed by the generic
quest-completion result. Campaign and encounter battles route through one
combined Results prototype after a brief automatic battlefield conclusion. Card
rewards use the canonical full-card surface, and required choices remain distinct
from automatic grants. The typed report builder, exact before/after snapshots,
non-card reveal treatments, contextual progress rows, and deletion of legacy
result/reward destinations remain open.

**Tasks:**
- [x] Approve the unified post-battle sequence: battlefield conclusion, then one combined Results surface for summoner/card XP, level reveals, acquired rewards, contextual quest/rating progress, and continue destination.
- [x] Reuse canonical card presentation for card previews and card reward reveals.
- [ ] Define reusable reveal treatments for item/equipment, gold/resource,
  Summoner/Card points, and special trait or eligibility rewards.
- [x] Keep automatic grants distinct from persistent required reward choices and
  use the same normalized reward contract regardless of source.
- [x] Route both campaign and encounter victory/defeat outcomes through the shared Results prototype.
- [ ] Deprecate `RewardScreen` and `EncounterResults` as competing end destinations once the unified route is complete.
- [ ] Preserve authoritative reward/progression mutation outside the presentation layer; the screen consumes a typed post-battle report and submits only explicit pending choices.

**Placement Rationale:**
The report builder belongs in the meta/application progression boundary because reward, XP, quest, and competitive results change independently but are consumed together after battle. Reusable reveal views belong under meta UI components; battle simulation must not own meta progression presentation.

**Related Doc:**
- `docs/technical/meta/unified-post-battle-flow-proposal.md`

**Likely Files:**
- `scripts/csharp/Battle/View/BattleScene.cs`
- `scenes/meta/screens/reward_screen.tscn`
- `scenes/meta/screens/academy_activity_results.tscn`
- reusable summoner level-reveal component (not yet created)

#### Give the First Quest a Real Reward and Prove the Complete Acquisition Loop
**Status:** 🔄 In Progress
**Category:** Quests / Rewards / Vertical Slice
**Urgency:** High — complete the generic reward contract proven by the first slice
**Ease:** Medium
**Scope:** Medium

**Description:**
Author an intentional reward for `Introduction to Magic`, grant it through the universal reward system when the quest is completed, and present it through the reusable acquisition flow. This is the first proof that NPC conversation, quest tracking, battle completion, return dialogue, permanent progression, and reward presentation form one coherent loop.

**Tasks:**
- [x] Decide the first quest's exact reward and why it matters to the player's next choice (Magic Bolt).
- [ ] Add a generic quest-completion reward contract rather than a professor- or Academy-specific grant path.
- [x] Commit automatic grants before presentation and persist any required reward choice.
- [x] Show the actual card or item visual when that type is awarded.
- [x] Update the Journal's reward preview and completed state from the same normalized reward data.
- [x] Validate idempotency so repeating dialogue or reloading cannot duplicate the reward.

**Remaining Scope:** The Academy curriculum adapter currently produces the
normalized completion summary. Add a generic quest reward-effect handler so
non-academic quests can author and grant rewards without Academy involvement.

**Placement Rationale:**
Quest completion may originate from any character or activity, so quest-to-reward orchestration belongs at the generic meta quest/reward boundary. The universal reward authority owns permanent grants; dialogue and Journal views only render normalized state.

**Likely Files:**
- `data/quests/quests.json`
- `scripts/csharp/Meta/Services/Campaign/Quests/`
- `scripts/csharp/Meta/Services/Rewards/`
- `scripts/meta/screens/quest_journal.gd`

#### Produce a Canonical Player-Facing Screen and State Inventory for Design Handoff
**Status:** 🔄 In Progress
**Category:** UI/UX / Product Architecture
**Urgency:** High — prevents designing obsolete or duplicate flows
**Ease:** Easy
**Scope:** Small

**Description:**
Inventory every currently reachable player-facing screen and overlay, then label each one `retain`, `redesign`, `merge`, `deprecate`, or `prototype-only`. Include required states and the canonical route between screens so the designer works from the intended game rather than legacy implementation accidents.

**Progress:** Structural review is complete for Summoner selection/reveal,
walkable campus HUD and Travel, dialogue, quest offer, Journal, Summoner Profile,
Trait development, Inventory, Collection/Decks, battle HUD and pause, Settings,
Online ranked loadout, and the combined Results direction. These remain
designer-facing scaffolds rather than final art. Generic Activity Preparation,
the obsolete first-card choice route, the Results state matrix, Shop/purchase
states, and legacy destination removal are the remaining major UI decisions.

**Tasks:**
- [ ] Map onboarding, summoner selection, campus HUD, dialogue, Journal, quest preparation, battle, results/rewards, summoner, upgrades, equipment, shop, collection/deck, settings, and online flows.
- [ ] Identify duplicate victory, reward, level-up, course, and navigation surfaces.
- [x] Define persistent campus HUD ownership: profile at upper left, tracked quest
  beneath it, utility actions on the center-right rail, and conceptual future
  Friends capacity without a dead control.
- [ ] Record empty, locked, loading, error, confirmation, newly-unlocked, and unspent-choice states for each retained surface.
- [x] State the 1920x1080 logical design viewport and current desktop
  mouse/keyboard assumptions.
- [ ] Define and validate the supported aspect-ratio range.
- [ ] Link existing Figma references and distinguish approved layouts from exploratory mockups.
- [x] Reserve conceptual capacity for a future Friends panel in the campus HUD's right-side action rail without shipping a nonfunctional control.

**Likely Files:**
- `docs/design/`
- `docs/technical/meta/`
- `scenes/meta/screens/`
- `scenes/meta/components/`

---

## AI-First Priority Queue (2026-03-08, desync pass)

1. Continue hot-path throttling in `_PhysicsProcess`
   Why first: Recent movement/perf fixes landed, but full scale-target optimization plan is not complete yet.
2. Investigate snapshot system overhaul for deterministic test-state setup
   Why now: Trait/system validation depends on quickly creating exact profile states; current snapshot flow is functional but too indirect and lacks schema/version guarantees.

---

## Production Scoping

### 🔴 HIGH PRIORITY

#### Recover the Bounded Walkable Academy Hub
**Status:** 🔄 In Progress
**Category:** Meta Navigation / UI/UX
**Effort:** Medium

**Description:**
Restore the contained walkable campus as the Academy's primary entry surface without turning it into an open world. Existing feature screens remain authoritative, persistent UI actions stay separate from physical navigation, and a Travel/Wayfinder control reaches eligible world waypoints without bypassing objectives or traversal.

**Tasks:**
- [x] Recover the bounded campus, player movement, placeable buildings, and entrance interactions on a fresh branch.
- [x] Route current Class Hall, Campus Shop, Mission Hall, Dorms, Online, Settings, and Summoner screens through one campus destination contract.
- [x] Preserve the former menu hub as a temporary fallback during recovery.
- [x] Replace the broad shortcut list with a Travel/Wayfinder panel backed by physical waypoint data.
- [x] Surface the tracked quest's nearest eligible waypoint without teleporting directly onto its objective.
- [x] Remove Journal, Spellbook, Inventory/Summoner, and Settings from physical travel destinations.
- [ ] Add authored initial availability and persisted discovery rules before the travel roster expands beyond the current graybox landmarks.
- [ ] Complete hands-on UX validation of movement, building entry, back navigation, and shortcuts.
- [ ] Replace placeholder campus presentation with an approved layout and production asset plan.
- [ ] Scope real-time presence separately after the single-player hub is proven.

**Related Doc:**
- `docs/design/walkable-academy-hub.md`

#### Establish a Product Direction Log
**Status:** 🔄 In Progress
**Category:** Documentation / Product History
**Effort:** Small

**Description:**
Create and maintain an internal direction log for medium- and large-scale game and product decisions. It records what changed, why, and what prior direction was superseded without duplicating public release notes or treating implementation history as product intent.

**Tasks:**
- [x] Define the inclusion threshold, entry format, authority rules, and update cadence.
- [x] Add `docs/project/direction-log.md` alongside the existing public changelog and technical development history.
- [x] Add repository guidance so agents update the log only for explicitly approved medium- or large-scale product decisions.
- [ ] Backfill major recent Academy, lesson/activity, deck-loadout, and UI changes from merged pull requests.
- [ ] Review the backfilled entries with the user before treating them as accepted historical interpretation.

**Priority Note:**
The framework is established. Complete the curated, user-reviewed historical backfill before further large UI and progression iterations accumulate.

#### Deprecate the Legacy Caravan Campaign Flow
**Status:** ⬜ Not Started
**Category:** Meta Progression / Legacy Cleanup
**Effort:** Medium

**Description:**
Retire the Caravan's campaign-node/event implementation now that Academy navigation and graph-based lessons have replaced the older linear campaign experience. The Campus Shop remains the persistent Academy shop. A future Caravan concept may return as a temporary or visiting vendor, but should not retain dependencies on the legacy campaign route.

**Tasks:**
- [ ] Inventory Caravan scene routes, event definitions, narrative cues, catalog entries, save fields, and tests.
- [ ] Decide whether existing Caravan purchase history needs a save migration or can remain ignored legacy data.
- [ ] Remove the dedicated legacy Caravan screen and campaign-event routing.
- [ ] Remove Caravan event definitions and graph-consistency requirements.
- [ ] Remove or archive Caravan-only localization, narrative content, shop catalog entries, and tests.
- [ ] Reassess the Caravan as a future Academy visiting-vendor feature under a separate design task.

**Scope Note:**
The current Campus Shop UI pass only removes stale Caravan behavior duplicated inside `ShopScreen`; full deprecation remains a separate reviewed change.

#### Scope Remaining Content, VFX, Items, and Academy Work
**Status:** 🔄 In Progress
**Category:** Planning / Production Scope
**Effort:** Medium

**Description:**
Maintain a dedicated scoping roadmap that turns fuzzy remaining production work into counted, grouped, knockable-down tasks. This includes spell VFX needs, reusable VFX kit assumptions, academy class content, items/equipment, upgrades, reward placement, and production asset acquisition.

**Tasks:**
- [ ] Build current runtime spell/VFX inventory table.
- [ ] Decide Year 1 spell VFX minimum count and first-pass four-element VFX count.
- [ ] Assign initial spells to reusable VFX archetypes.
- [ ] Draft Academy Year 1 course and reward matrix. Progress: Magic 101 playtest structure, rewards, and validation docs are in PR #348; remaining Year 1 courses still need the same treatment.
- [ ] Add a Practical Spellcraft lesson/activity that clearly teaches spells cannot be played during the preparation phase, including UX feedback so players understand why the play is blocked. Progress: PR #349 adds activity-local class rules and a Practical Spellcraft constrained loadout; remaining scope is battle-time prep-phase UX feedback when spell play is blocked.
- [ ] Inventory item/equipment catalog gaps and reward placement.
- [ ] Inventory upgrade/trait catalog gaps and special-resource cost policy.
- [ ] Convert scoped groups into smaller implementation TODOs.

**Related Docs:**
- `docs/tracking/completion-roadmap.md`
- `docs/tracking/remaining-work-scope.md`
- `docs/design/academy-forging-model.md`
- `docs/design/academy-forging-implementation-spec.md`
- `docs/technical/spell-system-audit.md`
- `docs/features/equipment-system.md`

**Notes:**
- Product/design docs remain the source of truth for intent. This tracking doc exists to count, group, estimate, and sequence the work.

---

## Architecture & Launch Routing

### 🔴 HIGH PRIORITY

#### Replace `scene_path`-driven battle launch with typed runtime routing
**Status:** ⬜ Not Started
**Category:** Architecture / Application
**Effort:** Medium

**Description:**
Battle launch surface selection (standard battle vs debug arena) is currently selected via ad-hoc `scene_path` overrides in event data. Move this decision into typed application-level routing so launch behavior is explicit, consistent, and testable.

**Tasks:**
- [ ] Add a typed battle runtime surface contract (for example `Standard`, `DebugArena`, `CustomScene`).
- [ ] Add a single application-layer router/policy used by campaign map + debug menu launch paths.
- [ ] Remove duplicated caller-side `scene_path` branching logic.
- [ ] Add regression tests for launch-surface resolution.

**Related Files:**
- `scripts/meta/screens/campaign_map.gd`
- `scripts/debug/debug_menu.gd`
- `scripts/csharp/Infrastructure/Data/Events/EventDefinition.cs`
- `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs`

#### Audit for similar stringly-typed runtime routing/policy decisions
**Status:** ⬜ Not Started
**Category:** Architecture / Quality
**Effort:** Small

**Description:**
Perform a targeted audit for other places where runtime behavior is selected via raw dictionary/string keys in multiple callers instead of centralized typed policy.

**Tasks:**
- [ ] Scan application and battle-launch flows for duplicated string-key branching.
- [ ] Convert highest-risk duplicated policies to typed/shared resolvers.
- [ ] Add TODO links for any deferred findings.

---

## Ranked Gameplay

### 🟡 MEDIUM PRIORITY

#### Add Client-Side Prediction
**Status:** ⬜ Not Started
**Category:** Multiplayer / Simulation
**Effort:** Medium

**Description:**
The client currently operates as a pure renderer — it applies host snapshots but does not run local simulation. Adding client-side prediction would reduce perceived input lag by running `Simulation.Tick()` locally on the client with local inputs, then reconciling when the authoritative snapshot arrives.

**Tasks:**
- [ ] Run `Simulation.Tick()` on client with local commands
- [ ] Implement snapshot reconciliation (rollback + replay on mismatch)
- [ ] Handle misprediction correction (smooth visual snapping)
- [ ] Ensure deterministic parity between host and client simulation

**Notes:**
- The simulation layer is already pure and Godot-free, making client prediction feasible
- `DesyncDetector` already compares local vs host state — extend for prediction reconciliation
- Low priority until latency becomes a user-facing issue

---

## Units & Combat

### 🟡 MEDIUM PRIORITY

#### Formalize Damage Outcome Semantics (Hit vs Evade vs On-Hit Effects)
**Status:** ⬜ Not Started
**Category:** Units & Combat / Simulation Correctness
**Effort:** Medium

**Description:**
Current runtime behavior allows some on-hit effects to trigger even when the attack was evaded. This should be formalized so effect application is driven by explicit damage outcomes, not indirect checks.

**Concrete Example (Observed):**
- Wind pushback ability can apply knockback even when the target evades the hit.
- Current flow:
  - `SimDamage.Calculate(...)` returns `(damage=0, wasEvaded=true)` and emits `AttackEvadedEvent`.
  - Downstream on-hit ability trigger paths currently gate on `target.CurrentHp > 0` rather than explicit `wasEvaded == false`.
  - Result: no HP loss (correct) but knockback may still apply (incorrect).

**Why this matters:**
- On-hit effects should apply only on actual successful hit outcomes.
- We need a formalized damage outcome contract (e.g., Hit, Evaded, Immune/Blocked, ZeroDamageHit) to remove ambiguous behavior coupling.

**Scope Notes:**
- Not blocking current PR merge.
- Should be addressed before adding more on-hit mechanics to avoid repeated edge-case patches.

**Tasks:**
- [ ] Define a formal damage outcome/result contract used by melee + projectile pipelines.
- [ ] Route on-hit ability/effect triggers through outcome semantics (not HP-side checks).
- [ ] Ensure evaded hits do not apply knockback or other on-hit effects.
- [ ] Add deterministic regression tests covering evade + on-hit interaction paths.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`
- `scripts/csharp/Battle/Simulation/Subsystems/SimAbilityOrchestrator.cs`
- `tests/csharp/Simulation/Abilities/AbilityTargetedKnockbackTest.cs`

---

### 🟢 LOW PRIORITY

#### Refactor Character-Specific Animation Logic to Composition
**Status:** ⬜ Not Started
**Category:** Architecture / Units
**Effort:** Medium

**Description:**
Move character-specific animation logic (breathing, bobbing, attack styles) out of the base `SpriteCharacter2D5Component` class into composable components.

**Current State:**
- Breathing animation: `enable_breathing`, `breathing_amplitude`, `breathing_speed` in base class
- Bobbing animation: `enable_bobbing`, `bob_speed`, `bob_amplitude` in base class
- Attack styles: `attack_style`, `cycle_attack_styles` in base class
- UnitVisual passes these to visual component via property setters

**Problems:**
- Every unit carries unused animation parameters
- Base class grows with each new character type
- Adding new behaviors requires modifying shared code

**Proposed Solution:**
Use composition pattern - create separate animation behavior components:
- `BreathingAnimationComponent` - for cloud-like units (Puff)
- `BobbingAnimationComponent` - for floating units
- `AttackEffectComponent` - for single-frame sprite attack effects

Units attach only the components they need.

**Related Files:**
- `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs`
- `scripts/csharp/Battle/View/UnitVisual.cs`
- `scenes/battle/units/puff_3d.tscn`

---

### 🟡 MEDIUM PRIORITY

#### Evaluate Non-Hard-Lane Phase 2 Experiments (Post Virtual-Lanes/Roles)
**Status:** ⬜ Not Started
**Category:** Units & Combat / Spatial Design
**Effort:** Medium

**Description:**
Now that virtual lanes + tactical roles are in, run a structured evaluation of the remaining non-hard-lane options from the research doc before committing to another implementation track.

**Candidates To Evaluate:**
- Command cohesion layer (formation/order memory after first contact)
- Engagement cells (soft locality partition for targeting/aggro)
- Frontline tension bands (readability + light behavior weighting)
- Reinforcement routing rules (spawn pressure-sector assignment)
- Objective anchors / side-value injection (off-center tactical value without hard rails)
- Role-specific pursuit budgets (deeper role discipline beyond current prototype)

**Evaluation Method:**
- Define one simulation-first prototype slice per candidate
- Run 40/80/100-unit scenarios with identical seed setups
- Compare against current prototype baseline (virtual lanes + tactical roles)
- Keep changes behavior-only first; defer UI unless candidate passes baseline gates

**Pass Criteria (must beat baseline):**
- Flanks remain viable without collapsing into center aggro
- Midline vortex reduced (more meaningful use of side space)
- Frontline location/readability improves in large fights
- Spawn decisions remain meaningful without hard rails
- CPU/tick cost remains bounded and measurable

**Primary References:**
- `docs/design/lane-system-research-no-lane-identity.md` (Section 5-7 option inventory)
- `scripts/csharp/Battle/Simulation/` (targeting/behavior/movement hot paths)

---


#### Implement Directional/Cone Attack System
**Status:** 🟡 Partial (Cone Targeting Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add support for melee attacks that only hit in a forward cone/arc instead of a full circle. Useful for units with lunge attacks, tongue attacks, or other forward-facing abilities.

**Current State:**
- ✅ Cone-aware reachability/attack checks exist in simulation targeting (`SimTargeting` / `SimBehavior`)
- ✅ Unit data includes cone tunables used by targeting profiles
- ✅ Runtime now separates movement footprint (`NavigationRadius`) from damage contact (`HurtboxRadius`) for projectile/AoE fairness.
- ✅ Debug overlays now split engage gate vs damage shape vs navigation footprint for tuning visibility.
- ⬜ Front-only melee engage geometry and strict engage-vs-damage-shape startup sync are still in progress.

**Requirements:**
- Add `AttackHitboxShape` enum (Sphere, Box, Capsule) for melee behavior
- Add `AttackHitboxSize` vector for non-sphere shapes
- Modify melee hitbox spawning to use configured shape (box for narrow forward attacks)

**Example Use Cases:**
- Frog tongue: narrow forward box hitbox, ~45° targeting cone
- Dragon bite: wide forward arc, large box hitbox
- Standard melee: full circle (current behavior, AttackConeAngle = 0)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs` - attack range, behavior logic (formerly in Unit3D/MeleeUnit3D)
- `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitboxComponent.cs` - CreateBoxShape already exists

---

#### Implement Single Target vs Multi Target Attack System
**Status:** 🟡 Partial (Runtime V1 Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add system to differentiate between single target attacks and multi target/AoE attacks for units.

**Current State:**
- ✅ Spells have AoE via `spell_radius` (Fireball works)
- ✅ Units now support vector-driven multi-target recipient resolution in simulation (`single`, `area`, `line`, `chain`) with deterministic ordering.
- 🔄 Remaining unit-facing work is balance + visual telegraphing for multi-target attacks.

**Progress Update (2026-03-10):**
- ✅ Added grouped attack-vector contract across `UnitDefinition -> SimUnitTemplate -> UnitData`.
- ✅ Implemented deterministic recipient resolution for area (sphere/box/capsule), line corridor, and chain hops.
- ✅ Added simulation test coverage for target limits, deterministic tie-breaks, secondary death handling, and trigger-mode behavior.
- ✅ Follow-up fix (2026-03-11): explicit preset `TargetLimit` values are now preserved (`1` primary-only, `0` unlimited); preset default limits apply only when unset.
- ⬜ Remaining: visual indicators/telegraphs for AoE vectors and gameplay balance pass for multi-target damage tuning.

**Progress Update (2026-03-11):**
- ✅ Added combat spatial split wiring for navigation footprint vs hurtbox contact across simulation movement/projectile paths.
- ✅ Added independent debug overlays for engage range and damage-shape visualization.
- 🔄 Remaining: engage startup must align strictly with authored forward damage shapes for front-only melee readability.

**Requirements:**
- ✅ Define attack target type in unit data (single, multi, aoe)
- ✅ Implement multi-target selection logic for units
- ✅ Add AoE/splash damage radius for area attacks on units
- ⬜ Visual indicators for AoE attacks
- ⬜ Balance damage for multi-target vs single-target

**Notes:**
- Foundation for unit variety (e.g., dragons with breath attacks)
- Multi-target may need reduced damage per target
- Consider different AoE shapes (circle, cone, line)

---

#### Improve Unit Hitboxes
**Status:** 🟡 Partial (Runtime Channels Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Flesh out and refine unit hitboxes for better collision detection and combat interactions.

**Current State:**
- ✅ Runtime data now carries separate `NavigationRadius` and hurtbox fields (`HurtboxRadius`, `HurtboxHeight`, `HurtboxHorizontal`, `HurtboxOffset`).
- ✅ Projectile contact and AoE radius checks read hurtbox channels instead of movement spacing.
- 🔄 Remaining: finish per-unit authored shape tuning and align melee engage gating with forward attack shapes.

**Requirements:**
- Review current hitbox sizes and shapes
- Adjust hitboxes to better match visual models
- Test with various unit types (melee, ranged, large, small)
- Ensure proper interaction with projectiles and melee attacks

**Notes:**
- Important for combat feel and fairness
- May need different hitbox sizes for different unit types
- Consider separate hitboxes for collision vs damage

---


#### Add Death Animations for Units
**Status:** 🟡 Partial (Infrastructure Ready)
**Category:** Visual Polish
**Effort:** Medium

**Description:**
Create death animations for all unit types to improve visual feedback when units are defeated.

**Current State:**
- Infrastructure exists: `_die()` calls `_update_animation("death")` with 1.0s delay before queue_free()
- Missing: Actual animation frames/assets for death animations

**Requirements:**
- Design death animation for each unit type
- Create animation assets/frames
- Add fade-out or removal timing

**Notes:**
- Consider particle effects (blood, sparks, etc.)
- Should not block gameplay flow

---

#### Add More Summon Unit Cards
**Status:** ⬜ Not Started
**Category:** Content
**Effort:** Variable (per card)

**Description:**
Design and implement additional summon cards to expand unit variety.

**Requirements:**
- Design unit stats and abilities
- Create unit models/visuals
- Balance against existing units
- Create card art and data

**Notes:**
- Follow existing unit creation patterns
- Test balance before adding to decks

---

## Database & Data Layer

### 🟢 LOW PRIORITY

---

## Core Game Systems

### 🟡 MEDIUM PRIORITY

---

#### Investigate Mobile/Desktop Compatibility
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Platform
**Effort:** Medium

**Description:**
Review the current game setup to ensure compatibility with both mobile and desktop platforms.

**Areas to Investigate:**
- UI scaling and touch input
- Resolution and aspect ratio handling
- Input method differences (touch vs mouse/keyboard)
- Performance on mobile devices
- Export settings for each platform

**Notes:**
- Important to address early to avoid major refactoring later
- May need platform-specific adjustments

---


## Visual Polish

### 🟡 MEDIUM PRIORITY

#### Improve Card Visual UI
**Status:** 🟡 Partial (Structural Standardization Complete)
**Category:** UI/UX
**Effort:** Medium

**Description:**
Enhance the visual design of card display including layout, typography, and effects.

**Current State:**
- Full cards use one 3:4 presentation contract with Compact, Standard, and Large
  design-space tiers across battle, deck, quest, and reward contexts.
- Card inspection no longer exposes rarity or tactical-role badges.
- Card details expose Level, XP, Card Points, Core, and acquired Trait paths.

**Remaining Work:**
- Apply designer-authored frame, typography, icon, and spacing treatments.
- Validate long names, descriptions, costs, locked cards, max Level, unspent Card
  Points, and newly acquired Traits at every canonical size.
- Refine hover/selection feedback and card transition animations without
  introducing screen-specific card proportions.

**Notes:**
- Should work with existing 3D tilt effect
- Consider glow/highlight for playable cards

---

#### Add Battle-Start Cinematic Camera Nudge
**Status:** ⬜ Not Started
**Category:** Visual Polish / Camera
**Effort:** Small

**Description:**
Add a subtle automatic camera intro at battle start (slight zoom-in motion) to improve scene presentation before normal player camera control takes over.

**Requirements:**
- Trigger once at battle start (non-looping)
- Keep motion subtle and brief
- Preserve current gameplay camera framing after intro
- Avoid interfering with manual zoom/pan behavior

**Notes:**
- Keep this strictly cosmetic (no gameplay timing impact)
- Coordinate with existing battle initialization flow in `BattleScene`

---

### 🟢 LOW PRIORITY

#### Clean Up Non-Production VFX
**Status:** 🔄 In Progress
**Category:** Visual Polish / VFX
**Effort:** Medium

**Description:**
Audit existing VFX and replace or remove effects that don't meet production quality standards. Some VFX were created as quick placeholders and need polish or replacement.

**VFX to Review:**
- Fireball effects (explosion, trail, spell) - check if quality matches new wind_puff style
- Lightning strike - verify visual consistency
- Any placeholder effects still in use

**Quality Criteria:**
- Consistent color palettes per element (fire=orange/red, wind=cyan/white, etc.)
- Appropriate particle counts (not too sparse or too heavy)
- Smooth fade-in/fade-out curves
- Proper additive blending where appropriate
- Pooling configured correctly for performance

**Notes:**
- Wind puff impact (wind_puff_impact) can serve as reference for quality bar
- Consider creating a VFX style guide document
- Progress 2026-06-02: Added shared placeholder spell VFX/readability conventions for active elemental spells, including exact-radius ground indicators and dedicated line/single-target/area visuals. Remaining work is still production-quality VFX polish and art-direction pass.

---

## Audio

### 🟡 MEDIUM PRIORITY

#### Scope Production Audio Library and Integration Plan
**Status:** ⬜ Not Started
**Category:** Audio / Production Scope
**Effort:** Medium

**Description:**
Plan the production audio pass before adding individual one-off sounds. The goal is to avoid placeholder or low-quality audio churn and define what should be commissioned, sourced, made in-house, or deferred.

**Current State:**
- AudioManager infrastructure exists with battle music and basic UI/card sounds.
- Battle music stops on game end, but victory/defeat stingers are not sourced.
- No production library exists yet for unit, spell, projectile, building, or resource feedback.

**Requirements:**
- [ ] Define audio categories and priority order: victory/defeat, unit attack, unit movement, unit death, spell cast, projectile impact, structure damage, and mana/resource gain.
- [ ] Decide source strategy for each category: commission, licensed pack, generated/source-designed, or defer.
- [ ] Map each category to existing runtime events or identify missing event hooks.
- [ ] Define mixing/readability rules so frequent sounds do not overwhelm battle clarity.
- [ ] Convert approved categories into implementation TODOs only after the production direction is chosen.

**Notes:**
- Supersedes the previous granular audio TODOs until we are ready to source real audio.
- Do not add throwaway placeholder sounds just to satisfy tracker items.

---

## UI Revamp

### 🟡 MEDIUM PRIORITY

#### Revamp Battle HUD
**Status:** 🟡 Partial (Functional Scaffold)
**Category:** UI/UX
**Effort:** Medium

**Description:**
Redesign the in-battle HUD elements for better clarity and visual appeal.

**Current State:**
- Mirrored player and opponent status surfaces show portrait, name, HP, and mana.
- The four-card hand remains anchored at the bottom center with battlefield transparency preserved.
- Preparation shows a field-preparation heading and countdown, followed by a brief battle-start transition.
- A battle timer is visible as a designer-facing scaffold; ordinary destroy-the-summoner battles do not currently end on timeout.
- Offline battles expose pause and speed controls. Online battles remain live, hide speed controls, and provide a confirmed forfeit action; offline battles also provide confirmed forfeit.

**Remaining Work:**
- Apply the approved visual language, production icons/frames, typography, and spacing.
- Confirm responsive safe zones and supported aspect-ratio behavior with the wider canonical screen inventory.
- Decide whether any battle mode uses an actual time limit before giving the timer gameplay authority.

**Notes:**
- Must not obstruct battlefield
- Critical information should be immediately readable

---

#### Revamp Settings Screen UI
**Status:** 🟡 Partial (Functional Scaffold)
**Category:** UI/UX
**Effort:** Small

**Description:**
Redesign settings/options screen for better usability and visual consistency.

**Current State:**
- One shared categorized settings panel serves the standalone screen, campus system-menu overlay, and battle pause-menu overlay.
- Audio supports master, music, and SFX volume plus mute while unfocused.
- Display supports window mode, resolution, VSync, and frame-rate limit.
- Controls support rebindable camera actions, edge-pan behavior, and camera speed.
- Gameplay and accessibility expose only settings backed by runtime behavior, including reduced camera motion and UI scale.
- Profile-backed settings merge safely with existing values; input remaps persist locally.
- Escape on campus opens a paused system menu with Resume, Settings, and confirmed Quit Game. Battle menu behavior remains mode-aware.

**Remaining Work:**
- Apply final visual polish and designer-authored information hierarchy.
- Expand accessibility options only alongside real runtime support.
- Validate layout at the final target viewport and supported aspect-ratio range.

**Notes:**
- Functional but visually basic

---

### 🟢 LOW PRIORITY

---

## Summoner System

### 🟡 MEDIUM PRIORITY

#### Define Summoner Oaths (Trait-Backed Campaign Choices)
**Status:** ⬜ Not Started
**Category:** Summoners / Identity
**Effort:** Medium

**Description:**
Add summoner Oaths as explicit, high-impact choices shown to the player in progression flow. Each chosen Oath is persisted as a permanent trait entry on that summoner and drives future gameplay modifiers/conditions.

**Requirements:**
- Define initial Oath sets per summoner (2-4 options each)
- Specify where Oath choice appears in summoner progression flow/UI
- Persist selected Oath as trait data on the summoner profile
- Ensure Oath traits resolve through existing trait runtime hooks (modifiers/triggers)
- Document Oath interactions with Level Traits, Story Traits, and Ultimate Traits

**Related Files:**
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- `docs/features/summoners/progression-system.md`
- `docs/design/trait-tree-screen-flow-spec.md`
- `scripts/csharp/Meta/Progression/Core/ProgressionState.cs`

**Notes:**
- Oaths should be irreversible per summoner to reinforce Fateforged's permanent-choice identity.
- Deferred until campaign-fleshing pass; trait curation remains the active priority now.

---

#### Curate Summoner Trait Catalog (Replace Placeholder AI Traits)
**Status:** 🔄 In Progress (Summon Runtime Pass Complete)
**Category:** Summoners / Traits
**Effort:** Large

**Description:**
Replace placeholder AI-generated trait content in the current workstream with curated, intentional trait lines that reinforce summoner identity, army doctrine, and meaningful tradeoffs.

**Tasks:**
- [x] Audit summon-trait entries and replace placeholder/generated stat lines with curated v1 values
- [x] Define summon-focused v1 trait sheet with tier bounds and stat priorities (`docs/design/summon-traits-v1.md`)
- [x] Author summon trait-line tier chains with prerequisites in runtime catalog
- [x] Implement card-rarity gating for `Legion` tiers (Common: IV, Rare: III, Epic: II, Legendary: none)
- [x] Implement per-card/per-rarity trait value override plumbing while keeping shared trait names
- [x] Wire additive stat and spawn-count trait effects through card effective stats + simulation runtime
- [x] Add deterministic coverage for evaluator gating, override resolution, and spawn-count/runtime behavior
- [x] Produce summon-focused curated trait draft: `docs/design/summon-traits-v1.md`
- [x] Define non-summon per-summoner identity trait lines (doctrine/tradeoff focus)
- [ ] Author campaign-facing Ultimate/Oath trait candidates and validate permanence/exclusivity interactions
- [ ] Evaluate simplifying the base trait tree: if traits are non-interconnected, represent each series stage with a single swappable UI element and add a clear progression display model

**Related Files:**
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- `docs/features/summoners/progression-system.md`
- `docs/project/vision.md`
- `docs/design/summon-traits-v1.md`

**Notes:**
- This replaces generic placeholder trait generation as the active trait work item.
- Favor mechanics that create visible doctrine changes over flat stat inflation.
- Summon stat-tree foundation is now implemented; remaining design scope is identity/campaign-layer trait work.

---

#### Implement Summoner Special Abilities
**Status:** 🟨 In Progress (Phase 2/5)
**Category:** Summoners
**Effort:** Large

**Description:**
Implement the runtime system for summoner active/passive abilities after the curated trait catalog is locked.

**Notes:**
- Unified trait runtime/data scaffolding (Pass 2) is in place and wired into simulation/session.
- Trait points are now deferred spend for summoners/cards; level-up no longer forces immediate picks.
- Current priority: curated non-placeholder trait + oath design pass.
- Pending runtime: evaluator/triggers, offer rolling, and full validation matrix completion.
- Phase 3: Curated Level Traits (trait selection at level-up)
- Phase 4: Ultimate Traits (level 10 capstone abilities)
- Phase 5: End-to-end validation matrix + progression tuning
- Foundation is ready via TraitCatalog modifier system

---

## Developer Tools

### 🔴 HIGH PRIORITY

#### Audit and Restore the Item Developer-Tooling Contract
**Status:** ⬜ Not Started
**Category:** Developer Tools / Items / Interop
**Urgency:** High — blocks representative Inventory UI validation
**Ease:** Medium
**Scope:** Medium

**Description:**
Audit the item debug commands and their GDScript-to-C# adapter as one bounded
tooling surface. The current `/items_grant` command calls a nonexistent runtime
method, while the same untested adapter also owns list, equip, unequip, and clear
operations. Decide from the audit whether the existing console surface is worth
repairing or should be replaced with a fresh item-test fixture/tool; do not
accumulate one-off callable-name patches.

**Tasks:**
- [ ] Inventory every item developer command and map it to the actual
  Godot-exposed service method and argument contract.
- [ ] Reproduce and explain why `GrantItem` is not callable through the current
  adapter, including whether its optional binding parameter caused contract
  drift.
- [ ] Check grant, grant-all, list, equip, unequip, and clear independently so
  the first repaired command does not mask other broken operations.
- [ ] Choose and document the smallest maintainable surface: retain the console
  commands behind one verified adapter, or replace them with a focused item-test
  fixture/tool.
- [ ] Add contract/smoke coverage for every retained operation and fail with a
  useful developer-facing message when its service is unavailable.
- [ ] Prove the completed workflow by granting a catalog item, seeing it in the
  Inventory overlay, equipping it to the active summoner, and clearing the test
  state.
- [ ] Keep the separate gameplay ownership migration visible; do not silently
  redefine account-wide versus summoner-bound behavior as part of this audit.

**Placement Rationale:**
The item service owns inventory rules and mutation; its infrastructure adapter
owns GDScript/C# translation; debug tooling only invokes those capabilities.
Callable names, argument coercion, and fallback errors therefore belong at the
adapter boundary rather than inside the Inventory UI or scattered console
commands.

**Likely Files:**
- `scripts/debug/dev_console.gd`
- `scripts/infrastructure/services/items_api.gd`
- `scripts/csharp/Meta/Services/Items/ItemService.cs`
- item-service adapter and developer-command tests (new or extended)

**Related Bug:**
- `docs/tracking/bugs.md` — Item Debug Grant Command Calls a Missing Runtime Method

**Execution Order:**
Run this before further item-modal visual review that requires populated data.
Complete the audit and contract tests first, then make the repair/rebuild choice,
then use the restored tool to validate Inventory. The summoner-ownership
migration remains a later, larger implementation task.

### 🟢 LOW PRIORITY

#### Hide/Remove Debug Menu Before Release
**Status:** ⬜ Not Started
**Category:** Developer Tools / Release Prep
**Effort:** Trivial

**Description:**
The Debug Menu (`scripts/debug/debug_menu.gd`) is hidden by default but can be shown with ` or F12 in debug builds. Before release, either:
- Remove the autoload entirely, or
- Ensure it only activates with a specific dev flag/command

**Current Behavior:**
- Panel hidden by default, toggle with ` or F12
- Already disabled in release builds via `OS.is_debug_build()` check

**Notes:**
- Low priority - it's already hidden in release builds
- May want to keep for internal testing but hide from players in beta/early access

---

#### Campaign Level Editor (Dev-Only Tool)
**Status:** ⬜ Not Started
**Category:** Developer Tools
**Effort:** Large

**Description:**
A UI tool for developers to design and configure campaign battles without touching code.

**Purpose:**
- Allow designers to create/edit campaign battles without touching code
- Configure enemy decks, AI behavior, rewards, difficulty
- Test battles directly from the editor

**Requirements:**
- **Access**: Dev-only tool (not accessible to players)
- **Location**: Separate scene, accessible from main menu in debug builds or via dev console
- Drag-and-drop cards to build enemy deck
- Set deck size (no player limits for enemies)
- Configure AI behavior (aggression, card priority, play speed)
- Set battle metadata (name, description, difficulty)
- Define reward structure (fixed/choice/random cards)
- Set unlock requirements (which battles must be completed first)
- Preview/test battle
- Save battle definitions to `campaign_service.gd` or separate JSON files

**Notes:**
- Hardcoded decks in `campaign_service.gd` work fine for now
- Only needed when managing 20+ battles becomes cumbersome

---

## Card & Spell System

### 🟡 MEDIUM PRIORITY

#### Revisit the Card Rarity Concept
**Status:** ⬜ Not Started
**Category:** Card System / Rewards / Product Design
**Effort:** Medium

**Description:**
Decide whether card rarity still has a meaningful job in a game where scarcity
and prestige primarily come from exclusive choices, discovery, quests, and
permanently unavailable alternatives. Rarity and tactical-role badges are no
longer shown on card inspection surfaces, but the underlying metadata remains.

**Tasks:**
- [ ] Audit every place rarity currently affects card data, visuals, rewards,
  shops, progression, trait eligibility, and balance overrides.
- [ ] Decide whether rarity is removed, retained for a narrower mechanical job,
  or replaced by a concept that better expresses intentional scarcity.
- [ ] Plan migration of rarity-dependent behavior before removing its data.

#### Deprecate Command Spells
**Status:** 🟡 Partial (Archived from Active Roster)
**Category:** Card & Spell System / Design
**Effort:** Medium

**Description:**
Command spells (spells that give commands/orders to units) should be deprecated and removed from the game design. Evaluate which command spells exist and plan their removal or replacement.

**Progress Update (2026-06-04):**
- ✅ Rally, Guard, and Charge are archived in the card catalog and not part of the active spell expansion target.
- 🔄 Remaining work is cleanup: remove or quarantine command-specific schema/UI/docs once no active runtime path needs them.

**Requirements:**
- Audit existing command spell implementations
- Identify any command spells in card catalog
- Remove or replace with non-command alternatives
- Update any documentation referencing command spells

---

## Architecture & Code Quality

### 🔴 HIGH PRIORITY

#### Make Ranked Results and Rating Authoritative
**Status:** ⬜ Not Started
**Category:** Architecture / Multiplayer / Security
**Effort:** Large
**Urgency:** High before public ranked launch
**Ease:** Hard
**Scope:** Large

**Description:**
`RankingService` currently calculates and saves rating locally, then submits a client-authored result payload. Replace that trust model with a provider-neutral competitive authority whose secure implementation validates match identity/outcome and owns rating, history, and leaderboard writes.

**Tasks:**
- [ ] Define coarse-grained competitive operations for queue identity, match start, terminal result, and rating result; do not expose arbitrary rating setters.
- [ ] Make the authoritative match/session record own participants, result, end reason, and deduplication by match ID.
- [ ] Treat client rating/history as a cache/read model rather than the source of truth.
- [ ] Validate submitted deck, owned card instances, summoner, traits, and equipment against authoritative ownership at competitive match start.
- [ ] Remove matchmaking reliance on client-supplied rating once the authoritative adapter exists.
- [ ] Decide and implement the backend adapter separately; do not couple the domain contract to Nakama.

**Placement:**
Competitive commands/results belong in a dedicated meta multiplayer/application authority port because match validation and rating policy change together; transport/provider code remains in `Infrastructure/Backend`.

**Likely Files:**
- `scripts/csharp/Meta/Ranking/RankingService.cs`
- `scripts/csharp/Meta/Ranking/LeaderboardService.cs`
- `scripts/csharp/Meta/Matchmaking/MatchmakingService.cs`
- `scripts/csharp/Battle/Session/HostSession.cs`
- `scripts/csharp/Infrastructure/Backend/`

---

#### Introduce Atomic Commerce Authority Before Valuable Economy Launch
**Status:** ⬜ Not Started
**Category:** Architecture / Economy / Security
**Effort:** Medium
**Urgency:** High before real-money or server-valued currency launch
**Ease:** Hard
**Scope:** Medium

**Description:**
Shop purchase flows currently validate balances, spend currency, grant rewards, roll back failures, and increment purchase limits through separate local calls. Introduce a provider-neutral commerce authority that owns the entire purchase transaction and an idempotent purchase receipt.

**Tasks:**
- [ ] Define a stable purchase/transaction ID and a coarse-grained `PurchaseOffering` command.
- [ ] Atomically validate price, balance, ownership, purchase limit, currency spend, universal reward grants, and purchase receipt.
- [ ] Use provider-verified billing receipts for real-money fulfillment; never trust a client completion callback as proof of payment.
- [ ] Migrate shop rewards from dictionary grants to universal typed offers/claims.
- [ ] Keep catalog presentation local/cacheable while the authority owns price/version acceptance and valuable mutations.
- [ ] Remove manual spend/grant/refund sequences after the local transactional adapter is proven.

**Placement:**
Commerce commands and receipts belong in `Meta/Services/Commerce` and `Meta/Domain/Profile/Commerce`; billing/backend verification stays in `Infrastructure` because provider integration changes independently from purchase policy.

**Likely Files:**
- `scripts/csharp/Meta/Services/Shop/ShopService.cs`
- `scripts/csharp/Meta/Services/Economy/EconomyService.cs`
- `scripts/csharp/Meta/Services/Rewards/RewardService.cs`
- `scripts/csharp/Infrastructure/Billing/`
- `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs`

---

### 🟡 MEDIUM PRIORITY

#### Extend Authority Boundaries to Permanent Progression Commands
**Status:** ⬜ Not Started
**Category:** Architecture / Progression / Security
**Effort:** Large
**Urgency:** Medium; required before secure account progression
**Ease:** Hard
**Scope:** Large, execute incrementally by capability

**Description:**
After the battle authority proves the pattern, migrate other permanent player-value mutations from direct repository/service calls to capability-specific, provider-neutral commands. Keep read models and UI local; move validation and durable mutation behind replaceable local/remote adapters.

**Tasks:**
- [ ] Academy/campaign: authorize enrollment, activity/course completion, campaign choices, and durable progress flags through typed progression commands.
- [ ] Levels/upgrades: authorize card/summoner level-up, resource costs, and XP spending atomically.
- [ ] Traits/Oaths: authorize trait-point spending and irreversible choices against prerequisites/exclusivity.
- [ ] Items/equipment: authorize ownership-changing item operations; validate equipped configurations wherever they affect secure/competitive play.
- [ ] Summoner/card unlocks and collection grants: permit mutations only through universal reward or explicit progression commands, not public repository methods.
- [ ] Split ports by capability when transaction rules differ; do not turn `IProgressionAuthority` into a broad service locator.

**Placement:**
Each command belongs with the meta domain that owns its rule, while local and future remote adapters sit at the application/infrastructure boundary. Consumers depend on small capability ports rather than `ProfileRepository`.

**Likely Files:**
- `scripts/csharp/Meta/Services/Campaign/Handlers/AcademyProgressHandler.cs`
- `scripts/csharp/Meta/Services/Campaign/Handlers/CampaignProgressHandler.cs`
- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`
- `scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`
- `scripts/csharp/Meta/Traits/Unified/`
- `scripts/csharp/Meta/Services/Items/Handlers/ItemEquipmentHandler.cs`
- `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs`

**Notes:**
- Local settings, UI preferences, debug state, and deck-editor interactions do not need remote authority by themselves.
- Secure modes must validate the final loadout and ownership rather than trusting locally edited profile data.

---

#### Replace Runtime Entity `int` IDs with Typed Value Objects
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Medium

**Description:**
Simulation/runtime entity references still use raw `int` IDs in many places (`UnitId`, projectile IDs, target IDs, network IDs). This is brittle because ID domains can be mixed accidentally and invalid combinations are not caught at compile time.

Migrate runtime IDs to strongly typed value objects (for example `UnitInstanceId`, `ProjectileInstanceId`, `NetworkEntityId`) while keeping deterministic behavior and snapshot/network compatibility.

**Goals:**
- Prevent ID-domain mixups at compile time
- Improve readability of hot-path code (`MatchState`, targeting, combat, snapshots)
- Reduce "magic negative ID" conventions for special targets

**Migration Notes:**
- Prefer `readonly record struct` wrappers around `int` values
- Keep wire format/snapshots stable by converting at serialization boundaries
- Do incrementally (type aliases/adapters first, then deep replacement)

**Likely Files:**
- `scripts/csharp/Battle/Simulation/Data/MatchState.cs`
- `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- Session protocol/snapshot builders (`scripts/csharp/Battle/Session/...`)

---

#### Audit for Global-Coordination-over-Local-State Anti-pattern
**Status:** ⬜ Not Started
**Category:** Architecture / Design Principles
**Effort:** Medium

**Description:**
Audit the codebase for places where per-entity concerns are solved via global sweeps or cross-system event coordination instead of putting the data directly on the entity.

**The Anti-pattern:**
Instead of giving an entity the information it needs to manage itself, we infer its state from an unrelated system event and sweep all entities matching some filter. This is indirect, fragile, and relies on assumptions about system state that may not hold.

**Example (fixed):**
Units needed to stay inactive during spawn reveal. Instead of giving each UnitData a `SpawnTimer` (local state), the initial approach was to activate all inactive units for a team when the summoner's casting completed (global sweep triggered by unrelated event). This breaks if multiple casts overlap, units are inactive for other reasons, etc.

**Principle:** If data belongs to an entity, put it on the entity.

**Areas to Audit:**
- [ ] Any `foreach unit where team == X` sweeps that could be per-unit timers/flags
- [ ] Any signal handlers that modify entities they don't own
- [ ] Any activation/deactivation logic driven by external events rather than self-contained state
- [ ] Phase transitions that sweep-modify entities vs entities reacting to phase themselves

---

#### Refactor Reward System to Typed RewardSpec Classes
**Status:** 🟡 Partial (Universal Engine + Academy Consumer Implemented)
**Category:** Architecture / Flag Proliferation
**Effort:** Medium

**Description:**
Replace source-specific and dictionary-based reward paths with the universal typed reward engine. Every consumer should author typed offers and grants, resolve through the shared deterministic runtime, persist universal pending state, and render normalized view models.

**Current Problem:**
- Academy and campaign battles are migrated, but shop and remaining non-battle event/campaign reward consumers still use legacy contracts.
- The remaining consumers do not yet provide stable source occurrence/transaction identities required for safe universal idempotency.

**Progress Update (2026-03-09):**
- ✅ Mission completion reward flow now uses `BattleRewardSpec`-derived grants in `CampaignRewardHandler` (flexible rewards + campaign gold included)
- ✅ Pending flexible choice now persists stable `chosen_catalog_id` to prevent index drift across resume/claim
- ✅ RewardScreen current-battle resolution hardened for per-summoner campaign progress and pending-reward fallback
- ✅ Regression coverage added for claim flow, mission completion flow, and choice-drift scenario

**Progress Update (2026-07-25):**
- ✅ Added a universal typed reward engine with strict JSON definitions, deterministic per-summoner resolution, persisted previews and pending choices, atomic idempotent claims, and typed grant handlers.
- ✅ Migrated Academy activity and course rewards to the universal engine, including fixed, choice, pool, mixed, bundled, and no-immediate-reward support.
- ✅ Replaced Academy reward-specific UI interpretation with normalized reward view models.
- 🔄 Battle migration is now the gated `battle-progression-authority` initiative. Pass 2 adds the authority contracts, persistence/session wiring, fail-closed local adapter, and mapped test skeletons; behavior implementation awaits Pass 3 approval.
- ⬜ Remaining: Migrate shop, event, and campaign reward consumers after their source transaction/occurrence identities are defined.
- ⬜ Remaining: Remove the legacy reward contracts only after all consumers have migrated.

**Progress Update (2026-08-05):**
- ✅ Migrated campaign battles to universal reward offers with durable attempt identity, attempt-scoped XP, summoner-scoped first-clear rewards, frozen resolved snapshots, and atomic idempotent completion/claim transactions.
- ✅ Migrated `RewardScreen` to normalized authority output and removed the battle-only reward configuration, spec, handler, pending state, flags, and old save readers.
- ⬜ Remaining: Migrate shop and non-battle event/campaign sources after their transaction identities are defined.

**Ideal State:**
- All reward-bearing sources author `RewardOfferDefinition` records with typed option sources and grant bundles.
- `UniversalRewardRuntime` owns deterministic resolution; profile persistence owns immutable snapshots, pending selections, atomic claims, and receipts.
- Each grant type has one registered handler and an explicit ownership target.
- Screens consume normalized reward view models and submit only claim and option IDs.
- Legacy battle/shop/event/campaign reward models are deleted after their consumers migrate.

**Remaining Files to Refactor:**
- Shop and non-battle event/campaign source catalogs that still author legacy rewards

---

### 🟢 LOW PRIORITY

#### Unify Catalog Localization-Key Validation
**Status:** ⬜ Not Started
**Category:** Architecture / Localization
**Effort:** Medium

**Description:**
Localization validation currently covers literal `Loc.t(...)` calls and event-catalog keys, but it is not a unified contract across authored catalogs. Cards still expose authoritative display strings rather than localization keys, so UI code must not synthesize unsupported `card.<id>.*` keys.

**Tasks:**
- [ ] Decide which authored catalogs require localization keys instead of authoritative display strings.
- [ ] Introduce a shared typed localization-key contract for those catalogs.
- [ ] Validate every authored key against the fallback locale in one catalog-integrity suite.
- [ ] Enumerate or eliminate remaining dynamically constructed localization-key patterns.

---

#### Audit Remaining Dynamic GDScript/C# Property Access
**Status:** ⬜ Not Started
**Category:** Architecture / Interop
**Effort:** Small

**Description:**
The old broad PascalCase/snake_case helper TODO is stale: most referenced files no longer carry that duplicated fallback pattern. Keep a smaller audit item for the few remaining dynamic property reads at interop boundaries.

**Current State:**
- The previously listed broad duplication across combat, spell, visual, and GDScript spatial files is no longer present.
- Remaining dynamic property access appears limited and should be handled case-by-case instead of introducing a broad helper prematurely.

**Tasks:**
- [ ] Audit remaining `Node.Get(...)` property reads at GDScript/C# boundaries.
- [ ] Prefer typed interfaces when the target type is stable.
- [ ] Add a tiny helper only if at least 3 active call sites need the same fallback behavior.

**Known Candidate:**
- `scripts/csharp/Battle/View/Spawning/SpawnPositionCalculator.cs`

**Notes:**
- This should stay small unless new duplication appears.

---

#### Remove Remaining Legacy Compatibility Paths (EventSequencer/Dialogue/BattleContext)
**Status:** 🟨 Partially Complete
**Category:** Architecture / Cleanup
**Effort:** Medium

**Description:**
Remove compatibility-only runtime paths that preserve deprecated behavior and are no longer aligned with current architecture rules.

**Tasks:**
- [x] Removed EventSequencer entirely with the Narrative Director replacement (2026-08-05).
- [x] Removed DialogueManager and all deprecated UI registration flows (2026-08-05).
- [ ] Remove `BattleContext` authority/level-cap compatibility bridges and service-fallback paths that exist only for legacy wiring.

**Related Files:**
- Narrative compatibility paths are gone; the remaining BattleContext bridge audit keeps this item open.
- `scripts/application/battle_context.gd`

---

## Performance

### 🔴 HIGH PRIORITY

#### Throttle Hot-Path Work in _PhysicsProcess
**Status:** 🔄 In Progress
**Category:** Performance / Units
**Effort:** Medium

**Description:**
Every unit runs targeting + behavior + 3+ spatial grid queries per physics frame. This becomes the primary performance bottleneck at scale (40-100 units).

**Current Behavior:**
- `UnitVisual._PhysicsProcess()` runs every frame for every active unit
- Simulation movement still performs frequent neighbor/avoidance work in dense battles.
- `UnitMovement.CorrectOverlaps()` and movement-neighbor queries can add extra hot-path work.
- Render priority recalculates every frame even when position unchanged

**Proposed Fix:**
- Throttle steering queries to run every 2-3 frames instead of every frame
- Cache steering results between updates
- Consolidate multiple `GetUnitsInRadius` calls into single query where possible
- Skip render priority calculation when position unchanged

**Progress Update (2026-03-08):**
- ✅ Landed simulation-side movement/perf wins (ORCA neighbor capping + context buffer reuse)
- 🔄 Remaining: complete visual/update-frequency throttling and render-priority skip path

**Related Files:**
- `scripts/csharp/Battle/View/UnitVisual.cs` (visual shell, formerly Unit3D.cs)
- `scripts/csharp/Battle/Simulation/Movement/ContextSteering.cs`
- `scripts/csharp/Battle/Simulation/Movement/MovementNeighborQuery.cs`
- `scripts/csharp/Battle/Simulation/Movement/OverlapCorrection.cs`
- `scripts/csharp/Battle/Simulation/Movement/OrcaAvoidance.cs`

---

### 🟢 LOW PRIORITY

#### Introduce Battle Composition Root + Dependency Injection
**Status:** ⬜ Not Started
**Category:** Architecture / Maintainability
**Effort:** Medium

**Description:**
Battle systems still rely on service-locator style autoload lookups (`/root/...`) inside runtime systems (EntityManager, BattleScene, session wiring). This couples systems to scene tree naming and makes tests harder to isolate.

**Proposed Fix:**
- Define small service interfaces for battle dependencies (`IVfxService`, `IAudioService`, etc.)
- Resolve autoload nodes once in a composition root (`BattleScene`/factory layer)
- Inject typed dependencies into runtime systems instead of looking up autoloads from inside those systems
- Keep dynamic `Call()` isolated behind adapter classes at interop boundaries

**Related Files:**
- `scripts/csharp/Battle/View/BattleScene.cs`
- `scripts/csharp/Battle/View/EntityManager.cs`
- `scripts/csharp/Battle/Session/BattleSessionFactory.cs`

**Notes:**
- Do this incrementally during normal battle refactors, not as one large rewrite.
- Team decision: use lightweight, feature-scoped DI (small interfaces + explicit `Init(...)` injection from `BattleScene`).
- Team decision: avoid a large global `GameServices` container; it can become another service locator.
- Team decision: apply DI first to high fan-out battle services (`VFX`, `Audio`, session-related adapters), then expand only when it reduces duplication.

---

#### Refactor Hard-coded /root/ Paths to Explicit Dependency Access
**Status:** ⬜ Not Started
**Category:** Architecture / Maintainability
**Effort:** Large

**Description:**
Hard-coded `/root/...` lookups still appear across runtime, meta, and service wiring. This is fragile and creates hidden dependencies, especially when battle systems pull global services directly.

**Current Behavior:**
- `get_node("/root/Campaign")`, `get_node("/root/ProfileRepo")`, etc.
- Dynamic path construction: `get_node_or_null("/root/" + signal_source)`
- If autoloads are renamed, lookups fail silently

**Proposed Fix:**
- For battle runtime code, prefer the composition-root/DI plan above.
- For meta/UI code, centralize repeated autoload access only where it reduces duplication without creating another global catch-all.
- Migrate one service area at a time during natural refactors.

**Notes:**
- Current scan is much smaller than the old 88+ estimate, but still broad enough to defer.
- Defer until natural refactoring or dedicated cleanup sprint
- Keep this aligned with `Introduce Battle Composition Root + Dependency Injection`; avoid replacing `/root/...` with a different global service locator.

---

## Multiplayer

### 🟢 LOW PRIORITY
