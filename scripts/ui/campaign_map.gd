extends Control
class_name CampaignMap

## Campaign Map - Visual node-based event progression
##
## Shows campaign events as nodes on a linear path.
## First event is onboarding if not yet complete.

## Node references
@onready var back_button: Button = %BackButton
@onready var center_button: Button = %CenterButton
@onready var progress_label: Label = %ProgressLabel
@onready var map_scroll: ScrollContainer = %MapScrollContainer
@onready var map_container: Control = %MapContainer
@onready var map_background: TextureRect = %MapBackground
@onready var detail_panel: Panel = %DetailPanel
@onready var popup_background: NinePatchRect = get_node_or_null("%Background")
@onready var event_name_label: Label = %EventNameLabel
@onready var difficulty_label: Label = %DifficultyLabel
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

## Asset paths - Replace these with real artwork when available
## Map background: Place your map texture here (any resolution, will scale to fit)
const MAP_BACKGROUND_TEXTURE: String = "res://assets/ui/campaign_map_background.png"

## Popup panel: 9-slice texture for the detail popup background
## Recommended 9-slice margins: 40px on all sides for rounded corners
## Texture size: 200x200 minimum (margins will be preserved, center will stretch)
const POPUP_NINEPATCH_TEXTURE: String = "res://assets/ui/popup_panel_9slice.png"
const POPUP_NINEPATCH_MARGINS: Rect2 = Rect2(40, 40, 40, 40)  # left, top, right, bottom

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

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignMap: Initializing...")

	# Connect buttons
	back_button.pressed.connect(_on_back_pressed)
	center_button.pressed.connect(_on_center_latest_pressed)
	start_event_button.pressed.connect(_on_start_event_pressed)
	deck_selector.item_selected.connect(_on_deck_selected)

	# Load map background texture if available
	if FileAccess.file_exists(MAP_BACKGROUND_TEXTURE):
		var map_tex: Texture2D = load(MAP_BACKGROUND_TEXTURE)
		if map_tex:
			map_background.texture = map_tex
			print("CampaignMap: Loaded map background texture")
	else:
		# Use placeholder color if no texture
		map_background.modulate = Color(0.15, 0.15, 0.2)

	# Load popup 9-slice texture if available
	if FileAccess.file_exists(POPUP_NINEPATCH_TEXTURE):
		var popup_tex: Texture2D = load(POPUP_NINEPATCH_TEXTURE)
		if popup_tex:
			popup_background.texture = popup_tex
			# Apply 9-slice margins
			popup_background.region_rect = POPUP_NINEPATCH_MARGINS
			popup_background.patch_margin_left = int(POPUP_NINEPATCH_MARGINS.position.x)
			popup_background.patch_margin_top = int(POPUP_NINEPATCH_MARGINS.position.y)
			popup_background.patch_margin_right = int(POPUP_NINEPATCH_MARGINS.size.x)
			popup_background.patch_margin_bottom = int(POPUP_NINEPATCH_MARGINS.size.y)
			print("CampaignMap: Loaded popup 9-slice texture")
	else:
		# Use placeholder style if no texture
		var popup_style: StyleBoxFlat = StyleBoxFlat.new()
		popup_style.bg_color = Color(0.15, 0.15, 0.2)
		popup_style.corner_radius_top_left = 20
		popup_style.corner_radius_top_right = 20
		popup_style.corner_radius_bottom_left = 20
		popup_style.corner_radius_bottom_right = 20
		popup_style.border_width_left = 2
		popup_style.border_width_right = 2
		popup_style.border_width_top = 2
		popup_style.border_width_bottom = 2
		popup_style.border_color = Color(0.4, 0.5, 0.7)
		detail_panel.add_theme_stylebox_override("panel", popup_style)

	# Connect to campaign service
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		if campaign.has_signal("battle_completed"):
			var battle_completed_signal: Signal = campaign.get("battle_completed")
			battle_completed_signal.connect(_on_event_completed)
		if campaign.has_signal("campaign_progress_changed"):
			var campaign_progress_signal: Signal = campaign.get("campaign_progress_changed")
			campaign_progress_signal.connect(_on_progress_changed)

	# Load and display map
	_refresh_map()
	_update_progress_display()

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
		var base_y: float = 800.0  # Center of 1600px height
		var wave_amplitude: float = 300.0  # Vertical variation from center
		var y_offset: float = sin(float(index) * 0.5) * wave_amplitude
		node_position = Vector2(start_x + index * NODE_SPACING, base_y + y_offset)
		if event_data.has("map_position"):
			push_warning("CampaignMap: Invalid map_position format for event, using calculated position")

	node_container.position = node_position

	var event_id: String = _safe_string(event_data.get("id", ""))
	var event_type: String = _safe_string(event_data.get("event_type", "battle"))

	# Create visual button
	var button: Button = Button.new()
	button.custom_minimum_size = NODE_SIZE
	button.size = NODE_SIZE

	# Style based on event type and state
	var is_onboarding_event: bool = (event_type == "onboarding")

	if is_onboarding_event:
		button.text = "⭐"
		button.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
	elif is_completed:
		button.text = "✓"
		button.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	elif is_unlocked:
		button.text = str(index + 1)
		button.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0))
	else:
		button.text = "🔒"
		button.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		button.disabled = true

	# Make button circular-ish
	var style: StyleBoxFlat = StyleBoxFlat.new()
	if is_onboarding_event:
		style.bg_color = Color(0.4, 0.35, 0.2)
		style.border_color = Color(1.0, 0.85, 0.3)
	elif is_completed:
		style.bg_color = Color(0.2, 0.4, 0.2)
		style.border_color = Color(0.3, 1.0, 0.3)
	elif is_unlocked:
		style.bg_color = Color(0.3, 0.3, 0.4)
		style.border_color = Color(0.5, 0.7, 1.0)
	else:
		style.bg_color = Color(0.2, 0.2, 0.2)
		style.border_color = Color(0.4, 0.4, 0.4)

	style.corner_radius_top_left = 40
	style.corner_radius_top_right = 40
	style.corner_radius_bottom_left = 40
	style.corner_radius_bottom_right = 40
	style.border_width_left = 3
	style.border_width_right = 3
	style.border_width_top = 3
	style.border_width_bottom = 3

	button.add_theme_stylebox_override("normal", style)
	button.add_theme_font_size_override("font_size", 32)

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

