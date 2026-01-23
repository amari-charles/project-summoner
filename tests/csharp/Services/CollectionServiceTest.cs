namespace ProjectSummoner.Tests.Services;

using GdUnit4;
using ProjectSummoner.Services.Collection;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for CollectionService static methods.
/// Instance methods that require Node context are tested via GUT integration tests.
/// </summary>
[TestSuite]
public class CollectionServiceTest
{
    // =============================================================================
    // GetDismantleValue Tests
    // =============================================================================

    [TestCase]
    public void GetDismantleValue_Common_Returns5()
    {
        var value = CollectionService.GetDismantleValue("common");
        AssertThat(value).IsEqual(5);
    }

    [TestCase]
    public void GetDismantleValue_Rare_Returns20()
    {
        var value = CollectionService.GetDismantleValue("rare");
        AssertThat(value).IsEqual(20);
    }

    [TestCase]
    public void GetDismantleValue_Epic_Returns100()
    {
        var value = CollectionService.GetDismantleValue("epic");
        AssertThat(value).IsEqual(100);
    }

    [TestCase]
    public void GetDismantleValue_Legendary_Returns500()
    {
        var value = CollectionService.GetDismantleValue("legendary");
        AssertThat(value).IsEqual(500);
    }

    [TestCase]
    public void GetDismantleValue_UnknownRarity_Returns0()
    {
        var value = CollectionService.GetDismantleValue("mythic");
        AssertThat(value).IsEqual(0);
    }

    [TestCase]
    public void GetDismantleValue_EmptyString_Returns0()
    {
        var value = CollectionService.GetDismantleValue("");
        AssertThat(value).IsEqual(0);
    }
}
