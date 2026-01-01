extends Control
class_name CampaignMap

## Campaign Map - Visual node-based event progression
##
## Shows campaign events as nodes on a linear path.
## First event is onboarding if not yet complete.

## Preloads
const SummonerIconWidgetScene: PackedScene = preload("res://scenes/ui/components/summoner_icon_widget.tscn")
const HamburgerButtonScene: PackedScene = preload("res://scenes/ui/components/hamburger_button.tscn")
const NavDrawerScene: PackedScene = preload("res://scenes/ui/components/nav_drawer.tscn")
const SnapshotManagerScene: PackedScene = preload("res://scenes/ui/screens/snapshot_manager.tscn")
const CampaignSelectorModalScene: PackedScene = preload("res://scenes/ui/components/campaign_selector_modal.tscn")

## Node references
@onready var locator_button: TextureButton = %LocatorButton
@onready var map_scroll: ScrollContainer = %MapScrollContainer
@onready var map_container: Control = %MapContainer
@onready var screen_background: ColorRect = %ScreenBackground
@onready var map_background: ColorRect = %MapBackground
@onready var detail_panel: Panel = %DetailPanel
@onready var event_name_label: Label = %EventNameLabel
@onready var difficulty_container: HBoxContainer = %DifficultyContainer
@onready var difficulty_label: Label = %DifficultyLabel
@onready var stars_container: HBoxContainer = %StarsContainer
@onready var description_label: Label = %DescriptionLabel
@onready var reward_label: Label = %RewardLabel
@onready var deck_column: VBoxContainer = $DetailPanel/MarginContainer/VBoxContainer/ContentColumns/RightColumn
@onready var deck_selector: ItemList = %DeckSelector
@onready var deck_info_label: Label = %DeckInfoLabel
@onready var active_deck_indicator: Label = %ActiveDeckIndicator
@onready var start_event_button: Button = %StartEventButton

## Map layout constants
const NODE_SPACING: float = 150.0  # Horizontal spacing between nodes
const NODE_SIZE: Vector2 = Vector2(80, 80)
const PATH_COLOR: Color = Color(0.4, 0.4, 0.5)
const PATH_WIDTH: float = 4.0
const MAP_CENTER_Y: float = 800.0  # Vertical center of 1600px map height
const MAP_WAVE_AMPLITUDE: float = 300.0  # Vertical variation for winding path

## UI positioning constants
const HAMBURGER_BUTTON_MARGIN: float = 20.0
const HAMBURGER_BUTTON_SIZE: float = 48.0
const CAMPAIGN_BANNER_MARGIN: float = 20.0
const CAMPAIGN_BANNER_WIDTH: float = 220.0  # Fits campaign name
const CAMPAIGN_BANNER_HEIGHT: float = 40.0
const SUMMONER_ICON_SIZE: float = 50.0
const SUMMONER_ICON_MARGIN: float = 20.0

## Kenny UI Pack star textures for difficulty display
const STAR_FILLED_TEXTURE: String = "res://assets/ui/kenny/PNG/Yellow/Default/star.png"
const STAR_EMPTY_TEXTURE: String = "res://assets/ui/kenny/PNG/Grey/Default/star_outline.png"
const STAR_SIZE: int = 24  # Size of each star icon

## Kenny UI Pack textures for event nodes
const EVENT_NODE_TEXTURE: String = "res://assets/ui/kenny/PNG/Grey/Default/button_round_depth_flat.png"
const EVENT_NODE_CHECKMARK: String = "res://assets/ui/kenny/PNG/Green/Default/icon_checkmark.png"
const CHECKMARK_SIZE: int = 32  # Size of checkmark overlay

## State
var selected_event_id: String = ""
var all_events: Array[Dictionary] = []
var event_nodes: Dictionary = {}  # event_id -> Control (fast lookup)
var event_render_order: Array[String] = []  # Explicit draw order

## Panning state
var is_panning: bool = false
var pan_start_position: Vector2 = Vector2.ZERO
var last_mouse_position: Vector2 = Vector2.ZERO
const PAN_THRESHOLD: float = 5.0  # Pixels to move before panning starts

## Deck selection state
var available_decks: Array[Dictionary] = []
var selected_deck_id: String = ""

## Summoner icon widget reference
var summoner_icon: SummonerIconWidget = null

## Navigation components
var hamburger_button: HamburgerButton = null
var nav_drawer: NavDrawer = null
var snapshot_manager: Node = null

## Campaign selector components
var campaign_banner: Button = null
var campaign_selector_modal: CampaignSelectorModal = null

## =============================================================================
## TYPE HELPERS
## =============================================================================

func _safe_string(variant: Variant, default: String = "") -> String:
	return variant if variant is String else default

