extends Control
class_name HeroSelection

## HeroSelection - Choose your starting hero
##
## Part of onboarding flow. Player picks one of five heroes representing the
## four core elements (Earth, Fire, Air, Water) plus a random option.
## Hero choice is saved to profile for future use.

@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2
@onready var select_button3: Button = %SelectButton3
@onready var select_button4: Button = %SelectButton4
@onready var select_button5: Button = %SelectButton5

var dialogue_manager: Node = null

# Core elemental heroes
const HERO_EARTH: String = "earth_hero"
const HERO_FIRE: String = "fire_hero"
const HERO_RANDOM: String = "random_hero"
const HERO_AIR: String = "air_hero"
const HERO_WATER: String = "water_hero"

func _ready() -> void:
	print("HeroSelection: Initializing...")

	# Hide hero buttons initially - will show after Merlin's dialogue
	select_button1.visible = false
	select_button2.visible = false
	select_button3.visible = false
	select_button4.visible = false
	select_button5.visible = false

	# Connect all hero selection buttons
	select_button1.pressed.connect(_on_hero_selected.bind(HERO_EARTH))
	select_button2.pressed.connect(_on_hero_selected.bind(HERO_FIRE))
	select_button3.pressed.connect(_on_hero_selected.bind(HERO_RANDOM))
	select_button4.pressed.connect(_on_hero_selected.bind(HERO_AIR))
	select_button5.pressed.connect(_on_hero_selected.bind(HERO_WATER))

	# Start Merlin's introduction dialogue
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")
	if dialogue_manager and dialogue_manager.has_signal("dialogue_ended") and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.connect("dialogue_ended", _on_merlin_dialogue_ended)
		dialogue_manager.call("start_dialogue", "merlin_hero_intro")
	else:
		# Fallback if dialogue system unavailable
		print("HeroSelection: DialogueManager not found, showing hero selection immediately")
		_show_hero_selection()

func _on_merlin_dialogue_ended() -> void:
	print("HeroSelection: Merlin dialogue complete, showing hero selection")
	_show_hero_selection()

func _show_hero_selection() -> void:
	select_button1.visible = true
	select_button2.visible = true
	select_button3.visible = true
	select_button4.visible = true
	select_button5.visible = true

func _on_hero_selected(hero_id: String) -> void:
	print("HeroSelection: Player selected hero: %s" % hero_id)

	# Save hero choice to profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile: Dictionary = profile_repo.call("get_active_profile")
		if not profile.is_empty():
			profile["meta"]["selected_hero"] = hero_id
			profile_repo.call("save_profile")

	# Mark affinity selection event as completed
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", "event_affinity")
		print("HeroSelection: Marked affinity selection as completed!")

	# Return to campaign map
	SceneManager.change_scene(SceneManager.SCENE_CAMPAIGN_MAP)
