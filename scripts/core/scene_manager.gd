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
const SCENE_MAIN_MENU: String = "res://scenes/ui/main_menu.tscn"
const SCENE_GAME_MODE_MENU: String = "res://scenes/ui/game_mode_menu.tscn"

## Campaign Scenes
const SCENE_CAMPAIGN_MAP: String = "res://scenes/ui/campaign_map.tscn"
const SCENE_HERO_SELECTION: String = "res://scenes/ui/hero_selection.tscn"
const SCENE_FIRST_CARD_SELECTION: String = "res://scenes/ui/first_card_selection.tscn"

## Collection Scenes
const SCENE_COLLECTION_SCREEN: String = "res://scenes/ui/collection_screen.tscn"

## Battle Scenes
const SCENE_BATTLE_3D: String = "res://scenes/battlefield/battle_3d.tscn"
const SCENE_REWARD_SCREEN: String = "res://scenes/ui/reward_screen.tscn"

## =============================================================================
## SCENE TRANSITION API
## =============================================================================

## Generic scene transition
## Use this for simple scene changes. Future enhancements (fade transitions,
## loading screens, etc.) can be added here.
func change_scene(scene_path: String) -> void:
	# TODO: Add fade transition here when implementing polish phase
	# await _fade_out()
	get_tree().change_scene_to_file(scene_path)
	# _fade_in() happens automatically in new scene
