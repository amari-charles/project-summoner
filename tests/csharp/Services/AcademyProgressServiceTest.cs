namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Campaign.Handlers;
using Fateforged.Meta.Deck;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class AcademyProgressServiceTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (var i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void FreshAcademyProgress_OffersRequiredIntroWithoutCommittingCapacity()
    {
        var repo = CreateRepo("academy_fresh_required_intro");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        service.GetAcademyProgress();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.RemainingEnrollments).IsEqual(3);
        AssertThat(progress.EnrolledCourses).NotContains(CourseIds.IntroductionToMagic101);
        AssertThat(
                progress.CourseActivityIndex.ContainsKey((string)CourseIds.IntroductionToMagic101)
            )
            .IsFalse();

        var intro = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        AssertThat(intro["is_enrolled"].AsBool()).IsFalse();
        AssertThat(intro["is_available"].AsBool()).IsTrue();
    }

    [TestCase]
    public void GetQuestJournalState_SeparatesActiveKnownAndCompletedAcademicChains()
    {
        var repo = CreateRepo("academy_quest_journal_projection");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        var journal = service.GetQuestJournalState();

        AssertThat(journal["current_year"].AsInt32()).IsEqual(1);
        AssertThat(journal["current_semester"].AsInt32()).IsEqual(1);
        AssertThat(journal["capacity_total"].AsInt32()).IsEqual(3);
        AssertThat(journal["capacity_committed"].AsInt32()).IsEqual(0);
        AssertThat(journal["capacity_completed"].AsInt32()).IsEqual(0);
        AssertThat(journal["capacity_remaining"].AsInt32()).IsEqual(3);

        var active = journal["active"].AsGodotArray();
        AssertThat(active).IsEmpty();
        var intro = journal["opportunities"].AsGodotArray()[0].AsGodotDictionary();
        AssertThat(intro["source_id"].AsString()).IsEqual((string)CourseIds.IntroductionToMagic101);
        AssertThat(intro["state"].AsString()).IsEqual("opportunity");
        AssertThat(intro["professor_id"].AsString()).IsEqual("general_magic");
        AssertThat(intro["professor_name_key"].AsString()).IsNotEmpty();
        AssertThat(intro["location_key"].AsString()).IsNotEmpty();
        AssertThat(intro["offer_dialogue_keys"].AsGodotArray()).IsNotEmpty();
        AssertThat(intro["accepted_dialogue_keys"].AsGodotArray()).IsNotEmpty();
        AssertThat(intro["reward_previews"].AsGodotArray()).IsNotEmpty();

        var opportunityIds = journal["opportunities"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["source_id"].AsString())
            .ToArray();
        AssertThat(opportunityIds).Contains((string)CourseIds.IntroductionToMagic101);
        AssertThat(opportunityIds).NotContains((string)CourseIds.SummoningBasics);
        AssertThat(opportunityIds).NotContains((string)CourseIds.PracticalSpellcraft);
        AssertThat(journal["completed"].AsGodotArray()).IsEmpty();
    }

    [TestCase]
    public void GetQuestJournalState_ReflectsPermanentCapacityCommitmentAtEnrollment()
    {
        var repo = CreateRepo("academy_quest_journal_capacity_commitment");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        service.GetAcademyProgress();

        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();

        var journal = service.GetQuestJournalState();
        AssertThat(journal["capacity_total"].AsInt32()).IsEqual(3);
        AssertThat(journal["capacity_committed"].AsInt32()).IsEqual(1);
        AssertThat(journal["capacity_remaining"].AsInt32()).IsEqual(2);
        AssertThat(journal["tracked_quest_id"].AsString())
            .IsEqual("academy:introduction_to_magic_101");

        var activeIds = journal["active"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["source_id"].AsString())
            .ToArray();
        AssertThat(activeIds).Contains((string)CourseIds.IntroductionToMagic101);
        AssertThat(activeIds).NotContains((string)CourseIds.SummoningBasics);

        var opportunityIds = journal["opportunities"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["source_id"].AsString())
            .ToArray();
        AssertThat(opportunityIds).NotContains((string)CourseIds.PracticalSpellcraft);
    }

    [TestCase]
    public void ProfessorQuestStates_ExposeOnlyTheKnownAvailableOfferMarker()
    {
        var repo = CreateRepo("academy_professor_offer_marker");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        var professors = service.GetProfessorQuestStates();
        AssertThat(professors).HasSize(5);

        var general = professors
            .Select(item => item)
            .First(item => item["id"].AsString() == "general_magic");
        AssertThat(general["quest_marker"].AsString()).IsEqual("!");
        AssertThat(general["opportunities"].AsGodotArray()).HasSize(1);

        var elementalMarkers = professors
            .Select(item => item)
            .Where(item => item["id"].AsString() != "general_magic")
            .Select(item => item["quest_marker"].AsString())
            .ToArray();
        AssertThat(elementalMarkers).ContainsExactly("", "", "", "");
    }

    [TestCase]
    public void FoundationChoice_UnlocksAfterIntroAndEitherPathUnlocksElements()
    {
        var repo = CreateRepo("academy_quest_dependency_chain");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        var foundationIds = service
            .GetQuestJournalState()["opportunities"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["source_id"].AsString())
            .ToArray();
        AssertThat(foundationIds).Contains((string)CourseIds.SummoningBasics);
        AssertThat(foundationIds).Contains((string)CourseIds.PracticalSpellcraft);

        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();

        var elementIds = service
            .GetQuestJournalState()["opportunities"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["source_id"].AsString())
            .ToArray();
        AssertThat(elementIds).Contains((string)CourseIds.IntroToFire);
        AssertThat(elementIds).Contains((string)CourseIds.IntroToWater);
        AssertThat(elementIds).Contains((string)CourseIds.IntroToEarth);
        AssertThat(elementIds).Contains((string)CourseIds.IntroToAir);
        AssertThat(elementIds).NotContains((string)CourseIds.PracticalSpellcraft);
    }

    [TestCase]
    public void EnrollAcademyCourse_RejectsFutureSemesterCourse()
    {
        var repo = CreateRepo("academy_reject_future_course");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        service.GetAcademyProgress();

        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToEmpowerment))
            .IsFalse();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CurrentYear).IsEqual(1);
        AssertThat(progress.CurrentSemester).IsEqual(1);
        AssertThat(progress.EnrolledCourses).NotContains(CourseIds.IntroductionToEmpowerment);
        AssertThat(progress.RemainingEnrollments).IsEqual(3);
    }

    [TestCase]
    public void GetAcademyCourse_ExposesDisplayGroupMetadata()
    {
        var repo = CreateRepo("academy_course_display_groups");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        service.GetAcademyProgress();

        var required = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        var foundationChoice = service.GetAcademyCourse((string)CourseIds.SummoningBasics);
        var elementElective = service.GetAcademyCourse((string)CourseIds.IntroToFire);
        var trackCourse = service.GetAcademyCourse((string)CourseIds.IntroductionToEmpowerment);

        AssertThat(required["group_id"].AsString()).IsEqual("required");
        AssertThat(required["group_title_key"].AsString()).IsEqual("academy.hub.group_required");
        AssertThat(required["track_title_key"].AsString()).IsEqual("academy.track.foundation");

        AssertThat(foundationChoice["group_id"].AsString()).IsEqual("year_1_semester_1_foundation");
        AssertThat(foundationChoice["group_title_key"].AsString())
            .IsEqual("academy.class_hall.foundation_choice");

        AssertThat(elementElective["group_id"].AsString()).IsEqual("year_1_semester_1_element");
        AssertThat(elementElective["group_title_key"].AsString())
            .IsEqual("academy.class_hall.element_elective");

        AssertThat(trackCourse["group_id"].AsString()).IsEqual("track_foundation");
        AssertThat(trackCourse["group_title_key"].AsString())
            .IsEqual("academy.class_hall.track_foundation");
        AssertThat(trackCourse["group_sort_order"].AsInt32()).IsGreater(20);
    }

    [TestCase]
    public void GetAcademyCourse_ExposesActivityLimitationViewFields()
    {
        var repo = CreateRepo("academy_activity_limitations_stub_fields");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            CardIds.Charge
        );

        var course = service.GetAcademyCourse((string)CourseIds.PracticalSpellcraft);
        var activities = course["activities"].AsGodotArray();
        var practice = activities
            .Select(item => item.AsGodotDictionary())
            .First(activity => activity["id"].AsString() == "practical_spellcraft_practice");

        var loadout = practice["loadout"].AsGodotDictionary();
        var rules = loadout["rules"].AsGodotDictionary();
        AssertThat(rules["has_rules"].AsBool()).IsTrue();
        AssertThat(rules["min_summons"].AsInt32()).IsEqual(1);
        AssertThat(rules["min_spells"].AsInt32()).IsEqual(2);
        AssertThat(rules["max_deck_size"].AsInt32()).IsEqual(12);
        AssertThat(loadout["supplied_cards"].AsGodotArray()).HasSize(2);

        var deckValidation = practice["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsTrue();
        AssertThat(deckValidation["status"].AsString()).IsEqual("valid");
        AssertThat(deckValidation["issues"].AsGodotArray()).IsEmpty();
    }

    [TestCase]
    public void GetAcademyActivityLaunchState_ReturnsValidityAndDeckSummary()
    {
        var repo = CreateRepo("academy_activity_limitations_launch_state");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var deckId = SetActiveDeck(repo, "Spellcraft Deck", CardIds.Charge);
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            deckId
        );

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice"
        );

        AssertThat(state["id"].AsString()).IsEqual("practical_spellcraft_practice");
        AssertThat(
                state["loadout"]
                    .AsGodotDictionary()["rules"]
                    .AsGodotDictionary()["has_rules"]
                    .AsBool()
            )
            .IsTrue();
        AssertThat(state["deck_validation"].AsGodotDictionary()["is_valid"].AsBool()).IsTrue();
        AssertThat(state["selected_deck"].AsGodotDictionary()["id"].AsString()).IsEqual(deckId);
    }

    [TestCase]
    public void ResolveAcademyActivityBattleConfig_PreservesExistingLoanerConfigForUnrestrictedBattle()
    {
        var repo = CreateRepo("academy_activity_limitations_resolve_stub");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        var config = service.ResolveAcademyActivityBattleConfig(
            (string)CourseIds.IntroductionToMagic101,
            "magic_101_spell_practice"
        );

        AssertThat(config.ContainsKey("enemy_side")).IsTrue();
        AssertThat(config.ContainsKey("player_side")).IsTrue();
        var playerSide = config["player_side"].AsGodotDictionary();
        var deck = playerSide["deck"].AsGodotDictionary();
        AssertThat(deck["source"].AsString()).IsEqual("authored");
        AssertThat(deck["cards"].AsGodotArray()).HasSize(2);
    }

    [TestCase]
    public void AcademyActivityLimitations_InvalidDeckReportsSpecificReasonsAndBlocksStart()
    {
        var repo = CreateRepo("academy_activity_limitations_invalid_deck");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetActiveDeck(repo, "No Spell Deck", CardIds.NeutralStarterUnit);

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice"
        );

        var deckValidation = state["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsFalse();
        AssertThat(deckValidation["status"].AsString()).IsEqual("invalid");
        AssertThat(state["can_start"].AsBool()).IsFalse();

        var issueCodes = ValidationIssueCodes(deckValidation);
        AssertThat(issueCodes).Contains("min_spells");
        AssertThat(issueCodes).Contains("required_card_missing");

        AssertThat(
                service.ResolveAcademyActivityBattleConfig(
                    (string)CourseIds.PracticalSpellcraft,
                    "practical_spellcraft_practice"
                )
            )
            .IsEmpty();
    }

    [TestCase]
    public void AcademyActivityLimitations_AllowedTypesAndBannedCardsRejectSpellInSummonClass()
    {
        var repo = CreateRepo("academy_activity_limitations_summon_only");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetActiveDeck(repo, "Spell In Summon Class", CardIds.FireWisp, CardIds.MagicBolt);

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.SummoningBasics,
            "summoning_basics_practice"
        );

        var deckValidation = state["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsFalse();

        var issueCodes = ValidationIssueCodes(deckValidation);
        AssertThat(issueCodes).Contains("card_type_not_allowed");
        AssertThat(issueCodes).Contains("banned_card");
    }

    [TestCase]
    public void AcademyActivityLimitations_RestrictedPlayerDeckRequiresActiveDeck()
    {
        var repo = CreateRepo("academy_activity_limitations_missing_active_deck");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.SummoningBasics,
            "summoning_basics_practice"
        );

        var deckValidation = state["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsFalse();
        AssertThat(deckValidation["issues"].AsGodotArray()).IsNotEmpty();
        AssertThat(
                service.ResolveAcademyActivityBattleConfig(
                    (string)CourseIds.SummoningBasics,
                    "summoning_basics_practice"
                )
            )
            .IsEmpty();
    }

    [TestCase]
    public void AcademyActivityLimitations_ElementRulesAllowCourseElementAndNeutralOnly()
    {
        var repo = CreateRepo("academy_activity_limitations_element_rule");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var wrongElementDeckId = SetActiveDeck(
            repo,
            "Wrong Element Deck",
            CardIds.WaterWisp,
            CardIds.NeutralStarterUnit
        );
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.IntroToFire,
            "intro_fire_practice",
            wrongElementDeckId
        );

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.IntroToFire,
            "intro_fire_practice"
        );

        var deckValidation = state["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsFalse();

        AssertThat(ValidationIssueCodes(deckValidation)).Contains("card_element_not_allowed");
    }

    [TestCase]
    public void AcademyActivityLimitations_MaxDeckSizeCountsPlayerAndLoanerCards()
    {
        var repo = CreateRepo("academy_activity_limitations_max_size");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var oversizedDeckId = SetActiveDeck(
            repo,
            "Oversized Spellcraft Deck",
            Enumerable.Repeat(CardIds.Charge, 12).ToArray()
        );
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            oversizedDeckId
        );

        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice"
        );

        var deckValidation = state["deck_validation"].AsGodotDictionary();
        AssertThat(deckValidation["is_valid"].AsBool()).IsFalse();
        AssertThat(ValidationIssueCodes(deckValidation)).Contains("max_cards");
    }

    [TestCase]
    public void AcademyActivityLimitations_PlayerPlusLoanersResolvesStableAuthoredDeck()
    {
        var repo = CreateRepo("academy_activity_limitations_composed_deck");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            CardIds.FireWisp,
            CardIds.Charge
        );

        var config = service.ResolveAcademyActivityBattleConfig(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice"
        );

        var playerSide = config["player_side"].AsGodotDictionary();
        var deck = playerSide["deck"].AsGodotDictionary();
        var cards = deck["cards"].AsGodotArray().Select(item => item.AsGodotDictionary()).ToList();

        AssertThat(deck["source"].AsString()).IsEqual("authored");
        AssertThat(cards.Select(card => card["catalog_id"].AsString()).ToArray())
            .Contains((string)CardIds.FireWisp);
        AssertThat(cards.Select(card => card["catalog_id"].AsString()).ToArray())
            .Contains((string)CardIds.NeutralStarterUnit);
        AssertThat(cards.Select(card => card["catalog_id"].AsString()).ToArray())
            .Contains((string)CardIds.MagicBolt);

        var magicBolt = cards.First(card =>
            card["catalog_id"].AsString() == (string)CardIds.MagicBolt
        );
        AssertThat(magicBolt["count"].AsInt32()).IsEqual(1);
    }

    [TestCase]
    public void AcademyActivityLimitations_ComposedDeckOrderIsDeterministic()
    {
        var repo = CreateRepo("academy_activity_limitations_deterministic");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            CardIds.FireWisp,
            CardIds.Charge
        );

        var first = ResolvedPlayerCardSignature(
            service.ResolveAcademyActivityBattleConfig(
                (string)CourseIds.PracticalSpellcraft,
                "practical_spellcraft_practice"
            )
        );
        var second = ResolvedPlayerCardSignature(
            service.ResolveAcademyActivityBattleConfig(
                (string)CourseIds.PracticalSpellcraft,
                "practical_spellcraft_practice"
            )
        );

        AssertThat(first).IsEqual(second);
        AssertThat(first).IsEqual("neutral_starter_unit:1|magic_bolt:1|fire_wisp:1|charge:1");
    }

    [TestCase]
    public void AcademyActivityLimitations_FixedClassDeckIgnoresSelectedDeck()
    {
        var repo = CreateRepo("academy_activity_limitations_fixed_deck");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        SetActiveDeck(repo, "Ignored Deck", CardIds.FireWisp, CardIds.FireWisp);

        var config = service.ResolveAcademyActivityBattleConfig(
            (string)CourseIds.IntroductionToMagic101,
            "magic_101_summon_practice"
        );
        var cards = config["player_side"]
            .AsGodotDictionary()["deck"]
            .AsGodotDictionary()["cards"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["catalog_id"].AsString())
            .ToArray();

        AssertThat(cards).Contains((string)CardIds.NeutralStarterUnit);
        AssertThat(cards).NotContains((string)CardIds.FireWisp);
    }

    [TestCase]
    public void EnrollAcademyCourse_AllowsUntakenIntroElementsInSecondSemester()
    {
        var repo = CreateRepo("academy_second_semester_intro_element");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.AdvanceAcademySemester()).IsTrue();

        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToWater)).IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CurrentSemester).IsEqual(2);
        AssertThat(progress.EnrolledCourses).Contains(CourseIds.IntroToWater);
        AssertThat(progress.RemainingEnrollments).IsEqual(2);
    }

    [TestCase]
    public void AdvanceAcademySemester_RejectsUnauthoredFutureSemester()
    {
        var repo = CreateRepo("academy_reject_unauthored_future_semester");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        service.GetAcademyProgress();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        progress.Academy.CurrentYear = 1;
        progress.Academy.CurrentSemester = 2;
        progress.Academy.RemainingEnrollments = 0;
        progress.Academy.EnrolledCourses.Clear();
        progress.Academy.CompletedCourses.Add(CourseIds.IntroductionToMagic101);
        progress.Academy.CompletedCourses.Add(CourseIds.FoundationsOfMagicII);
        repo.UpdateCampaignProgress(SummonerIds.Cole, progress);

        AssertThat(service.AdvanceAcademySemester()).IsFalse();

        var updated = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(updated.CurrentYear).IsEqual(1);
        AssertThat(updated.CurrentSemester).IsEqual(2);
    }

    [TestCase]
    public void CompleteAcademyActivity_UsesExplicitActivityAndExposesStartState()
    {
        var repo = CreateRepo("academy_activity_state");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsFalse();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101])
            .IsEqual(0);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();

        var course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        var activities = course["activities"].AsGodotArray();
        var summonPractice = activities[0].AsGodotDictionary();
        var basicDuel = activities[1].AsGodotDictionary();
        var spellPractice = activities[2].AsGodotDictionary();

        AssertThat(summonPractice["is_completed"].AsBool()).IsTrue();
        AssertThat(summonPractice["can_start"].AsBool()).IsTrue();
        AssertThat(basicDuel["is_current"].AsBool()).IsTrue();
        AssertThat(basicDuel["can_start"].AsBool()).IsTrue();
        AssertThat(spellPractice["is_locked"].AsBool()).IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();

        course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        activities = course["activities"].AsGodotArray();
        basicDuel = activities[1].AsGodotDictionary();
        spellPractice = activities[2].AsGodotDictionary();

        AssertThat(basicDuel["is_completed"].AsBool()).IsTrue();
        AssertThat(basicDuel["can_start"].AsBool()).IsTrue();
        AssertThat(spellPractice["is_current"].AsBool()).IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101])
            .IsEqual(2);
    }

    [TestCase]
    public void AcademyBattleConfig_WithoutResolvedLoadout_DoesNotSerializePlayerDeck()
    {
        var battleConfig = new AcademyBattleConfig
        {
            EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1)],
            EnemyHp = 30f,
        };

        var dict = AcademyProgressHandler.ToBattleConfigDict(battleConfig);

        AssertThat(dict.ContainsKey("player_side")).IsFalse();
        AssertThat(dict["biome_id"].AsString()).IsEqual((string)BiomeIds.Default);
    }

    [TestCase]
    public void AcademyBattleConfig_WithAuthoredBiome_SerializesBiomeId()
    {
        var battleConfig = new AcademyBattleConfig { Biome = BiomeIds.IslandWater, EnemyDeck = [] };

        var dict = AcademyProgressHandler.ToBattleConfigDict(battleConfig);

        AssertThat(dict["biome_id"].AsString()).IsEqual("island_water");
    }

    [TestCase]
    public void AcademyBattleConfig_WhenNoLoanerDeckAuthored_OmitsPlayerDeckOverride()
    {
        var battleConfig = new AcademyBattleConfig
        {
            EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1)],
            EnemyHp = 30f,
        };

        var dict = AcademyProgressHandler.ToBattleConfigDict(battleConfig);

        AssertThat(dict.ContainsKey("player_side")).IsFalse();
        AssertThat(dict.ContainsKey("enemy_side")).IsTrue();
    }

    [TestCase]
    public void AcademyBattleConfig_WhenEncounterAiAuthored_SerializesEncounterAi()
    {
        var battleConfig = new AcademyBattleConfig
        {
            EnemyDeck = [],
            EnemyHp = 25f,
            AiType = "none",
            EncounterAi = new AcademyEncounterAiConfig
            {
                Preset = "scripted_encounter",
                UseTrainerAi = false,
                Rules =
                [
                    new AcademyEncounterRule
                    {
                        Id = "spawn_training_target",
                        Kind = "event",
                        StartTime = 0.75f,
                        AiType = "simple",
                        AiPersonality = "aggressive",
                        AiPlayIntervalMin = 2f,
                        AiPlayIntervalMax = 3f,
                        Actions =
                        [
                            new AcademyEncounterAction
                            {
                                Kind = "spawn_units",
                                Source = "encounter",
                                CardId = CardIds.TrainingTarget,
                                Positions = [new AcademyEncounterPosition(10f, -2f)],
                            },
                        ],
                    },
                ],
            },
        };

        var dict = AcademyProgressHandler.ToBattleConfigDict(battleConfig);

        AssertThat(dict.ContainsKey("enemy_side")).IsTrue();
        var enemySide = dict["enemy_side"].AsGodotDictionary();
        var controller = enemySide["controller"].AsGodotDictionary();
        AssertThat(controller["kind"].AsString()).IsEqual("encounter_ai");
        var encounterAi = controller["encounter_ai"].AsGodotDictionary();
        AssertThat(encounterAi["preset"].AsString()).IsEqual("scripted_encounter");
        AssertThat(encounterAi["use_trainer_ai"].AsBool()).IsFalse();

        var rules = encounterAi["rules"].AsGodotArray();
        var rule = rules[0].AsGodotDictionary();
        AssertThat(rule["ai_type"].AsString()).IsEqual("simple");
        AssertThat(rule["ai_personality"].AsString()).IsEqual("aggressive");
        var aiConfig = rule["ai_config"].AsGodotDictionary();
        AssertThat(aiConfig["play_interval_min"].AsSingle()).IsEqual(2f);
        AssertThat(aiConfig["play_interval_max"].AsSingle()).IsEqual(3f);
        var actions = rule["actions"].AsGodotArray();
        var action = actions[0].AsGodotDictionary();
        AssertThat(action["card_id"].AsString()).IsEqual((string)CardIds.TrainingTarget);
    }

    [TestCase]
    public void CompleteAcademyActivity_Magic101GrantsActivityRewardsAndCompletesCourse()
    {
        var repo = CreateRepo("academy_assessment_rewards");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();
        var neutralCountBefore = repo.GetCardCount(CardIds.NeutralStarterUnit);
        var magicBoltCountBefore = repo.GetCardCount(CardIds.MagicBolt);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(neutralCountBefore);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(neutralCountBefore + 1);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(neutralCountBefore + 1);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_spell_practice"
                )
            )
            .IsTrue();
        AssertThat(repo.GetCardCount(CardIds.MagicBolt)).IsEqual(magicBoltCountBefore + 1);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment"
                )
            )
            .IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CompletedCourses).Contains(CourseIds.IntroductionToMagic101);
        AssertThat(progress.EnrolledCourses.Contains(CourseIds.IntroductionToMagic101)).IsFalse();
        AssertThat(progress.AssessmentOutcomes["magic_101_assessment"])
            .IsEqual(AcademyActivityOutcome.Victory);
        AssertThat(progress.Transcript).HasSize(1);
        AssertThat(repo.GetCardCount(CardIds.Puff)).IsEqual(0);
        AssertThat(repo.GetCardCount(CardIds.ManaBolt)).IsEqual(0);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment"
                )
            )
            .IsFalse();
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(neutralCountBefore + 1);
        AssertThat(repo.GetCardCount(CardIds.MagicBolt)).IsEqual(magicBoltCountBefore + 1);
    }

    [TestCase]
    public void CompleteAcademyActivity_RecordsNewRewardsForConsumeOnceSummary()
    {
        var repo = CreateRepo("academy_activity_reward_summary");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        var emptyRewardSummary = service.GetLastAcademyCompletionSummary();
        AssertThat(emptyRewardSummary["granted_rewards"].AsGodotArray()).IsEmpty();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();

        var summary = service.GetLastAcademyCompletionSummary();
        AssertThat(summary["course_id"].AsString())
            .IsEqual((string)CourseIds.IntroductionToMagic101);
        AssertThat(summary["activity_id"].AsString()).IsEqual("magic_101_basic_duel");
        AssertThat(summary["completed_course"].AsBool()).IsFalse();

        var rewards = summary["granted_rewards"].AsGodotArray();
        AssertThat(rewards).HasSize(1);
        var reward = rewards[0].AsGodotDictionary();
        AssertThat(reward["card_id"].AsString()).IsEqual((string)CardIds.NeutralStarterUnit);
        AssertThat(reward["source_type"].AsString()).IsEqual("activity");

        var consumed = service.ConsumeLastAcademyCompletionSummary();
        AssertThat(consumed["granted_rewards"].AsGodotArray()).HasSize(1);
        AssertThat(service.GetLastAcademyCompletionSummary()).IsEmpty();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        var replaySummary = service.GetLastAcademyCompletionSummary();
        AssertThat(replaySummary["outcome"].AsString()).IsEqual("Victory");
        AssertThat(replaySummary["granted_rewards"].AsGodotArray()).IsEmpty();
    }

    [TestCase]
    public void CompleteAcademyCourse_RecordsCourseRewardsForSummary()
    {
        var repo = CreateRepo("academy_course_reward_summary");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();

        var summary = service.ConsumeLastAcademyCompletionSummary();
        AssertThat(summary["course_id"].AsString()).IsEqual((string)CourseIds.SummoningBasics);
        AssertThat(summary["activity_id"].AsString()).IsEmpty();
        AssertThat(summary["completed_course"].AsBool()).IsTrue();

        var rewards = summary["granted_rewards"].AsGodotArray();
        AssertThat(rewards).HasSize(1);
        var reward = rewards[0].AsGodotDictionary();
        AssertThat(reward["card_id"].AsString()).IsEqual((string)CardIds.FireWisp);
        AssertThat(reward["source_type"].AsString()).IsEqual("course");
    }

    [TestCase]
    public void CompleteAcademyActivity_FailedActivityDoesNotGrantActivityReward()
    {
        var repo = CreateRepo("academy_failed_activity_no_reward");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();
        var cardCountBefore = repo.GetCardCount(CardIds.NeutralStarterUnit);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel",
                    (int)AcademyActivityOutcome.Defeat
                )
            )
            .IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101])
            .IsEqual(1);
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(cardCountBefore);
    }

    [TestCase]
    public void CompleteAcademyActivity_DefeatedAssessmentRecordsPermanentOutcomeAndAdvances()
    {
        var repo = CreateRepo("academy_assessment_defeat");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        CompleteIntroPracticeActivities(service);
        var neutralBefore = repo.GetCardCount(CardIds.NeutralStarterUnit);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment",
                    (int)AcademyActivityOutcome.Defeat
                )
            )
            .IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.AssessmentOutcomes["magic_101_assessment"])
            .IsEqual(AcademyActivityOutcome.Defeat);
        AssertThat(progress.CompletedCourses).Contains(CourseIds.IntroductionToMagic101);
        AssertThat(progress.Transcript[0].Grade).IsEqual("fail");
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(neutralBefore);
    }

    [TestCase]
    public void CompleteAcademyActivity_AbandonedAssessmentRecordsPermanentOutcome()
    {
        var repo = CreateRepo("academy_assessment_abandoned");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        CompleteIntroPracticeActivities(service);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment",
                    (int)AcademyActivityOutcome.Abandoned
                )
            )
            .IsTrue();
        AssertThat(
                repo.GetCampaignProgress(SummonerIds.Cole).Academy.AssessmentOutcomes[
                    "magic_101_assessment"
                ]
            )
            .IsEqual(AcademyActivityOutcome.Abandoned);
    }

    [TestCase]
    public void ClassLoadout_PersistsIndependentlyAndOnlyCreatesNamedDeckOnExplicitSave()
    {
        var repo = CreateRepo("academy_class_loadout_persistence");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var initialDeckCount = repo.ListDecks().Length;
        var selectedDeck = SetActiveDeck(repo, "Selection Source", CardIds.Charge);
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            selectedDeck
        );
        AssertThat(repo.ListDecks()).HasSize(initialDeckCount + 1);

        var reloadedService = CreateCampaignService(repo, SummonerIds.Cole);
        var state = reloadedService.GetAcademyActivityLaunchState(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice"
        );
        AssertThat(state["loadout"].AsGodotDictionary()["selected_cards"].AsGodotArray())
            .HasSize(1);
        RemoveAllCards(repo, CardIds.NeutralStarterUnit, CardIds.MagicBolt);
        var saveResult = reloadedService.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "",
            "My Lesson Deck"
        );
        AssertThat(saveResult["success"].AsBool()).IsTrue();
        AssertThat(saveResult["created"].AsBool()).IsTrue();
        AssertThat(repo.ListDecks()).HasSize(initialDeckCount + 2);
        var savedDeck = repo.GetDeck(DeckId.FromString(saveResult["deck_id"].AsString()));
        AssertThat(savedDeck).IsNotNull();
        AssertThat(savedDeck!.Name).IsEqual("My Lesson Deck");
        AssertThat(savedDeck.CardInstanceIds).HasSize(1);
        AssertThat(repo.GetProfileMetadata()!.Meta.SelectedDeck).IsEqual(selectedDeck);
        AssertThat(saveResult["omitted_supplied_card_ids"].AsGodotArray()).HasSize(2);
    }

    [TestCase]
    public void FillClassLoadoutFromDeck_CopiesInDeckOrderWithoutChangingSource()
    {
        var repo = CreateRepo("academy_fill_class_loadout");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var sourceDeckId = SetActiveDeck(
            repo,
            "Fill Source",
            CardIds.FireWisp,
            CardIds.WaterWisp,
            CardIds.Charge
        );
        var sourceBefore = repo.GetDeck(DeckId.FromString(sourceDeckId))!.CardInstanceIds.ToArray();

        var result = service.FillAcademyActivityLoadoutFromDeck(
            (string)CourseIds.IntroToFire,
            "intro_fire_practice",
            sourceDeckId
        );

        AssertThat(result["success"].AsBool()).IsTrue();
        AssertThat(result["copied_count"].AsInt32()).IsEqual(2);
        AssertThat(result["skipped_card_instance_ids"].AsGodotArray()).HasSize(1);
        var state = service.GetAcademyActivityLaunchState(
            (string)CourseIds.IntroToFire,
            "intro_fire_practice"
        );
        var selectedIds = state["loadout"]
            .AsGodotDictionary()["selected_cards"]
            .AsGodotArray()
            .Select(item => item.AsGodotDictionary()["card_instance_id"].AsString())
            .ToArray();
        AssertThat(selectedIds).ContainsExactly(sourceBefore[0].Value, sourceBefore[2].Value);
        AssertThat(repo.GetDeck(DeckId.FromString(sourceDeckId))!.CardInstanceIds)
            .ContainsExactly(sourceBefore);
    }

    [TestCase]
    public void SaveClassLoadoutToDeck_ReplacesConfirmedDeckAndPreservesActiveSelection()
    {
        var repo = CreateRepo("academy_replace_class_loadout");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var targetDeckId = SetActiveDeck(repo, "Keep This Name", CardIds.FireWisp);
        var selectedDeckId = SetActiveDeck(repo, "Selection Source", CardIds.Charge);
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            selectedDeckId
        );

        var result = service.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            targetDeckId,
            "Ignored New Name"
        );

        AssertThat(result["success"].AsBool()).IsTrue();
        AssertThat(result["created"].AsBool()).IsFalse();
        AssertThat(result["deck_id"].AsString()).IsEqual(targetDeckId);
        AssertThat(repo.GetDeck(DeckId.FromString(targetDeckId))!.Name).IsEqual("Keep This Name");
        AssertThat(repo.GetProfileMetadata()!.Meta.SelectedDeck).IsEqual(selectedDeckId);
    }

    [TestCase]
    public void ClassLoadoutDeckOperations_ReturnSpecificErrorsForInvalidRequests()
    {
        var repo = CreateRepo("academy_class_loadout_invalid_deck_requests");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        var missingSource = service.FillAcademyActivityLoadoutFromDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "missing-deck"
        );
        var missingName = service.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "",
            "  "
        );
        var missingTarget = service.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "missing-deck",
            ""
        );

        AssertThat(missingSource["error"].AsString()).IsEqual("source_deck_not_found");
        AssertThat(missingName["error"].AsString()).IsEqual("deck_name_required");
        AssertThat(missingTarget["error"].AsString()).IsEqual("target_deck_not_found");
    }

    [TestCase]
    public void SaveClassLoadoutToDeck_IncludesOwnedCopiesOfSuppliedCards()
    {
        var repo = CreateRepo("academy_save_owned_supplied_cards");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        repo.GrantCards(
            new[] { (CardIds.NeutralStarterUnit, "common"), (CardIds.MagicBolt, "common") }
        );
        var selectedDeckId = SetActiveDeck(repo, "Selection Source", CardIds.Charge);
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            selectedDeckId
        );

        var result = service.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "",
            "Owned Lesson Cards"
        );

        AssertThat(result["success"].AsBool()).IsTrue();
        AssertThat(result["omitted_supplied_card_ids"].AsGodotArray()).IsEmpty();
        var savedDeck = repo.GetDeck(DeckId.FromString(result["deck_id"].AsString()))!;
        var savedCatalogIds = savedDeck
            .CardInstanceIds.Select(instanceId => repo.GetCard(instanceId)!.CatalogId)
            .ToArray();
        AssertThat(savedCatalogIds).Contains(CardIds.Charge);
        AssertThat(savedCatalogIds).Contains(CardIds.NeutralStarterUnit);
        AssertThat(savedCatalogIds).Contains(CardIds.MagicBolt);
    }

    [TestCase]
    public void SaveClassLoadoutToDeck_RejectsDecksAboveTheGlobalMaximum()
    {
        var repo = CreateRepo("academy_save_oversized_loadout");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        repo.GrantCards(
            new[] { (CardIds.NeutralStarterUnit, "common"), (CardIds.MagicBolt, "common") }
        );
        var selectedDeckId = SetActiveDeck(
            repo,
            "Oversized Source",
            Enumerable.Repeat(CardIds.Charge, 11).ToArray()
        );
        SelectActiveDeckAsClassLoadout(
            repo,
            service,
            CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            selectedDeckId
        );

        var result = service.SaveAcademyActivityLoadoutToDeck(
            (string)CourseIds.PracticalSpellcraft,
            "practical_spellcraft_practice",
            "",
            "Too Large"
        );

        AssertThat(result["success"].AsBool()).IsFalse();
        AssertThat(result["error"].AsString()).IsEqual("deck_too_large");
        AssertThat(repo.ListDecks().Select(deck => deck.Name)).NotContains("Too Large");
    }

    [TestCase]
    public void CompleteAcademyActivity_ClaimedActivityRewardDoesNotGrantAgainAfterProgressRewind()
    {
        var repo = CreateRepo("academy_claimed_activity_reward_rewind");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101)).IsTrue();
        var cardCountBefore = repo.GetCardCount(CardIds.NeutralStarterUnit);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(cardCountBefore + 1);

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        progress.Academy.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101] = 1;
        repo.UpdateCampaignProgress(SummonerIds.Cole, progress);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();

        progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(repo.GetCardCount(CardIds.NeutralStarterUnit)).IsEqual(cardCountBefore + 1);
        AssertThat(repo.GetRewardState().ClaimReceipts).HasSize(1);

        var course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        var rewardPreviews = course["reward_previews"].AsGodotArray();
        var starterReward = rewardPreviews
            .Select(item => item.AsGodotDictionary())
            .First(reward =>
                reward["label_key"].AsString() == "academy.reward.neutral_starter_unit"
            );
        AssertThat(starterReward["status"].AsString()).IsEqual("claimed");
    }

    [TestCase]
    public void CompleteAcademyCourse_ClaimedCourseRewardDoesNotGrantAgainAfterProgressRepair()
    {
        var repo = CreateRepo("academy_claimed_course_reward_repair");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();

        var cardCountBefore = repo.GetCardCount(CardIds.FireWisp);

        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(repo.GetCardCount(CardIds.FireWisp)).IsEqual(cardCountBefore + 1);

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        progress.Academy.CompletedCourses.Remove(CourseIds.SummoningBasics);
        progress.Academy.EnrolledCourses.Add(CourseIds.SummoningBasics);
        repo.UpdateCampaignProgress(SummonerIds.Cole, progress);

        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();

        progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(repo.GetCardCount(CardIds.FireWisp)).IsEqual(cardCountBefore + 1);
        AssertThat(
                repo.GetRewardState()
                    .ClaimReceipts.Values.Count(receipt =>
                        receipt
                            .AppliedGrants.OfType<CardRewardGrantDefinition>()
                            .Any(grant => grant.CardId == CardIds.FireWisp)
                    )
            )
            .IsEqual(1);

        var course = service.GetAcademyCourse((string)CourseIds.SummoningBasics);
        var rewardPreviews = course["reward_previews"].AsGodotArray();
        var fireWispReward = rewardPreviews
            .Select(item => item.AsGodotDictionary())
            .First(reward => reward["label_key"].AsString() == "academy.reward.basic_summon");
        AssertThat(fireWispReward["status"].AsString()).IsEqual("claimed");
    }

    [TestCase]
    public void CompleteAcademyActivity_CourseWithNoImmediateRewardCompletesNormally()
    {
        var repo = CreateRepo("academy_preview_only_rewards");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.AdvanceAcademySemester()).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToEmpowerment))
            .IsTrue();

        var empowerment = service.GetAcademyCourse((string)CourseIds.IntroductionToEmpowerment);
        var rewards = empowerment["reward_previews"].AsGodotArray();
        AssertThat(rewards).IsEmpty();

        CompleteCourseActivities(service, CourseIds.IntroductionToEmpowerment, "empowerment");

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CompletedCourses).Contains(CourseIds.IntroductionToEmpowerment);
        AssertThat(progress.EnrolledCourses).NotContains(CourseIds.IntroductionToEmpowerment);
        AssertThat(progress.Transcript.Select(entry => entry.CourseId))
            .Contains(CourseIds.IntroductionToEmpowerment);
    }

    private CampaignService CreateCampaignService(
        IProfileRepository repo,
        SummonerId activeSummoner
    )
    {
        var service = CreateNode<CampaignService>();
        service.InitForTesting(repo);
        service.SetActiveSummonerGetter(Callable.From(() => (string)activeSummoner));
        service.InitializeCatalogs();
        return service;
    }

    private ProfileRepository CreateRepo(string profileId)
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId(profileId));
        repo.ResetProfile();
        if (!repo.IsSummonerUnlocked(SummonerIds.Cole))
            repo.UnlockSummoner(SummonerIds.Cole);
        return repo;
    }

    private static string SetActiveDeck(
        ProfileRepository repo,
        string deckName,
        params CardId[] catalogIds
    )
    {
        var granted = repo.GrantCards(catalogIds.Select(cardId => (cardId, "common")));
        var deckId = repo.UpsertDeck(
            new Deck
            {
                Id = DeckId.None,
                Name = deckName,
                SummonerId = SummonerIds.Cole,
                CardInstanceIds = [.. granted],
            }
        );
        repo.UpdateProfileMeta(new MetaUpdate { SelectedDeck = deckId.Value });
        return deckId.Value;
    }

    private static void SetClassLoadout(
        ProfileRepository repo,
        CampaignService service,
        CourseId courseId,
        string activityId,
        params CardId[] catalogIds
    )
    {
        var deckId = SetActiveDeck(repo, $"{activityId} cards", catalogIds);
        SelectActiveDeckAsClassLoadout(repo, service, courseId, activityId, deckId);
    }

    private static void SelectActiveDeckAsClassLoadout(
        ProfileRepository repo,
        CampaignService service,
        CourseId courseId,
        string activityId,
        string deckId
    )
    {
        var slots = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var instanceId in repo.GetDeck(DeckId.FromString(deckId))!.CardInstanceIds)
            slots.Add(new Godot.Collections.Dictionary { ["card_instance_id"] = instanceId.Value });
        AssertThat(service.UpdateAcademyActivityLoadout((string)courseId, activityId, slots))
            .IsTrue();
    }

    private static void RemoveAllCards(ProfileRepository repo, params CardId[] catalogIds)
    {
        var ids = catalogIds.ToHashSet();
        foreach (var card in repo.ListCards().Where(card => ids.Contains(card.CatalogId)).ToArray())
            repo.RemoveCard(card.Id);
    }

    private static string ResolvedPlayerCardSignature(Godot.Collections.Dictionary config)
    {
        var cards = config["player_side"]
            .AsGodotDictionary()["deck"]
            .AsGodotDictionary()["cards"]
            .AsGodotArray()
            .Select(item =>
            {
                var card = item.AsGodotDictionary();
                return $"{card["catalog_id"].AsString()}:{card["count"].AsInt32()}";
            });
        return string.Join("|", cards);
    }

    private static string[] ValidationIssueCodes(Godot.Collections.Dictionary validation) =>
        validation["issues"]
            .AsGodotArray()
            .Select(issue => issue.AsGodotDictionary()["code"].AsString())
            .ToArray();

    private static void CompleteIntroCourse(CampaignService service)
    {
        CompleteIntroPracticeActivities(service);
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment"
                )
            )
            .IsTrue();
    }

    private static void CompleteIntroPracticeActivities(CampaignService service)
    {
        var progress = service.GetAcademyProgress();
        var enrolled = progress["enrolled_courses"]
            .AsGodotArray()
            .Any(course => course.AsString() == (string)CourseIds.IntroductionToMagic101);
        if (!enrolled)
            AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToMagic101))
                .IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_spell_practice"
                )
            )
            .IsTrue();
    }

    private static void CompleteCourseActivities(
        CampaignService service,
        CourseId courseId,
        string activityPrefix
    )
    {
        AssertThat(service.CompleteAcademyActivity((string)courseId, $"{activityPrefix}_practice"))
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity((string)courseId, $"{activityPrefix}_assessment")
            )
            .IsTrue();
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var node = new T { Name = $"{typeof(T).Name}_Academy_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }
}
