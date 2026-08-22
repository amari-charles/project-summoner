# Unified Post-Battle Flow

Status: accepted product direction; canonical report migration is in progress.

## Problem

Battle completion currently has three competing presentation paths:

- `GameOverModal` gives immediate victory or defeat feedback inside every battle.
- `RewardScreen` handles campaign reward choices and may open the summoner level-up panel.
- `EncounterResults` handles encounter earnings and quest progress.

Other battle modes return directly or have mode-specific behavior. A campaign victory with no reward offer can skip the reward screen entirely. Card XP and card level changes therefore have no consistent post-battle presentation.

## Proposed ownership

Retain the in-battle conclusion overlay only as brief immediate feedback before
leaving the battlefield. It advances automatically into the post-battle flow so
the player does not acknowledge the same outcome twice.

Introduce a typed `PostBattleReport` assembled from authoritative results that were already committed by progression, reward, quest, and competitive services. The report should contain only presentation-ready facts:

- outcome and battle identity;
- summoner XP before/after and level changes;
- each participating card's XP before/after and level changes;
- automatic gold, item, and card grants;
- unresolved reward choices, when applicable;
- quest, curriculum, or rating progress;
- the scene to visit when the player continues.

One `PostBattleResults` screen renders the report as a single readable surface:

1. Battle outcome.
2. Summoner and card progression.
3. Acquired gold, items, and cards, including any required choice.
4. Contextual progress such as a quest step or competitive rating.
5. Continue to the report's destination.

The player does not click through those sections. They may reveal or animate in
sequence, but they remain on one screen. Cards that gained no XP are omitted;
the summoner progression row remains visible. Level-ups happen automatically
and receive stronger presentation rather than requiring confirmation.

The normal post-battle flow therefore has one deliberate advance: Continue from
the combined Results screen. The victory or defeat conclusion over the
battlefield is a short, non-interactive transition.

A required reward choice is the only additional interaction. Victory and defeat
use the same structure, with sections omitted when they have no relevant facts.

The screen must not infer rewards from battle mode or write unrelated progression state. The services that own those systems remain authoritative; the report builder only combines their committed results.

## Migration boundary

- Retain and rename the in-battle `GameOverModal` concept as a battle conclusion overlay.
- Replace `RewardScreen` and `EncounterResults` as independent navigation destinations.
- Build reusable summoner/card level reveal components; summoner leveling itself is automatic and never requires confirmation.
- Route campaign and encounter battles through the unified screen first, then add competitive and repeatable modes as their report fields are defined.

## Current implementation boundary

Campaign and encounter battles now route to the shared Results prototype. The
prototype reads committed XP and reward grants and can submit a pending campaign
reward choice. A fully typed `PostBattleReport`, before/after snapshots needed
for exact level-crossing animation, contextual quest/rating rows, and deletion
of the legacy screens remain migration work.
