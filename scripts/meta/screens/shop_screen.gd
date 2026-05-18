extends Control
class_name ShopScreen

## ShopScreen - UI for general shop purchases
##
## Displays offerings from ShopService and handles purchase flow

## Node references
@onready var close_button: Button = %CloseButton
@onready var leave_incomplete_button: Button = %LeaveIncompleteButton
@onready var leave_complete_button: Button = %LeaveCompleteButton
@onready var gold_label: Label = %GoldLabel
@onready var offering_list: GridContainer = %OfferingList
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var offering_name_label: Label = %OfferingNameLabel
@onready var price_label: Label = %PriceLabel
@onready var description_label: Label = %DescriptionLabel
@onready var purchase_button: Button = %PurchaseButton
@onready var purchase_popup: AcceptDialog = %PurchasePopup
@onready var error_popup: AcceptDialog = %ErrorPopup

## Offering card scene
const OFFERING_CARD_SCENE: PackedScene = preload("res://scenes/meta/components/offering_card.tscn")
const OFFERING_CARD_WIDTH: int = 192
const OFFERING_GRID_GAP: int = 16
const DETAIL_PANEL_WIDTH: int = 360

## State
var current_offerings: Array = []
var selected_offering: Dictionary = {}
var shop_id: String = "general"
var is_caravan_event: bool = false
var caravan_sequence_complete: bool = false
var has_purchased: bool = false  # Tracks if player made any purchase this session
var leave_incomplete_popup: ConfirmationDialog = null
var leave_complete_popup: ConfirmationDialog = null

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	leave_incomplete_button.pressed.connect(_on_leave_incomplete_pressed)
	leave_complete_button.pressed.connect(_on_leave_complete_pressed)
	purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect shop signals
	Shop.connect("PurchaseCompleted", _on_purchase_completed)
	Shop.connect("PurchaseFailed", _on_purchase_failed)

	# Connect profile signals for gold updates
	ProfileRepo.connect("DataChangedGodot", _on_data_changed)

	# Check if this is a caravan event (EventContext is configured)
	var event_id: String = EventContext.get_current_event_id()
	if not event_id.is_empty():
		var event_config: Dictionary = EventContext.get_event_config()
		var event_shop_id: String = event_config.get("shop_id", "")

		if event_shop_id.is_empty():
			push_error("ShopScreen: Caravan event '%s' is missing shop_id! Falling back to general shop." % event_id)
		else:
			is_caravan_event = true
			shop_id = event_shop_id

			# Set up caravan-specific UI
			_setup_caravan_ui()

			# Play event sequence on top of shop UI (if configured)
			var sequence_path: String = event_config.get("event_sequence", "")
			if not sequence_path.is_empty():
				var sequence: Resource = load(sequence_path)
				if sequence:
					# Connect to sequence completion
					if not EventSequencer.sequence_finished.is_connected(_on_caravan_sequence_complete):
						EventSequencer.sequence_finished.connect(_on_caravan_sequence_complete)

					# Play sequence (dialogue will appear on top of shop)
					await get_tree().process_frame  # Wait for shop UI to be ready
					EventSequencer.play_sequence(sequence)
				else:
					# Sequence failed to load - show leave buttons immediately
					_on_caravan_sequence_complete(null)
			else:
				# No sequence configured - show leave buttons immediately
				_on_caravan_sequence_complete(null)

	# Initialize display
	_update_gold_display()
	_load_offerings()
	_clear_detail_panel()
	_update_offering_grid_columns()

func _exit_tree() -> void:
	# Disconnect signals to prevent errors
	if Shop.is_connected("PurchaseCompleted", _on_purchase_completed):
		Shop.disconnect("PurchaseCompleted", _on_purchase_completed)
	if Shop.is_connected("PurchaseFailed", _on_purchase_failed):
		Shop.disconnect("PurchaseFailed", _on_purchase_failed)
	if ProfileRepo.is_connected("DataChangedGodot", _on_data_changed):
		ProfileRepo.disconnect("DataChangedGodot", _on_data_changed)

	# Disconnect caravan-specific signals
	if is_caravan_event and EventSequencer.sequence_finished.is_connected(_on_caravan_sequence_complete):
		EventSequencer.sequence_finished.disconnect(_on_caravan_sequence_complete)

