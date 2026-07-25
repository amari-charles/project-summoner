# Walkable Academy Hub

**Status:** Discontinued
**Type:** Rejected product direction / decision history
**Decision reversed:** 2026-07-24

---

## Decision

Fateforged will not pursue a walkable Academy hub. The prototype did not provide
enough value relative to its implementation, content, navigation, and future
maintenance costs.

The Academy will continue to use fast menu-based navigation. Development should
focus on the core forging loop: course enrollment, activities, assessments,
grades, permanent reward choices, transcript identity, and graduating a
differentiated summoner into battle.

## Current Product Direction

- Keep `scripts/meta/screens/academy_hub.gd` as the Academy's primary entry surface.
- Keep direct access to Class Hall, Campus Shop, Mission Hall, Dorms/Collection,
  Online, Settings, and the summoner screen.
- Improve the menu-based hub only when doing so supports clarity or completion of
  the Academy progression loop.
- Do not add player movement, door zones, walkable buildings, campus navigation,
  multiplayer presence, hub instancing, or hub-specific social interaction.
- Do not treat the abandoned walkable-hub prototype as a dependency for future
  Academy work.

## Prototype Disposition

The local prototype previously explored a bounded top-down campus with a movable
avatar and discrete building entrances. That implementation is not part of the
active product direction and should not be merged into the production line.

Any generally useful work developed alongside it should be separated and evaluated
on its own merits.

## Rationale

The walkable hub added a new interaction layer without advancing the game's
fundamental loop enough to justify its cost. Menu navigation is faster and keeps
development concentrated on the permanent choices and combat outcomes that define
Fateforged.
