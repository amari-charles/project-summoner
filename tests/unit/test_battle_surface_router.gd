extends GutTest

const BATTLE_SURFACE_ROUTER = preload("res://scripts/application/battle_surface_router.gd")


func test_missing_surface_resolves_to_standard_battle() -> void:
	assert_eq(BATTLE_SURFACE_ROUTER.resolve_scene({}), SceneManager.SCENE_BATTLE_3D)


func test_standard_surface_resolves_to_standard_battle() -> void:
	var config: Dictionary = {"runtime_surface": "standard"}
	assert_eq(
		BATTLE_SURFACE_ROUTER.resolve_surface(config),
		BATTLE_SURFACE_ROUTER.BattleRuntimeSurface.STANDARD
	)
	assert_eq(BATTLE_SURFACE_ROUTER.resolve_scene(config), SceneManager.SCENE_BATTLE_3D)


func test_debug_arena_surface_resolves_to_debug_arena() -> void:
	var config: Dictionary = {"runtime_surface": "debug_arena"}
	assert_eq(
		BATTLE_SURFACE_ROUTER.resolve_surface(config),
		BATTLE_SURFACE_ROUTER.BattleRuntimeSurface.DEBUG_ARENA
	)
	assert_eq(BATTLE_SURFACE_ROUTER.resolve_scene(config), SceneManager.SCENE_DEBUG_ARENA)


func test_unknown_surface_falls_back_to_standard_battle() -> void:
	var config: Dictionary = {"runtime_surface": "unsupported"}
	assert_eq(BATTLE_SURFACE_ROUTER.resolve_scene(config), SceneManager.SCENE_BATTLE_3D)
	assert_push_error("BattleSurfaceRouter: Unknown runtime surface 'unsupported'")
