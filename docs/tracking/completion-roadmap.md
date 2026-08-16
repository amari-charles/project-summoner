# Fateforged Completion Roadmap

**Status:** Collaborative working draft
**Created:** 2026-08-14
**Current focus:** Define the product foundation before decomposing medium-scope implementation work

## Purpose

This roadmap organizes the work required to move Fateforged from foundation-building into repeatable content production and, eventually, completion.

It does not treat unresolved product design as if it were settled. Early roadmap initiatives are bounded definition and feasibility efforts with concrete outputs. Their results will determine the later implementation plan.

Product intent remains in `docs/design/`, `docs/lore/`, and `docs/project/`. This tracking document records sequencing, dependencies, expected outputs, and completion gates.

## Confirmed Product Anchors

- The player begins an Academy journey, develops a distinct summoner, graduates through a capstone, and takes that summoner into online PvP.
- Exclusivity remains central. One summoner cannot experience every progression path or obtain every offered outcome. The exact reach of exclusivity across cards, items, upgrades, and cracked variants remains part of progression and power-model design.
- The exact number and naming of progression stages are not foundational decisions. Years, semesters, terms, ranks, or another structure can be chosen during progression design.
- The bounded walkable campus creates opportunities beyond static menu navigation.
- Quests should connect lessons, characters, locations, battles, rewards, shops, and discoveries rather than exist as an isolated side system.
- The first excursion should use movement and interaction between recognizable Fateforged battles. More experimental combat formats remain an intentional innovation track, not a prerequisite for beginning excursion work.
- Summoners remain stationary during combat across excursions and standard 1v1.
  Walkable exploration transitions into the same horizontal battle format; the
  moving-summoner greybox remains research evidence rather than production scope.
- Encounters found in bounded exploration spaces transition to separate,
  excursion-themed horizontal battlefields, then return the player to the
  exploration state with persistent outcomes. Battlefields do not need to fit
  literally inside the exploration map geometry.
- The campus is the recurring home base, but an excursion may continue through
  physically connected sub-locations before returning there.
- Courses primarily structure quests and battles. Dedicated classroom interiors
  are not foundational map requirements and must be justified by gameplay rather
  than by the existence of a course.
- Accepting an academic quest chain permanently commits its stated share of the
  player's limited curriculum capacity. Completion turns that commitment into
  earned yearly progress; abandonment cannot be used to reclaim the capacity.
- One Journal owns accepted quests, known opportunities, and completed history.
  Announced opportunities appear automatically; hidden opportunities appear
  only after world discovery. The Journal always exposes current year and
  committed-versus-completed academic capacity.
- Cracked cards are planned as risky variations of normal cards whose twist can enable unusual synergies without being strictly beneficial.
- Card cracking is an illicit, secret activity rather than an ordinary public
  Academy service. Its physical location and discovery path remain part of the
  world blueprint.
- A covert campus contact provides access to a persistent bounded tunnel area
  beneath the Academy. Its minimum world roles are cracking and ritual rooms;
  it is a small secondary hub, not a required black-market district or combat
  excursion.
- Elemental and summon design should begin with the creature's identity, then derive its abilities, stats, and upgrades.
- Card upgrade trees can contain permanently exclusive behavioral branches. Some
  branches may require gathered materials and an authored ritual, but ritual
  gating is an acquisition requirement rather than an independent power layer.
- The roadmap's near-term purpose is to complete reusable foundations so later work is primarily content authoring, tuning, and asset replacement.

## Planning Rules

- Do not choose a map roster before defining the activities those maps must support.
- Do not give every feature its own location. A location must justify its production cost through player value and reuse.
- Do not design cracked variants before normal card identities and balance expectations are coherent.
- Do not fill an item shop before defining how items contribute to player power and what economic job gold performs.
- Treat uncertain high-reward ideas as feasibility efforts with exit decisions, not as silently approved implementation scope.
- Keep later phases at outcome level until the strategic definition phase produces enough evidence to decompose them safely.

