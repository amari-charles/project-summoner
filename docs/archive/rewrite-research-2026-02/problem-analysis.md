# Problem Analysis: Why the Current Simulation Architecture is Broken

> **Purpose**: Documents exactly what went wrong with the `feature/match-state-simulation` branch and why per-bug fixes are futile. Any future session should read this to understand *why* we're rewriting, not just *what* to build.
>
> **Status**: Updated — 2026-02-22

---

## Executive Summary

The `feature/match-state-simulation` branch has **36+ external mutation sites** writing to MatchState outside `Simulation.Tick()`, a **dual-authority bug** where both host and client run the simulation, and an **unwired command queue** that bypasses the entire validation layer. The simulation code itself (SimBehavior, SimDamage, SimMovement, etc.) is solid — the problem is that the integration was half-completed, leaving two parallel systems fighting over the same state.

---

## Problem 1: 36 External Mutation Sites

The architecture doc states: *"Simulation.Tick() is the ONLY state mutator"* and *"State changes ONLY via validated commands."*

**Reality**: 36+ call sites write directly to MatchState outside of `Tick()`.

### Writes That Actively Cause Desync

| # | Method | Location | What It Does | Why It's Bad |
|---|--------|----------|-------------|--------------|
| 1 | `SyncStatsToMatchState()` | `Unit3D.cs:~1851` | Overwrites `AttackDamage`, `CurrentHp`, `MoveSpeed` from presentation | Fires every trigger activation during Battle. Overwrites sim values with stale presentation state |
| 2 | `Heal()` | `Unit3D.cs:~1158` | Writes `unitData.CurrentHp` directly | No Battle-phase guard. Called by on-kill trigger. Corrupts sim HP |
| 3 | `OnHealthDeath()` → `RemoveUnit()` | `Unit3D.cs:~1367` | Removes unit from `State.Units` dictionary | Presentation removes units mid-tick. Sim can't find unit on next iteration |
| 4 | `ApplySummonerDamage()` | `summoner.gd:~898` via hitbox | Writes summoner HP from physics hitbox | Units hitting summoner hurtbox write damage outside Tick. Sim may be reading HP simultaneously |
| 5 | `DeductMana()` + `StartCasting()` | `summoner.gd:~517,538` | Writes mana and casting state | Card play writes directly while Tick may be reading for mana regen |
| 6 | `SyncCardDraw/SyncHandAfterCardPlay/SyncDeckRecycle` | `summoner.gd:~450,470,587,589` | Modifies deck/hand/discard arrays | Deck state modified mid-simulation, corrupts snapshot hashes |

### Writes That Are Init/Coordination (Not Harmful)

These write at well-defined moments (match start, unit spawn, game end) and don't conflict with `Tick()`:

- `RegisterUnit()` — at spawn time
- `RegisterSummoner()` / `SetSummonerStats()` / `SetSummonerBonuses()` — at match init
- `SyncUnitActivationState()` — at phase transition
- `SetWinnerTeam()` — at game end
- `IncrementKillCount()` — bookkeeping
- `ApplySnapshot()` — client correction from host (intentional)
- `ApplyDeckSync()` — client correction from host
- `SetOvertime()` / `SkipPreparation()` — phase control

### The Root Design Violation

None of the GDScript calls (`summoner.gd`, `Unit3D`) go through a command queue. Every call is a **synchronous direct write** into MatchState. The command queue exists (`State.PendingCommands`) but `summoner.gd` calls `SimulationNode` methods directly, bypassing it entirely.

---

## Problem 2: Dual Authority Bug

### What Happens

Both host AND client call `SimulationNode._PhysicsProcess()`, which calls `Simulation.Tick()`. This means:

- **Host** runs `Tick()` → advances MatchState → builds snapshot → sends to client
- **Client** ALSO runs `Tick()` → advances its own MatchState → gets snapshot → tries to correct

### Why This Fails

