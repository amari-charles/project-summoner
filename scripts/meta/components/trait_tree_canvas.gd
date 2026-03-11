extends Control
class_name TraitTreeCanvas

## Draws connector lines behind positioned trait nodes.

var _edges: Array = []


func clear_edges() -> void:
	_edges.clear()
	queue_redraw()


func set_edges(edges: Array) -> void:
	_edges = edges
	queue_redraw()


func _draw() -> void:
	for edge_var: Variant in _edges:
		if not edge_var is Dictionary:
			continue
		var edge: Dictionary = edge_var
		var from_point: Vector2 = edge.get("from", Vector2.ZERO)
		var to_point: Vector2 = edge.get("to", Vector2.ZERO)
		var color: Color = edge.get("color", Color(0.45, 0.45, 0.45, 0.8))
		var width: float = float(edge.get("width", 2.0))

		# Draw in two segments to create clear bottom-up connectors.
		var mid_y: float = from_point.y + (to_point.y - from_point.y) * 0.45
		var corner_a: Vector2 = Vector2(from_point.x, mid_y)
		var corner_b: Vector2 = Vector2(to_point.x, mid_y)

		draw_line(from_point, corner_a, color, width, true)
		draw_line(corner_a, corner_b, color, width, true)
		draw_line(corner_b, to_point, color, width, true)