## Roadmap Backbone

| Phase | Outcome | Planning maturity |
| --- | --- | --- |
| 1. Define the Product Foundation | The intended engagement, progression, power, economy, content, and world models are coherent enough to scope. | Active; initiatives defined below. |
| 2. Build Reusable Foundations | Required shared systems, authoring and validation tools, greybox spaces, and content contracts are implemented. | Authoring pipeline committed; remaining detail follows Phase 1. |
| 3. Prove Representative Experiences | A small number of end-to-end experiences validate the systems, production model, and core battle baseline. | Core-battle validation gate committed; representative slices remain to be selected. |
| 4. Reach Content-Production Readiness | Ordinary quests, lessons, encounters, creatures, items, maps, and variants can be added without redesigning shared foundations. | Gate criteria agreed and recorded below. |
| 5. Fill, Balance, and Finish | Planned gameplay content, characters, lore, quests, and presentation are authored; placeholders are replaced; and the complete product is balanced and hardened. | Planning package. |

## Phase 1 — Define the Product Foundation

### Player Journey and Progression Spine

**Urgency:** High
**Ease:** Medium
**Scope:** Large

**Purpose:** Turn the confirmed Academy-to-graduation direction into a complete high-level player journey without prematurely fixing the number of years or semesters.

**Required outputs:**

- End-to-end journey from first arrival through graduation and online entry.
- Progression-stage model and pacing principles.
- Mechanism or mechanisms that create exclusivity.
- Rules for repeatable, irreversible, failed, missed, and post-graduation content.
- Relationship between quests, classes, exploration, rewards, and graduation.

**Exit result:** An approved progression blueprint that downstream systems can implement without inventing progression rules independently.

**Likely files:**

- `docs/project/vision.md`
- `docs/design/academy-forging-model.md`
- a future current progression design document

### Excursion Combat Format and Innovation Feasibility

**Urgency:** High
**Ease:** Medium
**Scope:** Medium

**Purpose:** Build the first excursion around exploration between recognizable Fateforged battles while investigating whether more experimental combat formats create enough distinctive, reusable value to justify their cost.

**Representative prototypes:**

- Exploration and world interaction leading into the standard battle format as the implementation baseline.
- Bounded movement with spell-only combat.
- Bounded movement with limited summons.
- A room- or encounter-bounded structure as one containment strategy, not an accepted final design.

**Required evaluation:**

- Mobile and desktop controls while moving, targeting, and playing cards.
- Camera behavior and battlefield readability.
- Definition of encounter start, completion, failure, and transition.
- Health, mana, and other resources across an extended activity.
- Summon spawning, following, expiration, recall, room transitions, and cleanup.
- Compatibility with Fateforged's army-battle identity.
- Engineering cost, asset cost, and reusable quest/content value.

**Exit result:** The baseline excursion format is playable using standard combat encounters. Any additional combat format is either accepted with a defined encounter boundary and summon-lifecycle model, kept experimental, or rejected based on evidence.

**Likely files:**

- `docs/design/excursion-combat-format.md`
- battle input, session, simulation, camera, and navigation systems if prototyping is approved
- reusable prototype scenes under battle or exploration ownership, chosen after the experience boundary is defined

### Player Engagement and Activity Model

**Urgency:** High
**Ease:** Medium
**Scope:** Large

**Purpose:** Define what players regularly do between major progression decisions and which activity families are valuable enough to support as reusable systems.

**Required outputs:**

- Target session rhythm.
- Selected families of exploration, conversation, quest, combat, and progression activities.
- Role of navigation, discovery, and physical presence.
- Boundary between authored, reusable, recombinable, and repeatable content.
- How activities feed cards, gold, items, relationships, exclusivity, and graduation.
- Results from excursion-format experiments where they affect the activity model.

**Exit result:** A small, approved set of activity archetypes that can support the intended experience and justify downstream systems.

