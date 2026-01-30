class_name NodeDetailPanelBase
extends Control

## NodeDetailPanelBase - Base class for type-specific event detail panels
##
## Each event type (battle, caravan, choice, etc.) should extend this base class
## and implement its own UI and behavior. This replaces flag-based conditionals
## with proper polymorphism.

## Emitted when user clicks the start/continue button
signal start_requested()

## Emitted when user clicks outside or presses escape
signal close_requested()

## The event data this panel is displaying
var event_data: Dictionary = {}

## The event ID being displayed
var event_id: String = ""

## =============================================================================
## ABSTRACT INTERFACE - Subclasses must implement these
## =============================================================================

## Configure the panel with event data. Called when a node is selected.
## Subclasses should populate their UI elements here.
func configure(event: Dictionary, id: String) -> void:
	event_data = event
	event_id = id
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
	var is_completed: bool = _safe_bool(Campaign.is_battle_completed(event_id))
	var is_repeatable: bool = _safe_bool(event_data.get("repeatable", true))

	if is_completed:
		if is_repeatable:
			return Loc.t("campaign.map.button_replay")
		else:
			return Loc.t("campaign.map.button_completed")
	else:
		return Loc.t("campaign.map.button_start_event")


## Check if the start button should be disabled.
## Subclasses can override for custom logic.
func is_start_disabled() -> bool:
	var is_completed: bool = _safe_bool(Campaign.is_battle_completed(event_id))
	var is_repeatable: bool = _safe_bool(event_data.get("repeatable", true))

	# Completed non-repeatable events can't be started
	if is_completed and not is_repeatable:
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
	# Fallback: convert catalog_id to title case (fire_wisp → Fire Wisp)
	return catalog_id.replace("_", " ").capitalize()
