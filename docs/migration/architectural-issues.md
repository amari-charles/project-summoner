# Architectural Issues

Structural issues identified during the `feature/host-authoritative-sim` branch review. These are systemic patterns — parallel systems, leaky abstractions, asymmetric codepaths, and inconsistent conventions — not individual bugs.

**Resolution status** is tracked per-issue. See `docs/architecture/target-architecture.md` for the full resolution map.

---

## Category 1: Parallel Systems (Same Job, Two Implementations)

- [ ] **#1 — Duplicate Damage Systems**
  - Godot-side: `DamageSystem.cs` (837 lines) — crit, evasion, elemental matchups, summoner bonuses
  - Sim-side: `SimDamage.cs` — identical damage calculation in pure sim layer
  - Both compute damage independently; results can diverge if either is updated without the other
  - **Resolution:** `SimDamage` is the source of truth. `DamageSystem.cs` blocked on deletion until UnitVisual replaces Unit3D.

- [ ] **#2 — Duplicate Modifier/Buff Systems**
  - Godot-side: `ModifierService` (`scripts/csharp/Systems/Modifiers/ModifierService.cs`)
  - Sim-side: `SimEffects.cs` + `ActiveBuff` tracked in `MatchState`
  - Two buff-tracking systems with no synchronization between them
  - **Resolution:** `SimEffects` is the source of truth. `ModifierService` blocked on deletion until UnitVisual replaces Unit3D.

- [ ] **#3 — Duplicate Projectile Systems**
  - Godot-side: `ProjectileService` (`scripts/csharp/Projectiles/ProjectileService.cs`) — scene tree nodes
  - Sim-side: `SimProjectile.cs` — pure sim, tick-based movement
  - Projectile logic runs in two places with no shared source of truth
  - **Resolution:** `SimProjectile` is the source of truth. `ProjectileService` blocked on deletion until ProjectileVisual replaces Projectile3D.

- [x] **#4 — Duplicate Ability Systems** ✅ Resolved
  - ~~Godot-side: `BaseAbility.cs` / `SlowOnHitAbility.cs`~~ **Deleted** — was dead code
  - Sim-side: `TriggerConfig` system is the only ability implementation
  - **Resolution:** `BaseAbility.cs`, `SlowOnHitAbility.cs`, `IAbilityConfig.cs` deleted. Wiring removed from Unit3D, UnitDefinition, UnitDefinitions, UnitSpawner, and `fire_spider_3d.tscn`.

---

## Category 2: Leaky Abstractions

- [ ] **#5 — NetworkId bleeds into simulation data model**
  - `UnitData.cs:13` has `NetworkId` field (default -1)
  - `UnitData.cs:120` has `TargetNetworkId` field (marked "legacy — kept for snapshot compatibility")
  - Core sim files (`SimBehavior`, `SimDamage`, `SimTargeting`, etc.) never read these fields — only `SimulationNode`, `StateSnapshotBuilder`, and `ClientRunner` use them
  - Networking metadata is embedded in a pure simulation struct
  - **Resolution:** `IdentityMap` stub exists at session boundary. UnitData fields remain until IdentityMap is implemented. See `docs/architecture/gameplay/session/README.md` for migration path.

- [ ] **#6 — DesyncDetector reads Godot scene tree**
  - `DesyncDetector.cs:189-256` — `ApplyStateCorrections()` reaches into the view layer
  - `DesyncDetector.cs:220` — `sceneTree.GetNodesInGroup("summoners")`
  - `DesyncDetector.cs:227` — `summoner.Get("team")` (duck-typed, no interface)
  - `DesyncDetector.cs:239` — `summoner.Get("current_hp")` (duck-typed, no interface)
  - Sync/correction code should read `MatchState`, not the scene tree
  - **Resolution:** Addressed when `ClientSession` replaces `ClientRunner`. `DesyncChecker` reads `MatchState` only.

