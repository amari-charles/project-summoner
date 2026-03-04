# Implementation Plan: Host-Authoritative Simulation Rewrite

> **Purpose**: Phase-by-phase plan for fixing the simulation architecture. Each phase is self-contained, independently testable, and designed for AI execution with clear entry/exit criteria.
>
> **Status**: Complete — All phases done (2026-02-27)
>
> **Branch**: `feature/host-authoritative-sim` (created from `main`)
>
> **Completed**:
> - [x] Pre-Work: Branch setup + sim core files copied
> - [x] Pre-Work: Sim core fixes (SimVector3, Tick Order Contract, PendingCommandBuffer, no using Godot)
> - [x] Phase 0: Host-only Tick + fixed timestep accumulator
> - [x] Phase 1: Data model foundation (damage types, groups, effects)
> - [x] Phase 2: Command-based card play + prep→battle transition
> - [x] Phase 3: Read-only Unit3D
> - [x] Phase 4: Summoner damage + flexible win conditions
> - [x] Phase 5: Abilities & triggers in simulation
> - [x] Phase 6: Spell cards via effect system
> - [x] Phase 7: Wire multiplayer
> - [x] Phase 8: Dead code removal & polish

---

## Pre-Work: Branch Setup

### Steps
1. Create `feature/host-authoritative-sim` from `main`
2. Cherry-pick simulation files from `feature/match-state-simulation` (see list below)
3. Run `dotnet build` — fix any compilation errors from missing references
4. Run single-player battle — verify the game still works

### Files to Cherry-Pick

**Simulation core** (new files, add directly):
- `scripts/csharp/Battle/Simulation/MatchState.cs`
- `scripts/csharp/Battle/Simulation/UnitData.cs`
- `scripts/csharp/Battle/Simulation/SummonerData.cs`
- `scripts/csharp/Battle/Simulation/GamePhase.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/csharp/Battle/Simulation/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/SimDamage.cs`
- `scripts/csharp/Battle/Simulation/SimMovement.cs`
- `scripts/csharp/Battle/Simulation/SimSteering.cs`
- `scripts/csharp/Battle/Simulation/SimTargeting.cs`
- `scripts/csharp/Battle/Simulation/SimProjectile.cs`
- `scripts/csharp/Battle/Simulation/SimEvent.cs` (and event subtypes)
- `scripts/csharp/Battle/Simulation/DeterministicRng.cs`

**Infrastructure** (new files, add directly):
- `scripts/csharp/Battle/Simulation/SimulationNode.cs` — **will be rewritten**, but bring the scaffold
- `scripts/csharp/Multiplayer/Core/LocalPlayer.cs`
- `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs`
- `scripts/csharp/Multiplayer/Sync/StateSnapshot.cs`
- `scripts/csharp/Multiplayer/Sync/MessageSerializer.cs`
- `scripts/csharp/Multiplayer/Authority/HostRunner.cs`
- `scripts/csharp/Multiplayer/Client/ClientRunner.cs`

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Single-player battle works (start → play cards → fight → win/loss)
- [ ] New simulation files are present but not yet wired into gameplay

---

## Phase 0: Host-Only Tick

> **Goal**: Eliminate the dual-authority bug with a single property change.

### Entry Criteria
- [ ] Pre-work complete, branch created, build passes

### Changes

1. **`SimulationNode.cs`** — Add a boolean `IsHost` property (default `true` for single-player)
2. **`SimulationNode._PhysicsProcess()`** — Gate `Tick()` call behind `IsHost` check
3. **`ClientRunner.cs`** — Set `SimulationNode.IsHost = false` when running as client
4. **`HostRunner.cs`** — Ensure `SimulationNode.IsHost = true` (should already be default)

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Single-player works (IsHost defaults to true, so no behavior change)
- [ ] In multiplayer: client no longer runs `Tick()` (verify with console print)

### Invariants
- [ ] `Tick()` only called when `IsHost == true`
- [ ] Client's `SimulationNode._PhysicsProcess` does NOT call `Tick()`

