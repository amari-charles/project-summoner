extends Control

## DialogueTest - Test scene for the dialogue system
##
## Provides buttons to test different dialogue scenarios

var dialogue_manager: Node = null

func _ready() -> void:
	# Wait for autoload
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")

	if not dialogue_manager:
		push_error("DialogueTest: DialogueManager not found! Make sure it's added to autoloads.")

func _on_start_simple_dialogue_pressed() -> void:
	if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.call("start_dialogue", "simple_greeting")
	else:
		print("DialogueManager not available")

func _on_start_choice_dialogue_pressed() -> void:
	if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.call("start_dialogue", "choice_example")
	else:
		print("DialogueManager not available")

func _on_start_chain_dialogue_pressed() -> void:
	if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.call("start_dialogue", "chain_start")
	else:
		print("DialogueManager not available")