func _update_detail_panel() -> void:
	if selected_event_id == "":
		event_name_label.text = "Select an Event"
		difficulty_label.text = ""
		description_label.text = "Click an event node to see details."
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
	var event_type: String = _safe_string(event.get("event_type", "battle"))

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
	if difficulty > 0:
		var diff_stars: String = "★".repeat(difficulty) + "☆".repeat(5 - difficulty)
		difficulty_label.text = "Difficulty: %s" % diff_stars
	else:
		difficulty_label.text = ""

	description_label.text = _safe_string(event.get("description", "No description."), "No description.")

	# Reward summary
	var reward_type: String = _safe_string(event.get("reward_type", "fixed"), "fixed")
	var reward_cards: Array = _safe_array(event.get("reward_cards", []))
	var reward_text: String = ""

	if reward_cards.size() > 0:
		match reward_type:
			"fixed":
				var card_names: Array[String] = []
				for reward_item: Variant in reward_cards:
					var reward: Dictionary = _safe_dict(reward_item)
					var count: int = _safe_int(reward.get("count", 1), 1)
					var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
					if count > 1:
						card_names.append("%dx %s" % [count, catalog_id.capitalize()])
					else:
						card_names.append(catalog_id.capitalize())
				reward_text = "Reward: " + ", ".join(card_names)

			"choice":
				var options: Array[String] = []
				for reward_item: Variant in reward_cards:
					var reward: Dictionary = _safe_dict(reward_item)
					var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
					options.append(catalog_id.capitalize())
				reward_text = "Reward: Choose from " + ", ".join(options)

			"random":
				var count: int = 0
				for reward_item: Variant in reward_cards:
					var reward: Dictionary = _safe_dict(reward_item)
					var reward_count: int = _safe_int(reward.get("count", 1), 1)
					count += reward_count
				reward_text = "Reward: Random (%d cards)" % count

	reward_label.text = reward_text

	# Enable/disable start button based on completion and repeatability
	var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", selected_event_id))
	var is_repeatable: bool = _safe_bool(event.get("repeatable", true))

	if is_completed:
		if is_repeatable:
			start_event_button.text = "REPLAY (no reward)"
			start_event_button.disabled = false
		else:
			# Non-repeatable events cannot be replayed
			start_event_button.text = "COMPLETED"
			start_event_button.disabled = true
	else:
		if event_type == "onboarding":
			start_event_button.text = "START"
		else:
			start_event_button.text = "START EVENT"
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
		deck_info_label.text = "Error: Decks service unavailable"
		return

	# Get all decks
	var decks_variant: Variant = decks.call("list_decks")
	var decks_array: Array = _safe_array(decks_variant)
	available_decks.assign(decks_array)

	if available_decks.is_empty():
		deck_selector.add_item("No decks available")
		deck_info_label.text = "Create a deck first"
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
	deck_info_label.text = "%d cards" % card_count

	# Validate deck and show status
	var is_valid: bool = _validate_selected_deck()
	if is_valid:
		active_deck_indicator.text = "✓ Ready"
		active_deck_indicator.modulate = Color(0.3, 1.0, 0.3)
	else:
		active_deck_indicator.text = "⚠ Invalid deck"
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
## PROGRESS DISPLAY
## =============================================================================

