extends BackNavigableScreen
class_name SummonerSwitchScreen

## SummonerSwitchScreen - Select an unlocked summoner from a readable roster.

const SummonerRosterItemScene: PackedScene = preload(
	"res://scenes/meta/components/summoner_roster_item.tscn"
)

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var summoner_list: VBoxContainer = %SummonerList
@onready var confirm_button: Button = %ConfirmButton

var _roster_items: Array[SummonerRosterItem] = []
var _selected_summoner_id: String = ""


func _ready() -> void:
	background.color = GameColorPalette.UI_BACKGROUND
	close_button.pressed.connect(_on_close_pressed)
	confirm_button.pressed.connect(_on_confirm_pressed)
	title_label.text = Loc.t("ui.summoner_switch.title")
	confirm_button.text = Loc.t("ui.summoner_switch.confirm")
	confirm_button.disabled = true
	_load_summoner_roster()


func _load_summoner_roster() -> void:
	for child: Node in summoner_list.get_children():
		child.queue_free()
	_roster_items.clear()
	_selected_summoner_id = ""

	var active_summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	var unlocked_ids: Array = SummonerSelectionApi.get_unlocked_summoner_ids_array()

	for summoner_id_value: Variant in unlocked_ids:
		var summoner_id: String = SafeTypeUtils.string(summoner_id_value, "")
		if summoner_id.is_empty():
			continue

		var roster_item: SummonerRosterItem = SummonerRosterItemScene.instantiate()
		summoner_list.add_child(roster_item)
		roster_item.set_summoner_data(summoner_id)
		roster_item.set_active(summoner_id == active_summoner_id)
		roster_item.select_pressed.connect(_on_summoner_selected)
		_roster_items.append(roster_item)


func _on_summoner_selected(summoner_id: String) -> void:
	if summoner_id.is_empty():
		return

	_selected_summoner_id = summoner_id
	for roster_item: SummonerRosterItem in _roster_items:
		roster_item.set_pending(roster_item.get_summoner_id() == _selected_summoner_id)
	confirm_button.disabled = false


func _on_confirm_pressed() -> void:
	if _selected_summoner_id.is_empty():
		return

	SummonerSelectionApi.set_active_summoner(_selected_summoner_id, null)
	_close()


func _on_close_pressed() -> void:
	_close()


func _close() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_SUMMONER_SCREEN
	SceneManager.transition_to(return_scene)


func _request_back_navigation() -> void:
	_on_close_pressed()
