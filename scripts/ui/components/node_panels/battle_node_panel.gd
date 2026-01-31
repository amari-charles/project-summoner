class_name BattleNodePanel
extends NodeDetailPanelBase

## BattleNodePanel - Detail panel for battle/combat events
##
## Shows event info, difficulty, rewards, and deck selection UI.
## Two-column layout with info on left, deck on right.

## Kenny UI Pack star textures for difficulty display
const STAR_FILLED_TEXTURE: String = "res://assets/ui/kenny/PNG/Yellow/Default/star.png"
const STAR_EMPTY_TEXTURE: String = "res://assets/ui/kenny/PNG/Grey/Default/star_outline.png"
const STAR_SIZE: int = 24

## Visual styling
const CLAIMED_DIM_COLOR: Color = Color(0.6, 0.6, 0.6, 1)

## UI References (set from scene)
@onready var event_name_label: Label = %EventNameLabel
@onready var difficulty_container: HBoxContainer = %DifficultyContainer
@onready var difficulty_label: Label = %DifficultyLabel
@onready var stars_container: HBoxContainer = %StarsContainer
@onready var description_label: Label = %DescriptionLabel
@onready var rewards_container: VBoxContainer = %RewardsContainer
@onready var first_clear_section: VBoxContainer = %FirstClearSection
@onready var first_clear_header: Label = %FirstClearHeader
@onready var first_clear_rewards: Label = %FirstClearRewards
@onready var first_clear_status: Label = %FirstClearStatus
@onready var every_battle_section: VBoxContainer = %EveryBattleSection
@onready var every_battle_header: Label = %EveryBattleHeader
@onready var every_battle_rewards: Label = %EveryBattleRewards
@onready var deck_column: VBoxContainer = %DeckColumn
@onready var deck_header_label: Label = %DeckHeaderLabel
@onready var active_deck_label: Label = %ActiveDeckLabel
@onready var change_deck_button: Button = %ChangeDeckButton
@onready var deck_selector: ItemList = %DeckSelector
@onready var deck_info_label: Label = %DeckInfoLabel
@onready var active_deck_indicator: Label = %ActiveDeckIndicator
@onready var start_button: Button = %StartButton

## Deck selection state
var available_decks: Array[Dictionary] = []
var selected_deck_id: String = ""
var _deck_selector_visible: bool = false


func _ready() -> void:
	# Connect buttons
	start_button.pressed.connect(_on_start_pressed)
	deck_selector.item_selected.connect(_on_deck_selected)
	change_deck_button.pressed.connect(_on_change_deck_pressed)

	# Set localized static labels
	deck_header_label.text = Loc.t("campaign.map.deck_header")


func _configure_impl() -> void:
	# Show/hide deck selection based on event configuration
	deck_column.visible = event.requires_deck

	# Load available decks if required
	if event.requires_deck:
		_load_decks()

	# Update labels using typed accessors
	event_name_label.text = event.name
	_update_difficulty_stars(event.difficulty)
	description_label.text = event.description if not event.description.is_empty() else "No description."

	# Build reward text
	_update_reward_display()

	# Update start button
	start_button.text = get_start_button_text()
	start_button.disabled = is_start_disabled()


func get_event_type() -> StringName:
	return EventTypeIDs.BATTLE


func can_start() -> bool:
	if not event.requires_deck:
		return true
	return not selected_deck_id.is_empty() and _validate_selected_deck()


## =============================================================================
## DIFFICULTY STARS
## =============================================================================

func _update_difficulty_stars(difficulty: int) -> void:
	# Clear existing stars
	for child: Node in stars_container.get_children():
		child.queue_free()

	# Hide entire container if no difficulty
	if difficulty <= 0:
		difficulty_container.visible = false
		return

	difficulty_container.visible = true
	difficulty_label.text = Loc.t("campaign.map.difficulty_label")

	# Load star textures
	var filled_tex: Texture2D = load(STAR_FILLED_TEXTURE)
	var empty_tex: Texture2D = load(STAR_EMPTY_TEXTURE)

	# Create 5 stars (filled for difficulty level, empty for rest)
	for i: int in range(5):
		var star: TextureRect = TextureRect.new()
		star.texture = filled_tex if i < difficulty else empty_tex
		star.custom_minimum_size = Vector2(STAR_SIZE, STAR_SIZE)
		star.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		star.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		stars_container.add_child(star)