func _safe_int(variant: Variant, default: int = 0) -> int:
	return variant if variant is int else default

func _safe_dict(variant: Variant) -> Dictionary:
	return variant if variant is Dictionary else {}

func _safe_array(variant: Variant) -> Array:
	return variant if variant is Array else []

func _safe_bool(variant: Variant, default: bool = false) -> bool:
	return variant if variant is bool else default

## Get display name for a card from CardCatalog, with fallback
func _get_card_display_name(catalog: Node, catalog_id: String) -> String:
	if catalog and catalog.has_method("get_card"):
		var card_data: Dictionary = _safe_dict(catalog.call("get_card", catalog_id))
		if not card_data.is_empty():
			return _safe_string(card_data.get("card_name", catalog_id), catalog_id)
	# Fallback: convert catalog_id to title case (fire_elemental → Fire Elemental)
	return catalog_id.replace("_", " ").capitalize()

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignMap: Initializing...")

	# Connect buttons
	locator_button.pressed.connect(_on_center_latest_pressed)
	start_event_button.pressed.connect(_on_start_event_pressed)
	deck_selector.item_selected.connect(_on_deck_selected)

	# Background texture is set directly in the scene file

	# Setup detail panel with fantasy border
	_setup_detail_panel_border()

	# Connect to campaign service
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		if campaign.has_signal("battle_completed"):
			var battle_completed_signal: Signal = campaign.get("battle_completed")
			battle_completed_signal.connect(_on_event_completed)
		if campaign.has_signal("campaign_progress_changed"):
			var campaign_progress_signal: Signal = campaign.get("campaign_progress_changed")
			campaign_progress_signal.connect(_on_progress_changed)

	# Connect to summoner selection changes
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_selection_changed)

	# Setup navigation (hamburger menu + nav drawer)
	_setup_navigation()

	# Setup campaign banner (top-left)
	_setup_campaign_banner()

	# Auto-redirect: If on onboarding but it's complete, switch to main campaign
	_check_auto_redirect_from_onboarding()

	# Setup summoner icon
	_setup_summoner_icon()

	# Load and display map
	_refresh_map()

	# Auto-scroll to latest mission (deferred to next frame so nodes are fully laid out)
	call_deferred("_on_center_latest_pressed")

func _draw() -> void:
	# Draw paths connecting events using explicit render order
	for i: int in range(event_render_order.size() - 1):
		var current_id: String = event_render_order[i]
		var next_id: String = event_render_order[i + 1]

		if not event_nodes.has(current_id):
			push_warning("CampaignMap: Missing node for event '%s'" % current_id)
			continue
		if not event_nodes.has(next_id):
			push_warning("CampaignMap: Missing node for event '%s'" % next_id)
			continue

		var start_node: Control = event_nodes[current_id]
		var end_node: Control = event_nodes[next_id]
		var start_pos: Vector2 = start_node.position + start_node.size / 2
		var end_pos: Vector2 = end_node.position + end_node.size / 2
		draw_line(start_pos, end_pos, PATH_COLOR, PATH_WIDTH)

## =============================================================================
## AUTO-REDIRECT FROM COMPLETED ONBOARDING
## =============================================================================

func _check_auto_redirect_from_onboarding() -> void:
	var campaign: Node = get_node_or_null("/root/Campaign")
	if not campaign:
		return

	# Check if currently on onboarding campaign
	var current_campaign_id: String = ""
	if campaign.has_method("get_current_campaign_id"):
		current_campaign_id = campaign.call("get_current_campaign_id")

	if current_campaign_id != String(CampaignIDs.ONBOARDING):
		return  # Not on onboarding, no redirect needed

	# Check if onboarding is complete
	if campaign.has_method("is_onboarding_complete"):
		var is_complete: bool = campaign.call("is_onboarding_complete")
		if is_complete:
			# Switch to academy trials (main campaign)
			print("CampaignMap: Onboarding complete, auto-switching to Academy Trials")
			if campaign.has_method("set_current_campaign"):
				campaign.call("set_current_campaign", String(CampaignIDs.ACADEMY_TRIALS))
				_update_campaign_banner_text()

## =============================================================================
## MAP DISPLAY
## =============================================================================

