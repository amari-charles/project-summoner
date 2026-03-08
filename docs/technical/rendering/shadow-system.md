# Shadow System Architecture

**Status:** CURRENT  
**Last Updated:** 2026-03-08

## Overview

Unit shadows are rendered by a shared view-layer helper:

- `ShadowHelper` creates the shadow `Sprite3D` and shader material.
- `SpriteVisualComponent` and `SkeletalVisualComponent` both consume the helper.
- Profiles control shadow look/placement without hardcoded per-class constants.

## Core Types

- `ShadowProfile` (`scripts/csharp/Battle/View/Visual/ShadowProfile.cs`)  
  Immutable value object for runtime shadow settings.
- `ShadowProfilePreset` + `ShadowProfiles`  
  Built-in presets (`Default`, `Soft`, `Dramatic`) for quick tuning.
- `ShadowProfileResource` (`scripts/csharp/Battle/View/Visual/ShadowProfileResource.cs`)  
  Optional per-unit resource override for full custom tuning in editor.

## Resolution Order

For each visual component:

1. If `ShadowProfileOverride` is assigned, use it.
2. Otherwise use `ShadowPreset`.
3. Sanitize values before applying to runtime shadow creation.

## Why This Design

- Shared logic remains centralized in `ShadowHelper` (no duplication).
- Presets keep common tuning simple.
- Resource override supports unit-specific art direction without branching code.
- `ShadowProfile` as a typed value object keeps the API explicit and testable.

## Notes

- Runtime body/shadow depth correctness still depends on `alpha_cut = 2` in the unit body `Sprite3D` scenes.
- Shadow creation is runtime-only under `UnitVisual` ancestry (ghost previews do not spawn gameplay shadows).
