# Technical Docs Index

**Status:** CURRENT  
**Last Updated:** 2026-03-08

This folder is for implementation references.

## Scope (What Belongs Here)

`docs/technical/` is for "how we implement and operate systems in code":

- runtime behavior details;
- data flow/pipeline details;
- tooling/debugging procedures;
- subsystem implementation gotchas.

## What Does Not Belong Here

- top-level architecture contracts and boundary models (`docs/architecture/`);
- product/system behavior specs and design intent (`docs/features/`);
- migration execution logs or completed refactor notes (`docs/archive/`).

## Quick Map

### Runtime Systems

- [runtime/simulation-architecture.md](runtime/simulation-architecture.md)
- [runtime/projectile-system.md](runtime/projectile-system.md)
- [runtime/hit-geometry-v1.md](runtime/hit-geometry-v1.md)
- [runtime/projectile-targeting.md](runtime/projectile-targeting.md)
- [runtime/targeting-system.md](runtime/targeting-system.md)
- [runtime/ability-system.md](runtime/ability-system.md)
- [runtime/trait-system-architecture.md](runtime/trait-system-architecture.md)
- [runtime/unit-collision-separation.md](runtime/unit-collision-separation.md)
- [runtime/unit-stat-pipeline.md](runtime/unit-stat-pipeline.md)
- [runtime/battle-enemy-spawning.md](runtime/battle-enemy-spawning.md)
- [runtime/reward-system-architecture.md](runtime/reward-system-architecture.md)

### Infrastructure / Platform

- [infrastructure/campaign-data.md](infrastructure/campaign-data.md)
- [infrastructure/save-system.md](infrastructure/save-system.md)
- [infrastructure/audio-system.md](infrastructure/audio-system.md)
- [infrastructure/dialogue-system.md](infrastructure/dialogue-system.md)
- [infrastructure/shop-cache-architecture.md](infrastructure/shop-cache-architecture.md)
- [infrastructure/gdscript-service-api-wrappers.md](infrastructure/gdscript-service-api-wrappers.md)

### Rendering

- [rendering/scene-script-configuration.md](rendering/scene-script-configuration.md)
- [rendering/framerate-independence.md](rendering/framerate-independence.md)
- [rendering/unit-animations.md](rendering/unit-animations.md)
- [rendering/shadow-system.md](rendering/shadow-system.md)
- [rendering/battle-camera-tuning.md](rendering/battle-camera-tuning.md)

### Tooling

- [tooling/debug-menu.md](tooling/debug-menu.md)
- [tooling/strict-typing-validation.md](tooling/strict-typing-validation.md)

### VFX

- [vfx/custom-data.md](vfx/custom-data.md)
- [vfx/pooling-best-practices.md](vfx/pooling-best-practices.md)

## Related Canonical Docs

- [../architecture/graph-of-graphs.md](../architecture/graph-of-graphs.md)
- [../architecture/target-architecture.md](../architecture/target-architecture.md)
- [../architecture/application-layer.md](../architecture/application-layer.md)
- [../features/README.md](../features/README.md)

## Archived Technical References

Historical technical docs moved to:

- `docs/archive/doc-reorg-2026-03/technical/`
