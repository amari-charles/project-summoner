# Legacy System Cleanup Validation

Date: 2026-08-23

## Scope and replacements

| Removed legacy owner | Canonical replacement | Reachability result |
|---|---|---|
| Static Academy hub, campaign map, course-node panels, and first-card selection | Walkable Academy campus, professor dialogue, generic quests/Journal, generic encounter preparation | Opening flow already routes Summoner selection → reveal → campus; no accepted onboarding caller requires first-card selection |
| Caravan screen, events, campaign nodes, shop catalog, narrative, and localization | Campus Shop plus ordinary Academy/world interaction and shop services | No runtime route, event, catalog, or repository API remains |
| `RewardScreen` and Academy/encounter-specific result destination | Typed `PostBattleReport` rendered by `PostBattleResults` | Battle conclusion remains automatic; campaign and encounter outcomes share one Results destination |
| Caller-authored battle `scene_path` selection | `BattleRuntimeSurface` plus application `BattleSurfaceRouter` | Academy/quest and debug callers resolve through one typed policy |
| Blanket account-wide normal items | Summoner-owned normal items plus explicitly shared event-exclusive items | Grants, queries, equipment, rewards, persistence, and developer tools use the same item-domain rules |

Named campus landmarks such as Class Hall remain where they still serve a
physical-world role. Their obsolete course-screen ownership was removed.

## Compatibility retained intentionally

- The serialized JSON key `caravan_purchases` is retained as
  `LegacyCaravanPurchaseIds` so old profiles round-trip without silent data
  loss. It has no repository service API or runtime reader and cannot control
  shops, routes, events, or progression.
- Profile schema v7 recovers ownership of a legacy normal item only when its
  existing equipped-Summoner provenance identifies the owner. Ambiguous normal
  instances are preserved unassigned and inaccessible instead of being granted
  to an arbitrary Summoner.
- Explicit account-wide binding remains supported only for definitions authored
  as event-exclusive and shared.
- `BattleContext` remains as the accepted session compatibility boundary; this
  pass changed its destination vocabulary but did not attempt the excluded broad
  authority rewrite.

## Regression coverage

- Item-domain tests cover normal grant context, per-Summoner inventory filtering,
  cross-Summoner equip rejection, explicitly shared event items, reward target
  validation, migration provenance, and ambiguous-data preservation.
- Item adapter coverage verifies every retained developer operation is exposed:
  grant, shared grant, grant all, list, equip, unequip, and clear.
- Route tests cover standard and debug-arena surface selection and reject raw
  custom scene policy.
- Campus/quest tests load the walkable campus, professor interaction, Journal
  sections, quest offer and tracking surfaces, encounter preparation, shared
  Spellbook/deck editing, Online, Settings, pause, battlefield conclusion, and
  combined Results.
- Results tests cover victory/defeat normalization, Summoner/Card XP grants,
  card rewards, no-reward results, required choices, selected grants, and
  presentation-only ownership.
- Persistence tests cover profile v7 item migration and inert Caravan payload
  round-tripping.

## Validation commands

Final command output and exact counts are recorded in the pull-request report
after the complete C#, GUT, build/type, catalog, structural, and headless-loading
checks have run on the final commit.
