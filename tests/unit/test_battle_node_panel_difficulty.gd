extends GutTest

## Regression tests for battle node difficulty display.

const BATTLE_NODE_PANEL_SCENE: PackedScene = preload("res://scenes/meta/components/node_panels/battle_node_panel.tscn")

var _spawned_nodes: Array[Node] = []


func after_each() -> void:
	for node: Node in _spawned_nodes:
		if is_instance_valid(node):
			node.queue_free()
	_spawned_nodes.clear()


func test_difficulty_label_includes_numeric_value_for_high_difficulty() -> void:
	var panel: Control = _spawn_panel()
	panel.call("_update_difficulty_stars", 12)

	var difficulty_label_node: Node = panel.find_child("DifficultyLabel", true, false)
	assert_true(difficulty_label_node is Label, "DifficultyLabel should exist")
	var difficulty_label: Label = difficulty_label_node as Label

	assert_true(difficulty_label.text.begins_with(Loc.t("campaign.map.difficulty_label")))
	assert_true(difficulty_label.text.contains("12"), "Difficulty label should include exact numeric difficulty")


func test_difficulty_stars_clamp_to_fixed_five_star_band() -> void:
	var panel: Control = _spawn_panel()
	panel.call("_update_difficulty_stars", 15)

	var stars_container_node: Node = panel.find_child("StarsContainer", true, false)
	assert_true(stars_container_node is HBoxContainer, "StarsContainer should exist")
	var stars_container: HBoxContainer = stars_container_node as HBoxContainer

	assert_eq(stars_container.get_child_count(), 5, "Battle node panel should always show five stars")


func _spawn_panel() -> Control:
	var panel: Control = BATTLE_NODE_PANEL_SCENE.instantiate()
	assert_not_null(panel, "BattleNodePanel scene should instantiate")
	get_tree().root.add_child(panel)
	_spawned_nodes.append(panel)
	return panel
