# GDScript Service API Wrappers

## Purpose

This project uses typed GDScript wrapper classes under `scripts/infrastructure/services/` to call C# autoload services safely.

Goals:
- Keep `Variant` conversion at the boundary.
- Avoid direct `Node.call("...")` usage in UI and gameplay scripts.
- Centralize interop behavior so method name and type changes are localized.

## Usage Rules

1. Call wrappers from gameplay/UI code, not raw autoload methods.
- Preferred: `CardServiceApi.get_card_dict(instance_id)`
- Avoid: `CardService.GetCardDict(instance_id)` or `service.call("GetCardDict", ...)`

2. Convert types inside wrappers with `SafeTypeUtils`.
- Wrappers should return typed values (`String`, `Dictionary`, `Array`, `bool`, `int`, `float`).
- Call sites should not cast `Variant` to primitives unless there is a strong reason.

3. Fail loudly when required services are missing.
- Wrappers must report missing autoloads with `push_error(...)`.
- Do not silently hide service failures with empty defaults only.

4. Keep wrapper methods one-to-one with service capabilities.
- Wrapper names use snake_case and map clearly to C# methods.
- Add wrapper methods when introducing new C# calls.

## Migration Pattern

When replacing direct calls:

1. Add or extend wrapper method in `scripts/infrastructure/services/*_api.gd`.
2. Move `Variant` conversion into wrapper via `SafeTypeUtils`.
3. Update call sites to use wrapper return types directly.
4. Run `godot --headless --editor --quit` and verify no new `GDScript::reload` unsafe warnings.

## Current Scope

The wrapper layer currently covers high-traffic services used by UI/meta/battle orchestration (for example: campaign, profile repo, decks, card service, reward, economy, items, summoner services, shop, catalogs).
