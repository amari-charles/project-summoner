extends Node3D
class_name BattlefieldVisuals3D

## BattlefieldVisuals3D - Manages 2.5D battlefield visual elements
##
## Creates bright, heroic battlefield environment using 2D sprites in 3D space.
## Sky and ground are Sprite3D nodes positioned at different Z depths for parallax.

## References to layers
@onready var sky_layer: Sprite3D = $SkyLayer
@onready var ground_layer: Sprite3D = $GroundLayer
@onready var gameplay_layer: Node3D = $GameplayLayer
@onready var ui_layer: CanvasLayer = $UILayer

func _ready() -> void:
	print("BattlefieldVisuals3D: Initializing 2.5D battlefield")
	_setup_sky()
	_setup_ground()

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

	# Apply to sky Sprite3D
	sky_layer.texture = gradient_texture

	print("BattlefieldVisuals3D: Applied bright sky gradient to Sprite3D")

func _setup_ground() -> void:
	# Load grass tile texture (existing texture from 2D implementation)
	var grass_texture = load("res://assets/textures/grass_tile.png")
	if grass_texture:
		ground_layer.texture = grass_texture
		print("BattlefieldVisuals3D: Applied grass tile texture to ground Sprite3D")
	else:
		push_warning("BattlefieldVisuals3D: grass_tile.png not found, ground will be invisible")

## Get the gameplay layer where units should be spawned
func get_gameplay_layer() -> Node3D:
	return gameplay_layer

## Get the UI layer for battlefield UI elements
func get_ui_layer() -> CanvasLayer:
	return ui_layer
