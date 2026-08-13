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

func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var text: String = file.get_as_text()
	file.close()
	return text
