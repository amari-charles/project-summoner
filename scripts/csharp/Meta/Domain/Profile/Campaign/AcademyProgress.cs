using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Academy;
using Fateforged.Cards;

namespace Fateforged.Domain.Profile.Campaign;

public class AcademyProgress
{
    [JsonPropertyName("current_year")]
    public int CurrentYear { get; set; } = 1;

    [JsonPropertyName("current_semester")]
    public int CurrentSemester { get; set; } = 1;

    [JsonPropertyName("remaining_enrollments")]
    public int RemainingEnrollments { get; set; }

    [JsonPropertyName("completed_courses")]
    public List<CourseId> CompletedCourses { get; set; } = [];

    [JsonPropertyName("enrolled_courses")]
    public List<CourseId> EnrolledCourses { get; set; } = [];

    [JsonPropertyName("course_activity_index")]
    public Dictionary<string, int> CourseActivityIndex { get; set; } = [];

    [JsonPropertyName("assessment_outcomes")]
    public Dictionary<string, AcademyActivityOutcome> AssessmentOutcomes { get; set; } = [];

    [JsonPropertyName("activity_loadouts")]
    public Dictionary<string, AcademyActivityLoadoutState> ActivityLoadouts { get; set; } = [];

    [JsonPropertyName("transcript")]
    public List<AcademyTranscriptEntry> Transcript { get; set; } = [];

    [JsonPropertyName("honors_eligibility")]
    public Dictionary<string, bool> HonorsEligibility { get; set; } = [];

    [JsonPropertyName("shop_purchases")]
    public Dictionary<string, int> ShopPurchases { get; set; } = [];

    [JsonPropertyName("reward_flags")]
    public Dictionary<string, int> RewardFlags { get; set; } = [];
}

public class AcademyActivityLoadoutState
{
    [JsonPropertyName("selected_card_instance_ids")]
    public List<CardInstanceId> SelectedCardInstanceIds { get; set; } = [];
}

public class AcademyTranscriptEntry
{
    [JsonPropertyName("course_id")]
    public CourseId CourseId { get; set; } = CourseId.None;

    [JsonPropertyName("grade")]
    public string Grade { get; set; } = "";

    [JsonPropertyName("honors")]
    public bool Honors { get; set; }

    [JsonPropertyName("semester_key")]
    public string SemesterKey { get; set; } = "";
}
