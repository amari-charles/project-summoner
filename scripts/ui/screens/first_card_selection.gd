extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Fire Elemental or Earth Sprite as their starter.
## Card is granted to collection and onboarding is marked complete.

# Deck name constant
const STARTER_DECK_NAME: String = "Starter Deck"

@onready var select_fire_elemental_button: Button = %SelectFireElementalButton
@onready var select_earth_sprite_button: Button = %SelectEarthSpriteButton

var dialogue_manager: Node = null

func _ready() -> void:
	print("FirstCardSelection: Initializing...")

	# Connect button handlers
	select_fire_elemental_button.pressed.connect(_on_card_selected.bind(CardIDs.FIRE_ELEMENTAL))
	select_earth_sprite_button.pressed.connect(_on_card_selected.bind(CardIDs.EARTH_SPRITE))

	# Start Merlin's introduction dialogue (buttons visible alongside dialogue)
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")
	if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.call("start_dialogue", "merlin_first_card_intro")
	else:
		print("FirstCardSelection: DialogueManager not found")

func _on_card_selected(catalog_id: StringName) -> void:
	print("FirstCardSelection: Player selected card: %s" % catalog_id)

	# Check if event already completed (idempotent - safe to run multiple times)
	var campaign: Node = get_node("/root/Campaign")
	var already_completed: bool = false
	if campaign and campaign.has_method("is_battle_completed"):
		var result: Variant = campaign.call("is_battle_completed", BattleIDs.EVENT_FIRST_SUMMON)
		already_completed = result if result is bool else false

	if already_completed:
		print("FirstCardSelection: First summon event already completed, skipping")
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
		return

	# Grant the chosen card to collection
	var collection: Node = get_node("/root/Collection")
	var card_instance_id: String = ""
	if collection and collection.has_method("grant_card"):
		var result: Variant = collection.call("grant_card", catalog_id, RarityIDs.COMMON)
		card_instance_id = result if result is String else ""
		print("FirstCardSelection: Granted %s to collection (instance: %s)" % [catalog_id, card_instance_id])

	# Find or create STARTER_DECK_NAME
	var decks: Node = get_node("/root/Decks")
	var deck_id: String = ""

	if decks and card_instance_id != "":
		# Search for existing STARTER_DECK_NAME
		if decks.has_method("list_decks"):
			var all_decks: Array[Dictionary] = decks.call("list_decks")
			for deck_dict: Dictionary in all_decks:
				if deck_dict.get("name", "") == STARTER_DECK_NAME:
					deck_id = deck_dict.get("id", "")
					print("FirstCardSelection: Found existing Starter Deck (id: %s)" % deck_id)
					break

		# If deck exists, add card to it; otherwise create new deck
		if deck_id != "":
			if decks.has_method("add_card_to_deck"):
				decks.call("add_card_to_deck", deck_id, card_instance_id)
				print("FirstCardSelection: Added card to existing Starter Deck")
		else:
			# Get the player's unlocked summoner to assign to the deck
			var summoner_id: String = _get_first_unlocked_summoner()
			if decks.has_method("create_deck"):
				var result: Variant = decks.call("create_deck", STARTER_DECK_NAME, [card_instance_id], summoner_id)
				deck_id = result if result is String else ""
				print("FirstCardSelection: Created new Starter Deck (id: %s) with summoner '%s'" % [deck_id, summoner_id])

		# Set as active deck
		if deck_id != "":
			var profile_repo: Node = get_node("/root/ProfileRepo")
			if profile_repo and profile_repo.has_method("get_active_profile"):
				var profile_variant: Variant = profile_repo.call("get_active_profile")
				var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
				if not profile.is_empty():
					profile["meta"]["selected_deck"] = deck_id
					if profile_repo.has_method("save_profile"):
						profile_repo.call("save_profile", true)  # Force immediate save
					print("FirstCardSelection: Set Starter Deck as active!")

	# Mark event as completed
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", BattleIDs.EVENT_FIRST_SUMMON)
		print("FirstCardSelection: Marked first summon event as completed!")

	# Return to campaign map
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Get the first unlocked summoner from the profile
func _get_first_unlocked_summoner() -> String:
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		return ""

	if profile_repo.has_method("get_unlocked_summoners"):
		var unlocked_variant: Variant = profile_repo.call("get_unlocked_summoners")
		if unlocked_variant is Array:
			var unlocked: Array = unlocked_variant
			if unlocked.size() > 0:
				var first_summoner: Variant = unlocked[0]
				if first_summoner is String:
					return first_summoner
	return ""
