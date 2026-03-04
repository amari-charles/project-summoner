# Session Layer

The session decides **how** the simulation gets run — locally or over a network. It's a dumb pipe that validates and routes Commands, ticks the simulation, and exposes results.

## Overview

`IGameSession` is the single interface that Input and View talk to. Everything above the session boundary goes through it.

For the full design, see [target-architecture.md &sect;3-4](../../target-architecture.md#3-session-layer). For detailed component specs (prediction, RNG sync, validation rules, etc.), see [design-specs.md](design-specs.md).

## Session Hierarchy

```
IGameSession                  <- Input + View talk to this only
+-- LocalSession              <- Singleplayer
+-- NetworkSession (abstract) <- Shared multiplayer infrastructure
    +-- HostSession           <- Ticks sim + broadcasts snapshots
    +-- ClientSession         <- Sends commands + applies snapshots
```

## IGameSession API

| Method / Event | Purpose |
|----------------|---------|
| `SubmitCommand(Command)` | Player wants to do something |
| `GetState() -> MatchState` | Read current game state (polled each frame) |
| `Tick(float delta)` | Advance game by one frame |
| `SimEventsEmitted` | Discrete event callback (damage, death, attack) |

## Key Components

| Component | Role |
|-----------|------|
| `CommandRouter` | Validates ALL commands before they reach the simulation |
| `IdentityMap` | UnitId <-> NetworkId O(1) bimap (network sessions only) |
| `SnapshotCodec` | Serialize/deserialize MatchState for network transmission |
| `DesyncChecker` | Hash comparison to detect host/client drift |

## Stubs

All stubs live in `scripts/csharp/Battle/Session/`. Method bodies throw `NotImplementedException`.

### LocalSession

```csharp
// scripts/csharp/Battle/Session/LocalSession.cs
public class LocalSession : IGameSession
{
    private readonly Simulation _simulation;
    private readonly CommandRouter _commandRouter;
    private readonly MatchState _state;
    private readonly List<SimEvent> _pendingEvents = new();

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public LocalSession(Simulation simulation, CommandRouter commandRouter, MatchState state)

    public MatchState GetState() => _state;
    public void SubmitCommand(ICommand command)   // Validate via CommandRouter, then queue
    public void Tick(float delta)                  // Tick simulation, collect events, fire SimEventsEmitted
}
```

### NetworkSession (abstract base)

```csharp
// scripts/csharp/Battle/Session/NetworkSession.cs
public abstract class NetworkSession : IGameSession
{
    protected readonly IdentityMap _identityMap = new();
    protected readonly SnapshotCodec _snapshotCodec = new();

    public abstract MatchState GetState();
    public abstract event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;
    public abstract void SubmitCommand(ICommand command);
    public abstract void Tick(float delta);

    public void HandleMessage(object message)     // Route incoming network data
}
```

### HostSession

```csharp
// scripts/csharp/Battle/Session/HostSession.cs
public class HostSession : NetworkSession
{
    private readonly Simulation _simulation;
    private readonly CommandRouter _commandRouter;

    public HostSession(Simulation simulation, CommandRouter commandRouter, MatchState state)

    public override MatchState GetState() => _state;
    public override void SubmitCommand(ICommand command)    // Validate + queue locally
    public override void Tick(float delta)                  // Tick sim, broadcast snapshots
    public void HandleRemoteCommand(int senderId, ICommand command)
}
```

### ClientSession

```csharp
// scripts/csharp/Battle/Session/ClientSession.cs
public class ClientSession : NetworkSession
{
    private readonly MatchState _localState = new();

    public override MatchState GetState() => _localState;
    public override void SubmitCommand(ICommand command)    // Send to host over network
    public override void Tick(float delta)                  // Apply latest snapshot
    public void ApplySnapshot(MatchState snapshot)
}
```

### CommandRouter

```csharp
// scripts/csharp/Battle/Session/CommandRouter.cs
public class CommandRouter
{
    public readonly record struct ValidationResult(bool IsValid, string Reason);
    public static readonly ValidationResult Valid = new(true, "");

    public ValidationResult Validate(ICommand command, MatchState state)
}
```

### IdentityMap

```csharp
// scripts/csharp/Battle/Session/IdentityMap.cs
public class IdentityMap
{
    public int GetNetworkId(int unitId)
    public int GetUnitId(int networkId)
    public void Register(int unitId, int networkId)
    public void Unregister(int unitId)
}
```

**Migration note (issue #5):** `UnitData` currently has `NetworkId` and `TargetNetworkId` fields that bleed networking into the simulation data model. When `IdentityMap` is implemented, these fields are removed from `UnitData` — all UnitId-to-NetworkId translation happens at the session boundary via `IdentityMap`. The simulation layer will only use `UnitId`; the network layer will only use `NetworkId`; `IdentityMap` bridges the two.

### SnapshotCodec

```csharp
// scripts/csharp/Battle/Session/SnapshotCodec.cs
public class SnapshotCodec
{
    public byte[] Encode(MatchState state)
    public MatchState Decode(byte[] data)
}
```

## Current Equivalents

| Target | Current File | Notes |
|--------|-------------|-------|
| `LocalSession` | *(none)* | SP goes directly through SimulationNode |
| `NetworkSession` | `MatchSession` (partial) | `Multiplayer/Core/MatchSession.cs` |
| `HostSession` | `HostRunner` | `Multiplayer/Authority/HostRunner.cs` |
| `ClientSession` | `ClientRunner` | `Multiplayer/Client/ClientRunner.cs` |
| `CommandRouter` | `RequestValidator` | `Multiplayer/Authority/RequestValidator.cs` — client-only today |
| `IdentityMap` | `NetworkIdRegistry` | `Multiplayer/Core/NetworkIdRegistry.cs` — maps Nodes, not ints |
| `SnapshotCodec` | `StateSnapshotBuilder` | `Multiplayer/Sync/StateSnapshotBuilder.cs` |
| `DesyncChecker` | `DesyncDetector` | `Multiplayer/Sync/DesyncDetector.cs` — rename only |
