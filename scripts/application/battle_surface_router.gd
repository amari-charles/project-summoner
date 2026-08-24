class_name BattleSurfaceRouter
extends RefCounted

## Resolves the typed battle runtime-surface contract to an application scene.
## Mirrors scripts/csharp/Infrastructure/Data/Events/BattleRuntimeSurface.cs.

enum BattleRuntimeSurface {
	STANDARD,
	DEBUG_ARENA,
}

const RUNTIME_SURFACE_KEY: String = "runtime_surface"
const STANDARD_ID: String = "standard"
const DEBUG_ARENA_ID: String = "debug_arena"


static func resolve_scene(battle_config: Dictionary) -> String:
	match resolve_surface(battle_config):
		BattleRuntimeSurface.DEBUG_ARENA:
			return SceneManager.SCENE_DEBUG_ARENA
		_:
			return SceneManager.SCENE_BATTLE_3D


static func resolve_surface(battle_config: Dictionary) -> BattleRuntimeSurface:
	var surface_id: String = SafeTypeUtils.string(
		battle_config.get(RUNTIME_SURFACE_KEY, STANDARD_ID),
		STANDARD_ID
	)
	match surface_id:
		DEBUG_ARENA_ID:
			return BattleRuntimeSurface.DEBUG_ARENA
		STANDARD_ID:
			return BattleRuntimeSurface.STANDARD
		_:
			push_error("BattleSurfaceRouter: Unknown runtime surface '%s'" % surface_id)
			return BattleRuntimeSurface.STANDARD
