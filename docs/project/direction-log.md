# Fateforged Product Direction Log

## Purpose

This document records meaningful changes in Fateforged's product and game direction: what was decided, why it changed, and what earlier direction it replaced.

It is not a release changelog, implementation diary, or list of every local design choice. Public release notes belong in [changelog.md](changelog.md), technical progress belongs in [development-history.md](development-history.md), and the current intended behavior belongs in the relevant product or design document.

## What Belongs Here

Record an explicitly approved decision when it does one or more of the following:

- Changes the structure of the player experience or core game loop.
- Introduces, replaces, or retires a major feature or player-facing flow.
- Changes which feature or system owns an important behavior or rule.
- Establishes a constraint that affects multiple features, screens, or future work.
- Changes progression, rewards, economy, matchmaking, content structure, or another broad product model.
- Supersedes prior direction in a way that future contributors may otherwise misunderstand.

A useful test is: **will someone later need to know why the game is structured this way, rather than merely how it was implemented?** If yes, the decision likely belongs here.

## What Does Not Belong Here

Do not record:

- Routine implementation details or file-level architecture choices.
- Refactors that preserve product behavior.
- Isolated bug fixes.
- Small spacing, positioning, color, tuning, or placeholder-art changes.
- Temporary experiments that have not been accepted as direction.
- Every pull request or feature implementation milestone.
- Ideas the user has not explicitly approved.

## Authority and Maintenance

- The user must approve the underlying product decision before it is recorded as accepted direction.
- Update this log when an approved direction is introduced, revised, superseded, or retired.
- Preserve old entries as historical records. A later entry should name the decision it supersedes instead of rewriting history.
- Link to the current design document whenever one exists. The design document remains authoritative for current behavior.
- Link to relevant pull requests when they help identify when the direction entered the product, but do not treat implementation alone as proof of product intent.
- Keep entries concise and focused on the decision and its consequences. Detailed implementation plans belong in technical documentation.

## Entry Format

```markdown
## YYYY-MM-DD — Decision title

**Status:** Accepted | Superseded | Retired
**Areas:** Player Journey, Academy, Battles, Decks, Progression, UI, etc.

### Decision

State the approved direction in direct language.

### Context

Explain the problem, previous direction, or product pressure that led to the decision.

### Consequences

- List the important constraints or follow-up implications.
- Focus on effects across features and future work, not file-level implementation.

### Supersedes

Link to an earlier direction-log entry when applicable, or write `None`.

### References

- Current design document
- Relevant tracking task
- Relevant pull request or implementation milestone, when useful
```

## Decision History

Entries are newest first. Historical backfill should include only decisions that can be supported by explicit user direction or authoritative product/design documentation.

## 2026-08-24 — Define the Academy as a world before authoring the opening

**Status:** Accepted
**Areas:** World, Academy, Campus, Content Sequence, Quests

### Decision

Defer detailed opening-sequence work. The next product-design focus is the
Academy as a physical place: its location in the wider world, history, scale,
interactable-place roster, districts, and layout.

Existing graybox and legacy labels—including Crystal Chamber, Training Grounds,
Mission Hall, Class Hall, and Dorms—are fungible. They do not become required
world canon merely because a document or scene already names them.

### Context

The runtime now provides a bounded walkable campus and the systems needed to
host characters and content, but the world those systems inhabit has not been
defined. Designing an introduction against provisional geography would allow a
temporary sequence to dictate the larger Academy by accident.

### Consequences

- Academy geography and place functions are settled before a final campus map.
- Interactable places must earn their physical presence instead of duplicating
  persistent UI tools.
- The opening remains deferred and will later be authored against accepted
  world details.
- Placeholder scene placement and inherited building names are not canon.

### Supersedes

The prior sequencing decision that made representative opening content the next
product task. It does not reject the accepted quest foundation or require its
current placeholder opening content to be removed.

### References

- `docs/design/academy-world-definition.md`
- `docs/design/walkable-academy-hub.md`
- `docs/tracking/todos.md`

## 2026-08-24 — Retire campaign and Caravan progression in favor of quests

**Status:** Accepted
**Areas:** Player Journey, Academy, Quests, Encounters, Shop, Architecture

### Decision

Fateforged has no player-facing campaign, campaign map, or campaign-run
progression model. The Caravan is retired as a shop and progression concept.
Mr. and Mrs. Merriweather remain in the world as the owners of the permanent
Campus Shop; they are not traveling Caravan merchants.

The progression history is explicit: the campaign map and Caravan were replaced
by an Academy course structure, and the Course Flow was then replaced by
professor-led quests and reusable generic encounters. Academic subjects may
still provide quest content, but a course system does not own enrollment,
progression, activity launch, rewards, or navigation.

### Context

The move to the Academy first made the campaign graph and run-bound Caravan
obsolete. The later quest rearchitecture removed the Course Flow as the
replacement progression owner, but the retirement of the original campaign and
Caravan was not recorded clearly. That gap left old design documents and
internal `Campaign` names looking authoritative after their product model had
already been superseded.

### Consequences

- Runtime routes, catalogs, events, UI, localization, narrative hooks, and save
  fields that exist only for the campaign map or Caravan are deleted without a
  compatibility path.
- The Campus Shop is the sole current shop location owned by the Merriweathers.
- Live quest and encounter capabilities currently housed under
  `CampaignService` remain behaviorally necessary, but the mixed service name
  and ownership are architecture debt. A bounded review must separate live
  quest/encounter responsibilities from obsolete campaign/course APIs before
  the services receive canonical names.
- Wholly superseded campaign, Caravan, and Course Flow documents move under
  `docs/archive/`; mixed documents are revised so active guidance contains only
  current intent. Historical direction-log entries remain in place and are
  superseded by this entry rather than rewritten.

### Supersedes

- The campaign-map and visible-Caravan direction recorded in the 2026-01-19
  ideation session and campaign structure documents.
- The Course Flow as the successor progression owner; see
  `2026-08-16 — Replace the old Course Flow with authoritative typed quest steps`.

### References

- `docs/design/quest-system.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`
- `docs/tracking/todos.md`
- PR `#379`

## 2026-08-23 — Establish a high-fantasy world with pre-industrial material culture

**Status:** Accepted
**Areas:** Worldbuilding, Academy, Environments, Art Direction, UI

### Decision

Fateforged is a high-fantasy world with a mystical tone and a broadly
pre-industrial material culture rather than a strictly historical medieval
setting. Magic is an established part of society while retaining a sense of
age, mystery, and significance, and the wider world contains regions shaped by
extreme elemental biomes and weather.

### Context

Defining the setting as merely fantasy left its technology, infrastructure, and
environmental language unclear. A pre-industrial material culture lets the
Academy support sophisticated engineered and magical infrastructure, including
drainage and underground service spaces, without making the world modern or
requiring strict medieval accuracy.

### Consequences

- Future environments, props, clothing, and UI may draw from multiple
  pre-industrial periods rather than one exact historical culture.
- Magic may perform functions that would otherwise require modern technology.
- Academy underworks, drainage systems, and similar infrastructure fit the
  setting when presented through its engineered and magical culture.
- Wider-world regions may be defined by strong elemental conditions, such as
  frozen plains or volcanic landscapes.

### Supersedes

None.

### References

- [The World of Fateforged](../lore/world.md)
- [UI Design Questionnaire Response](../archive/suspended-progression-models-2026-08/art/commissions/ui-design-questionnaire-response.md)

## 2026-08-23 — Restore the carousel for switching summoners

**Status:** Accepted
**Areas:** Summoners, UI, Art Direction

### Decision

Unlocked summoners are switched through an animated, wrap-around carousel. The
focused summoner is presented at full size with neighboring summoners visible
at reduced scale and opacity, and the player confirms the summoner they want to
make active.

### Context

The roster list was readable and reusable, but could be assembled largely from
generic UI assets. The original carousel provides a more distinctive character
showcase and gives the commissioned UI artist meaningful room to establish the
presentation.

### Consequences

- Summoner Switch is a carousel rather than a scrollable roster.
- The carousel uses character-focused displays and does not change the separate
  starting-summoner selection requirements.
- Switching wraps in both directions and keeps the adjacent choices visible.

### Supersedes

The Summoner Switch roster consequence recorded in
`2026-08-22 — Present starting summoners as characters rather than cards`.

### References

- `docs/features/summoners/README.md`
- `docs/archive/suspended-progression-models-2026-08/art/commissions/ui-design-handoff/README.md`

## 2026-08-23 — Cap player-built decks at 12 cards

**Status:** Accepted
**Areas:** Cards, Decks, Battles, UI

### Decision

Player-built decks contain no more than 12 cards. Authored enemy decks and fixed
encounter loadouts may continue to define their own composition rules.

### Context

Earlier project material described decks of up to 30 cards. The smaller cap
makes deck construction more selective and better supports the single-use-card
model, where every inclusion and deployment should matter.

### Consequences

- Deck creation, editing, validation, and count displays use 12 as the standard
  player maximum.
- Collection and preparation screens must not present 30 cards as a valid
  player deck.
- An authored activity may impose a lower cap without changing the global
  player maximum.

### Supersedes

The 30-card player-deck direction in the project brief and vision.

### References

- `docs/features/cards/system.md`
- `docs/project/brief.md`
- `docs/project/vision.md`

