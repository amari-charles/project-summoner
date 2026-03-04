# Gameplay Requirements for Simulation

> **Purpose**: Defines what the game should do — the user's design intent. This is the source of truth for gameplay mechanics. All architecture and implementation decisions flow from this document.
>
> **Status**: Finalized — 2026-02-22

---

## 1. Match Flow

**Status: Confirmed**

A match progresses through these stages:

### 1.1 Deck Selection (Pre-Match)

- Player selects a deck before entering the match
- Deck composition is locked once the match begins

### 1.2 Preparation Phase

- **Duration**: ~30 seconds (configurable per battle)
- **Allowed actions**: Play **summon cards only** — no spells
- **Mana**: Fixed pool (no regeneration). Spend wisely — whatever is left carries into battle
- **Units**: Spawn at chosen positions but remain **inactive** (no movement, no combat, no targeting)
- **Purpose**: Strategic army placement. Both players build their initial formations simultaneously

### 1.3 Battle Start Transition

- All prep-phase units **activate simultaneously** (begin moving, targeting, fighting)
- Player's hand **refreshes** — draw a fresh hand of 4 cards from the deck
- **Mana carries over** from prep (not refreshed)
- All card types now available (summons + spells)

### 1.4 Battle Phase

- Units fight **fully autonomously** — player has no control over unit behavior after summoning
- Player can continue playing cards (summons and spells) with remaining mana
- **No mana regeneration** — fixed pool for the entire match
- Battle continues until a win condition is met

### 1.5 Game Over

- Match ends when a win condition is satisfied
- Results displayed to both players

---

## 2. Card System

**Status: Confirmed**

### 2.1 Card Types

| Type | Description | Available During |
|------|-------------|-----------------|
| **Summon** | Spawns one or more units at a position | Prep + Battle |
| **Spell** | Applies an effect (damage, buff, debuff, revive, etc.) | Battle only |

### 2.2 Hand & Draw

- **Hand size**: 4 cards
- **Draw mechanic**: Replacement only — when you play a card, a new card is drawn from the deck into the same slot
- **Hand refresh at battle start**: Current hand is discarded, draw a fresh 4 from the deck
- **No auto-draw** — cards are only drawn when a card is played (replacement) or at battle start (refresh)

### 2.3 Mana

- **Starting mana**: Fixed pool at match start (default 100, configurable)
- **No regeneration** — mana only decreases when cards are played
- **Shared across phases**: Mana spent in prep is gone for battle. Strategic tradeoff between prep army size and battle-phase flexibility

### 2.4 Casting

- **All cards have a cast time** (channel duration based on card definition)
- **Cast lock**: While casting, the player **cannot play another card**
- **Cast speed modifier**: Summoner trait that modifies cast time (higher = faster casting)
- **Formula**: `effective_cast_time = base_cast_time / cast_speed`
- **Mana deducted immediately** when casting begins
- **On completion**: Unit spawns (summon) or spell effect triggers

### 2.5 Deck

- Deck is selected pre-match and locked for the duration
- When a card is played, it moves to the discard pile
- **Deck recycle**: When both hand and deck are empty, the discard pile is shuffled back into the deck (using seeded RNG) and a new hand is drawn
- Single-use per cycle — no infinite looping of a single card

### 2.6 Spawn Count & Formations

- **Configurable per card**: A card can spawn 1 unit or many (e.g., 1 titan vs 12 wisps)
- **Formation patterns**: Configurable per card (grid, line, ring, V-shape, etc.)
- Player picks a spawn point, units auto-arrange in the card's configured formation around that point

### 2.7 Spell Effects

**Status: Confirmed**

Spells need a flexible effect system that can support:
- Area damage (fireball, lightning strike)
- Buffs (speed boost, damage increase, shield)
- Debuffs (slow, weaken, vulnerability)
- Healing / revive
- Any combination of the above

The effect system should be **data-driven and extensible** — adding a new spell effect should not require new simulation code for each one.

