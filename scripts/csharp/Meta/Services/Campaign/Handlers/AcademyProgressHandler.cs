using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Deck;
using Fateforged.Meta.Rewards;
using Godot;
using DeckModel = Fateforged.Domain.Profile.Decks.Deck;

namespace Fateforged.Meta.Campaign.Handlers;

public class AcademyProgressHandler
{
    private const int DefaultSemesterEnrollments = 3;
    private static readonly RewardViewModelFactory RewardViews = new();
    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummonerFunc;
    private readonly UniversalRewardRuntime _universalRewards;
    private readonly IReadOnlyList<AcademyCourseDefinition> _courseCatalog;
    private Godot.Collections.Dictionary _lastCompletionSummary = [];

    public AcademyProgressHandler(
        IProfileRepository profileRepo,
        Func<SummonerId> getActiveSummonerFunc,
        UniversalRewardRuntime? universalRewards = null,
        IReadOnlyList<AcademyCourseDefinition>? courseCatalog = null
    )
    {
        _profileRepo = profileRepo;
        _getActiveSummonerFunc = getActiveSummonerFunc;
        _universalRewards =
            universalRewards
            ?? (
                profileRepo is IRewardProfileStore rewardProfileStore
                    ? UniversalRewardRuntime.Create(rewardProfileStore)
                    : UniversalRewardRuntime.CreateUnavailable()
            );
        _courseCatalog = courseCatalog ?? AcademyCourseCatalog.All;
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
        var course = FindCourse(CourseId.FromString(courseId));
        if (course == null)
            return [];

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        return ToCourseDict(course, academy, course.Year, course.Semester);
    }

    public Godot.Collections.Dictionary GetActivityLaunchState(string courseId, string activityId)
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return [];