## =============================================================================
## REWARD DISPLAY
## =============================================================================

func _update_reward_display() -> void:
	var is_completed: bool = Campaign.is_battle_completed(event.id)

	# Set headers
	first_clear_header.text = Loc.t("campaign.rewards.first_clear_header")
	every_battle_header.text = Loc.t("campaign.rewards.every_battle_header")

	# Build first clear rewards (gold + card)
	_update_first_clear_section(is_completed)

	# Build every battle rewards (XP)
	_update_every_battle_section()


func _update_first_clear_section(is_completed: bool) -> void:
	var catalog: Node = CardCatalog
	var reward_lines: Array[String] = []

	# Gold reward
	if event.gold_reward > 0:
		reward_lines.append(Loc.t("campaign.rewards.gold", {"amount": event.gold_reward}))

	# Card reward based on type
	match event.reward_type:
		RewardTypeIDs.FIXED:
			var reward_cards: Array = event.reward_cards
			if reward_cards.size() > 0:
				var card_names: Array[String] = []
				for reward_item: Variant in reward_cards:
					var reward: Dictionary = SafeTypeUtils.dict(reward_item)
					var count: int = SafeTypeUtils.int_val(reward.get("count", 1), 1)
					var catalog_id: String = SafeTypeUtils.string(reward.get("catalog_id", ""))
					var card_name: String = _get_card_display_name(catalog, catalog_id)
					if count > 1:
						card_names.append("%dx %s" % [count, card_name])
					else:
						card_names.append(card_name)
				reward_lines.append(Loc.t("campaign.rewards.fixed", {"cards": ", ".join(card_names)}))

		RewardTypeIDs.FLEXIBLE:
			reward_lines.append(Loc.t("campaign.rewards.card_choice"))

		RewardTypeIDs.NONE:
			pass  # No card reward line

	first_clear_rewards.text = "\n".join(reward_lines)

	# Show claimed status if completed
	if is_completed:
		first_clear_status.text = Loc.t("campaign.rewards.claimed")
		first_clear_status.visible = true
		# Dim the rewards text
		first_clear_rewards.modulate = CLAIMED_DIM_COLOR
	else:
		first_clear_status.text = ""
		first_clear_status.visible = false
		first_clear_rewards.modulate = Color(1, 1, 1, 1)

	# Hide section if no first-clear rewards
	first_clear_section.visible = reward_lines.size() > 0


func _update_every_battle_section() -> void:
	var reward_lines: Array[String] = []

	# Summoner XP reward
	if event.summoner_xp_reward > 0:
		reward_lines.append(Loc.t("campaign.rewards.summoner_xp", {"amount": event.summoner_xp_reward}))

	# Card XP is always earned but we don't have a fixed amount in event data
	# For now, just show summoner XP. Card XP is displayed on reward screen.

	every_battle_rewards.text = "\n".join(reward_lines)

	# Hide section if no every-battle rewards
	every_battle_section.visible = reward_lines.size() > 0


## =============================================================================
## DECK SELECTION
## =============================================================================

