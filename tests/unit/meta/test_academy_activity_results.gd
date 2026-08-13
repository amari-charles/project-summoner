extends GutTest

func test_academy_choice_tracks_only_authored_claim_and_option_ids() -> void:
	var screen: AcademyActivityResults = AcademyActivityResults.new()
	screen.choices = VBoxContainer.new()
	screen.continue_button = Button.new()
	screen.add_child(screen.choices)
	screen.add_child(screen.continue_button)
	screen._render_pending([
		{
			"claim_id": "claim-1",
			"status": "pending",
			"options": [
				{"option_id": "option-a", "grants": [{"id": "fire_wisp", "amount": 1}]},
				{"option_id": "option-b", "grants": [{"id": "puff", "amount": 1}]},
			],
		}
	])

	assert_eq(screen._pending_claim_id, "claim-1")
	assert_eq(screen.choices.get_child_count(), 2)
	assert_true(screen.continue_button.disabled)
	var selected: Button = screen.choices.get_child(1) as Button
	screen._select_option("option-b", selected)
	assert_eq(screen._selected_option_id, "option-b")
	assert_false(screen.continue_button.disabled)
	screen.free()
