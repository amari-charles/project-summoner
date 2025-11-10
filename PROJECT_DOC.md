# Project Summoner — Consolidated Design Document

**Status:** CURRENT (Contains systems not yet documented elsewhere)
**Last Updated:** 2025-01-10

This document contains design specifications for systems that are planned but not yet fully extracted into separate documentation:

- **Hero System**: Player-controlled summoners with affinities and progression
- **Battlefield Spec**: Arena layout, fog of war, and placement rules
- **Combat System**: Unit AI, targeting, damage resolution, and match flow

**Note:** The following sections have been extracted to separate documentation:
- Card System → See [docs/api/card-system.md](docs/api/card-system.md)
- Vision Document → See [docs/design/vision.md](docs/design/vision.md)
- Development Roadmap → See [docs/design/roadmap.md](docs/design/roadmap.md)

---

# Hero System Spec

**🧙 Project Summoner — Hero System Spec**

*Last updated: November 2025*

**Purpose:** Define the foundational structure for Heroes — the player-controlled summoners who shape battle strategy, resource flow, and collection bias.

---

## **1️⃣ Hero Overview**

Heroes serve as the player's identity and primary strategic modifier. Each hero brings unique starting traits, affinities, and potential growth paths.

**Core Principles:**

* **Asymmetry from the start:** Each player begins with a randomly assigned *Fated Hero*, ensuring no two journeys start identically.
* **Identity through play:** Heroes define the player's mana generation style, favored card elements, and potential signature ability.
* **Collection over time:** Heroes are collectible entities separate from cards. Players can unlock new heroes through play or rare rewards.

---

## **2️⃣ Hero Attributes (Baseline Structure)**

Each hero has a minimal shared schema:

| Field | Description |
| ----- | ----- |
| `id` | Unique hero identifier |
| `name` | Display name |
| `affinity` | Elemental focus that biases deck generation and stat modifiers |
| `rarity` | Determines hero growth potential, not immediate strength |
| `base_stats` | Core properties (HP, mana\_regen, summon\_speed, ability\_cooldown) |
| `signature_ability` | Optional active or passive trait that defines their unique edge |
| `deck_bias` | Elemental or mechanical weighting for random card rewards |

---

## **3️⃣ Affinity System**

Affinities define an elemental identity that connects heroes and cards.

**Primary Affinities:** Fire · Water · Nature · Storm · Earth · Void

Each affects gameplay flavor and potential bonuses, e.g.:

* *Fire* → offensive tempo
* *Water* → control & sustain
* *Nature* → resilience & regeneration
* *Storm* → burst & unpredictability
* *Earth* → defense & structure
* *Void* → hybrid or wildcard traits

Affinities influence deck bias and card stat variance, reinforcing the player's identity.

---

## **4️⃣ Hero Progression**

* Heroes gain **experience** from matches.
* Leveling costs **Essence** (see Economy System).
* Leveling increases base stats and may enhance the signature ability or unlock a new passive.
* Progression pacing should encourage attachment without grind.

---

## **5️⃣ Hero Unlocks**

* The first hero (Fated Hero) is randomized at account creation.
* Additional heroes can be earned through milestones, events, or rare card conversions.
* No monetized gacha — unlocks are achievement- or event-based.

---

## **6️⃣ Integration Hooks**

| System | Interaction |
| :---- | :---- |
| **Card System** | Hero affinity modifies card stat rolls and element distribution. |
| **Economy System** | Essence is spent to level heroes; Glory may influence unlock milestones. |
| **Battlefield/Combat** | Heroes determine mana regen rate and, eventually, ultimate availability. |

---

## **7️⃣ Player Experience Goals**

* Immediate identity and replay variety.
* Visible hero growth and affinity expression.
* Long-term mastery path that complements, not overshadows, card collection.

---

## **🔮 Future Considerations**

* **Signature Ability Design:** define per-hero active/passive system and activation rules.
* **Synergy Scaling:** decide whether affinity synergy should modify battle stats dynamically or only through deck generation.
* **Hero Customization:** cosmetic skins, minor perk trees, or artifact slots.
* **Dual-Affinity Heroes:** potential late-game feature (e.g., Fire \+ Void).

---

# Battlefield Spec

