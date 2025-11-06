extends CharacterBody3D

## Simple test character for Paper Mario style setup

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport

func _ready() -> void:
	print("=== Paper Mario Character Debug ===")
	print("Sprite3D exists: ", sprite_3d != null)
	print("Viewport exists: ", viewport != null)
	print("Viewport path: ", viewport.get_path())
	print("Viewport size: ", viewport.size)

	# Set up the viewport texture on the Sprite3D
	var viewport_texture = ViewportTexture.new()
	viewport_texture.viewport_path = viewport.get_path()
	sprite_3d.texture = viewport_texture

	print("Texture set on Sprite3D: ", sprite_3d.texture != null)
	print("Sprite3D pixel_size: ", sprite_3d.pixel_size)
	print("Sprite3D position: ", sprite_3d.position)
	print("Sprite3D rotation: ", sprite_3d.rotation_degrees)
	print("Character position: ", global_position)
	print("===================================")