- [ ] **#7 — SimulationNode is a god class**
  - `SimulationNode.cs` — 942 lines with too many responsibilities:
    - Owns `MatchState` and runs ticks
    - Card data population (`SimulationNode.cs:298-314`)
    - Snapshot application for client mode (`SimulationNode.cs:768-920`)
    - Unit ID management (`SimulationNode.cs:45-62` — `_claimedSimUnitIds`, `_unit3DBySimId`, `_nextClientUnitId`)
    - Team/coordinate remapping
    - Event emission
  - **Resolution:** Splits into `Simulation` + `IGameSession` + `BattleScene`/`EntityManager`. Stubs exist. Transition plan documented in `docs/architecture/gameplay/simulation/README.md`.

---

## Category 3: Asymmetric Host/Client Patterns

- [ ] **#8 — Frame counter never updated on client**
  - Host: `HostRunner.cs:79` sets `_session.CurrentFrame = SimulationNode.Current.State.FrameNumber` every frame
  - Client: `ClientRunner.cs` never sets `_session.CurrentFrame`
  - Client hash reports (`ClientRunner.cs:427`) send `CurrentFrame` which stays at 0
  - Host-side desync comparison likely skips these reports due to `MaxFrameLagTolerance` — desync detection is non-functional for clients
  - **Resolution:** `IGameSession.Tick()` standardizes frame advancement. Session stubs exist. ✅ Addressed by design.

- [ ] **#9 — Host bypasses RequestValidator**
  - Host: `HostRunner.cs:116-130` — `RequestCardPlay()` calls `SimulationNode.Current.SubmitCommand()` directly at line 127
  - Client: requests go through `RequestValidator` before reaching `SubmitCommand()` (at `HostRunner.cs:198-216`)
  - Host can play invalid cards; validation is client-only
  - **Resolution:** `CommandRouter` validates ALL commands regardless of session type. Stub exists. ✅ Addressed by design.

- [ ] **#10 — Different update mechanisms**
  - Host: event-driven via `OnTickCompleted` signals
  - Client: polling via `SimulationNode.ApplySnapshot()` each frame (`ClientRunner.cs:249`)
  - Structurally different codepaths for the same game state updates
  - **Resolution:** `IGameSession` unifies both poll (`GetState()`) and push (`SimEventsEmitted`). ✅ Addressed by design.

- [ ] **#11 — ClientRunner.HandleMessage has no default case**
  - `ClientRunner.cs:110-152` — `switch (message)` with 9 case branches
  - No `default` case — unrecognized message types are silently dropped
  - Future protocol messages will fail invisibly
  - **Resolution:** Addressed when `ClientSession` replaces `ClientRunner`.

---

## Category 4: SP vs MP Code Path Divergence

- [ ] **#12 — Three distinct game modes with different init paths**
  - SP: `SimulationNode.Initialize()` directly
  - MP Host: `HostRunner.Initialize()` → `SimulationNode`
  - MP Client: `ClientRunner.Initialize()` → `SimulationNode` with `IsClientMode=true` (`ClientRunner.cs:67-75`)
  - Hardcoded branching, not polymorphic — each path configures SimulationNode differently
  - **Resolution:** Three `IGameSession` implementations (Local, Host, Client) replace hardcoded branching. Stubs exist. ✅ Addressed by design.

- [ ] **#13 — Signal vs polling split**
  - SP & Host: react to sim events via `OnTickCompleted` delegate
  - Client: polls `MatchState` via snapshot application (`SimulationNode.cs:768-920`)
  - Same game, fundamentally different update models
  - **Resolution:** `IGameSession.SimEventsEmitted` provides a unified event interface. ✅ Addressed by design.

- [ ] **#14 — Card play routing divergence**
  - SP: `SimulationNode.SubmitCommand()` directly
  - Host: `HostRunner.RequestCardPlay()` → `SubmitCommand()` directly (line 127)
  - Client: `MatchSession.RequestCardPlay()` → network → host validates → `SubmitCommand()`
  - Three paths to the same action
  - **Resolution:** `IGameSession.SubmitCommand()` is the single entry point. ✅ Addressed by design.

