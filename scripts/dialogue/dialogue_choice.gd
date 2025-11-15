extends Resource
class_name DialogueChoice

## Represents a single choice option in a dialogue

## The text displayed for this choice
@export var choice_text: String = ""

## ID of the next dialogue to load when this choice is selected
@export var next_dialogue_id: String = ""

## Optional condition that must be true for this choice to be available
## Format: "variable_name" - checks if DialogueManager.variables[variable_name] is true
@export var condition: String = ""

## Optional action to execute when this choice is selected
## Format: "variable_name=value" - sets DialogueManager.variables[variable_name] = value
@export var action: String = ""