        var campaignProgress = GetOrCreateProgress();
        return ToActivityLaunchStateDict(
            located.activity,
            located.course,
            campaignProgress.Academy
        );
    }

    public bool UpdateActivityLoadout(
        string courseId,
        string activityId,
        Godot.Collections.Array<Godot.Collections.Dictionary> slots
    )
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return false;
        if (located.activity.Loadout.Mode != AcademyDeckMode.ClassLoadout)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var selected = new List<CardInstanceId>();
        foreach (var slot in slots)
        {
            var instanceId = CardInstanceId.FromString(
                slot.GetValueOrDefault("card_instance_id", "").AsString()
            );
            if (!instanceId.HasValue || selected.Contains(instanceId))
                return false;
            var card = _profileRepo.GetCard(instanceId);
            if (
                card == null
                || (card.BoundToSummonerId.HasValue && card.BoundToSummonerId != summonerId)
            )
                return false;
            selected.Add(instanceId);
        }

        var progress = GetOrCreateProgress();
        progress.Academy.ActivityLoadouts[ActivityLoadoutKey(located.course.Id, activityId)] =
            new AcademyActivityLoadoutState { SelectedCardInstanceIds = selected };
        _profileRepo.UpdateCampaignProgress(summonerId, progress);
        return true;
    }

    public Godot.Collections.Dictionary FillActivityLoadoutFromDeck(
        string courseId,
        string activityId,
        string sourceDeckId
    )
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return OperationFailure("activity_not_found");
        if (located.activity.Loadout.Mode != AcademyDeckMode.ClassLoadout)
            return OperationFailure("class_loadout_required");

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return OperationFailure("active_summoner_required");

        var sourceDeck = _profileRepo.GetDeck(DeckId.FromString(sourceDeckId));
        if (sourceDeck == null || sourceDeck.SummonerId != summonerId)
            return OperationFailure("source_deck_not_found");

        var progress = GetOrCreateProgress();
        var selected = GetSelectedInstanceIds(located.course.Id, located.activity, progress.Academy)
            .Where(instanceId =>
            {
                var card = _profileRepo.GetCard(instanceId);
                return card != null
                    && (!card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId);
            })
            .ToList();
        var selectedSet = selected.ToHashSet();
        var suppliedCount = located.activity.Loadout.SuppliedCards.Sum(entry => entry.Count);
        var authoredMax = located.activity.Loadout.Rules.MaxDeckSize;
        var maxDeckSize = authoredMax > 0
            ? Math.Min(authoredMax, DeckService.MaxDeckSize)
            : DeckService.MaxDeckSize;
        var openSlots = Math.Max(0, maxDeckSize - suppliedCount - selected.Count);
        var skipped = new Godot.Collections.Array<string>();
        var copied = 0;

        foreach (var instanceId in sourceDeck.CardInstanceIds)
        {
            if (selectedSet.Contains(instanceId))
                continue;

            var card = _profileRepo.GetCard(instanceId);
            if (
                card == null
                || (card.BoundToSummonerId.HasValue && card.BoundToSummonerId != summonerId)
                || !IsCardAllowedByActivityRules(card.CatalogId, located.activity.Loadout.Rules)
                || openSlots <= 0
            )
            {
                skipped.Add(instanceId.Value);
                continue;
            }

            selected.Add(instanceId);
            selectedSet.Add(instanceId);
            copied++;
            openSlots--;
        }

        progress.Academy.ActivityLoadouts[ActivityLoadoutKey(located.course.Id, activityId)] =
            new AcademyActivityLoadoutState { SelectedCardInstanceIds = selected };
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        return new Godot.Collections.Dictionary
        {
            ["success"] = true,
            ["source_deck_id"] = sourceDeck.Id.Value,
            ["copied_count"] = copied,
            ["skipped_card_instance_ids"] = skipped,
            ["selected_card_instance_ids"] = ToCardInstanceIdArray(selected),
        };
    }

    public Godot.Collections.Dictionary SaveActivityLoadoutToDeck(
        string courseId,
        string activityId,
        string targetDeckId,
        string newDeckName
    )
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return OperationFailure("activity_not_found");
        if (located.activity.Loadout.Mode != AcademyDeckMode.ClassLoadout)
            return OperationFailure("class_loadout_required");

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return OperationFailure("active_summoner_required");

        var replacing = !string.IsNullOrWhiteSpace(targetDeckId);
        DeckModel? targetDeck = null;
        if (replacing)
        {
            targetDeck = _profileRepo.GetDeck(DeckId.FromString(targetDeckId));
            if (targetDeck == null || targetDeck.SummonerId != summonerId)
                return OperationFailure("target_deck_not_found");
        }
        else if (string.IsNullOrWhiteSpace(newDeckName))
        {
            return OperationFailure("deck_name_required");
        }

        var progress = GetOrCreateProgress();
        var selected = GetSelectedInstanceIds(located.course.Id, located.activity, progress.Academy)
            .Where(instanceId =>
            {
                var card = _profileRepo.GetCard(instanceId);
                return card != null
                    && (!card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId);
            })
            .ToList();
        var selectedSet = selected.ToHashSet();
        var remaining = _profileRepo
            .ListCards()
            .Where(card =>
                !selectedSet.Contains(card.Id)
                && (!card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId)
            )
            .ToList();
        var omittedSupplied = new Godot.Collections.Array<string>();
        foreach (var supplied in located.activity.Loadout.SuppliedCards)
        {
            for (var i = 0; i < supplied.Count; i++)
            {
                var owned = remaining.FirstOrDefault(card => card.CatalogId == supplied.CardId);
                if (owned == null)
                {
                    omittedSupplied.Add(supplied.CardId.Value);
                    continue;
                }
                selected.Add(owned.Id);
                remaining.Remove(owned);
            }
        }

        if (selected.Count > DeckService.MaxDeckSize)
            return OperationFailure("deck_too_large");

        var id = _profileRepo.UpsertDeck(
            new DeckModel
            {
                Id = targetDeck?.Id ?? DeckId.None,
                ProfileId = _profileRepo.GetCurrentProfileId(),
                SummonerId = summonerId,
                Name = targetDeck?.Name ?? newDeckName.Trim(),
                Slot = targetDeck?.Slot ?? 0,
                IsActive = targetDeck?.IsActive ?? false,
                CardInstanceIds = selected,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            }
        );
        if (!id.HasValue)
            return OperationFailure("save_failed");

        return new Godot.Collections.Dictionary
        {
            ["success"] = true,
            ["deck_id"] = id.Value,
            ["created"] = !replacing,
            ["omitted_supplied_card_ids"] = omittedSupplied,
        };
    }

    public Godot.Collections.Dictionary ResolveActivityBattleConfig(
        string courseId,
        string activityId
    )
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return [];

        var academy = GetOrCreateProgress().Academy;
        var validation = ValidateDeckForActivity(located.course.Id, located.activity, academy);
        if (!validation.IsValid)
            return [];

        var resolvedPlayerDeck = ResolvePlayerDeckForActivity(
            located.course.Id,
            located.activity,
            academy
        );
        return ToBattleConfigDict(located.activity.BattleConfig, resolvedPlayerDeck);
    }

    public bool EnrollCourse(string courseId)
    {
        var course = FindCourse(CourseId.FromString(courseId));
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

    public bool CompleteActivity(
        string courseId,
        string activityId,
        AcademyActivityOutcome outcome = AcademyActivityOutcome.Victory
    )
    {
        ClearLastCompletionSummary();

        var course = FindCourse(CourseId.FromString(courseId));
        if (course == null)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;
        if (!academy.EnrolledCourses.Contains(course.Id))
            return false;

        var activityIndex = course.Activities.FindIndex(activity => activity.Id == activityId);
        if (activityIndex < 0)
            return false;

        var activity = course.Activities[activityIndex];
        var key = (string)course.Id;
        var currentIndex = academy.CourseActivityIndex.GetValueOrDefault(key, 0);

        if (activityIndex > currentIndex)
            return false;

        if (activityIndex < currentIndex)
        {
            if (activity.Role != AcademyActivityRole.Practice)
                return false;
            SetLastCompletionSummary(
                course.Id,
                activity.Id,
                outcome,
                academy.CompletedCourses.Contains(course.Id),
                []
            );
            return true;
        }

        var advances = outcome == AcademyActivityOutcome.Victory
            || activity.Role == AcademyActivityRole.Assessment;
        if (!advances)
        {
            SetLastCompletionSummary(course.Id, activity.Id, outcome, false, []);
            return true;
        }

        if (HasPendingRewardForCourse(course.Id))
            return false;

        var grantedRewards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var hasPending = false;
        if (outcome == AcademyActivityOutcome.Victory)
        {
            if (
                !TryEarnOffers(
                    course,
                    activity.Id,
                    activity.RewardOffers,
                    out grantedRewards,
                    out hasPending
                )
            )
                return false;
        }

        // Reward commits replace the repository's profile snapshot atomically. Refresh
        // progression before applying the activity state change so targeted campaign
        // grants are not overwritten by the pre-claim object graph.
        campaignProgress = GetOrCreateProgress();
        academy = campaignProgress.Academy;

        if (activity.Role == AcademyActivityRole.Assessment)
            academy.AssessmentOutcomes[activity.Id] = outcome;

        academy.ActivityLoadouts.Remove(ActivityLoadoutKey(course.Id, activity.Id));

        var nextIndex = currentIndex + 1;

        if (nextIndex >= course.Activities.Count)
        {
            academy.CourseActivityIndex[key] = nextIndex;
            _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
            if (hasPending)
            {
                SetLastCompletionSummary(
                    course.Id,
                    activity.Id,
                    outcome,
                    completedCourse: false,
                    grantedRewards: grantedRewards
                );
                return true;
            }
            return CompleteCourseInternal(
                courseId,
                grade: outcome == AcademyActivityOutcome.Victory ? "pass" : "fail",
                honors: false,
                resetSummary: false,
                existingRewards: grantedRewards,
                completedActivityId: activity.Id,
                completedActivityOutcome: outcome
            );
        }

        academy.CourseActivityIndex[key] = nextIndex;
        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        SetLastCompletionSummary(
            course.Id,
            activity.Id,
            outcome,
            completedCourse: false,
            grantedRewards: grantedRewards
        );
        return true;
    }

    public bool CompleteCourse(string courseId, string grade = "pass", bool honors = false)
    {
        return CompleteCourseInternal(
            courseId,
            grade,
            honors,
            resetSummary: true,
            existingRewards: [],
            completedActivityId: "",
            completedActivityOutcome: AcademyActivityOutcome.Victory
        );
    }

    private bool CompleteCourseInternal(
        string courseId,
        string grade,
        bool honors,
        bool resetSummary,
        Godot.Collections.Array<Godot.Collections.Dictionary> existingRewards,
        string completedActivityId,
        AcademyActivityOutcome completedActivityOutcome
    )
    {
        if (resetSummary)
            ClearLastCompletionSummary();

        var course = FindCourse(CourseId.FromString(courseId));
        if (course == null)
            return false;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return false;

        var campaignProgress = GetOrCreateProgress();
        var academy = campaignProgress.Academy;

        if (academy.CompletedCourses.Contains(course.Id))
        {
            SetLastCompletionSummary(
                course.Id,
                completedActivityId,
                completedActivityOutcome,
                completedCourse: true,
                grantedRewards: existingRewards
            );
            return true;
        }

        if (!academy.EnrolledCourses.Contains(course.Id))
            return false;

        if (HasPendingRewardForCourse(course.Id))
            return false;

        if (
            !TryEarnOffers(
                course,
                "course_completion",
                course.RewardOffers,
                out var courseRewards,
                out var hasPending
            )
        )
            return false;

        // Course rewards may target this campaign. Continue from the committed
        // profile snapshot rather than the state captured before the claim.
        campaignProgress = GetOrCreateProgress();
        academy = campaignProgress.Academy;

        if (hasPending)
        {
            var pendingGrantedRewards = CopyRewardArray(existingRewards);
            foreach (var reward in courseRewards)
                pendingGrantedRewards.Add(reward);
            SetLastCompletionSummary(
                course.Id,
                completedActivityId,
                completedActivityOutcome,
                completedCourse: false,
                grantedRewards: pendingGrantedRewards
            );
            return true;
        }

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

        var grantedRewards = CopyRewardArray(existingRewards);
        foreach (var reward in courseRewards)
        {
            grantedRewards.Add(reward);
        }

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        SetLastCompletionSummary(
            course.Id,
            completedActivityId,
            completedActivityOutcome,
            completedCourse: true,
            grantedRewards: grantedRewards
        );
        return true;
    }

    public Godot.Collections.Dictionary GetLastCompletionSummary() =>
        (Godot.Collections.Dictionary)_lastCompletionSummary.Duplicate(true);

    public Godot.Collections.Dictionary ConsumeLastCompletionSummary()
    {
        var summary = GetLastCompletionSummary();
        ClearLastCompletionSummary();
        return summary;
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

        var nextPeriod = GetNextSemester(academy.CurrentYear, academy.CurrentSemester);
        if (!ForSemester(nextPeriod.year, nextPeriod.semester).Any())
        {
            GD.PushWarning(
                $"AcademyProgressHandler: Cannot advance to unauthored academy semester year={nextPeriod.year} semester={nextPeriod.semester}"
            );
            return false;
        }

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
        AssignRequiredCourses(academy);

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        return true;
    }

    private static (int year, int semester) GetNextSemester(int year, int semester) =>
        semester == 1 ? (year, 2) : (year + 1, 1);

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

    private void EnsureAcademyInitialized(AcademyProgress academy)
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

        AssignRequiredCourses(academy);
    }

    private static int GetDefaultEnrollments(int year, int semester) =>
        year == 1 && semester is 1 or 2 ? DefaultSemesterEnrollments : DefaultSemesterEnrollments;

    private void AssignRequiredCourses(AcademyProgress academy)
    {
        foreach (
            var course in ForSemester(academy.CurrentYear, academy.CurrentSemester)
                .Where(course => course.IsRequired)
        )
        {
            if (academy.CompletedCourses.Contains(course.Id))
                continue;

            if (
                course.Prerequisites.Any(prerequisite =>
                    !academy.CompletedCourses.Contains(prerequisite)
                )
            )
                continue;

            if (!academy.EnrolledCourses.Contains(course.Id))
            {
                academy.RemainingEnrollments = Math.Max(
                    0,
                    academy.RemainingEnrollments - course.EnrollmentCost
                );
                academy.EnrolledCourses.Add(course.Id);
            }

            academy.CourseActivityIndex.TryAdd((string)course.Id, 0);
        }
    }

    private IEnumerable<AcademyCourseDefinition> GetCandidateCourses(AcademyProgress academy) =>
        GetCandidateCoursesForSemester(academy, academy.CurrentYear, academy.CurrentSemester);

    private IEnumerable<AcademyCourseDefinition> GetCandidateCoursesForSemester(
        AcademyProgress academy,
        int year,
        int semester
    )
    {
        var candidates = ForSemester(year, semester).ToList();

        if (year == 1 && semester == 2)
        {
            foreach (
                var intro in ForSemester(1, 1)
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
            : (available: false, reason: GetSemesterRelation(academy, viewedYear, viewedSemester));

        var rewards = ToUniversalOfferPreviewArray(
            course,
            "course_completion",
            course.RewardOffers
        );
        foreach (var activity in course.Activities)
        {
            foreach (
                var preview in ToUniversalOfferPreviewArray(
                    course,
                    activity.Id,
                    activity.RewardOffers
                )
            )
                rewards.Add(preview);
        }

        var activities = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var activity in course.Activities)
        {
            activities.Add(
                ToActivityDict(activity, course, academy, activityIndex: activities.Count)
            );
        }

        var activityIndex = academy.CourseActivityIndex.GetValueOrDefault((string)course.Id, 0);
        Godot.Collections.Dictionary nextActivity = new();
        if (activityIndex >= 0 && activityIndex < course.Activities.Count)
        {
            var activity = course.Activities[activityIndex];
            nextActivity = ToActivityDict(activity, course, academy, activityIndex);
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)course.Id,
            ["name_key"] = course.NameKey,
            ["description_key"] = course.DescriptionKey,
            ["year"] = course.Year,
            ["semester"] = course.Semester,
            ["track"] = course.Track.ToString(),
            ["track_title_key"] = GetTrackTitleKey(course.Track),
            ["enrollment_cost"] = course.EnrollmentCost,
            ["is_required"] = course.IsRequired,
            ["choice_group_id"] = course.ChoiceGroupId,
            ["group_id"] = GetCourseGroupId(course),
            ["group_title_key"] = GetCourseGroupTitleKey(course),
            ["group_sort_order"] = GetCourseGroupSortOrder(course),
            ["is_available"] = validation.available,
            ["unavailable_reason"] = validation.reason,
            ["is_current_semester"] = isCurrentSemester,
            ["is_enrolled"] = academy.EnrolledCourses.Contains(course.Id),
            ["is_completed"] = academy.CompletedCourses.Contains(course.Id),
            ["activity_index"] = activityIndex,
            ["activities"] = activities,
            ["next_activity"] = nextActivity,
            ["reward_previews"] = rewards,
            ["universal_reward_status"] = _universalRewards.ToStatusDictionary()["status"],
        };
    }

    private static string GetCourseGroupId(AcademyCourseDefinition course)
    {
        if (course.IsRequired)
            return "required";

        if (!string.IsNullOrEmpty(course.ChoiceGroupId))
            return course.ChoiceGroupId;

        return $"track_{GetTrackKey(course.Track)}";
    }

    private static string GetCourseGroupTitleKey(AcademyCourseDefinition course)
    {
        if (course.IsRequired)
            return "academy.hub.group_required";

        return course.ChoiceGroupId switch
        {
            "year_1_semester_1_foundation" => "academy.class_hall.foundation_choice",
            "year_1_semester_1_element" => "academy.class_hall.element_elective",
            "" => $"academy.class_hall.track_{GetTrackKey(course.Track)}",
            _ => "academy.class_hall.choice_group",
        };
    }

    private static int GetCourseGroupSortOrder(AcademyCourseDefinition course)
    {
        if (course.IsRequired)
            return 0;

        return course.ChoiceGroupId switch
        {
            "year_1_semester_1_foundation" => 10,
            "year_1_semester_1_element" => 20,
            "" => 30 + (GetTrackSortOrder(course.Track) * 10),
            _ => 900,
        };
    }

    private static int GetTrackSortOrder(AcademyTrack track) =>
        track switch
        {
            AcademyTrack.Foundation => 0,
            AcademyTrack.Binding => 1,
            AcademyTrack.Arcana => 2,
            AcademyTrack.Affinity => 3,
            AcademyTrack.Warding => 4,
            AcademyTrack.Warfare => 5,
            AcademyTrack.Command => 6,
            _ => 99,
        };

    private static string GetTrackTitleKey(AcademyTrack track) =>
        $"academy.track.{GetTrackKey(track)}";

    private static string GetTrackKey(AcademyTrack track) =>
        track switch
        {
            AcademyTrack.Foundation => "foundation",
            AcademyTrack.Binding => "binding",
            AcademyTrack.Arcana => "arcana",
            AcademyTrack.Affinity => "affinity",
            AcademyTrack.Warding => "warding",
            AcademyTrack.Warfare => "warfare",
            AcademyTrack.Command => "command",
            _ => track.ToString().ToLowerInvariant(),
        };

    private Godot.Collections.Dictionary ToActivityDict(
        AcademyCourseActivity activity,
        AcademyCourseDefinition course,
        AcademyProgress academy,
        int activityIndex
    )
    {
        var currentIndex = academy.CourseActivityIndex.GetValueOrDefault((string)course.Id, 0);
        var courseCompleted = academy.CompletedCourses.Contains(course.Id);
        var courseEnrolled = academy.EnrolledCourses.Contains(course.Id);
        var isCompleted = courseCompleted || activityIndex < currentIndex;
        var isCurrent = courseEnrolled && activityIndex == currentIndex;
        var hasPendingReward = HasPendingRewardForCourse(course.Id);
        var isLocked =
            !courseCompleted
            && (!courseEnrolled || activityIndex > currentIndex || hasPendingReward);
        var deckValidation = ValidateDeckForActivity(course.Id, activity, academy);
        var canStart =
            courseEnrolled
            && (isCurrent || (isCompleted && activity.Role == AcademyActivityRole.Practice))
            && !hasPendingReward
            && deckValidation.IsValid;
        var lifecycleState = isCompleted
            ? AcademyActivityLifecycleState.Completed
            : isLocked
                ? AcademyActivityLifecycleState.Locked
                : isCurrent
                    ? AcademyActivityLifecycleState.Active
                    : AcademyActivityLifecycleState.Available;

        return new Godot.Collections.Dictionary
        {
            ["id"] = activity.Id,
            ["execution_kind"] = activity.ExecutionKind.ToString(),
            ["role"] = activity.Role.ToString(),
            ["encounter_style"] = activity.EncounterStyle.ToString(),
            ["deck_mode"] = activity.Loadout.Mode.ToString(),
            ["lifecycle_state"] = lifecycleState.ToString(),
            ["label_key"] = activity.LabelKey,
            ["prerequisite_mode"] = activity.PrerequisiteMode.ToString(),
            ["prerequisites"] = ToStringArray(course.GetActivityPrerequisites(activityIndex)),
            ["repeatable"] = activity.Role == AcademyActivityRole.Practice,
            ["is_completed"] = isCompleted,
            ["is_current"] = isCurrent,
            ["is_locked"] = isLocked,
            ["can_start"] = canStart,
            ["loadout"] = ToLoadoutDict(course.Id, activity, academy),
            ["deck_validation"] = ToDeckValidationDict(deckValidation, activity.Loadout.Rules),
            ["battle_config"] = ToBattleConfigDict(activity.BattleConfig),
            ["reward_previews"] = ToUniversalOfferPreviewArray(
                course,
                activity.Id,
                activity.RewardOffers
            ),
        };
    }

    private Godot.Collections.Dictionary ToActivityLaunchStateDict(
        AcademyCourseActivity activity,
        AcademyCourseDefinition course,
        AcademyProgress academy
    )
    {
        var activityIndex = course.Activities.FindIndex(candidate => candidate.Id == activity.Id);
        var activityDict = ToActivityDict(activity, course, academy, activityIndex);
        activityDict["selected_deck"] = GetActiveDeckSummary();
        return activityDict;
    }

    private (AcademyCourseDefinition? course, AcademyCourseActivity? activity) FindActivity(
        string courseId,
        string activityId
    )
    {
        var course = FindCourse(CourseId.FromString(courseId));
        if (course == null)
            return (null, null);

        var activity = course.Activities.FirstOrDefault(candidate => candidate.Id == activityId);
        return (course, activity);
    }

    internal static Godot.Collections.Dictionary ToBattleConfigDict(
        AcademyBattleConfig? battleConfig
    ) => ToBattleConfigDict(battleConfig, resolvedPlayerDeck: null);

    private static Godot.Collections.Dictionary ToBattleConfigDict(
        AcademyBattleConfig? battleConfig,
        IReadOnlyList<DeckEntry>? resolvedPlayerDeck
    )
    {
        if (battleConfig == null)
            return new Godot.Collections.Dictionary();

        var dict = new Godot.Collections.Dictionary
        {
            ["biome_id"] = (string)battleConfig.Biome,
            ["enemy_side"] = ToEnemySideDict(battleConfig),
        };

        var playerDeck = resolvedPlayerDeck ?? [];
        if (playerDeck.Count > 0)
        {
            dict["player_side"] = new Godot.Collections.Dictionary
            {
                ["team"] = 0,
                ["source"] = "profile",
                ["summoner"] = new Godot.Collections.Dictionary { ["source"] = "profile" },
                ["deck"] = new Godot.Collections.Dictionary
                {
                    ["source"] = "authored",
                    ["cards"] = ToDeckEntriesArray(playerDeck),
                },
                ["controller"] = new Godot.Collections.Dictionary { ["kind"] = "player" },
            };
        }

        return dict;
    }

    private Godot.Collections.Dictionary ToLoadoutDict(
        CourseId courseId,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        IReadOnlyList<CardInstanceId> selectedIds = activity.Loadout.Mode == AcademyDeckMode.Owned
            ? GetActiveDeckInstanceIds()
            : GetSelectedInstanceIds(courseId, activity, academy);
        var selectedCards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var instanceId in selectedIds)
        {
            var card = _profileRepo.GetCard(instanceId);
            if (card == null)
                continue;
            selectedCards.Add(
                new Godot.Collections.Dictionary
                {
                    ["card_instance_id"] = instanceId.Value,
                    ["card_id"] = card.CatalogId.Value,
                    ["locked"] = false,
                }
            );
        }

        var availableCards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var summonerId = _getActiveSummonerFunc();
        foreach (
            var card in _profileRepo
                .ListCards()
                .Where(card =>
                    !card.BoundToSummonerId.HasValue || card.BoundToSummonerId == summonerId
                )
                .OrderBy(card => card.CatalogId.Value, StringComparer.Ordinal)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
        )
        {
            availableCards.Add(
                new Godot.Collections.Dictionary
                {
                    ["card_instance_id"] = card.Id.Value,
                    ["card_id"] = card.CatalogId.Value,
                    ["selected"] = selectedIds.Contains(card.Id),
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["mode"] = activity.Loadout.Mode.ToString(),
            ["supplied_cards"] = ToDeckEntriesArray(activity.Loadout.SuppliedCards),
            ["selected_cards"] = selectedCards,
            ["available_cards"] = availableCards,
            ["rules"] = ToRulesDict(activity.Loadout.Rules),
        };
    }

    private static Godot.Collections.Dictionary ToRulesDict(AcademyDeckRules rules) =>
        new()
        {
            ["has_rules"] = rules.HasRules,
            ["allowed_card_types"] = ToStringArray(
                rules.AllowedCardTypes.Select(type => type.ToString())
            ),
            ["allowed_elements"] = ToStringArray(
                rules.AllowedElements.Select(element => element.ToString())
            ),
            ["min_summons"] = rules.MinSummons,
            ["min_spells"] = rules.MinSpells,
            ["max_deck_size"] = rules.MaxDeckSize,
            ["required_owned_cards"] = ToCardIdArray(rules.RequiredOwnedCards),
            ["banned_cards"] = ToCardIdArray(rules.BannedCards),
        };

    private AcademyDeckValidationResult ValidateDeckForActivity(
        CourseId courseId,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        var issues = new List<AcademyDeckValidationIssue>();
        List<DeckEntry> effectiveDeck;
        switch (activity.Loadout.Mode)
        {
            case AcademyDeckMode.Fixed:
                effectiveDeck = CopyDeckEntries(activity.Loadout.SuppliedCards);
                if (effectiveDeck.Count == 0)
                    issues.Add(Issue("fixed_deck_empty"));
                break;
            case AcademyDeckMode.ClassLoadout:
                effectiveDeck = CopyDeckEntries(activity.Loadout.SuppliedCards);
                AppendDeckEntries(
                    effectiveDeck,
                    ResolveSelectedActivityEntries(courseId, activity, academy)
                );
                break;
            default:
                effectiveDeck = ResolveActiveDeckEntries();
                if (effectiveDeck.Count == 0)
                    issues.Add(Issue("owned_deck_required"));
                break;
        }

        ValidateDeckRules(effectiveDeck, activity.Loadout.Rules, issues);
        return issues.Count == 0
            ? AcademyDeckValidationResult.Valid()
            : new AcademyDeckValidationResult(false, "invalid", issues);
    }

    private List<DeckEntry> ResolvePlayerDeckForActivity(
        CourseId courseId,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        if (activity.Loadout.Mode == AcademyDeckMode.Owned)
            return ResolveActiveDeckEntries();

        var entries = CopyDeckEntries(activity.Loadout.SuppliedCards);
        if (activity.Loadout.Mode == AcademyDeckMode.ClassLoadout)
            AppendDeckEntries(entries, ResolveSelectedActivityEntries(courseId, activity, academy));
        return entries;
    }

    private List<DeckEntry> ResolveActiveDeckEntries()
    {
        var selectedDeckId = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        if (string.IsNullOrEmpty(selectedDeckId))
            return [];

        var deck = _profileRepo.GetDeck(DeckId.FromString(selectedDeckId));
        if (deck == null)
            return [];

        var entries = new List<DeckEntry>();
        foreach (var cardInstanceId in deck.CardInstanceIds)
        {
            var card = _profileRepo.GetCard(cardInstanceId);
            if (card == null || !card.CatalogId.HasValue)
                continue;

            AppendDeckEntry(entries, card.CatalogId, 1);
        }

        return entries;
    }

    private IReadOnlyList<CardInstanceId> GetActiveDeckInstanceIds()
    {
        var selectedDeckId = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        if (string.IsNullOrEmpty(selectedDeckId))
            return [];

        return _profileRepo.GetDeck(DeckId.FromString(selectedDeckId))?.CardInstanceIds ?? [];
    }

    private List<CardInstanceId> GetSelectedInstanceIds(
        CourseId courseId,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        var key = ActivityLoadoutKey(courseId, activity.Id);
        return academy.ActivityLoadouts.TryGetValue(key, out var state)
            ? state.SelectedCardInstanceIds.ToList()
            : [];
    }

    private List<DeckEntry> ResolveSelectedActivityEntries(
        CourseId courseId,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        var entries = new List<DeckEntry>();
        foreach (var instanceId in GetSelectedInstanceIds(courseId, activity, academy))
        {
            var card = _profileRepo.GetCard(instanceId);
            if (card != null)
                AppendDeckEntry(entries, card.CatalogId, 1);
        }
        return entries;
    }

    private static string ActivityLoadoutKey(CourseId courseId, string activityId) =>
        $"{courseId}:{activityId}";

    private static bool IsCardAllowedByActivityRules(CardId cardId, AcademyDeckRules rules)
    {
        return !IsCardBanned(cardId, rules)
            && IsCardTypeAllowed(cardId, rules)
            && IsCardElementAllowed(cardId, rules);
    }

    private static bool IsCardBanned(CardId cardId, AcademyDeckRules rules) =>
        rules.BannedCards.Contains(cardId);

    private static bool IsCardTypeAllowed(CardId cardId, AcademyDeckRules rules)
    {
        var card = CardCatalog.GetCard(cardId);
        return card != null
            && (rules.AllowedCardTypes.Count == 0 || rules.AllowedCardTypes.Contains(card.Type));
    }

    private static bool IsCardElementAllowed(CardId cardId, AcademyDeckRules rules)
    {
        var card = CardCatalog.GetCard(cardId);
        return card != null
            && (
                rules.AllowedElements.Count == 0
                || card.ElementalAffinity == Element.Neutral
                || rules.AllowedElements.Contains(card.ElementalAffinity)
            );
    }

    private static Godot.Collections.Dictionary OperationFailure(string error) =>
        new() { ["success"] = false, ["error"] = error };

    private static Godot.Collections.Array<string> ToCardInstanceIdArray(
        IEnumerable<CardInstanceId> instanceIds
    )
    {
        var result = new Godot.Collections.Array<string>();
        foreach (var instanceId in instanceIds)
            result.Add(instanceId.Value);
        return result;
    }

    private static void ValidateDeckRules(
        IReadOnlyList<DeckEntry> deck,
        AcademyDeckRules rules,
        List<AcademyDeckValidationIssue> issues
    )
    {
        var totalCards = deck.Sum(entry => entry.Count);
        var maxDeckSize = rules.MaxDeckSize > 0
            ? Math.Min(rules.MaxDeckSize, DeckService.MaxDeckSize)
            : DeckService.MaxDeckSize;
        if (totalCards > maxDeckSize)
        {
            issues.Add(Issue("max_cards", ("current", totalCards), ("count", maxDeckSize)));
        }

        var summonCount = CountCardsByType(deck, CardType.Summon);
        if (rules.MinSummons > 0 && summonCount < rules.MinSummons)
        {
            issues.Add(Issue("min_summons", ("count", rules.MinSummons), ("current", summonCount)));
        }

        var spellCount = CountCardsByType(deck, CardType.Spell);
        if (rules.MinSpells > 0 && spellCount < rules.MinSpells)
        {
            issues.Add(Issue("min_spells", ("count", rules.MinSpells), ("current", spellCount)));
        }

        if (rules.AllowedCardTypes.Count > 0)
        {
            foreach (var entry in deck)
            {
                if (!IsCardTypeAllowed(entry.CardId, rules))
                    issues.Add(Issue("card_type_not_allowed", ("card_id", (string)entry.CardId)));
            }
        }

        if (rules.AllowedElements.Count > 0)
        {
            foreach (var entry in deck)
            {
                if (!IsCardElementAllowed(entry.CardId, rules))
                    issues.Add(Issue("card_element_not_allowed", ("card_id", (string)entry.CardId)));
            }
        }

        var counts = deck.ToDictionary(entry => entry.CardId, entry => entry.Count);
        foreach (var requiredCard in rules.RequiredOwnedCards.Where(cardId => cardId.HasValue))
        {
            if (!counts.ContainsKey(requiredCard))
                issues.Add(Issue("required_card_missing", ("card_id", (string)requiredCard)));
        }

        foreach (var entry in deck)
        {
            if (IsCardBanned(entry.CardId, rules))
                issues.Add(Issue("banned_card", ("card_id", (string)entry.CardId)));
        }
    }

    private static int CountCardsByType(IReadOnlyList<DeckEntry> deck, CardType type) =>
        deck.Sum(entry => CardCatalog.GetCard(entry.CardId)?.Type == type ? entry.Count : 0);

    private static List<DeckEntry> CopyDeckEntries(IEnumerable<DeckEntry> entries)
    {
        var result = new List<DeckEntry>();
        AppendDeckEntries(result, entries);
        return result;
    }

    private static void AppendDeckEntries(
        List<DeckEntry> destination,
        IEnumerable<DeckEntry> entries
    )
    {
        foreach (var entry in entries)
            AppendDeckEntry(destination, entry.CardId, entry.Count);
    }

    private static void AppendDeckEntry(List<DeckEntry> destination, CardId cardId, int count)
    {
        if (!cardId.HasValue || count <= 0)
            return;

        var existingIndex = destination.FindIndex(entry => entry.CardId == cardId);
        if (existingIndex >= 0)
        {
            var existing = destination[existingIndex];
            destination[existingIndex] = new DeckEntry(existing.CardId, existing.Count + count);
            return;
        }

        destination.Add(new DeckEntry(cardId, count));
    }

    private static Godot.Collections.Dictionary ToDeckValidationDict(
        AcademyDeckValidationResult validation,
        AcademyDeckRules rules
    ) =>
        new()
        {
            ["is_valid"] = validation.IsValid,
            ["status"] = validation.Status,
            ["issues"] = ToValidationIssueArray(validation.Issues),
            ["has_rules"] = rules.HasRules,
        };

    private sealed record AcademyDeckValidationResult(
        bool IsValid,
        string Status,
        IReadOnlyList<AcademyDeckValidationIssue> Issues
    )
    {
        public static AcademyDeckValidationResult Valid() => new(true, "valid", []);
    }

    private sealed record AcademyDeckValidationIssue(
        string Code,
        Godot.Collections.Dictionary Arguments
    );

    private static AcademyDeckValidationIssue Issue(
        string code,
        params (string Key, Variant Value)[] arguments
    )
    {
        var values = new Godot.Collections.Dictionary();
        foreach (var (key, value) in arguments)
            values[key] = value;
        return new AcademyDeckValidationIssue(code, values);
    }

    private static Godot.Collections.Array ToValidationIssueArray(
        IReadOnlyList<AcademyDeckValidationIssue> issues
    )
    {
        var result = new Godot.Collections.Array();
        foreach (var issue in issues)
        {
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["code"] = issue.Code,
                    ["arguments"] = issue.Arguments.Duplicate(true),
                }
            );
        }
        return result;
    }

    private Godot.Collections.Dictionary GetActiveDeckSummary()
    {
        var selectedDeckId = _profileRepo.GetProfileMetadata()?.Meta.SelectedDeck ?? "";
        if (string.IsNullOrEmpty(selectedDeckId))
        {
            return new Godot.Collections.Dictionary
            {
                ["id"] = "",
                ["name"] = "",
                ["card_count"] = 0,
            };
        }

        var deck = _profileRepo.GetDeck(DeckId.FromString(selectedDeckId));
        if (deck == null)
        {
            return new Godot.Collections.Dictionary
            {
                ["id"] = selectedDeckId,
                ["name"] = "",
                ["card_count"] = 0,
            };
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = selectedDeckId,
            ["name"] = deck.Name,
            ["card_count"] = deck.CardInstanceIds.Count,
        };
    }

    private static Godot.Collections.Dictionary ToEnemySideDict(AcademyBattleConfig battleConfig)
    {
        var controllerKind = battleConfig.EncounterAi != null ? "encounter_ai" : "trainer_ai";
        var controller = new Godot.Collections.Dictionary
        {
            ["kind"] = controllerKind,
            ["ai_type"] = battleConfig.AiType,
            ["ai_difficulty"] = battleConfig.AiDifficulty,
            ["ai_config"] = new Godot.Collections.Dictionary
            {
                ["play_interval_min"] = battleConfig.AiPlayIntervalMin,
                ["play_interval_max"] = battleConfig.AiPlayIntervalMax,
            },
        };

        if (battleConfig.EncounterAi != null)
            controller["encounter_ai"] = ToEncounterAiDict(battleConfig.EncounterAi);

        return new Godot.Collections.Dictionary
        {
            ["team"] = 1,
            ["source"] = "authored",
            ["summoner"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["id"] = "academy_enemy",
                ["display_name"] = "Academy Opponent",
                ["hp"] = battleConfig.EnemyHp,
                ["max_hp"] = battleConfig.EnemyHp,
                ["mana"] = 100f,
                ["max_mana"] = 100f,
                ["cast_speed"] = 1f,
                ["damage_bonus"] = 0f,
                ["damage_reduction"] = 0f,
                ["soul_strength"] = 0f,
            },
            ["deck"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["deferred"] =
                    battleConfig.EnemyDeck.Count == 0 && battleConfig.EncounterAi != null,
                ["cards"] = ToDeckEntriesArray(battleConfig.EnemyDeck),
            },
            ["controller"] = controller,
        };
    }

    private static Godot.Collections.Dictionary ToEncounterAiDict(AcademyEncounterAiConfig config)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["preset"] = config.Preset,
            ["team"] = config.Team,
            ["rules"] = ToEncounterRuleArray(config.Rules),
        };

        if (config.UseTrainerAi.HasValue)
            dict["use_trainer_ai"] = config.UseTrainerAi.Value;

        return dict;
    }

    private static Godot.Collections.Array ToEncounterRuleArray(
        IEnumerable<AcademyEncounterRule> rules
    )
    {
        var array = new Godot.Collections.Array();
        foreach (var rule in rules)
        {
            var dict = new Godot.Collections.Dictionary
            {
                ["id"] = rule.Id,
                ["kind"] = rule.Kind,
                ["enabled"] = rule.Enabled,
                ["start_time"] = rule.StartTime,
                ["rhythm"] = rule.Rhythm,
                ["placement"] = rule.Placement,
                ["source"] = rule.Source,
                ["actions"] = ToEncounterActionArray(rule.Actions),
            };

            if (rule.EndTime.HasValue)
                dict["end_time"] = rule.EndTime.Value;
            if (rule.IntervalSeconds.HasValue)
                dict["interval_seconds"] = rule.IntervalSeconds.Value;
            if (rule.MaxExecutions.HasValue)
                dict["max_executions"] = rule.MaxExecutions.Value;
            if (rule.MaxAlive.HasValue)
                dict["max_alive"] = rule.MaxAlive.Value;
            if (rule.CardPool.Count > 0)
                dict["card_pool"] = ToCardIdArray(rule.CardPool);
            AddEncounterBehaviorFields(
                dict,
                rule.AiType,
                rule.AiPersonality,
                rule.AiPlayIntervalMin,
                rule.AiPlayIntervalMax
            );

            array.Add(dict);
        }
        return array;
    }

    private static Godot.Collections.Array ToEncounterActionArray(
        IEnumerable<AcademyEncounterAction> actions
    )
    {
        var array = new Godot.Collections.Array();
        foreach (var action in actions)
        {
            var dict = new Godot.Collections.Dictionary
            {
                ["kind"] = action.Kind,
                ["source"] = action.Source,
                ["team"] = action.Team,
                ["placement"] = action.Placement,
                ["activate_immediately"] = action.ActivateImmediately,
                ["allow_when_overwhelmed"] = action.AllowWhenOverwhelmed,
                ["ignore_caps"] = action.IgnoreCaps,
                ["rule_id"] = action.RuleId,
                ["enabled"] = action.Enabled,
            };

            if (action.CardId != CardId.None)
                dict["card_id"] = (string)action.CardId;
            if (action.CardIds.Count > 0)
                dict["card_ids"] = ToCardIdArray(action.CardIds);
            if (action.Position.HasValue)
                dict["position"] = ToEncounterPositionDict(action.Position.Value);
            if (action.Positions.Count > 0)
                dict["positions"] = ToEncounterPositionArray(action.Positions);
            AddEncounterBehaviorFields(
                dict,
                action.AiType,
                action.AiPersonality,
                action.AiPlayIntervalMin,
                action.AiPlayIntervalMax
            );

            array.Add(dict);
        }
        return array;
    }

    private static void AddEncounterBehaviorFields(
        Godot.Collections.Dictionary dict,
        string? aiType,
        string? aiPersonality,
        float? aiPlayIntervalMin,
        float? aiPlayIntervalMax
    )
    {
        if (!string.IsNullOrWhiteSpace(aiType))
            dict["ai_type"] = aiType;
        if (!string.IsNullOrWhiteSpace(aiPersonality))
            dict["ai_personality"] = aiPersonality;
        if (aiPlayIntervalMin.HasValue || aiPlayIntervalMax.HasValue)
        {
            var aiConfig = new Godot.Collections.Dictionary();
            if (aiPlayIntervalMin.HasValue)
                aiConfig["play_interval_min"] = aiPlayIntervalMin.Value;
            if (aiPlayIntervalMax.HasValue)
                aiConfig["play_interval_max"] = aiPlayIntervalMax.Value;
            dict["ai_config"] = aiConfig;
        }
    }

    private static Godot.Collections.Array ToCardIdArray(IEnumerable<CardId> cardIds)
    {
        var array = new Godot.Collections.Array();
        foreach (var cardId in cardIds)
            array.Add((string)cardId);
        return array;
    }

    private static Godot.Collections.Array<string> ToStringArray(IEnumerable<string> values)
    {
        var array = new Godot.Collections.Array<string>();
        foreach (var value in values)
            array.Add(value);
        return array;
    }

    private static Godot.Collections.Dictionary ToEncounterPositionDict(
        AcademyEncounterPosition position
    ) => new() { ["x"] = position.X, ["z"] = position.Z };

    private static Godot.Collections.Array ToEncounterPositionArray(
        IEnumerable<AcademyEncounterPosition> positions
    )
    {
        var array = new Godot.Collections.Array();
        foreach (var position in positions)
            array.Add(ToEncounterPositionDict(position));
        return array;
    }

    internal static Godot.Collections.Array ToDeckEntriesArray(IEnumerable<DeckEntry> entries)
    {
        var deck = new Godot.Collections.Array();
        foreach (var entry in entries)
        {
            deck.Add(
                new Godot.Collections.Dictionary
                {
                    ["catalog_id"] = (string)entry.CardId,
                    ["count"] = entry.Count,
                }
            );
        }

        return deck;
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

        if (!GetCandidateCourses(academy).Any(candidate => candidate.Id == course.Id))
            return (false, GetSemesterRelation(academy, course.Year, course.Semester));

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
            var groupedCompleted = _courseCatalog.Any(other =>
                other.ChoiceGroupId == course.ChoiceGroupId
                && academy.CompletedCourses.Contains(other.Id)
            );
            var groupedEnrolled = _courseCatalog.Any(other =>
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
        if (HasAnyPendingAcademyReward())
            return false;

        var requiredCourses = ForSemester(academy.CurrentYear, academy.CurrentSemester)
            .Where(course => course.IsRequired);

        if (requiredCourses.Any(course => !academy.CompletedCourses.Contains(course.Id)))
            return false;

        return academy.RemainingEnrollments == 0
            || !GetCandidateCourses(academy)
                .Any(course =>
                {
                    var validation = ValidateCourseAvailable(course, academy);
                    return validation.available;
                });
    }

    public Godot.Collections.Dictionary ClaimReward(
        string claimId,
        Godot.Collections.Array<string> selectedOptionIds
    )
    {
        var typedClaimId = new RewardClaimId(claimId);
        var state = _universalRewards.ProfileStore.GetRewardState();
        if (!state.ResolvedOffers.TryGetValue(claimId, out var snapshot))
            return ToClaimResultDict(InvalidClaim($"Reward claim '{claimId}' was not found."));
        if (
            snapshot.Source.SourceType is not "academy_activity" and not "academy_course"
            || snapshot.SummonerId != _getActiveSummonerFunc()
        )
            return ToClaimResultDict(
                InvalidClaim("Reward claim does not belong to this summoner.")
            );

        var result = _universalRewards.Claims.Claim(
            new RewardClaimRequest
            {
                ClaimId = typedClaimId,
                SelectedOptionIds = selectedOptionIds
                    .Select(id => new RewardOptionId(id))
                    .ToImmutableArray(),
            }
        );

        if (result.Status == RewardRuntimeStatus.Ready && result.Receipt != null)
            AppendClaimedRewardsToSummary(result.Receipt, snapshot.Source.SourceType, snapshot.Source.OccurrenceId);

        if (
            result.Status is RewardRuntimeStatus.Ready or RewardRuntimeStatus.AlreadyClaimed
            && !HasPendingRewardForCourse(new CourseId(snapshot.Source.SourceId))
        )
        {
            ResumeCourseAfterReward(snapshot.Source.SourceId);
        }

        return ToClaimResultDict(result);
    }

    private bool TryEarnOffers(
        AcademyCourseDefinition course,
        string occurrenceId,
        ImmutableArray<RewardOfferDefinition> offers,
        out Godot.Collections.Array<Godot.Collections.Dictionary> grantedRewards,
        out bool hasPending
    )
    {
        grantedRewards = [];
        hasPending = false;
        foreach (var offer in offers)
        {
            if (!TryEnsureResolved(course, occurrenceId, offer, earned: true, out var snapshot))
                return false;

            if (snapshot.SelectionMode == RewardSelectionMode.PlayerChoice)
            {
                if (
                    _universalRewards
                        .ProfileStore.GetRewardState()
                        .ClaimReceipts.ContainsKey(snapshot.ClaimId.Value)
                )
                    continue;
                hasPending = true;
                continue;
            }

            var claim = _universalRewards.Claims.Claim(
                new RewardClaimRequest { ClaimId = snapshot.ClaimId }
            );
            if (
                claim.Status
                    is not RewardRuntimeStatus.Ready
                        and not RewardRuntimeStatus.AlreadyClaimed
                || claim.Receipt == null
            )
                return false;

            if (claim.Status == RewardRuntimeStatus.Ready)
            {
                foreach (var grant in claim.Receipt.AppliedGrants)
                {
                    var granted = ToGrantViewDict(RewardViewModelFactory.CreateGrant(grant));
                    granted["claim_id"] = claim.Receipt.ClaimId.Value;
                    granted["source_type"] =
                        occurrenceId == "course_completion" ? "course" : "activity";
                    granted["source_id"] =
                        occurrenceId == "course_completion" ? (string)course.Id : occurrenceId;
                    grantedRewards.Add(granted);
                }
            }
        }
        return true;
    }

    private bool TryEnsureResolved(
        AcademyCourseDefinition course,
        string occurrenceId,
        RewardOfferDefinition offer,
        bool earned,
        out ResolvedRewardOfferSnapshot snapshot
    )
    {
        snapshot = null!;
        var summonerId = _getActiveSummonerFunc();
        var source = new RewardSourceContext
        {
            SourceType =
                occurrenceId == "course_completion" ? "academy_course" : "academy_activity",
            SourceId = (string)course.Id,
            OccurrenceId = occurrenceId,
        };
        var claimId = RewardIdentity.CreateClaimId(summonerId, source, offer.Id);
        var state = _universalRewards.ProfileStore.GetRewardState();
        if (state.ResolvedOffers.TryGetValue(claimId.Value, out var existingSnapshot))
        {
            snapshot = existingSnapshot;
            if (state.ClaimReceipts.ContainsKey(claimId.Value))
                return true;
            if (earned && snapshot.SelectionMode == RewardSelectionMode.PlayerChoice)
            {
                var existingPending = new PendingRewardSelection
                {
                    ClaimId = claimId,
                    ChooseCount = snapshot.ChooseCount,
                };
                return _universalRewards.ProfileStore.TryStoreResolvedOffer(
                    snapshot,
                    existingPending,
                    out _
                );
            }
            return true;
        }

        ulong seed = 0;
        if (
            offer.OptionSource is PoolRewardOptionSourceDefinition
            && !_universalRewards.ProfileStore.TryGetOrCreateRewardSeed(summonerId, out seed, out _)
        )
            return false;

        var result = _universalRewards.Resolver.Resolve(
            offer,
            new RewardResolutionContext
            {
                SummonerId = summonerId,
                SummonerSeed = seed,
                Source = source,
                Catalog = _universalRewards.Catalog,
                OwnedRewardKeys = _universalRewards.ProfileStore.GetOwnedRewardKeys(summonerId),
            }
        );
        if (result.Status != RewardRuntimeStatus.Ready || result.Snapshot == null)
            return false;

        snapshot = result.Snapshot;
        PendingRewardSelection? pending =
            earned && offer.Selection.Mode == RewardSelectionMode.PlayerChoice
                ? new PendingRewardSelection
                {
                    ClaimId = snapshot.ClaimId,
                    ChooseCount = snapshot.ChooseCount,
                }
                : null;
        return _universalRewards.ProfileStore.TryStoreResolvedOffer(snapshot, pending, out _);
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> ToUniversalOfferPreviewArray(
        AcademyCourseDefinition course,
        string occurrenceId,
        ImmutableArray<RewardOfferDefinition> offers
    )
    {
        var previews = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var offer in offers)
        {
            var source = new RewardSourceContext
            {
                SourceType =
                    occurrenceId == "course_completion" ? "academy_course" : "academy_activity",
                SourceId = (string)course.Id,
                OccurrenceId = occurrenceId,
            };
            var claimId = RewardIdentity.CreateClaimId(_getActiveSummonerFunc(), source, offer.Id);
            var state = _universalRewards.ProfileStore.GetRewardState();
            state.ResolvedOffers.TryGetValue(claimId.Value, out var snapshot);
            if (snapshot == null && offer.PreviewPolicy == RewardPreviewPolicy.Exact)
            {
                if (
                    TryEnsureResolved(
                        course,
                        occurrenceId,
                        offer,
                        earned: false,
                        out var resolvedSnapshot
                    )
                )
                    snapshot = resolvedSnapshot;
            }
            state = _universalRewards.ProfileStore.GetRewardState();
            state.PendingSelections.TryGetValue(claimId.Value, out var pending);
            state.ClaimReceipts.TryGetValue(claimId.Value, out var receipt);
            previews.Add(
                ToOfferViewDict(
                    RewardViews.Create(offer, snapshot, pending, receipt),
                    offer.Selection.ShowCount
                )
            );
        }
        return previews;
    }

    private static Godot.Collections.Dictionary ToOfferViewDict(
        RewardOfferViewModel offer,
        int showCount
    )
    {
        var options = new Godot.Collections.Array();
        foreach (var option in offer.Options)
        {
            var grants = new Godot.Collections.Array();
            foreach (var grant in option.Grants)
                grants.Add(ToGrantViewDict(grant));
            options.Add(
                new Godot.Collections.Dictionary
                {
                    ["option_id"] = option.Id.Value,
                    ["label_key"] = option.LabelKey,
                    ["description_key"] = option.DescriptionKey,
                    ["grants"] = grants,
                }
            );
        }
        var dict = new Godot.Collections.Dictionary
        {
            ["offer_id"] = offer.Id.Value,
            ["preview_policy"] = offer.PreviewPolicy.ToString(),
            ["selection_mode"] = offer.SelectionMode.ToString(),
            ["show_count"] = showCount,
            ["choose_count"] = offer.ChooseCount,
            ["category_key"] = offer.CategoryKey,
            ["options"] = options,
            ["status"] = offer.DisplayState.ToString().ToLowerInvariant(),
        };
        if (offer.ClaimId.HasValue)
            dict["claim_id"] = offer.ClaimId.Value.Value;
        if (offer.Options.Length > 0)
            dict["label_key"] = offer.Options[0].LabelKey;
        return dict;
    }

    private static Godot.Collections.Dictionary ToGrantViewDict(RewardGrantViewModel grant)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["kind"] = grant.Kind,
            ["ownership_scope"] = grant.OwnershipScope.ToString(),
            ["target_id"] = grant.TargetId,
            ["id"] = grant.ContentId,
            ["amount"] = grant.Amount,
        };
        if (grant.Kind == "card")
        {
            dict["card_id"] = grant.ContentId;
            dict["rarity"] = grant.Rarity;
        }
        return dict;
    }

    private bool HasAnyPendingAcademyReward()
    {
        var state = _universalRewards.ProfileStore.GetRewardState();
        return state.PendingSelections.Keys.Any(claimId =>
            state.ResolvedOffers.TryGetValue(claimId, out var snapshot)
            && snapshot.Source.SourceType.StartsWith("academy_", StringComparison.Ordinal)
        );
    }

    private bool HasPendingRewardForCourse(CourseId courseId)
    {
        var state = _universalRewards.ProfileStore.GetRewardState();
        return state.PendingSelections.Keys.Any(claimId =>
            state.ResolvedOffers.TryGetValue(claimId, out var snapshot)
            && snapshot.Source.SourceId == (string)courseId
            && snapshot.Source.SourceType.StartsWith("academy_", StringComparison.Ordinal)
        );
    }

    private void ResumeCourseAfterReward(string courseId)
    {
        var course = FindCourse(new CourseId(courseId));
        if (course == null)
            return;
        var progress = GetOrCreateProgress().Academy;
        if (
            progress.EnrolledCourses.Contains(course.Id)
            && progress.CourseActivityIndex.GetValueOrDefault(courseId) >= course.Activities.Count
        )
        {
            var finalActivity = course.Activities[^1];
            var finalOutcome = progress.AssessmentOutcomes.GetValueOrDefault(
                finalActivity.Id,
                AcademyActivityOutcome.Victory
            );
            CompleteCourseInternal(
                courseId,
                finalOutcome == AcademyActivityOutcome.Victory ? "pass" : "fail",
                false,
                resetSummary: false,
                existingRewards: CompletionSummaryRewards(),
                completedActivityId: finalActivity.Id,
                completedActivityOutcome: finalOutcome
            );
        }
    }

    private static RewardClaimResult InvalidClaim(string error) =>
        new() { Status = RewardRuntimeStatus.Invalid, Errors = [error] };

    private void AppendClaimedRewardsToSummary(
        RewardClaimReceipt receipt,
        string sourceType,
        string sourceId
    )
    {
        if (_lastCompletionSummary.Count == 0)
            return;
        var rewards = CompletionSummaryRewards();
        foreach (var grant in receipt.AppliedGrants)
        {
            var view = ToGrantViewDict(RewardViewModelFactory.CreateGrant(grant));
            view["claim_id"] = receipt.ClaimId.Value;
            view["source_type"] = sourceType == "academy_course" ? "course" : "activity";
            view["source_id"] = sourceId;
            rewards.Add(view);
        }
        _lastCompletionSummary["granted_rewards"] = rewards;
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> CompletionSummaryRewards()
    {
        if (
            _lastCompletionSummary.TryGetValue("granted_rewards", out var rewardsValue)
            && rewardsValue.VariantType == Variant.Type.Array
        )
        {
            var rewards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var value in rewardsValue.AsGodotArray())
                rewards.Add(value.AsGodotDictionary());
            return rewards;
        }
        return [];
    }

    private static Godot.Collections.Dictionary ToClaimResultDict(RewardClaimResult result)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["status"] = result.Status.ToString(),
            ["success"] =
                result.Status is RewardRuntimeStatus.Ready or RewardRuntimeStatus.AlreadyClaimed,
            ["errors"] = ToStringArray(result.Errors),
        };
        if (result.Receipt != null)
            dict["receipt"] = ToReceiptDict(result.Receipt);
        return dict;
    }

    private static Godot.Collections.Dictionary ToReceiptDict(RewardClaimReceipt receipt) =>
        new()
        {
            ["claim_id"] = receipt.ClaimId.Value,
            ["option_ids"] = ToStringArray(receipt.ClaimedOptionIds.Select(id => id.Value)),
            ["grants"] = ToGrantViewArray(receipt.AppliedGrants),
        };

    private static Godot.Collections.Array ToGrantViewArray(
        ImmutableArray<RewardGrantDefinition> grants
    )
    {
        var result = new Godot.Collections.Array();
        foreach (var grant in grants)
            result.Add(ToGrantViewDict(RewardViewModelFactory.CreateGrant(grant)));
        return result;
    }

    private void ClearLastCompletionSummary()
    {
        _lastCompletionSummary = [];
    }

    private void SetLastCompletionSummary(
        CourseId courseId,
        string activityId,
        AcademyActivityOutcome outcome,
        bool completedCourse,
        Godot.Collections.Array<Godot.Collections.Dictionary> grantedRewards
    )
    {
        _lastCompletionSummary = new Godot.Collections.Dictionary
        {
            ["course_id"] = (string)courseId,
            ["activity_id"] = activityId,
            ["outcome"] = outcome.ToString(),
            ["completed_course"] = completedCourse,
            ["granted_rewards"] = CopyRewardArray(grantedRewards),
        };
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> CopyRewardArray(
        Godot.Collections.Array<Godot.Collections.Dictionary> rewards
    )
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var reward in rewards)
        {
            result.Add((Godot.Collections.Dictionary)reward.Duplicate(true));
        }

        return result;
    }

    private AcademyCourseDefinition? FindCourse(CourseId courseId) =>
        _courseCatalog.FirstOrDefault(course => course.Id == courseId);

    private IEnumerable<AcademyCourseDefinition> ForSemester(int year, int semester) =>
        _courseCatalog.Where(course => course.Year == year && course.Semester == semester);
}
