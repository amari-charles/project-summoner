extends Button
class_name SpawnableUnitButton

## SpawnableUnitButton - A draggable button for spawning units in debug arena
##
## Can be dragged onto the battlefield to spawn a unit at that position.
## Returns drag data compatible with InputCollector.

## The Card resource to spawn (created from catalog)
var card: Card = null

## The display name of the unit
var unit_name: String = ""

## Reference to parent panel for team selection
var panel: Node = null


func _ready() -> void:
	text = unit_name
	custom_minimum_size = Vector2(140, 32)

	# Apply basic styling
	add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)


## Override to provide drag data
func _get_drag_data(_at_position: Vector2) -> Variant:
	# No drag preview text - ghost units on battlefield are the preview

	# Return drag data that InputCollector can use
	var team: int = 1  # Default to enemy team
	if panel and panel.has_method("get_spawn_team"):
		team = panel.get_spawn_team()

	return {
		"type": "debug_spawn",
		"card": card,
		"team": team
	}
