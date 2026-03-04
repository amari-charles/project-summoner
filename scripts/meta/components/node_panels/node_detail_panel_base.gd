class_name NodeDetailPanelBase
extends Control

## NodeDetailPanelBase - Base class for type-specific event detail panels
##
## Each event type (battle, caravan, choice, etc.) should extend this base class
## and implement its own UI and behavior. This replaces flag-based conditionals
## with proper polymorphism.
##
## Event data is accessed via the typed `event` property which provides
## type-safe accessors for all event properties. The underlying data comes
## from C# EventCatalog via TypedEventData wrapper.

## Emitted when user clicks the start/continue button
signal start_requested()

## Emitted when user clicks outside or presses escape
signal close_requested()

## Typed event data wrapper providing type-safe property access.
## Use this instead of accessing event_data directly.
var event: TypedEventData = TypedEventData.new()

## The raw event data dictionary (legacy, prefer using `event` property)
var event_data: Dictionary:
	get: return event.get_raw()

## The event ID being displayed
var event_id: String:
	get: return event.id
	set(value): event = TypedEventData.new(event.get_raw(), value)

## =============================================================================
## ABSTRACT INTERFACE - Subclasses must implement these
## =============================================================================

## Configure the panel with event data. Called when a node is selected.
## Subclasses should populate their UI elements here.
## @param event_dict: Event data dictionary from EventCatalog.ToDictionary()
## @param id: Event ID from EventCatalog
func configure(event_dict: Dictionary, id: String) -> void:
	event = TypedEventData.new(event_dict, id)
	_configure_impl()


## Internal configuration implementation - override in subclasses
func _configure_impl() -> void:
	push_error("NodeDetailPanelBase._configure_impl() not implemented in subclass")


## =============================================================================
## COMMON INTERFACE - Default implementations that subclasses can override
## =============================================================================

## Check if the panel's current state allows starting the event.
## Override in subclasses that need validation (e.g., deck selection).
func can_start() -> bool:
	return true


## Get the text for the start button based on completion state.
## Subclasses can override for custom button text.
func get_start_button_text() -> String:
	var is_completed: bool = SafeTypeUtils.bool_val(Campaign.IsBattleCompleted(event.id))

	if is_completed:
		# Combat events are always replayable (for XP grinding)
		if event.repeatable or event.is_combat():
			return Loc.t("campaign.map.button_replay")
		else:
			return Loc.t("campaign.map.button_completed")
	else:
		return Loc.t("campaign.map.button_start_event")


## Check if the start button should be disabled.
## Subclasses can override for custom logic.
func is_start_disabled() -> bool:
	var is_completed: bool = SafeTypeUtils.bool_val(Campaign.IsBattleCompleted(event.id))

	# Completed non-repeatable events can't be started
	# Exception: Combat events are always replayable (for XP grinding, no gold/card rewards)
	if is_completed and not event.repeatable and not event.is_combat():
		return true

	# Also check subclass validation
	return not can_start()


## Get the event type for this panel.
## Subclasses should override to return their specific type.
func get_event_type() -> StringName:
	return EventTypeIDs.BATTLE


## =============================================================================
## COMMON HELPERS - Shared utilities for all panel types
## =============================================================================

## Get display name for a card from CardCatalog, with fallback
func _get_card_display_name(catalog: Node, catalog_id: String) -> String:
	if catalog and catalog.has_method("GetCardAsDict"):
		var card_data: Dictionary = SafeTypeUtils.dict(catalog.call("GetCardAsDict", catalog_id))
		if not card_data.is_empty():
			return SafeTypeUtils.string(card_data.get("card_name", catalog_id), catalog_id)
	# Fallback: convert catalog_id to title case (fire_wisp → Fire Wisp)
	return catalog_id.replace("_", " ").capitalize()
