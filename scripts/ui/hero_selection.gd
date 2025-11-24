extends Control
class_name HeroSelection

## HeroSelection - Choose your starting hero
##
## Part of onboarding flow. Player picks one of five heroes representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Hero choice is saved to profile via ProfileRepo.set_starting_hero()
## If random is chosen, grants "Fortune Favors the Bold" trait.

@onready var hero_container: HBoxContainer = %HeroContainer

var dialogue_manager: Node = null
var hero_cards: Array[HeroCard] = []

# Hero card scene
const HeroCardScene: PackedScene = preload("res://scenes/ui/hero_card.tscn")

# Core elemental heroes (match HeroCatalog hero_ids)
const HERO_EARTH: String = "hero_earth"
const HERO_FIRE: String = "hero_fire"
const HERO_RANDOM: String = "random"
const HERO_WIND: String = "hero_wind"
const HERO_WATER: String = "hero_water"

func _ready() -> void:
	print("HeroSelection: Initializing...")

	# Create hero cards
	_create_hero_cards()

	# Hide hero cards initially - will show after Merlin's dialogue
	if hero_container:
		hero_container.visible = false

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
	if hero_container:
		hero_container.visible = true

## Create hero cards dynamically
func _create_hero_cards() -> void:
	if not hero_container:
		push_error("HeroSelection: HeroContainer not found!")
		return

	# List of heroes to display (4 core + random)
	var heroes_to_show: Array[String] = [
		HERO_EARTH,
		HERO_FIRE,
		HERO_WIND,
		HERO_WATER,
		HERO_RANDOM
	]

	for hero_id: String in heroes_to_show:
		var card: HeroCard = HeroCardScene.instantiate()
		hero_container.add_child(card)
		hero_cards.append(card)

		# Special handling for "random" option
		if hero_id == HERO_RANDOM:
			_setup_random_card(card)
		else:
			card.set_hero(hero_id)

		# Connect signal
		card.hero_selected.connect(_on_hero_selected)

## Setup the random hero card
func _setup_random_card(card: HeroCard) -> void:
	# Set random card data manually since it's not in catalog
	card.hero_id = HERO_RANDOM

	if card.hero_name_label:
		card.hero_name_label.text = "Random"
	if card.element_label:
		card.element_label.text = "???"
	if card.description_label:
		card.description_label.text = "Let fate choose your path"
	if card.hp_label:
		card.hp_label.text = "HP: ???"
	if card.mana_label:
		card.mana_label.text = "Mana: ???"
	if card.regen_label:
		card.regen_label.text = "Regen: ???"

func _on_hero_selected(hero_id: String) -> void:
	print("HeroSelection: Player selected hero: %s" % hero_id)

	# Handle random selection
	var final_hero_id: String = hero_id
	var chosen_random: bool = false
	if hero_id == HERO_RANDOM:
		chosen_random = true
		# Pick random hero from starting pool
		var random_pool: Array = [HERO_EARTH, HERO_FIRE, HERO_WIND, HERO_WATER]
		final_hero_id = random_pool[randi() % random_pool.size()]
		print("HeroSelection: Random selection chose: %s" % final_hero_id)

	# Save hero choice to profile using new ProfileRepo method
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("set_starting_hero"):
		var success: bool = profile_repo.call("set_starting_hero", final_hero_id, chosen_random)
		if success:
			print("HeroSelection: Successfully set starting hero: %s (random: %s)" % [final_hero_id, chosen_random])
		else:
			push_error("HeroSelection: Failed to set starting hero!")
	else:
		push_error("HeroSelection: ProfileRepo.set_starting_hero() not available!")

	# Mark affinity selection event as completed
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", "event_affinity")
		print("HeroSelection: Marked affinity selection as completed!")

	# Return to campaign map
	SceneManager.change_scene(SceneManager.SCENE_CAMPAIGN_MAP)
