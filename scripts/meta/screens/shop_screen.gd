extends BackNavigableScreen
class_name ShopScreen

## Campus Shop UI for the general shop catalog.
##
## Offerings are displayed six at a time. Selecting one opens the purchase modal.

const OFFERING_CARD_SCENE: PackedScene = preload("res://scenes/meta/components/offering_card.tscn")
const OFFERINGS_PER_PAGE: int = 6
const SHOP_ID: String = "general"

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var gold_label: Label = %GoldLabel
@onready var offering_list: GridContainer = %OfferingList
@onready var previous_page_button: Button = %PreviousPageButton
@onready var next_page_button: Button = %NextPageButton
@onready var page_label: Label = %PageLabel
@onready var detail_modal: Control = %DetailModal
@onready var modal_close_button: Button = %ModalCloseButton
@onready var offering_name_label: Label = %OfferingNameLabel
@onready var price_label: Label = %PriceLabel
@onready var description_label: Label = %DescriptionLabel
@onready var purchase_button: Button = %PurchaseButton
@onready var shopkeeper_placeholder_label: Label = %ShopkeeperPlaceholderLabel
@onready var item_art_placeholder_label: Label = %ItemArtPlaceholderLabel
@onready var purchase_popup: AcceptDialog = %PurchasePopup
@onready var error_popup: AcceptDialog = %ErrorPopup

var current_offerings: Array = []
var selected_offering: Dictionary = {}
var current_page: int = 0


func _ready() -> void:
	_apply_localized_copy()
	back_button.pressed.connect(_on_back_pressed)
	previous_page_button.pressed.connect(_on_previous_page_pressed)
	next_page_button.pressed.connect(_on_next_page_pressed)
	modal_close_button.pressed.connect(_close_detail_modal)
	purchase_button.pressed.connect(_on_purchase_pressed)

	Shop.connect("PurchaseCompleted", _on_purchase_completed)
	Shop.connect("PurchaseFailed", _on_purchase_failed)
	ProfileRepo.connect("DataChangedGodot", _on_data_changed)

	_update_gold_display()
	_load_offerings()
	_close_detail_modal()


func _apply_localized_copy() -> void:
	title_label.text = Loc.t("academy.campus.shop.name")
	shopkeeper_placeholder_label.text = Loc.t("shop.campus.shopkeeper_art_placeholder")
	item_art_placeholder_label.text = Loc.t("shop.campus.item_art_placeholder")
	purchase_button.text = Loc.t("shop.button.purchase")
	purchase_popup.title = Loc.t("shop.campus.purchase_success_title")
	error_popup.title = Loc.t("shop.campus.purchase_failed_title")


func _exit_tree() -> void:
	if Shop.is_connected("PurchaseCompleted", _on_purchase_completed):
		Shop.disconnect("PurchaseCompleted", _on_purchase_completed)
	if Shop.is_connected("PurchaseFailed", _on_purchase_failed):
		Shop.disconnect("PurchaseFailed", _on_purchase_failed)
	if ProfileRepo.is_connected("DataChangedGodot", _on_data_changed):
		ProfileRepo.disconnect("DataChangedGodot", _on_data_changed)


func _load_offerings() -> void:
	current_offerings = ShopApi.get_shop_offerings(SHOP_ID)
	current_page = mini(current_page, _last_page_index())
	_render_current_page()


func _render_current_page() -> void:
	for child: Node in offering_list.get_children():
		offering_list.remove_child(child)
		child.queue_free()

	var first_index: int = current_page * OFFERINGS_PER_PAGE
	var end_index: int = mini(first_index + OFFERINGS_PER_PAGE, current_offerings.size())
	for index: int in range(first_index, end_index):
		var offering: Dictionary = current_offerings[index]
		var offering_card: OfferingCard = OFFERING_CARD_SCENE.instantiate()
		offering_list.add_child(offering_card)
		offering_card.set_offering(offering)
		offering_card.card_clicked.connect(_on_offering_card_clicked.bind(offering))

	_update_page_controls()


func _update_page_controls() -> void:
	var page_count: int = maxi(1, int(ceil(float(current_offerings.size()) / OFFERINGS_PER_PAGE)))
	previous_page_button.disabled = current_page == 0
	next_page_button.disabled = current_page >= page_count - 1
	page_label.text = Loc.t("shop.campus.page", {"current": current_page + 1, "total": page_count})


func _last_page_index() -> int:
	if current_offerings.is_empty():
		return 0
	return int((current_offerings.size() - 1) / OFFERINGS_PER_PAGE)


func _update_gold_display() -> void:
	var resources: Dictionary = ProfileRepoApi.get_resources_dict()
	gold_label.text = Loc.t("ui.shop.gold_label", {"amount": resources.get("gold", 0)})


func _open_detail_modal(offering: Dictionary) -> void:
	selected_offering = offering
	offering_name_label.text = offering.get("display_name", "")
	price_label.text = Loc.t("ui.shop.price_format", {"price": offering.get("base_price", 0)})
	description_label.text = offering.get("description", "")
	_update_purchase_availability()
	detail_modal.visible = true
	modal_close_button.grab_focus()


func _close_detail_modal() -> void:
	selected_offering = {}
	detail_modal.visible = false
	purchase_button.disabled = true


func _update_purchase_availability() -> void:
	if selected_offering.is_empty():
		purchase_button.disabled = true
		return
	var offering_id: String = selected_offering.get("offering_id", "")
	var can_result: Dictionary = ShopApi.can_purchase_offering(offering_id, SHOP_ID)
	purchase_button.disabled = not can_result.get("can_purchase", false)


func _on_offering_card_clicked(offering: Dictionary) -> void:
	_open_detail_modal(offering)


func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_offering.is_empty():
		return
	ShopApi.purchase_offering(selected_offering.get("offering_id", ""), SHOP_ID)


func _on_purchase_completed(offering_id: String, completed_shop_id: String) -> void:
	if completed_shop_id != SHOP_ID:
		return
	purchase_popup.dialog_text = Loc.t("shop.purchased")
	purchase_popup.popup_centered()
	if selected_offering.get("offering_id", "") == offering_id:
		_update_purchase_availability()


func _on_purchase_failed(_offering_id: String, reason: String) -> void:
	error_popup.dialog_text = reason
	error_popup.popup_centered()


func _on_data_changed() -> void:
	_update_gold_display()
	_update_purchase_availability()


func _on_previous_page_pressed() -> void:
	if current_page <= 0:
		return
	current_page -= 1
	_close_detail_modal()
	_render_current_page()


func _on_next_page_pressed() -> void:
	if current_page >= _last_page_index():
		return
	current_page += 1
	_close_detail_modal()
	_render_current_page()


func _unhandled_input(event: InputEvent) -> void:
	if detail_modal.visible and event.is_action_pressed("ui_cancel"):
		_close_detail_modal()
		get_viewport().set_input_as_handled()


func _on_back_pressed() -> void:
	if NavigationContext.has_return():
		SceneManager.transition_to(NavigationContext.pop_return())
	else:
		SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CAMPUS)


func _request_back_navigation() -> void:
	if detail_modal.visible:
		_close_detail_modal()
		return
	_on_back_pressed()