func _refresh_map() -> void:
	# Clear existing state
	for child: Node in map_container.get_children():
		child.queue_free()
	event_nodes.clear()
	event_render_order.clear()

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("CampaignMap: Campaign service not found!")
		return

	var events_variant: Variant = campaign.call("get_all_battles")
	var events_array: Array = _safe_array(events_variant)
	all_events.assign(events_array)

	print("CampaignMap: Loaded %d total battles from Campaign service" % all_events.size())
	for event: Dictionary in all_events:
		print("  - %s (unlocked: %s, completed: %s)" % [
			event.get("id", "unknown"),
			campaign.call("is_battle_unlocked", event.get("id", "")),
			campaign.call("is_battle_completed", event.get("id", ""))
		])

	# Calculate centered starting position
	var event_count: int = all_events.size()
	var total_width: float = (event_count - 1) * NODE_SPACING if event_count > 0 else 0.0
	var map_width: float = map_container.custom_minimum_size.x
	var start_x: float = (map_width - total_width) / 2.0

	# Create nodes for all events and build render order
	var node_index: int = 0
	for event: Dictionary in all_events:
		var event_id: String = _safe_string(event.get("id", ""))
		if event_id.is_empty():
			push_warning("CampaignMap: Event missing 'id', skipping")
			continue

		var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", event_id))
		var is_unlocked: bool = _safe_bool(campaign.call("is_battle_unlocked", event_id))

		var event_node: Control = _create_event_node(event, node_index, start_x, is_unlocked, is_completed)
		map_container.add_child(event_node)
		event_nodes[event_id] = event_node
		event_render_order.append(event_id)
		node_index += 1

	# Trigger redraw for paths
	queue_redraw()

	print("CampaignMap: Created %d event nodes" % event_nodes.size())

func _create_event_node(event_data: Dictionary, index: int, start_x: float, is_unlocked: bool, is_completed: bool) -> Control:
	var node_container: Control = Control.new()
	node_container.custom_minimum_size = NODE_SIZE

	# Use map_position if available, otherwise calculate position
	var node_position: Vector2
	if event_data.has("map_position") and event_data.get("map_position") is Vector2:
		# Use fixed position from event data
		node_position = event_data.get("map_position")
	else:
		# Calculate position: winding path using sine wave
		var y_offset: float = sin(float(index) * 0.5) * MAP_WAVE_AMPLITUDE
		node_position = Vector2(start_x + index * NODE_SPACING, MAP_CENTER_Y + y_offset)
		if event_data.has("map_position"):
			push_warning("CampaignMap: Invalid map_position format for event, using calculated position")

	node_container.position = node_position

	var event_id: String = _safe_string(event_data.get("id", ""))
	var event_type: StringName = StringName(event_data.get("event_type", EventTypeIDs.BATTLE))
	var is_onboarding_event: bool = (event_type == EventTypeIDs.ONBOARDING)

	# Load the round button texture
	var node_texture: Texture2D = load(EVENT_NODE_TEXTURE)

	# Create texture button with Kenny round button
	var button: TextureButton = TextureButton.new()
	button.texture_normal = node_texture
	button.custom_minimum_size = NODE_SIZE
	button.size = NODE_SIZE
	button.ignore_texture_size = true
	button.stretch_mode = TextureButton.STRETCH_KEEP_ASPECT_CENTERED

	# Set opacity based on state (grey buttons, differentiate by opacity)
	if not is_unlocked:
		button.modulate = Color(1, 1, 1, 0.5)  # Locked: 50% opacity
		button.disabled = true
	else:
		button.modulate = Color(1, 1, 1, 1.0)  # Unlocked: full opacity

	# Add overlay icon for completed events
	if is_completed:
		var checkmark: TextureRect = TextureRect.new()
		checkmark.texture = load(EVENT_NODE_CHECKMARK)
		checkmark.custom_minimum_size = Vector2(CHECKMARK_SIZE, CHECKMARK_SIZE)
		checkmark.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		checkmark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		# Center the checkmark on the button
		checkmark.anchor_left = 0.5
		checkmark.anchor_right = 0.5
		checkmark.anchor_top = 0.5
		checkmark.anchor_bottom = 0.5
		checkmark.offset_left = -CHECKMARK_SIZE / 2.0
		checkmark.offset_right = CHECKMARK_SIZE / 2.0
		checkmark.offset_top = -CHECKMARK_SIZE / 2.0
		checkmark.offset_bottom = CHECKMARK_SIZE / 2.0
		button.add_child(checkmark)

	# Add star icon for onboarding events
	if is_onboarding_event:
		var star: TextureRect = TextureRect.new()
		star.texture = load(STAR_FILLED_TEXTURE)
		star.custom_minimum_size = Vector2(CHECKMARK_SIZE, CHECKMARK_SIZE)
		star.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		star.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		# Center the star on the button
		star.anchor_left = 0.5
		star.anchor_right = 0.5
		star.anchor_top = 0.5
		star.anchor_bottom = 0.5
		star.offset_left = -CHECKMARK_SIZE / 2.0
		star.offset_right = CHECKMARK_SIZE / 2.0
		star.offset_top = -CHECKMARK_SIZE / 2.0
		star.offset_bottom = CHECKMARK_SIZE / 2.0
		button.add_child(star)

	# Connect click handler
	if is_unlocked:
		button.pressed.connect(_on_event_node_clicked.bind(event_id))

	node_container.add_child(button)
	return node_container

