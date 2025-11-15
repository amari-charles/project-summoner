extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Fire Recruit or Ember Slinger as their starter.
## Card is granted to collection and onboarding is marked complete.

@onready var select_warrior_button: Button = %SelectWarriorButton
@onready var select_archer_button: Button = %SelectArcherButton

func _ready() -> void:
	print("FirstCardSelection: Initializing...")

	# Connect buttons
	select_warrior_button.pressed.connect(_on_card_selected.bind("fire_recruit"))
	select_archer_button.pressed.connect(_on_card_selected.bind("ember_slinger"))

func _on_card_selected(catalog_id: String) -> void:
	print("FirstCardSelection: Player selected card: %s" % catalog_id)

	# Grant the chosen card to collection
	var collection: Node = get_node("/root/Collection")
	var card_instance_id: String = ""
	if collection and collection.has_method("grant_card"):
		var result: Variant = collection.call("grant_card", catalog_id, "common")
		card_instance_id = result if result is String else ""
		print("FirstCardSelection: Granted %s to collection (instance: %s)" % [catalog_id, card_instance_id])

	# Create initial deck with this card
	var decks: Node = get_node("/root/Decks")
	if decks and card_instance_id != "" and decks.has_method("create_deck"):
		var result: Variant = decks.call("create_deck", "Starter Deck", [card_instance_id])
		var deck_id: String = result if result is String else ""
		print("FirstCardSelection: Created starter deck with card (deck_id: %s)" % deck_id)

		# Set it as the active deck
		var profile_repo: Node = get_node("/root/ProfileRepo")
		if profile_repo and profile_repo.has_method("get_active_profile"):
			var profile_variant: Variant = profile_repo.call("get_active_profile")
			var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
			if not profile.is_empty():
				profile["meta"]["selected_deck"] = deck_id
				profile["meta"]["onboarding_complete"] = true
				if profile_repo.has_method("save_profile"):
					profile_repo.call("save_profile", true)  # Force immediate save
				print("FirstCardSelection: Set starter deck as active and marked onboarding complete!")
	else:
		# Fallback: just mark onboarding complete even if deck creation failed
		var profile_repo: Node = get_node("/root/ProfileRepo")
		if profile_repo and profile_repo.has_method("get_active_profile"):
			var profile_variant: Variant = profile_repo.call("get_active_profile")
			var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
			if not profile.is_empty():
				profile["meta"]["onboarding_complete"] = true
				if profile_repo.has_method("save_profile"):
					profile_repo.call("save_profile")
				print("FirstCardSelection: Onboarding complete (no deck created)!")

	# Continue to campaign screen
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")
