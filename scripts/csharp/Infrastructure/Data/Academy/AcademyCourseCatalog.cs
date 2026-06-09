using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;

namespace Fateforged.Data.Academy;

/// <summary>
/// Static first-pass academy course catalog.
/// </summary>
public static class AcademyCourseCatalog
{
    private const string FoundationChoiceGroup = "year_1_semester_1_foundation";
    private const string ElementChoiceGroup = "year_1_semester_1_element";

    private enum AcademyBattleBand
    {
        Onboarding,
        Early,
    }

    public static IReadOnlyList<AcademyCourseDefinition> All { get; } =
    [
        new()
        {
            Id = CourseIds.IntroductionToMagic101,
            NameKey = "academy.course.introduction_to_magic_101.name",
            DescriptionKey = "academy.course.introduction_to_magic_101.description",
            Year = 1,
            Semester = 1,
            Track = AcademyTrack.Foundation,
            IsRequired = true,
            Activities = Magic101Activities(),
            Rewards = [],
        },
        new()
        {
            Id = CourseIds.SummoningBasics,
            NameKey = "academy.course.summoning_basics.name",
            DescriptionKey = "academy.course.summoning_basics.description",
            Year = 1,
            Semester = 1,
            Track = AcademyTrack.Binding,
            ChoiceGroupId = FoundationChoiceGroup,
            Activities = StandardActivities("summoning_basics"),
            Rewards =
            [
                CardReward(
                    "academy.reward.basic_summon",
                    "neutral",
                    "summon",
                    CardIds.FireWisp
                ),
            ],
        },
        new()
        {
            Id = CourseIds.PracticalSpellcraft,
            NameKey = "academy.course.practical_spellcraft.name",
            DescriptionKey = "academy.course.practical_spellcraft.description",
            Year = 1,
            Semester = 1,
            Track = AcademyTrack.Arcana,
            ChoiceGroupId = FoundationChoiceGroup,
            Activities = StandardActivities("practical_spellcraft"),
            Rewards =
            [
                CardReward("academy.reward.basic_spell", "neutral", "spell", CardIds.Charge),
            ],
        },
        IntroElement(CourseIds.IntroToFire, "fire"),
        IntroElement(CourseIds.IntroToWater, "water"),
        IntroElement(CourseIds.IntroToEarth, "earth"),
        IntroElement(CourseIds.IntroToAir, "air"),
        new()
        {
            Id = CourseIds.FoundationsOfMagicII,
            NameKey = "academy.course.foundations_of_magic_ii.name",
            DescriptionKey = "academy.course.foundations_of_magic_ii.description",
            Year = 1,
            Semester = 2,
            Track = AcademyTrack.Foundation,
            IsRequired = true,
            Prerequisites = [CourseIds.IntroductionToMagic101],
            Activities = StandardActivities("foundations_magic_ii", AcademyBattleBand.Early),
            Rewards = [CardReward("academy.reward.foundation_choice", "neutral", "mixed")],
        },
        new()
        {
            Id = CourseIds.IntroductionToEmpowerment,
            NameKey = "academy.course.introduction_to_empowerment.name",
            DescriptionKey = "academy.course.introduction_to_empowerment.description",
            Year = 1,
            Semester = 2,
            Track = AcademyTrack.Foundation,
            Activities = StandardActivities("empowerment", AcademyBattleBand.Early),
            Rewards =
            [
                new()
                {
                    Kind = AcademyRewardKind.CardTrait,
                    PreviewType = AcademyRewardPreviewType.Pool,
                    LabelKey = "academy.reward.first_empowerment",
                },
            ],
        },
        new()
        {
            Id = CourseIds.IntroductionToManaChanneling,
            NameKey = "academy.course.introduction_to_mana_channeling.name",
            DescriptionKey = "academy.course.introduction_to_mana_channeling.description",
            Year = 1,
            Semester = 2,
            Track = AcademyTrack.Binding,
            Activities = StandardActivities("mana_channeling", AcademyBattleBand.Early),
            Rewards =
            [
                new()
                {
                    Kind = AcademyRewardKind.CardTrait,
                    PreviewType = AcademyRewardPreviewType.Fixed,
                    LabelKey = "academy.reward.summon_quantity_channeling",
                    CardRole = "summon",
                },
            ],
        },
        ElementPracticum(CourseIds.FirePracticumI, CourseIds.IntroToFire, "fire"),
        ElementPracticum(CourseIds.WaterPracticumI, CourseIds.IntroToWater, "water"),
        ElementPracticum(CourseIds.EarthPracticumI, CourseIds.IntroToEarth, "earth"),
        ElementPracticum(CourseIds.AirPracticumI, CourseIds.IntroToAir, "air"),
    ];