### Files Modified
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/csharp/Multiplayer/Client/ClientRunner.cs`

---

## Phase 1: Data Model Foundation

> **Goal**: Establish the simulation data model with physical/magic damage types, defense stats, unit groups, and effect/buff state. This is the foundation everything else builds on.

### Entry Criteria
- [ ] Phase 0 complete
- [ ] Single-player still works

### Changes

1. **`UnitData.cs` — Add new fields**:
   - `AttackType` (Physical or Magic)
   - `PhysicalDefense`, `MagicDefense`
   - `Evasion` (% chance to dodge, base stat, defaults to 0. Uses seeded RNG)
   - `GroupId` (nullable — units spawned by same card share this)
   - `LeaderId` (nullable — which unit is the group leader)
   - `MovementStyle` (MoveToward, Kite, FollowLeader)
   - `TargetingPriority` (NearestEnemy, SummonerPriority, LeaderTarget)
   - `RetreatCondition` (None, HpThreshold)
   - `KiteRange` (preferred distance from target, for kiting units)
   - `List<ActiveBuff> ActiveBuffs` — currently active stat modifiers
   - `List<TriggerConfig> Triggers` — trigger definitions (on-kill, on-hit, etc.)

2. **`SummonerData.cs` — Add position field**:
   - `SummonerPosition` (fixed position on battlefield — summoner is a static base)

3. **`SimDamage.cs` — Update damage formula**:
   - Accept `DamageType` parameter (physical or magic)
   - Check `Evasion` first — seeded RNG roll, if evaded emit `AttackEvadedEvent` and skip damage
   - Check `PhysicalDefense` or `MagicDefense` based on damage type
   - Process shield absorption (oldest shield consumed first) before HP deduction
   - Keep existing: crit, elemental matchups, summoner bonuses
   - **Pluggable formula architecture**: The defense reduction calculation is a swappable function. The exact curve (flat, percentage, hybrid) is tunable without rewriting the pipeline. Floor at minimum damage (e.g., 1)

4. **Create effect system types**:
   - `DamageType` enum (Physical, Magic)
   - `TriggerType` enum (OnAttack, OnHit, OnKill, OnDeath, OnDamaged, HpThreshold, Periodic, OnSpawn, LeaderDeath)
   - `EffectType` enum (DirectDamage, Heal, StatModifier, DamageOverTime, HealOverTime, Shield, Stun, Slow, AoE, ChargeBonusDamage)
   - `MovementStyle` enum (MoveToward, Kite, FollowLeader)
   - `TargetingPriority` enum (NearestEnemy, SummonerPriority, LeaderTarget)
   - `RetreatCondition` enum (None, HpThreshold)
   - `ActiveBuff` struct (stat, modifier, remaining ticks, source)
   - `TriggerConfig` struct (trigger type, effect type, parameters)

5. **Create `SimEffects.cs`** — Effect application logic:
   - `ApplyEffect(MatchState, UnitData source, Effect, targets)` — central effect dispatch
   - `TickBuffs(MatchState)` — decrement durations, remove expired, apply periodic effects
   - Called from `Simulation.Tick()` as part of the main loop

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] New fields compile and don't break existing behavior
- [ ] SimDamage handles damage type parameter (defaults to Physical for backward compat)
- [ ] Effect types and structures defined and usable

### Invariants
- [ ] No new MatchState writes outside Tick
- [ ] Existing combat behavior unchanged (backward-compatible additions)

### Files Modified
- `scripts/csharp/Battle/Simulation/UnitData.cs`
- `scripts/csharp/Battle/Simulation/SummonerData.cs`
- `scripts/csharp/Battle/Simulation/SimDamage.cs`
- New: `scripts/csharp/Battle/Simulation/SimEffects.cs`
- New: `scripts/csharp/Battle/Simulation/EffectTypes.cs` (enums and data structures)

---

## Phase 2: Command-Based Card Play

> **Goal**: Route all card plays through the command queue. Close the summoner mutation paths.

### Entry Criteria
- [ ] Phase 1 complete
- [ ] Data model has damage types, groups, buffs

### Changes

1. **`Simulation.cs` — `Tick()` processes `PlayCardCommand`**:
   - Dequeue commands from `State.PendingCommands`
   - Validate each (mana check, hand bounds, phase check — prep: summon only, battle: all types)
   - On valid: deduct mana, start cast timer, move card from hand to discard, emit events
   - On complete cast: spawn unit(s) in MatchState with formation, assign group IDs, emit `CastingCompletedEvent`

2. **`Simulation.cs` — Handle prep→battle transition**:
   - On phase change to Battle: refresh hand (discard current, draw 4 from deck)
   - Activate all inactive units
   - Emit `HandChangedEvent`, `PhaseChangedEvent`

3. **`SimulationNode.cs` — Add `SubmitCommand()`**:
   - Public method that adds a command to `State.PendingCommands`
   - This is the ONLY entry point for external code to affect gameplay

4. **`summoner.gd` — Replace direct writes with command submission**:
   - Remove direct calls to `DeductMana()`, `StartCasting()`, `SyncHandAfterCardPlay()`, `SyncCardDraw()`, `SyncDeckRecycle()`
   - Instead: `sim_node.SubmitCommand(PlayCardCommand)` (via a GDScript-friendly wrapper)
   - Summoner listens for events to update UI

5. **`SimulationNode.cs` — Remove/internalize mutation methods**:
   - Remove public: `DeductMana()`, `StartCasting()`, `SyncHandAfterCardPlay()`, `SyncCardDraw()`, `SyncDeckRecycle()`
   - These are now handled inside `Tick()`

6. **`Simulation.cs` — Handle deck management inside Tick**:
   - Card draw (replacement draw after card play)
   - Deck recycle (shuffle discard into deck when both hand + deck empty)
   - All using seeded RNG for deck shuffle

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Single-player: drag card → command submitted → sim processes → unit spawns
- [ ] Prep phase: only summon cards accepted, spells rejected
- [ ] Battle start: hand refreshes, all card types available
- [ ] Mana deduction happens inside Tick, not in summoner.gd
- [ ] Cast timer is managed by Tick, summoner.gd shows progress from events
- [ ] Hand/deck/discard state managed by Tick, UI updates from events
- [ ] No GDScript code writes mana, casting, or deck state to MatchState

### Invariants
- [ ] `grep -rn "DeductMana\|StartCasting\|SyncHand\|SyncCard\|SyncDeck" scripts/ --include="*.gd"` returns zero results
- [ ] All card state changes flow through `Tick()`

### Files Modified
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/core/summoner.gd`
- `scripts/csharp/Battle/Simulation/Commands/PlayCardCommand.cs`