## 2026-08-23 — Standardize full cards on the gameplay proportion

**Status:** Accepted
**Areas:** Cards, Battles, Decks, Rewards, UI

### Decision

Use the battle card's 3:4 proportion for every full-card presentation. Screens
select from shared Compact, Standard, or Large design-space sizes rather than
authoring independent dimensions or transforming a differently sized card.

### Context

Battle, Online, deck building, quests, inspection, and reward surfaces had
drifted across both 3:4 and 2:3 cards with locally defined dimensions. This made
the same card change shape between otherwise connected player flows.

### Consequences

- Battle remains the authority for the canonical card proportion.
- Collection and deck-building cards move from 2:3 to the shared 3:4 format.
- Context may change a card's named size tier, but not its proportion.
- Bespoke legacy card-like panels must migrate to the shared presentation or
  stop presenting themselves as full cards.

### Supersedes

None.

### References

- `docs/design/card-presentation.md`

## 2026-08-22 — Make the battlefield conclusion automatic

**Status:** Accepted
**Areas:** Battle, Progression, Rewards, UI

### Decision

The battlefield displays a brief, non-interactive Victory or Defeat overlay
after combat ends, then automatically transitions to the combined Battle
Results screen. Results is the only click-through post-battle screen and owns
XP, level changes, rewards, reward choices, and Continue.

### Context

Requiring the player to click through “Player Wins” and then arrive at another
screen led by “Victory” duplicated the outcome without adding a decision. The
battle still needs a short visual beat so its ending does not feel abrupt, but
that beat does not need to become a separate screen interaction.

### Consequences

- The frozen battlefield remains visible beneath the timed outcome overlay.
- No Continue button appears on the battlefield conclusion.
- Battle Results uses a results title and retains Victory or Defeat as smaller
  context rather than presenting a second victory screen.
- Required reward selection remains the only post-battle interaction beyond the
  Results Continue action.

### Supersedes

The clickable battlefield-conclusion requirement within
`2026-08-22 — Use one combined Results screen after battle conclusion`.

### References

- `docs/archive/suspended-progression-models-2026-08/technical/meta/unified-post-battle-flow-proposal.md`
- `docs/tracking/todos.md`

## 2026-08-22 — Confirm the starting summoner with a character reveal

**Status:** Accepted
**Areas:** Onboarding, Summoners, Navigation, UI

### Decision

Every starting-summoner choice is followed by a single character-focused reveal
before the player enters the walkable Academy hub. A direct choice confirms that
the named summoner joins the player; Random explicitly identifies the character
fate selected.

### Context

Going directly from selection to campus made the commitment feel underwhelming,
and Random did not give the player a clear moment to understand its resolved
result. The earlier reveal's problem was its summoner-card presentation and
redundant mechanical information, not the confirmation beat itself.

### Consequences

- Direct and Random choices use the same one-click reveal flow.
- The screen shows character art or a character-art placeholder, name, elemental
  theme, and Continue—without stats, traits, rarity, or card framing.
- Random-specific language clearly names the resolved summoner.
- Continue enters the walkable Academy hub.

### Supersedes

`2026-08-22 — Remove the post-selection summoner reveal screen`

### References

- `docs/features/summoners/README.md`
- `docs/tracking/todos.md`

## 2026-08-22 — Remove the post-selection summoner reveal screen

**Status:** Accepted
**Areas:** Onboarding, Summoners, Navigation, UI

### Decision

Confirming the starting summoner completes that onboarding choice and returns
the player directly to the walkable Academy hub. There is no separate “Your
Champion Emerges” screen between selection and play.

### Context

The reveal repeated the choice using a legacy summoner-card presentation and
required another click without adding information. The selection screen already
owns the meaningful decision and confirmation moment.

### Consequences

- The legacy reveal scene, animation, localization, and route are removed.
- Starting-summoner setup still commits the summoner and starter deck before
  navigation.
- Any future celebratory treatment should occur within the selection transition
  rather than introducing another required screen.

### Supersedes

The legacy summoner-selection-to-reveal-to-campaign-map sequence.

### References

- `docs/tracking/todos.md`

## 2026-08-22 — Present starting summoners as characters rather than cards

**Status:** Accepted
**Areas:** Onboarding, Summoners, Progression, UI

### Decision

The starting-summoner choice presents the complete starting roster together as
characters. It does not use collectible-card framing or compare base stat lines.
Each option communicates the summoner's name and elemental theme without
turning the overview into a mechanical or temporary-copy comparison screen.

### Context

Summoners are the persistent characters through whom players experience the
Academy journey; presenting them as cards blurred that distinction. Their base
stats are not the meaningful onboarding choice and are expected to share a
common baseline. The choice should instead explain character fantasy and the
identity that meaningfully differentiates each path. Showing the whole roster
at once also makes the elemental choices and random option clear.

### Consequences

- Starting-summoner selection uses character-focused portraits or figures.
- All starting options, including Random, remain visible for comparison.
- Innate traits remain part of summoner identity mechanically, but their names
  and descriptions are not shown on the starting overview.
- Temporary personality descriptions are omitted until the characters have
  intentionally authored identities worth presenting.
- Base-stat comparison, rarity, mana cost, and card-frame language do not belong
  on this selection surface.
- Summoner Switch uses the character roster presentation; the legacy
  `SummonerCard` component is retired rather than retained as an alternate
  representation of summoners.

### Supersedes

The collectible-card presentation used by the legacy summoner selection flow.

### References

- `docs/features/summoners/README.md`
- `docs/tracking/todos.md`

## 2026-08-22 — Make Card Core visible, card-native, and behavior-led

**Status:** Accepted
**Areas:** Cards, Progression, Traits, UI

### Decision

Each Card has an explicitly authored Core representing its natural development.
The Core is visible when the Card is acquired, although its nodes may remain
locked by level, prerequisites, or cost. Its branches begin with meaningful
behavioral or strategic changes; supporting stat upgrades may reinforce a
chosen branch, but a global catalog of direct stat purchases is not Card Core.

Hidden and discovered Card development normally appears as acquired trait paths
beside Core. Core choices may permanently close alternatives.

### Context

The provisional Card detail flow labeled the existing global level-up offer
pool as `Core`. That made the choice of card largely incidental: investing in
generic stats was more legible than developing the creature's identity. The
approved progression model instead treats levels as reliable baseline growth
and uses authored development choices for build identity.

### Consequences

- Core membership must be declared per Card; broad tags cannot silently add
  global upgrades to it.
- Core is knowable from acquisition, while acquired traits own most hidden and
  world-discovered possibilities.
- Permanent branch closure is an intentional source of exclusivity.
- Each catalog Card still requires authored Core content; an empty unauthored
  Core is preferable to presenting the old global stat pool as finished design.

### Supersedes

The provisional classification of the current global level-up graph as Card
Core in `Make summoner and card development discovery-driven` (2026-08-18).

### References

- [Discovery-Driven Development](../design/discovery-driven-development.md)
- [Card Progression, Resources & Economy](../design/card-progression-economy.md)
- [Discovery-Driven Development Work Plan](../archive/suspended-progression-models-2026-08/tracking/discovery-driven-development-work-plan.md)

## 2026-08-19 — Inspect trait nodes contextually instead of reserving a side panel

**Status:** Accepted
**Areas:** Summoners, Cards, Traits, UI

### Decision

Keep the selected development tree as the dominant surface of its overlay.
Remove the permanent right-side node inspector. Hovering or focusing a node
shows a contextual popover beside it; clicking or tapping pins that popover so
the player can inspect requirements and use an available action. Clicking
elsewhere dismisses it, and the popover changes sides when needed to preserve
the selected node and readable branch relationships.

Keyboard and controller focus expose the same information, so node effects and
requirements are not dependent on mouse hover.

### Context

The first selected-trait prototype reserved a large right column for node
details. It compressed small trait paths and made the screen read as a management
dashboard. Reference review of established skill-tree interfaces showed that
node-local tooltips and contextual inspection better preserve the tree as the
primary decision surface.

### Consequences

- The overlay header retains only persistent path context, available points,
  and navigation.
- Node details contain the effect, relevant rank, cost, unmet requirements, and
  contextual action without permanently occupying canvas width.
- Irreversible acquisition may still use a concise confirmation after the
  contextual action is chosen.
- Final visual styling remains part of the external design pass.

### Supersedes

The fixed right-side details/action-panel decision in `Make summoner and card
development discovery-driven` (2026-08-18).

### References

- `docs/design/discovery-driven-development.md`
- `docs/design/trait-tree-screen-flow-spec.md`

## 2026-08-19 — Apply card levels automatically and bank configurable Card Points

**Status:** Accepted
**Areas:** Cards, Progression, UI

### Decision

Card levels apply automatically whenever an XP grant crosses a threshold. One
grant may resolve multiple levels and carries unused XP toward the next level.
Every gained level banks Card Points on that owned card instance; the amount per
level is one globally configurable progression value rather than being embedded
in individual cards or screens.

Card Point spending remains a separate player choice. Card inspection shows
level, XP, and banked points but does not offer a manual level-up action.

### Context

Leveling has no decision of its own once enough XP exists. Requiring a modal or
button delayed an inevitable state change and made XP behavior depend on which
screen the player visited.

### Consequences

