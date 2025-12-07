# Summoner and Nexus Architecture

This document describes the intended architecture for the Summoner (player character) and Nexus (win condition structure) systems.

## Overview

In Project Summoner, two key entities exist per player:

1. **Summoner** - The player character who commands units
2. **Nexus (Base3D)** - The mana construct being defended

## Summoner

The Summoner represents the player on the battlefield. They play cards and command units.

### Responsibilities

- Manage deck, hand, and card drawing
- Manage mana pool and regeneration
- Play cards to spawn units and cast spells
- Store summoner-specific stats and bonuses

### NOT Responsible For

- **Combat HP** - Summoners cannot be attacked or damaged
- **Win/Loss Condition** - Destroying a summoner does not end the game

### Summoner Stats

Summoner stats affect gameplay in the following ways:

| Stat | Effect |
|------|--------|
| `mana_regen` | Rate of mana regeneration per second |
| `max_mana` | Maximum mana pool (TODO: currently hardcoded as MANA_MAX) |
| `health` | Flows to Nexus HP (TODO: implement this flow) |

### Code Location

- `scripts/core/summoner.gd`
- Groups: `summoners`, `player_summoners` / `enemy_summoners`

## Nexus (Base3D)

The Nexus is the mana construct that each player defends. It's the physical target that units attack.

### Responsibilities

- Be the target for enemy unit attacks
- Track HP and emit damage/destroyed signals
- Serve as the win condition (destroy enemy nexus to win)

### Current Implementation

- `scripts/core/base_3d.gd`
- Groups: `bases`, `player_base` / `enemy_base`
- Has collision shape (BoxShape3D) for unit targeting
- HP bar displayed above the structure

### Future Considerations

The "Base" concept may evolve into:
- **Incarnation** - A manifestation of the summoner's power
- **Nexus** - A mana construct
- **Crystal** - A magical focal point

The visual representation can change, but the core mechanic remains: it's the structure units attack to win.

## Win Condition

**Only the Nexus (Base3D) destruction triggers game end.**

- When a Base3D reaches 0 HP, it emits `base_destroyed`
- GameController3D listens for this and calls `end_game()`
- The team whose base was destroyed loses

## Summoner Stats Flowing to Nexus

Currently planned (not yet implemented):

```
Summoner.health stat → Nexus.max_hp
```

This allows summoner progression to affect game difficulty through increased nexus durability.

## Group Usage

| Group | Contains | Used For |
|-------|----------|----------|
| `summoners` | All Summoner instances | Finding summoners for UI/spell targeting |
| `player_summoners` | Player's Summoner | Team-specific lookups |
| `enemy_summoners` | Enemy's Summoner | Team-specific lookups |
| `bases` | All Base3D instances | Unit attack targeting, win condition |
| `player_base` | Player's Base3D | Team-specific lookups |
| `enemy_base` | Enemy's Base3D | Team-specific lookups |

**Note:** Summoners are NOT in the `bases` group. They should not be found as attack targets.

## Migration Notes

As of 2025-11-25, the following cleanup was performed:

1. Removed `add_to_group("bases")` from Summoner
2. Removed unused HP/death code from Summoner:
   - `max_hp`, `current_hp` variables
   - `take_damage()`, `_die()` methods
   - `summoner_died` signal
3. Removed `_on_summoner_died` handler from GameController3D

These were vestigial from an earlier design where both Summoner and Base could be attacked.

As of 2025-12-07, `Summoner3D` was renamed to `Summoner` (consolidating the 2D and 3D implementations into a single 3D-only class).
