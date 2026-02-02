extends Node
class_name SceneManagerClass

## SceneManager - Centralized scene transition management
##
## Provides a single source of truth for all scene paths and
## a unified API for scene transitions. This prevents scattered
## hardcoded paths and makes it easy to add transition effects later.

## =============================================================================
## SCENE PATH CONSTANTS
## =============================================================================

## Main UI Scenes
const SCENE_TITLE_SCREEN: String = "res://scenes/ui/screens/title_screen.tscn"

## Campaign Scenes
const SCENE_CAMPAIGN_MAP: String = "res://scenes/ui/screens/campaign_map.tscn"
const SCENE_EVENT_SCREEN: String = "res://scenes/ui/screens/event_screen.tscn"
const SCENE_SUMMONER_SELECTION: String = "res://scenes/ui/screens/summoner_selection.tscn"
const SCENE_SUMMONER_REVEAL: String = "res://scenes/ui/modals/summoner_reveal.tscn"
const SCENE_SUMMONER_SCREEN: String = "res://scenes/ui/screens/summoner_screen.tscn"
const SCENE_SUMMONER_SWITCH: String = "res://scenes/ui/screens/summoner_switch_screen.tscn"
const SCENE_FIRST_CARD_SELECTION: String = "res://scenes/ui/screens/first_card_selection.tscn"

## Collection Scenes
const SCENE_COLLECTION_SCREEN: String = "res://scenes/ui/screens/collection_screen.tscn"

## Shop Scenes
const SCENE_SHOP_SCREEN: String = "res://scenes/ui/screens/shop_screen.tscn"
const SCENE_CARAVAN_SCREEN: String = "res://scenes/ui/screens/caravan_screen.tscn"
const SCENE_PREMIUM_STORE: String = "res://scenes/ui/screens/premium_store_screen.tscn"

## Special Events & Settings
const SCENE_SPECIAL_EVENTS: String = "res://scenes/ui/screens/special_events_screen.tscn"
const SCENE_SETTINGS: String = "res://scenes/ui/screens/settings_screen.tscn"
const SCENE_ONLINE: String = "res://scenes/ui/screens/online_screen.tscn"

## Battle Scenes
const SCENE_BATTLE_3D: String = "res://scenes/battlefield/battle_3d.tscn"
const SCENE_REWARD_SCREEN: String = "res://scenes/ui/screens/reward_screen.tscn"

## Multiplayer Scenes
const SCENE_MULTIPLAYER_LOBBY: String = "res://scenes/ui/screens/multiplayer_lobby.tscn"

## =============================================================================
## SCENE TRANSITION API
## =============================================================================

## Generic scene transition
## Use this for simple scene changes. Future enhancements (fade transitions,
## loading screens, etc.) can be added here.
##
## Note: This is the raw scene change. For full coordination (cleanup, service
## verification, waiting for scene coordinator), use SceneCoordinator.transition_to()
func change_scene(scene_path: String) -> void:
	# TODO: Add fade transition here when implementing polish phase
	# await _fade_out()
	get_tree().change_scene_to_file(scene_path)
	# _fade_in() happens automatically in new scene

## Coordinated scene transition with cleanup and initialization waiting
## This is the preferred API for scene changes - delegates to SceneCoordinator
func transition_to(scene_path: String) -> void:
	if SceneCoordinator:
		SceneCoordinator.transition_to(scene_path)
	else:
		# Fallback if SceneCoordinator not available (shouldn't happen)
		push_warning("SceneManager: SceneCoordinator not available, using raw change_scene()")
		change_scene(scene_path)
