extends RefCounted
class_name CardFormation

## Formation preview helper for multi-unit spawns.
## Pure math utility — no card/simulation dependencies.
##
## Note: Actual spawning uses C# formations via Card/Simulation.
## This is only used for UI spawn preview indicators.

const FORMATION_TWO_ROW_MAX: int = 20
const FORMATION_LARGE_ROW_DENSITY: float = 3.0
const DEFAULT_FORMATION_SPACING: float = 1.8
const DEFAULT_FORMATION_ROW_OFFSET: float = 0.5


## Generate a formation offset for a unit in a group (static/UI preview only)
static func generate_formation_offset(unit_index: int, unit_count: int) -> Vector3:
	if unit_count <= 1:
		return Vector3.ZERO

	var rows: int = 2 if unit_count <= FORMATION_TWO_ROW_MAX else ceili(sqrt(float(unit_count) / FORMATION_LARGE_ROW_DENSITY))
	var cols: int = ceili(float(unit_count) / float(rows))

	var row: int = unit_index / cols
	var col: int = unit_index % cols
	var units_in_row: int = mini(cols, unit_count - row * cols)

	var stagger: float = DEFAULT_FORMATION_ROW_OFFSET * DEFAULT_FORMATION_SPACING if row % 2 == 1 else 0.0

	var formation_depth: float = (rows - 1) * DEFAULT_FORMATION_SPACING
	var x_offset: float = row * DEFAULT_FORMATION_SPACING - formation_depth / 2.0

	var row_width: float = (units_in_row - 1) * DEFAULT_FORMATION_SPACING
	var z_offset: float = col * DEFAULT_FORMATION_SPACING - row_width / 2.0 + stagger

	return Vector3(x_offset, 0, z_offset)
