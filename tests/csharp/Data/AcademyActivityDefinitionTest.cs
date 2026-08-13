namespace Fateforged.Tests.Data;

using System;
using System.Linq;
using Fateforged.Data.Academy;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AcademyActivityDefinitionTest
{
    [TestCase]
    public void ACF_23_ActivityDimensionsComposeWithoutCombinedVariants()
    {
        var activity = new AcademyCourseActivity
        {
            Id = "boss_exam",
            ExecutionKind = AcademyActivityExecutionKind.Battle,
            Role = AcademyActivityRole.Assessment,
            EncounterStyle = AcademyEncounterStyle.Boss,
            Loadout = new AcademyActivityLoadoutDefinition
            {
                Mode = AcademyDeckMode.ClassLoadout,
            },
        };

        AssertThat(activity.ExecutionKind).IsEqual(AcademyActivityExecutionKind.Battle);
        AssertThat(activity.Role).IsEqual(AcademyActivityRole.Assessment);
        AssertThat(activity.EncounterStyle).IsEqual(AcademyEncounterStyle.Boss);
        AssertThat(activity.Loadout.Mode).IsEqual(AcademyDeckMode.ClassLoadout);
        AssertThat(Enum.GetNames<AcademyActivityRole>()).Contains("Standard", "Practice", "Assessment");
    }

    [TestCase]
    public void ACF_19_CatalogContainsNoTextOnlyActivityKind()
    {
        AssertThat(
                AcademyCourseCatalog.All.SelectMany(course => course.Activities)
                    .All(activity => activity.ExecutionKind != AcademyActivityExecutionKind.Lab)
            )
            .IsTrue();
    }

    [TestCase]
    public void ActivityGraph_DefaultsToLinearAndSupportsAuthoredBranchRules()
    {
        var course = new AcademyCourseDefinition
        {
            Activities =
            [
                new AcademyCourseActivity { Id = "root", Prerequisites = [] },
                new AcademyCourseActivity { Id = "second" },
                new AcademyCourseActivity
                {
                    Id = "branch",
                    Prerequisites = ["root", "second"],
                    PrerequisiteMode = AcademyActivityPrerequisiteMode.Any,
                },
            ],
        };

        AssertThat(course.GetActivityPrerequisites(0)).IsEmpty();
        AssertThat(course.GetActivityPrerequisites(1)).ContainsExactly("root");
        AssertThat(course.GetActivityPrerequisites(2)).ContainsExactly("root", "second");
        AssertThat(course.Activities[2].PrerequisiteMode)
            .IsEqual(AcademyActivityPrerequisiteMode.Any);
    }
}
