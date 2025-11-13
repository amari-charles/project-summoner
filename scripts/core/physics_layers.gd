extends Node

## Physics layer constants for 3D collision system (autoload singleton)
## These correspond to the layer names configured in project.godot
##
## Usage:
##   collision_layer = PhysicsLayers.GROUND_UNITS
##   collision_mask = PhysicsLayers.combine([GROUND_UNITS, TERRAIN])

## Layer bit positions (1-based as in Godot editor)
const GROUND_UNITS: int = 1  ## Layer 1: Ground-based units (warriors, archers, etc.)
const FLYING_UNITS: int = 2  ## Layer 2: Airborne units (demon imps, etc.)
const TERRAIN: int = 3  ## Layer 3: Environment collision (ground, obstacles)
const PROJECTILES: int = 4  ## Layer 4: Projectile collision

## Common collision mask combinations for units
const MASK_GROUND_MELEE: int = 0b00000101  ## Layers 1, 3 (ground units + terrain) - for melee that can only hit ground
const MASK_ANTI_AIR: int = 0b00000111  ## Layers 1, 2, 3 (ground + flying + terrain) - for units that can hit both layers
const MASK_PROJECTILE: int = 0b00000011  ## Layers 1, 2 (can hit ground and flying units)

## Combine multiple layer bits into a collision mask
## Example: combine([GROUND_UNITS, TERRAIN]) returns 0b00000101
static func combine(layers: Array[int]) -> int:
	var mask: int = 0
	for layer: int in layers:
		mask |= (1 << (layer - 1))  # Convert 1-based to bit position
	return mask

## Check if a mask includes a specific layer
static func has_layer(mask: int, layer: int) -> bool:
	return (mask & (1 << (layer - 1))) != 0
