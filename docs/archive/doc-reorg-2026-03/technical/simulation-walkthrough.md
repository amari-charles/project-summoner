# Architecture Walkthrough — Simulation Rewrite

> **Purpose**: Human-readable companion to `architecture-diagram.md`. Read this like a tour guide — it traces through actual classes, methods, and properties at each step of gameplay. The exhaustive reference stays in `architecture-diagram.md`; this is the "what happens when...?" doc.
>
> **Status**: Draft — 2026-02-22

---

## 1. The Big Picture

```
  ┌─────────────────────────────────────────────────────────────────────┐
  │                         INPUT LAYER                                 │
  │   InputCollector.cs (player drag)  ·  AI Opponent  ·  ClientSession │
  │                                                                     │
  │   Rule: All input becomes a PlayCardCommand — no direct state       │
  │         mutation allowed                                            │
  └────────────────────────────┬────────────────────────────────────────┘
                               │  ICommand
                               ▼
  ┌─────────────────────────────────────────────────────────────────────┐
  │                    SIMULATION LAYER  (pure C#, no Godot)            │
  │                                                                     │
  │   Simulation.Tick(fixedDelta) → processes commands, runs combat,    │
  │   moves units, ticks projectiles → returns List<SimEvent>           │
  │                                                                     │
  │   MatchState is the SINGLE SOURCE OF TRUTH                          │
  │                                                                     │
  │   Rule: ONLY simulation code writes to MatchState                   │
  │   (Tick for progression, ClientSession.ApplySnapshot for correction)│
  └──────────────┬──────────────────────────────────────┬───────────────┘
                 │  SimEvent list                       │
                 ▼                                      │
  ┌──────────────────────────────────┐   ┌──────────────┴──────────────┐
  │      SESSION LAYER               │   │    PRESENTATION LAYER       │
  │                                  │   │       (READ-ONLY)           │
  │  HostSession: drives Tick(),     │   │  SimulationNode: converts   │
  │    broadcasts snapshots/events   │   │    SimEvents -> Godot       │
  │                                  │   │    SimEvents → Godot        │
  │  ClientSession: sends commands,  │   │  UnitVisual / HUD /         │
  │    never runs deterministic tick,│   │    read MatchState,         │
  │    applies authoritative         │   │    react to signals         │
  │    snapshots from host.          │   │                             │
  │                                  │   │  Rule: NEVER write to       │
  │  Rule: Host is authoritative     │   │    MatchState               │
  │                                  │   │                             │
  │  Rule: Host is authoritative     │   │                             │
  └──────────────────────────────────┘   └─────────────────────────────┘
```

### The 6 Invariants

1. **Single writer (simulation subsystem)**: Only simulation code mutates `MatchState`: `Simulation.Tick(fixedDelta)` during normal progression, plus an explicit correction path (`ClientSession.ApplySnapshot(snapshot)`) on clients. Presentation/GDScript never writes to `MatchState`.
2. **Commands in, events out**: Player input becomes `ICommand`, simulation produces `SimEvent`s.
3. **Deterministic**: Same seed + same commands in same order = identical state within the same runtime/platform. Uses `DeterministicRng` (Xorshift32). Simulation runs at a **fixed timestep** (`fixedDelta`). Godot accumulates frame delta and calls `Tick(fixedDelta)` zero or more times per frame — never with a variable delta. Note: the sim uses floats; cross-machine drift is possible, so multiplayer relies on periodic snapshots as the authoritative source of truth to correct drift. This is a **server-authoritative event replication** architecture, not lockstep — determinism exists for replays, debugging, and predictable single-player behavior, not for multiplayer state agreement.
4. **Host authority**: Host calls `Tick(fixedDelta)`. Client never calls `Tick(fixedDelta)` — it receives events and periodic snapshots.
5. **Presentation is read-only**: GDScript reads `MatchState` through `SimulationNode` accessors. Writes flow through `SubmitCommand()`.
6. **Deterministic command ordering**: `PlayCardCommand` includes `IssuedFrame` (client-stamped, used for latency telemetry/debug — not used in ordering) and `Sequence` (per-player monotonic starting at 1). Host stamps `ExecuteFrame` on each command. Commands are processed in `(ExecuteFrame, Team, Sequence)` order — `Team` is a stable tie-breaker (0=host, 1=client), and `Sequence` breaks ties within the same team and frame. This guarantees identical replay from the same command log.

> **Coordinate system**: All sim positions are **canonical** (host perspective). `CoordinateTransform` converts at the network boundary for all position-bearing commands and messages (summon placement, spell targets, unit positions, projectile origins).

> **Godot nodes are derived views**: Godot nodes (`UnitVisual`, HP bars, etc.) are derived views keyed by `UnitId`/`NetworkId` — they are never the source of truth.

### Tick Order Contract

Each `Simulation.Tick(fixedDelta)` runs its systems in this fixed order. All flows in this document reference these steps — if a flow says "TickCasting processes the command", that happens at step 4 below.

