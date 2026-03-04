class_name NodePanelFactory

## NodePanelFactory - Creates type-specific node detail panels
##
## Maps event types to their corresponding panel scenes and instantiates them.
## This replaces flag-based conditionals in campaign_map.gd with proper type dispatch.

## Scene mappings for each event type
const PANEL_SCENES: Dictionary = {
	EventTypeIDs.BATTLE: preload("res://scenes/meta/components/node_panels/battle_node_panel.tscn"),
	EventTypeIDs.CARAVAN: preload("res://scenes/meta/components/node_panels/caravan_node_panel.tscn"),
	EventTypeIDs.CHOICE: preload("res://scenes/meta/components/node_panels/choice_node_panel.tscn"),
	EventTypeIDs.AFFINITY: preload("res://scenes/meta/components/node_panels/onboarding_node_panel.tscn"),
	EventTypeIDs.FIRST_SUMMON: preload("res://scenes/meta/components/node_panels/onboarding_node_panel.tscn"),
	EventTypeIDs.ONBOARDING: preload("res://scenes/meta/components/node_panels/onboarding_node_panel.tscn"),
}


## Create a panel for the given event type.
## Falls back to battle panel for unknown types.
static func create_panel(event_type: StringName) -> NodeDetailPanelBase:
	var scene: PackedScene = PANEL_SCENES.get(event_type, PANEL_SCENES[EventTypeIDs.BATTLE])
	return scene.instantiate() as NodeDetailPanelBase


## Check if we have a specific panel for this event type
static func has_panel_for_type(event_type: StringName) -> bool:
	return PANEL_SCENES.has(event_type)


## Get the event type from event data with fallback to BATTLE
static func get_event_type(event: Dictionary) -> StringName:
	var type_value: Variant = event.get("event_type", EventTypeIDs.BATTLE)
	if type_value is StringName:
		return type_value
	elif type_value is String:
		return StringName(type_value)
	else:
		return EventTypeIDs.BATTLE
