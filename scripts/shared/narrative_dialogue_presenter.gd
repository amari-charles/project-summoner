extends DialogueBoxBase
class_name NarrativeDialoguePresenter

@export var narrative_context: NarrativeDirectorApi.Context = NarrativeDirectorApi.Context.CAMPUS

@onready var panel: PanelContainer = %Panel
@onready var speaker_label: Label = %SpeakerLabel
@onready var line_label: Label = %LineLabel
@onready var choices: VBoxContainer = %Choices
@onready var skip_button: Button = %SkipButton

var _cue: Dictionary = {}

func _ready() -> void:
	_initialize_dialogue_box(panel, choices)
	skip_button.pressed.connect(_skip)
	skip_button.text = Loc.t("narrative.skip")
	NarrativeDirectorApi.register_presenter(narrative_context, present)

func _exit_tree() -> void:
	NarrativeDirectorApi.unregister_presenter(narrative_context)
	if not _cue.is_empty():
		NarrativeDirectorApi.cancel_active_cue()

func present(cue: Dictionary) -> void:
	_cue = cue
	speaker_label.text = Loc.t(SafeTypeUtils.string(cue.get("speaker_key")))
	var response_choices: Array[Dictionary] = []
	for value: Variant in SafeTypeUtils.array(cue.get("choices")):
		response_choices.append(SafeTypeUtils.dict(value))
	_present_dialogue(SafeTypeUtils.array(cue.get("line_keys")), response_choices)


func _display_dialogue_line(line_key: String) -> void:
	line_label.text = Loc.t(line_key) if not line_key.is_empty() else ""


func _render_dialogue_choices(response_choices: Array[Dictionary]) -> void:
	for choice: Dictionary in response_choices:
		var button: Button = Button.new()
		button.text = "%s%s" % [
			"◆ " if SafeTypeUtils.bool_val(choice.get("consequential")) else "",
			Loc.t(SafeTypeUtils.string(choice.get("text_key"))),
		]
		button.tooltip_text = Loc.t("narrative.remembered_choice") if SafeTypeUtils.bool_val(choice.get("consequential")) else ""
		button.pressed.connect(_finish.bind({"choice_id": SafeTypeUtils.string(choice.get("id"))}))
		choices.add_child(button)

func _skip() -> void:
	_skip_to_dialogue_choices_or_finish()


func _on_dialogue_exhausted() -> void:
	_finish({})


func _on_dialogue_skipped() -> void:
	_finish({"skipped": true})


func _on_dialogue_cancelled() -> void:
	# Clear this presenter before cancelling because cancellation may
	# synchronously present the next queued cue in the same context.
	_cue = {}
	_hide_dialogue()
	NarrativeDirectorApi.cancel_active_cue()

func _finish(result: Dictionary) -> void:
	var cue_id: String = SafeTypeUtils.string(_cue.get("cue_id"))
	if cue_id.is_empty() or not NarrativeDirectorApi.complete_cue(cue_id, result):
		return
	if NarrativeDirectorApi.is_cue_active_or_queued(cue_id):
		return
	_cue = {}
	_hide_dialogue()
