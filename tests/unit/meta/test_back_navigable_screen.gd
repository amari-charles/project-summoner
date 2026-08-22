extends GutTest

const BACK_SCREEN_SCRIPT: Script = preload("res://scripts/meta/screens/back_navigable_screen.gd")
const SCREEN_SCRIPTS_WITH_BACK_CONTROLS: PackedStringArray = [
	"res://scripts/meta/screens/academy_activity_preparation.gd",
	"res://scripts/meta/screens/card_trait_tree_screen.gd",
	"res://scripts/meta/screens/collection_screen.gd",
	"res://scripts/meta/screens/multiplayer_lobby.gd",
	"res://scripts/meta/screens/online_screen.gd",
	"res://scripts/meta/screens/premium_store_screen.gd",
	"res://scripts/meta/screens/quest_journal.gd",
	"res://scripts/meta/screens/settings_screen.gd",
	"res://scripts/meta/screens/shop_screen.gd",
	"res://scripts/meta/screens/snapshot_manager.gd",
	"res://scripts/meta/screens/special_events_screen.gd",
	"res://scripts/meta/screens/summoner_screen.gd",
	"res://scripts/meta/screens/summoner_switch_screen.gd",
	"res://scripts/meta/screens/trait_tree_screen.gd",
]


func test_every_screen_with_a_back_control_uses_shared_escape_behavior() -> void:
	for script_path: String in SCREEN_SCRIPTS_WITH_BACK_CONTROLS:
		var screen_script: Script = load(script_path) as Script
		assert_not_null(screen_script, "Expected screen script at %s" % script_path)
		assert_eq(
			screen_script.get_base_script(),
			BACK_SCREEN_SCRIPT,
			"Expected %s to inherit shared Escape-to-back behavior" % script_path
		)
