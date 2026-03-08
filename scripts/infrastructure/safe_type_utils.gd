class_name SafeTypeUtils
extends RefCounted

## SafeTypeUtils - Static utility class for safe type coercion
##
## Provides type-safe extraction from Variant values, commonly used when
## working with Dictionary data from C# interop or JSON parsing.
##
## Usage:
##   var name: String = SafeTypeUtils.string(dict.get("name"), "Unknown")
##   var count: int = SafeTypeUtils.int_val(dict.get("count"), 0)
##   var items: Array = SafeTypeUtils.array(dict.get("items"))


## Extract a String from a Variant, returning default if not a String
static func string(variant: Variant, default: String = "") -> String:
	return variant if variant is String else default


## Extract an int from a Variant, returning default if not an int
static func int_val(variant: Variant, default: int = 0) -> int:
	return variant if variant is int else default


## Extract a float from a Variant, returning default if not a float
## Also converts int to float if needed
static func float_val(variant: Variant, default: float = 0.0) -> float:
	if variant is float:
		var float_value: float = variant
		return float_value
	if variant is int:
		var int_value: int = variant
		return float(int_value)
	return default


## Extract a bool from a Variant, returning default if not a bool
static func bool_val(variant: Variant, default: bool = false) -> bool:
	return variant if variant is bool else default


## Extract an Array from a Variant, returning empty array if not an Array
static func array(variant: Variant) -> Array:
	return variant if variant is Array else []


## Extract a Dictionary from a Variant, returning empty dict if not a Dictionary
static func dict(variant: Variant) -> Dictionary:
	return variant if variant is Dictionary else {}


## Extract a Vector2 from a Variant, returning default if not a Vector2
static func vector2(variant: Variant, default: Vector2 = Vector2.ZERO) -> Vector2:
	return variant if variant is Vector2 else default


## Extract a Vector3 from a Variant, returning default if not a Vector3
static func vector3(variant: Variant, default: Vector3 = Vector3.ZERO) -> Vector3:
	return variant if variant is Vector3 else default
