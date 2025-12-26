extends GridContainer
class_name AnimatedGridContainer

## A GridContainer that animates children to their new positions when layout changes.
## Call capture_positions() before any operation that will change the layout.

const ANIMATION_DURATION: float = 0.25
const MIN_ANIMATION_DISTANCE: float = 1.0  ## Minimum pixels moved to trigger animation

## Stored positions from before layout change
var _old_positions: Dictionary = {}  # child -> Vector2

## Prevent re-entry during animation setup
var _animating: bool = false

## Pending animation (deferred to batch multiple layout changes)
var _animation_pending: bool = false


func _notification(what: int) -> void:
	if what == NOTIFICATION_SORT_CHILDREN:
		_schedule_animation()


## Call this before removing/adding children to capture current positions
func capture_positions() -> void:
	_old_positions.clear()
	for child: Node in get_children():
		if child is Control and not child.is_queued_for_deletion():
			var ctrl: Control = child as Control
			_old_positions[child] = ctrl.position


func _schedule_animation() -> void:
	if _animation_pending or _old_positions.is_empty():
		return
	_animation_pending = true
	call_deferred("_animate_to_new_positions")


func _animate_to_new_positions() -> void:
	_animation_pending = false

	if _animating or _old_positions.is_empty():
		return

	_animating = true

	for child: Node in get_children():
		# Skip nodes being deleted
		if child.is_queued_for_deletion():
			continue

		if child is Control and _old_positions.has(child):
			var ctrl: Control = child as Control
			var old_pos: Vector2 = _old_positions[child]
			var new_pos: Vector2 = ctrl.position

			if old_pos.distance_to(new_pos) > MIN_ANIMATION_DISTANCE:
				ctrl.position = old_pos
				var tween: Tween = create_tween()
				tween.tween_property(ctrl, "position", new_pos, ANIMATION_DURATION)
				tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)

	_old_positions.clear()
	_animating = false