    public static IReadOnlyList<AcademyCourseDefinition> ForSemester(int year, int semester) =>
        All.Where(course => course.Year == year && course.Semester == semester).ToArray();

    public static AcademyCourseDefinition? Find(CourseId id) =>
        All.FirstOrDefault(course => course.Id == id);

    private static AcademyCourseDefinition IntroElement(CourseId id, string element) =>
        new()
        {
            Id = id,
            NameKey = $"academy.course.intro_to_{element}.name",
            DescriptionKey = $"academy.course.intro_to_{element}.description",
            Year = 1,
            Semester = 1,
            Track = AcademyTrack.Affinity,
            ChoiceGroupId = ElementChoiceGroup,
            Activities = StandardActivities($"intro_{element}"),
            Rewards =
            [
                CardReward(
                    $"academy.reward.{element}_summon",
                    element,
                    "summon",
                    ElementSummonCard(element)
                ),
                CardReward(
                    $"academy.reward.{element}_spell",
                    element,
                    "spell",
                    ElementSpellCard(element)
                ),
            ],
        };

    private static AcademyCourseDefinition ElementPracticum(
        CourseId id,
        CourseId prerequisite,
        string element
    ) =>
        new()
        {
            Id = id,
            NameKey = $"academy.course.{element}_practicum_i.name",
            DescriptionKey = $"academy.course.{element}_practicum_i.description",
            Year = 1,
            Semester = 2,
            Track = AcademyTrack.Affinity,
            Prerequisites = [prerequisite],
            Activities = StandardActivities($"{element}_practicum_i", AcademyBattleBand.Early),
            Rewards =
            [
                new()
                {
                    Kind = AcademyRewardKind.CardTrait,
                    PreviewType = AcademyRewardPreviewType.Choice,
                    LabelKey = $"academy.reward.{element}_practice_choice",
                    Element = element,
                },
            ],
        };

    private static List<AcademyCourseActivity> StandardActivities(
        string prefix,
        AcademyBattleBand battleBand = AcademyBattleBand.Onboarding
    ) =>
    [
        new()
        {
            Id = $"{prefix}_lesson",
            Type = AcademyCourseActivityType.Lesson,
            LabelKey = "academy.activity.lesson",
            Repeatable = false,
        },
        new()
        {
            Id = $"{prefix}_practice",
            Type = AcademyCourseActivityType.PracticeBattle,
            LabelKey = "academy.activity.practice",
            Repeatable = true,
            BattleConfig = PracticeBattleConfig(battleBand),
        },
        new()
        {
            Id = $"{prefix}_assessment",
            Type = AcademyCourseActivityType.AssessmentBattle,
            LabelKey = "academy.activity.assessment",
            IsOfficialAssessment = true,
            Repeatable = false,
            BattleConfig = AssessmentBattleConfig(battleBand),
        },
    ];

