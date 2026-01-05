extends Control
class_name BattlefieldDropZone

## Drop zone overlay for the battlefield that handles card drops

var summoner: Summoner = null
var camera_2d: Camera2D = null
var camera_3d: Camera3D = null
var is_3d: bool = false
var _initialized: bool = false  # Track initialization state

## Spawn preview for summon cards (C# class: ProjectSummoner.SpawnPreview.SpawnPreview)
var _spawn_preview: Node3D = null  # Typed as Node3D for GDScript/C# interop
var _preview_card: Card = null  # Track which card we're previewing

## Spell preview for spell cards
var _spell_preview: SpellPreview = null

## Spawn zone overlay (shows valid/invalid zones while dragging summon cards)
var _spawn_zone_overlay: SpawnZoneOverlay = null

func _ready() -> void:
	# Minimal setup - just configure mouse filter
	# STOP filter is needed to receive drop events, but we're behind HandUI
	# so HandUI will receive mouse events in its area first
	mouse_filter = Control.MOUSE_FILTER_STOP

func _notification(what: int) -> void:
	if what == NOTIFICATION_DRAG_END:
		_cleanup_spawn_preview()
		_cleanup_spell_preview()
		_cleanup_spawn_zone_overlay()

## Initialize BattlefieldDropZone with the player summoner
## Called by BattleCoordinator after summoners are ready
func init(player_summoner: Node) -> void:
	if _initialized:
		return
	_initialized = true

	if not player_summoner:
		push_error("BattlefieldDropZone: init() called with null summoner!")
		return

	summoner = player_summoner

	# Find camera (2D or 3D) - these are scene-level, not coordinator-dependent
	camera_2d = get_viewport().get_camera_2d()
	camera_3d = get_viewport().get_camera_3d()
	is_3d = camera_3d != null and camera_2d == null

	if not camera_2d and not camera_3d:
		push_error("BattlefieldDropZone: Could not find camera!")

## Handle mouse events for two-stage spell targeting
func _gui_input(event: InputEvent) -> void:
	# Forward input to SpellTargetingManager if it's active
	if not SpellTargetingManager or not SpellTargetingManager.get("is_active"):
		return

	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		var world_pos: Vector3 = _screen_to_world_3d(mouse_event.position)

		if world_pos == Vector3.ZERO:
			return

		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			if mouse_event.pressed:
				# Mouse down - place circle and start arrow drag
				if SpellTargetingManager.has_method("handle_mouse_down"):
					SpellTargetingManager.call("handle_mouse_down", world_pos)
					accept_event()
			else:
				# Mouse up - confirm rally destination
				if SpellTargetingManager.has_method("handle_mouse_up"):
					SpellTargetingManager.call("handle_mouse_up", world_pos)
					accept_event()
		elif mouse_event.button_index == MOUSE_BUTTON_RIGHT and mouse_event.pressed:
			# Cancel targeting
			if SpellTargetingManager.has_method("_cancel_targeting"):
				SpellTargetingManager.call("_cancel_targeting")
				accept_event()

	elif event is InputEventMouseMotion:
		# Update arrow endpoint during drag
		var mouse_motion: InputEventMouseMotion = event
		var world_pos: Vector3 = _screen_to_world_3d(mouse_motion.position)

		if world_pos != Vector3.ZERO and SpellTargetingManager.has_method("handle_mouse_move"):
			SpellTargetingManager.call("handle_mouse_move", world_pos)
			accept_event()