func _on_event_node_clicked(event_id: String) -> void:
	selected_event_id = event_id
	_update_detail_panel()
	_show_popup()
	print("CampaignMap: Selected event: %s" % event_id)

func _show_popup() -> void:
	# Center the popup on screen
	var viewport_size: Vector2 = get_viewport_rect().size
	var popup_size: Vector2 = detail_panel.custom_minimum_size
	detail_panel.position = (viewport_size - popup_size) / 2
	detail_panel.size = popup_size
	detail_panel.visible = true

func _input(event: InputEvent) -> void:
	# Handle mouse button events
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton

		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			if mouse_event.pressed:
				# Check if clicking outside popup to dismiss it
				if detail_panel.visible:
					var popup_rect: Rect2 = Rect2(detail_panel.position, detail_panel.size)
					if not popup_rect.has_point(mouse_event.position):
						detail_panel.visible = false
						get_viewport().set_input_as_handled()
						return

				# Record potential panning start (but don't claim input yet to allow button clicks)
				if not detail_panel.visible:
					var scroll_rect: Rect2 = map_scroll.get_global_rect()
					if scroll_rect.has_point(mouse_event.position):
						pan_start_position = mouse_event.position
						last_mouse_position = mouse_event.position
			else:
				# Stop panning on release
				is_panning = false

	# Handle mouse motion for panning
	elif event is InputEventMouseMotion:
		var motion_event: InputEventMouseMotion = event as InputEventMouseMotion

		# Only start panning if mouse button is held and we've moved beyond threshold
		if motion_event.button_mask & MOUSE_BUTTON_MASK_LEFT:
			if not is_panning and not detail_panel.visible:
				# Check if we've moved beyond the threshold to start panning
				var distance: float = motion_event.position.distance_to(pan_start_position)
				if distance > PAN_THRESHOLD:
					var scroll_rect: Rect2 = map_scroll.get_global_rect()
					if scroll_rect.has_point(motion_event.position):
						is_panning = true
						last_mouse_position = motion_event.position

			if is_panning:
				var delta: Vector2 = motion_event.position - last_mouse_position

				# Update scroll positions (inverted - drag right scrolls left)
				map_scroll.scroll_horizontal -= int(delta.x)
				map_scroll.scroll_vertical -= int(delta.y)

				last_mouse_position = motion_event.position
				get_viewport().set_input_as_handled()
		else:
			# Reset pan tracking when button not held
			pan_start_position = Vector2.ZERO

## =============================================================================
## DETAIL PANEL
## =============================================================================

## Set up the detail panel border texture using ButtonStyleFactory
func _setup_detail_panel_border() -> void:
	var panel_bg: NinePatchRect = get_node_or_null("%Background")
	if not panel_bg:
		return
	ButtonStyleFactory.apply_panel_border(panel_bg)

	# Remove default panel style (we're using NinePatchRect instead)
	detail_panel.add_theme_stylebox_override("panel", StyleBoxEmpty.new())


## Update difficulty display with Kenny star icons
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


