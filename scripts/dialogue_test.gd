extends Control

## DialogueTest - Test scene for the dialogue system
##
## Provides buttons to test different dialogue scenarios

func _ready() -> void:
	pass

func _on_start_simple_dialogue_pressed() -> void:
	DialogueManager.start_dialogue("simple_greeting")

func _on_start_choice_dialogue_pressed() -> void:
	DialogueManager.start_dialogue("choice_example")

func _on_start_chain_dialogue_pressed() -> void:
	DialogueManager.start_dialogue("chain_start")
