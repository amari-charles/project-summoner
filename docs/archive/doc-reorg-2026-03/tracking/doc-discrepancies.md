# Documentation Discrepancies

This file tracks cases where a doc's claim diverges from what the code actually does.
Each entry includes the doc claim, the code behavior, and an assessment of whether it's
a future intent, a bug, or a doc error.

---

## Discrepancy: Canonical coordinate framing in simulation-architecture.md

- **Doc claimed:** "The simulation uses canonical coordinates (team 0 on left, team 1 on right)."
- **Code does:** Canonical space has `X < 0 = Host's spawn zone` and `X > 0 = Client's spawn zone`. Host is team 0, client is team 1. The "left/right" framing was an approximation — it is only valid if the camera places negative-X on the left, which is a visual/layout assumption not encoded in the simulation.
- **File + line:** `scripts/csharp/Battle/View/CoordinateTransform.cs:9-10`
- **Assessment:** Doc error / oversimplification. The updated `simulation-architecture.md` now uses the precise X-axis sign framing from the code rather than the left/right shorthand.

## Discrepancy: BattleContext authority API during host-authoritative migration

- **Doc claimed:** Multiplayer flow still relies on `BattleContext.is_multiplayer_battle()` / `BattleContext.has_authority()` checks during transition.
- **Code did (before fix):** Those methods (and `authority_provider` compatibility field) were removed from `battle_context.gd`, while C# still called them from `BattleSessionConfig` and `SimulationNode`.
- **File + line:** `scripts/application/battle_context.gd`, `scripts/csharp/Battle/Session/BattleSessionConfig.cs`, `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- **Assessment:** Migration regression (code bug). Restored compatibility accessors in `battle_context.gd` with config-based authority fallback.

## Discrepancy: InputCollector preview script paths after directory reorg

- **Doc claimed:** Battle/UI scripts moved from `scripts/ui/battle/` to `scripts/battle/ui/`.
- **Code did (before fix):** `InputCollector` still loaded spell and spawn preview scripts from legacy paths.
- **File + line:** `scripts/csharp/Battle/Input/InputCollector.cs`
- **Assessment:** Migration regression (code bug). Updated script paths to `res://scripts/battle/ui/...`.

## Discrepancy: Session docs still marked multiplayer as deferred stubs

- **Doc claimed:** `HostSession` / `ClientSession` transport wiring was still deferred and `SimulationNode` owned multiplayer transport handling.
- **Code now does:** Networking is owned by `HostSession` / `ClientSession` (`NetworkSession` transport lifecycle), and `SimulationNode` delegates through `IGameSession` with multiplayer session swap via `ConfigureMultiplayerSession(...)`.
- **File + line:** `scripts/csharp/Battle/Session/*.cs`, `scripts/csharp/Battle/Simulation/SimulationNode.cs`, `scripts/csharp/Battle/View/BattleScene.cs`
- **Assessment:** Docs were stale. Updated `docs/project/current-state.md` and `docs/architecture/gameplay/session/README.md` to match implementation.
