# Architecture Decisions

> **Purpose**: The core design rules for the simulation rewrite. Each decision includes rationale and the specific problem it solves. These decisions are non-negotiable once approved — any AI session working on the rewrite must follow them.
>
> **Status**: Finalized — 2026-02-26

---

## Decision 1: Host-Only Tick

### Rule

Only the **host** runs `Simulation.Tick()`. The client **never** runs `Tick()`.

### What the Host Does

- Runs `Simulation.Tick()` every physics frame
- Validates and applies commands from both players
- Emits simulation events (damage, death, spawn, phase change)
- Builds and sends state snapshots periodically (every N ticks)
- Broadcasts events to the client

### What the Client Does

- Sends commands (card plays, forfeit) to the host
- Receives events + snapshots from the host
- Applies events to update presentation (spawn units, play damage VFX, etc.)
- Applies snapshots to correct any drift
- **Never** calls `Simulation.Tick()`
- **Never** computes damage, movement, or targeting locally

### Rationale

The current branch has both host and client running `Tick()`, causing:
- Dual authority over game state
- Combat damage applied twice
- Death ordering divergence
- Snapshot corrections fighting against client simulation

With host-only tick, there's exactly one source of truth. The client is a dumb renderer.

### Single-Player Mode

In single-player, the local machine IS the host. `Simulation.Tick()` runs normally. There is no client. The AI opponent submits commands through the same command queue as a remote player would.

---

## Decision 2: Single Mutation Path

### Rule

Only **simulation code** writes to MatchState during the Battle phase: `Simulation.Tick(fixedDelta)` for normal progression, plus `SnapshotApplier.Apply()` on clients for host-authoritative correction.

No other code — not `Unit3D`, not `summoner.gd`, not `HostRunner`, not `ClientRunner` — may write to MatchState fields during gameplay. Presentation/GDScript never writes.

### Allowed Writes Outside Tick

| When | What | Why |
|------|------|-----|
| **Match initialization** | `RegisterUnit()`, `SetSummonerStats()`, deck setup | One-time setup before battle starts |
| **Client snapshot application** | `ApplySnapshot()` | Host-authoritative correction |
| **Phase transitions** | `SkipPreparation()`, `SetOvertime()` | System-level state changes (should become commands) |

### Forbidden Writes During Battle

All of these must be removed or routed through commands:
- `ApplyUnitDamage()`, `ApplySummonerDamage()` — damage goes through SimDamage
- `SyncUnitPosition()`, `SyncUnitTarget()` — movement/targeting goes through SimMovement/SimTargeting
- `SyncStatsToMatchState()` — triggers go through sim-side buff system
- `Heal()` — healing goes through sim-side effect
- `DeductMana()`, `StartCasting()` — card plays go through PlayCardCommand
- `SyncCardDraw()`, `SyncHandAfterCardPlay()`, `SyncDeckRecycle()` — deck management goes through Tick

### Enforcement

`SimulationNode` mutation methods should be marked `internal` or removed entirely. External code interacts with the simulation ONLY via:
1. **Commands** (input → command → queue → Tick validates and applies)
2. **Reads** (presentation reads MatchState, never writes)

### Rationale

Solves the 36 external mutation sites problem. If only `Tick()` writes, there's one deterministic code path to debug. No more whack-a-mole.

---

## Decision 3: Command-Based Input

### Rule

All gameplay actions go through the **command queue**. Nothing bypasses it.

### Command Flow

```
Player Input (drag card)
       │
       ▼
  Create PlayCardCommand
       │
       ▼
  Local sanity check (hand index valid? mana plausible?)
       │
       ├── [Single-player] Add to local command buffer → Tick() picks it up
       │
       └── [Multiplayer]
              ├── [Host] Add to command buffer → Tick() picks it up
              └── [Client] Send to host → Host adds to buffer → Tick() picks it up
```

### Command Types