- XP mutation, not UI, owns all earned level resolution and point awards.
- Exact-threshold and multi-level grants work without player confirmation.
- The global Card-Points-per-level setting can be tuned without editing card
  definitions.
- Point spending and world-driven opportunity acquisition remain deliberate,
  separately presented choices.

### References

- `docs/design/discovery-driven-development.md`
- `docs/design/card-progression-economy.md`

## 2026-08-18 — Make summoner and card development discovery-driven

**Status:** Accepted
**Areas:** Summoners, Cards, Traits, Quests, Rituals, Progression

### Decision

Keep XP and levels as reliable progression pacing, but decouple leveling from a
fully exposed default upgrade tree. Levels award bankable, entity-bound
development points. Quests, exploration, rituals, and other authored world
actions determine which summoner-trait and card-development opportunities become
available.

Levels also provide modest automatic base-stat growth. Summoners have no
separate default Core tree; they spend Trait Points across acquired trait paths.
Cards retain a native Core path and may acquire additional trait paths, with all
Card progression scoped to the unique owned card instance.

Undiscovered opportunities are hidden by default, while content may configure
hidden, visible-locked, available, acquired, and permanently closed states.
Quests commonly unlock access and rituals commonly acquire or transform, but
these are content guidelines rather than source restrictions. Either may reveal,
unlock, acquire, or transform when configured for the authored situation. An
acquisition may be free or atomically consume points, materials, and other costs.

### Context

The default loop of repeating battles, gaining XP, and choosing from a known
upgrade tree underused the game's world, quests, rituals, and exclusivity. The
new model preserves dependable advancement while making discovery determine the
player's actual build possibilities.

### Consequences

- Access and investment are separate: the world controls availability while
  points constrain commitment.
- Costs are per-opportunity and may combine points, materials, sacrifices, or no
  additional cost.
- A ritual that costs a point spends it within the ritual flow and grants the
  result there when that ritual is configured to acquire; it does not require a
  later duplicate purchase.
- Permanent spending may leave a player unable to develop a better trait found
  later. That consequence under hidden information is intentional.
- Not every trait must have upgrades, and rare free or sideways developments are
  allowed outside the point budget.
- Summoners and cards share state and acquisition rules without requiring the
  same screen topology or content structure.
- Development UI renders one selected path at a time instead of requiring an
  unbounded dynamic graph: Summoners select a trait, while Cards select Core or
  an acquired trait.
- Owned trait circles on the Summoner overview are the primary entry into trait
  development. The separate global `Upgrades` button and tree route are retired.
  The Traits section uses more vertical space, wraps icons across multiple rows,
  and scrolls only after its visible grid is full rather than adding a separate
  overflow route.
- A selected Summoner trait opens its tree in a large overlay. The initial
  overlay does not duplicate trait switching, and an atomic trait remains a
  consistent one-node tree rather than using a special presentation.
- Trait Points are visible both in the Summoner Traits header and in the open
  overlay. Card `Core` is the first selectable circle beside acquired Card traits,
  and every selection opens the same tree-overlay pattern.
- Exact automatic-growth values, affected stats, point cadence, and final visual
  composition remain open design work.
- Card battle caps limit effective level and automatic level-scaled stats, not
  the Card's acquired upgrades. The exact temporary projection remains a TODO;
  the permanent Card instance must never be downgraded or rewritten.
- Closed branches remain permanent in progression state but disappear from the
  default tree; an optional toggle can reveal them. Available opportunities stay
  available when unaffordable, with costs communicating the blocker, and newly
  revealed/unlocked nodes receive an attention treatment until inspected.
- Summoner levels grow both health and maximum mana; stronger Cards may become
  more mana-intensive as part of that power tradeoff. Automatic Card growth is
  configured per Card. Exact values remain tuning work.
- Reveals and unlocks use compact toast feedback, while actual acquisitions and
  transformations use the larger generic reward presentation.
- Node selection uses a fixed right-side details/action panel in the tree
  overlay. The tree itself communicates branch closure; confirmations do not
  enumerate rejected alternatives. Ritual acquisitions cannot be performed
  remotely from the tree.

### Supersedes

The fixed, fully exposed default-tree assumptions in the earlier summoner trait
tree flow and card level-up offer model.

### References

- `docs/design/discovery-driven-development.md`
- `docs/design/trait-tree-screen-flow-spec.md`
- `docs/design/card-progression-economy.md`

## 2026-08-17 — Combine summoner build management and player inventory

**Status:** Accepted
**Areas:** Summoners, Inventory, Equipment, Traits, UI

### Decision

Use the Summoner screen as the current build-management and inventory surface.
It shows the active summoner's portrait, level and XP, compact identity, stats,
equipped items, owned traits, banked upgrade points, and the player's owned item
inventory. The Traits area both summarizes owned traits and provides the entry
for spending points through `Upgrades`.

The campus Inventory action opens this combined surface. The inventory grid is
implemented as a reusable component so it can move to a dedicated screen later
if inventory develops enough independent complexity to justify one.

### Context

Equipment choices make the most sense beside the summoner they modify, and a
separate inventory screen would currently duplicate navigation while leaving
the Summoner screen underused. The earlier reorganization also separated related
information into cramped panels and failed to expose the player's owned items.

### Consequences

- Equipped slots and owned items are visible together.
- Selecting equippable inventory routes into the existing slot-management flow.
- Materials may be visible here while remaining usable only in their proper world context.
- A dedicated Inventory screen remains a future option, not a current requirement.
- Owned traits are summarized here; the trait tree remains the authoritative spend surface.

### Supersedes

The requirement in `Apply summoner levels automatically and bank upgrade choices`
that Inventory remain a separate system and the first compact panel reorganization.

### References

- `docs/design/trait-tree-screen-flow-spec.md`

## 2026-08-16 — Treat quest decisions as authored player dialogue

**Status:** Accepted
**Areas:** Quests, Dialogue, Academy, UI

### Decision

Quest acceptance and refusal are player-spoken responses inside the ongoing
dialogue, not detached confirmation controls. Each quest authors the responses
that actually exist. Mandatory quests may offer only an affirmative response;
quests with genuine choice may offer acceptance, refusal, or other contextual
responses. Important commitment information should be expressed naturally in
the relevant response instead of appearing as a separate rules line.

### Context

The first implementation exposed generic Accept and Not Yet buttons and appended
the assignment title and curriculum cost as isolated callouts. This made the
conversation feel like a modal transaction and falsely implied that the fixed
opening course could be declined.

### Consequences

- Quest data owns response text and the action attached to each response.
- Dialogue UI must support one or several full-width spoken responses.
- A refusal option is shown only when refusal is a real authored choice.
- Mechanical consequences remain clear, but their presentation belongs within
  the conversation when the player makes the decision.

### Supersedes

The generic Accept/Not Yet presentation within “Define the classical
professor-led quest experience.”

### References

- [Quest System](../design/quest-system.md)

## 2026-08-15 — Preserve standard combat as the first excursion baseline

**Status:** Accepted
**Areas:** Player Journey, Battles, Excursions, Maps, Quests, Controls

### Decision

Begin excursions with free movement and world interactions between encounters while meaningful fights use the recognizable Fateforged battle format. Solo encounters may vary their goals, enemies, decks, arenas, and rules without requiring a second live-combat control model.

Treat this as the safe implementation baseline, not a permanent creative restriction. Continue focused exploration of more innovative formats, including controllable movement during combat, and adopt them only when they provide distinctive reusable value that justifies their complexity.

### Context

Comparable games often expand solo play by placing established combat inside exploration, missions, progression, unusual objectives, or encounter-specific rules. This creates a broader adventure while protecting the mechanics players learn for the standard competitive game.

### Consequences

- The first excursion does not depend on solving controllable summoner movement during combat.
- Quest and map planning can proceed around exploration, interaction, and standard battle encounters.
- Encounter objectives and surrounding world actions are the first places to seek variety.
- Experimental combat formats remain an explicit discovery effort rather than being rejected or silently added to the required foundation.
- A novel format must prove that its player value and reuse justify its controls, engineering, balance, and content costs.

### Supersedes

The roadmap assumption that one global combat-movement decision must be implemented consistently across 1v1 and every other combat experience; no prior direction-log entry.

### References

