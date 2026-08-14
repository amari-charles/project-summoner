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

public enum AcademyActivityExecutionKind
{
    Battle,
    Lab,
}

public enum AcademyActivityRole
{
    Standard,
    Practice,
    Assessment,
}

public enum AcademyEncounterStyle
{
    Standard,
    Boss,
    Challenge,
}

public enum AcademyDeckMode
{
    Fixed,
    Owned,
    ClassLoadout,
}

public enum AcademyActivityLifecycleState
{
    Locked,
    Available,
    Active,
    Completed,
}

public enum AcademyActivityPrerequisiteMode
{
    All,
    Any,
}

public enum AcademyActivityOutcome
{
    Victory,
    Defeat,
    Abandoned,
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

    public IReadOnlyList<string> GetActivityPrerequisites(int activityIndex)
    {
        if (activityIndex < 0 || activityIndex >= Activities.Count)
            return [];

        var authored = Activities[activityIndex].Prerequisites;
        if (authored != null)
            return authored;

        return activityIndex == 0 ? [] : [Activities[activityIndex - 1].Id];
    }
}

public class AcademyCourseActivity
{
    public string Id { get; set; } = "";

    public AcademyActivityExecutionKind ExecutionKind { get; set; } =
        AcademyActivityExecutionKind.Battle;

    public AcademyActivityRole Role { get; set; } = AcademyActivityRole.Standard;

    public AcademyEncounterStyle EncounterStyle { get; set; } = AcademyEncounterStyle.Standard;

    public string LabelKey { get; set; } = "";

    // Null preserves the current authored shorthand: the previous activity is the
    // sole prerequisite. An explicit array enables roots and branching graphs.
    public List<string>? Prerequisites { get; set; }

    public AcademyActivityPrerequisiteMode PrerequisiteMode { get; set; } =
        AcademyActivityPrerequisiteMode.All;

    public AcademyBattleConfig? BattleConfig { get; set; }

    public AcademyActivityLoadoutDefinition Loadout { get; set; } = new();

    public ImmutableArray<RewardOfferDefinition> RewardOffers { get; init; } = [];
}

public class AcademyActivityLoadoutDefinition
{
    public AcademyDeckMode Mode { get; set; } = AcademyDeckMode.Owned;

    public List<DeckEntry> SuppliedCards { get; set; } = [];

    public AcademyDeckRules Rules { get; set; } = new();
}

public class AcademyDeckRules
{

    public List<CardType> AllowedCardTypes { get; set; } = [];

    public List<Element> AllowedElements { get; set; } = [];

    public int MinSummons { get; set; }

    public int MinSpells { get; set; }

    public int MaxDeckSize { get; set; }

    public List<CardId> RequiredOwnedCards { get; set; } = [];

    public List<CardId> BannedCards { get; set; } = [];

    public bool HasRules =>
        AllowedCardTypes.Count > 0
        || AllowedElements.Count > 0
        || MinSummons > 0
        || MinSpells > 0
        || MaxDeckSize > 0
        || RequiredOwnedCards.Count > 0
        || BannedCards.Count > 0;
}

public class AcademyBattleConfig
{
    public BiomeId Biome { get; set; } = BiomeIds.Default;

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