func _update_detail_panel() -> void:
	if selected_event_id == "":
		event_name_label.text = Loc.t("campaign.map.select_event")
		_update_difficulty_stars(0)  # Hide stars
		description_label.text = Loc.t("campaign.map.click_to_see_details")
		reward_label.text = ""
		start_event_button.disabled = true
		# Clear deck selection UI
		deck_selector.clear()
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Get event data
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var event: Dictionary = _safe_dict(campaign.call("get_battle", selected_event_id))
	if event.is_empty():
		return

	# Check if this event requires deck selection
	var requires_deck: bool = _safe_bool(event.get("requires_deck", true), true)
	var event_type: StringName = StringName(event.get("event_type", EventTypeIDs.BATTLE))

	# Show/hide deck selection based on event configuration
	if deck_column:
		deck_column.visible = requires_deck

	# Load available decks only if required
	if requires_deck:
		_load_decks()

	# Update labels
	event_name_label.text = _safe_string(event.get("name", "Unknown"), "Unknown")

	# Show difficulty for battles, hide for other event types
	var difficulty: int = _safe_int(event.get("difficulty", 0), 0)
	_update_difficulty_stars(difficulty)

	description_label.text = _safe_string(event.get("description", "No description."), "No description.")

	# Reward summary
	var reward_type: StringName = StringName(event.get("reward_type", RewardTypeIDs.FIXED))
	var reward_cards: Array = _safe_array(event.get("reward_cards", []))
	var reward_text: String = ""

	# Get CardCatalog for proper card names
	var catalog: Node = get_node_or_null("/root/CardCatalog")

	if reward_cards.size() > 0 and reward_type == RewardTypeIDs.FIXED:
		var card_names: Array[String] = []
		for reward_item: Variant in reward_cards:
			var reward: Dictionary = _safe_dict(reward_item)
			var count: int = _safe_int(reward.get("count", 1), 1)
			var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
			var card_name: String = _get_card_display_name(catalog, catalog_id)
			if count > 1:
				card_names.append("%dx %s" % [count, card_name])
			else:
				card_names.append(card_name)
		reward_text = Loc.t("campaign.rewards.fixed", {"cards": ", ".join(card_names)})

	reward_label.text = reward_text

	# Enable/disable start button based on completion and repeatability
	var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", selected_event_id))
	var is_repeatable: bool = _safe_bool(event.get("repeatable", true))

	if is_completed:
		if is_repeatable:
			start_event_button.text = Loc.t("campaign.map.button_replay")
			start_event_button.disabled = false
		else:
			# Non-repeatable events cannot be replayed
			start_event_button.text = Loc.t("campaign.map.button_completed")
			start_event_button.disabled = true
	else:
		if event_type == EventTypeIDs.ONBOARDING:
			start_event_button.text = Loc.t("campaign.map.button_start")
		else:
			start_event_button.text = Loc.t("campaign.map.button_start_event")
		start_event_button.disabled = false

## =============================================================================
## DECK SELECTION
## =============================================================================

func _load_decks() -> void:
	# Clear existing items
	deck_selector.clear()
	available_decks.clear()

	# Get decks service
	var decks: Node = get_node("/root/Decks")
	if not decks:
		push_error("CampaignMap: Decks service not found!")
		deck_info_label.text = Loc.t("campaign.map.error_decks_unavailable")
		return

	# Get active summoner ID to filter decks
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	var active_summoner_id: String = ""
	if summoner_selection and summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String:
			active_summoner_id = result

	# Get decks filtered by active summoner
	var decks_array: Array
	if not active_summoner_id.is_empty() and decks.has_method("list_decks_for_summoner"):
		var decks_variant: Variant = decks.call("list_decks_for_summoner", active_summoner_id)
		decks_array = _safe_array(decks_variant)
	else:
		var decks_variant: Variant = decks.call("list_decks")
		decks_array = _safe_array(decks_variant)
	available_decks.assign(decks_array)

	if available_decks.is_empty():
		deck_selector.add_item(Loc.t("campaign.map.error_create_deck_first"))
		deck_info_label.text = Loc.t("campaign.map.error_create_deck_first")
		active_deck_indicator.text = ""
		return

	# Populate ItemList with deck names
	for deck: Dictionary in available_decks:
		var deck_name: String = _safe_string(deck.get("name", "Unnamed Deck"), "Unnamed Deck")
		deck_selector.add_item(deck_name)

	# Get currently selected deck from profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile_variant: Variant = profile_repo.call("get_active_profile")
		var profile: Dictionary = _safe_dict(profile_variant)
		if not profile.is_empty() and profile.has("meta"):
			var meta: Dictionary = _safe_dict(profile.get("meta"))
			var active_deck: String = _safe_string(meta.get("selected_deck", ""))

			# Find the deck in available_decks and select it
			for i: int in range(available_decks.size()):
				var deck: Dictionary = available_decks[i]
				var deck_id: String = _safe_string(deck.get("id", ""))
				if deck_id == active_deck:
					deck_selector.select(i)
					selected_deck_id = deck_id
					break

	# Update deck info display
	_update_deck_info()

func _on_deck_selected(index: int) -> void:
	if index < 0 or index >= available_decks.size():
		return

	var deck: Dictionary = available_decks[index]
	selected_deck_id = _safe_string(deck.get("id", ""))

	# Update deck info display
	_update_deck_info()

	# Save selection to profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile_variant: Variant = profile_repo.call("get_active_profile")
		var profile: Dictionary = _safe_dict(profile_variant)
		if not profile.is_empty():
			if not profile.has("meta"):
				profile["meta"] = {}
			var meta: Dictionary = _safe_dict(profile.get("meta"))
			meta["selected_deck"] = selected_deck_id
			profile_repo.call("save_profile", true)  # Immediate save

	print("CampaignMap: Selected deck: %s" % selected_deck_id)

