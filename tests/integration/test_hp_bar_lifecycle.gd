extends GutTest

## Integration tests for current HP bar lifecycle architecture.
##
## HP bars are now shell-owned (`FloatingHPBar`) and track units directly
## via `TrackNode`, instead of being created through a global HPBarService.

const FLOATING_HP_BAR_SCRIPT: Script = preload("res://scripts/csharp/Battle/View/UI/FloatingHPBar.cs")


class MockDamageableNode:
	extends Node3D

	signal hp_changed(new_hp: float, max_hp: float)

	var current_hp: float = 100.0
	var max_hp: float = 100.0

	func get_current_hp() -> float:
		return current_hp

	func get_max_hp() -> float:
		return max_hp

	func apply_damage(amount: float) -> void:
		current_hp = maxf(current_hp - amount, 0.0)
		emit_signal("hp_changed", current_hp, max_hp)


var _test_units: Array[Node3D] = []
var _test_bars: Array[Node3D] = []


func before_each() -> void:
	_test_units.clear()
	_test_bars.clear()


func after_each() -> void:
	for unit: Node3D in _test_units:
		if is_instance_valid(unit) and unit.is_inside_tree():
			unit.queue_free()

	for bar: Node3D in _test_bars:
		if is_instance_valid(bar) and bar.is_inside_tree():
			bar.queue_free()

	_test_units.clear()
	_test_bars.clear()

	await get_tree().process_frame
	await get_tree().process_frame


func _is_csharp_available() -> bool:
	return FLOATING_HP_BAR_SCRIPT != null


func test_hp_bar_created_and_attached_to_tree() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	var unit: Node3D = _create_mock_unit()
	add_child(unit)
	_test_units.append(unit)

	var bar: Node3D = _create_bar_for_unit(unit)

	assert_true(is_instance_valid(bar), "HP bar should be instantiated")
	assert_true(bar.is_inside_tree(), "HP bar should be in scene tree")


func test_hp_bar_removed_when_tracked_unit_freed() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	var unit: Node3D = _create_mock_unit()
	add_child(unit)
	_test_units.append(unit)

	var bar: Node3D = _create_bar_for_unit(unit)
	assert_true(is_instance_valid(bar), "HP bar should exist before cleanup")

	unit.queue_free()
	_test_units.clear()

	await get_tree().process_frame
	await get_tree().process_frame

	assert_false(
		is_instance_valid(bar),
		"HP bar should be auto-freed via tracked node TreeExiting cleanup"
	)


func test_multi_unit_hp_bars_cleanup_on_mass_death() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	var unit_count: int = 10
	var units: Array[Node3D] = []
	var bars: Array[Node3D] = []

	for i: int in unit_count:
		var unit: Node3D = _create_mock_unit()
		add_child(unit)
		units.append(unit)
		_test_units.append(unit)

		var bar: Node3D = _create_bar_for_unit(unit)
		bars.append(bar)

	for bar: Node3D in bars:
		assert_true(is_instance_valid(bar), "Every spawned unit should have a bar")

	for unit: Node3D in units:
		unit.queue_free()
	_test_units.clear()

	await get_tree().process_frame
	await get_tree().process_frame

	for bar: Node3D in bars:
		assert_false(
			is_instance_valid(bar),
			"All bars should be cleaned when tracked units are mass-freed"
		)


func test_hp_signal_update_toggles_bar_visibility_after_damage() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	var unit: MockDamageableNode = MockDamageableNode.new()
	unit.name = "MockDamageable_%d" % randi()
	add_child(unit)
	_test_units.append(unit)

	var bar: Node3D = _create_bar_for_unit(unit)

	# Full HP with default settings (show-on-damage) should hide bar.
	await get_tree().process_frame
	assert_false(bar.visible, "Bar should be hidden at full HP in show-on-damage mode")

	# Damage emits hp_changed and should show the bar.
	unit.apply_damage(25.0)
	await get_tree().process_frame
	assert_true(bar.visible, "Bar should become visible after HP drops")


func _create_mock_unit() -> Node3D:
	var unit: MockDamageableNode = MockDamageableNode.new()
	unit.name = "MockUnit_%d" % randi()
	return unit


func _create_bar_for_unit(unit: Node3D) -> Node3D:
	var bar_object: Object = FLOATING_HP_BAR_SCRIPT.new()
	assert_true(bar_object is Node3D, "FloatingHPBar script should instantiate as Node3D")
	var bar: Node3D = bar_object as Node3D
	add_child(bar)
	_test_bars.append(bar)

	# C# methods called via `call()` require exact PascalCase names.
	bar.call("TrackNode", unit)

	return bar
