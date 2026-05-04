using System.Collections.Generic;
using System.Linq;

namespace Fateforged.Data.Academy;

/// <summary>
/// Static first-pass academy course catalog.
/// </summary>
public static class AcademyCourseCatalog
{
    private const string FoundationChoiceGroup = "year_1_semester_1_foundation";
    private const string ElementChoiceGroup = "year_1_semester_1_element";

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
            RewardPreviews =
            [
                CardReward($"academy.reward.{element}_summon", element, "summon"),
                CardReward($"academy.reward.{element}_spell", element, "spell"),
            ],
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
