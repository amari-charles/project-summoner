# Ranked System Architecture

This document describes the ranked multiplayer system for Fateforged.

## Overview

The ranked system uses a **host-authority model** with client-side prediction for responsive gameplay. This was chosen because:

1. **Godot physics are non-deterministic** - lockstep would require a custom engine fork
2. **Anti-cheat** - server/host validates all actions
3. **Migration path** - same protocol works for P2P, Nakama relay, or dedicated servers

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Protocol Layer (C#)                           │
│  Messages: CardPlayRequest, StateSnapshot, UnitSpawned, etc.    │
│  Transport-agnostic, same for P2P or dedicated server           │
└─────────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┴──────────────────┐
           ▼                                     ▼
  ┌─────────────────┐                 ┌─────────────────┐
  │ Host Runner     │                 │ Client Runner   │
  │ (Authority)     │                 │ (Prediction)    │
  │                 │                 │                 │
  │ • Validate      │   Snapshots     │ • Predict       │
  │ • Simulate      │ ───────────────►│ • Reconcile     │
  │ • Broadcast     │   (10 Hz)       │ • Interpolate   │
  └─────────────────┘                 └─────────────────┘
           │                                     │
           └──────────────────┬──────────────────┘
                              ▼
  ┌─────────────────────────────────────────────────────────────────┐
  │                 Transport Layer (C#)                             │
  │  IMatchTransport: P2PTransport → NakamaRelayTransport (future)  │
  └─────────────────────────────────────────────────────────────────┘
```

## Key Components

### Protocol Messages (`scripts/csharp/Multiplayer/Protocol/`)

| Message | Direction | Purpose |
|---------|-----------|---------|
| `CardPlayRequest` | Client → Host | Request to play a card |
| `CardPlayConfirmed` | Host → Client | Card play was valid |
| `CardPlayRejected` | Host → Client | Card play was invalid |
| `StateSnapshot` | Host → Client | Periodic state sync (10 Hz) |
| `UnitSpawned` | Host → Client | Unit created |
| `UnitDied` | Host → Client | Unit destroyed |
| `MatchEnded` | Host → Client | Game over |

### MatchSession (`scripts/csharp/Multiplayer/Core/MatchSession.cs`)

Central orchestrator that:
- Manages match lifecycle
- Routes messages to appropriate runner
- Emits Godot signals for game integration
- Tracks network IDs for entities

### HostRunner (`scripts/csharp/Multiplayer/Authority/HostRunner.cs`)

Runs on the host/server:
- Validates all client requests
- Runs authoritative simulation
- Broadcasts state snapshots at 10 Hz
- Determines win conditions

### ClientRunner (`scripts/csharp/Multiplayer/Client/ClientRunner.cs`)

Runs on clients:
- Predicts card plays locally (instant feedback)
- Sends requests to host for validation
- Reconciles when host confirms/rejects
- Interpolates entity positions from snapshots

### P2PTransport (`scripts/csharp/Multiplayer/Transport/P2PTransport.cs`)

ENet-based peer-to-peer transport:
- Host creates server on port 7777
- Client connects to host IP
- Uses Godot's built-in RPC for message passing

## ELO Rating System

### Configuration

| Parameter | Value | Description |
|-----------|-------|-------------|
| Starting ELO | 1200 | New player rating |
| K-Factor | 32 | Rating volatility |
| ELO Floor | 100 | Minimum rating |
| ELO Ceiling | 3000 | Maximum rating |

### Rank Tiers

| Tier | ELO Range |
|------|-----------|
| Bronze | 0-799 |
| Silver | 800-999 |
| Gold | 1000-1199 |
| Platinum | 1200-1399 |
| Diamond | 1400-1599 |
| Master | 1600-1799 |
| Grandmaster | 1800-1999 |
| Legend | 2000+ |

Each tier has 4 divisions (I-IV), where I is highest.

### Rating Calculation

Uses standard ELO formula:
```
Expected = 1 / (1 + 10^((opponent - player) / 400))
NewRating = OldRating + K * (ActualScore - ExpectedScore)
```

## Integration Points

### BattleContext

- New `BattleMode.MULTIPLAYER` mode
- `configure_multiplayer_battle()` sets up both players
- `MultiplayerAuthority` provider bridges to MatchSession
- `has_authority()` returns true only for host

### Game Controller

- Existing `has_authority()` checks work unchanged
- Win conditions evaluated only by host
- Host broadcasts `MatchEnded` to clients

### Card Playing

- Cards route through `MatchSession.RequestCardPlay()`
- Host validates (mana, position, etc.)
- Confirmed plays spawn units with network IDs
- Clients receive confirmation or rollback prediction

## Files

```
scripts/csharp/Multiplayer/
├── Protocol/
│   ├── Messages.cs           # All message types
│   └── MessageSerializer.cs  # Serialization
├── Core/
│   ├── MatchSession.cs       # Main orchestrator
│   ├── NetworkIdRegistry.cs  # Entity ID mapping
│   └── IMatchRunner.cs       # Runner interface
├── Authority/
│   ├── HostRunner.cs         # Host simulation
│   └── RequestValidator.cs   # Request validation
├── Client/
│   ├── ClientRunner.cs       # Client prediction
│   ├── PredictionBuffer.cs   # Unconfirmed actions
│   └── StateInterpolator.cs  # Smooth movement
├── Transport/
│   ├── IMatchTransport.cs    # Transport interface
│   └── P2PTransport.cs       # ENet implementation
└── Ranking/
    ├── EloCalculator.cs      # Rating math
    └── RankingService.cs     # Persistence

scripts/multiplayer/authority/
└── multiplayer_authority.gd  # GDScript bridge to MatchSession
```

## Future Work

1. **Nakama Integration** - Replace P2P with Nakama relay for NAT traversal
2. **Matchmaking** - Queue system based on ELO range
3. **Leaderboards** - Global and friends rankings
4. **Reconnection** - Handle disconnects gracefully
5. **Spectator Mode** - Watch live matches
6. **Replays** - Record and playback matches

## References

- [Gabriel Gambetta - Client-Server Architecture](https://www.gabrielgambetta.com/client-server-game-architecture.html)
- [Clash Royale Networking Analysis](https://blog.gemserk.com/2016/09/05/analyzing-clash-royale-multiplayer-solution/)
- [Godot Multiplayer Docs](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html)