---

## Category 5: Inconsistent Conventions

- [ ] **#15 — Singleton naming inconsistency**
  - `.Current` pattern: `SimulationNode.Current` (`SimulationNode.cs:31`), `MatchSession.Current` (`MatchSession.cs:20`)
  - `.Instance` pattern: `ReconnectionHandler.Instance`, `DamageSystem.Instance`, `ModifierService.Instance`, `ProjectileService.Instance`, `CardFactory.Instance`, and ~20 other services
  - Two naming conventions for the same singleton pattern, no clear rule for which to use
  - **Resolution:** Session layer uses constructor injection. Statics eliminated as session implementations take over. ✅ Addressed by design.

- [ ] **#16 — Four dependency lookup patterns**
  - (a) Static singletons: `SimulationNode.Current`, `DamageSystem.Instance`
  - (b) Constructor injection: `StateSnapshotBuilder(session)`
  - (c) Scene tree queries: `GetNodesInGroup("summoners")`
  - (d) Duck-typed `Get()` calls: `summoner.Get("team")` (`DesyncDetector.cs:227`)
  - No consistent dependency injection strategy
  - **Resolution:** Session layer uses constructor injection consistently. ✅ Addressed by design.

- [ ] **#17 — Team representation chaos**
  - `int` — bare integers used in most places
  - `LocalTeam` struct — exists in `scripts/csharp/Multiplayer/Core/TeamIndex.cs`
  - `NetworkTeam` struct — exists in `scripts/csharp/Multiplayer/Core/TeamIndex.cs`
  - GDScript `team` property — duck-typed access via `Get("team")`
  - Four ways to represent the same concept. `LocalTeam`/`NetworkTeam` exist but bare ints still dominate.
  - **Resolution:** `Team` value type stub created in `scripts/csharp/Simulation/Team.cs`. Migration happens incrementally as callers are touched. Session remaps at network boundary.

- [ ] **#18 — Four ID systems for units**
  - `UnitId` — sim-internal identity (`UnitData.cs`)
  - `NetworkId` — multiplayer identity (`UnitData.cs:13`)
  - `InstanceId` — Godot object identity
  - Scene node names — string-based identity
  - No unified identity system. `FindUnitIdByNetworkId()` (`ClientRunner.cs:380-389`) is an O(n) linear scan over `state.Units`.
  - **Resolution:** `UnitId` in simulation, `IdentityMap` bimap at session layer for O(1) translation. Stub exists. ✅ Addressed by design.

- [x] **#19 — State constants inconsistency** ✅ Resolved (partial)
  - `ActivationState` — proper enum in `scripts/csharp/Units/Enums.cs:43-47`
  - Sim-side mirror: `ActivationInactive`/`ActivationActive` as `const int` in `SimConstants.cs:13-14`
  - ~~Behavior states: `const int NoTarget = 0`, `Chasing = 1`, `InRange = 2`, `Attacking = 3` in `SimBehavior.cs:20-23`~~ **Now `BehaviorState` enum** in `scripts/csharp/Simulation/BehaviorState.cs`
  - Movement states: `const int MoveNone = 0`, `MoveForward = 1`, etc. in `SimBehavior.cs:48-51` — remain as const ints (movement result, not a state machine)
  - `GamePhase` — proper enum in `GamePhase.cs`
  - **Resolution:** `BehaviorState` enum created. `UnitData.BehaviorState` now uses the enum type. `SimBehavior` const ints are aliases of the enum. `MovementState` enum created for future use.

---

## Category 6: Dead/Vestigial Code

- [x] **#20 — AuthorityProvider signals are dead** ✅ Resolved
  - ~~`authority_provider.gd:12` — `signal action_confirmed(action: RefCounted)`~~
  - ~~`authority_provider.gd:16` — `signal action_rejected(action: RefCounted, reason: String)`~~
  - ~~`authority_provider.gd:19` — `signal state_update_received(state_data: Dictionary)`~~
  - **Resolution:** Dead signal declarations removed from `authority_provider.gd`.