func _update_deck_info() -> void:
	if selected_deck_id.is_empty():
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Find the selected deck
	var selected_deck: Dictionary = {}
	for deck: Dictionary in available_decks:
		if _safe_string(deck.get("id", "")) == selected_deck_id:
			selected_deck = deck
			break

	if selected_deck.is_empty():
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Show card count
	var card_ids: Array = _safe_array(selected_deck.get("card_ids", []))
	var card_count: int = card_ids.size()
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

	var decks: Node = get_node("/root/Decks")
	if not decks:
		return false

	var is_valid_variant: Variant = decks.call("validate_deck", selected_deck_id)
	return _safe_bool(is_valid_variant, false)

## =============================================================================
## EVENT START
## =============================================================================

func _on_start_event_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_event_id == "":
		return

	# Get event data to check event_type
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var event: Dictionary = _safe_dict(campaign.call("get_battle", selected_event_id))
	if event.is_empty():
		return

	var event_type: StringName = StringName(event.get("event_type", EventTypeIDs.BATTLE))
	var requires_deck: bool = _safe_bool(event.get("requires_deck", true), true)

	# Handle affinity selection event - route to summoner selection
	if event_type == EventTypeIDs.AFFINITY:
		print("CampaignMap: Starting affinity selection...")
		SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
		return

	# Handle first summon event - route to first card selection
	if event_type == EventTypeIDs.FIRST_SUMMON:
		print("CampaignMap: Starting first summon selection...")
		SceneManager.transition_to(SceneManager.SCENE_FIRST_CARD_SELECTION)
		return

	# Handle caravan events - navigate directly to shop with event context
	#
	# CARAVAN EVENT FLOW:
	# 1. CampaignMap configures EventContext with event data
	# 2. Navigate directly to ShopScreen (not EventScreen)
	# 3. ShopScreen detects EventContext.current_event_id is set
	# 4. ShopScreen switches to caravan mode:
	#    - Hides back button, shows "Leave Caravan" button
	#    - Loads event sequence from event_config
	#    - Plays dialogue on top of shop UI (seamless browsing during dialogue)
	# 5. After dialogue completes, "Leave Caravan" button becomes visible
	# 6. User can browse/purchase, then click "Leave Caravan"
	# 7. Confirmation modal appears (repeatable check)
	# 8. On confirm: EventContext.complete_event() marks event done
	# 9. NavigationContext returns to SCENE_CAMPAIGN_MAP
	#
	# This approach allows dialogue to play while shop is visible, avoiding
	# the jarring dialogue → black screen → shop transition.
	if event_type == EventTypeIDs.CARAVAN:
		print("CampaignMap: Starting caravan event: %s" % selected_event_id)

		# Check if event is already completed and not repeatable
		var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", selected_event_id), false)
		var is_repeatable: bool = _safe_bool(event.get("repeatable", false), false)

		if is_completed and not is_repeatable:
			push_warning("CampaignMap: Cannot start event '%s' - already completed and not repeatable" % selected_event_id)
			return

		# Configure EventContext with this event (ShopScreen will detect this)
		EventContext.configure_event(selected_event_id, SceneManager.SCENE_CAMPAIGN_MAP)

		# Navigate to shop (ShopScreen will play event sequence on top of UI)
		NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
		SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)
		return

	# Handle battle events
	print("CampaignMap: Starting battle event: %s" % selected_event_id)

	# Validate deck selection only if this event requires a deck
	if requires_deck:
		if selected_deck_id.is_empty():
			push_error("CampaignMap: No deck selected!")
			# Update UI to show error
			active_deck_indicator.text = Loc.t("campaign.map.deck_status_select_first")
			active_deck_indicator.modulate = Color(1.0, 0.3, 0.0)
			return

		if not _validate_selected_deck():
			push_error("CampaignMap: Selected deck is invalid!")
			# Error already shown in UI by _update_deck_info
			return

	# Store selected event in campaign service
	var profile_repo: Node = get_node("/root/ProfileRepo")
	var profile: Dictionary = _safe_dict(profile_repo.call("get_active_profile"))
	if not profile.is_empty():
		if not profile.has("campaign_progress"):
			profile["campaign_progress"] = {}
		var campaign_progress: Dictionary = _safe_dict(profile["campaign_progress"])
		campaign_progress["current_battle"] = selected_event_id
		profile_repo.call("save_profile", true)

	# Configure battle context
	print("CampaignMap: Configuring BattleContext with battle_id='%s'" % selected_event_id)
	BattleContext.configure_campaign_battle(selected_event_id)

	# Launch battle scene
	print("CampaignMap: Launching battle scene...")
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_center_latest_pressed() -> void:
	var latest_unlocked_id: String = _find_latest_unlocked_mission()
	if latest_unlocked_id.is_empty():
		print("CampaignMap: No unlocked missions to center on")
		return

	_scroll_to_event(latest_unlocked_id)
	print("CampaignMap: Centered on latest mission: %s" % latest_unlocked_id)

