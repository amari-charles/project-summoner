# Card System

# ***🪄 Project Summoner — Card System (Living Spec)***

 *Last updated: November 2025*

***Scope:** Defines how **cards** work end-to-end: data model, variance (stat, effect, rarity, visuals), generation rules, and lifecycle (keep / dismantle / transmute).*  
*It’s intentionally modular so we can iterate without rewriting core systems.*

---

## ***1️⃣ Card Taxonomy***

*Cards are the **only** way to act in battle.*  
*All cards are **single-use per match**.*

### ***Types***

* ***Unit** — summons one entity or formation (squad).*  
* ***Spell** — instant or timed effect.*  
* ***Structure** — stationary summon with HP, aura, or attack.*  
* ***Tactic** (optional future) — modifies deck or hero for that match.*

### ***Tags (multi-select)***

*Drive synergy and affinity bias.*

* *Element: `fire | water | nature | storm | earth | neutral`*  
* *Role: `tank | assault | support | ranged | air | structure | spell`*  
* *Family: e.g. `pyre`, `thorn`, `wisp`*  
* *Mechanics: `burn | freeze | heal | shield | root | silence | dash | stealth | summon_on_death | lifesteal | taunt`*

---

## ***2️⃣ Core Balance Fields***

*Each card has a baseline before variance and modifiers.*

***Shared fields***

* *`mana_cost` (1–10 typical)*  
* *`deployment_time_ms`*  
* *`rarity_base` (`common | rare | epic | legendary`)*  
* *`element`, `tags`, and derived `power_rating`*

***Per type***

***Unit***

* *HP, attack, attack\_rate, move\_speed, range*  
* *targets (`ground | air | both`), aggro radius*  
* *optional `on_death_effect`*

***Spell***

* *effect\_ref, radius, projectile\_speed, duration*

***Structure***

* *HP, armor, attack, attack\_rate, aura\_ref, duration*

---

## ***3️⃣ Variance System — Hybrid Rarity \+ Variant Framework***

### ***Philosophy***

***Variants define behavior. Rarity defines expression.***  
*Each card archetype has multiple variants that determine what it does, and each variant can exist at any rarity, which determines how far that behavior can be pushed.*

*This hybrid system preserves both **horizontal diversity** and **vertical mastery**, giving players a sense of discovery and progression.*

---

### ***3.1 Horizontal Variance — Functional Variants***

*Each card archetype can appear in multiple **variants**, each representing a different tactical function. Variants share the same fantasy but change playstyle.*

*Example (Fireball archetype):*

| *Variant* | *Description* | *Niche* |
| ----- | ----- | ----- |
| ***Focused Fireball*** | *Single, fast projectile* | *Precision burst* |
| ***Scatterburst*** | *Two splitting orbs* | *Area control* |
| ***Lingering Flame*** | *Leaves burning ground* | *Zone control* |
| ***Delayed Meteor*** | *Delayed multi-impact* | *Punish stationary foes* |

*These variants exist across all rarities — they are different cards, not tiers of one.*

---

### ***3.2 Vertical Variance — Rarity Expression***

*Each variant can appear at any rarity. Rarity does not unlock the variant but amplifies its expression.*

| *Rarity* | *What Changes* | *Feel* |
| :---- | :---- | :---- |
| ***Common*** | *Baseline stats, simple FX* | *Functional* |
| ***Rare*** | *Slightly refined mechanics or improved efficiency* | *Efficient* |
| ***Epic*** | *Variant reaches sharper extremes or gains subtle synergy* | *Refined* |
| ***Legendary*** | *Full expression of that variant’s fantasy; may include a unique flourish* | *Mastered* |

*This means you can have a **common Scatterburst Fireball** and a **legendary Scatterburst Fireball** — same play pattern, different intensity.*

---

### ***3.3 Example: Fireball Variant Grid***

| *Variant ↓ / Rarity →* | *Common* | *Rare* | *Epic* | *Legendary* |
| :---- | :---- | :---- | :---- | :---- |
| ***Focused Fireball*** | *baseline bolt* | *faster projectile* | *adds small splash* | *burst \+ minor stun* |
| ***Scatterburst*** | *twin short-range* | *wider spread* | *twin \+ small DoT* | *twin \+ flame trails* |
| ***Lingering Flame*** | *short zone* | *larger zone* | *longer duration* | *adds AoE slow* |
| ***Delayed Meteor*** | *single drop* | *shorter delay* | *adds shockwave* | *multi-meteor storm* |

*Every cell represents a valid card roll.*

---

### ***3.4 Supporting Variance Layers***

*Variants and rarity form the foundation, but each card also has **micro variance** layers for individuality:*

