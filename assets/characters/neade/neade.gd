extends Node2D

## Animation Events
## These methods are called by animation tracks at specific moments

signal attack_impact  # Fired when attack animation hits

## Called by animation track at the moment of impact
func _on_attack_impact() -> void:
	attack_impact.emit()