**Likely files:**

- a future player-engagement design document
- `docs/design/academy-class-flow.md` when its intended replacement is approved

### Content Scope and Reusability Strategy

**Urgency:** High
**Ease:** Medium
**Scope:** Medium

**Purpose:** Establish how proposed systems, maps, and assets earn a place in the production plan.

**Required outputs:**

- A value-versus-cost rubric covering usage frequency, breadth, player value, engineering cost, asset cost, and content capacity.
- Rules for preferring an existing interaction, reusable interior, reusable environment kit, or bespoke location.
- Minimum reuse expectations for new map and activity foundations.
- A process for identifying content that can be recombined or generated without becoming repetitive.
- A standard for what “base complete but ready for reskinning” means.

**Exit result:** Every proposed foundation can be accepted, reduced, deferred, or rejected using the same production criteria.

**Likely files:**

- this roadmap
- `docs/tracking/remaining-work-scope.md`
- future content-production guidance

### World and Map Blueprint

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Derive the required world topology and base-map roster from the approved engagement model and reuse strategy.

**Required outputs:**

- Relationship between the central campus and any bounded excursion spaces.
- Complete roster of required base locations for the approved scope.
- Gameplay purpose and supported activity archetypes for every location.
- Reuse case, environment kit, greybox scope, content capacity, and asset-replacement plan for every map.
- Travel, unlocking, return, failure, and revisit rules.
- Clear justification when an activity needs a dedicated map instead of an existing campus interaction, room, or interface.

**Exit result:** An approved map plan in which every location has enough value and reuse to justify implementation. No particular woods, ruins, training ground, underground district, or other example is assumed before this work is complete.

**Likely files:**

- `docs/design/walkable-academy-hub.md`
- a future world/map blueprint
- map and biome technical documents after the roster is approved

### Quest and Curriculum Experience

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Define quests as the connective experience across Academy progression rather than layering an unrelated quest log onto the existing interfaces.

**Required outputs:**

- Relationship between courses, lessons, quest chains, professors, other characters, and excursions.
- Quest lifecycle, state, failure, permanence, rewards, and progression integration.
- Responsibilities of the physical world, conversations, journal, course planning, preparation, results, and transcript interfaces.
- A replacement or revision plan for the current course-flow interface.
- Quest authoring model and supported objective/activity types based on the approved engagement model.

**Exit result:** An approved experience blueprint and implementation boundary for a reusable quest foundation.

**Likely files:**

- `docs/design/academy-class-flow.md`
- `docs/design/narrative-dialogue-system.md`
- a future quest-and-curriculum design document

### Overall Player-Power Model

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Define how every source of strength coexists before balancing or expanding any one progression system.

**Required outputs:**

- Roles of base cards, card levels, upgrades, summoner growth, equipment, cracked variants, and any expedition-specific resources.
- Relationship between card XP thresholds, permanently exclusive behavioral
  branches, ritual materials, and other transformation systems.
- Ownership and persistence boundaries for each power source.
- Expected magnitude, specialization, tradeoff, and stacking behavior.
- Guardrails preventing one system from making the others irrelevant.
- Relationship between campaign strength, exclusivity, graduation, and online PvP readability.

**Exit result:** A power-budget model that card stats, upgrade trees, items, cracked cards, rewards, and economy can share.

**Likely files:**

- `docs/design/card-progression-economy.md`
- `docs/design/academy-forging-model.md`
- a future player-power design document

### Gold Economy Model

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Give gold a deliberate economic job instead of treating it as a generic reward counter.

**Required outputs:**

- Gold sources, sinks, pacing, scarcity, and accumulation targets.
- Role of gold in school, exploration, shops, services, and any underground activity.
- Relationship between gold, player power, flexibility, access, information, and risk.
- Repeatability and anti-inflation rules.
- Economic expectations by progression stage.