func _update_progress_display() -> void:
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var completed_events: Array = _safe_array(campaign.call("get_completed_battles"))
	var completed: int = completed_events.size()

	var total_events: Array = _safe_array(campaign.call("get_all_battles"))
	var total: int = total_events.size()

	progress_label.text = "%d / %d Complete" % [completed, total]

## =============================================================================
## EVENT START
## =============================================================================

func _on_start_event_pressed() -> void:
	if selected_event_id == "":
		return

	# Get event data to check event_type
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var event: Dictionary = _safe_dict(campaign.call("get_battle", selected_event_id))
	if event.is_empty():
		return

	var event_type: String = _safe_string(event.get("event_type", "battle"))
	var requires_deck: bool = _safe_bool(event.get("requires_deck", true), true)

	# Handle affinity selection event - route to hero selection
	if event_type == "affinity":
		print("CampaignMap: Starting affinity selection...")
		SceneManager.change_scene(SceneManager.SCENE_HERO_SELECTION)
		return

	# Handle first summon event - route to first card selection
	if event_type == "first_summon":
		print("CampaignMap: Starting first summon selection...")
		SceneManager.change_scene(SceneManager.SCENE_FIRST_CARD_SELECTION)
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
	if event_type == "caravan":
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
		SceneManager.change_scene(SceneManager.SCENE_SHOP_SCREEN)
		return

	# Handle battle events
	print("CampaignMap: Starting battle event: %s" % selected_event_id)

	# Validate deck selection only if this event requires a deck
	if requires_deck:
		if selected_deck_id.is_empty():
			push_error("CampaignMap: No deck selected!")
			# Update UI to show error
			active_deck_indicator.text = "⚠ Select a deck first!"
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
	SceneManager.change_scene(SceneManager.SCENE_BATTLE_3D)

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("CampaignMap: Returning to game mode menu")
	SceneManager.change_scene(SceneManager.SCENE_GAME_MODE_MENU)

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
## SIGNALS
## =============================================================================

func _on_event_completed(_event_id: String) -> void:
	_refresh_map()
	_update_progress_display()
	_update_detail_panel()

func _on_progress_changed() -> void:
	_update_progress_display()
