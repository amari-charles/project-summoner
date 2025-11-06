extends CharacterBody3D

## Simple test character for Paper Mario style setup

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport

func _ready() -> void:
	# Set up the viewport texture on the Sprite3D
	var viewport_texture = ViewportTexture.new()
	viewport_texture.viewport_path = viewport.get_path()
	sprite_3d.texture = viewport_texture

	print("Paper Mario character initialized")
	print("Viewport path: ", viewport.get_path())
