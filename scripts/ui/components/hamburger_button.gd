extends Button
class_name HamburgerButton

## Hamburger menu button (☰) for opening navigation drawer
## Emits pressed signal when tapped/clicked

func _ready() -> void:
	# Ensure minimum touch target size (44x44)
	custom_minimum_size = Vector2(48, 48)

	# Style the button
	flat = true

	# Use hamburger unicode character as icon placeholder
	# TODO: Replace with proper icon asset when available
	text = "☰"

	# Connect our own hover effects
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

func _on_mouse_entered() -> void:
	modulate = Color(1.2, 1.2, 1.2, 1.0)

func _on_mouse_exited() -> void:
	modulate = Color(1.0, 1.0, 1.0, 1.0)
