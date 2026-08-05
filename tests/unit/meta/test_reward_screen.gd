extends GutTest


func test_urs_c21_academy_choice_renders_and_submits_only_claim_and_option_ids() -> void:
	var screen := AcademyCoursePath.new()
	screen._build_reward_modal()
	var harness := ClaimHarness.new()
	screen._claim_reward_override = Callable(harness, "claim")
	screen._course = {"reward_previews": []}

	screen._show_pending_reward_offer(
		{
			"claim_id": "claim-1",
			"status": "pending",
			"choose_count": 1,
			"options": [
				{
					"option_id": "option-a",
					"label_key": "academy.course_path.reward_fallback",
					"grants": [],
				},
				{
					"option_id": "option-b",
					"label_key": "academy.course_path.reward_fallback",
					"grants": [],
				},
			],
		}
	)

	assert_eq(screen._reward_option_buttons.size(), 2)
	var selected_button: Button = screen._reward_option_buttons["option-b"]
	selected_button.button_pressed = true
	screen._on_reward_option_toggled("option-b")
	assert_false(screen._reward_continue_button.disabled)

	screen._on_reward_continue_pressed()

	assert_eq(harness.claim_id, "claim-1")
	assert_eq(harness.option_ids, ["option-b"])
	screen.free()


class ClaimHarness:
	extends RefCounted

	var claim_id: String = ""
	var option_ids: Array[String] = []

	func claim(received_claim_id: String, received_option_ids: Array[String]) -> Dictionary:
		claim_id = received_claim_id
		option_ids = received_option_ids.duplicate()
		return {"success": false}
