extends Node2D

## Fire Elemental Rig Script
## Bridges animation events to signals for the skeletal component
##
## Animation Design Notes:
## - Body pivot rests at (300, 300) in ~600x800px bounds
## - Idle animation: 0.8s loop with bouncy hop effect
##   - Squash phase at 0.15s (body drops, squash scale 1.05x0.92)
##   - Stretch phase at 0.4s (body rises, stretch scale 0.97x1.05)
##   - Legs alternate: left leads down, right leads up (offset timing)
## - Attack animation: 0.8s with forward lunge
##   - Lunge to (400, 300) at 0.4s with body rotation
##   - Eye scales to 1.2x during attack for intensity
##   - Impact event fires at 0.4s (mid-attack)
## - Leg rotations use small values (±0.03 to ±0.25 radians) for subtle movement

signal attack_impact

## Called by AnimationPlayer method track during attack animation
func _on_attack_impact() -> void:
	attack_impact.emit()
