# Project Summoner — Hero System Spec

**Status:** PLANNED (Not yet implemented)
**Last Updated:** 2025-01-10
**Source:** Extracted from PROJECT_DOC.md

**Purpose:** Define the foundational structure for Heroes — the player-controlled summoners who shape battle strategy, resource flow, and collection bias.

---

## 1️⃣ Hero Overview

Heroes serve as the player's identity and primary strategic modifier. Each hero brings unique starting traits, affinities, and potential growth paths.

**Core Principles:**

* **Asymmetry from the start:** Each player begins with a randomly assigned *Fated Hero*, ensuring no two journeys start identically.
* **Identity through play:** Heroes define the player's mana generation style, favored card elements, and potential signature ability.
* **Collection over time:** Heroes are collectible entities separate from cards. Players can unlock new heroes through play or rare rewards.

---

## 2️⃣ Hero Attributes (Baseline Structure)

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

## 3️⃣ Affinity System

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

## 4️⃣ Hero Progression

* Heroes gain **experience** from matches.
* Leveling costs **Essence** (see Economy System).
* Leveling increases base stats and may enhance the signature ability or unlock a new passive.
* Progression pacing should encourage attachment without grind.

---

## 5️⃣ Hero Unlocks

* The first hero (Fated Hero) is randomized at account creation.
* Additional heroes can be earned through milestones, events, or rare card conversions.
* No monetized gacha — unlocks are achievement- or event-based.

---

## 6️⃣ Integration Hooks

| System | Interaction |
| :---- | :---- |
| **Card System** | Hero affinity modifies card stat rolls and element distribution. |
| **Economy System** | Essence is spent to level heroes; Glory may influence unlock milestones. |
| **Battlefield/Combat** | Heroes determine mana regen rate and, eventually, ultimate availability. |

---

## 7️⃣ Player Experience Goals

* Immediate identity and replay variety.
* Visible hero growth and affinity expression.
* Long-term mastery path that complements, not overshadows, card collection.

---

## 🔮 Future Considerations

* **Signature Ability Design:** define per-hero active/passive system and activation rules.
* **Synergy Scaling:** decide whether affinity synergy should modify battle stats dynamically or only through deck generation.
* **Hero Customization:** cosmetic skins, minor perk trees, or artifact slots.
* **Dual-Affinity Heroes:** potential late-game feature (e.g., Fire \+ Void).

---

*Related Documents:*
- [Card System](card-system.md)
- [Design Vision](../design/vision.md)
- [Roadmap](../design/roadmap.md)
