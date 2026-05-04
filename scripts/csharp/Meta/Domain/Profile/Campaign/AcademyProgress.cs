using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Academy;

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

    [JsonPropertyName("official_assessments_completed")]
    public List<string> OfficialAssessmentsCompleted { get; set; } = [];

    [JsonPropertyName("transcript")]
    public List<AcademyTranscriptEntry> Transcript { get; set; } = [];

    [JsonPropertyName("honors_eligibility")]
    public Dictionary<string, bool> HonorsEligibility { get; set; } = [];

    [JsonPropertyName("shop_purchases")]
    public Dictionary<string, int> ShopPurchases { get; set; } = [];
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
