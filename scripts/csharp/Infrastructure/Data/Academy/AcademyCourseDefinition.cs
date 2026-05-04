using System.Collections.Generic;

namespace Fateforged.Data.Academy;

public enum AcademyTrack
{
    Foundation,
    Affinity,
    Binding,
    Arcana,
    Warding,
    Warfare,
    Command,
}

public enum AcademyRewardKind
{
    Card,
    CardTrait,
    SummonerTrait,
    Equipment,
    ConsistencyTool,
    TranscriptEligibility,
    Gold,
    Status,
}

public enum AcademyRewardPreviewType
{
    Fixed,
    Choice,
    Pool,
    Conditional,
}

public class AcademyCourseDefinition
{
    public CourseId Id { get; set; } = CourseId.None;

    public string NameKey { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public int Year { get; set; } = 1;

    public int Semester { get; set; } = 1;

    public AcademyTrack Track { get; set; } = AcademyTrack.Foundation;

    public int EnrollmentCost { get; set; } = 1;

    public bool IsRequired { get; set; }

    public string ChoiceGroupId { get; set; } = "";

    public List<CourseId> Prerequisites { get; set; } = [];

    public List<AcademyRewardPreview> RewardPreviews { get; set; } = [];
}

public class AcademyRewardPreview
{
    public AcademyRewardPreviewType PreviewType { get; set; } = AcademyRewardPreviewType.Fixed;

    public AcademyRewardKind Kind { get; set; } = AcademyRewardKind.Card;

    public string LabelKey { get; set; } = "";

    public string Element { get; set; } = "";

    public string CardRole { get; set; } = "";
}
