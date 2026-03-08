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

- [simulation-architecture.md](simulation-architecture.md)
- [projectile-system.md](projectile-system.md)
- [hit-geometry-v1.md](hit-geometry-v1.md)
- [projectile-targeting.md](projectile-targeting.md)
- [targeting-system.md](targeting-system.md)
- [ability-system.md](ability-system.md)
- [trait-system-architecture.md](trait-system-architecture.md)
- [unit-collision-separation.md](unit-collision-separation.md)
- [unit-stat-pipeline.md](unit-stat-pipeline.md)
- [battle-enemy-spawning.md](battle-enemy-spawning.md)
- [reward-system-architecture.md](reward-system-architecture.md)

### Infrastructure / Platform

- [campaign-data.md](campaign-data.md)
- [save-system.md](save-system.md)
- [audio-system.md](audio-system.md)
- [dialogue-system.md](dialogue-system.md)
- [debug-menu.md](debug-menu.md)
- [scene-script-configuration.md](scene-script-configuration.md)
- [framerate-independence.md](framerate-independence.md)
- [strict-typing-validation.md](strict-typing-validation.md)
- [shop-cache-architecture.md](shop-cache-architecture.md)
- [gdscript-service-api-wrappers.md](gdscript-service-api-wrappers.md)

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
