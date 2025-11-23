extends Node

## Manages two-stage click-targeting for spells like Rally and Guard
##
## Flow:
## 1. Card is dropped → start_targeting() is called
## 2. Blue circle preview follows mouse
## 3. Left-click (down) → place circle, select units, start dragging arrow
## 4. Drag mouse → arrow extends from circle to cursor
## 5. Release (up) → confirm rally destination, cast spell
## 6. Right-click or Escape cancels → card returned to hand

## Emitted when targeting is cancelled (so card can be returned to hand)
signal targeting_cancelled(card_index: int)

## Targeting states
enum State {
	INACTIVE,              # Not targeting
	AWAITING_FIRST_CLICK,  # Preview circle follows mouse
	DRAGGING_ARROW         # Circle placed, dragging to set destination
}

## State
var current_state: State = State.INACTIVE
var is_active: bool = false
var pending_card_index: int = -1
var pending_card: Card = null
var summoner: Node = null
var camera_3d: Camera3D = null

## Two-stage targeting data
var circle_center: Vector3 = Vector3.ZERO
var rally_destination: Vector3 = Vector3.ZERO
var selected_units: Array[Node] = []  # Units selected at first click

## Preview visuals
var preview_circle: Node3D = null
var arrow_visual: Node3D = null
var selection_radius: float = 8.0

## Constants
const RAYCAST_DISTANCE: float = 1000.0
const SPAWN_PLANE_HEIGHT: float = 0.0

func _ready() -> void:
	set_process(false)  # Only process when active

## Start two-stage targeting mode
func start_targeting(card: Card, card_index: int, p_summoner: Node, p_camera: Camera3D) -> void:
	is_active = true
	current_state = State.AWAITING_FIRST_CLICK
	pending_card_index = card_index
	pending_card = card
	summoner = p_summoner
	camera_3d = p_camera

	# Get selection radius from card definition in catalog
	var card_def: Dictionary = CardCatalog.get_card(card.catalog_id)
	var radius_variant: Variant = card_def.get("selection_radius", 8.0)
	selection_radius = radius_variant if radius_variant is float else 8.0

	# Reset state
	circle_center = Vector3.ZERO
	rally_destination = Vector3.ZERO
	selected_units.clear()

	_show_preview_circle()
	set_process(true)

	print("SpellTargetingManager: Started targeting for %s (radius: %.1f)" % [card.card_name, selection_radius])

## Update preview visuals based on state
func _process(_delta: float) -> void:
	if not is_active:
		return

	var mouse_pos: Vector2 = get_viewport().get_mouse_position()
	var world_pos: Vector3 = _screen_to_world_3d(mouse_pos)

	if world_pos == Vector3.ZERO:
		if current_state == State.DRAGGING_ARROW and rally_destination != Vector3.ZERO:
			print("SpellTargetingManager[%s]: _process() got Vector3.ZERO from _screen_to_world_3d(), NOT updating rally_destination (current: %s)" % [get_instance_id(), rally_destination])
		return

	match current_state:
		State.AWAITING_FIRST_CLICK:
			# Preview circle follows mouse
			if preview_circle:
				preview_circle.global_position = world_pos
		State.DRAGGING_ARROW:
			# Update arrow endpoint to follow cursor
			var old_dest: Vector3 = rally_destination
			rally_destination = world_pos
			if old_dest != rally_destination:
				print("SpellTargetingManager[%s]: _process() updated rally_destination from %s to %s" % [get_instance_id(), old_dest, rally_destination])
			_update_arrow_visual()

## Handle input - forwarded from BattlefieldDropZone via _gui_input
func _confirm_target(world_pos: Vector3) -> void:
	print("SpellTargetingManager: Target confirmed at %s" % world_pos)

	if summoner and summoner.has_method("play_card_3d"):
		summoner.call("play_card_3d", pending_card_index, world_pos)

	_end_targeting()

## Cancel targeting
func _cancel_targeting() -> void:
	print("SpellTargetingManager: Targeting cancelled")
	targeting_cancelled.emit(pending_card_index)
	_end_targeting()

