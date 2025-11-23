# Project Summoner — Battlefield Spec

**Status:** IMPLEMENTED (MVP)
**Version:** 1.0
**Last Updated:** 2025-01-10
**Source:** Extracted from PROJECT_DOC.md

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

## 3 Base & Hero

| Aspect | Rule |
| ----- | ----- |
| **Base Object** | Fixed, physical structure at the rear of each territory. Possesses HP only. |
| **Victory Condition** | Destroying an opponent's base immediately ends the match. |
| **Hero Concept** | The summoner is *implied* to reside inside the base; not a controllable unit. |
| **Visual Representation** | Optional—may appear as energy core, tower, or similar focus. |
| **Future Hooks** | Elemental upgrades, add-on towers, or hero ultimates can be layered later. |

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
| **Mana System** | Pay full cost on cast; mana regenerates over time via base/hero stats. |
| **Vision Requirement** | Both **summons** and **spells** require vision at target location. |
| **Spawn Feedback** | Units appear with brief materialization FX for clarity. |

---

## 6 Combat and Interaction Assumptions

*(Defined here only as battlefield-related rules; full combat logic lives in the [Combat System Spec](combat-system.md).)*

* Units automatically seek and attack nearest visible enemy.
* Movement occurs freely in X and Y within bounds (no lanes).
* Collisions use soft separation to maintain readable spacing.
* Destroyed units fade out cleanly to preserve clarity in crowded fights.

---

## 7 Pacing and Readability

* Target match length: **3 – 5 minutes.**
* Midline visibility ensures immediate engagement opportunities.
* Fog of war and mana gating sustain tactical rhythm throughout the duel.
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
* Summon, spell, and vision systems respect placement rules.
* Base HP determines win/loss.
* 3–5 minute loop playable end-to-end with clear camera framing.

---

*Related Documents:*
- [Combat System](combat-system.md)
- [Card System](card-system.md)
- [Coordinate System](coordinate-system.md)
