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
	select_warrior_button.pressed.connect(_on_card_selected.bind(CardIDs.FIRE_RECRUIT))
	select_archer_button.pressed.connect(_on_card_selected.bind(CardIDs.EMBER_SLINGER))

func _on_card_selected(catalog_id: StringName) -> void:
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
				if profile_repo.has_method("save_profile"):
					profile_repo.call("save_profile", true)  # Force immediate save
				print("FirstCardSelection: Set starter deck as active!")

	# Mark onboarding event as completed
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", "event_onboarding")
		print("FirstCardSelection: Marked onboarding event as completed!")

	# Continue to campaign map
	get_tree().change_scene_to_file("res://scenes/ui/campaign_map.tscn")
