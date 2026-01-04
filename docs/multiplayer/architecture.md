# Multiplayer Architecture

This document describes the multiplayer architecture for Fateforged.

## Design Decisions

### Network Model: Hybrid Authority

We use a **hybrid approach** that starts with P2P host-authority and can migrate to dedicated servers later.

```
┌─────────────────────────────────────────────┐
│           AuthorityProvider (interface)      │
├─────────────────────────────────────────────┤
│  - has_authority()                           │
│  - execute_action()                          │
│  - request_action()                          │
└─────────────────────────────────────────────┘
         ▲                        ▲
         │                        │
┌─────────────────┐    ┌─────────────────────┐
│ HostAuthority   │    │ ServerAuthority     │
│ (P2P - Phase 1) │    │ (Dedicated - later) │
└─────────────────┘    └─────────────────────┘
```

**Phase 1 (Launch):** One player hosts, validates all actions, opponent connects P2P.
**Phase 2 (Scale):** Swap to dedicated server using same interface - no game logic rewrite.

### Backend: Nakama

We chose **Nakama** for backend services because:
- Open source (MIT license) - no vendor lock-in
- Self-hostable - control costs at scale
- Official Godot GDScript SDK
- Built-in: accounts, matchmaking, leaderboards, friends
- Can start on a $5 VPS, scale as needed

See `backend-research.md` for the full analysis.

### Game Mode: 1v1 Ranked PvP

Initial multiplayer is 1v1 only. This simplifies:
- Matchmaking (exactly 2 players)
- Authority (one host, one client)
- Win conditions (one winner, one loser)

Future modes (co-op, spectator, tournaments) build on this foundation.

### Offline Support

Single-player campaign works without internet:
- `LocalAuthority` is the default (no networking)
- Profile data stored in local JSON
- Nakama only required for multiplayer features
- If offline, multiplayer options hidden in UI

---

## Core Components

### 1. Authority Abstraction

The authority layer determines who validates game actions.

| Class | Purpose |
|-------|---------|
| `AuthorityProvider` | Abstract base interface |
| `LocalAuthority` | Single-player (offline), all actions immediate |
| `HostAuthority` | P2P host, validates and broadcasts |
| `ClientProxy` | P2P client, sends requests to host |
| `ServerAuthority` | Future dedicated server |

### 2. Seeded RNG

All gameplay randomness uses seeded RNG for determinism across clients.

**RNG Domains:**
- `DECK_SHUFFLE` - Card deck ordering
- `AI_DECISIONS` - Enemy card selection and timing
- `COMBAT_CRITS` - Critical hit rolls
- `SPAWN_POSITIONS` - AI spawn position jitter

**Non-deterministic (local RNG OK):**
- VFX particle variations
- Animation timing jitter
- Audio pitch variations

### 3. Action System

Game actions are validated by authority before execution.

```
Client                      Host/Authority
   │                              │
   │──── RequestAction ──────────>│
   │                              │ validate()
   │                              │ execute()
   │<──── ActionResult ───────────│
   │                              │
   │<──── BroadcastAction ────────│ (to all clients)
```

Actions include:
- `PlayCardAction` - Play a card at position
- `ForfeitAction` - Surrender the match

### 4. State Synchronization

**What syncs:**
- Card plays (card index, position)
- Unit spawns (network ID, position, stats)
- Damage events (attacker, target, amount)
- Unit deaths (network ID)
- Summoner HP changes
- Win/lose conditions

**Sync strategy:**
- Authority calculates, broadcasts results
- Clients apply results (no local simulation)
- Periodic state hashes for desync detection

### 5. Nakama Integration

```
┌─────────────────────────────────────────────┐
│                   Nakama                     │
├─────────────────────────────────────────────┤
│  Authentication     │  Matchmaking          │
│  - Device auth      │  - Ranked queue       │
│  - Email/social     │  - ELO-based matching │
├─────────────────────┼───────────────────────┤
│  Leaderboards       │  Player Data          │
│  - Global top 100   │  - Rating storage     │
│  - Friends          │  - Match history      │
└─────────────────────────────────────────────┘
```

---

## Data Flow

### Match Lifecycle

```
1. QUEUE
   └── Player joins ranked queue via Nakama
   └── Nakama finds opponent with similar rating

2. CONNECT
   └── Nakama returns match token + opponent info
   └── Host creates P2P session
   └── Client connects using token

3. SETUP
   └── Host generates RNG seed, shares to client
   └── Both initialize decks with seeded shuffle
   └── Preparation phase begins (30s)

4. BATTLE
   └── Players play cards → actions sent to host
   └── Host validates, executes, broadcasts
   └── Units fight autonomously (deterministic)

5. END
   └── Host detects win condition
   └── Broadcast match result
   └── Report result to Nakama (ELO update)
   └── Display post-match screen
```

### Card Play Flow

```
Player drags card → HandUI
       │
       ▼
   Create PlayCardAction
       │
       ▼
   authority.request_action(action)
       │
       ├── [LocalAuthority] Execute immediately
       │
       └── [HostAuthority/ClientProxy]
              │
              ├── [If host] Validate → Execute → Broadcast
              │
              └── [If client] Send to host → Wait for result
```

---

## File Structure

```
scripts/multiplayer/
├── authority/
│   ├── authority_provider.gd
│   ├── local_authority.gd
│   ├── host_authority.gd
│   └── client_proxy.gd
├── rng/
│   ├── battle_rng.gd
│   └── rng_domain.gd
├── core/
│   ├── network_state.gd
│   ├── peer_info.gd
│   └── match_config.gd
├── connection/
│   ├── room_code_service.gd
│   ├── p2p_host.gd
│   └── p2p_client.gd
├── actions/
│   ├── game_action.gd
│   ├── play_card_action.gd
│   └── action_validator.gd
├── sync/
│   ├── unit_sync.gd
│   ├── combat_sync.gd
│   └── state_snapshot.gd
├── nakama/
│   └── nakama_client.gd
├── auth/
│   ├── auth_service.gd
│   └── device_auth.gd
├── matchmaking/
│   └── matchmaking_service.gd
└── ranking/
    ├── elo_calculator.gd
    ├── ranking_service.gd
    └── leaderboard_service.gd
```

---

## Anti-Cheat Strategy

Since host has authority:
1. **Authority Validation** - All actions validated server-side
2. **Rate Limiting** - Max actions per second
3. **State Hashing** - Detect client manipulation
4. **Replay Storage** - Match data for review (Phase 4)

**Known limitations of P2P:**
- Host can cheat (delay opponent's actions, see deck, etc.)
- Mitigated later with dedicated servers

---

## Future Considerations

### Dedicated Servers (Phase 2+)
- `ServerAuthority` implements same interface
- Nakama orchestrates server allocation
- Both clients connect to server (no P2P)
- True anti-cheat (no trusted clients)

### Spectator Mode
- Additional client type (read-only)
- Receives all broadcast events
- No action permissions

### Tournaments
- Bracket system via Nakama
- Scheduled matches
- Prize distribution

---

*Last Updated: 2026-01-03*