    private static List<AcademyCourseActivity> Magic101Activities() =>
    [
        new()
        {
            Id = "magic_101_summon_practice",
            Type = AcademyCourseActivityType.PracticeBattle,
            LabelKey = "academy.activity.magic_101_summon_practice",
            Repeatable = true,
            BattleConfig = new AcademyBattleConfig
            {
                LoanerPlayerDeck = [new DeckEntry(CardIds.NeutralStarterUnit, 2)],
                EnemyDeck = [],
                EnemyHp = 25f,
                AiType = "none",
                AiDifficulty = 0,
                AiPlayIntervalMin = 4.0f,
                AiPlayIntervalMax = 5.0f,
                EncounterAi = ScriptedEncounter(
                    CapRule("magic_101_summon_practice_target_cap", maxAlive: 2),
                    SpawnEvent(
                        "magic_101_summon_practice_target_01",
                        0.75f,
                        CardIds.TrainingTarget,
                        [new AcademyEncounterPosition(10f, -2f)]
                    ),
                    SpawnEvent(
                        "magic_101_summon_practice_target_02",
                        8.0f,
                        CardIds.TrainingTarget,
                        [new AcademyEncounterPosition(10f, 2f)]
                    )
                ),
            },
        },
        new()
        {
            Id = "magic_101_basic_duel",
            Type = AcademyCourseActivityType.PracticeBattle,
            LabelKey = "academy.activity.magic_101_basic_duel",
            Repeatable = true,
            BattleConfig = new AcademyBattleConfig
            {
                LoanerPlayerDeck = [new DeckEntry(CardIds.NeutralStarterUnit, 3)],
                EnemyDeck = [new DeckEntry(CardIds.WeakEnemyUnit, 2)],
                EnemyHp = 35f,
                AiType = "simple",
                AiDifficulty = 0,
                AiPlayIntervalMin = 5.0f,
                AiPlayIntervalMax = 7.0f,
            },
            Rewards =
            [
                CardReward(
                    "academy.reward.neutral_starter_unit",
                    "neutral",
                    "summon",
                    CardIds.NeutralStarterUnit
                ),
            ],
        },
        new()
        {
            Id = "magic_101_spell_practice",
            Type = AcademyCourseActivityType.PracticeBattle,
            LabelKey = "academy.activity.magic_101_spell_practice",
            Repeatable = true,
            BattleConfig = new AcademyBattleConfig
            {
                LoanerPlayerDeck =
                [
                    new DeckEntry(CardIds.NeutralStarterUnit, 2),
                    new DeckEntry(CardIds.MagicBolt, 2),
                ],
                EnemyDeck = [],
                EnemyHp = 40f,
                AiType = "none",
                AiDifficulty = 0,
                AiPlayIntervalMin = 5.0f,
                AiPlayIntervalMax = 7.0f,
                EncounterAi = ScriptedEncounter(
                    CapRule("magic_101_spell_practice_enemy_cap", maxAlive: 3),
                    SpawnEvent(
                        "magic_101_spell_practice_enemy_01",
                        0.75f,
                        CardIds.WeakEnemyUnit,
                        [new AcademyEncounterPosition(10f, -3f)]
                    ),
                    SpawnEvent(
                        "magic_101_spell_practice_enemy_02",
                        7.0f,
                        CardIds.WeakEnemyUnit,
                        [new AcademyEncounterPosition(10f, 0f)]
                    ),
                    SpawnEvent(
                        "magic_101_spell_practice_enemy_03",
                        14.0f,
                        CardIds.WeakEnemyUnit,
                        [new AcademyEncounterPosition(10f, 3f)]
                    )
                ),
            },
            Rewards =
            [
                CardReward("academy.reward.magic_bolt", "neutral", "spell", CardIds.MagicBolt),
            ],
        },
        new()
        {
            Id = "magic_101_assessment",
            Type = AcademyCourseActivityType.AssessmentBattle,
            LabelKey = "academy.activity.magic_101_assessment",
            IsOfficialAssessment = true,
            Repeatable = false,
            BattleConfig = new AcademyBattleConfig
            {
                LoanerPlayerDeck =
                [
                    new DeckEntry(CardIds.NeutralStarterUnit, 3),
                    new DeckEntry(CardIds.MagicBolt, 2),
                ],
                EnemyDeck =
                [
                    new DeckEntry(CardIds.WeakEnemyUnit, 3),
                ],
                EnemyHp = 50f,
                AiType = "simple",
                AiDifficulty = 0,
                AiPlayIntervalMin = 5.0f,
                AiPlayIntervalMax = 6.5f,
            },
        },
    ];

