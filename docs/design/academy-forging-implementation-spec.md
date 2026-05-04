# Academy Forging Implementation Spec

**Status:** Draft implementation spec
**Date:** 2026-05-03
**Source design:** [Academy Forging Model](academy-forging-model.md)

## Goal

Replace the route-map campaign spine with an academy curriculum spine:

- four years
- two semesters per year
- limited Enrollments per semester
- course catalog
- multi-step class arcs
- transcript, grades, Honors, and permanent rewards
- campus shop economy
- graduation capstone
- graduated summoner enters online PvP

The first implementation should prove the loop with Year 1 content before expanding the full academy.

## MVP Scope

Implement enough to play through:

- Year 1, Semester 1
- Year 1, Semester 2
- placeholder future semesters/years
- placeholder Graduation Capstone gate

The MVP does not need formal majors, profession systems, remediation systems, pre-match opponent scouting, or complex event webs.

## Core Data Concepts

### Academy Progress

Extend or replace current campaign progress with academy-oriented state:

- current year
- current semester
- remaining Enrollments
- completed classes
- class grades
- Honors eligibility/marks
- transcript entries
- earned rewards
- campaign gold
- completed official assessments
- completed practice activities if needed for local class flow

Existing persistence currently centers on `CampaignProgress`, `CompletedBattles`, `CurrentBattle`, `PendingReward`, and `Gold`. The academy model can migrate in phases by adding academy fields before removing map-era fields.

### Course Definition

A course/class definition should include:

- class id
- display/localization key
- year/semester availability
- track
- Enrollment cost
- prerequisites
- Honors requirements if applicable
- reward preview
- activity list
- assessment ids
- repeatable practice flags
- grade/Honors objective definitions

### Course Activity

A course activity can be:

- lesson/story beat
- practice battle
- official assessment battle
- lab/special-rule challenge
- reward choice
- shop/event unlock

Practice activities may be repeatable. Official assessments and reward choices are permanent by default.

### Reward Preview

The catalog should support:

- fixed preview
- fixed choice preview
- pool/category preview
- conditional preview for Honors or grade-based access

Reward categories can include cards, traits, equipment, consistency tools, transcript eligibility, shop unlocks, gold, titles, cosmetics, or status.

Deck editing itself is not a reward category. Players can freely edit using owned tools.

### Campus Shop

The shop should be accessible from the Academy Hub and rotate stock by semester.

Shop data should support:

- item id
- price
- stock limit
- semester availability
- prerequisites/access gates
- exclusive item flag
- reward payload

Gold should be earned once from official progression, not repeatable practice.

## Year 1 Content

### Semester 1

Required:

- `Introduction to Magic 101`
  - grants one neutral/basic summon
  - grants one neutral/basic spell

Required foundation choice:

- `Summoning Basics`
  - rewards a summon
- `Practical Spellcraft`
  - rewards a spell

Element elective:

- `Intro to Fire`
- `Intro to Water`
- `Intro to Earth`
- `Intro to Air`

Each intro element class grants one elemental summon and one elemental spell.

Expected Semester 1 outcome:

- at least two summons
- at least two spells
- one extra summon or spell from the foundation choice

### Semester 2

Semester 2 uses a broader course catalog. Initial available classes:

- `Foundations of Magic II`
- `Introduction to Empowerment`
- `Introduction to Mana Channeling`
- any untaken `Intro Element` class
- follow-up class for the element taken in Semester 1

`Introduction to Empowerment` introduces chosen upgrades/traits.

`Introduction to Mana Channeling` teaches channeling more mana through a summon card, allowing larger summons or more units called at once.

## Systems To Implement

### Architecture Guardrails

- C# owns academy domain rules: course availability, activity state, completion, reward payloads, grade/Honors outcomes, gold, and persistence.
- GDScript academy screens render returned view models and send explicit user intents. They should not recreate progression rules from raw state.
- Course rewards should live with course definitions. Reward previews and reward grants must not be maintained in separate hardcoded maps.
- Battle completion should identify the exact academy activity being resolved. Avoid generic "complete next" calls that hide state assumptions.
- Reusable UI components should be extracted as the academy UI hardens, especially for course cards, activity nodes, modals, and map/path rendering.

### Catalog / Data

- Add academy course catalog data.
- Add Year 1 course definitions.
- Add placeholder Year 2-4 semester definitions.
- Add semester shop stock definitions.
- Keep localization keys out of hardcoded UI text.

Likely existing areas:

- `scripts/csharp/Infrastructure/Data/Events/`
- `scripts/csharp/Meta/Services/Campaign/`
- `localization/data/en.json`

### Progression Service

Add service behavior for:

- loading available courses for current semester
- spending Enrollments
- starting a class
- completing practice activity
- completing official assessment
- assigning grade/Honors outcome
- granting rewards
- advancing semester/year
- detecting Graduation Capstone readiness

Likely existing areas:

- `CampaignService`
- `CampaignProgressHandler`
- `CampaignRewardHandler`
- `NodeUnlockHandler` or a new academy unlock handler

### Persistence

Persist:

- current academic position
- Enrollments remaining
- completed classes
- transcript entries
- grades/Honors
- gold
- shop purchases/stock limits
- pending reward choices

Add backward-safe defaults so existing profiles can load.

Likely existing areas:

- `CampaignProgress`
- `PendingRewardData`
- `DtoConverters`
- `ProfileDataMapper`
- `ProfileRepository`
- persistence tests

### UI / Flow

Replace or adapt campaign map UI into an Academy Hub that behaves like a campus map, not a generic app menu. Use hub-map references such as:

- Game UI Database hub/map examples: https://www.gameuidatabase.com/index.php?scrn=6&scroll=150

Academy Hub should expose major campus destinations:

- Class Hall
- Dorms
- Campus Shop
- Mission Hall / Events placeholder
- Semester status

Class Hall should be the focused course-management screen and should show:

- enrolled classes
- open classes
- completed class history / transcript summaries when needed
- unavailable classes with prerequisites/reason
- Enrollment cost
- reward preview
- practice/assessment structure summary
- Honors availability when relevant

### Battle Launch

Class activities should launch battles using the existing battle/session pipeline where possible.

Battle results should return enough data to determine:

- completion
- win/loss
- performance objectives
- grade/Honors outcome
- gold payout
- reward unlocks

### Graduation Capstone

For MVP, implement as a placeholder locked final activity after Year 4. The full capstone design can come after Year 1 loop validation.

## Testing

Add tests for:

- Semester 1 guarantees at least one summon and one spell from Magic 101.
- Semester 1 available choices include one foundation choice and one element elective.
- Intro element classes grant one elemental summon and one elemental spell.
- Semester 2 includes Foundations II, Empowerment, Mana Channeling, untaken elements, and chosen-element follow-up.
- Enrollments decrement only when enrolling in a class.
- Practice activities do not award repeatable gold.
- Official assessments can award gold once.
- Deck editing is not modeled as a reward gate.
- Persistence round-trips academy progress.
- Existing profiles load with academy defaults.

## Suggested Build Order

1. Add academy data models and persistence fields with tests.
2. Add Year 1 course catalog data and availability logic.
3. Add enrollment and class-completion service methods.
4. Wire rewards/gold to class activity completion.
5. Add minimal Academy Hub/Course Catalog UI.
6. Add Campus Shop data and semester stock.
7. Add placeholder Year 2-4 progression and Graduation Capstone gate.

## Non-Goals For First Pass

- Full Year 2-4 content.
- Perfect balance.
- Final grade math.
- Full PvP reward allowance implementation.
- Formal major/minor system.
- Profession system.
- Complex social/event web.
