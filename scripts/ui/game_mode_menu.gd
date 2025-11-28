extends Control
class_name GameModeMenu

## Game Mode Selection Menu
## Allows player to choose between Campaign, Shop, Collection, and Events

## Preloads
const HeroIconWidgetScene: PackedScene = preload("res://scenes/ui/hero_icon_widget.tscn")
const HeroManagementPanelScene: PackedScene = preload("res://scenes/ui/hero_management_panel.tscn")

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

## Hero icon widget reference
var hero_icon: HeroIconWidget = null

func _ready() -> void:
	print("Game Mode Menu loaded")
	_setup_hero_icon()

## Navigate to Campaign map
func _on_campaign_pressed() -> void:
	print("Opening campaign map...")
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Navigate to Shop screen
func _on_shop_pressed() -> void:
	print("Opening shop...")
	SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)

## Navigate to Collection screen
func _on_collection_pressed() -> void:
	print("Opening collection screen...")
	SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

## PLACEHOLDER - Events not yet implemented
func _on_events_pressed() -> void:
	print("Events button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## Return to Main Menu
func _on_back_pressed() -> void:
	print("Returning to main menu...")
	SceneManager.transition_to(SceneManager.SCENE_MAIN_MENU)

## =============================================================================
## HERO ICON
## =============================================================================

func _setup_hero_icon() -> void:
	# Only show hero icon after affinity event is completed
	var campaign: Node = get_node_or_null("/root/Campaign")
	if campaign and campaign.has_method("is_battle_completed"):
		var is_completed: bool = campaign.call("is_battle_completed", BattleIDs.EVENT_AFFINITY)
		if not is_completed:
			return

	hero_icon = HeroIconWidgetScene.instantiate()
	add_child(hero_icon)

	# Position in top-left corner
	hero_icon.anchor_left = 0.0
	hero_icon.anchor_right = 0.0
	hero_icon.anchor_top = 0.0
	hero_icon.anchor_bottom = 0.0
	hero_icon.offset_left = 20
	hero_icon.offset_right = 70
	hero_icon.offset_top = 20
	hero_icon.offset_bottom = 70

	# Connect signal
	hero_icon.icon_clicked.connect(_on_hero_icon_clicked)

func _on_hero_icon_clicked() -> void:
	var panel: HeroManagementPanel = HeroManagementPanelScene.instantiate()
	add_child(panel)
	panel.open()
