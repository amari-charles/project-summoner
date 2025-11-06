extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Warrior or Archer as their starter.
## Card is granted to collection and onboarding is marked complete.

@onready var select_warrior_button: Button = %SelectWarriorButton
@onready var select_archer_button: Button = %SelectArcherButton

func _ready() -> void:
	print("FirstCardSelection: Initializing...")

	# Connect buttons
	select_warrior_button.pressed.connect(_on_card_selected.bind("warrior"))
	select_archer_button.pressed.connect(_on_card_selected.bind("archer"))

func _on_card_selected(catalog_id: String) -> void:
	print("FirstCardSelection: Player selected card: %s" % catalog_id)

	# Grant the chosen card to collection
	var collection = get_node("/root/Collection")
	var card_instance_id: String = ""
	if collection:
		card_instance_id = collection.grant_card(catalog_id, "common")
		print("FirstCardSelection: Granted %s to collection (instance: %s)" % [catalog_id, card_instance_id])

	# Create initial deck with this card
	var decks = get_node("/root/Decks")
	if decks and card_instance_id != "":
		var deck_id = decks.create_deck("Starter Deck", [card_instance_id])
		print("FirstCardSelection: Created starter deck with card (deck_id: %s)" % deck_id)

		# Set it as the active deck
		var profile_repo = get_node("/root/ProfileRepo")
		if profile_repo:
			var profile = profile_repo.get_active_profile()
			if not profile.is_empty():
				profile["meta"]["selected_deck"] = deck_id
				profile["meta"]["onboarding_complete"] = true
				profile_repo.save_profile(true)  # Force immediate save
				print("FirstCardSelection: Set starter deck as active and marked onboarding complete!")
	else:
		# Fallback: just mark onboarding complete even if deck creation failed
		var profile_repo = get_node("/root/ProfileRepo")
		if profile_repo:
			var profile = profile_repo.get_active_profile()
			if not profile.is_empty():
				profile["meta"]["onboarding_complete"] = true
				profile_repo.save_profile()
				print("FirstCardSelection: Onboarding complete (no deck created)!")

	# Continue to campaign screen
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")
