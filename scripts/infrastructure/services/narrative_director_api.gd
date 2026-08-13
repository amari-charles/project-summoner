class_name NarrativeDirectorApi
extends RefCounted

enum EventType {
	PREPARATION_OPENED,
	BATTLE_STARTED,
	BATTLE_PHASE_CHANGED,
	PLAYER_COMMAND_REJECTED,
	BATTLE_EVENT_OCCURRED,
	BATTLE_RESOLVED,
	ACTIVITY_COMPLETED,
	META_MOMENT_STARTED,
}

enum Context {
	PREPARATION,
	BATTLE,
	RESULTS,
	CAMPUS,
}

static func node() -> Node:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	return tree.root.get_node("NarrativeDirector")

static func publish_event(event_type: EventType, source_id: String, facts: Dictionary = {}) -> bool:
	return SafeTypeUtils.bool_val(
		node().call("PublishEvent", int(event_type), source_id, facts),
		false
	)

static func register_presenter(context: Context, presenter: Callable) -> void:
	node().call("RegisterPresenter", int(context), presenter)

static func unregister_presenter(context: Context) -> void:
	node().call("UnregisterPresenter", int(context))

static func complete_cue(cue_id: String, result: Dictionary = {}) -> bool:
	return SafeTypeUtils.bool_val(node().call("CompleteCue", cue_id, result), false)

static func cancel_active_cue() -> void:
	node().call("CancelActiveCue")

static func is_cue_active_or_queued(cue_id: String) -> bool:
	return SafeTypeUtils.bool_val(node().call("IsCueActiveOrQueued", cue_id), false)

static func get_pending_cue_count() -> int:
	return SafeTypeUtils.int_val(node().call("GetPendingCueCount"), 0)
