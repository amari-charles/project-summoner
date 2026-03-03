# Architecture Decisions

Settled decisions and open questions from architecture discussions. Each decision includes the reasoning — not just the conclusion — so future contributors understand *why*.

---

## Settled Decisions

### 1. Layered Architecture (Simulation / Session / Input+View)

**Decision:** Three layers with dependencies flowing strictly downward. Simulation is pure C# with zero Godot imports. Session orchestrates. Input and View are independent peers above Session.

**Reasoning:** The core problem was parallel systems — Godot-side services duplicating sim logic (issues #1–4). A strict layer boundary makes duplication structurally impossible: game logic can only live in Simulation, and the View can only read results. The Session layer exists because singleplayer and multiplayer need different orchestration (local tick vs network snapshot) but the same simulation.

**Reference:** [`target-architecture.md`](target-architecture.md) §1–§3

### 2. Documentation Practice

**Decision:** Document architecture decisions, component designs, and technical systems in `docs/`. Keep docs in sync with code changes. Every PR updates relevant docs.

**Reasoning:** Architecture decisions made in conversation disappear. Code comments capture *what* but not *why*. A central docs directory gives contributors the reasoning behind structural choices without archaeology.

**Reference:** `CLAUDE.md` → Documentation Updates section

### 3. Role-Based Naming (No `3D` Suffix)

**Decision:** View components are named by role, not Godot node type. `UnitVisual` not `Unit3D`. `EntityManager` not `GameView`. `BattleCamera` not `CameraController3D`.

**Reasoning:** This is a 2.5D game — the `3D` suffix reflects Godot plumbing, not the game's visual paradigm. Role-based names describe what components DO, making the architecture legible without engine knowledge. Consistent naming also makes grep/search reliable.

**Reference:** [`gameplay/view/README.md`](gameplay/view/README.md) → Naming Convention

### 4. Hybrid Data Model (Pull Continuous + Push Discrete)

**Decision:** Visual shells pull their own continuous state (position, HP) each frame from `IGameSession.GetState()`. EntityManager pushes discrete events (damage, death) by routing SimEvents to the correct shell.

**Reasoning:** Polling is natural for continuous state — you always want the latest position, not a stream of "moved to X" events. But polling for discrete events ("did anything die?") every frame is wasteful and easy to miss. The hybrid gives each data type the update pattern that fits it.

**Reference:** [`target-architecture.md`](target-architecture.md) §4, [`gameplay/view/README.md`](gameplay/view/README.md) → Data Model

### 5. Shell Pattern for Battlefield Entities

**Decision:** Every 3D battlefield entity (unit, projectile, summoner) is a "visual shell" — a passive `Node3D` that reads its own state from MatchState each frame and exposes reaction methods for EntityManager to call on events.

**Reasoning:** The shell pattern enforces the View-layer contract: read-only, no game logic, self-syncing. Each shell is responsible for its own continuous state (position, HP bar) but receives discrete events (damage flash, death) from EntityManager. This prevents shells from subscribing to events directly (which caused the N*M filter problem) and keeps lifecycle management centralized.

**Reference:** [`gameplay/view/battlefield/unit-visual.md`](gameplay/view/battlefield/unit-visual.md), [`gameplay/view/battlefield/projectile-visual.md`](gameplay/view/battlefield/projectile-visual.md), [`gameplay/view/battlefield/summoner-visual.md`](gameplay/view/battlefield/summoner-visual.md)

### 6. BattleHUD Independence

**Decision:** BattleHUD is an independent peer of EntityManager, not a child of it. Both read `IGameSession` but have no dependency on each other.

**Reasoning:** EntityManager owns the 3D battlefield. BattleHUD is a 2D overlay. They have different coordinate spaces, different lifecycles, and different concerns. Making BattleHUD independent means either can change without affecting the other, and it's possible to have a battle without HUD (replay viewer) or HUD without 3D (spectator mode).

**Reference:** [`gameplay/view/battle-hud.md`](gameplay/view/battle-hud.md), [`gameplay/view/README.md`](gameplay/view/README.md) → Component Map

### 7. SummonerVisual: Registered Shell

**Decision:** SummonerVisual follows the same shell pattern as UnitVisual (self-syncs from MatchState, receives events from EntityManager) but is *registered* at battle init rather than dynamically spawned.

**Reasoning:** Summoners are always present for the entire battle — they aren't created or destroyed mid-fight like units and projectiles. Spawning them dynamically would add unnecessary lifecycle complexity. However, they still use the shell pattern (not a special case) because consistency matters: one pattern for all 3D battlefield entities means one mental model, one set of invariants, and one event dispatch path. The only difference is when the shell is created (init vs mid-battle).

**Reference:** [`gameplay/view/battlefield/summoner-visual.md`](gameplay/view/battlefield/summoner-visual.md), [`gameplay/view/battlefield/`](gameplay/view/battlefield/)

### 8. View Layer Invariants

**Decision:** Eight rules that all View-layer code must follow. See [`gameplay/view/README.md`](gameplay/view/README.md) → Invariants for the full list.

**Reasoning:** Invariants are the mechanism for enforcing layer boundaries without a compiler. They're checkable in code review and make violations obvious. The specific set was derived from the problems in the current codebase: mixed concerns (#23, #24, #25), host/client branching (#8–#11), direct event subscriptions (N*M problem), and inconsistent naming.

**Reference:** [`gameplay/view/README.md`](gameplay/view/README.md) → Invariants, [`target-architecture.md`](target-architecture.md) → View invariants

### 9. Targeting Visuals: Input State + View Renders

**Decision:** Input layer owns the targeting state machine (what's selected, cursor position, valid targets). View layer reads that targeting state and renders the 3D visuals (range indicators, reticles, highlights). A shared targeting-state contract bridges the two.

**Reasoning:** Targeting is fundamentally a gesture lifecycle (drag start → move → confirm/cancel), which is Input's domain. But the visuals are 3D world-space objects, which is View's domain. Splitting state ownership from rendering respects both layer boundaries. The shared contract is small — just a data structure describing current targeting state.

**Affects:** `SpellTargetingManager` (retired — split between InputCollector and View), `RedirectManager` (gesture → InputCollector), `SummonPreview` (reads targeting state from Input), `BattlefieldDropZone` (absorbed into InputCollector).

### 10. AudioManager: Standalone Service

**Decision:** AudioManager lives outside the layer model as a standalone service, callable by any layer — like logging.

**Reasoning:** Audio is triggered from too many places to belong to one layer: battle SFX (View), UI clicks (HUD), music (ambient/phase-driven), spatial summoning chants (View). Making it standalone avoids forcing audio routing through a single layer. Any code that needs to play a sound calls AudioManager directly.

**Affects:** `scripts/services/audio_manager.gd` stays as an autoload, classified as a standalone service rather than belonging to any layer.

### 11. Unit-Type-Specific Logic: Composition

**Decision:** Unit-type behavior uses composition — data-driven strategies assembled by config. New unit types are created via configuration, not new classes.

**Reasoning:** Aligns with the existing `TriggerConfig` system in the simulation layer. Flexible and data-driven — designers can create new unit types by combining attack strategies, movement behaviors, and trigger configs without writing code. Avoids deep inheritance hierarchies and diamond problems. The sim layer already uses this pattern for abilities; extending it to all unit behavior is natural.

**Affects:** Both Simulation layer (behavior strategies, attack configs) and View layer (visual components selected by config). No per-unit subclasses like `FireSpider : SimUnit` — all differences are config-driven.

### 12. AI System: Input Layer Peer

**Decision:** AI is an Input-layer peer — it reads `IGameSession.GetState()` and calls `SubmitCommand()`, exactly like human input does.

**Reasoning:** Clean symmetry: human Input and AI Input are interchangeable consumers of `IGameSession`. AI can be tested in isolation against any session type. No special AI mode in Session — it's just another command source. Easy to support AI vs AI, human vs AI, or mixed scenarios without session logic changes.

**Affects:** `scripts/ai/` files become Input-layer peers. `LocalSession` doesn't need an AI mode — AI submits commands externally. `simple_ai.gd` follows the same pattern.

### 13. BattleContext: Typed BattleConfig Passed to Session

**Decision:** `BattleContext` autoload builds a typed `BattleConfig` data object that is passed to session constructors. Sessions receive everything they need at construction time.

**Reasoning:** Session should own its configuration after init, not reach into a global autoload mid-battle. A typed config object (`BattleConfig`) codifies the contract — decks, summoners, biome, game mode — and is passed to `LocalSession(config)`, `HostSession(config)`, etc. The `BattleContext` autoload remains as the builder/holder during scene transition, but sessions don't depend on it after construction.

**Affects:** `scripts/core/battle_context.gd` stays as autoload but becomes a `BattleConfig` builder. Session constructors take `BattleConfig` as a required parameter.

### 14. CardFactory: Cross-Cutting Utility (Base Stats Only)

**Decision:** CardFactory stays as a cross-cutting utility that creates cards with base stats from definitions. Stat modifications (player upgrades, card levels, traits, summoner bonuses, items) are applied separately during session initialization.

**Reasoning:** Clean separation: CardFactory does one thing (definition → card instance). The modifier pipeline is a separate concern owned by session init, where player-specific data is available. This keeps CardFactory pure and testable — it doesn't need to know about player profiles or progression systems.

**Affects:** `scripts/csharp/Cards/CardFactory.cs` stays as autoload, cross-cutting layer. Stat modification pipeline is a session-init concern (designed in Phase 5).

### 15. GameStateEvents: Keep for Non-Battle, Revisit in Phase 6

**Decision:** Battle events flow through `IGameSession.SimEventsEmitted`. `GameStateEvents` stays for meta-game signals (campaign progression, shop purchases, profile updates). Full reassessment during Phase 6 meta-game migration.

**Reasoning:** Two separate event domains: battle events are high-frequency, typed, and session-scoped. Meta-game events are infrequent, global, and cross-scene. Collapsing them into one system would force meta-game code to depend on `IGameSession`. Keeping them separate is cleaner. May be renamed or restructured in Phase 6.

**Affects:** `scripts/services/game_state_events.gd` stays as autoload for now. Battle code migrates to `SimEventsEmitted`.

### 16. Battle RNG: Isolated Sim RNG + Separate View RNG

**Decision:** All gameplay-affecting RNG (damage rolls, crits, targeting tiebreaks) goes through `DeterministicRng` in the simulation layer — seeded, deterministic, replayable. View layer gets its own separate `DeterministicRng` instance for visual randomness (particle spread, VFX variation). The two RNG instances never share state.

**Reasoning:** The sim's RNG sequence must be identical on host and client for deterministic simulation. If View code calls the sim's RNG, it advances the sequence unpredictably (different machines render different numbers of particles), causing desync. A separate View RNG instance preserves visual consistency across machines without affecting sim determinism. The GDScript `BattleRNG` autoload is retired for gameplay use.

**Affects:** `DeterministicRng` stays in sim layer. View layer creates its own `DeterministicRng` with a separate seed. `scripts/multiplayer/rng/battle_rng.gd` retired for gameplay RNG.

---

## Resolved Open Questions

*These were originally open questions, now settled. Kept here for historical context.*

- **A. Targeting Visuals** → Settled as Decision #9 (Input state + View renders)
- **B. AudioManager Placement** → Settled as Decision #10 (Standalone service)
- **C. Unit-Type-Specific Logic** → Settled as Decision #11 (Composition)
