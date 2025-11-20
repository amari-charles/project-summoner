extends Node

## RedirectManager - Tactical unit redirection system (Autoload: "RedirectManager")
##
## Allows players to tactically redirect units during battle:
## - Press button → enter redirect mode
## - Click → select units in radius
## - Drag → show target direction
## - Release → units attack nearest enemy for 3 seconds
##
## Features:
## - Separate Attack/Defend buttons with independent cooldowns
## - Fallback targeting when primary target dies
## - Visual feedback for selection and targeting
## - Mobile-friendly (no cancel, committed on button press)

## =============================================================================
## SIGNALS
## =============================================================================

## Emitted when redirect mode changes
signal mode_changed(new_mode: RedirectMode)

## Emitted when cooldowns update (for UI)
signal cooldown_updated(attack_cd: float, defend_cd: float)

## Emitted when units are selected for redirect
signal units_selected(units: Array, center_point: Vector3)

## Emitted when redirect is applied
signal redirect_applied(units: Array, target: Node3D, is_attack: bool)

## =============================================================================
## ENUMS
## =============================================================================

enum RedirectMode {
	NORMAL,            ## No redirect active
	REDIRECT_ATTACK,   ## Waiting for attack redirect input
	REDIRECT_DEFEND    ## Waiting for defend redirect input
}

## =============================================================================
## CONFIGURATION
## =============================================================================

## Selection radius when clicking to redirect units
const REDIRECT_RADIUS: float = 8.0

## How long units commit to forced target
const FORCED_TARGET_DURATION: float = 3.0

## Attack redirect button cooldown
const ATTACK_COOLDOWN: float = 12.0

## Defend redirect button cooldown
const DEFEND_COOLDOWN: float = 8.0

## Maximum distance to search for targets at release point
const TARGET_SEARCH_RADIUS: float = 10.0

## Visual colors
const ATTACK_COLOR: Color = Color(0.2, 0.8, 1.0, 0.5)   # Blue
const DEFEND_COLOR: Color = Color(0.2, 1.0, 0.4, 0.5)   # Green
const TARGET_COLOR: Color = Color(1.0, 0.2, 0.2, 0.8)   # Red

## =============================================================================
## STATE
## =============================================================================

## Current redirect mode
var current_mode: RedirectMode = RedirectMode.NORMAL

## Attack redirect cooldown timer
var attack_cooldown: float = 0.0

## Defend redirect cooldown timer
var defend_cooldown: float = 0.0

## Units currently selected for redirect
var selected_units: Array[Unit3D] = []

## Point where redirect drag started
var redirect_start_point: Vector3 = Vector3.ZERO

## Point where units were redirected to (for fallback targeting)
var original_redirect_point: Vector3 = Vector3.ZERO

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("RedirectManager: Initialized")

func _process(delta: float) -> void:
	# Update cooldowns
	if attack_cooldown > 0.0:
		attack_cooldown -= delta
		if attack_cooldown < 0.0:
			attack_cooldown = 0.0

	if defend_cooldown > 0.0:
		defend_cooldown -= delta
		if defend_cooldown < 0.0:
			defend_cooldown = 0.0

	# Emit cooldown updates every frame for UI
	cooldown_updated.emit(attack_cooldown, defend_cooldown)

## =============================================================================
## PUBLIC API
## =============================================================================

## Activate attack redirect mode
## Returns true if successful, false if on cooldown
func activate_redirect_attack() -> bool:
	if attack_cooldown > 0.0:
		push_warning("RedirectManager: Attack redirect on cooldown (%.1fs remaining)" % attack_cooldown)
		return false

	current_mode = RedirectMode.REDIRECT_ATTACK
	attack_cooldown = ATTACK_COOLDOWN
	mode_changed.emit(current_mode)
	print("RedirectManager: Attack redirect activated")
	return true

## Activate defend redirect mode
## Returns true if successful, false if on cooldown
func activate_redirect_defend() -> bool:
	if defend_cooldown > 0.0:
		push_warning("RedirectManager: Defend redirect on cooldown (%.1fs remaining)" % defend_cooldown)
		return false

	current_mode = RedirectMode.REDIRECT_DEFEND
	defend_cooldown = DEFEND_COOLDOWN
	mode_changed.emit(current_mode)
	print("RedirectManager: Defend redirect activated")
	return true

## Cancel current redirect mode (return to normal)
func cancel_redirect() -> void:
	current_mode = RedirectMode.NORMAL
	selected_units.clear()
	redirect_start_point = Vector3.ZERO
	mode_changed.emit(current_mode)
	print("RedirectManager: Redirect cancelled")

## Select all friendly units in radius around a point
## Returns array of selected units
func select_units_in_radius(point: Vector3, radius: float, team: int) -> Array[Unit3D]:
	selected_units.clear()

	var all_units: Array[Node] = get_tree().get_nodes_in_group("units")
	for node: Node in all_units:
		if not node is Unit3D:
			continue

		var unit: Unit3D = node as Unit3D

		# Only select friendly units that are alive
		if unit.team != team or not unit.is_alive:
			continue

		# Check if unit is within radius
		var distance: float = point.distance_to(unit.global_position)
		if distance <= radius:
			selected_units.append(unit)

	print("RedirectManager: Selected %d units in radius %.1f" % [selected_units.size(), radius])
	units_selected.emit(selected_units, point)
	return selected_units

