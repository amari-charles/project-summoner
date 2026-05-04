namespace Fateforged.Tests.Data;

using System.Linq;
using Fateforged.Data.Academy;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AcademyCourseCatalogTest
{
    [TestCase]
    public void Semester1_IncludesRequiredMagic101()
    {
        var semester1 = AcademyCourseCatalog.ForSemester(1, 1);
        var magic101 = semester1.FirstOrDefault(course =>
            course.Id == CourseIds.IntroductionToMagic101
        );

        AssertThat(magic101).IsNotNull();
        AssertThat(magic101!.IsRequired).IsTrue();
        AssertThat(magic101.Activities).HasSize(3);
        AssertThat(magic101.Activities.Any(activity => activity.IsOfficialAssessment)).IsTrue();
        AssertThat(magic101.RewardPreviews).HasSize(2);
        AssertThat(magic101.RewardPreviews.Any(reward => reward.CardRole == "summon")).IsTrue();
        AssertThat(magic101.RewardPreviews.Any(reward => reward.CardRole == "spell")).IsTrue();
    }

    [TestCase]
    public void Semester1_HasFoundationChoiceBetweenSummonAndSpell()
    {
        var foundationChoices = AcademyCourseCatalog
            .ForSemester(1, 1)
            .Where(course => course.ChoiceGroupId == "year_1_semester_1_foundation")
            .ToArray();

        AssertThat(foundationChoices).HasSize(2);
        AssertThat(foundationChoices.Select(course => course.Id))
            .Contains(CourseIds.SummoningBasics);
        AssertThat(foundationChoices.Select(course => course.Id))
            .Contains(CourseIds.PracticalSpellcraft);
    }

    [TestCase]
    public void Semester1_ElementIntrosGrantSummonAndSpell()
    {
        var elementCourses = AcademyCourseCatalog
            .ForSemester(1, 1)
            .Where(course => course.ChoiceGroupId == "year_1_semester_1_element")
            .ToArray();

        AssertThat(elementCourses).HasSize(4);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToFire);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToWater);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToEarth);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToAir);

        foreach (var course in elementCourses)
        {
            AssertThat(course.RewardPreviews.Any(reward => reward.CardRole == "summon")).IsTrue();
            AssertThat(course.RewardPreviews.Any(reward => reward.CardRole == "spell")).IsTrue();
        }
    }

    [TestCase]
    public void Semester2_IncludesAcceptedFirstPassCourses()
    {
        var semester2Ids = AcademyCourseCatalog
            .ForSemester(1, 2)
            .Select(course => course.Id)
            .ToArray();

        AssertThat(semester2Ids).Contains(CourseIds.FoundationsOfMagicII);
        AssertThat(semester2Ids).Contains(CourseIds.IntroductionToEmpowerment);
        AssertThat(semester2Ids).Contains(CourseIds.IntroductionToManaChanneling);
        AssertThat(semester2Ids).Contains(CourseIds.FirePracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.WaterPracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.EarthPracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.AirPracticumI);
    }

    [TestCase]
    public void Year1Activities_HaveGentleBattleTuning()
    {
        var magic101 = AcademyCourseCatalog
            .ForSemester(1, 1)
            .First(course => course.Id == CourseIds.IntroductionToMagic101);
        var practice = magic101.Activities.First(activity =>
            activity.Type == AcademyCourseActivityType.PracticeBattle
        );
        var assessment = magic101.Activities.First(activity =>
            activity.Type == AcademyCourseActivityType.AssessmentBattle
        );

        AssertThat(practice.BattleConfig).IsNotNull();
        AssertThat(practice.BattleConfig!.AiType).IsEqual("passive");
        AssertThat(practice.BattleConfig.EnemyHp).IsLessEqual(20f);
        AssertThat(practice.BattleConfig.EnemyDeck).HasSize(1);

        AssertThat(assessment.BattleConfig).IsNotNull();
        AssertThat(assessment.BattleConfig!.AiType).IsEqual("simple");
        AssertThat(assessment.BattleConfig.AiDifficulty).IsEqual(0);
        AssertThat(assessment.BattleConfig.AiPlayIntervalMin).IsGreaterEqual(8.0f);
    }

    [TestCase]
    public void Semester2Activities_StepUpWithoutUsingNormalAi()
    {
        var foundations2 = AcademyCourseCatalog
            .ForSemester(1, 2)
            .First(course => course.Id == CourseIds.FoundationsOfMagicII);
        var assessment = foundations2.Activities.First(activity =>
            activity.Type == AcademyCourseActivityType.AssessmentBattle
        );

        AssertThat(assessment.BattleConfig).IsNotNull();
        AssertThat(assessment.BattleConfig!.AiType).IsEqual("simple");
        AssertThat(assessment.BattleConfig.AiDifficulty).IsLessEqual(1);
        AssertThat(assessment.BattleConfig.EnemyDeck).HasSize(2);
        AssertThat(assessment.BattleConfig.AiPlayIntervalMin).IsGreaterEqual(6.0f);
    }
}
