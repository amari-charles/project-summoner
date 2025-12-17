# Summoner and Incarnation Architecture

**Last Updated:** 2025-12-16

This document describes the architecture for the Summoner (player character) and Incarnation (win condition target) systems.

## Overview

In Fateforged, two key entities exist per player:

1. **Summoner** - The wizard-commander who builds and commands the army
2. **Incarnation** - The summoner's magical presence on the battlefield (win condition)

## The Incarnation Concept

### What Is the Incarnation?

The **Incarnation** is the summoner's magical presence projected onto the battlefield. It's what makes the summoner's army real and functional in that space.

- **Not the summoner themselves** — the summoner commands from elsewhere
- **A projection of power** — breaking it severs their connection to this battle
- **Works for any context** — duels, sparring, academic competitions, or war

### Why Incarnation Instead of Base/Nexus?

| Old Approach | Problem | New Approach |
|--------------|---------|--------------|
| Base/Nexus as target | Arbitrary structure with no narrative meaning | Incarnation — the summoner's magical presence |
| Static building | Doesn't fit contexts like sparring or duels | Works universally |
| "Destroy enemy base" | Generic, no fantasy | "Sever their connection to the battle" |

### Visual Representation

The Incarnation is represented as a **glowing elemental orb/presence** — a manifestation of magical energy. The exact visual can vary by summoner affinity (fire summoner = fiery orb, water summoner = flowing water presence, etc.).

## Summoner

The Summoner represents the player character. They play cards and command units.

### Responsibilities

- Manage deck, hand, and card drawing
- Manage fixed mana pool (50 mana default, no regeneration)
- Play cards to spawn units and cast spells (with summon time delay)
- Store summoner-specific stats and bonuses

### NOT Responsible For

- **Combat HP** — Summoners cannot be attacked or damaged
- **Win/Loss Condition** — Destroying a summoner does not end the game

### Summoner Stats

| Stat | Effect |
|------|--------|
| `max_mana` | Starting mana pool for the battle |
| `mana_regen` | Reserved for future mechanics (currently 0) |
| `health` | Flows to Incarnation HP |

### Code Location

- `scripts/core/summoner.gd`
- Groups: `summoners`, `player_summoners` / `enemy_summoners`

## Incarnation (Base3D)

The Incarnation is the magical target that each player defends. It's what units attack to win.

### Responsibilities

- Be the target for enemy unit attacks
- Track HP and emit damage/destroyed signals
- Serve as the win condition (destroy enemy Incarnation to win)

### Current Implementation

- `scripts/core/base_3d.gd`
- Groups: `bases`, `player_base` / `enemy_base`
- Has collision shape (BoxShape3D) for unit targeting
- HP bar displayed above the presence

### Future Visual Development

The Incarnation visual will evolve to:
- Reflect summoner element (fire orb, water presence, etc.)
- Pulse and react to battle events
- Show clear damage states
- Have a satisfying destruction animation

## Win Condition

**Only the Incarnation (Base3D) destruction triggers game end.**

- When a Base3D reaches 0 HP, it emits `base_destroyed`
- GameController3D listens for this and calls `end_game()`
- The team whose Incarnation was destroyed loses

## Battle Flow Integration

The Incarnation ties into the two-phase battle system:

### PREPARATION Phase
- Incarnations are invulnerable (units are INACTIVE anyway)
- Players build formations around defending their own Incarnation
- Strategic positioning relative to Incarnation placement

### BATTLE Phase
- Units activate and advance toward enemy Incarnation
- First Incarnation destroyed ends the match
- Reinforcements can be summoned to protect weakened Incarnations

## Group Usage

| Group | Contains | Used For |
|-------|----------|----------|
| `summoners` | All Summoner instances | Finding summoners for UI/spell targeting |
| `player_summoners` | Player's Summoner | Team-specific lookups |
| `enemy_summoners` | Enemy's Summoner | Team-specific lookups |
| `bases` | All Incarnation (Base3D) instances | Unit attack targeting, win condition |
| `player_base` | Player's Incarnation | Team-specific lookups |
| `enemy_base` | Enemy's Incarnation | Team-specific lookups |

**Note:** Summoners are NOT in the `bases` group. They should not be found as attack targets.

## Terminology Migration

As of 2025-12-16, the following terminology is being adopted:

| Old Term | New Term | Notes |
|----------|----------|-------|
| Base | Incarnation | Narrative-appropriate win condition |
| Nexus | Incarnation | Same change |
| Destroy base | Sever connection | More evocative language |

The code still uses `Base3D` class name for now, but documentation and player-facing text should use "Incarnation."

## Migration History

**2025-11-25:** Removed HP/death code from Summoner (summoners can't be attacked)

**2025-12-07:** Consolidated `Summoner3D` → `Summoner` (single 3D-only class)

**2025-12-16:** Adopted Incarnation terminology; updated documentation to reflect new battle system design
