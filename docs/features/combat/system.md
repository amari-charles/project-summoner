# Fateforged — Combat System Spec (v2.0)

**Status:** IMPLEMENTED
**Last Updated:** 2025-12-30

**Scope:** Defines battle phases, unit simulation, targeting, movement, damage, and win conditions.

---

## 1 Battle Phases

Battles use a two-phase system designed to create the fantasy of two armies clashing:

### Phase 1: PREPARATION (30 seconds)

- Both players start with their full mana pool (50 mana by default)
- Summon units to build your army formation
- Units spawn but remain **INACTIVE** — they idle and don't fight
- Countdown timer visible at top of screen
- Card draw continues normally
- Playing a card locks the player briefly (summon time delay)

**Design Intent:** This phase gives players time to plan formations and commit resources before the chaos of battle. It creates the "two armies facing off" moment.

### Phase 2: BATTLE (until victory)

- All units **ACTIVATE** and begin fighting autonomously
- Players can still summon reinforcements with remaining mana
- Same summon time rules apply
- Battle continues until one Incarnation is destroyed

**Design Intent:** The battle phase is where strategy meets execution. Initial formations matter, but tactical reinforcement can turn the tide.

---

## 2 Win Condition: The Incarnation

Victory requires destroying the enemy's **Incarnation** — the summoner's magical presence on the battlefield.

### What Is the Incarnation?

- A manifestation of the summoner's power projected onto the field
- Not the summoner themselves (they command from elsewhere)
- Breaking it severs their connection to this battle
- Visually represented as a glowing elemental orb/presence

### Why Incarnation Instead of Base/Nexus?

| Old Approach | Problem | New Approach |
|--------------|---------|--------------|
| Base/Nexus as target | Arbitrary structure with no narrative meaning | Incarnation — the summoner's magical presence |
| Static building | Doesn't fit all battle contexts | Works for duels, sparring, or war |

### Win Condition Types

Battles can define different win conditions via `WinConditionIDs`:

| Condition | Behavior | Config Fields |
|-----------|----------|---------------|
| `DESTROY_INCARNATION` | Default — destroy enemy incarnation to win | (none) |
| `SURVIVE_TIME` | Survive for duration, win on timeout | `time_limit` (seconds) |
| `TIMED_DESTROY` | Destroy incarnation within time limit, lose on timeout | `time_limit` (seconds) |
| `KILL_COUNT` | Kill N enemy units to win | `kill_target` (int) |

---

## 3 Mana System

### Fixed Mana Pool (No Regeneration)

- Players start with full mana (50 by default, modified by summoner stats)
- **No mana regeneration during battle**
- All resources available upfront for strategic planning
- Forces commitment — you can't wait and react indefinitely

### Why Fixed Pool?

| Old System | Problem | New System |
|------------|---------|------------|
| Mana regenerates over time | Felt like a trickle, reactive gameplay | Fixed pool forces strategic planning |
| Wait for mana, play reactively | No urgency, passive gameplay rewarded | Commit early or save for reinforcements |

---

## 4 Summon Time

When playing a card, there's a delay before the unit appears:

- Card determines `summon_time` (in seconds)
- Player sees casting indicator (circular cooldown)
- Summoning circle VFX appears at spawn location
- Player cannot play other cards during summon time
- After delay, unit spawns (INACTIVE during prep, ACTIVE during battle)

**Design Intent:** Adds weight to summoning powerful units. Creates anticipation and counterplay windows.

---

## 5 Unit Model

### Shared Fields

`team`, `hp`, `move_speed`, `attack_damage`, `attack_range`, `attack_rate`, `attack_windup`, `aggro_radius`, `is_ranged`, `is_flying`, `tags`

### Activation States

| State | Phase | Behavior |
|-------|-------|----------|
| `INACTIVE` | PREPARATION | Unit idles, doesn't move or attack |
| `ACTIVE` | BATTLE | Unit follows normal AI behavior |

### Combat States

`IDLE`, `CHASE`, `ATTACK`, `HOLD`, `DEAD`

---

## 6 Simulation Loop

Fixed-timestep tick (≈60 FPS).
Order each frame:

1. Resolve player input + summons (with summon time delay)
2. Check battle phase (PREPARATION or BATTLE)
3. **If PREPARATION:** Units remain INACTIVE
4. **If BATTLE:** Units → Sense → Decide → Move → Act
5. Projectiles / Spells update
6. Damage queue resolve → Deaths handled
7. FX + Events
8. Win condition check (Incarnation destroyed?)

---

## 7 Sensing

- Acquire nearest visible enemy within `aggro_radius`
- Must be inside team vision (fog aware)
- If none found → no target
- **Only during BATTLE phase** — units don't sense during PREPARATION

---

## 8 Decision Logic (Always-Advance Baseline)