## Check if we can drop the card here
func _can_drop_data(at_position: Vector2, data: Variant) -> bool:
	# Validate drop data
	if not data is Dictionary:
		_cleanup_spawn_preview()
		return false

	var data_dict: Dictionary = data

	# Handle debug spawn from UnitSpawnerPanel
	if data_dict.get("type") == "debug_spawn":
		return _can_drop_debug_spawn(at_position, data_dict)

	if not data_dict.has("card_index") or not data_dict.has("card") or not data_dict.has("source"):
		_cleanup_spawn_preview()
		return false

	var source_variant: Variant = data_dict.get("source")
	if not source_variant is String:
		_cleanup_spawn_preview()
		return false
	var source: String = source_variant
	if source != "hand":
		_cleanup_spawn_preview()
		return false

	# Check if we have a summoner
	if not summoner:
		_cleanup_spawn_preview()
		return false

	var is_enabled_variant: Variant = summoner.get("is_enabled")
	if not is_enabled_variant is bool:
		push_warning("BattlefieldDropZone: summoner missing is_enabled property")
	var is_enabled: bool = is_enabled_variant if is_enabled_variant is bool else false  # Fail safe
	if not is_enabled:
		_cleanup_spawn_preview()
		return false

	# Get the card
	var card_index_variant: Variant = data_dict.get("card_index")
	if not card_index_variant is int:
		_cleanup_spawn_preview()
		return false
	var card_index: int = card_index_variant

	var hand_variant: Variant = summoner.get("hand")
	var hand: Array = hand_variant if hand_variant is Array else []
	if card_index < 0 or card_index >= hand.size():
		_cleanup_spawn_preview()
		return false

	var card_variant: Variant = data_dict.get("card")
	if not card_variant is Card:
		_cleanup_spawn_preview()
		return false
	var card: Card = card_variant

	# Check if we can afford it
	var mana_variant: Variant = summoner.get("mana")
	var mana: float = mana_variant if mana_variant is float else 0.0
	if mana < card.mana_cost:
		_cleanup_spawn_preview()
		return false

	# Handle preview based on card type
	if is_3d:
		var world_pos: Vector3 = _screen_to_world_3d(at_position)

		if card.card_type == Card.CardType.SUMMON:
			# Clean up spell preview if switching card types
			_cleanup_spell_preview()
			# Check if cursor is in valid territory (for visual feedback)
			var is_valid_zone: bool = BattlefieldConstants.is_valid_spawn_position_for_team(world_pos, UnitConstants.Team.PLAYER)
			# Clamp position to valid zone for actual spawn location
			var clamped_pos: Vector3 = BattlefieldConstants.clamp_spawn_position_for_team(world_pos, UnitConstants.Team.PLAYER)
			# Show preview at clamped position with color based on cursor validity
			# Red = cursor over invalid zone (unit will snap), Blue = cursor over valid zone
			_update_spawn_preview(clamped_pos, card, is_valid_zone)
			# Show spawn zone overlay while dragging summon cards (red overlay shows invalid territory)
			_show_spawn_zone_overlay()
		elif card.card_type == Card.CardType.SPELL:
			# Clean up spawn preview if switching card types
			_cleanup_spawn_preview()
			_cleanup_spawn_zone_overlay()
			# Show spell targeting preview
			_update_spell_preview(world_pos, card)

	# Valid drop (snapping handles invalid zones)
	return true

## Handle the card drop
func _drop_data(at_position: Vector2, data: Variant) -> void:
	if not _can_drop_data(at_position, data):
		return

	if not data is Dictionary:
		return
	var data_dict: Dictionary = data

	# Handle debug spawn from UnitSpawnerPanel
	if data_dict.get("type") == "debug_spawn":
		_drop_debug_spawn(at_position, data_dict)
		return

	var card_index_variant: Variant = data_dict.get("card_index")
	if not card_index_variant is int:
		return
	var card_index: int = card_index_variant

	# Get the card to check if it needs click-targeting
	var card_variant: Variant = data_dict.get("card")
	if not card_variant is Card:
		return
	var card: Card = card_variant

	# Check if this card needs click-targeting (Rally/Guard with command_type)
	var needs_targeting: bool = _card_needs_click_targeting(card)

	if needs_targeting and SpellTargetingManager and is_3d:
		# Delegate to targeting manager for click-targeting
		SpellTargetingManager.start_targeting(card, card_index, summoner, camera_3d)
	elif is_3d:
		# Immediate play in 3D
		var world_pos_3d: Vector3 = _screen_to_world_3d(at_position)
		# Clamp spawn position for summon cards only (spells can target anywhere)
		if card.card_type == Card.CardType.SUMMON:
			world_pos_3d = BattlefieldConstants.clamp_spawn_position_for_team(world_pos_3d, UnitConstants.Team.PLAYER)
		if summoner.has_method("play_card_3d"):
			summoner.call("play_card_3d", card_index, world_pos_3d)
	else:
		# Immediate play in 2D
		var world_pos_2d: Vector2 = _screen_to_world_2d(at_position)
		if summoner.has_method("play_card"):
			summoner.call("play_card", card_index, world_pos_2d)

## Convert screen coordinates to 2D world coordinates
func _screen_to_world_2d(screen_pos: Vector2) -> Vector2:
	if not camera_2d:
		return screen_pos

	var viewport_center: Vector2 = get_viewport().get_visible_rect().size / 2
	var offset_from_center: Vector2 = (screen_pos - viewport_center) / camera_2d.zoom
	return camera_2d.global_position + offset_from_center

## Convert screen coordinates to 3D world coordinates
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Create a ray from camera through mouse position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var to: Vector3 = from + camera_3d.project_ray_normal(screen_pos) * BattlefieldConstants.RAYCAST_DISTANCE

	# Intersect with spawn plane (where units spawn)
	var spawn_y: float = BattlefieldConstants.SPAWN_PLANE_HEIGHT
	var t: float = (spawn_y - from.y) / (to.y - from.y)
	if t < 0 or t > 1:
		return Vector3.ZERO

	var hit_pos: Vector3 = from + (to - from) * t
	return hit_pos

