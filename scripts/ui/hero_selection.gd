extends Control
class_name HeroSelection

## HeroSelection - Choose your starting hero
##
## Part of onboarding flow. Player picks one of two placeholder heroes.
## Hero choice is saved to profile for future use.

@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2

const HERO_1 = "warrior_chief"
const HERO_2 = "arcane_sage"

func _ready() -> void:
	print("HeroSelection: Initializing...")

	# Connect buttons
	select_button1.pressed.connect(_on_hero_selected.bind(HERO_1))
	select_button2.pressed.connect(_on_hero_selected.bind(HERO_2))

func _on_hero_selected(hero_id: String) -> void:
	print("HeroSelection: Player selected hero: %s" % hero_id)

	# Save hero choice to profile
	var profile_repo = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile = profile_repo.get_active_profile()
		if not profile.is_empty():
			profile["meta"]["selected_hero"] = hero_id
			profile_repo.save_profile()

	# Continue to first card selection
	get_tree().change_scene_to_file("res://scenes/ui/first_card_selection.tscn")