- `docs/design/excursion-combat-format.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Use campus as home base without flattening excursions into separate trips

**Status:** Accepted
**Areas:** World, Campus, Excursions, Maps, Quests

### Decision

Use the magic campus as the player's recurring home base for preparation,
activity selection, progression, and choosing what comes next. Allow an
excursion to continue through physically connected sub-locations when the world
and quest support it—for example, a forest leading into ruins within that
forest—without requiring a return to campus between every area.

### Context

The campus needs a clear role in the core loop, but treating every playable
location as an isolated campus spoke would make the world feel artificial and
would constrain multi-stage quests. Conversely, defining travel mechanics
before the required locations are known would let navigation assumptions dictate
the world prematurely.

### Consequences

- The normal rhythm is campus preparation and selection, excursion activity,
  then a campus return for progression and the next choice.
- Excursions may be composed of multiple bounded, connected spaces.
- A site can be physically nested inside another excursion region instead of
  requiring a direct campus entrance or its own standalone trip.
- Departure points, unlocking, shortcuts, and fast travel remain downstream of
  the approved world and feature roster.

### References

- `docs/design/walkable-academy-hub.md`

## 2026-08-23 — Separate owned Inventory from the Summoner overview

**Status:** Accepted; implementation is the next UI slice
**Areas:** Summoners, Inventory, Equipment, Navigation, UI

### Decision

Give owned-item Inventory a dedicated screen reached by the campus bag action.
Keep the Summoner screen focused on character identity, level and XP, core stats,
traits and trait development, and currently equipped items. The Summoner screen
may later open Inventory filtered to compatible equipment from an equipped slot,
but it does not own the full item collection.

### Context

The bag icon currently opens a screen whose primary identity is the summoner,
which does not match the player's navigation expectation. Retaining the full
owned-item grid merely to fill layout space overloads the Summoner screen; its
remaining space can instead support stronger character presentation and clearer
progression hierarchy.

### Consequences

- The bag icon and profile/summoner access no longer route to the same purpose.
- Equipped items remain visible on the Summoner screen because they describe the
  active build.
- Cards and decks remain owned by the Spellbook, not Inventory.
- Inventory layout, item categories, and filtering are the next dedicated design
  and implementation task.

### Supersedes

The 2026-08-17 decision to use the Summoner screen as the combined build and
owned-inventory surface.

### References

- `docs/tracking/todos.md`

## 2026-08-23 — Bind gameplay Inventory to individual summoners

**Status:** Accepted; UI scoped, persistence migration pending
**Areas:** Inventory, Equipment, Summoners, Persistence, Rewards

### Decision

Gameplay items belong to the summoner who acquires them. They are not shared
across the player's roster. The bag opens Inventory for the active summoner, and
equipment selection only considers that summoner's compatible owned items.

### Context

Inventory contributes to each summoner's distinct collection and build, matching
the existing per-summoner card and deck model. Sharing gameplay equipment across
the account would weaken that separation and make the bag's ownership context
ambiguous.

### Consequences

- The reusable Inventory overlay always has an explicit summoner context.
- Account-level cosmetics or purchases are separate from gameplay Inventory.
- The legacy `AccountWide` gameplay-item definitions, grant paths, and saved data
  require a dedicated migration before persistence fully matches this decision.

### References

- `docs/features/equipment-system.md`
- `docs/tracking/todos.md`

## 2026-08-23 — Make Inventory a passive category browser

**Status:** Accepted; container treatment remains under evaluation
**Areas:** Inventory, Items, Equipment, World Interaction, UI

### Decision

Make the normal bag Inventory primarily a large grid with `All`, `Equipment`,
`Materials`, `Consumables`, and `Quest Items` filters. Selecting an item opens a
smaller inspection modal rather than reserving permanent detail space. The bag
supports browsing and inspection; meaningful item use remains with the relevant
world location or character.

Equipment is the contextual exception. Opening Inventory through a summoner's
equipment slot filters the same collection to compatible items and adds equip or
unequip actions to inspection.

### Context

Inventory needs to accommodate likely item categories for external UI design
even if a provisional category is removed later. Keeping rituals, cracking,
quest delivery, and commerce out of the bag also preserves the purpose of the
campus and its dedicated interactions.

### Consequences

- The item grid occupies nearly all of the Inventory surface.
- Category tabs and their empty states are part of the designer handoff.
- Consumables remain a provisional supported category, not a commitment that
  portable consumables must exist.
- The final choice between a large overlay and a dedicated screen is still open.

### References

- `docs/features/equipment-system.md`
- `docs/tracking/todos.md`

## 2026-08-22 — Replace the audio-only placeholder with shared game settings

**Status:** Accepted
**Areas:** Settings, Battle UI, Accessibility, Controls

### Decision

Treat Settings as a complete, scalable player surface rather than an audio-only
placeholder. Use a left-side category list for Audio, Display, Controls,
Gameplay, and Accessibility, with one shared settings component available from
both the standalone screen and the battle menu.

### Context

The existing screen exposed only music and sound-effect volume, leaving the UI
designer without the categories and states required for a credible PC settings
experience. The shared categorized model keeps navigation predictable, prevents
the standalone and in-battle versions from drifting, and lets new settings be
added without redesigning the screen.

### Consequences

- Supported settings apply immediately and persist.
- The current surface includes volume controls, focus muting, window/display
  options, keyboard bindings, camera preferences, reduced camera motion, and UI
  scale.
- Offline and online battle menus open the same settings component; an online
  battle continues behind it.
- Unsupported capabilities are not represented as working controls merely to
  fill categories.

### Supersedes

The standalone audio-only settings screen and the separate audio-only battle
settings panel.

### References

- `scenes/shared/settings_panel.tscn`

## 2026-08-22 — Use one combined Results screen after battle conclusion

**Status:** Accepted
**Areas:** Battle, Progression, Rewards, UI

### Decision

End meaningful battles with two presentation surfaces: first, a clickable
victory or defeat conclusion over the battlefield; second, one combined Results
screen containing relevant summoner XP, participating-card XP, automatic level
reveals, acquired rewards, reward choices, and contextual progress.

The Results sections may reveal automatically but do not require separate
clicks. Cards with no XP gain are omitted, while summoner progression remains
visible. Automatic leveling does not pause for confirmation. A required reward
choice is the only interaction beyond continuing from each of the two surfaces.

### Context

The prior flow split immediate battle outcome, campaign rewards, and Academy
encounter results across competing screens. It could skip progression feedback,
repeat victory messaging, and force future UI work to reconcile several
source-specific flows. A single Results surface gives the UI designer one
complete contract without turning every result category into its own page.

### Consequences

- Campaign and quest encounter battles share the same post-battle destination.
- Victory and defeat use the same structure, omitting irrelevant empty sections.
- Progression and reward services remain authoritative; Results only presents
  committed facts and submits explicit reward choices.
- A typed report with exact before/after snapshots remains required for polished
  XP rollover and multi-level animation.
- Legacy `RewardScreen` and `EncounterResults` should be deleted after all
  remaining battle modes and pending Academy reward choices use the canonical
  route.

### References

- `docs/archive/suspended-progression-models-2026-08/technical/meta/unified-post-battle-flow-proposal.md`
- `docs/tracking/todos.md`

## 2026-08-16 — Apply summoner levels automatically and bank upgrade choices

**Status:** Accepted
**Areas:** Summoners, Progression, Equipment, UI

### Decision

Summoner levels apply automatically whenever earned XP crosses a threshold. A
single XP grant may resolve multiple levels and carries leftover XP toward the
next threshold. Each level banks an upgrade point; spending that point remains
a separate, optional action through `Upgrades`.

The Summoner screen is an overview and management surface for identity, level
and XP, core stats, equipped items, and the entry into Upgrades. Inventory
remains a separate system, while equipment slots on the Summoner screen may open
a filtered equipment selector. Gold, a manual Level Up action, stat previews,
and a duplicate full trait list do not belong on the overview.

### Context

Manual level confirmation adds a click without a meaningful decision, while
forcing an upgrade choice at the moment of leveling interrupts the player's
current activity. Banking the choice preserves agency and lets the player make
permanent build decisions deliberately. Separating Inventory from the Summoner
screen also keeps item ownership distinct from the currently equipped build.

### Consequences

- XP mutation owns level resolution; presentation does not execute level-ups.
- One XP award can emit multiple level events and grants one banked point per level.
- Post-battle presentation may reveal a level gain but cannot require immediate spending.
- `Upgrades` is the player-facing term for entering the existing summoner trait tree.
- The final visual design and precise level-up reveal treatment remain designer work.

### Supersedes

The manual `Level Up` plus `Traits` two-button flow in the original trait-tree
screen specification and the manual summoner level-up modal.

### References

- `docs/design/trait-tree-screen-flow-spec.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Use generic quest and encounter systems across every context

**Status:** Accepted
**Areas:** Quests, Encounters, Battles, Academy, Excursions, Architecture

### Decision

Build one generic quest system and one generic encounter system that are applied
to Academy, wilderness, underground, side-quest, and future contexts. A battle
is a reusable encounter referenced by a quest step; it is not an Academy
activity owned by a special academic progression pipeline.

Academic courses use the same quest definitions, typed steps, encounter launch,
and encounter-completion events as every other quest. Curriculum capacity,
grades, and transcript effects attach through explicit typed quest rules. The
quest and encounter cores do not contain professor-, course-, semester-, or
Academy-specific branching.

### Context

The first rearchitecture proposal correctly separated quest sequencing from
academic records, but still assigned battle preparation and execution to an
`AcademyProgressHandler`. That boundary would force a forest quest or another
non-Academy source involving a battle either to depend on Academy code or to
create a second battle-quest pipeline.

### Consequences

- `QuestProgressHandler` is the sole quest state authority in every context.
- Generic encounter definitions own reusable battle configuration and
  preparation requirements and emit generic completion events.
- Current Academy activity definitions migrate into encounter definitions.
- Current Academy preparation/results screens migrate to generic encounter
  screens rather than remaining Academy-owned infrastructure.
- Domain-specific behavior is added through typed rule handlers, not generic
  script hooks or context checks inside quest/encounter cores.
- The rearchitecture proposal is revised before implementation.

### Supersedes

