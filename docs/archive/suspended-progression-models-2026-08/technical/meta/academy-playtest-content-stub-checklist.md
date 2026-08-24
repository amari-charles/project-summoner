# Academy Playtest Content Stub Checklist

**Status:** PASS 3 COMPLETE, PR REVIEW READY
**Initiative:** `academy-playtest-content`
**Domain:** `meta`
**Last Updated:** `2026-06-09`
**Companion Plan:** `academy-playtest-content-plan.md`
**Companion Validation:** `academy-playtest-content-validation-cases.md`

## Scope

This checklist tracks the approved first Academy playtest batch: loaner player deck support, Magic 101 placeholder content, activity-owned rewards, and the battle-side config migration needed to keep Academy authoring decoupled from battle runtime.

## Checklist

| Item ID | Item | Status | Notes |
|---|---|---|---|
| S01 | Add typed Academy battle-config field for loaner player decks | Complete | Added `AcademyBattleConfig.LoanerPlayerDeck`. |
| S02 | Serialize Academy loaner deck through the generic battle config boundary | Complete | Emits `player_side.deck.cards` only when a loaner deck is authored. |
| S03 | Preserve default profile deck behavior when no loaner deck is authored | Complete | Serializer omits `player_side`, so the battle side resolver loads the profile deck. |
| S04 | Avoid battle runtime dependency on Academy concepts | Complete | Battle session resolves generic `player_side` and `enemy_side` definitions. |
| S05 | Add focused boundary tests | Complete | Added service-level serialization tests for authored and omitted loaner decks. |
| S06 | Remove debug-named runtime deck override from Academy path | Complete | Academy now emits the production-facing side config shape. |
| S07 | Add reusable Magic 101 placeholder cards | Complete | Added `Neutral Starter Unit`, `Magic Bolt`, `Training Target`, and `Weak Enemy Unit`. |
| S08 | Add reusable Magic 101 placeholder units | Complete | Added neutral starter, passive target, and low-pressure enemy unit definitions. |
| S09 | Validate placeholder content shape with tests | Complete | Added card catalog and unit definition tests for the new placeholders. |
| S10 | Add activity-owned Academy rewards | Complete | Activities can now preview and grant rewards without UI-owned reward logic. |
| S11 | Wire Magic 101 to playable activities | Complete | Replaced literal lesson scaffold with four battle activities using loaner decks. |
| S12 | Validate Magic 101 reward cadence | Complete | Tests cover starter-unit and Magic Bolt activity grants plus duplicate repeat safety. |
| S13 | Persist activity reward claims | Complete | Academy progress stores claimed activity reward keys and preserves them through DTO conversion. |
| S14 | Mark claimed activity rewards in previews | Complete | Claimed activity rewards return `grant_state=claimed` and are not grantable in previews. |

## Review Notes

1. This step intentionally keeps the reusable side config contract in the battle/session layer.
2. Academy code now authors intent with a course-facing name.
3. Remaining balance work should happen in content data, not by adding Academy-specific battle branches.
