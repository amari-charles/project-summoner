using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Godot;

namespace Fateforged.Meta.Campaign.Handlers;

public class AcademyProgressHandler
{
    private const int DefaultSemesterEnrollments = 3;

    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummonerFunc;
    private readonly Func<string, string, string>? _grantCardFunc;

    public AcademyProgressHandler(
        IProfileRepository profileRepo,
        Func<SummonerId> getActiveSummonerFunc,
        Func<string, string, string>? grantCardFunc
    )
    {
        _profileRepo = profileRepo;
        _getActiveSummonerFunc = getActiveSummonerFunc;
        _grantCardFunc = grantCardFunc;
    }

    public Godot.Collections.Dictionary GetProgress()
    {
        var progress = GetOrCreateProgress();
        return DtoConverters.ToDict(progress.Academy);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableCourses()
    {
        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        return GetCoursesForSemester(academy.CurrentYear, academy.CurrentSemester);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCoursesForSemester(
        int year,
        int semester
    )
    {
        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var course in GetCandidateCoursesForSemester(academy, year, semester))
        {
            result.Add(ToCourseDict(course, academy, year, semester));
        }

        return result;
    }

    public Godot.Collections.Dictionary GetCourse(string courseId)
    {
        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
        if (course == null)
            return [];

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        return ToCourseDict(course, academy, course.Year, course.Semester);
    }

    public bool EnrollCourse(string courseId)
    {
        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
        if (course == null)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        var validation = ValidateCourseAvailable(course, academy);
        if (!validation.available)
        {
            GD.PushWarning(
                $"AcademyProgressHandler: Cannot enroll in '{courseId}': {validation.reason}"
            );
            return false;
        }

        academy.RemainingEnrollments -= course.EnrollmentCost;
        academy.EnrolledCourses.Add(course.Id);
        academy.CourseActivityIndex[(string)course.Id] = 0;

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        return true;
    }

    public bool CompleteNextActivity(string courseId)
    {
        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
        if (course == null)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        if (!academy.EnrolledCourses.Contains(course.Id))
            return false;

        var key = (string)course.Id;
        var nextIndex = academy.CourseActivityIndex.GetValueOrDefault(key, 0) + 1;
        academy.CourseActivityIndex[key] = nextIndex;

        if (nextIndex >= course.Activities.Count)
        {
            _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
            return CompleteCourse(courseId);
        }

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        return true;
    }

    public bool CompleteCourse(string courseId, string grade = "pass", bool honors = false)
    {
        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
        if (course == null)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;

        if (!academy.EnrolledCourses.Contains(course.Id) && !course.IsRequired)
            return false;

        if (!academy.CompletedCourses.Contains(course.Id))
            academy.CompletedCourses.Add(course.Id);

        academy.EnrolledCourses.Remove(course.Id);
        academy.CourseActivityIndex.Remove((string)course.Id);
        academy.Transcript.Add(
            new AcademyTranscriptEntry
            {
                CourseId = course.Id,
                Grade = string.IsNullOrEmpty(grade) ? "pass" : grade,
                Honors = honors,
                SemesterKey = $"year_{academy.CurrentYear}_semester_{academy.CurrentSemester}",
            }
        );

        GrantCourseCards(course.Id);

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        return true;
    }

    public bool AdvanceSemester()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;

        if (!CanAdvanceSemester(academy))
            return false;

        if (academy.CurrentSemester == 1)
        {
            academy.CurrentSemester = 2;
        }
        else
        {
            academy.CurrentSemester = 1;
            academy.CurrentYear += 1;
        }

        academy.RemainingEnrollments = GetDefaultEnrollments(
            academy.CurrentYear,
            academy.CurrentSemester
        );
        academy.EnrolledCourses.Clear();

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        return true;
    }

