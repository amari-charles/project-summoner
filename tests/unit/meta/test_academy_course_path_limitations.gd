extends GutTest

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_academy_course_path_limitations")
	ProfileRepo.ResetProfile()
	ProfileRepo.UnlockSummoner("summoner_cole")
	SummonerSelectionApi.set_active_summoner("summoner_cole", null)
	NavigationContext.clear()
	BattleContext.clear()
	await get_tree().process_frame


func after_all() -> void:
	NavigationContext.clear()
	BattleContext.clear()
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_invalid_restricted_activity_start_stays_on_course_path_and_renders_reasons() -> void:
	assert_true(CampaignApi.enroll_academy_course("practical_spellcraft"), "Expected test profile to enroll in Practical Spellcraft")
	assert_true(
		CampaignApi.complete_academy_activity("practical_spellcraft", "practical_spellcraft_lesson", true),
		"Expected lesson completion to unlock the practice activity"
	)

	var screen: AcademyCoursePath = _build_screen("practical_spellcraft")
	var harness := TransitionHarness.new()
	screen._scene_transition_override = Callable(harness, "transition_to")

	screen._start_activity({
		"id": "practical_spellcraft_practice",
		"type": "PracticeBattle"
	})

	assert_true(screen.activity_modal.visible, "Invalid deck should reopen the activity modal")
	assert_eq(harness.last_transition_scene, "", "Invalid deck should not transition to battle")
	assert_true(screen.modal_body_label.text.contains(Loc.t("academy.course_path.deck_status")), "Deck status should render")
	assert_true(screen.modal_body_label.text.contains("required card"), "Service-owned invalid reason should render")

	screen.free()


func test_edit_deck_routes_to_collection_and_returns_to_course_path() -> void:
	var screen: AcademyCoursePath = _build_screen("summoning_basics")
	var harness := TransitionHarness.new()
	screen._scene_transition_override = Callable(harness, "transition_to")

	screen._on_edit_deck_pressed()

	assert_eq(BattleContext.academy_course_id, "summoning_basics")
	assert_eq(NavigationContext.peek_return(), SceneManager.SCENE_ACADEMY_COURSE_PATH)
	assert_eq(harness.last_transition_scene, SceneManager.SCENE_COLLECTION_SCREEN)

	screen.free()


func _build_screen(course_id: String) -> AcademyCoursePath:
	var screen: AcademyCoursePath = AcademyCoursePath.new()
	screen._course_id = course_id
	screen._course = {
		"id": course_id,
		"name_key": "academy.course.%s.name" % course_id,
		"description_key": "academy.course.%s.description" % course_id
	}
	screen.activity_modal = Control.new()
	screen.modal_body_label = Label.new()
	screen.add_child(screen.activity_modal)
	screen.add_child(screen.modal_body_label)
	return screen


class TransitionHarness:
	extends RefCounted

	var last_transition_scene: String = ""

	func transition_to(scene_path: String) -> void:
		last_transition_scene = scene_path