| Command | Data | Description |
|---------|------|-------------|
| `PlayCardCommand` | `PlayerIndex, HandIndex, SpawnPosition` | Play a card from hand |
| `ForfeitCommand` | `PlayerIndex` | Surrender |
| ~~`CastSpellCommand`~~ | *(Merged into `PlayCardCommand` — see architecture-diagram.md Design Decision #2)* | Spells use `PlayCardCommand` with nullable `TargetPosition`/`TargetUnitId` |

### What Tick Does With Commands

1. Dequeue commands for current tick
2. Validate each against current MatchState (mana check, hand bounds, phase check, etc.)
3. Apply valid commands (deduct mana, start cast timer, etc.)
4. Reject invalid commands (emit rejection event)
5. Continue with simulation systems (movement, targeting, combat, etc.)

### Rationale

Solves the unwired command queue problem. Currently `summoner.gd` calls `DeductMana()` + `StartCasting()` directly. With command-based input:
- All actions are validated by the simulation against current state
- All actions have deterministic timing (applied at a specific tick)
- Host can validate client commands before applying
- Replay becomes possible (replay = feed saved commands into fresh simulation)

---

## Decision 4: Read-Only Presentation

### Rule

During Battle phase, `Unit3D` and `summoner.gd` are **read-only** consumers of MatchState. They render state; they don't compute it.

### Unit3D Responsibilities (After Rewrite)

| DO | DON'T |
|----|-------|
| Read position from MatchState, update visual position | Compute movement with `MoveAndSlide()` during Battle |
| Read HP from MatchState, update health bar | Apply damage via hitbox physics |
| Read target from MatchState, face toward target | Run targeting logic |
| Play attack animation when sim emits `UnitAttackedEvent` | Spawn hitboxes for damage detection |
| Play death animation when sim emits `UnitDiedEvent` | Decide when unit dies |
| Show damage numbers from sim events | Compute damage amounts |

### summoner.gd Responsibilities (After Rewrite)

| DO | DON'T |
|----|-------|
| Submit `PlayCardCommand` when player drags a card | Deduct mana directly |
| Read mana/HP from MatchState for UI | Write mana/HP to MatchState |
| Show casting progress from MatchState | Start/stop casting timers |
| Update hand display from sim events | Manage deck/hand/discard arrays |

### Rationale

Solves the dual combat path problem. If `Unit3D` never writes damage, there's no double-damage. If `summoner.gd` never writes mana, there's no race condition with `Tick()`.

---

## Decision 5: Event-Driven UI

### Rule

GDScript reacts to **simulation events** (signals), not polling or computing.

### Event Flow

```
Simulation.Tick()
       │
       ├── Emits SimEvents (UnitDamaged, UnitDied, CastingStarted, etc.)
       │
       ▼
SimulationNode.EmitEvents()
       │
       ├── Converts SimEvents to Godot signals
       │
       ▼
GDScript signal handlers
       │
       ├── Unit3D: Play animations, show VFX, update visual state
       ├── summoner.gd: Update UI, show casting bar
       ├── HUD: Update mana display, kill count
       └── Camera: Shake on big hits, etc.
```

### Key Events

| Event | Consumers | What They Do |
|-------|-----------|-------------|
| `UnitAttackedEvent` | `Unit3D` | Play attack animation |
| `UnitDamagedEvent` | `Unit3D` | Show damage number, flash |
| `UnitDiedSimEvent` | `Unit3D` | Play death animation, cleanup |
| `SummonerHpChangedEvent` | `summoner.gd`, HUD | Update HP bar |
| `SummonerManaChangedEvent` | `summoner.gd`, HUD | Update mana display |
| `CastingStartedEvent` | `summoner.gd` | Show casting bar |
| `CastingCompletedEvent` | `summoner.gd`, presentation layer | Sim registers `UnitData` in MatchState; presentation spawns visual `Unit3D` |
| `HandChangedEvent` | Hand UI | Update card display |
| `PhaseChangedEvent` | Battle controller | Transition phases |
| `GameOverEvent` | Game controller | Show results |

### Rationale

Decouples presentation from simulation completely. The simulation emits facts ("unit 7 took 50 damage"), presentation reacts to them ("show damage number, flash red"). No presentation code needs to understand combat math.

---

## Decision 6: Branch Strategy

### Rule

Create a **new branch from `main`**, cherry-pick working simulation files from the current branch.

### What to Cherry-Pick

These files from `feature/match-state-simulation` are solid and should be brought over:

**Simulation core** (pure C#, no Godot deps):
- `MatchState.cs`, `UnitData.cs`, `SummonerData.cs`, `GamePhase.cs`
- `Simulation.cs` (the Tick loop)
- `SimBehavior.cs`, `SimDamage.cs`, `SimMovement.cs`, `SimSteering.cs`, `SimTargeting.cs`, `SimProjectile.cs`
- `DeterministicRng.cs`
- `SimEvent.cs` and event types

**Multiplayer infrastructure** (needs fixes but structure is correct):
- `HostRunner.cs`, `ClientRunner.cs`
- `StateSnapshotBuilder.cs`, `StateSnapshot.cs`
- `MessageSerializer.cs`
- `LocalPlayer.cs`

**What NOT to bring**:
- `SimulationNode.cs` — rewrite from scratch with read-only API
- Any GDScript changes that add direct MatchState writes
- The trigger system integration (will be rebuilt in sim)

### Why Not Fix In Place

The current branch has 7+ commits of incremental fixes layered on top of a broken integration. It's easier to start from `main` (which works for single-player) and layer the simulation files on top correctly, than to untangle the current branch's interleaved fixes and regressions.

### Rationale

Clean starting point. The simulation code is good — the integration is not. Cherry-picking lets us reuse the good code while getting a clean slate for the integration layer.

---

## Decision 7: Simulation-Side Effect System

### Rule

Abilities, spells, buffs, debuffs, and triggers all run **inside the simulation** using a shared, data-driven effect system. This is architectural — not a late addition.

### Why Day-One

> "I want the architecture to cover as many possibilities as possible. It will not be easy to build these in later."

The effect system touches every part of the simulation:
- **UnitData** must carry buff/debuff state, group relationships, and trigger configuration
- **SimDamage** must route through the effect pipeline (damage types, modifiers, shields)
- **SimBehavior** must fire triggers at the right moments (on-kill, on-hit, on-death, etc.)
- **The damage formula** must account for physical/magic types and defense stats

Bolting this on after building a "simple" combat system would require rewriting the core data model and damage pipeline.

### What the Effect System Must Support

- **Triggers**: On-attack, on-hit, on-kill, on-death, on-damaged, HP threshold, timed/periodic, on-spawn, leader-death
- **Effects**: Direct damage (phys/magic), heal, stat modifier, DoT, HoT, shield, stun/freeze, slow, revive, AoE
- **Spells and abilities share the same effect system** — a spell is just an effect triggered by a command instead of a unit event
- **Data-driven**: New effects/abilities should be configurable, not require new simulation code each time

### Shield Mechanics

- **Shields are stackable** — multiple shield buffs can coexist on a single unit
- **Oldest consumed first** — when damage is absorbed, the oldest shield is depleted before newer ones
- Shields absorb damage before HP is affected, processed during the damage pipeline in `SimDamage`
- Shield is a buff/ability effect only — not a base stat on units

### Spell Targeting Modes

Each spell specifies its targeting mode in its definition:
- **Position-based**: Player picks a point. AoE hits everything in radius. Single-target hits nearest valid unit to that point
- **Unit-based**: Player picks a specific unit. Buff/heal applies to that unit. AoE centers on that unit

The simulation resolves targets differently based on the spell's targeting mode. This is part of the spell definition data, not hardcoded per-spell logic.

### Unit Relationships

The simulation state must support unit groups:
- **Group ID**: Units spawned by the same card share a group
- **Leader**: One unit in a group can be the leader
- **Follower targeting**: Followers target what the leader targets
- **Relationship triggers**: Leader death can trigger effects on followers

### Rationale

The mama duck example: 1 leader + 3 followers, followers target the leader's target, leader death triggers a buff/debuff on followers. This requires group tracking, conditional targeting, and relationship-aware triggers — all in the simulation layer.

---

## Decision 8: Physical/Magic Damage Types

### Rule

Every attack and ability has a **damage type** — physical or magic. Defense is split into **Physical Defense** and **Magic Defense**.

### Impact on Architecture

- **UnitData**: Needs `AttackType` (physical/magic), `PhysicalDefense`, `MagicDefense`, `Evasion` fields
- **SimDamage**: Damage formula checks the attack's damage type against the appropriate defense stat. Evasion is checked before damage calculation (seeded RNG roll). The formula itself is **pluggable** — the exact reduction curve (flat, percentage, hybrid) is tunable without rewriting the system
- **Abilities**: Each ability's damage effect specifies its damage type independently of the unit's basic attack
- A single unit can deal both damage types (e.g., physical basic attack + magic ability)
- **Shields**: Stackable buff effects (not base stats). Multiple shields coexist, oldest consumed first. Processed in the damage pipeline after defense reduction, before HP deduction

### Base Stats vs Buff Effects

| Category | Stats |
|----------|-------|
| **Base stats** (on every unit, can be 0) | PhysicalDefense, MagicDefense, Evasion |
| **Buff/ability effects only** | Shield (stackable, oldest first), Armor (temporary damage reduction) |

### Rationale

Creates deeper strategic gameplay. Allows for units that are physically tanky but magic-vulnerable, enabling deck-building counterplay and richer unit differentiation. The pluggable damage formula allows balance tuning without architectural changes.

---

## Decision 9: Flexible Win Conditions

### Rule

Win conditions are a **configurable, predicate-based system** — not hardcoded types. Each battle specifies its win condition through configuration.

### Examples

| Condition | Predicate |
|-----------|-----------|
| Destroy base | Enemy summoner HP reaches 0 |
| Survive | Player summoner alive after X seconds |
| First blood | First team to kill any enemy unit |
| Kill count | First team to kill N units |

### Impact on Architecture

- `Simulation.Tick()` evaluates win condition predicates each tick (not a hardcoded HP check)
- Win condition configuration is part of match initialization
- New win conditions can be added by defining new predicates, not by modifying the simulation code

---

## Decision 10: Static Summoner Base

### Rule

The summoner is a **static base** at a fixed position — not a moving unit. Units advance toward it and deal damage when they reach it.

### Impact on Architecture

- The summoner is NOT in the `Units` dictionary — it's tracked separately in `SummonerData`
- Targeting uses a fixed position, not a unit reference
- Summoner damage is handled in the simulation as a distance check against the summoner's position
- No movement, no abilities, no targeting logic for the summoner itself

---

## Summary: The Six Invariants

After the rewrite, these invariants must ALWAYS hold:

1. **Only simulation code mutates MatchState**: `Simulation.Tick(fixedDelta)` during normal progression, plus `SnapshotApplier.Apply()` on clients for authoritative correction. Presentation/GDScript never writes.
2. **Client never runs `Tick(fixedDelta)`** — it receives events and snapshots from the host
3. **All gameplay actions go through commands** — no direct mutation
4. **`Unit3D` and `summoner.gd` are read-only during Battle** — they render, not compute
5. **Events drive the UI** — GDScript reacts to signals, doesn't poll or compute state
6. **Deterministic command ordering** — commands processed in `(ExecuteFrame, Team, Sequence)` order for replay determinism

Any code that violates these invariants must be rejected during review.

---

*Last updated: 2026-02-26*
*Status: Finalized*
