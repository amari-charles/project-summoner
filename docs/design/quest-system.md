# Quest System

**Status:** Accepted foundation — introductory vertical slice pending
**Type:** Product/design intent (source of truth for quest experience)
**Accepted:** 2026-08-16

## Purpose

Quests are the connective structure between professors, courses, campus life,
excursions, battles, rewards, shops, discoveries, and progression. They should
make the Academy feel physically inhabited without obscuring the measurable
four-year curriculum or turning every interaction into a menu.

The intended experience is classical and readable: characters visibly offer
known quests, dialogue begins and resolves them, one objective can be tracked in
the walkable-world HUD, and one Journal organizes the complete quest record.

## Quest Vocabulary

- **Opportunity:** A quest or quest chain the player knows about but has not
  accepted.
- **Academic chain:** A course expressed through connected quests. Accepting it
  permanently commits curriculum capacity.
- **Side quest:** A non-academic quest. Side quests do not consume curriculum
  capacity and have no hard active-count limit.
- **Active quest:** An accepted quest that is not complete.
- **Tracked quest:** The one active quest whose current objective appears in the
  exploration HUD.
- **Objective:** The current actionable requirement within a quest.
- **Turn-in:** A character interaction that resolves an objective or quest when
  authored closure matters.
- **Hidden opportunity:** An authored opportunity that remains absent from
  markers and the Journal until its discovery condition is satisfied.

## Character and Marker Rules

Professors are the stewards of academic chains. Each professor has a regular
place on campus and may appear elsewhere when an authored quest calls for it.
Professors do not require individual classroom interiors.

Character quest markers use three baseline states:

- `!` — a known, currently available opportunity;
- `?` — an active quest is ready to progress or turn in at this character;
- no marker — ordinary conversation, locked content, future content, or a hidden
  opportunity that has not been discovered.

Hidden quests require an explicit data representation, but hidden-discovery
presentation is deferred beyond the first vertical slice.

## Navigation and World Guidance

The campus is intended to be compact and learnable through named landmarks, so a
minimap is not a foundational requirement. Journal entries for known
opportunities identify the professor and their landmark, such as a lakeside or
mountain-side teaching area.

The intended future guidance tool is an optional magical trail for a tracked
quest. It is deferred until playtesting demonstrates the need and should not
block the initial quest implementation.

## Acceptance Flow

Quest acceptance occurs naturally within character dialogue:

`Talk to character → hear a general overview → Accept or Not Yet`

There is no required standalone quest-contract screen. The player does not need
to see every quest step, exact reward, or future consequence before accepting.

Character dialogue, Journal copy, and internal activity labels are separate
authoring layers. A professor speaks in their own voice about the situation and
the player's role; internal labels such as `Practice` are never substituted for
spoken lines. Short mechanical callouts such as the assignment title, objective,
curriculum cost, and permanence may appear in an accent color inside the
conversation so their gameplay significance remains unmistakable.

For an academic chain, the final dialogue choices must communicate:

- the exact curriculum cost on the **Accept** choice;
- that the commitment is permanent.

Remaining capacity may also appear there, but is optional when another visible
part of the interface already makes it clear. Accepting automatically tracks the
new quest.

## Active Quest Presentation

Walkable exploration screens show a single one-line quest banner directly below
the player profile icon:

`Quest title — current objective`

Only the tracked quest appears. Clicking the banner opens that quest's details
in the Journal. The Journal and banner do not appear during battles or online
PvP.

Persistent navigation controls live in a stable top-right or right-edge region
of the walkable-world UI. The Journal is available there and through a hotkey.

## Journal

The Journal is available from any walkable exploration space. Its accepted
structural layout is a full-screen three-region quest log:

- **Category rail:** Active, Open, and Completed, with one category selected at
  a time.
- **Quest list:** only the quests in the selected category.
- **Quest detail:** the selected quest's title, description, current objective
  when active, professor or source portrait and name, named location,
  curriculum cost when applicable, and known rewards.
- **Header:** current year and curriculum-capacity status.