## Check if a card needs click-targeting (Rally/Guard with command_type)
func _card_needs_click_targeting(card: Card) -> bool:
	return card.needs_click_targeting()

## Update spawn preview position and visibility
func _update_spawn_preview(world_pos: Vector3, card: Card, is_valid_zone: bool = true, team: int = 0) -> void:
	if world_pos == Vector3.ZERO:
		_cleanup_spawn_preview()
		return

	# Create preview if needed or if card changed
	if not _spawn_preview or _preview_card != card:
		_cleanup_spawn_preview()
		_create_spawn_preview(card, team)

	if not _spawn_preview:
		return

	# Calculate safe spawn positions per-unit (same logic as actual spawning in card.gd)
	var positions: Array[Vector3] = _calculate_safe_spawn_positions(world_pos, card)
	_spawn_preview.UpdatePositions(positions)  # C# uses PascalCase
	_spawn_preview.SetValid(is_valid_zone)  # C# uses PascalCase

## Create a new spawn preview for the card
func _create_spawn_preview(card: Card, team: int = 0) -> void:
	if not card.unit_scene:
		return

	# Load C# SpawnPreview class via script path for GDScript/C# interop
	var spawn_preview_script: Script = load("res://scripts/csharp/SpawnPreview/SpawnPreview.cs")
	_spawn_preview = spawn_preview_script.new()
	_preview_card = card

	# Add to 3D scene (find a suitable parent)
	var viewport: Viewport = get_viewport()
	if viewport:
		var root_3d: Node = _find_3d_root(viewport)
		if root_3d:
			root_3d.add_child(_spawn_preview)
			_spawn_preview.Setup(card.unit_scene, card.spawn_count, team)  # C# uses PascalCase

## Find a suitable 3D root node to parent the preview
func _find_3d_root(viewport: Viewport) -> Node:
	# Try to find the battlefield or any Node3D
	var root: Node = viewport.get_tree().current_scene
	if root:
		# Look for battlefield - must be a Node3D
		var battlefield: Node = root.find_child("Battlefield3D", true, false)
		if battlefield and battlefield is Node3D:
			return battlefield

		# Try pattern match but verify it's Node3D
		var battlefield_match: Node = root.find_child("Battlefield*", true, false)
		if battlefield_match and battlefield_match is Node3D:
			return battlefield_match

		# Fallback to scene root if it's Node3D
		if root is Node3D:
			return root

		# Search for any Node3D child
		for child: Node in root.get_children():
			if child is Node3D:
				return child

	return null

## Calculate safe spawn positions for preview (matches card.gd spawn logic)
func _calculate_safe_spawn_positions(center_pos: Vector3, card: Card) -> Array[Vector3]:
	var positions: Array[Vector3] = []
	var scene_tree: SceneTree = get_tree()

	if not scene_tree:
		# Fallback: just return center position for each unit
		for i: int in card.spawn_count:
			positions.append(center_pos + Card.generate_formation_offset(i, card.spawn_count))
		return positions

	# Get collision_radius from the unit scene (instantiate temporarily to check)
	var collision_radius: float = 0.5
	if card.unit_scene:
		var temp_unit: Node = card.unit_scene.instantiate()
		if temp_unit and "collision_radius" in temp_unit:
			collision_radius = temp_unit.get("collision_radius")
		# Also try C# property name
		if temp_unit and "CollisionRadius" in temp_unit:
			collision_radius = temp_unit.get("CollisionRadius")
		if temp_unit:
			temp_unit.queue_free()

	# Calculate position for each unit (same logic as card.gd lines 281-288)
	for i: int in card.spawn_count:
		var offset: Vector3 = Card.generate_formation_offset(i, card.spawn_count)
		var desired_pos: Vector3 = center_pos + offset
		var safe_pos: Vector3 = BattlefieldConstants.find_safe_spawn_position(desired_pos, scene_tree, collision_radius)
		positions.append(safe_pos)

	return positions

## Clean up the spawn preview
func _cleanup_spawn_preview() -> void:
	if _spawn_preview and is_instance_valid(_spawn_preview):
		_spawn_preview.Cleanup()  # C# uses PascalCase
	_spawn_preview = null
	_preview_card = null

