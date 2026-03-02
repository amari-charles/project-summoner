# Documentation Discrepancies

This file tracks cases where a doc's claim diverges from what the code actually does.
Each entry includes the doc claim, the code behavior, and an assessment of whether it's
a future intent, a bug, or a doc error.

---

## Discrepancy: Canonical coordinate framing in simulation-architecture.md

- **Doc claimed:** "The simulation uses canonical coordinates (team 0 on left, team 1 on right)."
- **Code does:** Canonical space has `X < 0 = Host's spawn zone` and `X > 0 = Client's spawn zone`. Host is team 0, client is team 1. The "left/right" framing was an approximation — it is only valid if the camera places negative-X on the left, which is a visual/layout assumption not encoded in the simulation.
- **File + line:** `scripts/csharp/Multiplayer/Core/CoordinateTransform.cs:9-10`
- **Assessment:** Doc error / oversimplification. The updated `simulation-architecture.md` now uses the precise X-axis sign framing from the code rather than the left/right shorthand.
