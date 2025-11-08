extends PanelContainer
class_name UnitInfoPanel

## Displays information about the currently selected or hovered unit
## Shows unit name, HP, attack damage, and speed

@onready var unit_name_label: Label = $MarginContainer/VBoxContainer/UnitNameLabel
@onready var hp_label: Label = $MarginContainer/VBoxContainer/HPLabel
@onready var attack_label: Label = $MarginContainer/VBoxContainer/AttackLabel
@onready var speed_label: Label = $MarginContainer/VBoxContainer/SpeedLabel

var current_unit: Unit3D = null

func _ready() -> void:
	# Start hidden
	visible = false

	# Connect to UnitSelectionManager signals
	UnitSelectionManager.unit_selected.connect(_on_unit_selected)
	UnitSelectionManager.unit_deselected.connect(_on_unit_deselected)
	UnitSelectionManager.unit_hovered.connect(_on_unit_hovered)
	UnitSelectionManager.unit_unhovered.connect(_on_unit_unhovered)

func _process(_delta: float) -> void:
	# Update HP if we're tracking a unit
	if current_unit and is_instance_valid(current_unit):
		_update_hp_display()

## Show info for selected unit
func _on_unit_selected(unit: Unit3D) -> void:
	print("UnitInfoPanel: Unit selected - %s" % unit.name)
	current_unit = unit
	_update_display()
	visible = true

## Hide when unit deselected
func _on_unit_deselected(_unit: Unit3D) -> void:
	print("UnitInfoPanel: Unit deselected")
	current_unit = null
	visible = false

## Show info on hover (optional - can disable if you only want selection)
func _on_unit_hovered(unit: Unit3D) -> void:
	print("UnitInfoPanel: Unit hovered - %s" % unit.name)
	# Only show on hover if no unit is selected
	if not current_unit:
		current_unit = unit
		_update_display()
		visible = true

## Hide when hover ends (only if not selected)
func _on_unit_unhovered(_unit: Unit3D) -> void:
	# Only hide if this was a hover (not a selection)
	var selected = UnitSelectionManager.get_selected_unit()
	if not selected:
		current_unit = null
		visible = false

## Update all display labels
func _update_display() -> void:
	if not current_unit or not is_instance_valid(current_unit):
		return

	# Unit name
	unit_name_label.text = current_unit.name

	# HP
	_update_hp_display()

	# Attack damage
	attack_label.text = "Attack: %.0f" % current_unit.attack_damage

	# Move speed
	speed_label.text = "Speed: %.1f" % current_unit.move_speed

## Update just the HP label (called every frame)
func _update_hp_display() -> void:
	if not current_unit or not is_instance_valid(current_unit):
		return

	hp_label.text = "HP: %.0f / %.0f" % [current_unit.current_hp, current_unit.max_hp]