The visual skin may become an Academy folio or magical notebook, but that styling
must preserve the three-region information structure. The initial stacked-card
Journal is graybox scaffolding, not the accepted final layout.

Some announced opportunities are added automatically and produce a small
notification. Hidden opportunities do not appear until discovered. The Journal
contains all active side quests; only HUD tracking is limited to one.

## Progression and Completion

Every quest progresses through an ordered set of typed steps. A step identifies
one player-facing objective and the authoritative event that completes it, such
as talking to a particular NPC, interacting with a world location, completing
an Academy battle activity, or returning to a quest giver. The Journal and HUD
show the current step; they do not expose or launch a parallel course-node flow.

Academy battle activities remain reusable gameplay definitions for battle
configuration, preparation, rules, loadouts, and rewards. They are referenced
by quest steps rather than acting as the quest progression model themselves.

Quest objectives advance from authoritative gameplay events. Returning to a
character is required only when dialogue, a decision, a reward, or narrative
closure adds value. Routine objectives may advance automatically.

When a turn-in is required, resolution occurs naturally during closing dialogue;
there is no separate **Complete Quest** button. Fixed rewards are delivered in
dialogue with a compact received notification. A focused reward-choice interface
appears only when the reward actually requires player choice.

Academic capacity is permanently committed at acceptance. Completion converts
that commitment into completed academic progress. Abandonment cannot refund the
capacity.

## Initial Faculty and Dependency Chain

The initial graybox faculty contains five persistent campus professors:

- one general-magic professor;
- one professor for Fire;
- one professor for Water;
- one professor for Earth;
- one professor for Wind.

All five physically exist from the beginning. Only currently available quests
receive markers. The general professor begins as a supportive mentor: reassuring
without removing the student's responsibility, attentive to learning rather
than performance, and clear about the assignment. The other professors'
personalities and all final art remain later content work.

The accepted opening dependency is:

`Introduction to Magic → Summoning Basics OR Practical Spellcraft → four elemental opportunities`

Introduction to Magic is fixed but not auto-accepted. The general professor
begins with `!`, and the player accepts through dialogue. After the introduction,
the same professor offers the mutually exclusive Summoning Basics and Practical
Spellcraft chains. Completing either unlocks the four elemental professors'
introductory opportunities.

The elemental professors occupy compact, subject-appropriate landmarks within
one continuous central campus rather than separate maps or a selection lineup:

- Earth near the mountain or rocky edge;
- Water near the lake or shoreline;
- Fire near an appropriate outdoor practice area to be finalized;
- Wind near an elevated or exposed area to be finalized.

These are campus landmarks, not miniature biomes or bespoke classroom interiors.

## First Vertical Slice

The first playable proof uses placeholder professor identities and existing
battle content as scaffolding:

1. The general professor offers Introduction to Magic with `!`.
2. Dialogue supplies a general overview and inline Accept/Not Yet choices.
3. Acceptance permanently commits the displayed curriculum cost and tracks the
   quest.
4. The current step directs the player to a physical Practice Grounds
   interaction on campus.
5. Interacting there opens Activity Preparation for one basic training battle.
6. Completing the battle returns the player to campus and advances the current
   step to the general professor.
7. The general professor gains `?`.
8. Closing dialogue completes the chain and unlocks the first foundation fork.

The slice must prove NPC interaction, markers, acceptance, persistence, Journal
projection, HUD tracking, battle-driven objective advancement, turn-in, and
dependency unlocking. It does not establish final quest writing or course
content.

The old Class Hall enrollment browser and full-screen Course Flow are deprecated
in their entirety. They must not remain as alternate enrollment, activity
selection, launch, progression, or return paths. Activity Preparation and battle
results remain reusable, but they enter and exit through the active quest step.

## Deferred Decisions and Work

- Final professor names, the four elemental professors' personalities, visual
  designs, and full dialogue passes.
- Final elemental-landmark layout and campus art.
- Hidden quest discovery presentation and content.
- Magical-trail implementation.
- Final marker art and Journal/folio styling.
- Exact yearly curriculum budgets and final course costs.
- Final side-quest roster and authoring policy.
- Final introductory battle and reward content.
