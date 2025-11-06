extends Node2D
class_name BattlefieldVisuals

## BattlefieldVisuals - Manages battlefield visual elements
##
## Creates bright, heroic battlefield environment.
## Ground uses existing tiled texture system (already correct).

## References to layers
@onready var sky: ColorRect = $BackgroundLayer/Sky
@onready var ground_layer: CanvasLayer = $GroundLayer
@onready var zone_markers: Node2D = $ZoneMarkers
@onready var gameplay_layer: Node2D = $GameplayLayer
@onready var effects_layer: CanvasLayer = $EffectsLayer
@onready var ui_layer: CanvasLayer = $UILayer

func _ready() -> void:
	print("BattlefieldVisuals: Initializing bright, heroic battlefield")
	_setup_sky()

func _setup_sky() -> void:
	# Create gradient for sky (top to bottom: light blue → peachy horizon)
	var gradient = Gradient.new()
	gradient.add_point(0.0, Color(0.53, 0.81, 0.98, 1.0))  # Light sky blue at top
	gradient.add_point(0.6, Color(0.39, 0.58, 0.93, 1.0))  # Azure in middle
	gradient.add_point(1.0, Color(0.95, 0.85, 0.75, 1.0))  # Warm peachy horizon

	# Create gradient texture
	var gradient_texture = GradientTexture2D.new()
	gradient_texture.gradient = gradient
	gradient_texture.fill = GradientTexture2D.FILL_LINEAR
	gradient_texture.fill_from = Vector2(0, 0)
	gradient_texture.fill_to = Vector2(0, 1)
	gradient_texture.width = 1920
	gradient_texture.height = 540

	# Apply to sky ColorRect as texture
	sky.texture = gradient_texture

	print("BattlefieldVisuals: Applied bright sky gradient")

## Get the gameplay layer where units should be spawned
func get_gameplay_layer() -> Node2D:
	return gameplay_layer

## Get the effects layer for particles and visual effects
func get_effects_layer() -> CanvasLayer:
	return effects_layer

## Get the UI layer for battlefield UI elements
func get_ui_layer() -> CanvasLayer:
	return ui_layer
