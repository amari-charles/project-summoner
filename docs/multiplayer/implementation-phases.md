# Multiplayer Implementation Phases

Detailed breakdown of the multiplayer implementation roadmap.

---

## Phase 1: Network Foundation

**Goal:** Build core networking infrastructure for local testing with room codes.

**Duration:** 2-3 weeks

### 1.1 Authority Abstraction Layer

Create an abstraction that allows the same game code to work under different authority models.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/authority/authority_provider.gd` | Abstract base class |
| `scripts/multiplayer/authority/local_authority.gd` | Single-player (offline) |
| `scripts/multiplayer/authority/host_authority.gd` | P2P host |
| `scripts/multiplayer/authority/client_proxy.gd` | P2P client |

**Modified Files:**
| File | Changes |
|------|---------|
| `scripts/core/battle_context.gd` | Add `authority_provider` reference |
| `scripts/core/game_controller_3d.gd` | Use authority checks for state changes |

**Key Interface:**
```gdscript
class_name AuthorityProvider extends RefCounted

func has_authority() -> bool
func is_local_action(player_id: int) -> bool
func execute_action(action: GameAction) -> void
func request_action(action: GameAction) -> void
```

---

### 1.2 Seeded RNG System

Replace all randomness with seeded RNG for deterministic gameplay.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/rng/battle_rng.gd` | Singleton managing seeded RNG |
| `scripts/multiplayer/rng/rng_domain.gd` | Separate streams per system |

**Files to Modify:**
| File | Current | Change To |
|------|---------|-----------|
| `scripts/core/summoner.gd` | `deck.shuffle()` | `BattleRNG.shuffle_array(deck, RNGDomain.DECK_SHUFFLE)` |
| `scripts/ai/heuristic_ai.gd` | `randf_range()` | `BattleRNG.randf_range(min, max, RNGDomain.AI_DECISIONS)` |
| `scripts/csharp/Combat/DamageSystem.cs` | `GD.Randf()` | Seeded RNG for crit rolls |

**RNG Domains:**
- `DECK_SHUFFLE` - Deck ordering
- `AI_DECISIONS` - Enemy AI randomness
- `COMBAT_CRITS` - Critical hit rolls
- `SPAWN_POSITIONS` - AI spawn jitter

---

### 1.3 Network State Management

Track connection state and peer information.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/core/network_state.gd` | Connection state autoload |
| `scripts/multiplayer/core/peer_info.gd` | Peer data class |
| `scripts/multiplayer/core/match_config.gd` | Match configuration |

**Signals:**
```gdscript
signal connection_established(peer_id: int)
signal connection_lost(peer_id: int)
signal match_started(config: MatchConfig)
signal match_ended(result: MatchResult)
```

---

### 1.4 P2P Connection (Room Codes)

Basic P2P for local network testing.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/connection/room_code_service.gd` | Generate/validate codes |
| `scripts/multiplayer/connection/p2p_host.gd` | Host logic |
| `scripts/multiplayer/connection/p2p_client.gd` | Client logic |
| `scenes/ui/screens/multiplayer_lobby.tscn` | Lobby UI |

**Flow:**
1. Host creates room → generates 6-char code
2. Client enters code → resolves to IP:port
3. ENet connection established
4. Handshake exchanges player info

---

## Phase 2: Game Synchronization

**Goal:** Make battles work in multiplayer with proper state sync.

**Duration:** 3-4 weeks

### 2.1 Action System

Unified system for validating and replicating card plays.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/actions/game_action.gd` | Base action class |
| `scripts/multiplayer/actions/play_card_action.gd` | Card play action |
| `scripts/multiplayer/actions/action_validator.gd` | Server-side validation |
| `scripts/multiplayer/actions/action_replicator.gd` | Client replication |

**Modified Files:**
| File | Changes |
|------|---------|
| `scripts/core/summoner.gd` | `play_card_3d()` creates action |
| `scripts/cards/card.gd` | Add network serialization |

---

### 2.2 Unit Spawn Sync

Ensure units spawn identically on both clients.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/unit_sync.gd` | Spawn/death sync |
| `scripts/multiplayer/sync/network_id_registry.gd` | Node-to-ID mapping |

**Modified Files:**
| File | Changes |
|------|---------|
| `scripts/cards/card.gd` | Assign network IDs on spawn |
| `scripts/csharp/Units/Unit3D.cs` | Add `NetworkId` property |

---

### 2.3 Combat Event Sync

Synchronize damage, healing, and effects.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/combat_sync.gd` | Combat replication |

**Events to Sync:**
- `DamageDealt` - Attacker, target, amount, is_crit
- `UnitDied` - Unit network_id
- `SummonerDamaged` - Team, amount

**Modified Files:**
| File | Changes |
|------|---------|
| `scripts/csharp/Combat/DamageSystem.cs` | Emit network events |
| `scripts/services/game_state_events.gd` | Add network signals |

---

### 2.4 Win Condition Sync

Authority detects and broadcasts win conditions.

**Modified Files:**
| File | Changes |
|------|---------|
| `scripts/core/game_controller_3d.gd` | Authority-only detection, broadcast result |

---

### 2.5 State Snapshot System

Periodic snapshots for desync detection.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/state_snapshot.gd` | State capture |
| `scripts/multiplayer/sync/desync_detector.gd` | Desync handling |

