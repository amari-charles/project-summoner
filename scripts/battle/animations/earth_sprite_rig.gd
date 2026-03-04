extends Node2D

## Earth Sprite Rig Script
## Bridges animation events to signals for the skeletal component
##
## Animation Design Notes:
## - Body pivot rests at center of ~600x800px bounds
## - Idle animation: Gentle bounce with leaf sway
## - Walk animation: Rocky stomp with body bob
## - Attack animation: Body lunges forward, arms swing

signal attack_impact

## Called by AnimationPlayer method track during attack animation
func _on_attack_impact() -> void:
	attack_impact.emit()