**Exit result:** An approved economy model that can drive reward placement, shop inventory, pricing, and balance.

**Likely files:**

- `docs/design/card-progression-economy.md`
- `docs/design/academy-forging-model.md`
- a future economy design document

### Item and Shop Power Model

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Define what can be bought or earned and how items make the player stronger, more specialized, or more flexible.

**Required outputs:**

- Supported item categories and ownership/equipment rules.
- Interactions with the summoner, cards, summons, combat, quests, and exploration.
- Item power budget, stacking rules, rarity, and upgrade expectations.
- Acquisition and placement roles for shops, quests, classes, exploration, and special sources.
- Shop purpose, inventory structure, refresh/unlock behavior, and relationship to gold.

**Exit result:** An approved item and shop model ready for catalog design and implementation decomposition.

**Likely files:**

- `docs/features/equipment-system.md`
- `docs/design/academy-forging-model.md`
- item, shop, and economy design documents after the model is approved

### Core Card Roles and Stat Framework

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Establish coherent normal-card identities and balance expectations before broad roster production or cracked variants.

**Required outputs:**

- Card and creature combat roles.
- Stat vocabulary, baseline ranges, budgets, curves, and comparison rules.
- Rarity, spawn-count, mana-cost, upgrade, and specialization expectations.
- Roster coverage and intentional gaps by element.
- Repeatable review and playtest method for card balance.

**Exit result:** Existing and future cards can be evaluated against one shared framework rather than tuned independently.

**Likely files:**

- `docs/project/vision.md`
- element content working notes
- card and combat balance design documents

### Cracked Card Model

**Urgency:** Medium until the normal-card framework is stable
**Ease:** Hard
**Scope:** Large

**Purpose:** Turn the accepted risky-variant concept into a safe, expressive, and authorable system.

**Required outputs:**

- Mutation principles and supported kinds of twists.
- Required risk, collateral effect, or uncertainty expectations.
- Acquisition and player-agency model.
- Ownership, deckbuilding, upgrade, duplicate, and permanence rules.
- Presentation that makes altered behavior and risk understandable.
- Interaction with synergies, balance budgets, items, quests, and online play.

**Exit result:** An approved cracked-card design and authoring model built on stable normal cards. Its presentation must support the accepted illicit, secret character of cracking without assuming that this requires a dedicated district or map.

**Likely files:**

- a future cracked-card design document
- card data and progression systems after design approval

### Battle Creature and Upgrade-Tree Content Bible

**Urgency:** High
**Ease:** Medium
**Scope:** Large

**Purpose:** Replace ability-first elemental filling with deliberate creature-first content design.

**Required outputs:**

- Standard concept sequence: creature fantasy, battlefield behavior, signature interaction, strengths/weaknesses, stats, upgrades, and visual direction.
- Initial manually planned creature roster and elemental coverage.
- Lightweight concept sketches and identity sheets.
- Upgrade-tree structure and content standards.
- Review criteria for distinctiveness, elemental identity, gameplay need, and production feasibility.

**Exit result:** Creatures and upgrades can be produced consistently without starting from detached mechanics.

**Likely files:**

- element content working notes
- `docs/design/summon-traits-v1.md`
- future creature concept and roster documents

## Phase 2 — Committed Foundation Workstream

### Content Authoring and Validation Pipeline

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Make ordinary content additions use stable authoring contracts, reusable templates, and automated validation rather than bespoke integration work.

**Included foundation:**

- Authoring contracts and templates for the approved content types.
- Catalog, reference, and semantic validation.
- Direct launch of authored content for focused testing.
- Deterministic test-state setup.
- Stable typed routing, reward, and other shared content-facing contracts.
- Clear placeholder, acceptance, and later asset-replacement requirements.

**Exit result:** Representative content can be authored and tested through the intended production path without inventing new shared architecture for ordinary entries.

Detailed implementation scope follows the Phase 1 product decisions; this placement does not begin pipeline design or implementation.

