using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Events;

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

public enum AcademyCourseActivityType
{
    Lesson,
    PracticeBattle,
    AssessmentBattle,
    RewardChoice,
    Lab,
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

    public List<AcademyCourseActivity> Activities { get; set; } = [];

    public List<AcademyCourseReward> Rewards { get; set; } = [];
}

public class AcademyCourseActivity
{
    public string Id { get; set; } = "";

    public AcademyCourseActivityType Type { get; set; } = AcademyCourseActivityType.Lesson;

    public string LabelKey { get; set; } = "";

    public bool IsOfficialAssessment { get; set; }

    public bool Repeatable { get; set; }

    public AcademyBattleConfig? BattleConfig { get; set; }
}

public class AcademyBattleConfig
{
    public List<DeckEntry> EnemyDeck { get; set; } = [];

    public float EnemyHp { get; set; } = 35f;

    public string AiType { get; set; } = "simple";

    public int AiDifficulty { get; set; } = 0;

    public float AiPlayIntervalMin { get; set; } = 7.0f;

    public float AiPlayIntervalMax { get; set; } = 10.0f;
}

public class AcademyCourseReward
{
    public AcademyRewardPreviewType PreviewType { get; set; } = AcademyRewardPreviewType.Fixed;

    public AcademyRewardKind Kind { get; set; } = AcademyRewardKind.Card;

    public string LabelKey { get; set; } = "";

    public string Element { get; set; } = "";

    public string CardRole { get; set; } = "";

    public CardId CardId { get; set; } = CardId.None;

    public string Rarity { get; set; } = "common";
}