    private CampaignProgress GetOrCreateProgress()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return new CampaignProgress();

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        EnsureAcademyInitialized(progress.Academy);
        _profileRepo.UpdateCampaignProgress(summonerId, progress);
        return progress;
    }

    private static void EnsureAcademyInitialized(AcademyProgress academy)
    {
        if (
            academy.CurrentYear == 1
            && academy.CurrentSemester == 1
            && academy.RemainingEnrollments == 0
            && academy.CompletedCourses.Count == 0
            && academy.EnrolledCourses.Count == 0
        )
        {
            academy.RemainingEnrollments = GetDefaultEnrollments(1, 1);
        }
    }

    private static int GetDefaultEnrollments(int year, int semester) =>
        year == 1 && semester is 1 or 2 ? DefaultSemesterEnrollments : DefaultSemesterEnrollments;

    private IEnumerable<AcademyCourseDefinition> GetCandidateCourses(AcademyProgress academy) =>
        GetCandidateCoursesForSemester(academy, academy.CurrentYear, academy.CurrentSemester);

    private IEnumerable<AcademyCourseDefinition> GetCandidateCoursesForSemester(
        AcademyProgress academy,
        int year,
        int semester
    )
    {
        var candidates = AcademyCourseCatalog.ForSemester(year, semester).ToList();

        if (year == 1 && semester == 2)
        {
            foreach (
                var intro in AcademyCourseCatalog
                    .ForSemester(1, 1)
                    .Where(course =>
                        course.ChoiceGroupId == "year_1_semester_1_element"
                        && !academy.CompletedCourses.Contains(course.Id)
                    )
            )
            {
                candidates.Add(intro);
            }
        }

        return candidates.DistinctBy(course => course.Id);
    }

    private Godot.Collections.Dictionary ToCourseDict(
        AcademyCourseDefinition course,
        AcademyProgress academy,
        int viewedYear,
        int viewedSemester
    )
    {
        var isCurrentSemester =
            viewedYear == academy.CurrentYear && viewedSemester == academy.CurrentSemester;
        var validation = isCurrentSemester
            ? ValidateCourseAvailable(course, academy)
            : (
                available: false,
                reason: GetSemesterRelation(academy, viewedYear, viewedSemester)
            );

        var rewards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var reward in course.RewardPreviews)
        {
            rewards.Add(
                new Godot.Collections.Dictionary
                {
                    ["kind"] = reward.Kind.ToString(),
                    ["preview_type"] = reward.PreviewType.ToString(),
                    ["label_key"] = reward.LabelKey,
                    ["element"] = reward.Element,
                    ["card_role"] = reward.CardRole,
                }
            );
        }

        var activities = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var activity in course.Activities)
        {
            activities.Add(
                new Godot.Collections.Dictionary
                {
                    ["id"] = activity.Id,
                    ["type"] = activity.Type.ToString(),
                    ["label_key"] = activity.LabelKey,
                    ["is_official_assessment"] = activity.IsOfficialAssessment,
                    ["repeatable"] = activity.Repeatable,
                    ["battle_config"] = ToBattleConfigDict(activity.BattleConfig),
                }
            );
        }

        var activityIndex = academy.CourseActivityIndex.GetValueOrDefault((string)course.Id, 0);
        Godot.Collections.Dictionary nextActivity = new();
        if (activityIndex >= 0 && activityIndex < course.Activities.Count)
        {
            var activity = course.Activities[activityIndex];
            nextActivity = new Godot.Collections.Dictionary
            {
                ["id"] = activity.Id,
                ["type"] = activity.Type.ToString(),
                ["label_key"] = activity.LabelKey,
                ["is_official_assessment"] = activity.IsOfficialAssessment,
                ["repeatable"] = activity.Repeatable,
                ["battle_config"] = ToBattleConfigDict(activity.BattleConfig),
            };
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)course.Id,
            ["name_key"] = course.NameKey,
            ["description_key"] = course.DescriptionKey,
            ["year"] = course.Year,
            ["semester"] = course.Semester,
            ["track"] = course.Track.ToString(),
            ["enrollment_cost"] = course.EnrollmentCost,
            ["is_required"] = course.IsRequired,
            ["choice_group_id"] = course.ChoiceGroupId,
            ["is_available"] = validation.available,
            ["unavailable_reason"] = validation.reason,
            ["is_current_semester"] = isCurrentSemester,
            ["is_enrolled"] = academy.EnrolledCourses.Contains(course.Id),
            ["is_completed"] = academy.CompletedCourses.Contains(course.Id),
            ["activity_index"] = activityIndex,
            ["activities"] = activities,
            ["next_activity"] = nextActivity,
            ["reward_previews"] = rewards,
        };
    }

    private static Godot.Collections.Dictionary ToBattleConfigDict(
        AcademyBattleConfig? battleConfig
    )
    {
        if (battleConfig == null)
            return new Godot.Collections.Dictionary();

        var enemyDeck = new Godot.Collections.Array();
        foreach (var entry in battleConfig.EnemyDeck)
        {
            enemyDeck.Add(
                new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = (string)entry.CardId,
                    ["count"] = entry.Count,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["enemy_deck"] = enemyDeck,
            ["enemy_hp"] = battleConfig.EnemyHp,
            ["ai_type"] = battleConfig.AiType,
            ["ai_difficulty"] = battleConfig.AiDifficulty,
            ["ai_config"] = new Godot.Collections.Dictionary
            {
                ["play_interval_min"] = battleConfig.AiPlayIntervalMin,
                ["play_interval_max"] = battleConfig.AiPlayIntervalMax,
            },
            ["card_xp_reward"] = 0,
            ["summoner_xp_reward"] = 0,
        };
    }

    private static string GetSemesterRelation(AcademyProgress academy, int year, int semester)
    {
        var viewedIndex = ((year - 1) * 2) + semester;
        var currentIndex = ((academy.CurrentYear - 1) * 2) + academy.CurrentSemester;
        return viewedIndex < currentIndex ? "past_semester" : "future_semester";
    }

    private (bool available, string reason) ValidateCourseAvailable(
        AcademyCourseDefinition course,
        AcademyProgress academy
    )
    {
        if (academy.CompletedCourses.Contains(course.Id))
            return (false, "completed");

        if (academy.EnrolledCourses.Contains(course.Id))
            return (false, "enrolled");

        if (academy.RemainingEnrollments < course.EnrollmentCost)
            return (false, "not_enough_enrollments");

        foreach (var prerequisite in course.Prerequisites)
        {
            if (!academy.CompletedCourses.Contains(prerequisite))
                return (false, "missing_prerequisite");
        }

        if (
            !string.IsNullOrEmpty(course.ChoiceGroupId)
            && course.Year == academy.CurrentYear
            && course.Semester == academy.CurrentSemester
        )
        {
            var groupedCompleted = AcademyCourseCatalog.All.Any(other =>
                other.ChoiceGroupId == course.ChoiceGroupId
                && academy.CompletedCourses.Contains(other.Id)
            );
            var groupedEnrolled = AcademyCourseCatalog.All.Any(other =>
                other.ChoiceGroupId == course.ChoiceGroupId
                && academy.EnrolledCourses.Contains(other.Id)
            );
            if (groupedCompleted || groupedEnrolled)
                return (false, "choice_group_taken");
        }

        return (true, "");
    }

    private bool CanAdvanceSemester(AcademyProgress academy)
    {
        var requiredCourses = AcademyCourseCatalog
            .ForSemester(academy.CurrentYear, academy.CurrentSemester)
            .Where(course => course.IsRequired);

        if (requiredCourses.Any(course => !academy.CompletedCourses.Contains(course.Id)))
            return false;

        return academy.RemainingEnrollments == 0 || !GetCandidateCourses(academy).Any(course =>
        {
            var validation = ValidateCourseAvailable(course, academy);
            return validation.available;
        });
    }

    private void GrantCourseCards(CourseId courseId)
    {
        if (_grantCardFunc == null)
            return;

        foreach (var cardId in GetCourseCardRewards(courseId))
        {
            _grantCardFunc((string)cardId, "common");
        }
    }

    private static IEnumerable<CardId> GetCourseCardRewards(CourseId courseId)
    {
        if (courseId == CourseIds.IntroductionToMagic101)
            return [CardIds.Puff, CardIds.ManaBolt];
        if (courseId == CourseIds.SummoningBasics)
            return [CardIds.FireWisp];
        if (courseId == CourseIds.PracticalSpellcraft)
            return [CardIds.Charge];
        if (courseId == CourseIds.IntroToFire)
            return [CardIds.FireWisp, CardIds.Fireball];
        if (courseId == CourseIds.IntroToWater)
            return [CardIds.WaterWisp, CardIds.WaterJet];
        if (courseId == CourseIds.IntroToEarth)
            return [CardIds.EarthWisp, CardIds.Fortify];
        if (courseId == CourseIds.IntroToAir)
            return [CardIds.WindWisp, CardIds.TailWind];

        return [];
    }
}
