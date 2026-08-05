using System.Collections.Generic;
using System.Collections.Immutable;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;

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

    public ImmutableArray<RewardOfferDefinition> RewardOffers { get; init; } = [];
}

public class AcademyCourseActivity
{
    public string Id { get; set; } = "";

    public AcademyCourseActivityType Type { get; set; } = AcademyCourseActivityType.Lesson;

    public string LabelKey { get; set; } = "";

    public bool IsOfficialAssessment { get; set; }

    public bool Repeatable { get; set; }

    public AcademyBattleConfig? BattleConfig { get; set; }

    public AcademyActivityLimitations Limitations { get; set; } = new();

    public ImmutableArray<RewardOfferDefinition> RewardOffers { get; init; } = [];
}

public class AcademyActivityLimitations
{
    public List<DeckEntry> FixedClassDeck { get; set; } = [];

    public List<DeckEntry> AdditionalLoanerCards { get; set; } = [];

    public List<CardType> AllowedCardTypes { get; set; } = [];

    public List<Element> AllowedElements { get; set; } = [];

    public int MinSummons { get; set; }

    public int MinSpells { get; set; }

    public int MaxDeckSize { get; set; }

    public List<CardId> RequiredCards { get; set; } = [];

    public List<CardId> BannedCards { get; set; } = [];

    public bool HasRules =>
        FixedClassDeck.Count > 0
        || AdditionalLoanerCards.Count > 0
        || AllowedCardTypes.Count > 0
        || AllowedElements.Count > 0
        || MinSummons > 0
        || MinSpells > 0
        || MaxDeckSize > 0
        || RequiredCards.Count > 0
        || BannedCards.Count > 0;
}

public class AcademyBattleConfig
{
    public List<DeckEntry> LoanerPlayerDeck { get; set; } = [];

    public List<DeckEntry> EnemyDeck { get; set; } = [];

    public float EnemyHp { get; set; } = 35f;

    public string AiType { get; set; } = "simple";

    public int AiDifficulty { get; set; } = 0;

    public float AiPlayIntervalMin { get; set; } = 7.0f;

    public float AiPlayIntervalMax { get; set; } = 10.0f;

    public AcademyEncounterAiConfig? EncounterAi { get; set; }
}

public class AcademyEncounterAiConfig
{
    public string Preset { get; set; } = "default_trainer";

    public int Team { get; set; } = 1;

    public bool? UseTrainerAi { get; set; }

    public List<AcademyEncounterRule> Rules { get; set; } = [];
}

public class AcademyEncounterRule
{
    public string Id { get; set; } = "";

    public string Kind { get; set; } = "event";

    public bool Enabled { get; set; } = true;

    public float StartTime { get; set; }

    public float? EndTime { get; set; }

    public string Rhythm { get; set; } = "steady";

    public float? IntervalSeconds { get; set; }

    public int? MaxExecutions { get; set; }

    public int? MaxAlive { get; set; }

    public string Placement { get; set; } = "neutral";

    public string Source { get; set; } = "encounter";

    public string? AiType { get; set; }

    public string? AiPersonality { get; set; }

    public float? AiPlayIntervalMin { get; set; }

    public float? AiPlayIntervalMax { get; set; }

    public List<CardId> CardPool { get; set; } = [];

    public List<AcademyEncounterAction> Actions { get; set; } = [];
}

public class AcademyEncounterAction
{
    public string Kind { get; set; } = "spawn_units";

    public string Source { get; set; } = "encounter";

    public int Team { get; set; } = 1;

    public CardId CardId { get; set; } = CardId.None;

    public List<CardId> CardIds { get; set; } = [];

    public AcademyEncounterPosition? Position { get; set; }

    public List<AcademyEncounterPosition> Positions { get; set; } = [];

    public string Placement { get; set; } = "neutral";

    public bool ActivateImmediately { get; set; } = true;

    public string? AiType { get; set; }

    public string? AiPersonality { get; set; }

    public float? AiPlayIntervalMin { get; set; }

    public float? AiPlayIntervalMax { get; set; }

    public bool AllowWhenOverwhelmed { get; set; }

    public bool IgnoreCaps { get; set; }

    public string RuleId { get; set; } = "";

    public bool Enabled { get; set; } = true;
}

public readonly record struct AcademyEncounterPosition(float X, float Z);
