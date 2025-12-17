extends Node2D

## Fire Elemental Rig Script
## Bridges animation events to signals for the skeletal component

signal attack_impact

## Called by AnimationPlayer method track during attack animation
func _on_attack_impact() -> void:
	attack_impact.emit()