### 2.8 Spell Targeting

**Status: Confirmed**

Each spell specifies its targeting mode in its definition. **Both modes are supported**:

| Mode | Description | Example |
|------|-------------|---------|
| **Position-based** | Player picks a point on the battlefield. AoE hits everything in radius. Single-target hits the nearest valid unit to that point | Fireball, Lightning Strike |
| **Unit-based** | Player picks a specific unit. Buff/heal applies to that unit. AoE centers on that unit | Heal, Shield, Targeted Slow |

The targeting mode is a property of the spell definition — the simulation resolves targets differently based on the mode.

---

## 3. Summoner

**Status: Confirmed**

### 3.1 Battlefield Presence

- The summoner is a **static base** — a fixed position on the battlefield, not a moving unit
- Units that have no enemy targets will advance toward the enemy summoner
- Units deal damage to the summoner when they reach it
- **Summoner HP reaching 0 = that player loses**

### 3.2 Summoner Stats

| Stat | Description |
|------|-------------|
| **HP / Max HP** | Summoner health. 0 HP = defeat |
| **Mana / Max Mana** | Fixed pool for the match |
| **Cast Speed** | Multiplier on cast times (higher = faster) |
| **Damage Bonus** | % bonus to all owned units' damage |
| **Damage Reduction** | Flat reduction to damage taken |
| **Element** | Summoner's elemental affinity |

### 3.3 Progression (Pre-Match)

- Summoners have persistent progression (levels, traits, boons) that affects their match stats
- Progression stats are **read at match initialization** and baked into summoner state — no mid-match progression changes

---

## 4. Combat System

**Status: Confirmed**

### 4.1 Damage Types

- **Physical/Magic split**: Each attack and ability has a damage type — **physical** or **magic**
- A single unit can have both types (e.g., physical basic attack + magic ability)
- Damage type determines which defense stat is checked

### 4.2 Defense Stats

**Status: Confirmed**

**Base stats** (present on every unit, can be 0):

| Stat | Description |
|------|-------------|
| **Physical Defense** | Reduces incoming physical damage |
| **Magic Defense** | Reduces incoming magic damage |
| **Evasion** | % chance to dodge an attack entirely. Uses seeded RNG. Defaults to 0 |

**Buff/ability effects only** (not base stats):

| Effect | Description |
|--------|-------------|
| **Shield** | Absorbs damage before HP is affected. Stackable — multiple shields can coexist, oldest consumed first |
| **Armor** | Temporary damage reduction effect. Applied via abilities/buffs, not a permanent base stat |

### 4.3 Damage Formula

**Status: Confirmed**

The system is architected for **swappable/pluggable formulas** — the exact reduction curve (flat vs percentage vs hybrid) is tunable without rewriting the damage system.

**Damage pipeline** (order of operations):

1. **Base damage** (from attack stat)
2. **Damage type** (physical or magic — determines which defense stat applies)
3. **Critical hit check** (seeded RNG, per-unit crit chance)
4. **Elemental matchup multiplier** (advantage/disadvantage from matchup table)
5. **Summoner damage bonus** (global % bonus to all owned units)
6. **Defense reduction** (physical defense or magic defense, based on damage type)
7. **Floor at minimum** (e.g., 1 — attacks always deal at least minimum damage)

The formula implementation is a pluggable function that takes the pipeline inputs and returns final damage. Changing the reduction curve (e.g., from flat subtraction to diminishing returns) requires changing only the formula function, not the pipeline.

### 4.4 Elemental Matchups

**Status: Confirmed — core from day one**

Core cycle: **Fire > Wind > Earth > Water > Fire**

| Attacker | Defender | Multiplier |
|----------|----------|-----------|
| Fire | Wind | 1.25x (advantage) |
| Wind | Earth | 1.25x |
| Earth | Water | 1.25x |
| Water | Fire | 1.25x |
| Reverse | direction | 0.8x (disadvantage) |

