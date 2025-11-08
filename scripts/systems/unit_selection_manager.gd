extends Node
class_name UnitSelectionManager

## Central manager for unit selection and hover state
## Singleton (autoload) that tracks which unit is selected/hovered
## Emits signals for UI and visual feedback systems to respond to

## Currently hovered unit (mouse over)
var hovered_unit: Unit3D = null

## Currently selected unit (clicked)
var selected_unit: Unit3D = null

## Emitted when mouse enters a unit
signal unit_hovered(unit: Unit3D)

## Emitted when mouse exits a unit
signal unit_unhovered(unit: Unit3D)

## Emitted when a unit is selected (clicked)
signal unit_selected(unit: Unit3D)

## Emitted when a unit is deselected (clicked elsewhere or new selection)
signal unit_deselected(unit: Unit3D)

func _ready() -> void:
	pass

## Set the currently hovered unit
## Called by units when mouse_entered signal fires
func set_hovered_unit(unit: Unit3D) -> void:
	if hovered_unit == unit:
		return

	# Unhover previous unit
	if hovered_unit and is_instance_valid(hovered_unit):
		unit_unhovered.emit(hovered_unit)

	# Set new hovered unit
	hovered_unit = unit
	if unit:
		unit_hovered.emit(unit)

## Clear hovered unit
## Called by units when mouse_exited signal fires
func clear_hovered_unit(unit: Unit3D) -> void:
	# Only clear if this unit is currently hovered (prevents race conditions)
	if hovered_unit == unit:
		unit_unhovered.emit(unit)
		hovered_unit = null

## Select a unit
## Called by units when clicked
func select_unit(unit: Unit3D) -> void:
	if selected_unit == unit:
		return  # Already selected

	# Deselect previous unit
	if selected_unit and is_instance_valid(selected_unit):
		unit_deselected.emit(selected_unit)

	# Select new unit
	selected_unit = unit
	if unit:
		unit_selected.emit(unit)

## Clear selection
## Called when clicking empty space or when selected unit dies
func clear_selection() -> void:
	if selected_unit and is_instance_valid(selected_unit):
		unit_deselected.emit(selected_unit)
	selected_unit = null

## Get currently selected unit
func get_selected_unit() -> Unit3D:
	if selected_unit and is_instance_valid(selected_unit):
		return selected_unit
	return null

## Get currently hovered unit
func get_hovered_unit() -> Unit3D:
	if hovered_unit and is_instance_valid(hovered_unit):
		return hovered_unit
	return null
