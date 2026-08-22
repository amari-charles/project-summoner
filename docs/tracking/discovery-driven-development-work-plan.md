# Discovery-Driven Development Work Plan

**Status:** In progress; Card-native Core contract and first representative path implemented, broader content and domain states remain
**Date:** 2026-08-22
**Source of truth:** [Discovery-Driven Development](../design/discovery-driven-development.md)

## Outcome

Replace the current fully exposed global-tree flow with progression that combines
automatic level growth, banked owner-bound points, acquired traits, compact
per-trait paths, configurable world actions, and permanent authored choices.

The replacement must prove one Summoner trait and one Card end to end before it
is generalized across the catalog.

## Current Foundation To Reuse

- Summoner XP can already apply multiple automatic levels and bank one Trait
  Point per level.
- Each `CardInstance` already owns independent level, XP, upgrades, and unspent
  points.
- Summoners already receive a level-based health and mana multiplier; its exact
  curve and affected stats need product tuning rather than a second growth
  system.
- Shared trait evaluation, view-model, canvas, and confirmation prototypes exist.
- Current trait-tree state supports owned, available, locked, and derived
  permanent closure for authored exclusive branches. Durable hidden/revealed
  state and optional closed-path inspection are not yet implemented.
- Card XP now applies every earned level automatically, carries remainder XP,
  and banks globally configured Card Points per level. The obsolete manual
  level-up service surface still needs final technical removal.

## Required UI Changes

### Summoner surfaces

- Keep level, XP, unspent Trait Points, owned-trait icons, equipment, and
  inventory on the Summoner overview.
- Replace the current route into one global Summoner tree with a trait collection
  and selected-trait development view.
- Make owned trait circles the primary navigation into development and remove
  the separate `Upgrades` button/global-tree route.
- Open the selected trait tree as a large overlay over the Summoner screen.
- Start without a second trait list or switcher inside the overlay; close and
  select another trait from the Summoner screen. Reconsider only if testing shows
  repeated backtracking is cumbersome.
- Support roughly zero to twenty owned traits as a multi-row wrapping icon grid
  without displaying every path at once. Give the section more vertical height,
  then scroll when the visible grid is full. Show an intentional empty state
  when the Summoner owns none.
- Render atomic traits consistently as valid one-node trees.
- Do not add a separate `+N` or `View All` collection route for overflow.
- Move unspent-point feedback into the Traits area without implying that a valid
  spend is always available.
- Repeat the available Trait Point count inside the selected-trait overlay so
  the player can evaluate costs without returning to the overview.

### Card surfaces

- [x] Show card level, XP progress, banked Card Points, and acquired-trait icons in
  Card detail/Collection.
- Replace manual card-level purchase UI with automatic level/point feedback.
- [x] Present `Core` as the first selectable circle beside acquired trait circles;
  clicking any circle opens the same large tree overlay focused on that path.
- [ ] Show which unique card instance is being developed when the player owns
  multiple copies of the same catalog card.
- [x] Allow a world-granted trait to appear as a new selectable entry without
  relaying out an enormous card graph.

**Progress (2026-08-22):** Card details now own a wrapping Core-plus-acquired-trait
list and open the shared selected-path overlay without navigating to the legacy
global card tree. Core membership is explicitly authored per Card rather than
drawn from the global level-up pool. Fire Wisp provides the first representative
behavior-led permanent fork; closed alternatives disappear from the normal
view. Other Cards still require authored Core content, and durable hidden,
revealed, and acquisition-provenance states remain in later bundles.

### Shared development view

- Reuse one bounded path renderer for Summoner traits, Card Core, and Card
  traits.
- Render hidden opportunities as absent by default.
- Visually distinguish known-locked, available, acquired, and permanently
  closed opportunities when closed-path display is explicitly enabled.
- Hide closed paths by default and support a `Show Closed Paths` toggle rather
  than leaving permanently lost branches as ordinary visual clutter.
- Keep an access-unlocked node in the available state when it is unaffordable;
  communicate missing points/materials in the cost and action treatment.
- Give newly revealed or unlocked nodes an attention marker until the player
  inspects them.
- Show exact effects, access requirements, configured costs, acquisition method,
  source/provenance, and permanently closed alternatives in details.
- Keep the tree as the dominant surface. Show node details/actions in a nearby
  contextual popover on hover or focus, pin it on click/tap, and support the
  same information through keyboard/controller focus.
- Keep effect descriptions out of the overlay header; it identifies the trait
  and point balance while each node owns its own effect text.
- Support compact linear tracks, small forks, and larger authored paths without
  requiring one universal unbounded auto-layout canvas.
- Communicate branching and exclusivity through connector lines and node layout.
  A confirmation may show the selected result, cost, and permanence, but does
  not repeat or enumerate the alternative branch that will close.
- If an opportunity belongs to a ritual, route the player to or describe the
  physical ritual rather than offering a duplicate purchase in the path view. A
  later Track action may provide navigation without remote acquisition.

### World and feedback surfaces

- Distinguish `revealed`, `unlocked`, `acquired`, and `transformed` feedback in
  quest, reward, and ritual presentation.