Outer elements: Lightning, Life, Death, Shadow — with their own matchup rules.

Future elements: Poison, Occultist, Holy, Ice, Metal, Spirit — the system supports arbitrary elements via the matchup table. New elements and their matchup values will be designed and added when they are implemented. No matchup definitions needed now.

### 4.5 Critical Hits

- Per-unit crit chance stat
- Rolled using seeded deterministic RNG
- Crit multiplier (configurable per unit, default 1.5x)

### 4.6 Summoner Damage

- Units that reach the enemy summoner's position deal damage to it
- Summoner damage is processed in the simulation (not physics hitboxes)
- Summoner has its own damage reduction stat

---

## 5. Unit System

**Status: Confirmed**

### 5.1 Unit Types

| Type | Description |
|------|-------------|
| **Melee** | Close range, walks to target, attacks directly |
| **Ranged** | Fires projectiles, may kite (maintain distance from target) |
| **Flying** | Elevated movement layer, can only be targeted by units with air-targeting |

### 5.2 Unit Stats

**Status: Confirmed**

Core stats every unit has:
| Stat | Description |
|------|-------------|
| **HP / Max HP** | Hit points |
| **Attack Damage** | Base damage per attack |
| **Attack Type** | Physical or Magic |
| **Attack Range** | Distance at which unit can attack |
| **Attack Speed** | Attacks per second |
| **Move Speed** | Movement velocity |
| **Crit Chance** | Probability of critical hit |
| **Crit Multiplier** | Damage multiplier on crit |
| **Movement Layer** | Ground or Air |
| **Element** | Unit's elemental type |
| **Physical Defense** | Reduces physical damage taken (base stat, can be 0) |
| **Magic Defense** | Reduces magic damage taken (base stat, can be 0) |
| **Evasion** | % chance to dodge an attack entirely (base stat, defaults to 0) |

### 5.3 Unit Lifecycle

```
Spawning → Active → Dying → Dead
```

- **Spawning**: During summon reveal animation. Not yet targetable or active
- **Active**: Full gameplay — moves, targets, attacks
- **Dying**: Death animation playing, removed from simulation
- **Dead**: Cleaned up from state

### 5.4 Autonomous Behavior

Units are **fully autonomous** once active. Player has zero control over unit behavior after summoning.

**Behavior loop**:
1. **Acquire target** → Find best enemy to attack
2. **Move toward target** → Navigate with steering/separation
3. **Attack when in range** → Deal damage on cooldown
4. **If no enemies** → Advance toward enemy summoner

### 5.5 Unit Differentiation

Units differ from each other through three axes:

1. **Stats**: Fast/weak vs slow/strong, short range vs long range, etc.
2. **Abilities**: Charge bonus, aura, death explosion, slow on hit, and more
3. **Behavior profile**: Each unit type gets a structured behavior profile:

**Movement Style**:
| Style | Description |
|-------|-------------|
| **MoveToward** | Standard — walk directly toward target |
| **Kite** | Maintain preferred range from target. Has a configurable kite range parameter |
| **FollowLeader** | Stay near group leader, move where leader moves |

**Targeting Priority**:
| Priority | Description |
|----------|-------------|
| **NearestEnemy** | Standard — target the closest enemy |
| **SummonerPriority** | Prefer attacking the enemy summoner over other units |
| **LeaderTarget** | Attack whatever the group leader is attacking |

**Retreat Condition**:
| Condition | Description |
|-----------|-------------|
| **None** | Default — no retreat behavior |
| **HpThreshold** | Flee or change behavior below X% HP |

Adding a new behavior style = add one enum value + its sim logic. No system redesign needed.

### 5.6 Unit Relationships

**Status: Confirmed — critical architecture requirement**

Units can have relationships with other units:
- **Parent-child groups**: A "mama duck" card spawns 1 leader + 3 followers
- **Follower targeting**: Followers attack whatever the leader is targeting
- **Death triggers on relationships**: When the leader dies, followers may receive a buff or debuff
- **Group identity**: Units spawned by the same card may share a group ID for relationship tracking

