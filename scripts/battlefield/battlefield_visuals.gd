extends Node2D
class_name BattlefieldVisuals

## BattlefieldVisuals - Manages battlefield visual elements and atmosphere
##
## Handles visual layering, zone indicators, and prepares structure for
## future hand-drawn art assets.

## References to visual layers
@onready var background_layer: CanvasLayer = $BackgroundLayer
@onready var ground_layer: CanvasLayer = $GroundLayer
@onready var zone_markers: Node2D = $ZoneMarkers
@onready var gameplay_layer: Node2D = $GameplayLayer
@onready var effects_layer: CanvasLayer = $EffectsLayer
@onready var ui_layer: CanvasLayer = $UILayer

## Visual elements
@onready var sky: ColorRect = $BackgroundLayer/Sky
@onready var horizon: ColorRect = $BackgroundLayer/Horizon
@onready var player_zone_border: Line2D = $ZoneMarkers/PlayerZoneBorder
@onready var enemy_zone_border: Line2D = $ZoneMarkers/EnemyZoneBorder

func _ready() -> void:
	print("BattlefieldVisuals: Initialized layered battlefield")
	_setup_colors()

func _setup_colors() -> void:
	# Apply colors from ColorPalette
	sky.color = ColorPalette.SKY_DARK
	horizon.color = ColorPalette.SKY_HORIZON

	# Zone borders with our palette colors
	player_zone_border.default_color = ColorPalette.with_alpha(ColorPalette.PLAYER_ZONE_PRIMARY, 0.4)
	enemy_zone_border.default_color = ColorPalette.with_alpha(ColorPalette.ENEMY_ZONE_PRIMARY, 0.4)

	print("BattlefieldVisuals: Applied color palette")

## Get the gameplay layer where units should be spawned
func get_gameplay_layer() -> Node2D:
	return gameplay_layer

## Get the effects layer for particles and visual effects
func get_effects_layer() -> CanvasLayer:
	return effects_layer

## Get the UI layer for battlefield UI elements
func get_ui_layer() -> CanvasLayer:
	return ui_layer