    private static AcademyEncounterAiConfig ScriptedEncounter(params AcademyEncounterRule[] rules) =>
        new()
        {
            Preset = "scripted_encounter",
            UseTrainerAi = false,
            Rules = [.. rules],
        };

    private static AcademyEncounterRule CapRule(string id, int maxAlive) =>
        new()
        {
            Id = id,
            Kind = "cap",
            MaxAlive = maxAlive,
        };

    private static AcademyEncounterRule SpawnEvent(
        string id,
        float startTime,
        CardId cardId,
        List<AcademyEncounterPosition> positions
    ) =>
        new()
        {
            Id = id,
            Kind = "event",
            StartTime = startTime,
            Actions =
            [
                new AcademyEncounterAction
                {
                    Kind = "spawn_units",
                    Source = "encounter",
                    CardId = cardId,
                    Positions = positions,
                    Placement = "neutral",
                    ActivateImmediately = true,
                },
            ],
        };

    private static AcademyBattleConfig PracticeBattleConfig(AcademyBattleBand battleBand) =>
        battleBand switch
        {
            AcademyBattleBand.Early => new AcademyBattleConfig
            {
                EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1), new DeckEntry(CardIds.Puff, 1)],
                EnemyHp = 45f,
                AiType = "simple",
                AiDifficulty = 1,
                AiPlayIntervalMin = 7.0f,
                AiPlayIntervalMax = 10.0f,
            },
            _ => new AcademyBattleConfig
            {
                EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1)],
                EnemyHp = 20f,
                AiType = "passive",
                AiDifficulty = 0,
                AiPlayIntervalMin = 999f,
                AiPlayIntervalMax = 999f,
            },
        };

    private static AcademyBattleConfig AssessmentBattleConfig(AcademyBattleBand battleBand) =>
        battleBand switch
        {
            AcademyBattleBand.Early => new AcademyBattleConfig
            {
                EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1), new DeckEntry(CardIds.Puff, 1)],
                EnemyHp = 55f,
                AiType = "simple",
                AiDifficulty = 1,
                AiPlayIntervalMin = 6.0f,
                AiPlayIntervalMax = 9.0f,
            },
            _ => new AcademyBattleConfig
            {
                EnemyDeck = [new DeckEntry(CardIds.FireWisp, 1)],
                EnemyHp = 35f,
                AiType = "simple",
                AiDifficulty = 0,
                AiPlayIntervalMin = 8.0f,
                AiPlayIntervalMax = 11.0f,
            },
        };

    private static AcademyCourseReward CardReward(
        string labelKey,
        string element,
        string role,
        CardId cardId = default
    ) =>
        new()
        {
            Kind = AcademyRewardKind.Card,
            PreviewType = AcademyRewardPreviewType.Fixed,
            LabelKey = labelKey,
            Element = element,
            CardRole = role,
            CardId = cardId,
        };

    private static CardId ElementSummonCard(string element) =>
        element switch
        {
            "fire" => CardIds.FireWisp,
            "water" => CardIds.WaterWisp,
            "earth" => CardIds.EarthWisp,
            "air" => CardIds.WindWisp,
            _ => CardId.None,
        };

    private static CardId ElementSpellCard(string element) =>
        element switch
        {
            "fire" => CardIds.Fireball,
            "water" => CardIds.WaterJet,
            "earth" => CardIds.Fortify,
            "air" => CardIds.TailWind,
            _ => CardId.None,
        };
}