## Clean up targeting state
func _end_targeting() -> void:
	print("SpellTargetingManager[%s]: _end_targeting() called - rally_destination is currently: %s" % [get_instance_id(), rally_destination])
	is_active = false
	current_state = State.INACTIVE
	pending_card_index = -1
	pending_card = null
	summoner = null
	camera_3d = null
	circle_center = Vector3.ZERO
	# Don't clear rally_destination here - let the spell retrieve and clear it
	# rally_destination = Vector3.ZERO
	selected_units.clear()
	_hide_preview_circle()
	_hide_arrow_visual()
	set_process(false)
	print("SpellTargetingManager[%s]: _end_targeting() finished - rally_destination is now: %s" % [get_instance_id(), rally_destination])

## Get the current rally destination (for spells to query)
## Returns Vector3.ZERO if no rally destination is set
func get_rally_destination() -> Vector3:
	print("SpellTargetingManager[%s]: get_rally_destination() called, returning: %s" % [get_instance_id(), rally_destination])
	return rally_destination

## Clear the rally destination after it's been used
func clear_rally_destination() -> void:
	print("SpellTargetingManager: clear_rally_destination() called - clearing %s" % rally_destination)
	rally_destination = Vector3.ZERO

## Reset the SpellTargetingManager to initial state
## Called between battles to clear any persisted state
func reset() -> void:
	print("SpellTargetingManager: Resetting state...")

	# Cancel any active targeting
	if is_active:
		_cancel_targeting()

	# Clear all state (defensive reset)
	is_active = false
	current_state = State.INACTIVE
	pending_card_index = -1
	pending_card = null
	summoner = null
	camera_3d = null
	circle_center = Vector3.ZERO
	rally_destination = Vector3.ZERO
	selected_units.clear()
	_hide_preview_circle()
	_hide_arrow_visual()
	set_process(false)

	print("SpellTargetingManager: Reset complete")

## Handle first click - place circle and select units
func handle_mouse_down(world_pos: Vector3) -> void:
	if current_state != State.AWAITING_FIRST_CLICK:
		return

	print("SpellTargetingManager: First click - placing circle at %s" % world_pos)

	# Place circle at clicked position
	circle_center = world_pos
	rally_destination = world_pos  # Initialize to same position

	# Move preview circle to placed position and make it semi-permanent
	if preview_circle:
		preview_circle.global_position = circle_center

	# Select units within radius
	_select_units_in_radius()

	# Create arrow visual
	_show_arrow_visual()

	# Transition to dragging state
	current_state = State.DRAGGING_ARROW
	print("SpellTargetingManager: %d units selected, now dragging arrow" % selected_units.size())

## Handle drag - update arrow endpoint
func handle_mouse_move(world_pos: Vector3) -> void:
	if current_state != State.DRAGGING_ARROW:
		return

	rally_destination = world_pos
	_update_arrow_visual()

## Handle release - confirm rally destination and cast spell
func handle_mouse_up(world_pos: Vector3) -> void:
	if current_state != State.DRAGGING_ARROW:
		return

	print("SpellTargetingManager: Mouse up - rally destination: %s" % world_pos)
	rally_destination = world_pos

	# Cast the spell with both positions
	_cast_spell()

## Cast the spell with circle center and rally destination
func _cast_spell() -> void:
	if not summoner or not pending_card:
		_end_targeting()
		return

	print("SpellTargetingManager[%s]: Casting spell - circle: %s, destination: %s, units: %d" % [
		get_instance_id(), circle_center, rally_destination, selected_units.size()
	])

	# CRITICAL: Stop processing BEFORE calling play_card_3d to prevent race conditions
	# where _process() could modify rally_destination while the card is reading it
	set_process(false)

	# Rally destination is stored in this singleton and will be retrieved by the spell

	# Call play_card_3d with circle center as the position
	if summoner.has_method("play_card_3d"):
		summoner.call("play_card_3d", pending_card_index, circle_center)

	_end_targeting()

