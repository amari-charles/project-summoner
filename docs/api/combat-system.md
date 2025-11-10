# Project Summoner — Combat System Spec (v1.1)

**Status:** IMPLEMENTED (MVP)
**Last Updated:** 2025-01-10
**Source:** Extracted from PROJECT_DOC.md

**Scope:** Defines unit simulation, targeting, movement, damage, and objective behavior for the MVP offline prototype.

---

## 1 Simulation Loop

Fixed-timestep tick (≈60 FPS).
Order each frame:

1. Resolve player input + summons
2. Mana regen + match timer
3. Units → Sense → Decide → Move → Act
4. Projectiles / Spells update
5. Damage queue resolve → Deaths handled
6. FX + Events
7. Win check (base HP ≤ 0)

---

## 2 Unit Model

Shared fields: `team`, `hp`, `move_speed`, `attack_damage`, `attack_range`, `attack_rate`, `attack_windup`, `aggro_radius`, `is_ranged`, `is_flying`, `tags`.

**States:** `IDLE`, `CHASE`, `ATTACK`, `HOLD`, `DEAD`.

---

## 3 Sensing

* Acquire nearest visible enemy within `aggro_radius`.
* Must be inside team vision (fog aware).
* If none found → no target.

---

## 4 Decision Logic (Always-Advance Baseline)

| Condition | State / Behavior |
| ----- | ----- |
| Enemy in attack\_range (+LOS) | ATTACK (wind-up → resolve → cooldown) |
| Enemy seen but out of range | CHASE (move toward until in range) |
| No enemy in aggro | **ADVANCE toward enemy base** (attack-move) |
| Base in range + no enemy within intercept radius (~200 px) | ATTACK BASE |

**Rule of thumb:** Units always press forward unless actively attacking.
Keeps tempo and ensures bases die when front is won.

---

## 5 Movement

* **Seek:** vector toward target or base.
* **Separation:** repulse from near allies (<48 px).
* **Clamp:** stay within battlefield bounds.
* **Flying flag:** ignores separation.

---

## 6 Attacks

* Wind-up → Hit → Cooldown.
* Melee = instant damage at resolve.
* Ranged = spawn projectile (120–180 px/s MVP).
* Attack rate ≈ 1 / `attack_rate`.
* Retarget allowed each tick.

---

## 7 Projectiles

* Constant speed, light homing each tick.
* Hit when within small radius (≈8 px) → enqueue damage.
* Friendly-fire off.
* Despawn on miss timeout (2 s MVP).

---

## 8 Damage & Death

* Resolve damage queue post-actions.
* `hp -= amount`; if ≤0 → DEAD.
* Quick fade (≤0.4 s).
* On-death effects disabled for MVP.

---

## 9 Base Objective Behavior

* Each base is a static unit with `is_base = true`.
* Attacked like any unit; destroyed = instant victory for opponent.
* Units that win their fight resume advancing → attack base automatically.

---

## 10 Match Flow Integration

* Time limit ≈ 5 min.
* **Overtime (≥ 4 min):** +50 % mana regen + base takes +33 % damage for closure.
* **Tiebreak:** higher base HP → win → then total base damage → draw.

---

## 11 Card Exhaustion

* Cards are single-use.
* If player has 0 cards + no units alive → **Exhausted State**.
  * Gains no new vision; enemy gains full vision.
  * Can still win if remaining forces finish base.
* No auto-win trigger for opponent.

---

## 12 Offline AI Sandbox

* Identical mana rules.
* Spends mana when ≥ threshold.
* Picks card type weights (frontline > ranged > spell).
* Places units randomly within front third of own half.
* Difficulty knobs: mana bonus %, play interval jitter.

---

## 13 Performance Targets

* ≤ 100 active units on screen.
* < 5 ms simulation per frame on mid-range PC.
* Combat stable in soak tests (20 v 20 for 2 min @ 60 FPS).

---

## 14 Future Behavior Extensions

| Feature | Description | Purpose |
| ----- | ----- | ----- |
| **Hold/Guard Orders** | Unit stops at summon point until enemy in range. | Supports ranged formations or towers. |
| **Retreat/Regroup AI** | Pull back when isolated or low HP. | Prevent suicidal pushes. |
| **Unit Roles** | `advance_on_clear`, `defensive_anchor`, `ambusher`. | Adds personality to archetypes. |
| **Path Weights** | Light navmesh with preferred lanes or flank routes. | Enables terrain depth later. |

---

## 15 Definition of Done (MVP Combat)

* Units spawn and auto-advance toward enemy base.
* Fights resolve and push front lines naturally.
* Base destruction ends match.
* Offline AI completes loops reliably.
* Overtime ensures no stalemates.

---

*Related Documents:*
- [Card System](card-system.md)
- [Hero System](hero-system.md)
- [Coordinate System](coordinate-system.md)
