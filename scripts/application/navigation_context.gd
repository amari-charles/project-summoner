extends Node

## NavigationContext - Manages navigation stack for proper scene returns
##
## This autoload tracks where screens should return to, enabling
## proper navigation flow for nested screens (e.g., Event -> Shop -> Event)
##
## Architecture:
## - Screens push their return destination before navigating elsewhere
## - Screens check for return destination when going back
## - Prevents hardcoded navigation paths
##
## Usage:
##   # Before navigating to shop from event
##   NavigationContext.push_return(SceneManager.SCENE_QUEST_JOURNAL)
##   SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)
##
##   # In shop back button
##   var return_to: String = NavigationContext.pop_return()
##   SceneManager.transition_to(return_to)

## Navigation stack
var _return_stack: Array[String] = []
var _context_values: Dictionary = {}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("NavigationContext: Initializing...")
	print("NavigationContext: Ready")

## =============================================================================
## NAVIGATION STACK
## =============================================================================

## Push a return destination onto the stack
func push_return(scene_path: String) -> void:
	_return_stack.append(scene_path)
	print("NavigationContext: Pushed return destination: %s (stack size: %d)" % [scene_path, _return_stack.size()])

## Pop and return the last return destination
func pop_return() -> String:
	if _return_stack.is_empty():
		push_warning("NavigationContext: pop_return() called with empty stack")
		return ""

	var destination: String = _return_stack.pop_back()
	print("NavigationContext: Popped return destination: %s (stack size: %d)" % [destination, _return_stack.size()])
	return destination

## Peek at the last return destination without removing it
func peek_return() -> String:
	if _return_stack.is_empty():
		return ""
	return _return_stack[-1]

## Check if there is a return destination
func has_return() -> bool:
	return not _return_stack.is_empty()

## Clear the navigation stack
func clear() -> void:
	var old_size: int = _return_stack.size()
	_return_stack.clear()
	_context_values.clear()
	print("NavigationContext: Cleared navigation stack (was size %d)" % old_size)

## Get stack size (for debugging)
func get_stack_size() -> int:
	return _return_stack.size()


## =============================================================================
## CONTEXT VALUES
## =============================================================================

## Store a lightweight context value for the next screen.
func set_value(key: String, value: Variant) -> void:
	if key.is_empty():
		return
	_context_values[key] = value


## Get a context value without removing it.
func get_value(key: String, default_value: Variant = null) -> Variant:
	if key.is_empty():
		return default_value
	if not _context_values.has(key):
		return default_value
	return _context_values[key]


## Get a context value and remove it from the store.
func consume_value(key: String, default_value: Variant = null) -> Variant:
	var value: Variant = get_value(key, default_value)
	clear_value(key)
	return value


## Remove a specific context value.
func clear_value(key: String) -> void:
	if key.is_empty():
		return
	_context_values.erase(key)