- Use a compact toast for reveal/unlock feedback and the larger generic reward
  presentation for acquisition/transformation.
- Show point costs inside ritual initiation when the ritual acquires the result.
- Add level and point-award reveals for both Summoners and participating Cards to
  the unified post-activity results flow.
- Notify the player when a new trait/path becomes available without revealing
  content that remains hidden.

## Implementation Work Bundles

### 1. Representative content and growth contract

**Urgency:** High  
**Ease:** Medium  
**Scope:** Medium

**Included work:**

- Author one Summoner trait containing at least one point-funded development and
  one world-driven development.
- [x] Author one Card Core path with a permanent fork.
- Author one acquired Card trait with a permanent fork.
- Define Card-specific automatic stat-growth curves and later mana-cost tuning
  rather than applying one universal Card formula.
- Tune the existing Summoner `5% per level` health-and-mana behavior later; both
  stats remain part of automatic level growth.
- Assign point costs, material costs, free acquisitions, hidden states, and
  branch closures to the examples.
- Walk through spending every point before a later hidden trait is discovered;
  preserve the intentional no-refund/no-respec consequence.

**Progress (2026-08-22):** Fire Wisp now has an explicit visible Core. Its first
choice is between spawning two individually weaker wisps and condensing power
into one stronger wisp; each choice opens a supporting second node and
permanently closes the other branch. This proves the card-native membership,
behavior-first fork, supporting-stat node, and closure contract. Values are
representative tuning, not final balance.

**Rationale:** The examples determine the smallest honest data and UI contract.
Building generic infrastructure first would encode another speculative tree.

**Likely files:**

- `docs/design/discovery-driven-development.md`
- new representative content spec under `docs/design/`
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- Card definitions/catalog files selected by the example

### 2. Development opportunity domain and persistence

**Urgency:** High  
**Ease:** Hard  
**Scope:** Large

**Included work:**

- Model owner, path, opportunity, prerequisites, visibility, configured costs,
  acquisition effects, provenance, exclusivity, and path ordering explicitly.
- Add durable hidden/revealed state and optional closed-path inspection; owned,
  available, locked, and derived permanent closure now exist for authored Core
  branches.
- Persist revealed/unlocked/closed opportunities separately from acquired
  effects; do not infer durable world discovery solely from current UI state.
- Persist an acquisition record per owned upgrade instead of only an ordered ID
  list. At minimum, record path, source/occurrence, owner level at acquisition,
  acquisition sequence, and configured cost snapshot.
- Keep Card development keyed by `CardInstanceId`, never catalog ID alone.
- Represent Summoner trait ownership separately from upgrades belonging to that
  trait.
- Build capped Card state as a temporary effective projection. Never mutate,
  downgrade, or reconstruct the permanent Card instance itself merely to enter
  a capped battle.

**Placement rationale:** Permanent progression rules and owner state belong in
`Meta/Domain/Profile` and `Meta/Services/Traits`; authored definitions belong in
`Infrastructure/Data`. Quest and ritual code consume these contracts but do not
own their rules.

**Likely files:**

- `scripts/csharp/Meta/Services/Traits/TraitTreeModels.cs`
- `scripts/csharp/Meta/Services/Traits/TraitTreeEvaluator.cs`
- `scripts/csharp/Meta/Domain/Profile/Collection/CardInstance.cs`
- `scripts/csharp/Meta/Domain/Profile/Summoners/SummonerInstance.cs`
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- persistence DTO/converter and profile repository files

### 3. Authoritative level, point, and acquisition operations

**Urgency:** High  
**Ease:** Hard  
**Scope:** Large

**Included work:**

- [x] Make Card XP apply every affordable level automatically, carry remaining
  XP, and bank a globally configured number of Card Points per level.
- Apply configured automatic Card stat growth and validate existing Summoner
  level growth against the approved curve.
- Replace the current point-only `spend_trait_point` operation with a typed
  acquisition command that validates opportunity state, owner, costs,
  prerequisites, and exclusivity atomically.
- Support free, point-only, material-only, sacrifice, and combined costs.
- Support reveal, unlock, acquire, and transform effects independently of the
  world source that requested them.
- Return normalized success/rejection results suitable for UI and rewards.

**Placement rationale:** These mutations belong to capability-specific Meta
progression services because costs, ownership, and permanent branch closure must
commit together. UI, Quest, and Ritual layers request commands and render their
results.

**Likely files:**

- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`
- `scripts/csharp/Meta/Services/Cards/CardService.cs`
- `scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`
- `scripts/csharp/Meta/Services/Traits/TraitTreeService.cs`
- `scripts/infrastructure/services/card_service_api.gd`
- `scripts/infrastructure/services/summoner_progression_api.gd`
- `scripts/infrastructure/services/trait_tree_api.gd`

### 4. Modular development UI replacement

**Urgency:** High  
**Ease:** Medium  
**Scope:** Large

**Included work:**

- Convert the shared canvas from a whole-owner tree into a bounded selected-path
  renderer, retaining usable node-state and confirmation behavior.
- Build reusable owner/path selection and opportunity-detail components.
- Rework Summoner navigation from global `Upgrades` tree to owned traits.
- Rework Card detail navigation to Core plus acquired traits.
- Add the required empty, hidden, locked, available-but-unaffordable, acquired,
  optionally shown closed, newly changed, and irreversible-confirmation states.
- Keep final visual styling replaceable for the external UI design pass.

**Progress (2026-08-18):** The Summoner overview now uses owned trait circles as
the entry point, the separate global `Upgrades` button is disconnected, the
collection wraps in a taller scrolling region, and a selected trait opens in a
large overlay with a bounded graph. The first fixed side inspector was rejected
after reference research; node details now use a contextual popover so the tree
keeps the full content area.
This is an initial integration against the existing three-state service. Card
Core/trait entry, the full discovery-state model, closed-path visibility, newly
changed treatment, configurable costs, and representative authored paths remain.

**Placement rationale:** Shared path/detail components belong under
`scenes/meta/components` and `scripts/meta/components`; owner-specific screen
composition remains under `meta/screens` because Card and Summoner navigation
change independently.

**Likely files:**

- `scenes/meta/components/` and `scripts/meta/components/trait_tree_canvas.gd`
- `scenes/meta/screens/trait_tree_screen.tscn`
- `scenes/meta/screens/card_trait_tree_screen.tscn`
- `scripts/meta/screens/trait_tree_screen.gd`
- `scripts/meta/screens/card_trait_tree_screen.gd`
- `scripts/meta/screens/summoner_screen.gd`
- `scripts/meta/modals/card_detail_modal.gd`

### 5. Quest, ritual, reward, and result integration

**Urgency:** High  
**Ease:** Hard  
**Scope:** Medium

**Included work:**

- Add typed progression effects that let any world source reveal, unlock,
  acquire, or transform a targeted Summoner trait or Card-instance path.
- Route Quest outcomes through those generic effects instead of source-specific
  trait mutation.
- Define Ritual initiation as an atomic configured transaction when it acquires
  a result, including any point/material costs.
- Add normalized reveal/acquisition feedback to the universal reward/result
  presentation.
- Prove both preferred patterns and exceptions: Quest unlock, Ritual acquire,
  Quest acquire, and Ritual unlock.

**Placement rationale:** Generic development effects belong with Meta
progression/rewards. Quest and Ritual definitions author effects; their UI does
not own permanent mutation.

**Likely files:**

- `scripts/csharp/Meta/Services/Campaign/Quests/`
- `scripts/csharp/Meta/Services/Rewards/`
- `scripts/csharp/Meta/Services/Traits/`
- future Ritual domain/application files
- reward and results scenes under `scenes/meta/`

### 6. Battle integration, migration, and cleanup

**Urgency:** Medium  
**Ease:** Hard  
**Scope:** Medium

**Included work:**

- Apply automatic base growth and acquired developments consistently when
  building battle loadouts.
- Implement Card level caps as temporary effective-level/stat projections while
  preserving every acquired Core and trait upgrade. Exact projection mechanics
  remain a dedicated TODO.
- Validate unique Card-instance progression in deck, save/load, and competitive
  loadout paths.
- [x] Remove the manual Card level-up modal from normal player-facing flows.
- Remove the obsolete manual level-up service methods and
  level-up-resource-cost purchase hooks after callers and compatibility needs
  are audited.
- Retire the fixed global-tree and separate one-off-tab routes after all callers
  use the modular replacement.
- Update debug commands and discard obsolete save compatibility rather than
  maintaining dual progression systems.

**Placement rationale:** Effective battle power is consumed by Meta loadout and
Battle session construction; deprecated Meta UI/services are removed only after
the new authority path is proven.

**Likely files:**

- `scripts/csharp/Meta/Services/LevelCapService.cs`
- battle loadout/session builders
- `scripts/debug/dev_console.gd`
- affected progression tests

## Verification Required Across Bundles

- Automatic multi-level Card and Summoner XP with remainder carry.
- One point granted per level and points bank safely.
- Small automatic growth applies exactly once and respects caps.
- Two copies of one catalog card retain independent progression.
- Hidden opportunities never leak through view models or UI.
- Every configurable world-action effect transitions to the correct state.
- All cost combinations commit atomically with acquisition.
- Permanent sibling branches close and cannot be reopened.
- Atomic traits render as valid one-node trees.
- Ten acquired Card traits and twenty Summoner traits remain navigable without
  rendering one enormous graph.
- Save/load preserves revelation, access, acquisition, closure, costs paid, and
  point balances.

## Recommended Execution Order

1. Approve the representative Summoner trait, Card, and automatic growth curves.
2. Implement the domain/persistence contract against those examples.
3. Implement authoritative automatic leveling and acquisition transactions.
4. Build the modular selected-path UI on the real read model.
5. Prove Quest and Ritual integrations end to end.
6. Resolve battle-cap behavior, migrate callers, and delete legacy flows.
7. Expand the catalog only after the complete slice is playable and reviewed.