| *Layer* | *Description* | *Impact* |
| :---- | :---- | :---- |
| ***Stat Variance*** | *minor numeric drift around baseline values* | *feel difference* |
| ***Effect Variance*** | *small micro-modifiers (e.g., \+1 chain, short burn)* | *behavioral nuance* |
| ***Visual Variance*** | *tint, aura, particle tweak* | *cosmetic identity* |

*These stack with the variant/rarity system to create endless individuality without chaos.*

---

### ***3.5 Summary***

* *Variants \= What the card does (horizontal difference).*  
* *Rarity \= How far that variant can go (vertical mastery).*  
* *Micro-variance adds texture within that framework.*  
* *Players chase both **new expressions** (discovering variants) and **refinement** (upgrading their favorite ones).*  
* *The system supports fate, asymmetry, mastery, and individuality all at once.*

---

## ***4️⃣ Effects System (Compositional)***

*Effects are **data-driven payloads** attached to cards. These define primary and secondary behaviors, scaled by hero affinity and stats.*

---

## ***5️⃣ Generation Rules (Drops & Crafting)***

1. *Roll archetype → variant → rarity → stat/effect/visual variance.*  
2. *Player chooses to keep, dismantle, or transmute new cards.*  
3. *Higher rarities deepen existing play patterns rather than replace them.*

---

## ***6️⃣ Player Experience Goals***

* *Discover horizontal variants (new playstyles).*  
* *Master vertical rarity paths (stronger versions of favorite variants).*  
* *Every card feels handcrafted — no duplicates, no grind.*  
* *Players develop emotional attachment to their army through uniqueness and expression.*

# Vision Document

# **Project Summoner — Vision Document**

## **One-Sentence Elevator Pitch**

A real-time **1v1 summoning battler** where every card is single-use, every hero is unique, and every match feels like commanding your own army of magic.

---

## **Core Fantasy**

You are a **wizard-commander**, leading a living army of summons and spells. Each match is a duel of wits and will — limited resources, shifting odds, and one decisive ultimate.  
 You win not by outspending, but by **out-summoning**: using timing, positioning, and courage to turn your finite deck into victory.

---

## **Design Pillars**

1. **Real-Time Strategy on One Screen**

   * 3–5-minute duels on a fixed horizontal battlefield.

   * No camera panning; pure tactical tension.

2. **Every Card Counts**

   * Single-use cards — every deployment matters.

   * Decks up to 30 cards, creating pacing from skirmish to all-out war.

3. **Asymmetric Heroes & Fate**

   * Collectable summoners with unique mana curves, affinities, and growth potential.

   * First hero chosen by *fate* to create a unique player journey.

4. **Meaningful Risk, Earned Reward**

   * Optional wagers with emotional stakes.

   * Power variance matters long-term but never decides early matches.

5. **Collection Pride & Personal Growth**

   * Rarity equals *potential*, not instant power.

   * Even common heroes can become legends.

---

## **Unique Selling Points**

* **Single-use deck system** — deep tactical decisions unlike any other mobile battler.

* **Summoner heroes as resource engines** — bases with personality and strategic identity.

* **Optional wagers** — emotional stakes without gambling.

* **Randomized fated origins** — every player’s story begins uniquely.

* **Fast, one-screen RTS feel** — real-time readability built for mobile.

---

## **Tone & Emotion**

Competitive yet **mythic** — *Clash Royale meets Hades*.  
 High-contrast fantasy with distinct elemental identities.  
 Serious, mystical, and proud.

---

## **High-Level Structure**

* **Core Loop:** Collect → Build → Battle → Reward → Evolve

* **Session Length:** 3–5 minutes

* **Monetization:** Cosmetics and hero unlocks (no pay-to-win)

* **Platform:** Mobile-first, expandable to PC

* **Engine:** Godot

---

## **Vision Summary**

**Project Summoner** is a competitive, emotionally charged dueling game where individuality is built into the rules.  
 Every match is different. Every army is unique.  
 Every victory is personal.

# Solo Development Roadmap

# **🧭 Project Summoner — Solo Development Roadmap**

⚙️ Engine: Godot 4 (2D horizontal field)  
 🧠 Focus: tight prototype first, systems later  
 🎯 Goal: reach a showcase-ready vertical slice while retaining full ownership

---

## **Phase 0 — Foundations (1 week)**

*Set up environment, pipeline, and testbed.*

**Goals**

* Install Godot 4, set up version control (Git \+ remote repo).

* Create a working scene with camera, base UI, and simple state machine.

* Load one “unit” prefab (e.g., colored square) and move it across the screen.

**Deliverables**

* Project folder structure: `scenes/`, `scripts/`, `assets/`, `data/cards.json`.

* Base class: `Card.gd` and `Unit.gd` (spawnable with stats).

* Horizontal field prototype (one screen, two bases).

**Success Metric:** you can spawn a dummy unit that walks and damages a base.

---

