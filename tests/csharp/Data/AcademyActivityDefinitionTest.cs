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
}
