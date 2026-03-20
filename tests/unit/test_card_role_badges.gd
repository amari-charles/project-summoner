extends GutTest

## UI regression tests for tactical role badges in card modals.

const CARD_DETAIL_MODAL_SCENE: PackedScene = preload("res://scenes/meta/modals/card_detail_modal.tscn")
const CARD_FULL_STATS_MODAL_SCENE: PackedScene = preload("res://scenes/meta/modals/card_full_stats_modal.tscn")
const SUMMON_CARD_ID: String = "puff"
const SPELL_CARD_ID: String = "fireball"

var _spawned_nodes: Array[Node] = []


func after_each() -> void:
	for node: Node in _spawned_nodes:
		if is_instance_valid(node):
			node.queue_free()
	_spawned_nodes.clear()


func test_card_detail_modal_shows_role_badge_for_summon() -> void:
	var modal: Control = _spawn_modal(CARD_DETAIL_MODAL_SCENE)

	modal.call("open_for_card", "", SUMMON_CARD_ID)

	var role_badge_label: Label = _get_role_badge_label(modal)
	var role_badge_panel: PanelContainer = role_badge_label.get_parent()
	assert_true(role_badge_panel.visible, "Role badge should be visible for summon cards")
	assert_eq(role_badge_label.text, "BACKLINER", "Puff should render the BACKLINER role badge")


func test_card_detail_modal_hides_role_badge_for_spell() -> void:
	var modal: Control = _spawn_modal(CARD_DETAIL_MODAL_SCENE)

	modal.call("open_for_card", "", SPELL_CARD_ID)

	var role_badge_label: Label = _get_role_badge_label(modal)
	var role_badge_panel: PanelContainer = role_badge_label.get_parent()
	assert_false(role_badge_panel.visible, "Role badge should be hidden for spell cards")


func test_card_full_stats_modal_shows_role_badge_for_summon() -> void:
	var modal: Control = _spawn_modal(CARD_FULL_STATS_MODAL_SCENE)

	modal.call("open_for_card", "", SUMMON_CARD_ID)

	var role_badge_label: Label = _get_role_badge_label(modal)
	var role_badge_panel: PanelContainer = role_badge_label.get_parent()
	assert_true(role_badge_panel.visible, "Role badge should be visible for summon cards")
	assert_eq(role_badge_label.text, "BACKLINER", "Puff should render the BACKLINER role badge")


func test_card_full_stats_modal_hides_role_badge_for_spell() -> void:
	var modal: Control = _spawn_modal(CARD_FULL_STATS_MODAL_SCENE)

	modal.call("open_for_card", "", SPELL_CARD_ID)

	var role_badge_label: Label = _get_role_badge_label(modal)
	var role_badge_panel: PanelContainer = role_badge_label.get_parent()
	assert_false(role_badge_panel.visible, "Role badge should be hidden for spell cards")


func test_card_full_stats_modal_renders_attack_damage_stat_row_for_summon() -> void:
	var modal: Control = _spawn_modal(CARD_FULL_STATS_MODAL_SCENE)

	modal.call("open_for_card", "", SUMMON_CARD_ID)

	var damage_row: Node = _find_stat_row(modal, "stat_damage")
	assert_not_null(damage_row, "Full stats should include a damage row for summon cards")

	var value_label: Label = damage_row.get_child(1) as Label
	assert_not_null(value_label, "Damage row should include a value label")
	assert_eq(value_label.text, "12", "Puff base attack damage should be shown in full stats")


func test_card_full_stats_modal_renders_additional_non_core_stats() -> void:
	var modal: Control = _spawn_modal(CARD_FULL_STATS_MODAL_SCENE)

	modal.call("open_for_card", "", SUMMON_CARD_ID)

	var aggro_row: Node = _find_stat_row(modal, "aggro_radius")
	assert_not_null(aggro_row, "Full stats should include aggro radius when present")


func _spawn_modal(scene: PackedScene) -> Control:
	var modal: Control = scene.instantiate()
	assert_not_null(modal, "Modal scene should instantiate")
	get_tree().root.add_child(modal)
	_spawned_nodes.append(modal)
	return modal


func _get_role_badge_label(modal: Node) -> Label:
	var role_badge_label: Node = modal.find_child("RoleBadgeLabel", true, false)
	assert_true(role_badge_label is Label, "RoleBadgeLabel node should exist and be a Label")
	return role_badge_label as Label


func _find_stat_row(modal: Node, stat_id: String) -> Node:
	var row_name: String = "StatRow_%s" % stat_id
	return modal.find_child(row_name, true, false)