func _find_latest_unlocked_mission() -> String:
	# Iterate through event_render_order in reverse
	# Return first event that is unlocked but not completed
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return ""

	for i: int in range(event_render_order.size() - 1, -1, -1):
		var event_id: String = event_render_order[i]
		var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", event_id))
		var is_unlocked: bool = _safe_bool(campaign.call("is_battle_unlocked", event_id))

		if is_unlocked and not is_completed:
			return event_id

	# If all completed, return last event
	if event_render_order.size() > 0:
		return event_render_order[event_render_order.size() - 1]

	return ""

func _scroll_to_event(event_id: String) -> void:
	if not event_nodes.has(event_id):
		push_warning("CampaignMap: Cannot scroll to missing event '%s'" % event_id)
		return

	var node: Control = event_nodes[event_id]
	var node_center_x: float = node.position.x + node.size.x / 2
	var node_center_y: float = node.position.y + node.size.y / 2

	# Calculate scroll position to center the node in viewport (both X and Y)
	var viewport_width: float = map_scroll.size.x
	var viewport_height: float = map_scroll.size.y

	var scroll_target_x: float = node_center_x - (viewport_width / 2)
	scroll_target_x = max(0, scroll_target_x)  # Clamp to valid range

	var scroll_target_y: float = node_center_y - (viewport_height / 2)
	scroll_target_y = max(0, scroll_target_y)  # Clamp to valid range

	# Set scroll position (both horizontal and vertical)
	map_scroll.scroll_horizontal = int(scroll_target_x)
	map_scroll.scroll_vertical = int(scroll_target_y)

## =============================================================================
## NAVIGATION (Hamburger Menu + Nav Drawer)
## =============================================================================

func _setup_navigation() -> void:
	# Create hamburger button in top-right corner
	hamburger_button = HamburgerButtonScene.instantiate()
	add_child(hamburger_button)

	hamburger_button.anchor_left = 1.0
	hamburger_button.anchor_right = 1.0
	hamburger_button.anchor_top = 0.0
	hamburger_button.anchor_bottom = 0.0
	hamburger_button.offset_left = -(HAMBURGER_BUTTON_MARGIN + HAMBURGER_BUTTON_SIZE)
	hamburger_button.offset_right = -HAMBURGER_BUTTON_MARGIN
	hamburger_button.offset_top = HAMBURGER_BUTTON_MARGIN
	hamburger_button.offset_bottom = HAMBURGER_BUTTON_MARGIN + HAMBURGER_BUTTON_SIZE

	hamburger_button.pressed.connect(_on_hamburger_pressed)

	# Create nav drawer (hidden by default)
	nav_drawer = NavDrawerScene.instantiate()
	add_child(nav_drawer)

	# Connect nav drawer signals
	nav_drawer.collection_pressed.connect(_on_nav_collection_pressed)
	nav_drawer.events_pressed.connect(_on_nav_events_pressed)
	nav_drawer.shop_pressed.connect(_on_nav_shop_pressed)
	nav_drawer.settings_pressed.connect(_on_nav_settings_pressed)

	# Connect debug-only signals
	if OS.is_debug_build():
		nav_drawer.snapshots_pressed.connect(_on_nav_snapshots_pressed)

func _on_hamburger_pressed() -> void:
	if nav_drawer:
		nav_drawer.open()

