extends Control
class_name EventScreen

var _event_id: String = ""
var _returning: bool = false

func _ready() -> void:
	_event_id = EventContext.get_current_event_id()
	if _event_id.is_empty():
		_return_to_campaign()
		return
	var director: Node = NarrativeDirectorApi.node()
	if not director.is_connected("CueCompleted", _on_cue_completed):
		director.connect("CueCompleted", _on_cue_completed)
	await get_tree().process_frame
	NarrativeDirectorApi.publish_event(
		NarrativeDirectorApi.EventType.META_MOMENT_STARTED,
		"event.%s" % _event_id,
		{"summoner_id": SummonerSelectionApi.get_active_summoner_id()}
	)
	if NarrativeDirectorApi.get_pending_cue_count() == 0:
		_finish_event()

func _exit_tree() -> void:
	var director: Node = NarrativeDirectorApi.node()
	if director.is_connected("CueCompleted", _on_cue_completed):
		director.disconnect("CueCompleted", _on_cue_completed)

func _on_cue_completed(_cue_id: String) -> void:
	if NarrativeDirectorApi.get_pending_cue_count() == 0:
		_finish_event()

func _finish_event() -> void:
	if _returning:
		return
	EventContext.complete_event()
	_return_to_campaign()

func _return_to_campaign() -> void:
	if _returning:
		return
	_returning = true
	var return_to: String = EventContext.get_return_scene()
	EventContext.clear_event()
	SceneManager.transition_to(return_to if not return_to.is_empty() else SceneManager.SCENE_CAMPAIGN_MAP)