## Phase 3 — Committed Validation Gate

### Core Battle Experience Validation

**Urgency:** High
**Ease:** Hard
**Scope:** Large

**Purpose:** Prove that the existing army battle is a stable foundation for large-scale content production.

**Required evidence:**

- Correct combat outcomes for the supported mechanics.
- Stable targeting, attack geometry, and hitbox behavior.
- Acceptable performance at representative army sizes.
- Adequate controls and readability on the target platforms.
- The intended preparation, autonomous army clash, on-field summoners, and in-battle card play remain the baseline 1v1 format unless validation produces a reason to reopen it.
- The standard 1v1 movement rule is coherent; optional excursion experiments do not block this baseline.
- Focused regression coverage for the accepted baseline.

**Exit result:** The core army battle is accepted as content-ready, or specific failed criteria return to Phase 2 as bounded foundation work.

Optional spatial experiments, additional cards, final animation/VFX, client prediction, and general polish do not block this gate unless testing demonstrates that one is required for the accepted baseline.

Production networking, matchmaking, authority, and online hardening do not block content readiness. The 1v1 format and local core battle do.

## Phase 4 — Content-Production Readiness Gate

### General threshold

A foundational system is not complete when it is only designed. It must be implemented, integrated, persisted where applicable, and proven through at least one real end-to-end example using placeholder art where necessary.

The complete catalog for a system can remain content-filling work unless a stricter requirement is listed below.

### Maps and expandable experiences

- Every map required by a core experience exists as a playable greybox.
- The complete future roster of excursion maps or other expandable location families does not need to be planned.
- If an expandable experience family is accepted, at least one representative instance is playable end to end with placeholder art.
- Final environmental art, dressing, and later location variants belong to content filling.

### Quests

- One basic quest can be received from an NPC, tracked, completed, resolved, persisted, and rewarded.
- One additional quest proves cross-system integration with another approved system.
- Every conceivable quest objective type does not need to be implemented before the gate.

### Gold, shop, and items

- Every approved gold source category and sink category is implemented.
- One combined loop proves earning gold, purchasing an item, equipping or using it, and observing its gameplay effect.
- Every approved mechanically distinct item category has at least one working representative.
- Full item catalogs, final prices, and broad economy tuning remain content-filling work.

### Cracked cards

- The real player-facing acquisition or transformation process is implemented; a debug grant is not sufficient.
- The proof covers acquisition, ownership, deckbuilding, risky in-game behavior, and persistence.
- The full cracked-card catalog remains content-filling work.

### Cards, creatures, and upgrades

- A shared card-role and stat framework is validated through a representative balanced subset; the entire future roster need not be balanced before the gate.
- A small curated sample of cards and characters meets the intended quality bar across meaningfully different gameplay functions.
- The sample is selected by function, such as spell versus summon, flying versus grounded, ranged versus melee, and relevant upgrade behavior; it is not selected to satisfy an elemental quota.
- Full creature, card, and upgrade-tree production remains content-filling work.

### 1v1 and online play

- The local core battle proves the intended 1v1 format.
- The first excursion can enter this standard format without requiring a second combat-control model.
- Production networking and online-service hardening can be completed later.

### Explicit later work

The following do not block content readiness:

- full quest, lesson, lore, dialogue, and world-character content;
- complete excursion-map and location rosters;
- complete creature, card, item, upgrade, and cracked-card catalogs;
- final map art, animation, VFX, audio, and presentation polish;
- production networking, matchmaking, authority hardening, and release operations;
- optional combat experiments not required by the accepted core-battle baseline.

## Current-State Gap Audit

**Audited:** 2026-08-14
**Automated baseline:** 1,207 C# tests passing; repeated orphan-node warnings remain non-failing technical hygiene.

The project has substantial reusable infrastructure, but much of the Academy experience was built for the previous static course-flow model. Existing systems should be reused where they fit the approved direction, not preserved as mandatory player-facing structure.

