using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Deck;
using Godot;

namespace Fateforged.Meta.Campaign.Handlers;

public class AcademyProgressHandler
{
    private const int DefaultSemesterEnrollments = 3;
    private const string RewardGrantStateGrantable = "grantable";
    private const string RewardGrantStatePreviewOnly = "preview_only";
    private const string RewardGrantStateClaimed = "claimed";

    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummonerFunc;
    private readonly Func<string, string, string>? _grantCardFunc;
    private Godot.Collections.Dictionary _lastCompletionSummary = [];

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

    public Godot.Collections.Dictionary GetActivityLaunchState(string courseId, string activityId)
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null || located.course == null)
            return [];

        var campaignProgress = GetOrCreateProgress();
        return ToActivityLaunchStateDict(located.activity, located.course, campaignProgress.Academy);
    }

    public Godot.Collections.Dictionary ResolveActivityBattleConfig(string courseId, string activityId)
    {
        var located = FindActivity(courseId, activityId);
        if (located.activity == null)
            return [];

        var validation = ValidateDeckForActivity(located.activity);
        if (!validation.IsValid)
            return [];

        var resolvedPlayerDeck = ResolvePlayerDeckForActivity(located.activity);
        return ToBattleConfigDict(located.activity.BattleConfig, resolvedPlayerDeck);
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

    public bool CompleteActivity(string courseId, string activityId, bool succeeded = true)
    {
        ClearLastCompletionSummary();

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

        var activityIndex = course.Activities.FindIndex(activity => activity.Id == activityId);
        if (activityIndex < 0)
            return false;

        var activity = course.Activities[activityIndex];
        var key = (string)course.Id;
        var currentIndex = academy.CourseActivityIndex.GetValueOrDefault(key, 0);

        if (activityIndex > currentIndex)
            return false;

        if (activityIndex < currentIndex)
            return succeeded && activity.Repeatable;

        if (!succeeded)
            return false;

        var grantedRewards = GrantActivityRewards(course, activity, academy);

        if (
            activity.IsOfficialAssessment
            && !academy.OfficialAssessmentsCompleted.Contains(activity.Id)
        )
        {
            academy.OfficialAssessmentsCompleted.Add(activity.Id);
        }

        var nextIndex = currentIndex + 1;

        if (nextIndex >= course.Activities.Count)
        {
            academy.CourseActivityIndex[key] = nextIndex;
            _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
            return CompleteCourseInternal(
                courseId,
                grade: "pass",
                honors: false,
                resetSummary: false,
                existingRewards: grantedRewards,
                completedActivityId: activity.Id
            );
        }

        academy.CourseActivityIndex[key] = nextIndex;
        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        SetLastCompletionSummary(
            course.Id,
            activity.Id,
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
            completedActivityId: ""
        );
    }

    private bool CompleteCourseInternal(
        string courseId,
        string grade,
        bool honors,
        bool resetSummary,
        Godot.Collections.Array<Godot.Collections.Dictionary> existingRewards,
        string completedActivityId
    )
    {
        if (resetSummary)
            ClearLastCompletionSummary();

        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
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
                completedCourse: true,
                grantedRewards: existingRewards
            );
            return true;
        }

        if (!academy.EnrolledCourses.Contains(course.Id))
            return false;

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
        foreach (var reward in GrantCourseRewards(course, academy))
        {
            grantedRewards.Add(reward);
        }

        _profileRepo.UpdateCampaignProgress(summonerId, campaignProgress);
        SetLastCompletionSummary(
            course.Id,
            completedActivityId,
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
        if (!AcademyCourseCatalog.ForSemester(nextPeriod.year, nextPeriod.semester).Any())
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

        AssignRequiredCourses(academy);
    }

    private static int GetDefaultEnrollments(int year, int semester) =>
        year == 1 && semester is 1 or 2 ? DefaultSemesterEnrollments : DefaultSemesterEnrollments;

    private static void AssignRequiredCourses(AcademyProgress academy)
    {
        foreach (
            var course in AcademyCourseCatalog
                .ForSemester(academy.CurrentYear, academy.CurrentSemester)
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

        var rewards = ToCourseRewardPreviewArray(course, academy);

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
        var isLocked = !courseCompleted && (!courseEnrolled || activityIndex > currentIndex);
        var deckValidation = ValidateDeckForActivity(activity);
        var canStart =
            courseEnrolled
            && (isCurrent || (isCompleted && activity.Repeatable))
            && deckValidation.IsValid;

        return new Godot.Collections.Dictionary
        {
            ["id"] = activity.Id,
            ["type"] = activity.Type.ToString(),
            ["label_key"] = activity.LabelKey,
            ["is_official_assessment"] = activity.IsOfficialAssessment,
            ["repeatable"] = activity.Repeatable,
            ["is_completed"] = isCompleted,
            ["is_current"] = isCurrent,
            ["is_locked"] = isLocked,
            ["can_start"] = canStart,
            ["limitations"] = ToLimitationsDict(activity.Limitations),
            ["limitation_summary"] = ToLimitationSummaryArray(activity.Limitations),
            ["deck_validation"] = ToDeckValidationDict(deckValidation, activity.Limitations),
            ["invalid_reasons"] = ToStringArray(deckValidation.InvalidReasons),
            ["battle_config"] = ToBattleConfigDict(activity.BattleConfig),
            ["reward_previews"] = ToActivityRewardPreviewArray(course, activity, academy),
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
        var course = AcademyCourseCatalog.Find(CourseId.FromString(courseId));
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
            ["enemy_side"] = ToEnemySideDict(battleConfig),
            ["card_xp_reward"] = 0,
            ["summoner_xp_reward"] = 0,
        };

        var playerDeck = resolvedPlayerDeck ?? battleConfig.LoanerPlayerDeck;
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

    private static Godot.Collections.Dictionary ToLimitationsDict(
        AcademyActivityLimitations limitations
    ) =>
        new()
        {
            ["has_rules"] = limitations.HasRules,
            ["fixed_class_deck"] = ToDeckEntriesArray(limitations.FixedClassDeck),
            ["additional_loaner_cards"] = ToDeckEntriesArray(limitations.AdditionalLoanerCards),
            ["allowed_card_types"] = ToStringArray(
                limitations.AllowedCardTypes.Select(type => type.ToString())
            ),
            ["allowed_elements"] = ToStringArray(
                limitations.AllowedElements.Select(element => element.ToString())
            ),
            ["min_summons"] = limitations.MinSummons,
            ["min_spells"] = limitations.MinSpells,
            ["max_deck_size"] = limitations.MaxDeckSize,
            ["required_cards"] = ToCardIdArray(limitations.RequiredCards),
            ["banned_cards"] = ToCardIdArray(limitations.BannedCards),
        };

    private static Godot.Collections.Array<string> ToLimitationSummaryArray(
        AcademyActivityLimitations limitations
    )
    {
        var summaries = new Godot.Collections.Array<string>();
        if (!limitations.HasRules)
            return summaries;

        if (limitations.FixedClassDeck.Count > 0)
            summaries.Add("Uses a fixed class deck.");
        if (limitations.AdditionalLoanerCards.Count > 0)
            summaries.Add("Adds temporary class loaner cards.");
        if (limitations.AllowedCardTypes.Count > 0)
            summaries.Add($"Allowed card types: {string.Join(", ", limitations.AllowedCardTypes)}.");
        if (limitations.AllowedElements.Count > 0)
            summaries.Add($"Allowed elements: {string.Join(", ", limitations.AllowedElements)}.");
        if (limitations.MinSummons > 0)
            summaries.Add($"Requires at least {limitations.MinSummons} summon card(s).");
        if (limitations.MinSpells > 0)
            summaries.Add($"Requires at least {limitations.MinSpells} spell card(s).");
        if (limitations.MaxDeckSize > 0)
            summaries.Add($"Deck size cap: {limitations.MaxDeckSize} card(s).");
        if (limitations.RequiredCards.Count > 0)
            summaries.Add("Requires specific teaching cards.");
        if (limitations.BannedCards.Count > 0)
            summaries.Add("Bans specific cards for this activity.");

        return summaries;
    }

    private AcademyDeckValidationResult ValidateDeckForActivity(AcademyCourseActivity activity)
    {
        var limitations = activity.Limitations;
        if (!limitations.HasRules)
            return AcademyDeckValidationResult.Valid("unrestricted", "No activity-specific deck rules.");

        var effectiveDeck = ResolvePlayerDeckForActivity(activity) ?? [];
        var reasons = new List<string>();

        if (
            effectiveDeck.Count == 0
            && limitations.FixedClassDeck.Count == 0
            && limitations.AdditionalLoanerCards.Count > 0
        )
        {
            reasons.Add("Select or edit an active deck before starting this activity.");
        }

        ValidateDeckRules(effectiveDeck, limitations, reasons);

        return reasons.Count == 0
            ? AcademyDeckValidationResult.Valid("valid", "Current deck satisfies the class rules.")
            : new AcademyDeckValidationResult(false, "invalid", "Deck does not satisfy the class rules.", reasons);
    }

    private List<DeckEntry>? ResolvePlayerDeckForActivity(AcademyCourseActivity activity)
    {
        var limitations = activity.Limitations;
        if (limitations.FixedClassDeck.Count > 0)
            return CopyDeckEntries(limitations.FixedClassDeck);

        if (!limitations.HasRules)
            return null;

        var entries = ResolveActiveDeckEntries();
        AppendDeckEntries(entries, limitations.AdditionalLoanerCards);
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

    private static void ValidateDeckRules(
        IReadOnlyList<DeckEntry> deck,
        AcademyActivityLimitations limitations,
        List<string> reasons
    )
    {
        var totalCards = deck.Sum(entry => entry.Count);
        if (limitations.MaxDeckSize > 0 && totalCards > limitations.MaxDeckSize)
        {
            reasons.Add(
                $"Deck has {totalCards} cards; this activity allows at most {limitations.MaxDeckSize}."
            );
        }

        var summonCount = CountCardsByType(deck, CardType.Summon);
        if (limitations.MinSummons > 0 && summonCount < limitations.MinSummons)
        {
            reasons.Add(
                $"Deck needs {limitations.MinSummons} summon card(s); it currently has {summonCount}."
            );
        }

        var spellCount = CountCardsByType(deck, CardType.Spell);
        if (limitations.MinSpells > 0 && spellCount < limitations.MinSpells)
        {
            reasons.Add(
                $"Deck needs {limitations.MinSpells} spell card(s); it currently has {spellCount}."
            );
        }

        var allowedTypes = limitations.AllowedCardTypes.ToHashSet();
        if (allowedTypes.Count > 0)
        {
            foreach (var entry in deck)
            {
                var card = CardCatalog.GetCard(entry.CardId);
                if (card != null && !allowedTypes.Contains(card.Type))
                    reasons.Add($"{card.Name} is not an allowed card type for this activity.");
            }
        }

        var allowedElements = limitations.AllowedElements.ToHashSet();
        if (allowedElements.Count > 0)
        {
            allowedElements.Add(Element.Neutral);
            foreach (var entry in deck)
            {
                var card = CardCatalog.GetCard(entry.CardId);
                if (card != null && !allowedElements.Contains(card.ElementalAffinity))
                    reasons.Add($"{card.Name} is not an allowed element for this activity.");
            }
        }

        var counts = deck.ToDictionary(entry => entry.CardId, entry => entry.Count);
        foreach (var requiredCard in limitations.RequiredCards.Where(cardId => cardId.HasValue))
        {
            if (!counts.ContainsKey(requiredCard))
                reasons.Add($"Deck is missing required card '{requiredCard}'.");
        }

        foreach (var bannedCard in limitations.BannedCards.Where(cardId => cardId.HasValue))
        {
            if (counts.ContainsKey(bannedCard))
                reasons.Add($"Deck contains banned card '{bannedCard}'.");
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

    private static void AppendDeckEntries(List<DeckEntry> destination, IEnumerable<DeckEntry> entries)
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
        AcademyActivityLimitations limitations
    ) =>
        new()
        {
            ["is_valid"] = validation.IsValid,
            ["status"] = validation.Status,
            ["invalid_reasons"] = ToStringArray(validation.InvalidReasons),
            ["message"] = validation.Message,
            ["has_rules"] = limitations.HasRules,
        };

    private sealed record AcademyDeckValidationResult(
        bool IsValid,
        string Status,
        string Message,
        IReadOnlyList<string> InvalidReasons
    )
    {
        public static AcademyDeckValidationResult Valid(string status, string message) =>
            new(true, status, message, []);
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
                ["deferred"] = battleConfig.EnemyDeck.Count == 0 && battleConfig.EncounterAi != null,
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
    ) =>
        new() { ["x"] = position.X, ["z"] = position.Z };

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

    private Godot.Collections.Array<Godot.Collections.Dictionary> GrantCourseRewards(
        AcademyCourseDefinition course,
        AcademyProgress academy
    )
    {
        var grantedRewards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (_grantCardFunc == null)
            return grantedRewards;

        for (var index = 0; index < course.Rewards.Count; index++)
        {
            var reward = course.Rewards[index];
            if (GetRewardGrantState(reward) != RewardGrantStateGrantable)
                continue;

            var claimKey = GetCourseRewardClaimKey(course.Id, index, reward);
            if (academy.CourseRewardsClaimed.Contains(claimKey))
                continue;

            var instanceId = _grantCardFunc((string)reward.CardId, reward.Rarity);
            if (!string.IsNullOrEmpty(instanceId))
            {
                academy.CourseRewardsClaimed.Add(claimKey);
                grantedRewards.Add(
                    ToGrantedRewardDict(reward, instanceId, "course", (string)course.Id)
                );
            }
        }

        return grantedRewards;
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> GrantActivityRewards(
        AcademyCourseDefinition course,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        var grantedRewards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (_grantCardFunc == null)
            return grantedRewards;

        for (var index = 0; index < activity.Rewards.Count; index++)
        {
            var reward = activity.Rewards[index];
            if (GetRewardGrantState(reward) != RewardGrantStateGrantable)
                continue;

            var claimKey = GetActivityRewardClaimKey(course.Id, activity.Id, index, reward);
            if (academy.ActivityRewardsClaimed.Contains(claimKey))
                continue;

            var instanceId = _grantCardFunc((string)reward.CardId, reward.Rarity);
            if (!string.IsNullOrEmpty(instanceId))
            {
                academy.ActivityRewardsClaimed.Add(claimKey);
                grantedRewards.Add(
                    ToGrantedRewardDict(reward, instanceId, "activity", activity.Id)
                );
            }
        }

        return grantedRewards;
    }

    private static string GetRewardGrantState(AcademyCourseReward reward) =>
        reward.Kind == AcademyRewardKind.Card && reward.CardId.HasValue
            ? RewardGrantStateGrantable
            : RewardGrantStatePreviewOnly;

    private static IEnumerable<(AcademyCourseReward reward, string grantState)> GetCourseRewardPreviews(
        AcademyCourseDefinition course,
        AcademyProgress academy
    )
    {
        for (var index = 0; index < course.Rewards.Count; index++)
        {
            var reward = course.Rewards[index];
            var claimKey = GetCourseRewardClaimKey(course.Id, index, reward);
            var grantState = academy.CourseRewardsClaimed.Contains(claimKey)
                ? RewardGrantStateClaimed
                : GetRewardGrantState(reward);
            yield return (reward, grantState);
        }
    }

    private static IEnumerable<(AcademyCourseReward reward, string grantState)> GetActivityRewardPreviews(
        AcademyCourseDefinition course,
        AcademyCourseActivity activity,
        AcademyProgress academy
    )
    {
        for (var index = 0; index < activity.Rewards.Count; index++)
        {
            var reward = activity.Rewards[index];
            var claimKey = GetActivityRewardClaimKey(course.Id, activity.Id, index, reward);
            var grantState = academy.ActivityRewardsClaimed.Contains(claimKey)
                ? RewardGrantStateClaimed
                : GetRewardGrantState(reward);
            yield return (reward, grantState);
        }
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> ToCourseRewardPreviewArray(
        AcademyCourseDefinition course,
        AcademyProgress academy
    )
    {
        var rewards = GetCourseRewardPreviews(course, academy).ToList();
        foreach (var activity in course.Activities)
        {
            rewards.AddRange(GetActivityRewardPreviews(course, activity, academy));
        }

        return ToRewardPreviewArray(rewards);
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary> ToActivityRewardPreviewArray(
        AcademyCourseDefinition course,
        AcademyCourseActivity activity,
        AcademyProgress academy
    ) => ToRewardPreviewArray(GetActivityRewardPreviews(course, activity, academy));

    private static Godot.Collections.Array<Godot.Collections.Dictionary> ToRewardPreviewArray(
        IEnumerable<(AcademyCourseReward reward, string grantState)> rewards
    )
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var (reward, grantState) in rewards)
        {
            result.Add(
                new Godot.Collections.Dictionary
                {
                    ["kind"] = reward.Kind.ToString(),
                    ["preview_type"] = reward.PreviewType.ToString(),
                    ["grant_state"] = grantState,
                    ["is_grantable"] = grantState == RewardGrantStateGrantable,
                    ["label_key"] = reward.LabelKey,
                    ["element"] = reward.Element,
                    ["card_role"] = reward.CardRole,
                    ["card_id"] = (string)reward.CardId,
                }
            );
        }

        return result;
    }

    private static string GetActivityRewardClaimKey(
        CourseId courseId,
        string activityId,
        int rewardIndex,
        AcademyCourseReward reward
    ) => $"{courseId}:{activityId}:{rewardIndex}:{reward.Kind}:{reward.CardId}";

    private static string GetCourseRewardClaimKey(
        CourseId courseId,
        int rewardIndex,
        AcademyCourseReward reward
    ) => $"{courseId}:course:{rewardIndex}:{reward.Kind}:{reward.CardId}";

    private void ClearLastCompletionSummary()
    {
        _lastCompletionSummary = [];
    }

    private void SetLastCompletionSummary(
        CourseId courseId,
        string activityId,
        bool completedCourse,
        Godot.Collections.Array<Godot.Collections.Dictionary> grantedRewards
    )
    {
        _lastCompletionSummary = new Godot.Collections.Dictionary
        {
            ["course_id"] = (string)courseId,
            ["activity_id"] = activityId,
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

    private static Godot.Collections.Dictionary ToGrantedRewardDict(
        AcademyCourseReward reward,
        string instanceId,
        string sourceType,
        string sourceId
    ) =>
        new()
        {
            ["kind"] = reward.Kind.ToString(),
            ["label_key"] = reward.LabelKey,
            ["element"] = reward.Element,
            ["card_role"] = reward.CardRole,
            ["card_id"] = (string)reward.CardId,
            ["rarity"] = reward.Rarity,
            ["instance_id"] = instanceId,
            ["source_type"] = sourceType,
            ["source_id"] = sourceId,
        };
}