1. **Increment frame** — `FrameNumber++`, `MatchTime += fixedDelta` (Battle only). Commands due are evaluated against this post-increment `FrameNumber`.
2. **Drain and execute due commands** — drain `PendingCommandBuffer` where `ExecuteFrame <= FrameNumber`, sort by `(ExecuteFrame, Team, Sequence)`, execute in order. Note: commands execute under the *current* phase — if Preparation expires this frame, commands scheduled for this frame still execute under Preparation (phase change happens at step 3).
3. **Phase timers / transitions** — decrement `PrepTimeRemaining`, transition to Battle if expired (activate units, refresh hands). Phase acts as a gate: casting, units, and projectiles run under the phase that is current *after* this step. Implementation detail: delegates to `TickPreparationTimers()` when `Phase == Preparation`; no-op during Battle.
4. **Tick casting** — decrement cast timers, handle completions (spawn units / apply spells), replacement draws
5. **Tick units** — per alive active unit: cooldowns → targeting → behavior → movement → delayed ranged resolution (`TickPendingDamage`)
6. **Tick projectiles** — `SimProjectile.TickAll()`: advance, hit detect, apply damage, cleanup dead
7. **Tick effects** — `SimEffects.TickBuffs()`: decrement durations, apply periodic (DoT/HoT), remove expired, fire periodic triggers
8. **Tick delayed effects** — `SimEffects.TickDelayedEffects()`: process due ability timers (death explosions, etc.)
9. **Death cleanup** — decrement `DeathCleanupTimer` on dead units, remove expired, emit `UnitRemovedEvent`
10. **Evaluate win conditions** — `IWinCondition.Evaluate(state)` → if met, set `Phase = GameOver`, emit `GameOverEvent`
11. **Return events** — collected `List<SimEvent>` returned to caller

---

## 2. Gameplay Flows

### Flow 1: Match Starts

**Step 1**: `BattleScene._Ready()` creates a `SimulationNode` child.
  → `SimulationNode._Ready()` sets `Current = this`, `ProcessPriority = -100`

**Step 2**: `BattleScene` calls `SimulationNode.Initialize(prepDuration, matchDuration, winCondition)`.
  → Creates fresh `MatchState` with `Phase = Preparation`, `PrepTimeRemaining = prepDuration`
  → Creates `DeterministicRng` from `MatchSession.Current.Seed` (multiplayer) or timestamp (single-player)
  → Creates `Simulation(state)` instance

**Step 3**: Summoner data is registered for each team.
  → `MatchState.Summoners[0]` and `MatchState.Summoners[1]` get populated with HP, mana, cast speed, element, deck, hand

**Step 4**: `SimulationNode._PhysicsProcess(delta)` runs an accumulator loop.
  → Accumulates `_accumulator += delta` each Godot physics frame
  → While `_accumulator >= fixedDelta`: calls `Simulation.Tick(fixedDelta)`, subtracts `fixedDelta` from accumulator
  → This means 0 or more sim ticks per Godot frame — always at the same step size
  → First tick: `MatchState.FrameNumber` becomes 1, phase is `Preparation`

---

### Flow 2: Preparation Phase

**Step 1**: Each fixed tick, `Simulation.Tick(fixedDelta)` runs the full Tick Order Contract.
  → At step 3, `Phase == Preparation` delegates to `TickPreparationTimers()`

**Step 2**: `TickPreparationTimers()` decrements the timer.
  → `_state.PrepTimeRemaining -= fixedDelta`
  → Emits: `PrepTimerUpdatedEvent(remaining)`

**Step 3**: Player drags a **summon card** to the battlefield during prep.
  → `InputCollector` calls `SimulationNode.SubmitCommand(PlayCardCommand)`
  → `PlayCardCommand` is enqueued into `MatchState.PendingCommandBuffer`

**Step 4**: Casting begins (processed by `TickCasting()`).
  → `SummonerData.IsCasting = true`, `CastingTimeRemaining = castTime / CastSpeed`
  → `SummonerData.Mana -= cost`
  → Emits: `CastingStartedEvent`, `SummonerManaChangedEvent`