| Track | Current foundation | Content-readiness gap | Ratings |
| --- | --- | --- | --- |
| Player experience, progression, quests, and world/maps | Walkable campus, Academy progression, course screens, narrative runtime, rewards, and persistence exist. The Academy catalog contains 14 courses and 30 battle activities. | No quest domain exists. The engagement model, revised curriculum experience, map blueprint, and any representative excursion remain undefined. Existing course screens and the activity graph are provisional infrastructure. | Urgency High / Ease Hard / Scope Large |
| Core combat and excursion format | Deterministic simulation/session/view architecture and extensive combat coverage exist. | Standard battles are not yet connected to a playable excursion. Damage outcomes, remaining baseline attack geometry, representative-scale performance, and platform controls require closure. Controllable combat movement remains an optional discovery track. | Urgency High / Ease Hard / Scope Large |
| Cards, creatures, upgrades, and cracked cards | 87 card definitions, 54 unit definitions, traits, upgrades, modifiers, stat calculation, and catalog tests exist. | No approved balance framework, creature-first production standard, functionally diverse accepted sample, or cracked-card domain exists. | Urgency High / Ease Medium / Scope Large |
| Player power, gold, items, and shops | Account/campaign gold services, functional card/pack shops, item ownership/equipment, persistence, stat modifiers, four equipment slots, and eight placeholder items exist. | Gold has no approved economic job. Current shops do not sell equipment, item-category coverage lacks focused tests, and purchase flows still require safe universal transaction integration. | Urgency High / Ease Medium / Scope Large |
| Content authoring, validation, and production tooling | Academy, reward, and narrative catalogs have meaningful validation; debug arena presets and a strong automated suite exist. | Authoring is fragmented between JSON and static C# catalogs. Quest authoring is absent, typed battle routing is unfinished, shop/event reward migration is incomplete, and deterministic direct-state tooling remains limited. | Urgency High / Ease Hard / Scope Large |

### Audit evidence

- Active bug tracker: no open bugs.
- Academy catalog: 14 courses, 30 battle activities, 2 authored reward offers, and 1 explicit activity biome assignment.
- Battle environments: 2 biome resources (`summer_plains` and `island_water`).
- Quest-domain implementation: none found.
- Cracked-card implementation: none found.
- Player-controlled summoner movement command: none found; no longer required for the first excursion baseline.
- Item catalog: 8 placeholder stat-modifier items; focused item-service tests were not found.
- Current general shop: sells cards and card packs rather than equipment.

## Existing Backlog Routing

This routing supersedes tracker priority labels only for roadmap sequencing. It does not mark the underlying tasks complete or revise product intent.

### Foundation-path work

- Complete hands-on bounded-campus UX validation; final campus art remains later.
- Replace `scene_path`-driven battle launch with typed runtime routing.
- Formalize damage-outcome semantics.
- Finish only the attack-geometry and hitbox behavior required by the accepted representative combat sample.
- Validate the control/input portion of mobile and desktop compatibility for the standard battle baseline and for any experimental format promoted beyond discovery.
- Complete universal reward migration for foundation consumers, including shop and quest-facing rewards.
- Provide safe local atomic purchase behavior for the approved gold/item economy; remote/provider authority remains later.
- Complete the hot-path performance work required to pass representative-scale core-battle validation.
- Add catalog/localization validation where it is required by the approved content-authoring pipeline.
- Improve deterministic direct-state and content-launch tooling enough to validate representative content efficiently.

### Conditional work after Phase 1 decisions

- Deprecate the legacy Caravan and superseded Academy/course-flow paths only after the replacement journey is approved and wired.
- Complete remaining directional, multi-target, telegraph, hitbox, upgrade-resource, Oath, trait, and summoner-ability work only where the accepted power model and representative sample require it.
- Move tutorial dialogue triggers, remove remaining BattleContext compatibility, or build a campaign/content editor only where the approved quest and authoring boundaries justify the work.
- Continue additional non-hard-lane experiments only if core-battle validation fails for a spatial reason.

