# Session Layer

The session decides **how** simulation is run: locally or over network. It validates/routes commands, advances runtime (`Tick`), and exposes state/events to View and Input.

## Session Hierarchy

```
IGameSession                  <- Input + View talk to this only
+-- LocalSession              <- Singleplayer/quest/tutorial/practice
+-- NetworkSession (abstract) <- Shared multiplayer transport/message plumbing
    +-- HostSession           <- Authoritative tick + remote command validation + snapshot broadcast
    +-- ClientSession         <- Command send + snapshot apply
```

## IGameSession API

| Method / Event | Purpose |
|----------------|---------|
| `SubmitCommand(ICommand)` | Input submits player intent |
| `GetState() -> MatchState` | View polls current state |
| `Tick(float delta)` | Advance session runtime |
| `SimEventsEmitted` | Discrete event stream for VFX/UI/audio |

## Concrete Components

| Component | Current role |
|-----------|--------------|
| `LocalSession` | Validate via `CommandRouter`, queue commands, tick `Simulation` |
| `NetworkSession` | Owns `IMatchTransport`, protocol serialization, message subscription lifecycle |
| `HostSession` | Handles remote requests, derives authoritative team from sender identity (ignores payload team), validates/queues commands, ticks simulation, broadcasts snapshots and match end, pauses simulation during reconnect grace |
| `ClientSession` | Sends local commands to host, applies host snapshots to local `MatchState`, emits first-snapshot hook, derives visual events from snapshot deltas |
| `CommandRouter` | Validation gate for all session types |
| `IdentityMap` | UnitId <-> NetworkId bimap at session boundary |
| `SnapshotCodec` | Binary snapshot codec (available for session sync paths) |

## Runtime Wiring

- `SimulationNode.Initialize(...)` creates `LocalSession` by default.
- `BattleScene.SetupMultiplayer()` constructs/reuses `IMatchTransport`.
- `SimulationNode.ConfigureMultiplayerSession(transport, isHost)` swaps to `HostSession` or `ClientSession`.
- `SimulationNode` remains a scene bridge (fixed-step scheduling + Godot signal forwarding), not a transport owner.

## Reconnect Behavior (Multiplayer)

- Disconnects now enter a reconnect grace window (30s) instead of immediate hard fail.
- Host pauses authoritative simulation while waiting for peer rejoin.
- Nakama transport attempts socket+match rejoin automatically and re-emits connectivity events on success.
- If grace expires, session resolves match as disconnect loss.

## Notes

- `UnitData.NetworkId` / `TargetNetworkId` are still present during migration compatibility.
- Long-term cleanup target is to keep network identifiers strictly at the session boundary via `IdentityMap`.
