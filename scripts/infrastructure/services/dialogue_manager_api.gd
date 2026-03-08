class_name DialogueManagerApi
extends RefCounted

static func start_dialogue(dialogue_id: String, context: Dictionary = {}) -> void:
	DialogueManager.call("start_dialogue", dialogue_id, context)