## Show spawn zone overlay (valid/invalid zones on the ground)
func _show_spawn_zone_overlay() -> void:
	if _spawn_zone_overlay:
		return  # Already showing

	_spawn_zone_overlay = SpawnZoneOverlay.new()

	# Add to 3D scene
	var viewport: Viewport = get_viewport()
	if viewport:
		var root_3d: Node = _find_3d_root(viewport)
		if root_3d:
			root_3d.add_child(_spawn_zone_overlay)

## Clean up the spawn zone overlay
func _cleanup_spawn_zone_overlay() -> void:
	if _spawn_zone_overlay and is_instance_valid(_spawn_zone_overlay):
		_spawn_zone_overlay.cleanup()
	_spawn_zone_overlay = null


## Update spell preview position
func _update_spell_preview(world_pos: Vector3, card: Card) -> void:
	if world_pos == Vector3.ZERO:
		_cleanup_spell_preview()
		return

	# Create preview if needed or if card changed
	if not _spell_preview or _preview_card != card:
		_cleanup_spell_preview()
		_create_spell_preview(card)

	if not _spell_preview:
		return

	_spell_preview.update_position(world_pos)
	# Spells can be cast anywhere
	_spell_preview.set_valid(true)


## Create a new spell preview for the card
func _create_spell_preview(card: Card) -> void:
	_spell_preview = SpellPreview.new()
	_preview_card = card

	# Add to 3D scene
	var viewport: Viewport = get_viewport()
	if viewport:
		var root_3d: Node = _find_3d_root(viewport)
		if root_3d:
			root_3d.add_child(_spell_preview)
			# Use spell radius if available, otherwise default
			var radius: float = card.spell_radius if card.spell_radius > 0 else SpellPreview.DEFAULT_RADIUS
			_spell_preview.setup(radius)


## Clean up the spell preview
func _cleanup_spell_preview() -> void:
	if _spell_preview and is_instance_valid(_spell_preview):
		_spell_preview.cleanup()
	_spell_preview = null


# =============================================================================
# DEBUG SPAWN HANDLING
# =============================================================================

## Check if we can accept a debug spawn drop
func _can_drop_debug_spawn(at_position: Vector2, data: Dictionary) -> bool:
	# Always allow debug spawns if we're in a debug arena
	var arena: DebugArenaController = _find_debug_arena_controller()
	if not arena:
		return false

	# Get card from drag data (created by UnitSpawnerPanel)
	var card: Card = data.get("card")
	if not card:
		return false

	# Show spawn preview for debug spawns
	if is_3d:
		var world_pos: Vector3 = _screen_to_world_3d(at_position)
		if world_pos != Vector3.ZERO:
			# Debug spawns are always valid (no team restrictions)
			var team: int = data.get("team", 1)  # Get team from drag data
			_update_spawn_preview(world_pos, card, true, team)

	return true


## Handle a debug spawn drop
func _drop_debug_spawn(at_position: Vector2, data: Dictionary) -> void:
	# Clean up preview before spawning
	_cleanup_spawn_preview()

	# Get card from drag data
	var card: Card = data.get("card")
	if not card:
		push_error("BattlefieldDropZone: No card in debug spawn data")
		return

	var team: int = data.get("team", 1)  # Default to enemy team

	# Convert screen position to world position
	var world_pos: Vector3 = _screen_to_world_3d(at_position)
	if world_pos == Vector3.ZERO:
		push_error("BattlefieldDropZone: Failed to convert screen position to world")
		return

	# Get battlefield for spawning
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if not battlefield:
		push_error("BattlefieldDropZone: No battlefield found for debug spawn")
		return

	# Get modifier system (optional)
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")

	# Convert team int to UnitConstants.Team enum
	var unit_team: UnitConstants.Team = UnitConstants.Team.PLAYER if team == 0 else UnitConstants.Team.ENEMY

	# Spawn with no animation - debug spawns should appear immediately
	# (spawn reveal tweens don't run when game is paused)
	const DEBUG_SPAWN_DURATION: float = 0.0
	card.play_3d(world_pos, unit_team, battlefield, modifier_system, DEBUG_SPAWN_DURATION)

	# Activate newly spawned units immediately (debug mode bypasses prep phase)
	await get_tree().process_frame
	_activate_recent_spawns()


## Find the DebugArenaController in the scene
func _find_debug_arena_controller() -> DebugArenaController:
	var controllers: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.GAME_CONTROLLER)
	for controller: Node in controllers:
		if controller is DebugArenaController:
			return controller
	return null


## Activate recently spawned units (for debug mode)
func _activate_recent_spawns() -> void:
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	for unit: Node in units:
		# Activate if the unit has the method and isn't already active
		if unit.has_method("Activate") and unit.has_method("IsActive"):
			if not unit.IsActive():
				unit.Activate()
