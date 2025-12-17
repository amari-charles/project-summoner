# Summoner Architecture

**Last Updated:** 2025-12-17

This document describes the architecture for the Summoner system, which serves as both the player character and the attack target (win condition).

## Overview

In Fateforged, the **Summoner** is the central entity for each player, handling:

1. **Card Management** - Deck, hand, and card playing
2. **Mana System** - Fixed pool for the battle
3. **Win Condition** - The target units attack to win

## The Unified Summoner Concept

### What Is the Summoner?

The **Summoner** is the wizard-commander who projects their power onto the battlefield. They are:

- **The army commander** - plays cards to spawn units and cast spells
- **The attack target** - destroying the summoner wins the game
- **The magical anchor** - their presence sustains the battle

### Why Unified Design?

| Old Approach | Problem | New Approach |
|--------------|---------|--------------|
| Separate Summoner + Base3D | Two entities to manage, confusing architecture | Single Summoner class |
| Base as "Incarnation" | Extra abstraction with unclear benefit | Summoner IS the target |
| Dual signal systems | hp_changed on base, mana on summoner | All signals on Summoner |

### Visual Representation

The Summoner is represented by a castle sprite on the battlefield. Future plans include summoner-specific visuals based on element/faction (fire summoner = fiery presence, etc.).

## Summoner Responsibilities

### Card & Mana Management
- Manage deck, hand, and card drawing
- Fixed mana pool (50 mana default, no regeneration)
- Play cards with summon time delay (casting indicator)
- Store summoner-specific stats and bonuses from traits

### Combat & Win Condition
- Track HP (default 300, configurable via traits)
- Receive damage from enemy units
- Emit `summoner_destroyed` signal when HP reaches 0
- Display HP bar above position
- Show hit feedback animation (flash + shake)

### Summoner Stats

| Stat | Effect |
|------|--------|
| `max_mana` | Starting mana pool for the battle |
| `max_hp` | Summoner's health (default 300) |
| `damage_bonus` | Applied to friendly unit attacks |
| `damage_reduction` | Applied to incoming damage |

## Code Location

- `scripts/core/summoner.gd`
- Groups: `summoners`, `bases`, `player_summoners`/`enemy_summoners`, `player_bases`/`enemy_bases`

## Win Condition

**Summoner destruction triggers game end.**

- When a Summoner reaches 0 HP, it emits `summoner_destroyed`
- `GameController3D` listens for this signal and calls `end_game()`
- The team whose Summoner was destroyed loses

## Battle Flow Integration

### PREPARATION Phase
- Summoners are invulnerable (units are INACTIVE)
- Players place units to defend their Summoner
- Strategic positioning relative to Summoner location

### BATTLE Phase
- Units activate and advance toward enemy Summoner
- First Summoner destroyed ends the match
- Reinforcements can be summoned to protect weakened Summoners

## Group Usage

| Group | Contains | Used For |
|-------|----------|----------|
| `summoners` | All Summoner instances | Finding summoners for UI/spell targeting |
| `player_summoners` | Player's Summoner | Team-specific lookups |
| `enemy_summoners` | Enemy's Summoner | Team-specific lookups |
| `bases` | All Summoner instances | Unit attack targeting (same as summoners) |
| `player_bases` | Player's Summoner | Team-specific attack targets |
| `enemy_bases` | Enemy's Summoner | Team-specific attack targets |

**Note:** Summoners are in BOTH `summoners` and `bases` groups. They are the attack targets.

## Signal Reference

| Signal | Emitted When |
|--------|--------------|
| `summoner_ready(summoner)` | After init() completes |
| `mana_changed(current, max)` | Mana spent or modified |
| `hand_changed(hand)` | Card drawn or played |
| `hp_changed(current, max)` | Damage taken or HP modified |
| `summoner_damaged(summoner, damage)` | Damage received |
| `summoner_destroyed(summoner)` | HP reaches 0 |
| `casting_started(card, duration)` | Summon time delay begins |
| `casting_completed(card)` | Unit spawns after delay |

## Migration History

**2025-11-25:** Removed HP/death code from Summoner (summoners can't be attacked)

**2025-12-07:** Consolidated `Summoner3D` to `Summoner` (single 3D-only class)

**2025-12-16:** Adopted Incarnation terminology; documented separate Base3D concept

**2025-12-17:** Merged Summoner and Base3D - Summoner is now the attack target. Removed Base3D class entirely. This simplifies the architecture by eliminating a redundant entity.