## =============================================================================
## INITIALIZATION
## =============================================================================

## Set up caravan-specific UI elements
func _setup_caravan_ui() -> void:
	# Hide close button
	close_button.visible = false

	# Both leave buttons start hidden, shown after dialogue
	leave_incomplete_button.visible = false
	leave_complete_button.visible = false

	# Create "Leave" confirmation popup (exits without completing - can return)
	leave_incomplete_popup = ConfirmationDialog.new()
	leave_incomplete_popup.dialog_text = Loc.t("shop.caravan.leave_incomplete_confirmation")
	leave_incomplete_popup.ok_button_text = Loc.t("shop.caravan.leave_incomplete_button")
	leave_incomplete_popup.cancel_button_text = Loc.t("shop.caravan.stay_button")
	leave_incomplete_popup.confirmed.connect(_on_leave_incomplete_confirmed)
	add_child(leave_incomplete_popup)

	# Create "Leave without purchasing" confirmation popup (completes event - allows progression)
	leave_complete_popup = ConfirmationDialog.new()
	leave_complete_popup.dialog_text = Loc.t("shop.caravan.leave_complete_confirmation")
	leave_complete_popup.ok_button_text = Loc.t("shop.caravan.leave_complete_button")
	leave_complete_popup.cancel_button_text = Loc.t("shop.caravan.stay_button")
	leave_complete_popup.confirmed.connect(_on_leave_complete_confirmed)
	add_child(leave_complete_popup)

## Set the shop ID and reload offerings (called by EventSequencer for caravans)
func set_shop_id(new_shop_id: String) -> void:
	shop_id = new_shop_id
	_load_offerings()
	_update_gold_display()
	_clear_detail_panel()

func _load_offerings() -> void:
	# Clear existing offering cards
	for child: Node in offering_list.get_children():
		child.queue_free()

	# Load offerings from ShopService
	current_offerings = ShopApi.get_shop_offerings(shop_id)

	# Create offering cards
	for offering: Dictionary in current_offerings:
		var offering_card: OfferingCard = OFFERING_CARD_SCENE.instantiate()
		offering_list.add_child(offering_card)
		offering_card.set_offering(offering)
		offering_card.card_clicked.connect(_on_offering_card_clicked.bind(offering))
	_update_offering_grid_columns()

func _update_gold_display() -> void:
	var resources: Dictionary = ProfileRepoApi.get_resources_dict()
	var gold: int = resources.get("gold", 0)
	gold_label.text = Loc.t("ui.shop.gold_label", {"amount": gold})

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _clear_detail_panel() -> void:
	selected_offering = {}
	offering_name_label.text = Loc.t("ui.shop.select_offering")
	price_label.text = Loc.t("ui.shop.price_placeholder")
	description_label.text = Loc.t("ui.shop.description_placeholder")
	purchase_button.disabled = true

func _update_detail_panel(offering: Dictionary) -> void:
	selected_offering = offering
	offering_name_label.text = offering.get("display_name", "")

	var price: int = offering.get("base_price", 0)
	price_label.text = Loc.t("ui.shop.price_format", {"price": price})

	description_label.text = offering.get("description", "")

	# Enable/disable purchase button based on affordability
	var can_result: Dictionary = ShopApi.can_purchase_offering(offering.get("offering_id", ""), shop_id)
	purchase_button.disabled = not can_result.get("can_purchase", false)

## =============================================================================
## PURCHASE FLOW
## =============================================================================

func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_offering.is_empty():
		return

	# Attempt purchase
	ShopApi.purchase_offering(selected_offering.get("offering_id", ""), shop_id)

	# ShopService will emit PurchaseCompleted or PurchaseFailed signals
	# which are handled by _on_purchase_completed and _on_purchase_failed

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_offering_card_clicked(offering: Dictionary) -> void:
	_update_detail_panel(offering)
	_sync_offering_selection()

