# Academy Encounter Balance Roadmap

**Status:** Draft
**Date:** 2026-05-03
**Owner:** Combat Design / Academy Campaign

## Goal

Academy combat should teach first, then differentiate. The first course battle should be nearly unlosable, early assessments should be forgiving, and later courses should gradually make grades, Honors, and reward quality reflect player mastery.

## Balance Philosophy

1. **First practice battle: learn the controls**
   - Passive or nearly passive opponent.
   - One weak enemy card.
   - Very low enemy HP.
   - Player learns to play a card, watch units move, and win.

2. **First assessment: still forgiving**
   - Slow simple AI.
   - One enemy pressure at a time.
   - Losing should be unlikely unless the player does nothing.

3. **Semester 2: first real step up**
   - Two-card enemy decks are acceptable.
   - AI can play slowly but actively.
   - Assessment performance can start mattering for grades or Honors later.

4. **Later years: transcript differentiation**
   - Difficulty should come from course identity, special rules, objectives, and better enemy plans.
   - Losses should usually reduce upside rather than brick the summoner.
   - Honors should require understanding the class, not merely surviving overtuned fights.

## Encounter Tuning Bands

| Band | Player State | Enemy Deck | Enemy AI | Enemy HP | Design Promise |
|---|---|---|---|---|---|
| Onboarding Practice | first class | 1 weak card | Passive | Very low | Cannot realistically lose |
| Onboarding Assessment | first semester | 1 weak card | Slow simple | Low | First official check, still safe |
| Early Semester | 3-5 cards | 2 simple cards | Slow simple | Low/moderate | Choices start to matter |
| Standard Course | established deck | focused deck | Simple/heuristic | Moderate | Course mechanics matter |
| Honors/Advanced | prepared deck | focused or special-rule deck | Stronger AI | Moderate/high | Optional excellence check |
| Capstone | full summoner | signature deck | Scripted/heuristic hybrid | High | Graduation test |

## Current Implementation Notes

- Academy course activities can carry their own battle tuning.
- Year 1 Semester 1 practice battles use passive AI and very low HP.
- Year 1 Semester 1 assessments use slow simple AI.
- Year 1 Semester 2 steps up to two-card enemy decks with slow simple AI.
- Repeatable practice should not grant repeatable gold or permanent power.

## Next Balance Work

- Author per-course encounter identities instead of using generic default fights.
- Add grade and Honors objectives after the basic battle flow is stable.
- Add a simulation or smoke-test harness for first-course win rates.
- Tune rewards only after the activity difficulty curve feels coherent.
