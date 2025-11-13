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

# Core elemental heroes
const HERO_EARTH: String = "earth_hero"
const HERO_FIRE: String = "fire_hero"
const HERO_RANDOM: String = "random_hero"
const HERO_AIR: String = "air_hero"
const HERO_WATER: String = "water_hero"

func _ready() -> void:
	print("HeroSelection: Initializing...")

	# Connect all hero selection buttons
	select_button1.pressed.connect(_on_hero_selected.bind(HERO_EARTH))
	select_button2.pressed.connect(_on_hero_selected.bind(HERO_FIRE))
	select_button3.pressed.connect(_on_hero_selected.bind(HERO_RANDOM))
	select_button4.pressed.connect(_on_hero_selected.bind(HERO_AIR))
	select_button5.pressed.connect(_on_hero_selected.bind(HERO_WATER))

func _on_hero_selected(hero_id: String) -> void:
	print("HeroSelection: Player selected hero: %s" % hero_id)

	# Save hero choice to profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile: Dictionary = profile_repo.call("get_active_profile")
		if not profile.is_empty():
			profile["meta"]["selected_hero"] = hero_id
			profile_repo.call("save_profile")

	# Continue to first card selection
	get_tree().change_scene_to_file("res://scenes/ui/first_card_selection.tscn")