# **Project Summoner — Battlefield Spec**

**Version 1.0** | *Last updated Nov 2025*
 **Scope:** Defines the MVP battlefield structure, visibility rules, and summoning constraints for all real-time matches.

---

## **1 Overview**

The battlefield is a **continuous 2D horizontal arena** representing the dueling ground between two summoners.
 It is intentionally simple for the first playable build—flat terrain, one base per side, and no environmental modifiers—while supporting future expansion (terrain, multi-lane maps, PvE zones).

---

## **2 Core Layout**

| Element | Description |
| ----- | ----- |
| **Dimensions** | Field width ≈ 1.5 – 2 × screen width. Height covers full vertical play band. |
| **Camera** | Player-controlled panning (drag or edge scroll). Optional: click unit to follow. |
| **Territory** | Each player controls one half of the field. The neutral midline is visible from match start. |
| **Ground Type** | Flat; no terrain bonuses or obstacles in MVP. |
| **Boundaries** | Units remain within rectangular limits; flyers may later ignore boundaries. |

---

## **3 Base & Hero**

| Aspect | Rule |
| ----- | ----- |
| **Base Object** | Fixed, physical structure at the rear of each territory. Possesses HP only. |
| **Victory Condition** | Destroying an opponent's base immediately ends the match. |
| **Hero Concept** | The summoner is *implied* to reside inside the base; not a controllable unit. |
| **Visual Representation** | Optional—may appear as energy core, tower, or similar focus. |
| **Future Hooks** | Elemental upgrades, add-on towers, or hero ultimates can be layered later. |

---

## **4 Fog of War & Vision**

| Aspect | Rule |
| ----- | ----- |
| **Model** | Per-unit vision radius; team vision is the union of all allied sight areas. |
| **Initial Vision** | Player sees their entire half \+ neutral midline at match start; enemy half begins under fog. |
| **Fog Behavior** | Areas fade back to fog once no allied unit has sight. |
| **Purpose** | Enables scouting, stealth, and flanking tactics while keeping early engagements readable. |
| **Rendering Guideline** | Start with binary dark/visible mask; smooth gradients optional later. |

---

## **5 Summoning & Spell Placement**

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

## **6 Combat and Interaction Assumptions**

*(Defined here only as battlefield-related rules; full combat logic lives in the Combat Spec.)*

* Units automatically seek and attack nearest visible enemy.

* Movement occurs freely in X and Y within bounds (no lanes).

* Collisions use soft separation to maintain readable spacing.

* Destroyed units fade out cleanly to preserve clarity in crowded fights.

---

## **7 Pacing and Readability**

* Target match length: **3 – 5 minutes.**

* Midline visibility ensures immediate engagement opportunities.

* Fog of war and mana gating sustain tactical rhythm throughout the duel.

* Player-controlled camera panning allows tactical positioning and awareness across the battlefield.

---

## **8 Out-of-Scope (MVP Exclusions)**

* Terrain modifiers or obstacles.

* Multiple bases or secondary objectives.

* Weather, elevation, or environmental buffs/debuffs.

* Player-controlled hero units.

* Dynamic lighting or cinematic zooms.

---

## **9 Future Considerations**

| Feature | Purpose |
| ----- | ----- |
| **Terrain Zones** | Add movement or elemental effects for strategic variety. |
| **Multi-Base Maps** | Enable longer or multi-phase matches. |
| **Hero Manifestations** | Visualize hero during ultimates or late-game awakenings. |
| **Advanced Vision Types** | Scouting units, stealth fields, shared team vision in co-op modes. |

---

**Definition of Done (MVP Battlefield)**

* Continuous horizontal arena implemented with fog-of-war masking.

* Summon, spell, and vision systems respect placement rules.

* Base HP determines win/loss.

* 3–5 minute loop playable end-to-end with clear camera framing.

---

# Combat System Spec

# **Project Summoner — Combat System Spec (v1.1)**

*Last updated Nov 2025*
 **Scope:** Defines unit simulation, targeting, movement, damage, and objective behavior for the MVP offline prototype.

---

## **1 Simulation Loop**

Fixed-timestep tick (≈60 FPS).
 Order each frame:

1. Resolve player input \+ summons

2. Mana regen \+ match timer