func _load_decks() -> void:
	deck_selector.clear()
	available_decks.clear()

	# Get active summoner ID to filter decks
	var active_summoner_id: String = ""
	var result: Variant = SummonerSelection.GetActiveSummonerId()
	if result is String:
		active_summoner_id = result

	# Get decks filtered by active summoner
	var decks_array: Array
	if not active_summoner_id.is_empty():
		var decks_variant: Variant = Decks.list_decks_for_summoner(active_summoner_id)
		decks_array = SafeTypeUtils.array(decks_variant)
	else:
		var decks_variant: Variant = Decks.list_decks()
		decks_array = SafeTypeUtils.array(decks_variant)
	available_decks.assign(decks_array)

	if available_decks.is_empty():
		active_deck_label.text = Loc.t("campaign.map.error_create_deck_first")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		change_deck_button.visible = false
		return

	change_deck_button.visible = true

	# Populate ItemList with deck names
	for deck: Dictionary in available_decks:
		var deck_name: String = SafeTypeUtils.string(deck.get("name", "Unnamed Deck"), "Unnamed Deck")
		deck_selector.add_item(deck_name)

	# Get currently selected deck from profile
	var profile_variant: Variant = ProfileRepo.get_active_profile()
	var profile: Dictionary = SafeTypeUtils.dict(profile_variant)
	var found_deck: bool = false
	if not profile.is_empty() and profile.has("meta"):
		var meta: Dictionary = SafeTypeUtils.dict(profile.get("meta"))
		var active_deck: String = SafeTypeUtils.string(meta.get("selected_deck", ""))

		# Find the deck in available_decks and select it
		for i: int in range(available_decks.size()):
			var deck: Dictionary = available_decks[i]
			var deck_id: String = SafeTypeUtils.string(deck.get("id", ""))
			if deck_id == active_deck:
				deck_selector.select(i)
				selected_deck_id = deck_id
				found_deck = true
				break

	# Auto-select first deck if none selected
	if not found_deck and available_decks.size() > 0:
		var first_deck: Dictionary = available_decks[0]
		selected_deck_id = SafeTypeUtils.string(first_deck.get("id", ""))
		deck_selector.select(0)
		_save_deck_selection()

	_update_deck_info()


func _on_deck_selected(index: int) -> void:
	if index < 0 or index >= available_decks.size():
		return

	var deck: Dictionary = available_decks[index]
	selected_deck_id = SafeTypeUtils.string(deck.get("id", ""))

	_save_deck_selection()
	_update_deck_info()

	# Hide selector after selection
	_deck_selector_visible = false
	deck_selector.visible = false
	change_deck_button.text = Loc.t("campaign.map.change_deck")

	# Update start button state
	start_button.disabled = is_start_disabled()


func _on_change_deck_pressed() -> void:
	_deck_selector_visible = not _deck_selector_visible
	deck_selector.visible = _deck_selector_visible
	if _deck_selector_visible:
		change_deck_button.text = Loc.t("campaign.map.done")
	else:
		change_deck_button.text = Loc.t("campaign.map.change_deck")


func _save_deck_selection() -> void:
	ProfileRepo.update_profile_meta({"selected_deck": selected_deck_id})


func _update_deck_info() -> void:
	if selected_deck_id.is_empty():
		active_deck_label.text = Loc.t("campaign.map.no_deck_selected")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Find the selected deck
	var selected_deck: Dictionary = {}
	for deck: Dictionary in available_decks:
		if SafeTypeUtils.string(deck.get("id", "")) == selected_deck_id:
			selected_deck = deck
			break

	if selected_deck.is_empty():
		active_deck_label.text = Loc.t("campaign.map.no_deck_selected")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Show deck name
	var deck_name: String = SafeTypeUtils.string(selected_deck.get("name", "Unnamed Deck"), "Unnamed Deck")
	active_deck_label.text = deck_name

	# Show card count
	var card_instance_ids: Array = SafeTypeUtils.array(selected_deck.get("card_instance_ids", []))
	var card_count: int = card_instance_ids.size()
	deck_info_label.text = Loc.t("campaign.map.deck_card_count", {"count": card_count})

	# Validate deck and show status
	var is_valid: bool = _validate_selected_deck()
	if is_valid:
		active_deck_indicator.text = Loc.t("campaign.map.deck_status_ready")
		active_deck_indicator.modulate = Color(0.3, 1.0, 0.3)
	else:
		active_deck_indicator.text = Loc.t("campaign.map.deck_status_invalid")
		active_deck_indicator.modulate = Color(1.0, 0.5, 0.0)


func _validate_selected_deck() -> bool:
	if selected_deck_id.is_empty():
		return false
	var is_valid_variant: Variant = Decks.validate_deck(selected_deck_id)
	return SafeTypeUtils.bool_val(is_valid_variant, false)


## =============================================================================
## START EVENT
## =============================================================================

func _on_start_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	# Validate deck if required
	if event.requires_deck:
		if selected_deck_id.is_empty():
			active_deck_indicator.text = Loc.t("campaign.map.deck_status_select_first")
			active_deck_indicator.modulate = Color(1.0, 0.3, 0.0)
			return

		if not _validate_selected_deck():
			return

	start_requested.emit()
