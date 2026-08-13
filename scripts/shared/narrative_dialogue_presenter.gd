extends Control
class_name NarrativeDialoguePresenter

@export var narrative_context: NarrativeDirectorApi.Context = NarrativeDirectorApi.Context.CAMPUS

@onready var panel: PanelContainer = %Panel
@onready var speaker_label: Label = %SpeakerLabel
@onready var line_label: Label = %LineLabel
@onready var choices: VBoxContainer = %Choices
@onready var skip_button: Button = %SkipButton

var _cue: Dictionary = {}
var _line_keys: Array = []
var _line_index: int = 0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	panel.visible = false
	gui_input.connect(_on_gui_input)
	skip_button.pressed.connect(_skip)
	skip_button.text = Loc.t("narrative.skip")
	NarrativeDirectorApi.register_presenter(narrative_context, present)

func _exit_tree() -> void:
	NarrativeDirectorApi.unregister_presenter(narrative_context)
	if not _cue.is_empty():
		NarrativeDirectorApi.cancel_active_cue()

func present(cue: Dictionary) -> void:
	_cue = cue
	_line_keys = SafeTypeUtils.array(cue.get("line_keys"))
	_line_index = 0
	panel.visible = true
	speaker_label.text = Loc.t(SafeTypeUtils.string(cue.get("speaker_key")))
	_render_line()

func _render_line() -> void:
	_clear_choices()
	if _line_index < _line_keys.size():
		line_label.text = Loc.t(SafeTypeUtils.string(_line_keys[_line_index]))
		return
	var authored_choices: Array = SafeTypeUtils.array(_cue.get("choices"))
	if authored_choices.is_empty():
		_finish({})
		return
	line_label.text = ""
	for value: Variant in authored_choices:
		var choice: Dictionary = SafeTypeUtils.dict(value)
		var button: Button = Button.new()
		button.text = "%s%s" % [
			"◆ " if SafeTypeUtils.bool_val(choice.get("consequential")) else "",
			Loc.t(SafeTypeUtils.string(choice.get("text_key"))),
		]
		button.tooltip_text = Loc.t("narrative.remembered_choice") if SafeTypeUtils.bool_val(choice.get("consequential")) else ""
		button.pressed.connect(_finish.bind({"choice_id": SafeTypeUtils.string(choice.get("id"))}))
		choices.add_child(button)

func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		_advance()

func _unhandled_key_input(event: InputEvent) -> void:
	if panel.visible and event.pressed and (event.is_action("ui_accept") or event.is_action("ui_select")):
		_advance()

func _advance() -> void:
	if _cue.is_empty() or _line_index >= _line_keys.size():
		return
	_line_index += 1
	_render_line()

func _skip() -> void:
	if not SafeTypeUtils.array(_cue.get("choices")).is_empty():
		_line_index = _line_keys.size()
		_render_line()
		return
	_finish({"skipped": true})

func _finish(result: Dictionary) -> void:
	var cue_id: String = SafeTypeUtils.string(_cue.get("cue_id"))
	if cue_id.is_empty() or not NarrativeDirectorApi.complete_cue(cue_id, result):
		return
	_cue = {}
	panel.visible = false
	_clear_choices()

func _clear_choices() -> void:
	for child: Node in choices.get_children():
		child.queue_free()
