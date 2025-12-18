extends Node3D
class_name GhostUnit3D

## Transparent preview of a unit during card drag
## Shows what the spawned unit will look like with ghostly transparency

const COMPONENT_SCENE_PATH: String = "res://scenes/units/sprite_character_2d5_component.tscn"

const VALID_TINT: Color = Color(0.7, 0.85, 1.0, 0.5)  # Light blue, 50% alpha
const INVALID_TINT: Color = Color(1.0, 0.5, 0.5, 0.5)  # Red, 50% alpha

var visual_component: SpriteCharacter2D5Component = null
var _visual_root: Node = null  # Store reference to the duplicated visual
var _is_valid: bool = true


## Initialize ghost with unit scene data
func setup(unit_scene: PackedScene) -> void:
	if not unit_scene:
		print("GhostUnit3D: No unit_scene provided")
		return

	# Instantiate unit to find its Visual child
	var temp_unit: Node = unit_scene.instantiate()
	if not temp_unit:
		print("GhostUnit3D: Failed to instantiate unit")
		return

	print("GhostUnit3D: Instantiated temp_unit=", temp_unit.name)

	# Find the Visual child node (works for both sprite and skeletal units)
	var visual_node: Node = temp_unit.get_node_or_null("Visual")
	if not visual_node:
		print("GhostUnit3D: No Visual child found")
		temp_unit.queue_free()
		return

	print("GhostUnit3D: Found Visual node: ", visual_node.get_class())

	# Get the scene file path to instantiate fresh (avoids ViewportTexture reference issues)
	var scene_path: String = visual_node.scene_file_path
	print("GhostUnit3D: Visual scene_path=", scene_path)

	var new_visual: Node = null

	if scene_path and scene_path != "":
		# Instantiate fresh from scene file
		var visual_scene: PackedScene = load(scene_path)
		if visual_scene:
			new_visual = visual_scene.instantiate()
			print("GhostUnit3D: Instantiated fresh visual from scene")

			# Copy relevant properties BEFORE adding to tree (so _ready() sees them)
			# Copy skeletal_scene or other visual properties
			if "skeletal_scene" in visual_node:
				var skel_scene: Variant = visual_node.get("skeletal_scene")
				if skel_scene:
					new_visual.set("skeletal_scene", skel_scene)
					print("GhostUnit3D: Copied skeletal_scene=", skel_scene)
			if "scale_factor" in visual_node:
				new_visual.set("scale_factor", visual_node.get("scale_factor"))
			# For sprite-based units
			if "sprite_frames" in visual_node:
				var frames: Variant = visual_node.get("sprite_frames")
				if frames:
					new_visual.set("sprite_frames", frames)
			if "sprite_scale" in visual_node:
				new_visual.set("sprite_scale", visual_node.get("sprite_scale"))
			if "viewport_scale" in visual_node:
				new_visual.set("viewport_scale", visual_node.get("viewport_scale"))

	if not new_visual:
		# Fallback to duplicate
		new_visual = visual_node.duplicate(DUPLICATE_USE_INSTANTIATION | DUPLICATE_SCRIPTS | DUPLICATE_SIGNALS)
		print("GhostUnit3D: Used duplicate fallback")

	if not new_visual:
		print("GhostUnit3D: Failed to create Visual")
		temp_unit.queue_free()
		return

	# Add to ghost (this triggers _ready() which needs skeletal_scene to be set)
	add_child(new_visual)

	# Store references
	_visual_root = new_visual
	if new_visual is SpriteCharacter2D5Component:
		visual_component = new_visual

	# Clean up temp unit
	temp_unit.queue_free()

	print("GhostUnit3D: Setup complete, visual_root=", _visual_root)

	# Apply ghost transparency AFTER the component fully initializes
	# SkeletalCharacter2D5Component uses await in _ready(), so we need to wait
	# for multiple frames for its visual children to be created
	_apply_ghost_appearance_deferred(new_visual)


## Apply ghost transparency after waiting for component to initialize
## SkeletalCharacter2D5Component uses await in _ready(), so children don't exist immediately
func _apply_ghost_appearance_deferred(node: Node) -> void:
	# Wait for the component to fully initialize (skeletal components await 2 frames)
	await get_tree().process_frame
	await get_tree().process_frame

	if not is_instance_valid(node):
		return

	print("GhostUnit3D: Applying ghost appearance after init wait")

	# Ensure ghost faces correct direction (player units face left in this game layout)
	if node.has_method("set_flip_h"):
		node.call("set_flip_h", true)

	_apply_ghost_appearance_to_node(node)


## Apply ghost transparency to a node and its children
func _apply_ghost_appearance_to_node(node: Node) -> void:
	var tint: Color = VALID_TINT if _is_valid else INVALID_TINT

	# For SkeletalCharacter2D5Component, modulate the skeletal_instance directly
	# IMPORTANT: Sprite3D renders from a ViewportTexture that's pre-rendered,
	# so modulating the Sprite3D has no effect. We must modulate the 2D content inside.
	if node is SkeletalCharacter2D5Component:
		var skeletal_comp: SkeletalCharacter2D5Component = node
		if skeletal_comp.skeletal_instance:
			skeletal_comp.skeletal_instance.modulate = tint
			print("GhostUnit3D: Applied tint to skeletal_instance")
		return  # Don't recurse into skeletal component

	# For SpriteCharacter2D5Component, modulate works on the 2D sprite inside viewport
	if node is SpriteCharacter2D5Component:
		var sprite_comp: SpriteCharacter2D5Component = node
		if sprite_comp.animated_sprite:
			sprite_comp.animated_sprite.modulate = tint
			print("GhostUnit3D: Applied tint to animated_sprite")
		return  # Don't recurse into sprite component

	# Apply to Sprite3D (fallback for other cases)
	if node is Sprite3D:
		var sprite3d: Sprite3D = node
		sprite3d.modulate = tint

	# Apply to CanvasItem children inside SubViewport (AnimatedSprite2D, Skeleton2D, etc.)
	if node is CanvasItem:
		var canvas_item: CanvasItem = node
		canvas_item.modulate = tint

	# Recursively apply to children
	for child: Node in node.get_children():
		_apply_ghost_appearance_to_node(child)


## Apply ghost transparency and tint
func _apply_ghost_appearance() -> void:
	if _visual_root:
		_apply_ghost_appearance_to_node(_visual_root)


## Set whether the spawn position is valid (changes tint color)
func set_valid(is_valid: bool) -> void:
	if _is_valid == is_valid:
		return
	_is_valid = is_valid
	_apply_ghost_appearance()


## Clean up resources
func cleanup() -> void:
	if _visual_root and is_instance_valid(_visual_root):
		_visual_root.queue_free()
	_visual_root = null
	visual_component = null
	queue_free()