func _on_purchase_completed(offering_id: String, _shop_id: String) -> void:
	# Track that player made a purchase
	has_purchased = true

	# Show success popup
	purchase_popup.dialog_text = Loc.t("shop.purchased")
	purchase_popup.popup_centered()

	# Refresh detail panel (purchase count may have changed)
	if not selected_offering.is_empty() and selected_offering.get("offering_id", "") == offering_id:
		_update_detail_panel(selected_offering)

	# Gold display will update via resources_updated signal

func _on_purchase_failed(offering_id: String, reason: String) -> void:
	# Show error popup
	error_popup.dialog_text = reason
	error_popup.popup_centered()

func _on_data_changed() -> void:
	_update_gold_display()

	# Update detail panel if an offering is selected (affordability may have changed)
	if not selected_offering.is_empty():
		_update_detail_panel(selected_offering)

## Handle caravan sequence completion (dialogue finished)
func _on_caravan_sequence_complete(_sequence: Resource) -> void:
	caravan_sequence_complete = true

	# Show both leave buttons now that dialogue is complete
	if leave_incomplete_button:
		leave_incomplete_button.visible = true
	if leave_complete_button:
		leave_complete_button.visible = true

## Handle "Leave" button (exit without completing - can return later)
func _on_leave_incomplete_pressed() -> void:
	if leave_incomplete_popup:
		leave_incomplete_popup.popup_centered()

## Handle "Leave without purchasing" button (completes event - allows progression)
func _on_leave_complete_pressed() -> void:
	if leave_complete_popup:
		# Update popup text based on whether player made a purchase
		if has_purchased:
			leave_complete_popup.dialog_text = Loc.t("shop.caravan.leave_complete_confirmation_purchased")
		else:
			leave_complete_popup.dialog_text = Loc.t("shop.caravan.leave_complete_confirmation")
		leave_complete_popup.popup_centered()

## Handle leave incomplete confirmation (user wants to leave but can return)
func _on_leave_incomplete_confirmed() -> void:
	_leave_shop(false)  # Don't complete the event

## Handle leave complete confirmation (user wants to skip and move on)
func _on_leave_complete_confirmed() -> void:
	_leave_shop(true)  # Complete the event

func _on_close_pressed() -> void:
	# This should only be called for non-caravan shops
	if is_caravan_event:
		push_warning("ShopScreen: Close button pressed for caravan event (should be hidden)")
		return

	_leave_shop()

## Leave the shop and return to previous scene
## If complete_event is true, marks the caravan event as complete (allows progression)
## If complete_event is false, leaves the event incomplete (can return later, blocks progression)
func _leave_shop(complete_event: bool = true) -> void:
	if is_caravan_event:
		var event_id: String = EventContext.get_current_event_id()
		if not event_id.is_empty():
			if complete_event:
				EventContext.complete_event()
			EventContext.clear_event()

	# Check if we have a return destination from NavigationContext
	if NavigationContext.has_return():
		var return_to: String = NavigationContext.pop_return()
		SceneManager.transition_to(return_to)
	else:
		# Default: return to campaign map (main hub)
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_update_offering_grid_columns()

func _update_offering_grid_columns() -> void:
	if not is_instance_valid(offering_list):
		return
	var available_width: float = maxf(420.0, size.x - DETAIL_PANEL_WIDTH - 180.0)
	var column_width: float = OFFERING_CARD_WIDTH + OFFERING_GRID_GAP
	offering_list.columns = maxi(2, int(floor((available_width + OFFERING_GRID_GAP) / column_width)))

func _sync_offering_selection() -> void:
	var selected_id: String = selected_offering.get("offering_id", "")
	for child: Node in offering_list.get_children():
		if child is OfferingCard:
			var card: OfferingCard = child as OfferingCard
			card.set_selected(card.offering.get("offering_id", "") == selected_id)