---

## Phase 3: Read-Only Unit3D

> **Goal**: Make `Unit3D` a pure presentation node. Close all presentation → MatchState writes.

### Entry Criteria
- [ ] Phase 2 complete
- [ ] Card plays work through commands

### Changes

1. **`Unit3D._PhysicsProcess()` — Read-only sync**:
   - Read position from MatchState → update `GlobalPosition`
   - Read HP from MatchState → update health component
   - Read target from MatchState → face toward target
   - Read lifecycle from MatchState → trigger animations
   - **Remove**: `SyncPositionToMatchState()`, `SyncStatsToMatchState()`, any MatchState writes

2. **`Unit3D` — Remove damage write paths**:
   - Remove `OnTakeDamage()` writing to MatchState (damage comes from `SimDamage` inside Tick)
   - Remove `Heal()` writing to MatchState (healing comes from sim)
   - Remove `OnHealthDeath()` → `RemoveUnit()` (death comes from sim event)

3. **`Unit3D` — React to sim events**:
   - Connect to `UnitAttackedEvent` → play attack animation
   - Connect to `UnitDamagedEvent` → show damage number, health bar update
   - Connect to `UnitDiedSimEvent` → play death animation, then cleanup
   - Connect to `UnitRegisteredEvent` → initial setup

4. **`SimulationNode.cs` — Remove Unit mutation methods**:
   - Remove public: `ApplyUnitDamage()`, `SyncUnitPosition()`, `SyncUnitTarget()`, `ForceSetUnitHp()`, `RemoveUnit()` (the external one)