- [ ] **#21 — Prediction stubs**
  - `ClientRunner.cs:359-364` — `ApplyPrediction()` logs only, no mana deduction. Comment: "Full implementation deferred"
  - `ClientRunner.cs:366-371` — `RollbackPrediction()` logs only, no mana restoration. Comment: "Full implementation deferred"
  - Called from the card play path but do nothing
  - **Resolution:** Addressed when `ClientSession` replaces `ClientRunner`. Prediction will be designed as part of the ClientSession implementation.

- [x] **#22 — Godot ability system is disconnected** ✅ Resolved
  - ~~`SlowOnHitAbility.cs:72` — `target.HasMethod("apply_modifier")` check~~
  - ~~`SlowOnHitAbility.cs:74` — `target.Call("apply_modifier", ...)` if method exists~~
  - ~~`apply_modifier` does not exist on `Unit3D` or any GDScript in the project~~
  - ~~`HasMethod` check silently returns false — entire `BaseAbility` hierarchy is unreachable dead code~~
  - **Resolution:** `BaseAbility.cs`, `SlowOnHitAbility.cs`, `IAbilityConfig.cs` deleted. Wiring removed from Unit3D, UnitDefinition, UnitDefinitions, UnitSpawner, `fire_spider_3d.tscn`.

---

## Category 7: Mixed Concerns (Game Logic Entangled with View)

- [ ] **#23 — Unit3D mixes combat logic with visual rendering**
  - 2304 lines with no separation between game logic and view code
  - Game logic: targeting (`UpdateTargeting()` lines 1506-1524), behavior state machine (`UpdateBehavior()` lines 1526-1606), attack cooldowns (`UpdateCooldowns()` lines 1479-1504), trigger system (lines 1019-1196), damage application (`TakeDamage()` line 1265)
  - View code: shadow creation (lines 564-584), visual component setup (lines 557-561), animation updates (`UpdateAnimation()` line 1702), debug visualization (`_Process()` lines 1982-2034), HP bar management (lines 1833-1853)
  - Target: Unit3D becomes a pure visual shell positioned by GameView. All combat logic lives in simulation subsystems.
  - **Resolution:** `UnitVisual` stub exists in `scripts/csharp/View/UnitVisual.cs`. ✅ Addressed by design.

- [ ] **#24 — Projectile3D mixes hit detection with VFX**
  - 1128 lines coupling collision/damage with visual effects
  - Game logic: ground collision (lines 189-198), homing target tracking (lines 235-246), direct hit detection (lines 263-274), damage dealing via `HitResolver` (`HitTarget()` lines 510-553), pierce logic (lines 555-574), AoE damage (lines 603-643)
  - View code: visual scene instantiation (lines 157-161), material duplication (line 161), impact VFX via VFXManager (lines 580-601), fade-out tweens (`ExpireWithFade()` lines 649-705), particle management (lines 1055-1087)
  - Target: SimProjectile handles movement/collision/damage in the sim layer. ProjectileView is a visual shell that reads ProjectileState from MatchState.
  - **Resolution:** `ProjectileVisual` stub exists in `scripts/csharp/View/ProjectileVisual.cs`. ✅ Addressed by design.

- [ ] **#25 — GameController3D mixes game state with UI orchestration**
  - 1048 lines coupling game flow with visual setup
  - Game logic: simulation initialization (lines 171-185), phase transitions (lines 324-366, 503-537), win condition setup and evaluation (lines 825-871), kill count objective tracking (lines 874-918), game start/end state (lines 390-466)
  - View code: UI panel initialization (lines 925-949), redirect input raycasting (lines 676-734), unit tinting for selection feedback (lines 792-818), game-over label display (lines 550-562), audio management (lines 99-103)
  - Target: game flow logic moves into Session layer. GameController3D becomes a thin orchestrator that wires Input and View to IGameSession.
  - **Resolution:** `BattleScene` stub exists in `scripts/csharp/View/BattleScene.cs`. ✅ Addressed by design.