This means the simulation state must track:
- Which unit is the leader of a group
- Which units belong to a group
- Relationship-aware trigger conditions

---

## 6. Ability & Effect System

**Status: Confirmed — must be architectural from day one**

> "I want the architecture to cover as many possibilities as possible. It will not be easy to build these in later."

### 6.1 Design Requirements

- Abilities and spell effects should share the **same underlying effect system**
- The system must be **data-driven** — defining a new ability or spell effect should be a matter of configuration, not new simulation code for each one
- Effects must run **inside the simulation** (deterministic, inside `Tick()`) — not in the presentation layer

### 6.2 Trigger Types

The system needs to support these trigger conditions:

| Trigger | Description |
|---------|-------------|
| **On attack** | When this unit attacks |
| **On hit** | When this unit deals damage |
| **On kill** | When this unit kills a target |
| **On death** | When this unit dies |
| **On damaged** | When this unit takes damage |
| **HP threshold** | When HP drops below a percentage |
| **Timed / periodic** | Every N seconds/ticks |
| **On spawn** | When this unit first becomes active |
| **Leader death** | When this unit's group leader dies |

### 6.3 Effect Types

The system needs to support these effect categories:

| Effect | Description |
|--------|-------------|
| **Direct damage** | Deal damage to target(s) — physical or magic type |
| **Heal** | Restore HP to target(s) |
| **Stat modifier** | Temporarily change a stat (attack, speed, defense, etc.) |
| **Damage over time** | Periodic damage for a duration |
| **Heal over time** | Periodic healing for a duration |
| **Shield** | Absorb X damage before HP is affected |
| **Stun / freeze** | Prevent attacks and/or movement |
| **Slow** | Reduce move speed |
| **Revive** | Bring a dead unit back (specifics designed when implemented) |
| **AoE** | Apply an effect to all units in a radius |
| **Charge bonus** | Bonus damage after traveling a distance |

### 6.4 Current Abilities (Reference)

These abilities exist in the current codebase and should be supported by the new system:

| Ability | Trigger | Effect |
|---------|---------|--------|
| **ChargeAbility** | Distance traveled threshold | Bonus damage on next attack |
| **AuraAbility** | Periodic (every N seconds) | Damage or heal units in radius |
| **DeathExplosionAbility** | On death | AoE damage around dying unit |
| **SlowOnHitAbility** | On attack | Reduce target's move speed |

---

## 7. Projectile System

**Status: Confirmed**

Projectile behavior is **configurable per card/projectile definition** — not generic. Each projectile definition specifies its movement type and parameters as data.

### 7.1 Movement Types

| Type | Description |
|------|-------------|
| **Straight** | Linear path from attacker to target |
| **Arc** | Curved arc trajectory (Bezier or parabolic) |
| **Homing** | Tracks and follows moving target |
| **Ballistic** | Parabolic arc with gravity |
| **Weaving Homing** | Veers off then corrects course toward target |

### 7.2 Per-Definition Configuration

The movement type and its parameters are **data on the projectile definition**, not hardcoded per-type logic. Examples:

| Projectile | Movement Type | Key Parameters |
|------------|---------------|---------------|
| Arrow | Ballistic | Arc height, gravity |
| Mana Bolt | Straight | Speed |
| Missile | Weaving Homing | Veer angle, correction rate, speed |
| Fireball | Arc | Bezier control points, speed |
| Tracking Bolt | Homing | Tracking strength, speed |

The simulation implements the movement math for each movement type, but **which type** a projectile uses comes from its definition. Adding a new projectile = define its movement type + parameters in data.

### 7.3 Projectile Properties

- **Speed**: How fast the projectile travels
- **Hit radius**: How close to a unit counts as a "hit"
- **Pierce count**: How many units the projectile can pass through
- **AoE radius**: Area of effect damage on impact
- **Lifetime**: Maximum time before the projectile expires