The `AcademyProgressHandler` / Academy-activity execution boundary in the prior
quest-step rearchitecture proposal. It does not change the accepted curriculum,
professor, or Course Flow deprecation decisions.

### References

- `docs/design/quest-system.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/technical/meta/quest-step-rearchitecture-proposal.md`

## 2026-08-16 — Replace the old Course Flow with authoritative typed quest steps

**Status:** Accepted
**Areas:** Quests, Courses, Campus, Progression, UI, Architecture

### Decision

Deprecate the old Class Hall enrollment browser and full-screen Course Flow in
their entirety. They must not survive as alternate ways to enroll, select or
launch activities, advance course progression, or receive post-battle routing.
The physical Class Hall building may be repurposed later, but does not justify
retaining its current screen.

Represent quests as ordered typed steps completed by authoritative world or
gameplay events. Academy battle activities remain reusable definitions for
preparation, battle configuration, loadouts, and rewards, and are referenced by
quest steps instead of owning the overall progression flow.

The introductory proof follows: accept from the general professor, interact
with the campus Practice Grounds, complete its training battle, return to
campus, and close the quest with the professor.

### Context

The professor-led flow could accept and display the introductory quest but had
no route into its first battle. The only working launcher remained the old
Course Flow, while its activity index could represent only a sequence of
battles—not talk, world interaction, battle, and return as one coherent quest.
Patching a direct link to that screen would preserve the superseded experience
and leave two progression models.

### Consequences

- The Journal and HUD expose the current quest step but do not become generic
  activity launchers.
- NPCs and world interaction points advance only steps targeting them.
- Activity Preparation and Results are retained, with quest-owned entry and
  campus return routing.
- Course-node scenes, routes, APIs, and tests are deleted after their quest-step
  replacements are wired.
- The exact architecture and migration sequence are proposed in
  `docs/archive/suspended-progression-models-2026-08/technical/meta/quest-step-rearchitecture-proposal.md`.

### Supersedes

Any remaining interpretation that the old Class Hall or Course Flow might keep
responsibility for enrollment, activity selection, activity launch, or course
progression.

### References

- `docs/design/quest-system.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/technical/meta/quest-step-rearchitecture-proposal.md`

## 2026-08-16 — Separate character dialogue from quest UI and adopt the three-region Journal

**Status:** Accepted
**Areas:** Quests, Dialogue, Professors, UI

### Decision

Use separate authored text for character dialogue, Journal descriptions and
objectives, and internal activity labels. NPCs speak in their own voice rather
than reciting system labels. Concise mechanical callouts may appear in an accent
color inside dialogue when the player must recognize an assignment, objective,
cost, or permanent commitment.

Structure the Journal as a category rail for Active, Open, and Completed; a list
containing only the selected category; and a detail region showing the selected
quest's source portrait/name, location, description, objective, and known
rewards. Begin the general professor as a supportive mentor; the other
professors' personalities remain content decisions.

### Context

The initial dialogue exposed internal wording such as `Practice` directly to the
player and felt like a quest database rather than a conversation. The first
stacked-card and later two-pane Journal descriptions also did not match the
approved mockup's clearer separation between navigation, selection, and detail.

### Consequences

- Quest data needs explicit dialogue fields for offer, accepted, active/reminder,
  and turn-in states rather than deriving speech from objective labels.
- The reusable NPC component remains role-agnostic; personality belongs to
  authored character content.
- Journal projections expose source identity, location, and reward previews.
- Final visual skinning may evolve without collapsing the three information
  regions.

### Supersedes

The two-pane Journal layout and generic-dialogue portions of the earlier
classical professor-led quest decision. Its progression, markers, acceptance,
tracking, and curriculum-commitment decisions remain accepted.

### References

- `docs/design/quest-system.md`
- `docs/archive/suspended-progression-models-2026-08/technical/meta/quest-system-foundation-plan.md`

## 2026-08-16 — Define the classical professor-led quest experience

**Status:** Accepted
**Areas:** Quests, Professors, Campus, Courses, UI, Progression

### Decision

Implement quests through a classical character-led flow. Known available quests
use `!`, character turn-ins use `?`, and other character states have no quest
marker. Acceptance and completion happen naturally through dialogue. Academic
acceptance must state exact curriculum cost and permanence; remaining capacity
is optional when already clear elsewhere.

One tracked quest appears as a one-line banner beneath the profile icon in
walkable spaces. Clicking it opens a full-screen two-pane Journal with a quest
list on the left and selected details on the right. Persistent Journal access
lives in the top-right or right-edge exploration navigation. Neither surface
appears during battles.

Begin with five persistent placeholder professors on one continuous campus: one
general professor and one for each element. Use the dependency sequence
Introduction to Magic, then Summoning Basics or Practical Spellcraft, then the
four elemental opportunities. All professors exist from the beginning at their
own campus landmarks, but only available content receives markers.

### Context

The earlier direction established professor-led academic chains and a Journal
but intentionally left their actual player-facing language unresolved. The
accepted experience preserves familiar quest readability without requiring a
minimap, dedicated course-selection tree, classroom interiors, or a detailed
contract screen. Landscape landmarks give professors memorable physical homes,
while limited curriculum capacity preserves the permanent tradeoff.

### Consequences

- Introduction to Magic is fixed but begins as an offered quest rather than an
  auto-enrolled course.
- The Journal must be redesigned from its stacked-card graybox into the accepted
  two-pane layout.
- Side quests have no hard active-count limit; only one quest is tracked.
- Fixed rewards resolve through dialogue with a compact received notification;
  a focused choice UI appears only for selectable rewards.
- A hidden-opportunity field belongs in the model, but hidden-discovery UX and
  magical trails are deferred.
- The first vertical slice must prove offer, acceptance, tracking, battle-driven
  progress, return dialogue, completion, and dependency unlocking.

### Supersedes

The earlier requirement that remaining curriculum capacity must always appear
inside the acceptance interaction. Exact cost and permanence remain mandatory;
remaining capacity may instead be supplied by persistent surrounding UI.

### References

- `docs/design/quest-system.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/design/walkable-academy-hub.md`
- `docs/archive/suspended-progression-models-2026-08/technical/meta/quest-system-foundation-plan.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Make card cracking illicit and secret

**Status:** Accepted
**Areas:** Cracked Cards, World, Campus, Quests, Progression

### Decision

Present card cracking as an illicit, secret activity rather than a normal,
publicly sanctioned Academy service. The exact operator, location, discovery
path, and consequences remain to be designed through the feature-to-world
blueprint.

### Context

Cracked cards are risky alterations of normal cards. Their place in the world
should reinforce that identity. This decision defines the intended experience
without prematurely requiring an expensive black-market district; a hidden
person, room, or other reuse of the eventual world structure may be sufficient.

### Consequences

- Cracking should not be exposed as an ordinary campus-shop transaction.
- Access should involve discovery, introduction, or another secretive gate.
- The map plan must compare a reused hidden space with any dedicated location
  on player value, narrative fit, and production cost.
- Detailed cracking rules and progression consequences remain dedicated design
  work.

### Supersedes

The earlier direction that a black market or underground source was only a
possible presentation. The secret and illicit character is now required; a
specific black-market location is still not required.

### References

- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Place cracking and rituals in a contact-gated Academy underground

**Status:** Accepted
**Areas:** World, Campus, Cracked Cards, Card Progression, Quests

### Decision

Add a persistent, bounded tunnel area beneath the Academy that behaves like a
small secondary hub. The player enters by talking to a covert campus contact
rather than selecting a permanent UI destination. Its minimum physical roles
are a card-cracking space and a room for card-progression rituals.

### Context

Both cracking and rituals benefit from a secret physical setting, but a complete
black-market district would be expensive. A reusable underground kit gives the
two systems a coherent home, can support additional secret characters or quest
moments when justified, and requires less bespoke environment art. Routing entry
through a character also avoids extra navigation UI and makes the player engage
with the world.

### Consequences

- The underground is not a default combat excursion; combat appears only when
  an authored activity calls for it.
- Its baseline scope is a small walkable layout and the rooms needed by cracking
  and rituals.
- Additional tunnels, encounters, NPCs, and secrets are optional expansion, not
  prerequisites for the foundational map.
- The contact becomes the authoritative entry point even after discovery unless
  this direction is explicitly revised.

### References

- `docs/design/walkable-academy-hub.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Do not require physical classrooms for every course

**Status:** Accepted
**Areas:** Courses, Quests, Campus, Maps, Scope

### Decision

Treat courses primarily as structures for quests, battles, and progression.
Attending class is not a major recurring activity, so the foundational world
does not require a bespoke classroom or interior for each course. Add a teaching
space only when a specific playable activity makes meaningful use of it.

### Context

A room that only adds a walk before speaking to a professor creates environment
art, navigation, and repetition without adding gameplay. The Academy fantasy can
instead be carried by professors, quest context, course progression, battles,
and excursions. This separates the value of interacting with an instructor from
the cost of building a unique room for every subject.

### Consequences

- The map roster cannot infer one interior from every course in the catalog.
- Professor and course interactions may reuse campus spaces and existing
  interfaces.
- A shared classroom, laboratory, or bespoke interior remains possible when an
  authored activity justifies it.
- The physical placement of professors and the division of responsibility
  between NPC interaction and the Course Flow remain world-blueprint work.

### References

- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Commit curriculum capacity when an academic quest chain is accepted

**Status:** Accepted
**Areas:** Courses, Quests, Progression, Exclusivity, UI

### Decision

Every academic quest chain declares how much of the player's limited curriculum
capacity it consumes. Accepting the chain commits that amount permanently;
finishing it converts the commitment into completed academic progress toward the
current year. An unfinished or abandoned chain does not refund its capacity.

### Context

The four-year structure is clearest when the player can measure progress against
a limited yearly academic budget. Presenting the cost at the moment of quest
acceptance allows professors and world discovery to participate in enrollment
without hiding the permanent tradeoff or requiring every choice to originate in
a course tree. Permanent commitment prevents the player from accepting every
course and postponing the real choice until later.

### Consequences

- Acceptance must show the chain cost, currently committed capacity, remaining
  capacity, and the fact that the commitment cannot be refunded.
- Course discovery and the final acceptance interface remain open design work;
  this rule applies regardless of presentation.
- Curriculum capacity and completed academic progress require unambiguous,
  measurable presentation across all four years.
- Player-facing terminology must not confuse this academic resource with gold or
  other spendable currencies.
- Failure, stalled chains, exact yearly budgets, and protection against invalid
  remaining-capacity combinations still require explicit rules.

### Supersedes

Any interpretation that course cost is paid only after completion or can be
recovered by abandoning an accepted course.

### References

- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Manage academic chains and world quests through one Journal

**Status:** Accepted
**Areas:** Quests, Courses, UI, Progression, Discovery

### Decision

Use one Journal with three sections: Active quests, known Opportunities, and
Completed history. The Journal shows the player's current year, permanently
committed curriculum capacity, and completed academic progress. Announced
opportunities are added automatically, while hidden opportunities remain absent
until discovered through the world.

Courses are experienced as quest chains. Accepting an academic opportunity moves
it into Active and quest progression exposes its next actionable step; the
player does not launch each lesson from a row of course nodes.

### Context

The player needs one clear place to understand current obligations without a
central catalog revealing every secret or making the Academy feel like a level
select. Separating known opportunities from accepted quests preserves informed
curriculum commitments, while discoverable opportunities give exploration and
NPC interaction real value.

### Consequences

- Active, available, and completed content have explicit and distinct states.
- A hidden quest cannot leak through the Journal before its discovery condition.
- Once discovered, an opportunity receives the same cost and permanence preview
  as an announced academic chain.
- The current Class Hall and node-based Course Flow are provisional and require
  redesign; they are no longer the accepted way to launch every lesson.
- Exact Journal layout, notifications, filtering, quest limits, and the retained
  role of the Class Hall remain dedicated experience-design work.

### Supersedes

The assumption that the Class Hall course browser and a full-screen node-based
Course Flow are the final authoritative experience for enrollment and activity
launching.

### References

- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-14 — Design elemental summons creature-first

**Status:** Accepted
**Areas:** Cards, Summons, Elements, Progression, Content Production

### Decision

Begin elemental summon design with the creature's identity and fantasy, then derive its battlefield behavior, abilities, stats, upgrades, and visual direction. Do not use detached ability ideas as the default starting point for filling the elemental roster.

### Context

Ability-first ideation produced mechanics without always producing memorable, coherent creatures. The intended content process should make the creature itself the source of its gameplay identity.

### Consequences

- Elemental roster work requires manually planned creature concepts and lightweight visual exploration.
- Card-stat and upgrade-tree work should preserve the creature's identity rather than flattening it into generic balance packages.
- Mechanics can still inspire concepts, but they are not the default organizing principle.

### Supersedes

The ability-first working approach used during early elemental ideation; no prior direction-log entry.

### References

- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`
- `docs/design/fire-content-working-notes.md`
- `docs/design/water-content-working-notes.md`
- `docs/design/earth-content-working-notes.md`
- `docs/design/wind-content-working-notes.md`

## 2026-08-14 — Add cracked cards as risky normal-card variants

**Status:** Accepted
**Areas:** Cards, Decks, Progression, Quests, Online

### Decision

Add cracked cards as variations of normal cards with a meaningful twist or altered rule. A cracked variation can enable unusual synergies, but its change is risky and is not required to be beneficial.

### Context

Cracked cards create build possibilities through altered behavior rather than a straightforward power tier. For example, a spell might gain broader impact while also affecting allies, creating both a new opportunity and a new liability.

### Consequences

- Normal-card identity and balance must be coherent before cracked variants are broadly authored.
- Cracked-card behavior and risk must be understandable to the player.
- Acquisition, permanence, deckbuilding limits, balance rules, and the exact cracking process remain dedicated design work.
- A black market or underground source is a possible presentation, not an accepted location or acquisition model.

### Supersedes

None.

### References

- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-14 — Use quests to connect the expanded Academy experience

**Status:** Accepted
**Areas:** Player Journey, Academy, Quests, Maps, Characters, Progression, UI

### Decision

Use quests as connective structure across lessons, characters, locations, battles, rewards, shops, and discoveries. The bounded walkable campus should support experiences beyond static menu navigation while the overall journey still culminates in graduation and online PvP.

### Context

Recovering the bounded campus opened opportunities for a more lived-in Academy experience. Keeping lessons primarily as a static interface risks making the curriculum feel disconnected from the world and its characters.

### Consequences

- The current course-flow interface must be reevaluated rather than assumed to be the final primary experience.
- The physicality of the school, professor interactions, quest delivery, and the relationship between courses and quest chains require dedicated design work.
- Additional bounded locations are selected only after defining player engagement and evaluating production value and reuse.
- Features do not automatically require bespoke locations; a character, existing campus space, reusable room, or interface may be sufficient.
- The exact map roster and controllable-combat model remain unapproved until their roadmap initiatives conclude.

### Supersedes

The assumption that the Academy curriculum is experienced primarily through static course-selection and activity-flow interfaces; no prior direction-log entry.

### References

- `docs/design/walkable-academy-hub.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`
## 2026-08-16 — Use card-specific summon radii in moving-summoner encounters

**Status:** Accepted
**Areas:** Excursions, Combat, Cards, Summons, Movement

### Decision

In moving-summoner excursion encounters, replace the standard team-half summon restriction with a card-specific radius centered on the summoner. Keep the placement model configurable so standard battles can retain team-half summoning.

### Context

A moving summoner should change where creatures can enter play. Making the summoner a mobile deployment point gives movement tactical purpose, while card-specific distances create design space for different creature identities and roles.

### Consequences

- The input preview, authoritative command validation, and AI must use the same configured placement rule.
- Each summon card owns its placement radius; the summoner supplies the moving center point.
- Spell targeting remains separate from summon placement.
- Standard battles continue to use team-half placement unless explicitly configured otherwise.
- Prototype range values are tuning inputs, not final card balance.

### Supersedes

The team-half summon restriction within the moving-summoner excursion experiment. Standard battle behavior is not superseded.

### References

- `docs/design/excursion-combat-format.md`

## 2026-08-16 — Make mana a non-regenerating excursion resource

**Status:** Accepted
**Areas:** Excursions, Combat, Mana, Progression, Items

### Decision

Use one limited mana supply across an entire excursion. Mana does not regenerate naturally during encounters or between rooms.

### Context

Unlimited or automatically restored mana removes the pressure connecting one excursion encounter to the next. A shared supply makes inefficient victories consequential and gives the excursion a meaningful resource-management arc.

### Consequences

- Mana spent in one room reduces the player's options in later rooms.
- Infinite mana in the compact ruin prototype remains test scaffolding only.
- Mana recovery through items, rewards, resting, or other interactions remains undecided and must not accidentally make waiting a complete recovery strategy.
- Excursion length, card costs, starting mana, and recovery frequency must eventually be balanced together.

### Supersedes

None.

### References

- `docs/design/excursion-combat-format.md`

## 2026-08-16 — Vary quest card acquisition while preserving exclusivity

**Status:** Accepted
**Areas:** Quests, Cards, Rewards, Academy, Progression

### Decision

Do not force every permanent card acquisition into the same choice format. One
quest may grant a single authored card, another may end with a choice between two
cards, and an earlier course, route, or quest decision may determine a later
card reward. Card acquisition may be presented organically through the quest
instead of always appearing as a generic post-battle grant.

### Context

Exclusive choices remain the core of summoner identity, but requiring a fully
informed multi-card comparison after every meaningful activity would make each
reward laborious and repetitive. Exclusivity should shape the player's overall
path without forcing identical decision UI at every acquisition.

### Consequences

- Fixed and selectable card rewards can coexist across authored quests.
- Major explicit choices can remain fully previewed when appropriate; not every
  card reward needs to be a landmark comparison.
- Upstream commitments may determine downstream rewards without adding a second
  redundant choice at the moment of acquisition.
- Organic presentation does not loosen acquisition limits, repeatability rules,
  or permanent closure of alternatives.
- This decision does not approve unrestricted random card drops or a farmable
  catch-everything acquisition loop.

### Supersedes

The implicit assumption that preserving exclusivity requires every permanent
card reward to use the same explicit multi-option presentation.

### References

- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`

## 2026-08-16 — Use permanent behavioral branches in card upgrade trees

**Status:** Accepted
**Areas:** Cards, Upgrades, Progression, Materials, Quests

### Decision