5. **Hitbox/Hurtbox system — Disable during Battle**:
   - During Battle phase, melee hitboxes and projectile Area3D collisions should NOT trigger damage
   - All combat is handled by `SimBehavior` → `SimDamage` inside Tick

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Units move correctly (positions from sim, visuals follow)
- [ ] Units attack and deal damage (all through sim, using phys/magic damage types)
- [ ] Units die when HP reaches 0 (death from sim event, not hitbox)
- [ ] No `Unit3D` code writes to MatchState during Battle

### Invariants
- [ ] `Unit3D` has no calls to SimulationNode mutation methods during Battle
- [ ] All damage goes through SimDamage with damage type

### Files Modified
- `scripts/csharp/Units/Unit3D.cs` (or `scripts/units/unit_3d.gd`)
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` (add Battle-phase guard or remove)

---

## Phase 4: Summoner Damage & Win Conditions

> **Goal**: Route summoner damage through the simulation. Implement flexible win conditions.

### Entry Criteria
- [ ] Phase 3 complete
- [ ] Unit combat works through sim

### Changes

1. **`SimBehavior.cs` — Handle summoner targeting in sim**:
   - When a unit has no enemy unit targets, target the enemy summoner's fixed position
   - When in range, apply damage to `SummonerData` inside Tick (using phys/magic damage types)

2. **`summoner.gd` — Remove hurtbox damage path**:
   - Remove the hitbox/hurtbox damage path
   - Summoner HP is now only written by Tick
   - Summoner reads HP from MatchState via signals

3. **`SimulationNode.cs` — Remove `ApplySummonerDamage()`**

4. **Win condition system in `Simulation.Tick()`**:
   - Evaluate configurable win condition predicates each tick
   - Support: destroy base (summoner HP ≤ 0), survive time, first blood, kill count, timed destroy
   - Win condition is set during match initialization
   - On win: set `GamePhase.GameOver`, `WinnerTeam`, emit `GameOverEvent`

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Units advance toward enemy summoner when no enemy units remain
- [ ] Summoner takes damage from units (via sim, not hitbox)
- [ ] Game ends when win condition is met (not just summoner HP check)
- [ ] Win condition is configurable per battle

### Invariants
- [ ] `ApplySummonerDamage` not called from GDScript
- [ ] Win condition determined by simulation predicates, not GDScript

### Files Modified
- `scripts/csharp/Battle/Simulation/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/SimDamage.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/core/summoner.gd`
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- New: `scripts/csharp/Battle/Simulation/WinCondition.cs` (predicate system)

---

## Phase 5: Abilities & Triggers

> **Goal**: Wire the effect system from Phase 1 into combat. Migrate abilities from presentation to simulation.

### Entry Criteria
- [ ] Phase 4 complete
- [ ] Effect types and data model from Phase 1 in place

### Changes

1. **`SimBehavior.cs` — Fire triggers at combat moments**:
   - After damage: fire `OnHit` triggers on attacker, `OnDamaged` triggers on defender
   - After kill: fire `OnKill` triggers on killer, `OnDeath` triggers on dying unit
   - After attack: fire `OnAttack` triggers
   - Each tick: check `HpThreshold`, tick `Periodic` triggers
   - On leader death: fire `LeaderDeath` triggers on all group members

2. **`SimEffects.cs` — Process triggered effects**:
   - Look up trigger config on the unit
   - Apply the configured effect (damage, heal, stat modifier, slow, etc.)
   - Add/remove buffs from `ActiveBuffs`
   - Emit events for VFX (buff applied, buff expired)

3. **`SimEffects.cs` — Tick buffs each frame**:
   - Decrement buff durations
   - Apply periodic effects (DoT, HoT)
   - Remove expired buffs, restore modified stats
   - **Shield stacking**: Multiple shields can coexist on a unit. When damage is absorbed, the oldest shield is consumed first. Shields are tracked as ordered buff entries in `ActiveBuffs`. Shield absorption is integrated into the `SimDamage` pipeline (after defense reduction, before HP deduction)

4. **`SimTargeting.cs` — Group-aware targeting**:
   - If unit has a `LeaderId`, target what the leader is targeting
   - Fallback to normal targeting if leader has no target or is dead

5. **Migrate current abilities**:
   - `ChargeAbility` → Track distance in `UnitData`, check on attack, apply bonus damage
   - `AuraAbility` → Periodic trigger, area check, apply damage/heal/buff effect
   - `DeathExplosionAbility` → OnDeath trigger, AoE damage effect
   - `SlowOnHitAbility` → OnHit trigger, slow stat modifier effect

6. **`Unit3D` — Remove trigger/ability processing**:
   - Remove `_PhysicsProcess()` trigger logic
   - React to buff/effect events for VFX only

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] On-kill heal works (unit heals after killing a target)
- [ ] On-hit slow works (target slowed after being hit)
- [ ] Aura damage/heal works (periodic area effect)
- [ ] Death explosion works (AoE on death)
- [ ] Charge bonus works (distance-based damage boost)
- [ ] Group targeting works (followers target leader's target)
- [ ] Buff durations expire correctly
- [ ] All effects are deterministic (no presentation-layer logic)

### Invariants
- [ ] All stat modifications happen inside Tick
- [ ] `Unit3D` has no direct UnitData writes
- [ ] Triggers fire deterministically based on sim events

### Files Modified
- `scripts/csharp/Battle/Simulation/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/SimEffects.cs`
- `scripts/csharp/Battle/Simulation/SimDamage.cs` (shield integration)
- `scripts/csharp/Battle/Simulation/SimTargeting.cs` (group targeting)
- `scripts/csharp/Battle/Simulation/UnitData.cs` (charge tracking fields)
- `scripts/csharp/Units/Unit3D.cs` (remove trigger processing)

---

## Phase 6: Spell Cards

> **Goal**: Support spell cards using the effect system from Phase 5.

### Entry Criteria
- [ ] Phase 5 complete (effect system working)

### Changes

1. **`PlayCardCommand` — Handle spell cards**:
   - Spell cards use the same command but with additional data (target position, target unit ID)
   - Validation: spell cards rejected during prep phase

2. **`Simulation.Tick()` — Process spell casting completion**:
   - On spell cast complete: apply spell's effect(s) using `SimEffects`
   - Spells use the same effect types as abilities (damage, heal, buff, debuff, AoE, etc.)
   - **Spell targeting mode** (per-spell, from definition):
     - **Position-based**: Player picks a point. AoE hits everything in radius. Single-target hits nearest valid unit to that point
     - **Unit-based**: Player picks a specific unit. Buff/heal applies to that unit. AoE centers on that unit
   - The `SpellTargetingMode` enum (Position, Unit) is part of the spell definition data
   - Target resolution logic in `SimEffects` branches on the targeting mode

3. **Spell definitions**:
   - Each spell card maps to one or more effects
   - Effect configuration: damage type, amount, radius, duration, targeting mode, etc.
   - Data-driven — no per-spell simulation code

### Acceptance Criteria
- [ ] Spell cards work in simulation
- [ ] Damage spells deal the correct damage type (physical or magic)
- [ ] Buff/debuff spells apply effects through the effect system
- [ ] Spells only available during battle phase
- [ ] VFX plays on spell cast

### Files Modified
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/csharp/Battle/Simulation/SimEffects.cs` (spell-specific targeting)
- `scripts/csharp/Battle/Simulation/Commands/PlayCardCommand.cs` (spell data)

