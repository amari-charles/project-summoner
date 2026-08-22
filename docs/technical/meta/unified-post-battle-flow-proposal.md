# Unified Post-Battle Flow Proposal

Status: proposed; the presentation and leveling decisions below are not yet accepted product direction.

## Problem

Battle completion currently has three competing presentation paths:

- `GameOverModal` gives immediate victory or defeat feedback inside every battle.
- `RewardScreen` handles campaign reward choices and may open the summoner level-up panel.
- `EncounterResults` handles encounter earnings and quest progress.

Other battle modes return directly or have mode-specific behavior. A campaign victory with no reward offer can skip the reward screen entirely. Card XP and card level changes therefore have no consistent post-battle presentation.

## Proposed ownership

Retain the in-battle conclusion overlay only as immediate feedback and the pause before leaving the battlefield. Its Continue action should always hand meaningful battles to one post-battle flow.

Introduce a typed `PostBattleReport` assembled from authoritative results that were already committed by progression, reward, quest, and competitive services. The report should contain only presentation-ready facts:

- outcome and battle identity;
- summoner XP before/after and level changes;
- each participating card's XP before/after and level changes;
- automatic gold, item, and card grants;
- unresolved reward choices, when applicable;
- quest, curriculum, or rating progress;
- the scene to visit when the player continues.

One `PostBattleScreen` renders the report in a stable sequence:

1. Battle outcome.
2. Summoner and card progression.
3. Acquired gold, items, and cards, including any required choice.
4. Contextual progress such as a quest step or competitive rating.
5. Continue to the report's destination.

The screen must not infer rewards from battle mode or write unrelated progression state. The services that own those systems remain authoritative; the report builder only combines their committed results.

## Migration boundary

- Retain and rename the in-battle `GameOverModal` concept as a battle conclusion overlay.
- Replace `RewardScreen` and `EncounterResults` as independent navigation destinations.
- Build reusable summoner/card level reveal components; summoner leveling itself is automatic and never requires confirmation.
- Route campaign and encounter battles through the unified screen first, then add competitive and repeatable modes as their report fields are defined.

## Decision required

The existing level-up panels require the player to confirm spending available XP. A unified result screen needs one explicit rule: either levels apply automatically when XP crosses a threshold and the screen announces them, or the screen pauses for a player-confirmed level-up action. This changes progression behavior and should be decided before implementation.