1. The client's `Tick()` runs on its local physics frame timing, which differs from the host's
2. Both sides apply commands at different ticks (client doesn't wait for host validation)
3. The snapshot correction fights against the client's own simulation, creating oscillation
4. Combat damage is applied twice — once by the client's sim, once by the snapshot correction
5. Unit death ordering diverges immediately because combat timing differs

### What Should Happen

Only the **host** runs `Simulation.Tick()`. The client should:
- Send commands (card plays) to the host
- Receive events + snapshots from the host
- Apply them to update its local presentation
- Never run its own simulation tick

---

## Problem 3: Unwired Command Queue

### The Design

The architecture specifies a command-based input flow:
```
Player Input → Command → Queue → Tick() validates and applies → Events emitted
```

### The Reality

`summoner.gd` calls `SimulationNode` methods directly:
```
Player drags card → summoner.play_card_3d() → SimulationNode.DeductMana() + StartCasting()
                                             → SimulationNode.SyncHandAfterCardPlay()
                                             → SimulationNode.SyncCardDraw()
```

The command queue (`QueuePlayCard()`) exists on `SimulationNode` but is only called in specific multiplayer code paths. The primary single-player flow bypasses it entirely.

### Impact

- Commands aren't validated by the simulation before execution
- Mana deduction happens in GDScript before the sim can check it
- Card state changes (hand → discard → draw) happen outside the deterministic tick
- There's no single entry point for "play a card" — the logic is split between `summoner.gd` and `Simulation.Tick()`

---

## Problem 4: Dual Combat Paths

### SimBehavior (Simulation Layer)

Inside `Simulation.Tick()`:
```
SimBehavior.TickBehavior() → SimTargeting → SimMovement → SimDamage
                                                           ↓
                                                    writes UnitData.CurrentHp
```

This is the **correct** path — deterministic, runs identically on all machines.

### DamageSystem (Presentation Layer)

In Godot's physics loop:
```
Unit3D attack → SpawnMeleeHitbox()/SpawnProjectile()
                    ↓
              HitboxComponent (Area3D) overlaps HurtboxComponent
                    ↓
              HitResolver.ResolveHit() → DamageSystem.ApplyDamage()
                    ↓
              Unit3D.OnTakeDamage() → SimulationNode.ApplyUnitDamage()
                    ↓
              writes UnitData.CurrentHp
```

This is the **legacy** path — non-deterministic, depends on Godot physics frame timing.

### Both Run Simultaneously

During Battle phase, both paths fire:
1. `SimBehavior` writes damage to MatchState via `Tick()`
2. `DamageSystem` hitboxes also detect hits and write damage via `ApplyUnitDamage()`

Result: **Double damage**, non-deterministic death timing, immediate hash divergence.

A fragile phase guard was added to `Unit3D.OnTakeDamage()` to return early during Battle, but this is a bandaid — the hitbox system still fires, wastes resources, and any code path that bypasses the guard re-introduces the double-damage bug.

Similarly for projectiles: `SimProjectile` runs a deterministic projectile simulation, but `RangedUnit3D` still spawns physics `Projectile3D` nodes that independently detect hits.

---

## Problem 5: Why Per-Bug Fixes Don't Work

### The Whack-a-Mole Pattern

Every fix attempt on this branch followed the same pattern:
1. Observe a desync (hash mismatch, units dying at wrong times)
2. Add a guard/check to one code path
3. Another external mutation site causes a new desync
4. Add another guard
5. Repeat

This happened because the **architecture itself is broken**. With 36 external write sites, fixing any one site just moves the divergence to the next one.

### The Fundamental Issue

The simulation layer and the presentation layer both believe they own the game state. The integration was half-completed:

| System | Designed For | Actual State |
|--------|-------------|-------------|
| `Simulation.Tick()` | Sole state mutator | One of many state mutators |
| `SimBehavior` | Drives all combat | Runs in parallel with DamageSystem |
| `SimMovement` | Drives all movement | Positions also synced from Godot physics |
| `SimProjectile` | Drives all projectiles | Projectile3D nodes also detect hits |
| Command queue | All input goes through commands | Bypassed by direct GDScript calls |
| `Unit3D` | Read-only presentation | Actively writes HP, damage, targeting, stats |
| `summoner.gd` | Submits commands | Directly mutates mana, casting, deck state |

### What a Fix Requires

You can't fix this by adding more guards. You need to:
1. **Choose one authority** — `Simulation.Tick()` is the only writer during Battle
2. **Close all other write paths** — Remove or gate every external mutation method
3. **Wire the command queue** — All player actions go through commands
4. **Make presentation read-only** — `Unit3D` and `summoner.gd` read from MatchState, never write
5. **Host-only simulation** — Client doesn't run `Tick()`

This is what the rewrite accomplishes.

---

## What's Worth Keeping

The simulation code itself is well-written and should be cherry-picked:

| Component | Status | Notes |
|-----------|--------|-------|
| `MatchState` / `UnitData` / `SummonerData` | ✅ Good | Clean data model |
| `Simulation.Tick()` loop | ✅ Good | Correct structure |
| `SimBehavior` | ✅ Good | Deterministic unit behavior |
| `SimDamage` | ✅ Good | Deterministic damage calculation |
| `SimMovement` | ✅ Good | Deterministic movement |
| `SimSteering` | ✅ Good | Deterministic separation/flanking |
| `SimTargeting` | ✅ Good | Deterministic target acquisition |
| `SimProjectile` | ✅ Good | Deterministic projectile simulation |
| `DeterministicRng` | ✅ Good | Seeded per-domain RNG |
| `StateSnapshotBuilder` | ✅ Good | Hash computation, snapshot building |
| `MessageSerializer` | ✅ Good | JSON serialization with round-trip tests |
| `HostRunner` / `ClientRunner` | ⚠️ Needs fixes | Structure is fine, data flow is wrong |
| `SimulationNode` | ⚠️ Needs cleanup | Too many public mutation methods |
| Trigger system | ❌ Needs migration | Writes directly to UnitData |
| DamageSystem hitboxes | ❌ Remove for sim combat | Legacy path, conflicts with SimDamage |

---

*Last updated: 2026-02-22*
*Source: Codebase analysis of `feature/match-state-simulation` branch + planning transcript*