---

## Phase 7: Wire Multiplayer

> **Goal**: Host broadcasts events + snapshots. Client receives and renders. Two players can play a match.

### Entry Criteria
- [ ] Phases 0-4 complete (ideally 0-6)
- [ ] Single-player battle is fully functional through the simulation
- [ ] No external MatchState writes during Battle (verified by mutation audit)

### Changes

1. **`HostRunner.cs` — Broadcast simulation events**:
   - After `Tick()`, serialize events and send to client
   - Key events: `UnitSpawned`, `UnitDied`, `UnitDamaged`, `SummonerHpChanged`, `PhaseChanged`, `GameOver`, `BuffApplied`, `BuffExpired`
   - Continue building/sending periodic state snapshots

2. **`HostRunner.cs` — Receive and validate client commands**:
   - Receive `PlayCardCommand` from client
   - Remap coordinates (canonical ↔ local)
   - Add to command queue for next Tick

3. **`ClientRunner.cs` — Apply events from host**:
   - Receive events → create/destroy/update presentation nodes
   - Handle all event types including buff/effect events

4. **`ClientRunner.cs` — Apply snapshots from host**:
   - Periodic full state correction (including buff states, group relationships)

5. **`ClientRunner.cs` — Send commands to host**:
   - When local player plays a card, create `PlayCardCommand`, remap coordinates, send to host

