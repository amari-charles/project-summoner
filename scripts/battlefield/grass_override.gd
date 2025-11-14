extends Node3D

## Override script to apply painterly grass shader after BiomeConfig loads
## Attach this to a child node of the scene root

@export var grass_material: ShaderMaterial

func _ready() -> void:
	print("GrassOverride: Script starting...")

	# Wait for BiomeConfig to apply first
	await get_tree().process_frame
	await get_tree().process_frame  # Wait an extra frame to be safe

	print("GrassOverride: Looking for BaseBattlefield3D...")

	# Find the Background mesh in BaseBattlefield3D (sibling node)
	var parent: Node = get_parent()
	if not parent:
		push_error("GrassOverride: No parent node")
		return

	var battlefield: Node3D = parent.get_node_or_null("BaseBattlefield3D")
	if not battlefield:
		push_error("GrassOverride: Could not find BaseBattlefield3D")
		print("GrassOverride: Parent children: ", parent.get_children())
		return

	print("GrassOverride: Found BaseBattlefield3D, looking for Background...")
	var background: MeshInstance3D = battlefield.get_node_or_null("Background")
	if not background:
		push_error("GrassOverride: Could not find Background mesh")
		print("GrassOverride: Battlefield children: ", battlefield.get_children())
		return

	print("GrassOverride: Found Background mesh")

	if not grass_material:
		push_error("GrassOverride: No grass_material assigned!")
		return

	print("GrassOverride: grass_material is assigned, applying...")
	# Override the material with our painterly grass shader
	background.set_surface_override_material(0, grass_material)

	# Set render priority to ensure grass renders behind everything
	# Lower values render first (behind), higher values render last (on top)
	# Material already has render_priority set, but we can ensure it's applied
	var mat: ShaderMaterial = background.get_surface_override_material(0)
	if mat:
		# Ensure render_priority is set (material should already have -10)
		if mat.render_priority == 0:
			mat.render_priority = -10
		print("GrassOverride: ✓ Render priority: ", mat.render_priority)

	print("GrassOverride: ✓ Applied painterly grass shader to Background mesh!")

	# Verify it was applied
	var current_mat: Material = background.get_surface_override_material(0)
	if current_mat == grass_material:
		print("GrassOverride: ✓ Material verification successful")
	else:
		push_error("GrassOverride: Material was not applied correctly!")