## Find nearest enemy (unit or structure) to a point
## Returns the nearest enemy node, or null if none found
func find_nearest_enemy(point: Vector3, team: int, search_radius: float) -> Node3D:
	var nearest: Node3D = null
	var nearest_dist: float = INF

	# Search for enemy units
	var all_units: Array[Node] = get_tree().get_nodes_in_group("units")
	for node: Node in all_units:
		if not node is Unit3D:
			continue

		var unit: Unit3D = node as Unit3D

		# Only consider enemy units that are alive
		if unit.team == team or not unit.is_alive:
			continue

		var dist: float = point.distance_to(unit.global_position)
		if dist < search_radius and dist < nearest_dist:
			nearest = unit
			nearest_dist = dist

	# Search for enemy structures
	var all_structures: Array[Node] = get_tree().get_nodes_in_group("structures")
	for node: Node in all_structures:
		if not node is Node3D:
			continue

		var structure: Node3D = node as Node3D

		# Check if structure has team property
		if not structure.has_meta("team"):
			continue

		var structure_team: int = structure.get_meta("team")
		if structure_team == team:
			continue

		var dist: float = point.distance_to(structure.global_position)
		if dist < search_radius and dist < nearest_dist:
			nearest = structure
			nearest_dist = dist

	# Search for enemy bases (Base3D uses @export var team, not meta)
	var all_bases: Array[Node] = get_tree().get_nodes_in_group("bases")
	for node: Node in all_bases:
		if not node is Node3D:
			continue

		var base: Node3D = node as Node3D

		# Check if base has team property
		if not "team" in base:
			continue

		var base_team_variant: Variant = base.get("team")
		var base_team: int = base_team_variant if base_team_variant is int else -1
		if base_team == team:
			continue

		var dist: float = point.distance_to(base.global_position)
		if dist < search_radius and dist < nearest_dist:
			nearest = base
			nearest_dist = dist

	if nearest:
		print("RedirectManager: Found nearest enemy at distance %.1f" % nearest_dist)
	else:
		push_warning("RedirectManager: No enemy found within search radius %.1f" % search_radius)

	return nearest

## Find fallback target when primary dies
## Searches for second-closest enemy in the original redirect area
func find_fallback_target(original_point: Vector3, team: int, search_radius: float, exclude: Node3D = null) -> Node3D:
	var fallback: Node3D = null
	var fallback_dist: float = INF

	# Search for enemy units (excluding the dead one)
	var all_units: Array[Node] = get_tree().get_nodes_in_group("units")
	for node: Node in all_units:
		if not node is Unit3D:
			continue

		var unit: Unit3D = node as Unit3D

		# Skip excluded unit, friendly units, and dead units
		if unit == exclude or unit.team == team or not unit.is_alive:
			continue

		var dist: float = original_point.distance_to(unit.global_position)
		if dist < search_radius and dist < fallback_dist:
			fallback = unit
			fallback_dist = dist

	# Search for enemy structures
	var all_structures: Array[Node] = get_tree().get_nodes_in_group("structures")
	for node: Node in all_structures:
		if not node is Node3D:
			continue

		var structure: Node3D = node as Node3D

		# Skip excluded structure
		if structure == exclude:
			continue

		# Check if structure has team property
		if not structure.has_meta("team"):
			continue

		var structure_team: int = structure.get_meta("team")
		if structure_team == team:
			continue

		var dist: float = original_point.distance_to(structure.global_position)
		if dist < search_radius and dist < fallback_dist:
			fallback = structure
			fallback_dist = dist

	# Search for enemy bases
	var all_bases: Array[Node] = get_tree().get_nodes_in_group("bases")
	for node: Node in all_bases:
		if not node is Node3D:
			continue

		var base: Node3D = node as Node3D

		# Skip excluded base
		if base == exclude:
			continue

		# Check if base has team property
		if not "team" in base:
			continue

		var base_team_variant: Variant = base.get("team")
		var base_team: int = base_team_variant if base_team_variant is int else -1
		if base_team == team:
			continue

		var dist: float = original_point.distance_to(base.global_position)
		if dist < search_radius and dist < fallback_dist:
			fallback = base
			fallback_dist = dist

	if fallback:
		print("RedirectManager: Found fallback target at distance %.1f" % fallback_dist)

	return fallback

## Apply forced targets to selected units
## Units will commit to the target for FORCED_TARGET_DURATION seconds
func apply_forced_targets(units: Array[Unit3D], target: Node3D, duration: float, original_point: Vector3) -> void:
	if not target:
		push_warning("RedirectManager: Cannot apply forced targets - no valid target")
		return

	var is_attack: bool = current_mode == RedirectMode.REDIRECT_ATTACK

	print("RedirectManager: Applying to ", units.size(), " units, target=", target.name, " duration=", duration)

	var applied_count: int = 0
	for unit: Unit3D in units:
		if not is_instance_valid(unit) or not unit.is_alive:
			print("RedirectManager: Skipping invalid/dead unit")
			continue

		unit.forced_target = target
		unit.forced_target_timer = duration
		unit.original_redirect_point = original_point

		# Reset target lock timer to force immediate re-acquisition
		unit.target_lock_timer = 0.0

		applied_count += 1

		print("RedirectManager: Unit ", unit.name, " forced_target=", str(unit.forced_target.name) if unit.forced_target else "null", " timer=", unit.forced_target_timer)

	print("RedirectManager: Applied forced targets to %d/%d units for %.1fs" % [applied_count, units.size(), duration])
	redirect_applied.emit(units, target, is_attack)

	# Return to normal mode
	cancel_redirect()

## Get current mode color for visual feedback
func get_current_mode_color() -> Color:
	match current_mode:
		RedirectMode.REDIRECT_ATTACK:
			return ATTACK_COLOR
		RedirectMode.REDIRECT_DEFEND:
			return DEFEND_COLOR
		_:
			return Color.WHITE

## Check if attack redirect is available (not on cooldown)
func is_attack_available() -> bool:
	return attack_cooldown <= 0.0

## Check if defend redirect is available (not on cooldown)
func is_defend_available() -> bool:
	return defend_cooldown <= 0.0