### 7.4 Hit Detection

- Deterministic — decided by the simulation, not physics engine
- Per-tick path advancement with line-segment distance check
- Presentation-layer projectiles are visual only — the sim decides hit/miss

---

## 8. Win Conditions

**Status: Confirmed — flexible system**

### 8.1 Design

Win conditions should be a **configurable, predicate-based system** — not hardcoded types. Each battle can specify its win condition(s) through configuration.

### 8.2 Examples the System Must Support

| Condition | Predicate |
|-----------|-----------|
| **Destroy base** | Enemy summoner HP reaches 0 |
| **Survive** | Player summoner alive after X seconds |
| **First blood** | First team to kill any enemy unit |
| **Kill count** | First team to kill N enemy units |
| **Timed destroy** | Destroy enemy base within X seconds or lose |

### 8.3 Overtime

**Status: Deferred — placeholder acceptable, design later**

When the match timer expires without a winner, overtime activates. Placeholder behavior is acceptable for the initial implementation.

---

## 9. Battlefield

**Status: Confirmed layout, specific dimensions from existing implementation are acceptable**

### 9.1 Layout

- **Flat open rectangle** — no obstacles, no pathfinding needed
- Each player spawns on their side of the battlefield
- Summoner bases are at fixed positions on opposite ends
- **Terrain/obstacles may be explored in the future** but are not in scope for this version

### 9.2 Coordinate System

- **Canonical coordinates**: All simulation and network messages use a single coordinate space
- **Perspective flip**: Client sees the battlefield mirrored (their side on the near side)

### 9.3 Spawn Zones

- Each player can only spawn units in their designated half of the battlefield
- Spawn positions are validated by the simulation

---

## 10. Multiplayer

**Status: Confirmed (from architecture decisions)**

### 10.1 Authority Model

- **Host-authoritative**: Only the host runs the simulation. Client is a renderer
- **Command-based**: All player actions (card plays, forfeit) go through a command queue validated by the simulation
- **Event-driven**: Host broadcasts simulation events to the client for presentation updates

### 10.2 State Synchronization

- Host sends periodic state snapshots for drift correction
- Desync detection via state hash comparison
- Client applies snapshots to correct any divergence

### 10.3 Single-Player

- In single-player, the local machine is the host — no networking
- AI opponent submits commands through the same command queue as a human player would
- Same simulation code path for both modes

---

## 11. Randomness

**Status: Confirmed**

All gameplay-affecting randomness uses a **shared seeded RNG** with per-domain state, ensuring deterministic behavior across host and client.

| Domain | Usage |
|--------|-------|
| **Deck shuffle** | Initial deck order, deck recycle shuffle |
| **AI decisions** | Enemy card selection and timing |
| **Combat crits** | Critical hit rolls |
| **Spawn positions** | AI spawn position jitter |
| **Projectile behavior** | Veer direction for weaving projectiles |

Non-deterministic local RNG is used for presentation-only effects (VFX particles, animation jitter, audio pitch).

---

## Requirements Status

All gameplay requirements are **confirmed and finalized**. No open design questions remain.

| Area | Status |
|------|--------|
| Defense stats | Confirmed — PhysDef, MagicDef, Evasion as base stats; Shield & Armor as buff effects |
| Damage formula | Confirmed — pluggable formula with defined pipeline |
| Spell targeting | Confirmed — position-based and unit-based modes, per-spell configurable |
| Unit behaviors | Confirmed — behavior profiles with movement style, targeting priority, retreat condition |
| Projectiles | Confirmed — per-definition configuration with movement type + parameters |
| Elemental matchups | Confirmed — core + outer elements defined; future elements added via matchup table when implemented |
| Overtime | Deferred — placeholder acceptable, design later |

---

*Last updated: 2026-02-22*
*Status: Finalized*
*Source: Direct user decisions from gameplay review session*
