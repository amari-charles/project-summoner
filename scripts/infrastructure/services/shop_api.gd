class_name ShopApi
extends RefCounted

static func purchase_offering(offering_id: String, shop_id: String) -> bool:
	return SafeTypeUtils.bool_val(Shop.call("PurchaseOffering", offering_id, shop_id), false)

static func is_offering_owned(offering_id: String, shop_id: String) -> bool:
	return SafeTypeUtils.bool_val(Shop.call("IsOfferingOwned", offering_id, shop_id), false)

static func get_shop_offerings(shop_id: String) -> Array:
	return SafeTypeUtils.array(Shop.call("GetShopOfferings", shop_id))

static func can_purchase_offering(offering_id: String, shop_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Shop.call("CanPurchaseOffering", offering_id, shop_id))
