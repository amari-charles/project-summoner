extends GutTest

const LEGACY_PATHS: Array[String] = [
	"res://scripts/application/dialogue_manager.gd",
	"res://scripts/application/event_sequencer.gd",
	"res://scripts/battle/battle_dialogue_controller.gd",
	"res://scripts/infrastructure/data/event_step.gd",
	"res://scenes/shared/dialogue_box.tscn",
]

func test_narrative_catalog_and_presenter_are_authored_and_loadable() -> void:
	var file: FileAccess = FileAccess.open("res://data/narrative/narrative.json", FileAccess.READ)
	assert_not_null(file)
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	file.close()
	assert_true(parsed is Dictionary)
	assert_false(SafeTypeUtils.array(parsed.get("cues")).is_empty())
	assert_false(SafeTypeUtils.array(parsed.get("dialogue")).is_empty())
	assert_true(ResourceLoader.exists("res://scenes/shared/narrative_dialogue_presenter.tscn"))

func test_legacy_narrative_runtime_is_fully_removed() -> void:
	for path: String in LEGACY_PATHS:
		assert_false(FileAccess.file_exists(path), "Legacy path should be removed: %s" % path)
	var project_text: String = _read("res://project.godot")
	assert_false(project_text.contains('DialogueManager="'))
	assert_false(project_text.contains('EventSequencer="'))

func test_presenter_skip_preserves_required_choices() -> void:
	var script_text: String = _read("res://scripts/shared/narrative_dialogue_presenter.gd")
	assert_true(script_text.contains("if not SafeTypeUtils.array(_cue.get(\"choices\")).is_empty()"))
	assert_true(script_text.contains("consequential"))
	assert_true(script_text.contains("_release_dialogue_focus"))

func test_hidden_presenter_does_not_block_underlying_screen_controls() -> void:
	var scene: PackedScene = load("res://scenes/shared/narrative_dialogue_presenter.tscn")
	var presenter: NarrativeDialoguePresenter = scene.instantiate() as NarrativeDialoguePresenter
	add_child(presenter)
	assert_eq(presenter.mouse_filter, Control.MOUSE_FILTER_IGNORE)
	presenter.present({
		"cue_id": "test",
		"speaker_key": "dialogue.merlin_summoner_intro.speaker",
		"line_keys": ["dialogue.merlin_summoner_intro.line_1"],
		"choices": [],
	})
	assert_eq(presenter.mouse_filter, Control.MOUSE_FILTER_STOP)
	presenter._cue = {}
	presenter.free()

func test_clicking_visible_dialogue_text_advances_the_narrative() -> void:
	var scene: PackedScene = load("res://scenes/shared/narrative_dialogue_presenter.tscn")
	var presenter: NarrativeDialoguePresenter = scene.instantiate() as NarrativeDialoguePresenter
	var host: Control = Control.new()
	host.size = Vector2(1152, 648)
	add_child_autofree(host)
	host.add_child(presenter)
	await get_tree().process_frame
	presenter.present({
		"cue_id": "test_click_advance",
		"speaker_key": "dialogue.merlin_summoner_intro.speaker",
		"line_keys": [
			"dialogue.merlin_summoner_intro.line_1",
			"dialogue.merlin_summoner_intro.line_2",
		],
		"choices": [],
	})
	await get_tree().process_frame

	var line_label: Label = presenter.get_node("Panel/Margin/Root/LineLabel") as Label
	var first_line: String = line_label.text
	var click: InputEventMouseButton = InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	click.position = line_label.get_global_rect().get_center()
	click.global_position = click.position
	get_viewport().push_input(click, true)
	await get_tree().process_frame

	assert_ne(line_label.text, first_line)
	assert_eq(presenter._line_index, 1)
	presenter._cue = {}

func test_space_advances_visible_dialogue_text() -> void:
	var scene: PackedScene = load("res://scenes/shared/narrative_dialogue_presenter.tscn")
	var presenter: NarrativeDialoguePresenter = scene.instantiate() as NarrativeDialoguePresenter
	add_child_autofree(presenter)
	await get_tree().process_frame
	presenter.present({
		"cue_id": "test_space_advance",
		"speaker_key": "dialogue.merlin_summoner_intro.speaker",
		"line_keys": [
			"dialogue.merlin_summoner_intro.line_1",
			"dialogue.merlin_summoner_intro.line_2",
		],
		"choices": [],
	})

	var space: InputEventKey = InputEventKey.new()
	space.keycode = KEY_SPACE
	space.pressed = true
	presenter._input(space)

	assert_eq(presenter._line_index, 1)
	presenter._cue = {}

func test_space_spam_does_not_select_an_unfocused_response() -> void:
	var scene: PackedScene = load("res://scenes/shared/narrative_dialogue_presenter.tscn")
	var presenter: NarrativeDialoguePresenter = scene.instantiate() as NarrativeDialoguePresenter
	add_child_autofree(presenter)
	await get_tree().process_frame
	presenter.present({
		"cue_id": "test_space_choice_guard",
		"speaker_key": "dialogue.merlin_summoner_intro.speaker",
		"line_keys": ["dialogue.merlin_summoner_intro.line_1"],
		"choices": [{
			"id": "accept",
			"text_key": "dialogue.choice_example.choice_1",
		}],
	})

	var space: InputEventKey = InputEventKey.new()
	space.keycode = KEY_SPACE
	space.pressed = true
	presenter._input(space)
	await get_tree().process_frame

	var choices: VBoxContainer = presenter.get_node("Panel/Margin/Root/Choices") as VBoxContainer
	assert_eq(choices.get_child_count(), 1)
	assert_null(get_viewport().gui_get_focus_owner())
	presenter._input(space)
	assert_false(presenter._cue.is_empty())
	presenter._cue = {}

func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var text: String = file.get_as_text()
	file.close()
	return text