6. **Team remapping**:
   - All events/snapshots use network team indices (0=host, 1=client)
   - Client remaps to local indices

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] Two players can connect (host + client)
- [ ] Both can play cards and see units appear on both screens
- [ ] Combat plays out identically on both screens
- [ ] Win/loss appears correctly for both players
- [ ] State hash checks pass (no persistent desyncs)

### Invariants
- [ ] Client never calls `Simulation.Tick()`
- [ ] All client state comes from events + snapshots
- [ ] Coordinate remapping applied to all network messages

### Files Modified
- `scripts/csharp/Multiplayer/Authority/HostRunner.cs`
- `scripts/csharp/Multiplayer/Client/ClientRunner.cs`
- `scripts/csharp/Multiplayer/Sync/MessageSerializer.cs`
- `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs` (buff/group state in snapshots)

---

## Phase 8: Dead Code Removal & Polish

> **Goal**: Clean up all legacy code paths that are no longer used.

### Entry Criteria
- [ ] All previous phases complete
- [ ] Full game working in single-player and multiplayer

### Changes

1. **Remove unused SimulationNode mutation methods** — Any remaining public write methods
2. **Remove legacy DamageSystem hitbox combat** — The sim handles all combat now
3. **Remove legacy `SyncPositionToMatchState()`** — Positions come from sim
4. **Remove presentation-layer ability scripts** — `ChargeAbility.cs`, `AuraAbility.cs`, etc. (now in sim)
5. **Remove unused imports and dead code**
6. **Update technical documentation**

### Acceptance Criteria
- [ ] `dotnet build` passes
- [ ] No dead code (methods that are never called)
- [ ] No `// TODO` comments related to the rewrite
- [ ] Technical docs updated

### Files Modified
- Various (cleanup pass)

---

## Phase Summary

| Phase | Goal | Key Metric |
|-------|------|-----------|
| **Pre-work** | Branch setup, cherry-pick sim files | Build passes |
| **Phase 0** | Host-only Tick | Client doesn't run Tick |
| **Phase 1** | Data model foundation (damage types, groups, effects) | New fields compile, effect types defined |
| **Phase 2** | Command-based card play + prep→battle transition | No direct mana/deck writes from GDScript |
| **Phase 3** | Read-only Unit3D | No Unit3D → MatchState writes during Battle |
| **Phase 4** | Summoner damage + flexible win conditions | Win condition from configurable predicates |
| **Phase 5** | Abilities & triggers in simulation | All triggers fire deterministically in Tick |
| **Phase 6** | Spell cards via effect system | Spells work using shared effect system |
| **Phase 7** | Wire multiplayer | Two players can complete a match |
| **Phase 8** | Dead code removal | Clean codebase |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| **Phase 1 data model may need iteration** | Design types to be extensible. Use lists/enums rather than fixed fields where possible |
| **Phase 2 is complex** (card play has many moving parts) | Break into sub-tasks: mana → casting → hand management → draw → recycle → prep→battle |
| **Phase 3 may break visual sync** (Unit3D reads from MatchState) | Add interpolation for smooth visuals between tick updates |
| **Phase 5 effect system complexity** | Migrate one ability at a time, test each independently |
| **Cherry-pick conflicts** | Resolve carefully, prefer the simulation version for sim files |
| **Single-player regression** | Test after every phase — single-player must always work |

---

*Last updated: 2026-02-27*
*Status: All phases complete*