| Condition | State / Behavior |
|-----------|------------------|
| Enemy in attack_range (+LOS) | ATTACK (wind-up → resolve → cooldown) |
| Enemy seen but out of range | CHASE (move toward until in range) |
| No enemy in aggro | **ADVANCE toward enemy Incarnation** (attack-move) |
| Incarnation in range + no enemy within intercept radius | ATTACK INCARNATION |

**Rule of thumb:** Units always press forward unless actively attacking.
Keeps tempo and ensures Incarnations die when the front is won.

---

## 9 Movement

- **Seek:** vector toward target or Incarnation
- **Separation:** repulse from near allies (<48 px)
- **Clamp:** stay within battlefield bounds
- **Flying flag:** ignores separation

---

## 9.1 Flying Units

Flying units hover above the battlefield at a fixed altitude, creating vertical gameplay.

### Properties

| Property | Description | Default |
|----------|-------------|---------|
| `movement_layer` | `GROUND` or `AIR` | `GROUND` |
| `flight_altitude` | Height above ground (Y position) | 4.0 |
| `prefer_targets_below` | Prioritize targets directly beneath | false |
| `below_target_radius` | XZ radius for "below" targeting | 6.0 |

### Targeting Rules

| Attacker | Target | Can Target? | Notes |
|----------|--------|-------------|-------|
| Ground Melee | Flying | ❌ No | Can't reach |
| Ground Ranged | Flying | ✅ Yes | Uses 3D distance for range |
| Flying | Ground | ✅ Yes | Normal targeting |
| Flying | Flying | ✅ Yes | Normal targeting |

### Shadow System

Flying units have a shadow component that stays pinned to the ground:
- Shadow position updated every physics frame
- Shadow scales smaller and fades with altitude
- Provides visual grounding for floating units

### Attack Positioning (e.g., Storm Cloud)

Units with `prefer_targets_below = true` have special attack behavior:
- **Target normally** — chase enemies like any unit
- **Attack only when above** — must be within `below_target_radius` XZ distance
- Creates "hover and strike" gameplay pattern

---

## 10 Attacks

- Wind-up → Hit → Cooldown
- Melee = instant damage at resolve
- Ranged = spawn projectile (120–180 px/s)
- Attack rate ≈ 1 / `attack_rate`
- Retarget allowed each tick

---

## 11 Projectiles

- Constant speed, light homing each tick
- Hit when within small radius (≈8 px) → enqueue damage
- Friendly-fire off
- Despawn on miss timeout (2s)

---

## 12 Damage & Death

- Resolve damage queue post-actions
- `hp -= amount`; if ≤0 → DEAD
- Quick fade (≤0.4s)
- On-death effects supported

---

## 13 Match Flow Integration

- Preparation phase: 30 seconds
- Battle phase: no time limit (until victory)
- **Overtime (optional):** If enabled, +50% damage to Incarnations after time threshold

---

## 14 Card Exhaustion

- Cards are single-use
- If player has 0 mana + no cards in hand + no units alive → **Exhausted State**
  - Gains no new vision; enemy gains full vision
  - Can still win if remaining forces finish Incarnation
- No auto-win trigger for opponent

---

## 15 Offline AI Sandbox

- Same fixed mana pool rules
- Spends mana during preparation and battle phases
- Picks card type weights (frontline > ranged > spell)
- Places units randomly within front third of own half
- Difficulty knobs: mana bonus %, play interval jitter

---

## 16 Performance Targets

- ≤ 100 active units on screen
- < 5 ms simulation per frame on mid-range PC
- Combat stable in soak tests (20 v 20 for 2 min @ 60 FPS)

---

## 17 Future Behavior Extensions

| Feature | Description | Purpose |
|---------|-------------|---------|
| **Hold/Guard Orders** | Unit stops at summon point until enemy in range | Supports ranged formations |
| **Retreat/Regroup AI** | Pull back when isolated or low HP | Prevent suicidal pushes |
| **Unit Roles** | `advance_on_clear`, `defensive_anchor`, `ambusher` | Adds personality to archetypes |
| **Path Weights** | Light navmesh with preferred lanes or flank routes | Enables terrain depth later |

---

## 18 Definition of Done

- ✅ Two-phase battle system (PREPARATION → BATTLE)
- ✅ Fixed mana pool (no regeneration)
- ✅ Summon time mechanics with VFX
- ✅ Unit activation states (INACTIVE/ACTIVE)
- ✅ Units spawn and advance toward enemy Incarnation
- ✅ Fights resolve and push front lines naturally
- ✅ Incarnation destruction ends match
- ✅ Offline AI completes loops reliably
- ✅ Prep phase countdown timer visible

---

*Related Documents:*
- [Card System](../cards/system.md)
- [Summoner System](../summoners/README.md)
- [Coordinate System](../coordinates/system.md)