Allow card upgrade trees to contain mutually exclusive branches that permanently
change card behavior. Choosing one branch makes its sibling branch unavailable
for that summoner. Some branches may be gated by a ritual requiring gathered
items or materials.

### Context

Repeated battles need to advance existing cards rather than relying on frequent
new-card acquisition. Behavior-changing branches let leveling deepen a card's
identity and create exclusive builds. Ritual requirements connect battles,
quests, exploration, items, shops, and card progression without making creature
ownership itself depend on arbitrary environmental tools.

The initial example is a segmented worm summon whose branches reinterpret its
body: death can split it into two smaller worms, or lethal damage can remove one
segment while the shortened creature survives.

### Consequences

- Battle participation and card XP can unlock access to meaningful upgrade milestones.
- Selecting one behavioral branch permanently closes the alternative branch.
- Ritual materials can give repeat battles and curated excursions a progression purpose.
- A ritual is a branch unlock requirement, not an additional stackable upgrade system.
- A rarer ritual path must not automatically become the strictly superior choice;
  branch power must fit the eventual shared player-power budget.
- Exact branch counts, milestone cadence, material taxonomy, and the relationship
  to elevation and cracked cards remain dedicated design work.

### Supersedes

The weaker assumption that card upgrade choices are independent selections that
can all remain available across later levels.

### References

- `docs/design/card-progression-economy.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Require one coherent summoner-control model across combat

**Status:** Accepted constraint; movement decision remains open
**Areas:** Combat, Excursions, 1v1, Controls, Scope

### Decision

Do not ship controllable summoner movement as a separate ruin-only combat mode.
If combat movement is accepted, it must become a coherent foundational rule for
standard 1v1 with both summoners able to participate. Otherwise, excursion and
standard battles both retain stationary summoners. Exploration movement outside
combat does not violate this constraint.

### Context

A mobile excursion format paired with stationary 1v1 would require two sets of
combat controls, AI assumptions, targeting rules, spatial behavior, maps, and
content expectations. That risks both excessive scope and an incoherent game.
The compact ruin remains useful for evaluating movement, but it cannot establish
an excursion-only exception by itself.

### Consequences

- Movement must be evaluated by what it adds to the core battle, not by whether
  a walkable quest map needs more activity.
- A promoted movement model must account for both summoners, AI, PvP controls,
  spells, creatures, kiting, camera behavior, networking, and production art.
- Ruin and quest planning cannot assume action combat while the core movement
  decision remains open.
- The existing movement prototype remains experimental evidence, not committed
  production scope.

### Supersedes

The possibility that moving-summoner combat could be adopted only for excursions
while standard 1v1 remained a separate stationary format.

### References

- `docs/design/excursion-combat-format.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Keep summoners stationary during combat

**Status:** Accepted
**Areas:** Combat, Excursions, 1v1, Controls, Scope

### Decision

Keep summoners stationary during combat in both standard 1v1 and excursions.
Players can move while exploring, but entering combat transitions into the
established horizontal Fateforged battle format. The moving-summoner greybox is
retained as research evidence and is not production scope.

### Context

The wider game loop now has enough activity and progression without requiring
summoner movement to make excursions meaningful: curated exploration, quests,
route and resource decisions, persistent excursion mana, repeated battles for XP
and materials, varied card acquisition, shops and items, and permanent upgrade
branches. Movement would add direct dodging and mobile deployment, but it would
also redesign targeting, kiting, AI, controls, camera behavior, multiplayer, map
geometry, and animation across the entire core battle.

### Consequences

- Excursion planning can use dedicated horizontal battle spaces without needing
  continuous action combat inside the exploration geometry.
- Battle variety must come from objectives, enemy compositions, restrictions,
  bosses, resources, and progression rather than summoner movement.
- The card-specific summon-radius rule remains prototype-only unless a future
  approved core-combat redesign reintroduces moving summoners.
- The moving-summoner unit-displacement bug does not block production work while
  the experimental room remains isolated.
- Reconsidering combat movement requires a new explicit product decision.

### Supersedes

The open movement decision in `Require one coherent summoner-control model across
combat`. It also retires moving-summoner combat and card-radius placement from
the production baseline while preserving their prototype history.

### References

- `docs/design/excursion-combat-format.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Transition exploration encounters into separate battlefields

**Status:** Accepted
**Areas:** World, Excursions, Combat, Maps, Quests

### Decision

When the player reaches an encounter in a bounded exploration location, transition
to a separate large horizontal battlefield themed to that location. Resolve the
battle there, then return the player to the relevant exploration state with the
outcome applied. The combat arena does not need to fit literally inside the exact
exploration geometry where the encounter began.

### Context

Fateforged battlefields require much more horizontal space than believable forest
paths, ruin chambers, and other curated exploration spaces. Requiring every
encounter to unfold physically in place would distort map scale or force a second
combat system. A deliberate transition preserves both the established combat
format and coherent exploration environments.

### Consequences

- Exploration maps and battle arenas have separate scale and composition needs.
- A forest, ruin, or other excursion can reuse a small themed arena family across
  several encounter points instead of authoring a battlefield into every room.
- Encounter state must persist across the transition so victory, defeat, resource
  spending, rewards, and cleared paths affect the exploration map on return.
- The transition presentation, return point, defeat handling, and arena-selection
  policy require definition in the world and quest blueprint.
- This supports one combat system rather than excursion-specific action combat.

### Supersedes

The assumption that an excursion battle must occur at literal scale inside the
walkable location where the encounter was discovered.

### References

- `docs/design/excursion-combat-format.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Move from catalog-first enrollment to professor-led academic quest chains

**Status:** Accepted
**Areas:** Courses, Quests, Professors, Campus, Progression, UI

### Decision

Retain the measurable four-year Academy structure while moving the lived course
experience from a catalog-first lesson tree to professor-led academic quest
chains. Professors have regular campus locations, steward their chains, and may
appear elsewhere as those quests progress. They do not each require a unique
classroom.

Some academic opportunities are announced and others remain hidden until
discovered. One Journal organizes known opportunities, active quests, and
completed history. Accepting an academic chain clearly and permanently commits
its stated share of that year's curriculum capacity; completing the chain turns
that commitment into progress toward finishing the year.

### Context

The existing catalog and course tree make progression legible but disconnect
courses from professors, the walkable campus, quests, and exploration. A purely
professor-driven model would make the world matter but could obscure the
player's obligations and permanent tradeoffs. The accepted model gives
characters ownership of courses while the Journal and curriculum-capacity
display preserve measurable progression and clarity.

### Consequences

- The node-based course tree is no longer the primary enrollment or lesson-launch
  interface.
- Professors, NPC state, quest state, Journal state, curriculum commitment, and
  yearly progression must share one authoritative flow.
- Announced opportunities appear automatically; hidden opportunities cannot
  appear before their discovery condition is met.
- Acceptance must show the cost, remaining capacity, and permanence before the
  player confirms.
- The exact yearly capacity, individual chain costs, Journal and HUD layout,
  non-academic active-quest limit, hidden-opportunity disclosure, and retained
  purpose of the Class Hall remain dedicated design work.

### Supersedes

The catalog-first assumption in the original Academy model and the current
Course Flow's role as the authoritative launcher for every lesson. The four-year
Academy journey, limited curriculum budget, transcript identity, and graduation
capstone remain in force.

### References

- `docs/archive/suspended-progression-models-2026-08/design/academy-class-flow.md`
- `docs/archive/suspended-progression-models-2026-08/design/academy-forging-model.md`
- `docs/design/walkable-academy-hub.md`
- `docs/archive/suspended-progression-models-2026-08/tracking/completion-roadmap.md`

## 2026-08-16 — Make the Spellbook a persistent campus action

**Status:** Accepted
**Areas:** Campus, Collection, Decks, Navigation, UI

### Decision

Expose card collection and deck management through a persistent Spellbook button
on the left side of the walkable-campus HUD. This replaces the Dorms as the
physical entrance to the collection/deck screen.

### Context

The collection and active deck are frequently used player tools rather than a
place-specific campus activity. Requiring the player to locate or shortcut to a
Dorms building adds navigation without strengthening the campus fantasy. A
Spellbook action communicates the feature more directly and keeps it available
while the player is exploring the campus.

### Consequences

- The Dorms are removed from the current campus destination roster.
- The collection/deck screen remains authoritative; the campus HUD only routes
  to it.
- The Spellbook is visible during campus exploration and remains available in
  the general shortcut list.
- Future Dorms content requires a separate purpose rather than inheriting card
  and deck management by default.

### Supersedes

The Dorms-to-`SCENE_COLLECTION_SCREEN` routing in the bounded walkable Academy
hub decision.

### References

- `docs/design/walkable-academy-hub.md`

## 2026-08-16 — Group the Spellbook with the right-side campus actions

**Status:** Accepted
**Areas:** Campus, Collection, Decks, Navigation, UI

### Decision

Place the persistent Spellbook button in the right-side campus action rail with
the Journal, Inventory, and shortcut menu rather than on a separate left-side
rail.

### Context

The Spellbook is one of the player's persistent meta tools. Grouping those tools
in one action rail is more coherent than dividing them across both screen edges.

### Consequences

- The campus has one persistent action rail rather than separate left and right
  rails.