**Snapshot Contents:**
- Unit positions (quantized)
- Unit HP values
- Summoner HP/mana
- Frame number
- RNG state hash

---

## Phase 3: Nakama Integration

**Goal:** Add backend services for auth, matchmaking, rankings.

**Duration:** 2-3 weeks

### 3.1 Nakama Client Setup

Install and configure Nakama SDK.

**Tasks:**
- Add Nakama Godot SDK (AssetLib or manual)
- Create `scripts/multiplayer/nakama/nakama_client.gd` wrapper
- Add NakamaClient autoload to project

---

### 3.2 Authentication

Implement user authentication.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/auth/auth_service.gd` | Auth abstraction |
| `scripts/multiplayer/auth/device_auth.gd` | Device-based auth |
| `scenes/ui/screens/login_screen.tscn` | Optional login UI |

**Flow:**
1. First launch → create guest account (device ID)
2. Guest can optionally link to email/social
3. Session tokens stored locally for auto-login

---

### 3.3 Ranked Matchmaking

Implement ranked queue.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/matchmaking/matchmaking_service.gd` | Queue management |
| `scripts/multiplayer/matchmaking/match_ticket.gd` | Ticket data |
| `scenes/ui/screens/ranked_queue.tscn` | Queue UI |

**Match Properties:**
```gdscript
{
    "mode": "ranked_1v1",
    "rating": player_elo,
    "region": player_region
}
```

---

### 3.4 ELO System

Skill-based rating.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/elo_calculator.gd` | ELO math |
| `scripts/multiplayer/ranking/ranking_service.gd` | Rating management |

**Parameters:**
- Starting ELO: 1200
- K-factor: 32
- Floor: 800

---

### 3.5 Match Reporting

Report outcomes to Nakama.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/match_reporter.gd` | Submit results |

---

### 3.6 Leaderboards

Display rankings.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/leaderboard_service.gd` | Fetch data |
| `scenes/ui/screens/leaderboard_screen.tscn` | Leaderboard UI |

---

## Phase 4: Polish

**Goal:** Handle edge cases, improve UX, production readiness.

**Duration:** 2-3 weeks

### 4.1 Reconnection Handling

Handle disconnections gracefully.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/connection/reconnection_handler.gd` | Reconnection logic |
| `scenes/ui/overlays/reconnecting_overlay.tscn` | "Reconnecting..." UI |

**Flow:**
1. Detect disconnection
2. Show overlay, attempt reconnect (30s timeout)
3. On success: resync from snapshot
4. On failure: forfeit match

---

### 4.2 Latency Compensation

Smooth experience despite latency.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/latency_compensator.gd` | Prediction/smoothing |

**Techniques:**
- Local prediction for card plays
- Smooth interpolation for unit positions
- Rollback on authority rejection

---

### 4.3 Error Handling

Robust error handling for all network operations.

**New Files:**
| File | Purpose |
|------|---------|
| `scripts/multiplayer/error/error_handler.gd` | Centralized handling |
| `scripts/multiplayer/error/error_codes.gd` | Error definitions |

---

### 4.4 Multiplayer UI

Complete UI for multiplayer experience.

**New Scenes:**
| Scene | Purpose |
|-------|---------|
| `scenes/ui/screens/multiplayer_menu.tscn` | Main MP menu |
| `scenes/ui/screens/match_loading.tscn` | Loading screen |
| `scenes/ui/screens/match_result.tscn` | Post-match results |
| `scenes/ui/overlays/opponent_info.tscn` | In-battle opponent display |

---

### 4.5 Anti-Cheat

Basic anti-cheat measures.

**Strategies:**
- Authority validation for all actions
- Rate limiting (max actions/second)
- State hashing for manipulation detection
- Replay recording for review

---

## Dependency Graph

```
Phase 1.1 (Authority) ─┐
                       ├──> Phase 2.1 (Actions) ──> Phase 2.2 (Unit Sync)
Phase 1.2 (RNG) ───────┤                                    │
                       │                                    v
Phase 1.3 (Network) ───┴──> Phase 2.3 (Combat Sync) ──> Phase 2.4 (Win)
                       │                                    │
Phase 1.4 (P2P) ───────┘                                    v
                                                      Phase 2.5 (Snapshot)
                                                            │
                                                            v
                    Phase 3.1 (Nakama) ──> Phase 3.2 (Auth) ──> Phase 3.3 (MM)
                                                                      │
                                                                      v
                                                    Phase 3.4 (ELO) ──> Phase 3.5 (Report)
                                                                      │
                                                                      v
                                                              Phase 3.6 (Leaderboards)
                                                                      │
                                                                      v
                    Phase 4.1 (Reconnect) ──> Phase 4.2 (Latency) ──> Phase 4.3 (Errors)
                                                                      │
                                                                      v
                                                              Phase 4.4 (UI)
                                                                      │
                                                                      v
                                                              Phase 4.5 (Anti-Cheat)
```

---

## Recommended Start

Begin with **Phase 1.2 Seeded RNG System**:
- Foundational for all multiplayer
- Low risk - doesn't break single-player
- Testable immediately without networking
- Small scope, clear success criteria

---

*Last Updated: 2026-01-03*
