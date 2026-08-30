extends RefCounted
class_name QuestGuidance

const INDICATOR_NAME: StringName = &"QuestObjectiveIndicatorLayer"
const QuestObjectiveIndicatorScript: Script = preload(
	"res://scripts/meta/components/quest_objective_indicator.gd"
)


static func current_target_id() -> String:
	var journal: Dictionary = QuestApi.get_journal_state()
	var tracked_id: String = SafeTypeUtils.string(journal.get("tracked_quest_id"))
	for value: Variant in SafeTypeUtils.array(journal.get("active")):
		var quest: Dictionary = SafeTypeUtils.dict(value)
		if not tracked_id.is_empty() and SafeTypeUtils.string(quest.get("id")) != tracked_id:
			continue
		return SafeTypeUtils.string(quest.get("current_target_id"))
	return ""


static func is_target_active(target_id: String) -> bool:
	return not target_id.is_empty() and current_target_id() == target_id


static func show_for(target: Control, target_id: String, action_key: String = "") -> void:
	if not is_instance_valid(target) or not is_target_active(target_id):
		return
	var tree: SceneTree = target.get_tree()
	if tree == null or tree.root == null:
		return
	var layer: CanvasLayer = tree.root.get_node_or_null(NodePath(String(INDICATOR_NAME))) as CanvasLayer
	if layer == null:
		layer = CanvasLayer.new()
		layer.name = INDICATOR_NAME
		layer.layer = 120
		tree.root.add_child(layer)
		var indicator: QuestObjectiveIndicator = QuestObjectiveIndicatorScript.new()
		indicator.name = "Indicator"
		layer.add_child(indicator)
	var active_indicator: QuestObjectiveIndicator = layer.get_node("Indicator") as QuestObjectiveIndicator
	active_indicator.target = target
	active_indicator.set_action_text(Loc.t(
		action_key if not action_key.is_empty() else _default_action_key(target_id)
	))
	active_indicator.visible = true


static func _default_action_key(target_id: String) -> String:
	if target_id == "card_detail":
		return "quest.guidance.right_click"
	return "quest.guidance.click"


static func clear() -> void:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	if tree == null or tree.root == null:
		return
	var layer: CanvasLayer = tree.root.get_node_or_null(NodePath(String(INDICATOR_NAME))) as CanvasLayer
	if layer != null:
		var indicator: QuestObjectiveIndicator = layer.get_node_or_null("Indicator") as QuestObjectiveIndicator
		if indicator != null:
			indicator.target = null
			indicator.visible = false