func _on_nav_collection_pressed() -> void:
	print("CampaignMap: Opening Collection...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

func _on_nav_events_pressed() -> void:
	print("CampaignMap: Opening Special Events...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SPECIAL_EVENTS)

func _on_nav_shop_pressed() -> void:
	print("CampaignMap: Opening Premium Store...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_PREMIUM_STORE)

func _on_nav_settings_pressed() -> void:
	print("CampaignMap: Opening Settings...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SETTINGS)

func _on_nav_snapshots_pressed() -> void:
	print("CampaignMap: Opening Snapshot Manager...")
	if snapshot_manager == null:
		snapshot_manager = SnapshotManagerScene.instantiate()
		add_child(snapshot_manager)
	if snapshot_manager.has_method("show_manager"):
		snapshot_manager.show_manager()

## =============================================================================
## CAMPAIGN BANNER
## =============================================================================

func _setup_campaign_banner() -> void:
	# Create container for banner styling
	var banner_container: Control = Control.new()
	add_child(banner_container)

	# Position centered at top
	banner_container.anchor_left = 0.5
	banner_container.anchor_right = 0.5
	banner_container.anchor_top = 0.0
	banner_container.anchor_bottom = 0.0
	banner_container.offset_left = -CAMPAIGN_BANNER_WIDTH / 2
	banner_container.offset_right = CAMPAIGN_BANNER_WIDTH / 2
	banner_container.offset_top = CAMPAIGN_BANNER_MARGIN
	banner_container.offset_bottom = CAMPAIGN_BANNER_MARGIN + CAMPAIGN_BANNER_HEIGHT

	# Dark background
	var dark_bg: ColorRect = ColorRect.new()
	dark_bg.color = Color(0.12, 0.1, 0.15, 0.95)
	dark_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(dark_bg)

	# Fantasy border overlay
	var border: NinePatchRect = NinePatchRect.new()
	ButtonStyleFactory.apply_panel_border(border)
	border.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(border)

	# Button on top
	campaign_banner = Button.new()
	campaign_banner.flat = true
	campaign_banner.alignment = HORIZONTAL_ALIGNMENT_CENTER
	campaign_banner.add_theme_font_size_override("font_size", 20)
	campaign_banner.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(campaign_banner)

	_update_campaign_banner_text()
	campaign_banner.pressed.connect(_on_campaign_banner_pressed)

func _update_campaign_banner_text() -> void:
	if not campaign_banner:
		return

	var campaign: Node = get_node_or_null("/root/Campaign")
	if campaign and campaign.has_method("get_current_campaign_id"):
		var campaign_id: String = campaign.call("get_current_campaign_id")
		if campaign.has_method("get_campaign"):
			var campaign_data: Dictionary = campaign.call("get_campaign", campaign_id)
			var name_key: String = campaign_data.get("name_key", "")
			if not name_key.is_empty():
				campaign_banner.text = Loc.t(name_key)
			else:
				campaign_banner.text = campaign_id
		else:
			campaign_banner.text = campaign_id
	else:
		campaign_banner.text = Loc.t("campaign.selector.title")

func _on_campaign_banner_pressed() -> void:
	if campaign_selector_modal == null:
		campaign_selector_modal = CampaignSelectorModalScene.instantiate()
		add_child(campaign_selector_modal)
		campaign_selector_modal.campaign_selected.connect(_on_campaign_selected)
		campaign_selector_modal.closed.connect(_on_campaign_modal_closed)

	campaign_selector_modal.open()

func _on_campaign_selected(campaign_id: String) -> void:
	var campaign: Node = get_node_or_null("/root/Campaign")
	if campaign and campaign.has_method("set_current_campaign"):
		var success: bool = campaign.call("set_current_campaign", campaign_id)
		if success:
			_update_campaign_banner_text()
			_refresh_map()

	if campaign_selector_modal:
		campaign_selector_modal.hide()

func _on_campaign_modal_closed() -> void:
	pass  # Modal hides itself

## =============================================================================
## SUMMONER ICON
## =============================================================================

func _setup_summoner_icon() -> void:
	# Only show summoner icon after affinity event is completed
	# Check shared progress since affinity is part of onboarding (account-wide)
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("get_shared_campaign_progress"):
		var shared_progress: Dictionary = profile_repo.call("get_shared_campaign_progress")
		var completed_battles: Array = shared_progress.get("completed_battles", [])
		if String(BattleIDs.EVENT_AFFINITY) not in completed_battles:
			return

	summoner_icon = SummonerIconWidgetScene.instantiate()
	add_child(summoner_icon)

	# Position in top-left corner
	summoner_icon.anchor_left = 0.0
	summoner_icon.anchor_right = 0.0
	summoner_icon.anchor_top = 0.0
	summoner_icon.anchor_bottom = 0.0
	summoner_icon.offset_left = SUMMONER_ICON_MARGIN
	summoner_icon.offset_right = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE
	summoner_icon.offset_top = SUMMONER_ICON_MARGIN
	summoner_icon.offset_bottom = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE

	# Connect signal
	summoner_icon.icon_clicked.connect(_on_summoner_icon_clicked)

func _on_summoner_icon_clicked() -> void:
	# Push current scene for return navigation
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SCREEN)

## =============================================================================
## SIGNALS
## =============================================================================

func _on_event_completed(_event_id: String) -> void:
	_refresh_map()
	_update_detail_panel()

func _on_progress_changed() -> void:
	# Full refresh when progress changes (e.g., snapshot loaded)
	_refresh_map()
	_update_detail_panel()
	_update_campaign_banner_text()
	if summoner_icon:
		summoner_icon.refresh()

func _on_summoner_selection_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	# Refresh summoner icon, map, and deck list when summoner changes
	if summoner_icon:
		summoner_icon.refresh()
	_refresh_map()
	_update_detail_panel()
