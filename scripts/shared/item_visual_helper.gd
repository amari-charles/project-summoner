class_name ItemVisualHelper
extends RefCounted

## Shared fallback presentation for items whose authored icon has not landed yet.

const DEFAULT_ITEM_ICON: Texture2D = preload(
	"res://assets/icons/1bit_pixel_icons/Sprites_Cropped/Travel_Backpack_Bag_Pouch_Small.png"
)


static func get_icon(item: Dictionary) -> Texture2D:
	var icon_path: String = SafeTypeUtils.string(item.get("icon_path", ""), "")
	if not icon_path.is_empty() and ResourceLoader.exists(icon_path):
		var authored_icon: Texture2D = load(icon_path) as Texture2D
		if authored_icon != null:
			return authored_icon
	return DEFAULT_ITEM_ICON
