namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
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
                    "magic_101_practice"
                )
            )
            .IsFalse();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(0);

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_lesson"
                )
            )
            .IsTrue();

        var course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        var activities = course["activities"].AsGodotArray();
        var lesson = activities[0].AsGodotDictionary();
        var practice = activities[1].AsGodotDictionary();
        var assessment = activities[2].AsGodotDictionary();

        AssertThat(lesson["is_completed"].AsBool()).IsTrue();
        AssertThat(lesson["can_start"].AsBool()).IsFalse();
        AssertThat(practice["is_current"].AsBool()).IsTrue();
        AssertThat(practice["can_start"].AsBool()).IsTrue();
        AssertThat(assessment["is_locked"].AsBool()).IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_practice"
                )
            )
            .IsTrue();

        course = service.GetAcademyCourse((string)CourseIds.IntroductionToMagic101);
        activities = course["activities"].AsGodotArray();
        practice = activities[1].AsGodotDictionary();
        assessment = activities[2].AsGodotDictionary();

        AssertThat(practice["is_completed"].AsBool()).IsTrue();
        AssertThat(practice["can_start"].AsBool()).IsTrue();
        AssertThat(assessment["is_current"].AsBool()).IsTrue();

        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_practice"
                )
            )
            .IsTrue();
        progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CourseActivityIndex[(string)CourseIds.IntroductionToMagic101]).IsEqual(2);
    }

    [TestCase]
    public void CompleteAcademyActivity_FinalAssessmentCompletesCourseAndGrantsCatalogRewards()
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
                    "magic_101_lesson"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_practice"
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

        var progress = repo.GetCampaignProgress(SummonerIds.Cole).Academy;
        AssertThat(progress.CompletedCourses).Contains(CourseIds.IntroductionToMagic101);
        AssertThat(progress.EnrolledCourses.Contains(CourseIds.IntroductionToMagic101)).IsFalse();
        AssertThat(progress.OfficialAssessmentsCompleted).Contains("magic_101_assessment");
        AssertThat(progress.Transcript).HasSize(1);
        AssertThat(granted).Contains(CardIds.Puff);
        AssertThat(granted).Contains(CardIds.ManaBolt);
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
                    "magic_101_lesson"
                )
            )
            .IsTrue();
        AssertThat(
                service.CompleteAcademyActivity(
                    (string)CourseIds.IntroductionToMagic101,
                    "magic_101_practice"
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
