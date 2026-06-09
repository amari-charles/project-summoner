namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Campaign.Handlers;
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
    public void FreshAcademyProgress_AutoEnrollsRequiredIntroCourse()
    {
        var repo = CreateRepo("academy_fresh_required_intro");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        service.GetAcademyProgress();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.RemainingEnrollments).IsEqual(2);
        AssertThat(progress.EnrolledCourses).Contains(CourseIds.IntroductionToMagic101);
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(0);

        var intro = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        AssertThat(intro["is_enrolled"].AsBool()).IsTrue();
        AssertThat(intro["is_available"].AsBool()).IsFalse();
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
        AssertThat(progress.RemainingEnrollments).IsEqual(2);
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

        AssertThat(foundationChoice["group_id"].AsString())
            .IsEqual("year_1_semester_1_foundation");
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
    public void EnrollAcademyCourse_AllowsUntakenIntroElementsInSecondSemester()
    {
        var repo = CreateRepo("academy_second_semester_intro_element");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.AdvanceAcademySemester()).IsTrue();

        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToWater)).IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CurrentSemester).IsEqual(2);
        AssertThat(progress.EnrolledCourses).Contains(CourseIds.IntroToWater);
        AssertThat(progress.RemainingEnrollments).IsEqual(1);
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

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsFalse();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(0);

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
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(2);
    }

    [TestCase]
    public void AcademyBattleConfig_WhenLoanerDeckAuthored_SerializesPlayerDeckOverride()
    {
        var battleConfig = new AcademyBattleConfig
        {
            LoanerPlayerDeck =
            [
                new DeckEntry(CardIds.Puff, 2),
                new DeckEntry(CardIds.ManaBolt, 1),
            ],
            EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1)],
            EnemyHp = 30f,
        };

        var dict = AcademyProgressHandler.ToBattleConfigDict(battleConfig);

        AssertThat(dict.ContainsKey("player_side")).IsTrue();

        var playerSide = dict["player_side"].AsGodotDictionary();
        var playerDeck = playerSide["deck"].AsGodotDictionary();
        var loanerDeck = playerDeck["cards"].AsGodotArray();
        AssertThat(loanerDeck).HasSize(2);

        var first = loanerDeck[0].AsGodotDictionary();
        var second = loanerDeck[1].AsGodotDictionary();
        AssertThat(first["catalog_id"].AsString()).IsEqual((string)CardIds.Puff);
        AssertThat(first["count"].AsInt32()).IsEqual(2);
        AssertThat(second["catalog_id"].AsString()).IsEqual((string)CardIds.ManaBolt);
        AssertThat(second["count"].AsInt32()).IsEqual(1);
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
        var granted = new List<CardId>();
        service.SetCollectionCallbacks(
            Callable.From(
                (string catalogId, string _rarity) =>
                {
                    granted.Add(CardId.FromString(catalogId));
                    return $"test_{catalogId}";
                }
            )
        );

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_summon_practice"
                )
            )
            .IsTrue();
        AssertThat(granted).IsEmpty();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(granted).Contains(CardIds.NeutralStarterUnit);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_basic_duel"
                )
            )
            .IsTrue();
        AssertThat(granted.Count(card => card == CardIds.NeutralStarterUnit)).IsEqual(1);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_spell_practice"
                )
            )
            .IsTrue();
        AssertThat(granted).Contains(CardIds.MagicBolt);

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
        AssertThat(progress.OfficialAssessmentsCompleted).Contains("magic_101_assessment");
        AssertThat(progress.Transcript).HasSize(1);
        AssertThat(granted).NotContains(CardIds.Puff);
        AssertThat(granted).NotContains(CardIds.ManaBolt);
    }

    [TestCase]
    public void CompleteAcademyActivity_FailedActivityDoesNotGrantActivityReward()
    {
        var repo = CreateRepo("academy_failed_activity_no_reward");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var granted = new List<CardId>();
        service.SetCollectionCallbacks(
            Callable.From(
                (string catalogId, string _rarity) =>
                {
                    granted.Add(CardId.FromString(catalogId));
                    return $"test_{catalogId}";
                }
            )
        );

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
                    false
                )
            )
            .IsFalse();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(1);
        AssertThat(progress.ActivityRewardsClaimed).IsEmpty();
        AssertThat(granted).IsEmpty();
    }

    [TestCase]
    public void CompleteAcademyActivity_ClaimedActivityRewardDoesNotGrantAgainAfterProgressRewind()
    {
        var repo = CreateRepo("academy_claimed_activity_reward_rewind");
        var service = CreateCampaignService(repo, SummonerIds.Cole);
        var granted = new List<CardId>();
        service.SetCollectionCallbacks(
            Callable.From(
                (string catalogId, string _rarity) =>
                {
                    granted.Add(CardId.FromString(catalogId));
                    return $"test_{catalogId}_{granted.Count}";
                }
            )
        );

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
        AssertThat(granted.Count(card => card == CardIds.NeutralStarterUnit)).IsEqual(1);

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
        AssertThat(granted.Count(card => card == CardIds.NeutralStarterUnit)).IsEqual(1);
        AssertThat(progress.Academy.ActivityRewardsClaimed).HasSize(1);

        var course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        var rewardPreviews = course["reward_previews"].AsGodotArray();
        var starterReward = rewardPreviews
            .Select(item => item.AsGodotDictionary())
            .First(reward =>
                reward.TryGetValue("card_id", out var cardId)
                && cardId.AsString() == (string)CardIds.NeutralStarterUnit
            );
        AssertThat(starterReward["grant_state"].AsString()).IsEqual("claimed");
        AssertThat(starterReward["is_grantable"].AsBool()).IsFalse();
    }

    [TestCase]
    public void CompleteAcademyActivity_PreviewOnlyRewardsCompleteWithoutGrantingCards()
    {
        var repo = CreateRepo("academy_preview_only_rewards");
        var service = CreateCampaignService(repo, SummonerIds.Cole);

        CompleteIntroCourse(service);
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.SummoningBasics)).IsTrue();
        AssertThat(service.CompleteAcademyCourse((string)CourseIds.IntroToFire)).IsTrue();
        AssertThat(service.AdvanceAcademySemester()).IsTrue();
        AssertThat(service.EnrollAcademyCourse((string)CourseIds.IntroductionToEmpowerment))
            .IsTrue();

        var granted = new List<CardId>();
        service.SetCollectionCallbacks(
            Callable.From(
                (string catalogId, string _rarity) =>
                {
                    granted.Add(CardId.FromString(catalogId));
                    return $"test_{catalogId}";
                }
            )
        );

        var empowerment = service.GetAcademyCourse((string)CourseIds.IntroductionToEmpowerment);
        var rewards = empowerment["reward_previews"].AsGodotArray();
        var reward = rewards[0].AsGodotDictionary();
        AssertThat(reward["kind"].AsString()).IsEqual(AcademyRewardKind.CardTrait.ToString());
        AssertThat(reward["grant_state"].AsString()).IsEqual("preview_only");
        AssertThat(reward["is_grantable"].AsBool()).IsFalse();

        CompleteCourseActivities(service, CourseIds.IntroductionToEmpowerment, "empowerment");

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CompletedCourses).Contains(CourseIds.IntroductionToEmpowerment);
        AssertThat(progress.EnrolledCourses).NotContains(CourseIds.IntroductionToEmpowerment);
        AssertThat(progress.Transcript.Select(entry => entry.CourseId))
            .Contains(CourseIds.IntroductionToEmpowerment);
        AssertThat(granted).IsEmpty();
    }

    private CampaignService CreateCampaignService(IProfileRepository repo, SummonerId activeSummoner)
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

    private static void CompleteIntroCourse(CampaignService service)
    {
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
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_assessment"
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
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)courseId,
                    $"{activityPrefix}_lesson"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)courseId,
                    $"{activityPrefix}_practice"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)courseId,
                    $"{activityPrefix}_assessment"
                )
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
