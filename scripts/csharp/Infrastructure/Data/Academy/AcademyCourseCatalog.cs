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
            Activities = StandardActivities("magic_101"),
            RewardPreviews =
            [
                CardReward("academy.reward.neutral_basic_summon", "neutral", "summon"),
                CardReward("academy.reward.neutral_basic_spell", "neutral", "spell"),
            ],
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
            RewardPreviews = [CardReward("academy.reward.basic_summon", "neutral", "summon")],
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
            RewardPreviews = [CardReward("academy.reward.basic_spell", "neutral", "spell")],
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
            RewardPreviews = [CardReward("academy.reward.foundation_choice", "neutral", "mixed")],
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
            RewardPreviews =
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
            RewardPreviews =
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
            RewardPreviews =
            [
                CardReward($"academy.reward.{element}_summon", element, "summon"),
                CardReward($"academy.reward.{element}_spell", element, "spell"),
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
            RewardPreviews =
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

    private static AcademyRewardPreview CardReward(string labelKey, string element, string role) =>
        new()
        {
            Kind = AcademyRewardKind.Card,
            PreviewType = AcademyRewardPreviewType.Fixed,
            LabelKey = labelKey,
            Element = element,
            CardRole = role,
        };
}
