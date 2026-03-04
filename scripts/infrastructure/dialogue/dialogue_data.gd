extends Resource
class_name DialogueData

## Represents a single dialogue node with text, character info, and optional choices

## Unique identifier for this dialogue
@export var dialogue_id: String = ""

## Character speaking this dialogue
@export var character_name: String = ""

## Optional portrait/icon for the character
@export var portrait: Texture2D = null

## Array of dialogue lines to display sequentially
## Each line is shown one at a time with typewriter effect
@export var lines: Array[String] = []

## Optional choices presented after all lines are shown
## If empty, dialogue auto-advances to next_dialogue_id
@export var choices: Array[DialogueChoice] = []

## ID of the next dialogue to load after this one completes
## Only used if choices array is empty
@export var next_dialogue_id: String = ""

## Whether this dialogue should automatically continue to next
## If false, requires player input to advance
@export var auto_advance: bool = false