### Later content, polish, architecture, and release work

- Additional summon-card production, full creature/trait/upgrade catalogs, and broad balance passes.
- Death animations, final VFX/audio, battle-start camera polish, full HUD/card/settings visual revamps, and debug-menu removal.
- Online deck-selection UI, client prediction, ranked authority, remote permanent-progression authority, and release operations.
- Character-animation composition, runtime typed-ID migration, broad dependency-injection/root-path cleanup, and other architecture improvements not required by a failed foundation criterion.
- Product-direction-log historical backfill; useful documentation hygiene, but not a content-readiness blocker.

## Agreed Dependency Chains

### World and engagement

`Baseline excursion format + optional combat experiments → Player engagement model → Scope/reuse strategy → World/map blueprint → Quest and content foundations`

Quest-and-curriculum design can develop alongside the engagement work, but its final interface and system boundaries depend on the accepted activity and world models.

### Battle content

`Creature-first roster design → Core card roles/stat framework → Upgrade-tree framework → Cracked-card model`

The creature roster and stat framework will iterate together, but cracked variants should not become production content until normal-card identities are coherent.

### Power and economy

`Overall player-power model → Item/shop power model ↔ Gold economy model`

Items and gold must be designed together after the broader sources of player power are bounded.

## Parallel Delivery Strategy

Run two lanes in parallel and synchronize them at roadmap gates.

### Certainty lane

Implement bounded foundations whose product role is already stable. The initial approved groups are:

- formal damage-outcome semantics;
- typed battle-launch routing;
- universal reward and transaction cleanup;
- bounded-campus movement and navigation validation;
- deterministic content-launch and test-state tooling;
- core-battle performance work required by the accepted validation baseline.

Each group remains limited to its roadmap outcome. Certainty-lane work does not authorize adjacent redesign, final polish, or speculative architecture.

### Discovery lane

Resolve high-impact uncertainties through design, feasibility work, and representative prototypes before committing their full implementation scope.

Discovery results can add, remove, or reshape later certainty-lane work. Multiple discovery tracks may proceed in parallel when they do not depend on one another.

The initial approved discovery efforts are:

- excursion-combat innovation experiments;
- player journey and engagement model;
- overall player-power model.

### Integration checkpoints

At each checkpoint:

- review the playable game produced by the certainty lane;
- reassess tentative decisions using the new context;
- reconcile discoveries with implemented foundations;
- promote only sufficiently stable work into the certainty lane;
- stop or reduce work whose expected reuse or player value no longer justifies its cost.

## Transition to Medium-Scope Work

Phase 1 is complete when its outputs form one coherent product foundation and the approved models no longer contradict one another.

Only then should the roadmap be decomposed into medium-scope implementation bundles such as:

- implementing the accepted quest runtime;
- greyboxing an approved reusable location;
- building the accepted expedition prototype into production architecture;
- authoring the first item catalog and shop stock;
- normalizing card stats against the approved framework;
- implementing cracked-card data and behavior;
- producing individual creature concepts and upgrade trees.

The purpose of Phase 1 is not to answer everything in one document. It is to make those implementation bundles stable enough that completing them moves the product toward a known whole.

## Roadmap-Planning Status

The initial critical-path and milestone review is complete:

- [x] Check whether a major top-level effort is missing.
- [x] Separate content-readiness blockers from later product-completion work.
- [x] Order the blockers by dependency and identify work that can proceed in parallel.
- [x] Define the gate for “foundation complete; primarily producing content now.”
- [x] Map existing active TODOs into the roadmap, deferring work that does not support the critical path.

**Exit result:** Met. Stop expanding the roadmap abstractly. Select bounded work from the certainty or discovery lane, execute it, and revise the roadmap only when evidence or an approved direction change requires it.