3. Units → Sense → Decide → Move → Act

4. Projectiles / Spells update

5. Damage queue resolve → Deaths handled

6. FX \+ Events

7. Win check (base HP ≤ 0\)

---

## **2 Unit Model**

Shared fields: `team`, `hp`, `move_speed`, `attack_damage`, `attack_range`, `attack_rate`, `attack_windup`, `aggro_radius`, `is_ranged`, `is_flying`, `tags`.
 **States:** `IDLE`, `CHASE`, `ATTACK`, `HOLD`, `DEAD`.

---

## **3 Sensing**

* Acquire nearest visible enemy within `aggro_radius`.

* Must be inside team vision (fog aware).

* If none found → no target.

---

## **4 Decision Logic (Always-Advance Baseline)**

| Condition | State / Behavior |
| ----- | ----- |
| Enemy in attack\_range (+LOS) | ATTACK (wind-up → resolve → cooldown) |
| Enemy seen but out of range | CHASE (move toward until in range) |
| No enemy in aggro | **ADVANCE toward enemy base** (attack-move) |
| Base in range \+ no enemy within intercept radius (\~200 px) | ATTACK BASE |

**Rule of thumb:** Units always press forward unless actively attacking.
 Keeps tempo and ensures bases die when front is won.

---

## **5 Movement**

* **Seek:** vector toward target or base.

* **Separation:** repulse from near allies (\<48 px).

* **Clamp:** stay within battlefield bounds.

* **Flying flag:** ignores separation.

---

## **6 Attacks**

* Wind-up → Hit → Cooldown.

* Melee \= instant damage at resolve.

* Ranged \= spawn projectile (120–180 px/s MVP).

* Attack rate ≈ 1 / `attack_rate`.

* Retarget allowed each tick.

---

## **7 Projectiles**

* Constant speed, light homing each tick.

* Hit when within small radius (≈8 px) → enqueue damage.

* Friendly-fire off.

* Despawn on miss timeout (2 s MVP).

---

## **8 Damage & Death**

* Resolve damage queue post-actions.

* `hp -= amount`; if ≤0 → DEAD.

* Quick fade (≤0.4 s).

* On-death effects disabled for MVP.

---

## **9 Base Objective Behavior**

* Each base is a static unit with `is_base = true`.

* Attacked like any unit; destroyed \= instant victory for opponent.

* Units that win their fight resume advancing → attack base automatically.

---

## **10 Match Flow Integration**

* Time limit ≈ 5 min.

* **Overtime (≥ 4 min):** \+50 % mana regen \+ base takes \+33 % damage for closure.

* **Tiebreak:** higher base HP → win → then total base damage → draw.

---

## **11 Card Exhaustion**

* Cards are single-use.

* If player has 0 cards \+ no units alive → **Exhausted State**.

  * Gains no new vision; enemy gains full vision.

  * Can still win if remaining forces finish base.

* No auto-win trigger for opponent.

---

## **12 Offline AI Sandbox**

* Identical mana rules.

* Spends mana when ≥ threshold.

* Picks card type weights (frontline \> ranged \> spell).

* Places units randomly within front third of own half.

* Difficulty knobs: mana bonus %, play interval jitter.

---

## **13 Performance Targets**

* ≤ 100 active units on screen.

* \< 5 ms simulation per frame on mid-range PC.

* Combat stable in soak tests (20 v 20 for 2 min @ 60 FPS).

---

## **14 Future Behavior Extensions**

| Feature | Description | Purpose |
| ----- | ----- | ----- |
| **Hold/Guard Orders** | Unit stops at summon point until enemy in range. | Supports ranged formations or towers. |
| **Retreat/Regroup AI** | Pull back when isolated or low HP. | Prevent suicidal pushes. |
| **Unit Roles** | `advance_on_clear`, `defensive_anchor`, `ambusher`. | Adds personality to archetypes. |
| **Path Weights** | Light navmesh with preferred lanes or flank routes. | Enables terrain depth later. |

---

## **15 Definition of Done (MVP Combat)**

* Units spawn and auto-advance toward enemy base.

* Fights resolve and push front lines naturally.

* Base destruction ends match.

* Offline AI completes loops reliably.

* Overtime ensures no stalemates.
