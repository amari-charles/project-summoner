extends Node2D

## Fire Ant Rig Script
## Bridges animation events to signals for the skeletal component
##
## Animation Design Notes:
## - Body pivot rests at center in 2100x1500px canvas
## - Ant faces right with 4 legs, eye, and 2 antennae
## - Legs ordered 1-4 from back (left) to front (right)
## - Idle animation: gentle breathing/bobbing
## - Walk animation: diagonal leg pairs alternate (1+3 vs 2+4)
## - Attack animation: lunge forward with mandible motion
##
## Visual Component Notes:
## - FeetOffsetPixels = 530 (ViewportPadding 200 + ~330px whitespace below feet in PNGs)
## - Leg content ends at Y~1167 in 1500px canvas, leaving ~333px whitespace below

signal attack_impact

## Called by AnimationPlayer method track during attack animation
func _on_attack_impact() -> void:
	attack_impact.emit()
