# VFXManager

VFX pooling and spawning service.

## What It Is

A service that manages visual effects — pooling, instantiation, and cleanup. Called by EntityManager for event-triggered environmental effects.

## Responsibilities

- Pool and reuse VFX instances to avoid allocation spikes
- Spawn VFX at world positions (impact particles, spell effects, AoE ground effects)
- Manage VFX lifetimes (auto-cleanup after duration)

## What It Does NOT Do

- Subscribe to SimEvents directly (EntityManager routes to it)
- Game logic of any kind
- Per-frame sync (VFX are fire-and-forget)

## API

| Method | Purpose |
|--------|---------|
| `SpawnDeathVFX(position)` | Death particles at unit position |
| `SpawnSpellVFX(spellId, position)` | Spell-specific VFX at target |
| `SpawnImpactVFX(position)` | Generic impact particles |
| `SpawnAoEGroundEffect(position, radius)` | Ground decal for area effects |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Called by | `EntityManager` | Environmental VFX on events |
| Called by | `UnitVisual` | Unit-specific VFX (optional) |
| Standalone | — | No dependencies on game state |

## Today

VFXManager exists but isn't fully wired to the event pipeline. `SimEventSignalEmitter` converts SimEvents to Godot signals, but VFXManager doesn't subscribe to those signals — there's a gap in the pipeline. EntityManager fills that gap by routing events to both shells and VFXManager.
