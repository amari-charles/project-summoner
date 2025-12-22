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
const SCENE_TITLE_SCREEN: String = "res://scenes/ui/title_screen.tscn"

## Campaign Scenes
const SCENE_CAMPAIGN_MAP: String = "res://scenes/ui/campaign_map.tscn"
const SCENE_EVENT_SCREEN: String = "res://scenes/ui/event_screen.tscn"
const SCENE_SUMMONER_SELECTION: String = "res://scenes/ui/summoner_selection.tscn"
const SCENE_SUMMONER_REVEAL: String = "res://scenes/ui/summoner_reveal.tscn"
const SCENE_SUMMONER_SCREEN: String = "res://scenes/ui/summoner_screen.tscn"
const SCENE_SUMMONER_SWITCH: String = "res://scenes/ui/summoner_switch_screen.tscn"
const SCENE_FIRST_CARD_SELECTION: String = "res://scenes/ui/first_card_selection.tscn"

## Collection Scenes
const SCENE_COLLECTION_SCREEN: String = "res://scenes/ui/collection_screen.tscn"
const SCENE_DECK_BUILDER: String = "res://scenes/ui/deck_builder.tscn"

## Shop Scenes
const SCENE_SHOP_SCREEN: String = "res://scenes/ui/shop_screen.tscn"
const SCENE_PREMIUM_STORE: String = "res://scenes/ui/premium_store_screen.tscn"

## Special Events & Settings
const SCENE_SPECIAL_EVENTS: String = "res://scenes/ui/special_events_screen.tscn"
const SCENE_SETTINGS: String = "res://scenes/ui/settings_screen.tscn"

## Battle Scenes
const SCENE_BATTLE_3D: String = "res://scenes/battlefield/battle_3d.tscn"
const SCENE_REWARD_SCREEN: String = "res://scenes/ui/reward_screen.tscn"

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
