# Fateforged — Battlefield Spec

**Status:** IMPLEMENTED (MVP)
**Version:** 1.0
**Last Updated:** 2025-12-16

**Scope:** Defines the MVP battlefield structure, visibility rules, and summoning constraints for all real-time matches.

---

## 1 Overview

The battlefield is a **continuous 2D horizontal arena** representing the dueling ground between two summoners.
It is intentionally simple for the first playable build—flat terrain, one base per side, and no environmental modifiers—while supporting future expansion (terrain, multi-lane maps, PvE zones).

---

## 2 Core Layout

| Element | Description |
| ----- | ----- |
| **Dimensions** | Field width ≈ 1.5 – 2 × screen width. Height covers full vertical play band. |
| **Camera** | Player-controlled panning (drag or edge scroll). Optional: click unit to follow. |
| **Territory** | Each player controls one half of the field. The neutral midline is visible from match start. |
| **Ground Type** | Flat; no terrain bonuses or obstacles in MVP. |
| **Boundaries** | Units remain within rectangular limits; flyers may later ignore boundaries. |

---

## 3 Summoner on the Battlefield

| Aspect | Rule |
| ----- | ----- |
| **Summoner** | The summoner is physically present on the battlefield, fighting alongside their army. Possesses HP. |
| **Victory Condition** | Defeating an opponent's summoner immediately ends the match. |
| **Stakes** | The summoner IS on the field — not commanding from afar, but personally invested in the battle. |
| **Visual Representation** | The summoner character, styled by their elemental affinity. |
| **Future Hooks** | Elemental variations, visual upgrades, or summoner ultimates can be layered later. |

---

## 4 Fog of War & Vision

| Aspect | Rule |
| ----- | ----- |
| **Model** | Per-unit vision radius; team vision is the union of all allied sight areas. |
| **Initial Vision** | Player sees their entire half + neutral midline at match start; enemy half begins under fog. |
| **Fog Behavior** | Areas fade back to fog once no allied unit has sight. |
| **Purpose** | Enables scouting, stealth, and flanking tactics while keeping early engagements readable. |
| **Rendering Guideline** | Start with binary dark/visible mask; smooth gradients optional later. |

---

## 5 Summoning & Spell Placement

| Mechanic | Rule |
| ----- | ----- |
| **Placement Zone** | Player may summon only within their own half of the field and only at *visible* positions. |
| **Precision** | Tap or click exact point → unit spawns at nearest open space if blocked. |
| **Card Usage** | Cards are single-use per match. |
| **Cooldowns** | None; mana is the sole gating resource. |
| **Mana System** | Fixed mana pool (100 by default); no regeneration during battle. |
| **Vision Requirement** | Both **summons** and **spells** require vision at target location. |
| **Spawn Feedback** | Units appear with brief materialization FX for clarity. |

### Spawn Zone Restrictions

Units can only be spawned on the player's own half of the battlefield:

| Team | Valid Spawn Zone | Boundary Behavior |
| ----- | ----- | ----- |
| **Player** | X ≤ 0 (left half) | Attempts to spawn at X > 0 snap to X = 0 |
| **Enemy** | X > 0 (right half) | Attempts to spawn at X ≤ 0 snap to X = 0 |

**Visual Feedback:**
- While dragging a summon card, a red overlay appears on the enemy's half indicating invalid spawn territory
- The spawn preview circle shows blue when cursor is over valid territory, red when over invalid territory
- When cursor is over invalid territory, the preview is positioned at the boundary where the unit will actually spawn
- Spells are unaffected and can target anywhere on the battlefield

**Implementation:** See `BattlefieldConstants.clamp_spawn_position_for_team()` for the clamping logic.

---

## 6 Combat and Interaction Assumptions

*(Defined here only as battlefield-related rules; full combat logic lives in the [Combat System Spec](../combat/system.md).)*

* Units automatically seek and attack nearest visible enemy.
* Movement occurs freely in X and Y within bounds (no lanes).
* Collisions use soft separation to maintain readable spacing.
* Destroyed units fade out cleanly to preserve clarity in crowded fights.

---

## 7 Battle Phases and Pacing

Battles use a two-phase system:

1. **PREPARATION (30 seconds):** Build army formations with full mana pool. Units spawn but remain inactive.
2. **BATTLE (until victory):** All units activate and fight. Reinforcements can still be summoned.

**Pacing Notes:**
* Target match length: **3 – 5 minutes.**
* Preparation phase creates "two armies facing off" moment.
* Fixed mana pool forces strategic commitment rather than reactive play.
* Player-controlled camera panning allows tactical positioning and awareness across the battlefield.

---

## 8 Out-of-Scope (MVP Exclusions)

* Terrain modifiers or obstacles.
* Multiple bases or secondary objectives.
* Weather, elevation, or environmental buffs/debuffs.
* Player-controlled hero units.
* Dynamic lighting or cinematic zooms.

---

## 9 Future Considerations

| Feature | Purpose |
| ----- | ----- |
| **Terrain Zones** | Add movement or elemental effects for strategic variety. |
| **Multi-Base Maps** | Enable longer or multi-phase matches. |
| **Hero Manifestations** | Visualize hero during ultimates or late-game awakenings. |
| **Advanced Vision Types** | Scouting units, stealth fields, shared team vision in co-op modes. |

---

## Definition of Done (MVP Battlefield)

* Continuous horizontal arena implemented with fog-of-war masking.
* Two-phase battle system (PREPARATION → BATTLE) functional.
* Summon, spell, and vision systems respect placement rules.
* Summoner HP determines win/loss.
* 3–5 minute loop playable end-to-end with clear camera framing.

---

*Related Documents:*
- [Combat System](../combat/system.md)
- [Card System](../cards/system.md)
- [Coordinate System](../coordinates/system.md)