## **Phase 1 — Sandbox Prototype (Minimum Playable, 3–4 weeks)**

*Make one battle fun against AI.*

**Goals**

* Implement mana system \+ card-draw logic (4-card hand).

* Add 6–8 core card archetypes (e.g., melee, ranged, tank, spell, structure).

* Add simple “AI” that summons units on a timer.

* Add match-end condition (base HP ≤ 0 → win/loss screen).

* Very simple visuals (colored shapes, element-tinted particles).

**Deliverables**

* JSON-driven card data (name, variant, rarity, stats).

* One hero with passive buff (e.g., Fire Affinity \+10 % damage).

* Local data save for deck composition.

**Success Metric:** playable match lasts 2–4 minutes and feels engaging.  
 **Stretch Goal:** prototype dismantle/keep screen (no real economy yet).

---

## **Phase 2 — Core Loop Build (Playable Demo, 6–8 weeks)**

*Connect gameplay to progression and economy.*

**Goals**

* Implement keep / dismantle / transmute flow after each match.

* Add resource currencies (Gold, Essence, Fragments).

* Expand to 12–16 cards using your hybrid rarity-variant system.

* Basic UI polish: deck builder, reward screen, hero stats view.

* Add minimal sound \+ particles for impact feedback.

* Persistent save/load for collection and resources.

**Deliverables**

* Functional meta-loop: **Play → Earn → Decide → Grow.**

* One hero leveling system tied to Essence.

* Two AI difficulties.

**Success Metric:** game feels like a loop, not just a sandbox.  
 **Stretch Goal:** short “campaign” of 3 AI duels with increasing stakes.

---

## **Phase 3 — Showcase / Vertical Slice (3–4 months total elapsed)**

*Make it look and feel like a finished indie game.*

**Goals**

* Replace placeholder art with simple stylized 2D kit (Kenney assets \+ palette).

* Add unique FX per element (recolor particle \+ shader tint).

* Add simple music \+ UI sound set.

* Add hero selection screen (2 heroes, different affinities).

* Tighten combat pacing, add small ability cooldowns or ultimates.

* Add post-match summary (XP, rewards, streak).

**Deliverables**

* Polished build for Itch.io or Steam demo.

* 2 heroes × 16–20 cards × variance \= \~100 unique rolls.

* Trailer-ready footage (60 s).

**Success Metric:** 5–10 people can play it and say “I’d wishlist this.”  
 **Stretch Goal:** implement asynchronous PvP (upload replay data only).

---

## **Phase 4 — Post-Showcase Options (optional)**

| Path | Description | Time Est. |
| ----- | ----- | ----- |
| **Content Expansion** | \+5 new archetypes using same framework. | 1–2 months |
| **Online PvP Prototype** | Basic networking via Godot Multiplayer API. | 2–3 months |
| **Mobile Port** | Adapt UI \+ controls. | 1–2 months |
| **Team Up Phase** | Bring on artist or composer under rev-share. | variable |

---

## **Time \+ Effort Reality**

| Dev Mode | Hours / week | Est. to Showcase |
| ----- | ----- | ----- |
| part-time (10–15 h) | nights/weekends | 6–9 months |
| half-time (20–25 h) | consistent schedule | 5–6 months |
| full-time (35–40 h) | dedicated push | 3–4 months |

---

## **Smart Reuse Principles**

* Every new **archetype** must support 3–5 variants (color \+ behavior tweaks).

* Use **shader tinting** for element recolors.

* Store **variants in data**, not new prefabs.

* One **particle prefab** per element can be used for 80 % of attacks.

* Procedural names (“Flame Warden”, “Frost Warden”) \= instant variety.

---

## **✅ Definition of Done (Showcase Stage)**

* 2D horizontal arena with working battle loop.

* Deck, mana, card variance, and economy all functional.

* At least 1 hour of compelling gameplay.

* Polished enough for public demo or pitch video.

* You retain 100 % ownership, all code/assets local.

# Hero System Spec

**🧙 Project Summoner — Hero System Spec**

*Last updated: November 2025*

**Purpose:** Define the foundational structure for Heroes — the player-controlled summoners who shape battle strategy, resource flow, and collection bias.

---

## **1️⃣ Hero Overview**

Heroes serve as the player's identity and primary strategic modifier. Each hero brings unique starting traits, affinities, and potential growth paths.

**Core Principles:**

* **Asymmetry from the start:** Each player begins with a randomly assigned *Fated Hero*, ensuring no two journeys start identically.  
* **Identity through play:** Heroes define the player’s mana generation style, favored card elements, and potential signature ability.  
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

Affinities influence deck bias and card stat variance, reinforcing the player’s identity.

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
| **Victory Condition** | Destroying an opponent’s base immediately ends the match. |
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

# Project Summoner — Combat System Spec (v1

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

