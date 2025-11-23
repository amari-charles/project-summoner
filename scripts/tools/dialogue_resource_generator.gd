@tool
extends EditorScript

## DialogueResourceGenerator
##
## Automatically generates DialogueData .tres files from localization JSON.
## This prevents manual mistakes when creating dialogue resources.
##
## Usage:
##   1. Add dialogue entries to localization/data/en.json under "dialogue" section
##   2. Run this script from Godot Editor: File > Run
##   3. Dialogue .tres files will be created in resources/dialogue/
##
## Example JSON structure:
## {
##   "dialogue": {
##     "my_dialogue_id": {
##       "text": "Hello, traveler!",
##       "speaker": "Merlin"
##     }
##   }
## }

const LOCALIZATION_PATH: String = "res://localization/data/en.json"
const DIALOGUE_OUTPUT_DIR: String = "res://resources/dialogue/"
const DIALOGUE_SCRIPT_PATH: String = "res://scripts/dialogue/dialogue_data.gd"

func _run() -> void:
	print("DialogueResourceGenerator: Starting...")

	# Load localization JSON
	var file: FileAccess = FileAccess.open(LOCALIZATION_PATH, FileAccess.READ)
	if not file:
		push_error("DialogueResourceGenerator: Failed to open localization file: %s" % LOCALIZATION_PATH)
		return

	var json_text: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var parse_result: Error = json.parse(json_text)
	if parse_result != OK:
		push_error("DialogueResourceGenerator: Failed to parse JSON: %s" % json.get_error_message())
		return

	var data: Dictionary = json.data
	if not data.has("dialogue"):
		push_error("DialogueResourceGenerator: No 'dialogue' section found in localization file")
		return

	var dialogues: Dictionary = data["dialogue"]
	var created_count: int = 0
	var updated_count: int = 0
	var skipped_count: int = 0

	# Ensure output directory exists
	if not DirAccess.dir_exists_absolute(DIALOGUE_OUTPUT_DIR):
		DirAccess.make_dir_recursive_absolute(DIALOGUE_OUTPUT_DIR)

	# Generate .tres file for each dialogue
	for dialogue_id: String in dialogues.keys():
		var dialogue_data: Dictionary = dialogues[dialogue_id]

		# Validate required fields
		if not dialogue_data.has("text") or not dialogue_data.has("speaker"):
			push_warning("DialogueResourceGenerator: Skipping '%s' - missing 'text' or 'speaker'" % dialogue_id)
			skipped_count += 1
			continue

		var output_path: String = DIALOGUE_OUTPUT_DIR + dialogue_id + ".tres"

		# Check if file already exists and has correct script path
		if FileAccess.file_exists(output_path):
			var existing_content: String = FileAccess.get_file_as_string(output_path)
			if existing_content.contains("res://scripts/dialogue/dialogue_data.gd"):
				# File exists and is correct, skip
				skipped_count += 1
				continue
			else:
				# File exists but needs updating
				_generate_dialogue_resource(dialogue_id, output_path)
				updated_count += 1
		else:
			# Create new file
			_generate_dialogue_resource(dialogue_id, output_path)
			created_count += 1

	print("DialogueResourceGenerator: Complete!")
	print("  Created: %d" % created_count)
	print("  Updated: %d" % updated_count)
	print("  Skipped: %d" % skipped_count)
	print("  Total dialogues: %d" % dialogues.size())

func _generate_dialogue_resource(dialogue_id: String, output_path: String) -> void:
	var tres_content: String = """[gd_resource type="Resource" script_class="DialogueData" load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/dialogue/dialogue_data.gd" id="1_dialogue"]

[resource]
script = ExtResource("1_dialogue")
dialogue_id = "%s"
text = "dialogue.%s.text"
speaker = "dialogue.%s.speaker"
portrait = ""
duration = 0.0
""" % [dialogue_id, dialogue_id, dialogue_id]

	var file: FileAccess = FileAccess.open(output_path, FileAccess.WRITE)
	if not file:
		push_error("DialogueResourceGenerator: Failed to write file: %s" % output_path)
		return

	file.store_string(tres_content)
	file.close()

	print("DialogueResourceGenerator: Generated %s" % output_path)