- The Spellbook continues to replace the Dorms as the collection/deck entrance.

### Supersedes

Only the left-side placement specified by `Make the Spellbook a persistent
campus action`; the persistent access and removal of the Dorms route remain in
force.

### References

- `docs/design/walkable-academy-hub.md`

## 2026-08-23 — Use Escape for the campus system menu

**Status:** Accepted
**Areas:** Campus, Settings, Navigation, UI

### Decision

On the walkable campus, Escape opens a modal system menu instead of routing
directly to Settings. The menu pauses campus activity and offers Resume,
Settings, and Quit Game. Quit Game requires confirmation and exits the
application.

### Context

Settings must remain quickly accessible, but opening it directly from Escape
does not provide the conventional resume-or-exit choices expected from a game
system menu. The title screen is currently a loading splash rather than a usable
return destination, so Quit Game exits the application.

### Consequences

- Escape within the system menu resumes campus play.
- Escape from its Settings surface returns to the system menu.
- The system menu reuses the shared categorized settings component.
- Dialogue, quest, and reward overlays retain first priority for Escape.

### References

- `scenes/meta/components/campus_system_menu.tscn`
- `scenes/shared/settings_panel.tscn`

## 2026-08-23 — Place future Friends access in the persistent campus HUD

**Status:** Accepted placement; implementation deferred
**Areas:** Campus, Social, Online, Quests, UI

### Decision

Treat Friends as a future global social system and reserve its entry point in
the walkable campus HUD's right-side action rail. It is a contextual panel, not
a physical campus destination or a Settings category. Do not ship a dead button
solely to reserve the position.

### Context

Friends may eventually support relationship management, battle and joint-quest
invitations, shared-map parties, gifting, and player presence in a populated
Academy hub. Much of that scope may arrive after release, but the current HUD
design should not make the later social system awkward to introduce.

### Consequences

- The current designer handoff reserves conceptual rail capacity but does not
  require Friends screens or functionality.
- Social capabilities can be introduced in independent slices rather than as
  one monolithic feature.
- Shared Academy presence remains an instanced, bounded-hub direction rather
  than implying an unrestricted open world.
- Gifting and cooperative rewards must preserve exclusivity and progression
  constraints.

### References

- `docs/design/friends-and-shared-presence.md`
- `docs/design/walkable-academy-hub.md`

## 2026-08-23 — Remember a separate ranked deck for each summoner

**Status:** Accepted
**Areas:** Online, Ranked, Summoners, Decks, UI

### Decision

The Online screen shows the globally active summoner and a separately persisted
ranked deck before queueing. Ranked deck choice is remembered per summoner and
does not change the deck selected for offline activities. A summoner without a
valid ranked deck cannot enter matchmaking and must choose one through the
existing collection/deck-management flow.

### Context

Cards and decks belong to individual summoners, while players may prepare a
different deck for offline encounters. A global ranked selection or reuse of the
offline active deck would either cross summoner ownership boundaries or make one
mode unexpectedly alter another.

### Consequences

- Changing summoners from Online still changes the global active summoner.
- Returning to Online restores that summoner's last ranked deck.
- Ranked matchmaking and deck exchange use ranked selection exclusively.
- The collection screen gains a contextual confirmation state instead of a
  second deck-management implementation.

### References

- `docs/design/ranked-loadout-flow.md`

## 2026-08-23 — Separate world travel from persistent UI actions

**Status:** Accepted
**Areas:** Campus, Excursions, Quests, Navigation, UI

### Decision

Replace the broad campus shortcut menu with a Travel/Wayfinder action for
physical navigation. Travel can span campus landmarks and future excursion
regions. Persistent tools such as Journal, Spellbook, Inventory/Summoner, and
Settings retain their existing UI homes and do not appear as travel entries.

When a tracked quest has an eligible physical destination, Travel may surface
its nearest valid waypoint. Selecting it moves the player to the waypoint—not
directly onto the quest objective—so travel cannot bypass traversal, encounters,
locked paths, or discoveries.

### Context

The existing shortcut catalog conflates direct screen routing with movement
through the world. That duplicates persistent buttons and weakens the meaning of
the walkable campus. A quest-aware waypoint system preserves convenience while
scaling to forests, ruins, underground areas, and other bounded locations.

### Consequences

- Physical buildings remain interaction points for the screens they represent.
- Waypoints may be initially available or discovery-gated according to authored
  context.
- Secret locations are not automatically exposed as travel destinations.
- The Travel control can expand beyond campus without becoming a universal menu.

### Supersedes

The shortcut-menu rule that every screen and campus destination must appear in
one direct-routing list.

### References

- `docs/design/walkable-academy-hub.md`

## 2026-08-23 — Present the Summoner Profile over the active world

**Status:** Accepted
**Areas:** Summoners, Campus, Navigation, Progression UI

### Decision

Present the Summoner Profile as a fixed-size centered character-sheet overlay
instead of an edge-to-edge destination screen. When opened from the walkable
campus, the campus remains visible and dimmed behind the profile. Identity and
build management remain equally important: character art, level and XP, equipped
items, stats, and owned traits share the profile without restoring the full
Inventory grid merely to fill space.

### Context

Removing owned Inventory left the full-screen composition with unused space.
Adding unrelated information would overload the feature, while stretching the
remaining sections would make the hierarchy weaker. A large overlay gives the
character and progression information enough room without pretending they need
an entire viewport or disconnecting the player from the campus context.

### Consequences

- Campus profile access opens and closes in place rather than performing a scene
  transition.
- The world pauses player movement while the profile is open and resumes it on
  close.
- Trait development and equipment selection can open above the profile.
- Legacy routes may host the same reusable profile surface; they do not own a
  duplicate layout.
- Final art, typography, and information styling remain designer work.

### References

- `docs/design/walkable-academy-hub.md`
- `docs/design/discovery-driven-development.md`

## 2026-08-23 — Reuse distinct utility overlays across player contexts

**Status:** Accepted
**Areas:** Campus, Navigation, Quests, Cards, Inventory, Online UI

### Decision

Present the Summoner Profile, Spellbook/Deck manager, Journal, and Inventory as
four distinct utility overlays rather than full-screen destinations or sections
of one combined modal. When invoked from a walkable space, the host remains
visible and dimmed while traversal pauses. The same Spellbook/Deck overlay may
also be hosted over Online for ranked deck selection.

Online remains a full destination screen. Encounter preparation retains its
activity-specific embedded loadout editor because that surface owns encounter
constraints and supplied cards; it is not interchangeable with general
collection management.

### Context

These utilities support inspecting or changing persistent player state without
requiring the player to mentally leave the current place. Reusing each utility
across hosts avoids duplicate layouts, while keeping them separate preserves
their different information needs. Not every major game surface benefits from
this treatment: Online and activity preparation each own context that warrants
a dedicated composition.

### Consequences

- Each utility uses a fixed, centered surface with dimming and a clear close or
  Escape path.
- Closing a utility resumes the exact host context that opened it.
- Ranked deck selection and ordinary deck management share one implementation
  with context-specific confirmation behavior.
- Standalone utility routes may remain as compatibility fallbacks, but do not
  define alternate UI designs.

### References

- `docs/design/walkable-academy-hub.md`
- `docs/design/quest-system.md`
- `docs/design/ranked-loadout-flow.md`

## 2026-08-23 — Remove prose description from the Summoner Profile

**Status:** Accepted
**Areas:** Summoners, Progression UI

### Decision

Remove the separate identity/description panel from the Summoner Profile. The
summoner's portrait and name communicate immediate character identity, while
Stats occupy the full upper region of the build-information column. Traits and
equipped items remain the other build-defining sections.

### Context

The temporary character description did not justify a permanent profile region
and competed with information the player uses to understand the active build.
Character writing can be authored as content without reserving structural space
for it on this management surface.

### Consequences

- The profile contains no prose description or `IDENTITY` heading.
- Stats are no longer constrained to half of the upper right column.
- Summoner configuration descriptions remain valid content for other contexts.

### References

- `docs/design/walkable-academy-hub.md`

## 2026-08-23 — Reserve account-wide item ownership for authored event exclusives

**Status:** Accepted; clarifies the earlier Inventory-binding decision
**Areas:** Items, Inventory, Events, Summoners, Persistence, Rewards

### Decision

All normal gameplay items are owned by the summoner who acquires them. An
event-exclusive item may be authored as either summoner-bound or account-wide;
event provenance alone does not force either ownership mode. Ordinary Shop,
quest, campaign, and world-acquired gameplay items are not account-wide.

### Context

Summoner ownership preserves distinct builds and collection choices. Some
limited event rewards may still need account-wide reach so the event does not
require repetitive acquisition for every summoner, but that is an explicit
content choice rather than the default item rule.

### Consequences

- The normal item grant path requires an active summoner and binds the item to it.
- Event-exclusive reward definitions must declare their ownership target.
- Shared event items remain visible to eligible summoners and require a clear
  shared-state presentation.
- The persistence migration must convert legacy blanket-account-wide gameplay
  items without removing support for explicitly shared event exclusives.

### Supersedes

This clarifies the 2026-08-23 gameplay-Inventory binding entry, whose wording
could be read as forbidding every account-wide gameplay exception.

### References

- `docs/features/items/system.md`
- `docs/features/equipment-system.md`