**Step 5**: Cast completes — units spawn as **inactive** (won't fight until battle).
  → `SummonerData.IsCasting = false`
  → Units added to `MatchState.Units` with `ActivationState = Inactive`
  → Emits: `CastingCompletedEvent`, `UnitRegisteredEvent`

**Step 6**: Timer reaches zero.
  → `_state.Phase = GamePhase.Battle`
  → Emits: `PhaseChangedEvent(Battle)`

---

### Flow 3: Battle Starts

**Step 1**: Phase transitions to `Battle` inside `TickPreparation(fixedDelta, events)`.
  → `_state.Phase = GamePhase.Battle`
  → Emits: `PhaseChangedEvent(Battle)`

**Step 2**: `TickPreparation(fixedDelta, events)` activates all units on phase change.
  → Calls `ActivateAllUnits()` — iterates `MatchState.Units`, sets `ActivationState = Active` for every inactive unit
  → Emits: `UnitActivationChangedEvent(unitId, Active)` per unit
  → Units now appear in `MatchState.GetAliveActiveUnits()` queries

**Step 3**: `RefreshHands()` — sim discards current hand and draws fresh cards.
  → For each summoner: moves current hand to `DiscardPile`, draws `MaxHandSize` cards from `Deck`
  → Emits: `HandChangedEvent(team, newHand)` per summoner (always carries the full hand array)

**Step 4**: Next tick enters Battle. All subsequent ticks follow the Tick Order Contract (see above).
  → `MatchTime` advances at step 1, casting/units/projectiles run at steps 4–6, win conditions at step 10

---

### Flow 4: Player Plays a Summon Card

**Step 1**: Player drags card from hand to battlefield.
  → `InputCollector` performs local sanity check (mana, hand index)

**Step 2**: `InputCollector` calls `AuthorityBridge.RequestCardPlay(cardIndex, position, summonerNode)`.
  → In single-player: `LocalAuthority.RequestCardPlay()` validates and executes immediately
  → In multiplayer host: `HostAuthority.RequestCardPlay()` validates, executes, broadcasts
  → In multiplayer client: `ClientAuthority.RequestCardPlay()` sends `CardPlayRequest` to host

**Step 3**: Command enters simulation.
  → `PlayCardCommand(team, cardIndex, spawnPosition, networkId, sequence, issuedFrame)` enqueued into `MatchState.PendingCommandBuffer`
  → Host stamps `ExecuteFrame = FrameNumber + 1` on the command, where `FrameNumber` is the host sim's current frame at receipt time. This ensures commands never execute mid-frame and deterministic ordering is preserved. This introduces a minimum 1-tick input latency for all players (including the host); presentation can mask it with local ghost placement
  → Commands are sorted and processed in `(ExecuteFrame, Team, Sequence)` order for determinism

**Step 4**: `TickCasting()` processes the command.
  → Deducts mana: `summoner.Mana -= cost`
  → Starts cast timer: `summoner.IsCasting = true`, `summoner.CastingTimeRemaining = castTime / CastSpeed`
  → Emits: `SummonerManaChangedEvent`, `CastingStartedEvent`

**Step 5**: Cast timer counts down each tick.
  → `summoner.CastingTimeRemaining -= fixedDelta`

**Step 6**: Cast completes (`CastingTimeRemaining <= 0`).
  → `summoner.IsCasting = false`
  → Emits: `CastingCompletedEvent(team, cardIndex, spawnPosition, networkId)`

**Step 7**: Sim registers unit, presentation spawns visual.
  → On `CastingCompletedEvent`, **sim** creates `UnitData` (from card catalog + spawn position) and adds to `MatchState.Units[unitId]`
  → Emits: `UnitRegisteredEvent(unitId, networkId, catalogId, team, position)`
  → `SimulationNode` converts `UnitRegisteredEvent` to a Godot signal
  → Presentation layer listens, instantiates `UnitVisual` scene at `spawnPosition`

**Step 8**: Replacement draw — card moves to discard, new card drawn from deck.
  → `SummonerData.Hand[cardIndex]` replaced with top of `SummonerData.Deck`
  → Played card appended to `SummonerData.DiscardPile`
  → Emits: `CardDrawnEvent`, `HandChangedEvent`

---

### Flow 5: Player Plays a Spell Card

**Step 1–6**: Same as summon card (drag → command → validate → deduct mana → cast timer).

**Step 7**: Cast completes — instead of spawning units, applies spell effects.
  → Resolve targets based on `SpellTargetingMode` (Position or Unit)
  → `SimEffects.ApplyEffect()` applies DirectDamage / Heal / Buff / etc.
  → Emits: `CastingCompletedEvent` plus effect-specific events (e.g., `UnitDamagedEvent`, `BuffAppliedEvent`)

**Step 8**: Replacement draw (same as summon card).

---

### Flow 6: A Unit's Frame (Single Tick)

What happens to **one unit** during `TickUnits()`:

**Step 1**: `SimBehavior.TickCooldowns(unit, fixedDelta)` — decrement timers.
  → `unit.AttackCooldown -= fixedDelta`
  → `unit.TargetLockTimer -= fixedDelta`
  → `unit.ForcedTargetTimer -= fixedDelta` (clears forced target when expired)
  → `unit.AttackAnimationTimer -= fixedDelta`

**Step 2**: `SimBehavior.TickTargeting(unit, state)` — find a target.
  → If `unit.ForcedTargetUnitId` is set and valid, use it
  → If target lock expired or current target dead: `SimTargeting.AcquireTarget(unit, state)`
  → AcquireTarget iterates enemy units, applies layer filter + aggro radius + cone reachability
  → Scores by distance (closer = better) + health (lower HP = better)
  → Falls back to enemy summoner (`MatchState.GetSummonerTargetId()`) if no enemy units alive
  → Sets `unit.TargetUnitId` to best match

**Step 3**: `SimBehavior.TickBehavior(unit, state, fixedDelta, events)` — decide what to do.
  → Resolves target position via `ResolveTargetPosition()` (works for both units and summoners)
  → Computes XZ distance to target
  → **Out of range** → state = `Chasing`, returns `MoveTowardTarget`
  → **In range, cone OK, cooldown ready** → state = `Attacking`, applies damage (see Flow 7)
  → **In range, cone NOT OK** → fallback movement (strafe/idle/move toward)
  → **In range, cooldown not ready** → state = `InRange`, returns `MoveNone`

**Step 4**: `SimMovement.Tick(unit, result, state, fixedDelta)` — move the unit.
  → Reads `BehaviorResult.Movement` to decide: forward / toward target / strafe / none
  → Updates `unit.Position` and `unit.Velocity`

**Step 5**: `SimBehavior.TickPendingDamage(unit, state, fixedDelta, events)` — resolve delayed ranged outcomes.
  → Decrements `unit.PendingDamageTimer`
  → When timer expires:
    - unit target: spawns authoritative `SimProjectileData` (canonical ranged path)
    - summoner target: applies delayed direct summoner damage

> **Ranged attacks — projectiles vs delayed fields**: Unit-vs-unit ranged attacks use `SimProjectileData` in `MatchState.Projectiles` (see Flow 10). `PendingDamage*` fields are a windup buffer before projectile spawn (or delayed summoner damage), not the final unit-damage mechanism.

---

### Flow 7: An Attack Lands (Damage Pipeline)

**Step 1**: `SimBehavior.TickBehavior()` decides to attack (cooldown ready, in range, constraint OK).
  → Sets `unit.BehaviorState = Attacking`
  → Emits: `UnitAttackedEvent(attackerUnitId, targetUnitId)`

**Step 2**: Branch by unit type:
  → **Melee** (`UnitType == 0`): applies immediate unit damage via `SimDamage.Calculate()` path.
  → **Ranged** (`UnitType == 1`):
    - if `ProjectileDelay > 0`: starts delayed windup via `PendingDamage*`, then spawns projectile in `TickPendingDamage`
    - if no delay: spawns projectile immediately
  → Projectile hits later resolve damage in `SimProjectile.ApplyHit()` (see Flow 10).

**Step 3**: `SimDamage.Calculate(baseDamage, attacker, target, attackerSummoner, targetSummoner, rng)` (called by melee pending damage or projectile hit):
  → a. **Crit check**: `rng.NextFloat() < attacker.CritChance` → `damage *= attacker.CritDamage`
  → b. **Elemental matchup**: `ElementMatchups.GetMultiplier(attackerElement, targetElement)` → `damage *= multiplier`
  → c. **Summoner damage bonus**: `damage *= 1 + attackerSummoner.DamageBonus / 100`
  → d. **Per-element bonus**: `damage *= 1 + attackerSummoner.GetElementalDamageBonus(element) / 100`
  → e. **Summoner damage reduction**: `damage = max(damage - targetSummoner.DamageReduction, 0)`
  → f. **Round**: `damage = round(damage * 10) / 10` (1 decimal place)
  → Returns: `(float damage, bool isCrit)`

**Step 4**: Apply to target (on melee timer expire or projectile hit).
  → `target.CurrentHp -= damage`
  → Emits: `UnitDamagedEvent(targetUnitId, attackerUnitId, damage, isCrit)`

**Step 5**: Death check.
  → If `target.CurrentHp <= 0` → see Flow 8

**Step 6**: Reset attacker cooldown.
  → `unit.AttackCooldown = 1.0 / unit.AttackSpeed`

---

### Flow 8: A Unit Dies

**Step 1**: HP drops to zero inside damage application.
  → `target.CurrentHp = 0`
  → `target.IsAlive = false`
  → `state.KillCount++`
  → Emits: `UnitDiedSimEvent(unitId, killerUnitId)`
  → Subsequent ticks skip this unit — `GetAliveActiveUnits()` filters by `IsAlive == true`

**Step 2**: `SimulationNode` converts the event to a Godot signal.
  → `UnitDiedSim` signal with `(unitId, killerUnitId)`
  → Presentation layer starts death animation on `UnitDiedSim`

**Step 3**: Sim-owned cleanup timer ticks down.
  → On death, sim sets `unit.DeathCleanupTimer = DeathCleanupSeconds` (e.g., 2.0)
  → Unit stays in `MatchState.Units` with `IsAlive = false` — presentation can still read state (position, catalogId, team) during death animation
  → Each tick, sim decrements: `unit.DeathCleanupTimer -= fixedDelta`
  → When `DeathCleanupTimer <= 0`, sim proceeds to Step 4

**Step 4**: Cleanup timer expires — sim removes unit.
  → Sim removes unit from `MatchState.Units`
  → Emits: `UnitRemovedEvent(unitId)`
  → `SimulationNode` converts to `UnitRemoved` Godot signal
  → Presentation cleans up: unregisters from `SimSpatialGrid`, removes HP bar, destroys visual node

---

### Flow 9: An Ability Fires (e.g., Death Explosion)

**Step 1**: Unit dies (Flow 8 triggers).
  → `UnitDiedSimEvent` emitted by sim

**Step 2**: Sim enqueues a delayed effect.
  → On `UnitDiedSimEvent`, if the dying unit has a death ability, sim adds an entry to the effect queue (e.g., `UnitData.PendingAbilityTimer` or a dedicated `MatchState.DelayedEffects` list)
  → Entry records: effect type, origin position, delay timer, parameters (radius, damage)

**Step 3**: `Simulation.Tick()` processes due effects.
  → `SimEffects.TickDelayedEffects(state, fixedDelta, events)` decrements timers each tick
  → When timer expires, resolves the effect:
  → a. Iterates `MatchState.Units` within `ExplosionRadius` of origin position
  → b. For each target: `SimDamage.Calculate(baseDamage, ...)` — same pipeline as Flow 7 Step 3
  → c. `target.CurrentHp -= damage`
  → d. Death check per target (may trigger further Flow 8 chains)

**Step 4**: Sim emits events.
  → Emits: `AbilityTriggeredEvent(abilityType, position, radius)` — for VFX/audio
  → Emits: `UnitDamagedEvent(targetUnitId, sourceUnitId, damage, isCrit)` per target hit

**Step 5**: Presentation plays VFX/audio (read-only).
  → `SimulationNode` converts `AbilityTriggeredEvent` to Godot signal
  → Presentation spawns explosion VFX at origin position, plays audio

---

### Flow 10: A Projectile's Lifecycle

**Step 1**: Ranged unit attacks — `SimProjectile.Spawn()` called.
  → Allocates `SimProjectileData` with `state.NextProjectileId()`
  → Initializes movement-specific fields (path length, velocity, weaving params)
  → Added to `state.Projectiles[id]`

**Step 2**: Each tick, `SimProjectile.TickAll(state, fixedDelta, events)` processes all projectiles.
  → Saves `proj.LastPosition = proj.CurrentPosition`
  → Advances `proj.TimeAlive += fixedDelta`

**Step 3**: Movement advances based on `MovementType`:
  → **Straight** (`TickStraight`): `progress += (speed * fixedDelta) / pathLength`, lerp position
  → **Arc** (`TickArc`): quadratic Bezier with control point at midpoint + arcHeight
  → **Ballistic** (`TickBallistic`): `y = y0 + v0*t - 0.5*g*t^2`, horizontal velocity * time
  → **WeavingHoming** (`TickWeavingHoming`): 3 phases — Straight → Veer (random L/R) → Homing (steer toward target)

**Step 4**: Hit detection — `CheckHits()`.
  → For each alive enemy unit: compute `PointToSegmentDistance(unit.Position, proj.LastPosition, proj.CurrentPosition)`
  → If `distance <= proj.HitRadius` → hit
  → Note: This is an O(P·U) brute-force loop. Sufficient for current unit/projectile counts. If it becomes a bottleneck, replace with a deterministic spatial grid (must be sim-owned, not the presentation-layer `SpatialGrid`). Any future spatial optimization must preserve deterministic iteration order (stable sorting by UnitId) — non-deterministic container iteration (e.g., HashSet) will break replay determinism.

**Step 5**: `ApplyHit()` on hit.
  → Attacker resolved from `proj.SourceUnitId` (stored at spawn time)
  → `SimDamage.Calculate(proj.Damage, attacker, target, ...)` for damage
  → `target.CurrentHp -= damage`
  → `proj.PierceRemaining--`
  → Emits: `ProjectileHitSimEvent(proj.ProjectileId, target.UnitId, hitPosition)`
  → Emits: `UnitDamagedEvent(targetUnitId, attackerUnitId = proj.SourceUnitId, damage, isCrit)`
  → If `PierceRemaining <= 0` → `proj.IsDead = true`

**Step 6**: AoE on impact (if `proj.AoeRadius > 0`).
  → `ApplyAoE()` hits all enemies within radius
  → Each target gets full damage calculation

**Step 7**: Path completion (progress >= 1.0).
  → Direct hit check against target at endpoint
  → AoE if configured
  → `proj.IsDead = true`

**Step 8**: Cleanup.
  → Dead projectiles removed from `state.Projectiles` at end of `TickAll()`

---

### Flow 11: Deck Runs Out

**Step 1**: Card play or replacement draw tries to draw from `SummonerData.Deck`.
  → `Deck.Count == 0`

**Step 2**: Recycle discard pile into deck.
  → `SummonerData.Deck = shuffle(SummonerData.DiscardPile)` using `DeterministicRng`
  → `SummonerData.DiscardPile.Clear()`
  → Emits: `DeckRecycledEvent(team)`

**Step 3**: Draw from refreshed deck.
  → Normal draw resumes from shuffled deck

---

### Flow 12: Game Ends

**Step 1**: Summoner HP reaches zero (most common).
  → Inside `SimBehavior.ApplyDamageToSummoner()` or `TickPendingDamage()`
  → `summoner.CurrentHp = 0`, `summoner.IsAlive = false`

**Step 2**: Game over event emitted.
  → `_state.Phase = GamePhase.GameOver` (set by win condition evaluation)
  → Emits: `GameOverEvent(winnerTeam, "Summoner destroyed")`

**Step 3**: `SimulationNode` converts to Godot signal.
  → `GameOver` signal with `(winnerTeam, reason)`

**Step 4**: Presentation shows results.
  → HUD shows win/loss screen
  → Units stop fighting (subsequent ticks are no-ops in `GameOver` phase)

**Step 5**: In multiplayer, host broadcasts `MatchEnded`.
  → `MatchSession.BroadcastMatchEnd(winnerIndex, reason)`

---

### Flow 13: Multiplayer — Client Plays a Card

**Step 1**: Client player drags card.
  → `InputCollector` calls into `SimulationNode.PlayCard(...)`.
  → `SimulationNode` remaps team/perspective and converts local position to canonical before creating `PlayCardCommand`.

**Step 2**: `ClientSession.SubmitCommand()` handles.
  → Builds `CardPlayRequest(sequence, playerIndex, cardIndex, canonicalPosition, timestamp)`.
  → Sends request through `IMatchTransport` to host.
  → Client does not run local deterministic sim prediction.

**Step 3**: Host receives `CardPlayRequest`.
  → `HostSession.HandleMessage()` resolves authoritative team from sender identity.
  → Validates via `CommandRouter.Validate(...)`.
  → Queues accepted command for next simulation frame.

**Step 4**: Host tick executes.
  → `Simulation.Tick()` processes command.
  → Summoner/card state and unit spawning happen in authoritative `MatchState`.
  → Host emits gameplay events and broadcasts periodic `StateSnapshot` (10Hz).

**Step 5**: Client applies authoritative snapshot.
  → `ClientSession.ApplySnapshot(StateSnapshot)` overwrites frame/phase/time + summoners + units + projectiles.
  → Entities absent from snapshot are removed locally.
  → `EntityManager` spawns/despawns shells from state diff and interpolates unit render positions.

> **Events vs snapshots precedence**:
> - Snapshots are authoritative state (`FrameNumber`, `Phase`, `MatchTime`, `Summoners[]`, `Units[]`, `Projectiles[]`).
> - On snapshot apply: overwrite all listed entity state. Entities present in `MatchState.Units` but absent from `snapshot.Units` are removed (they died on the host). Entities in the snapshot but not locally present are created. UX tradeoff: snapshot corrections may cause units to disappear without a local death animation if the client missed prior `UnitDied` events.
> - **Projectiles are replicated** in `StateSnapshot.Projectiles[]`. On snapshot apply, clients upsert active projectile state and remove projectiles missing from the snapshot. `EntityManager` spawns/despawns `ProjectileVisual` shells from this authoritative list.
> - **RNG is host-only**. Clients do not hold or advance `DeterministicRng` state. All RNG-dependent outcomes (crit, evasion, shuffle) are resolved by the host and communicated via events/snapshots.
> - Current transport expects ordered/reliable delivery from the match transport implementation; no explicit `ServerEventId` reordering layer exists in current session code.

> **Hand/deck replication**: Hand/deck/discard are authoritative from host snapshots (`StateSnapshot.Summoners[]`). Client does not simulate deck progression locally.

---

## 3. Data At a Glance

### MatchState

| Field | Type | Description |
|-------|------|-------------|
| `FrameNumber` | `long` | Monotonically increasing tick counter |
| `MatchTime` | `float` | Elapsed battle time in seconds (only advances in Battle phase) |
| `Phase` | `GamePhase` | `Preparation`, `Battle`, or `GameOver` |
| `PrepTimeRemaining` | `float` | Seconds left in prep phase |
| `IsOvertime` | `bool` | Whether overtime rules are active |
| `WinnerTeam` | `int?` | Team index of winner (null if ongoing) |
| `KillCount` | `int` | Total units killed (for kill-count win conditions) |
| `Summoners` | `SummonerData[2]` | Index 0 = team 0 (host), index 1 = team 1 (client) |
| `Units` | `Dictionary<int, UnitData>` | All units, keyed by MatchState-local unit ID. Simulation loops must iterate in stable order (sort by key) for determinism — C# `Dictionary` iteration order is not spec-guaranteed across runtime versions |
| `Projectiles` | `Dictionary<int, SimProjectileData>` | All active projectiles. Same iteration-order rule as `Units` |
| `PendingCommandBuffer` | `List<ICommand>` | Commands waiting to be processed. Each tick, sim drains due commands (where `ExecuteFrame <= currentFrame`), sorts by `(ExecuteFrame, Team, Sequence)`, executes in order, and discards. After executing due commands, the buffer retains only commands with `ExecuteFrame > currentFrame` |
| `Rng` | `DeterministicRng` | Seeded RNG — same seed = same results |

### SummonerData

| Category | Field | Type | Description |
|----------|-------|------|-------------|
| Identity | `Team` | `int` | 0 or 1 |
| Identity | `Position` | `Vector3` | Fixed position on battlefield |
| Identity | `ElementId` | `int` | Elemental affinity |
| Health | `CurrentHp` | `float` | Current health |
| Health | `MaxHp` | `float` | Maximum health |
| Health | `IsAlive` | `bool` | False when HP <= 0 |
| Mana | `Mana` | `float` | Current mana |
| Mana | `MaxMana` | `float` | Maximum mana |
| Stats | `CastSpeed` | `float` | Casting speed multiplier (default 1.0) |
| Stats | `DamageBonus` | `float` | % bonus to all friendly unit damage |
| Stats | `DamageReduction` | `float` | Flat damage reduction for friendly summoner |
| Casting | `IsCasting` | `bool` | Currently casting a card |
| Casting | `CastingTimeRemaining` | `float` | Seconds until cast completes |
| Casting | `CastingTimeTotal` | `float` | Original cast duration |
| Casting | `CastingCardIndex` | `int` | Hand index being cast (-1 = none) |
| Casting | `CastingSpawnPosition` | `Vector3` | Where units will spawn |
| Casting | `CastingNetworkId` | `int` | Pre-assigned network ID (-1 = none) |
| Deck | `Deck` | `List<string>` | Draw pile (card catalog IDs) |
| Deck | `Hand` | `List<string>` | Current hand (up to `MaxHandSize`) |
| Deck | `DiscardPile` | `List<string>` | Played/discarded cards |
| Deck | `MaxHandSize` | `int` | Hand size limit (default 4) |

### UnitData

| Category | Field | Type | Description |
|----------|-------|------|-------------|
| Identity | `UnitId` | `int` | MatchState-local unique ID |
| Identity | `NetworkId` | `int` | Multiplayer ID (-1 = unassigned) |
| Identity | `CatalogId` | `string` | Card catalog reference |
| Identity | `Team` | `int` | 0 or 1 |
| Core Stats | `CurrentHp` / `MaxHp` | `float` | Health |
| Core Stats | `AttackDamage` | `float` | Base damage per hit |
| Core Stats | `AttackSpeed` | `float` | Attacks per second |
| Core Stats | `MoveSpeed` | `float` | Movement speed |
| Core Stats | `AttackRange` | `float` | Attack reach distance |
| Core Stats | `AggroRadius` | `float` | Target acquisition range (default 20) |
| Core Stats | `CritChance` / `CritDamage` | `float` | Crit probability and multiplier (default 1.5x) |
| Type | `UnitType` | `int` | 0 = Melee, 1 = Ranged |
| Type | `MovementLayer` | `int` | 0 = Ground, 1 = Air |
| Type | `ElementId` | `int` | Elemental affinity |
| Targeting | `TargetUnitId` | `int?` | Current target (null = none) |
| Targeting | `TargetLockTimer` | `float` | Seconds before re-evaluating target |
| Targeting | `ForcedTargetUnitId` | `int?` | Override target (from abilities) |
| Targeting | `ForcedTargetTimer` | `float` | Duration of forced target |
| Targeting | `HasConeConstraint` | `bool` | Requires facing check |
| Targeting | `ConeHalfAngle` | `float` | Cone width in degrees (default 30) |
| Targeting | `TargetLayerFilter` | `int` | 0=Ground, 1=Air, 2=Both |
| Targeting | `DistanceScorerWeight` | `float` | Weight for distance scoring (default 1) |
| Targeting | `HealthScorerWeight` | `float` | Weight for HP-based scoring |
| Movement | `Position` | `Vector3` | World position |
| Movement | `Velocity` | `Vector3` | Current velocity vector |
| Movement | `IsFacingRight` | `bool` | Facing direction |
| Movement | `FallbackMovement` | `int` | 0=MoveToward, 1=Strafe, 2=Idle |
| Movement | `FlightAltitude` | `float` | Y offset for flying units |
| Combat | `AttackCooldown` | `float` | Seconds until next attack |
| Combat | `AttackAnimationTimer` | `float` | Remaining attack animation time |
| Combat | `BehaviorState` | `int` | 0=NoTarget, 1=Chasing, 2=InRange, 3=Attacking |
| Pending Dmg | `PendingDamageTimer` | `float` | Delayed ranged windup timer before resolving a pending outcome |
| Pending Dmg | `PendingDamageTargetId` | `int?` | Pending delayed target (unit for projectile spawn, or summoner target ID) |
| Pending Dmg | `PendingDamageAmount` | `float` | Base damage payload used when the delayed outcome resolves |
| Lifecycle | `IsAlive` | `bool` | False when HP <= 0 |
| Lifecycle | `DeathCleanupTimer` | `float` | Seconds remaining before dead unit is removed from MatchState. Set on death, ticked by sim. Gives presentation time to animate |
| Lifecycle | `ActivationState` | `int` | 0=Inactive (prep), 1=Active (battle) |

### SimProjectileData

| Category | Field | Type | Description |
|----------|-------|------|-------------|
| Identity | `ProjectileId` | `int` | Unique ID in MatchState |
| Identity | `SourceUnitId` | `int` | Who fired it |
| Identity | `TargetUnitId` | `int` | Intended target |
| Identity | `Team` | `int` | Friendly fire check |
| Damage | `Damage` | `float` | Base damage on hit |
| Damage | `SourceElementId` | `int` | For elemental matchup |
| Movement | `MovementType` | `int` | 0=Straight, 1=Arc, 2=Ballistic, 3=WeavingHoming |
| Movement | `Speed` | `float` | Travel speed |
| Movement | `Progress` | `float` | 0.0 to 1.0 along path (path-based types) |
| Movement | `PathLength` | `float` | Total path distance |
| Position | `StartPosition` | `Vector3` | Where it spawned |
| Position | `TargetPosition` | `Vector3` | Where it's heading |
| Position | `CurrentPosition` | `Vector3` | Current location |
| Position | `LastPosition` | `Vector3` | Previous tick location (for line-segment hit detection) |
| Position | `Direction` | `Vector3` | Current facing |
| Hit | `HitRadius` | `float` | How close to register a hit (default 2.5) |
| Hit | `PierceRemaining` | `int` | Hits left before dying |
| Hit | `AoeRadius` | `float` | Splash radius (0 = single target) |
| Lifecycle | `TimeAlive` | `float` | Seconds since spawn |
| Lifecycle | `Lifetime` | `float` | Max lifetime before auto-expire (default 5s) |
| Lifecycle | `IsDead` | `bool` | Marked for removal |
| Arc | `ArcHeight` | `float` | Peak height of arc |
| Ballistic | `Gravity` | `float` | Downward acceleration (default 9.8) |
| Ballistic | `HorizontalVelocity` | `Vector3` | XZ velocity component |
| Ballistic | `InitialVerticalVelocity` | `float` | Launch Y velocity |
| Ballistic | `TotalTime` | `float` | Predicted flight time |
| Weaving | `WeavingPhase` | `int` | 0=Straight, 1=Veering, 2=Homing |
| Weaving | `PhaseTimer` | `float` | Time in current phase |
| Weaving | `VeerDirection` | `Vector3` | Random L/R veer vector |
| Weaving | `SteerStrength` | `float` | Degrees/second turn rate (default 180) |
| Weaving | `ScaledVeerDelay` | `float` | Straight phase duration (distance-scaled) |
| Weaving | `ScaledVeerDuration` | `float` | Veer phase duration (distance-scaled) |

---

## 4. Quick Reference

### All SimEvents

| Event | When It Fires | Who Listens |
|-------|--------------|-------------|
| `PhaseChangedEvent` | Prep → Battle, Battle → GameOver | HUD (phase indicator), all systems |
| `PrepTimerUpdatedEvent` | Every tick during Preparation | HUD (countdown timer) |
| `MatchTimeUpdatedEvent` | Every tick during Battle | HUD (match clock) |
| `SummonerHpChangedEvent` | Summoner takes damage | HUD (HP bar), game over check |
| `SummonerManaChangedEvent` | Mana deducted for card play | HUD (mana display), hand UI |
| `CastingStartedEvent` | Card play begins casting | SummonerVisual (cast bar) |
| `CastingCompletedEvent` | Cast timer reaches zero | SummonerVisual (spawn unit / apply spell) |
| `CardDrawnEvent` | New card drawn into hand slot | Hand UI (card display) |
| `HandChangedEvent` | Hand contents change (draw, refresh) — always carries full hand array | Hand UI (full refresh) |
| `DeckRecycledEvent` | Discard pile shuffled back into deck | HUD (deck counter), audio |
| `UnitActivationChangedEvent` | Unit activation state changes (Inactive → Active) | Presentation (enable unit visuals) |
| `AbilityTriggeredEvent` | Sim-owned ability fires (e.g., death explosion) | Presentation (VFX, audio) |
| `UnitRegisteredEvent` | New unit added to MatchState | Presentation (create UnitVisual node) |
| `UnitRemovedEvent` | Unit removed from MatchState | Presentation (cleanup UnitVisual) |
| `UnitAttackedEvent` | Unit begins an attack | UnitVisual (play attack animation) |
| `UnitDamagedEvent` | Unit takes damage | UnitVisual (flash, HP bar update), floating damage numbers |
| `UnitDiedEvent` | Unit HP reaches zero | UnitVisual (death anim), abilities (death triggers) |
| `ProjectileHitEvent` | Projectile hits a unit | ProjectileVisual (impact VFX) |
| `GameOverEvent` | Win condition met | HUD (result screen), MatchSession (broadcast) |

### All Commands

| Command | Fields | When Valid |
|---------|--------|------------|
| `PlayCardCommand` | `Team: int`, `CardIndex: int`, `SpawnPosition: Vector3`, `NetworkId: int`, `Sequence: int`, `IssuedFrame: long` (client-stamped), `ExecuteFrame: long` (host-stamped) | Preparation (summons only) or Battle (all cards). Must have enough mana, valid hand index, valid spawn zone. Host stamps `ExecuteFrame` on receipt; commands processed in `(ExecuteFrame, Team, Sequence)` order. |
| `ForfeitCommand` | `Team: int` | Any phase before GameOver |

### Enums

**GamePhase**: `Preparation (0)`, `Battle (1)`, `GameOver (2)`

**Element**: `Neutral`, `Fire`, `Water`, `Wind`, `Earth`, `Lightning`, `Shadow`, `Poison`, `Life`, `Death`, `Occultist`, `Holy`, `Ice`, `Metal`, `Spirit`

**MovementType** (SimProjectileData constants): `Straight (0)`, `Homing (1)`, `Arc (2)`, `Ballistic (3)`, `WeavingHoming (4)`

**WeavingPhase** (SimProjectileData constants): `PhaseStraight (0)`, `PhaseVeering (1)`, `PhaseHoming (2)`

**BehaviorState** (SimBehavior constants): `NoTarget (0)`, `Chasing (1)`, `InRange (2)`, `Attacking (3)`

**FallbackMovement** (SimBehavior constants): `MoveToward (0)`, `Strafe (1)`, `Idle (2)`

**TargetLayerFilter** (SimTargeting): `GroundOnly (0)`, `AirOnly (1)`, `Both (2+)`

**CardPlayStatus**: `Queued`, `Confirmed`, `Rejected`

> **SimEvents vs network messages**: `SimEvent`s are the simulation's internal output — `Simulation.Tick()` returns a `List<SimEvent>`. `HostSession` may convert selected sim outcomes into protocol messages for clients (for example `MatchEnded` and `SummonerDamageFlash`) while snapshots remain the authoritative correction channel. `SimulationNode` also forwards local sim events to presentation. The mapping is not strictly 1:1 and can vary by message type.

### Network Messages (Client → Host)

| Message | Key Fields |
|---------|-----------|
| `CardPlayRequest` | `Sequence`, `PlayerIndex`, `CardIndex`, `Position` (canonical), `ClientTimestamp` |
| `ForfeitRequest` | `PlayerIndex` |
| `StateHashReport` | `Frame`, `Hash` — hash must be built from a deterministic serialization of `MatchState` (entities sorted by UnitId/ProjectileId, floats rounded to fixed precision). RNG state is excluded (host-only). |
| `PlayerReady` | `PlayerIndex` |

### Network Messages (Host → Client)

| Message | Key Fields |
|---------|-----------|
| `CardPlayConfirmed` | `Sequence`, `PlayerIndex`, `CardIndex`, `Position`, `SpawnedUnitNetworkId` |
| `CardPlayRejected` | `Sequence`, `PlayerIndex`, `Reason` |
| `StateSnapshot` | `Frame`, `MatchTime`, `Phase`, `PrepTimeRemaining`, `Summoners[]`, `Units[]`, `Projectiles[]`, `StateHash`, `IsOvertime` |
| `UnitSpawned` | `NetworkId`, `UnitType`, `Team`, `Position` (canonical), `SourceSequence`, `SourcePlayerIndex` |
| `UnitDied` | `NetworkId`, `KillerNetworkId` |
| `DamageDealt` | `TargetNetworkId`, `Amount`, `IsCrit`, `SourceNetworkId` |
| `SummonerDamaged` | `Team`, `Amount`, `NewHp` |
| `SummonerDamageFlash` | `Team`, `Damage`, `AttackerUnitId` |
| `SummonerDestroyed` | `Team`, `KillerUnitId` |
| `MatchEnded` | `WinnerIndex`, `Reason`, `Duration` |

---

*Last updated: 2026-02-26*
*Companion to: architecture-diagram.md (exhaustive AI reference)*
