extends CharacterBody3D

## Simple test character for Paper Mario style setup

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var model_2d: Node2D = $Sprite3D/SubViewport/2DModel
@onready var test_sprite: ColorRect = $Sprite3D/SubViewport/2DModel/TestSprite

func _ready() -> void:
	print("=== Paper Mario Character Debug ===")
	print("Sprite3D exists: ", sprite_3d != null)
	print("Viewport exists: ", viewport != null)
	print("Viewport path: ", viewport.get_path())
	print("Viewport size: ", viewport.size)
	print("Viewport transparent_bg: ", viewport.transparent_bg)

	# Check 2D content
	print("\n2D Content:")
	print("2DModel exists: ", model_2d != null)
	print("2DModel visible: ", model_2d.visible if model_2d else "N/A")
	print("TestSprite exists: ", test_sprite != null)
	print("TestSprite visible: ", test_sprite.visible if test_sprite else "N/A")
	print("TestSprite size: ", test_sprite.size if test_sprite else "N/A")
	print("TestSprite color: ", test_sprite.color if test_sprite else "N/A")

	print("\n3D Sprite:")
	print("Texture set on Sprite3D: ", sprite_3d.texture != null)
	print("Sprite3D pixel_size: ", sprite_3d.pixel_size)
	print("Sprite3D scale: ", sprite_3d.scale)
	print("Sprite3D position: ", sprite_3d.position)
	print("Sprite3D rotation: ", sprite_3d.rotation_degrees)
	print("Sprite3D visible: ", sprite_3d.visible)
	print("Sprite3D alpha_cut: ", sprite_3d.alpha_cut)
	print("Character position: ", global_position)
	print("===================================")

func _process(_delta: float) -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