## Select all units within radius of circle center
func _select_units_in_radius() -> void:
	selected_units.clear()

	# Find all units in the scene
	var all_units: Array[Node] = get_tree().get_nodes_in_group("units")

	for unit_node: Node in all_units:
		if not unit_node is Node3D:
			continue

		var unit: Node3D = unit_node
		var distance: float = unit.global_position.distance_to(circle_center)

		if distance <= selection_radius:
			selected_units.append(unit)
			# TODO: Add highlight VFX to unit
			print("SpellTargetingManager: Selected unit at distance %.1f" % distance)

## Show preview circle
func _show_preview_circle() -> void:
	if preview_circle:
		preview_circle.queue_free()

	# Create placeholder cylinder
	preview_circle = Node3D.new()
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var cylinder: CylinderMesh = CylinderMesh.new()

	# Configure cylinder to show selection radius
	cylinder.top_radius = selection_radius
	cylinder.bottom_radius = selection_radius
	cylinder.height = 0.1  # Very thin, just a visual indicator

	mesh_instance.mesh = cylinder

	# Create semi-transparent blue material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = Color(0.3, 0.6, 1.0, 0.3)  # Blue with 30% opacity
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED  # Visible from both sides
	mesh_instance.material_override = material

	preview_circle.add_child(mesh_instance)

	# Add to scene
	get_tree().root.add_child(preview_circle)

## Hide preview circle
func _hide_preview_circle() -> void:
	if preview_circle:
		preview_circle.queue_free()
		preview_circle = null

## Show arrow visual from circle to cursor
func _show_arrow_visual() -> void:
	if arrow_visual:
		arrow_visual.queue_free()

	arrow_visual = Node3D.new()

	# Create a simple cylinder as the arrow shaft
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.top_radius = 0.2
	cylinder.bottom_radius = 0.2
	cylinder.height = 1.0  # Will scale this based on distance

	mesh_instance.mesh = cylinder

	# Create bright green material for visibility
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = Color(0.2, 1.0, 0.2, 0.8)  # Bright green
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	mesh_instance.material_override = material

	arrow_visual.add_child(mesh_instance)
	get_tree().root.add_child(arrow_visual)

	_update_arrow_visual()

## Update arrow visual to point from circle to destination
func _update_arrow_visual() -> void:
	if not arrow_visual:
		return

	var direction: Vector3 = rally_destination - circle_center
	var distance: float = direction.length()

	if distance < 0.5:  # Too short to show (reduced threshold)
		arrow_visual.visible = false
		return

	arrow_visual.visible = true

	# Position at midpoint
	arrow_visual.global_position = circle_center + direction * 0.5

	# Reset scale and rotation
	arrow_visual.scale = Vector3.ONE
	arrow_visual.rotation = Vector3.ZERO

	# Orient cylinder to point from circle to destination
	# Cylinder is Y-up by default, so we need to align Y-axis with direction
	var normalized_dir: Vector3 = direction.normalized()

	# Create basis that points Y-axis along direction
	var up: Vector3 = Vector3.UP
	if abs(normalized_dir.dot(up)) > 0.99:  # Nearly parallel, use different up
		up = Vector3.RIGHT

	var right: Vector3 = up.cross(normalized_dir).normalized()
	var forward: Vector3 = normalized_dir.cross(right).normalized()

	arrow_visual.basis = Basis(right, normalized_dir, forward)

	# Scale to match distance
	arrow_visual.scale = Vector3(1, distance, 1)

## Hide arrow visual
func _hide_arrow_visual() -> void:
	if arrow_visual:
		arrow_visual.queue_free()
		arrow_visual = null

## Convert screen coordinates to 3D world coordinates
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Create a ray from camera through screen position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var to: Vector3 = from + camera_3d.project_ray_normal(screen_pos) * RAYCAST_DISTANCE

	# Intersect with spawn plane (where units spawn)
	var spawn_y: float = SPAWN_PLANE_HEIGHT
	var t: float = (spawn_y - from.y) / (to.y - from.y)
	if t < 0 or t > 1:
		return Vector3.ZERO

	var hit_pos: Vector3 = from + (to - from) * t
	return hit_pos
