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
- `docs/tracking/completion-roadmap.md`

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
- `docs/tracking/completion-roadmap.md`

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
- `docs/design/academy-class-flow.md`
- `docs/technical/meta/quest-step-rearchitecture-proposal.md`

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
  `docs/technical/meta/quest-step-rearchitecture-proposal.md`.

### Supersedes

Any remaining interpretation that the old Class Hall or Course Flow might keep
responsibility for enrollment, activity selection, activity launch, or course
progression.

### References

- `docs/design/quest-system.md`
- `docs/design/academy-class-flow.md`
- `docs/technical/meta/quest-step-rearchitecture-proposal.md`

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
- `docs/technical/meta/quest-system-foundation-plan.md`

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
- `docs/design/academy-class-flow.md`
- `docs/design/walkable-academy-hub.md`
- `docs/technical/meta/quest-system-foundation-plan.md`
- `docs/tracking/completion-roadmap.md`

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

- `docs/tracking/completion-roadmap.md`

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
- `docs/tracking/completion-roadmap.md`

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

- `docs/design/academy-class-flow.md`
- `docs/tracking/completion-roadmap.md`

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

- `docs/design/academy-forging-model.md`
- `docs/tracking/completion-roadmap.md`

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

- `docs/design/academy-class-flow.md`
- `docs/design/academy-forging-model.md`
- `docs/tracking/completion-roadmap.md`

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

- `docs/tracking/completion-roadmap.md`
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

- `docs/tracking/completion-roadmap.md`

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
- `docs/design/academy-class-flow.md`
- `docs/tracking/completion-roadmap.md`
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

- `docs/design/academy-forging-model.md`

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
- `docs/tracking/completion-roadmap.md`

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
- `docs/tracking/completion-roadmap.md`

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
- `docs/tracking/completion-roadmap.md`

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
- `docs/tracking/completion-roadmap.md`

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

- `docs/design/academy-class-flow.md`
- `docs/design/academy-forging-model.md`
- `docs/design/walkable-academy-hub.md`
- `docs/tracking/completion-roadmap.md`
