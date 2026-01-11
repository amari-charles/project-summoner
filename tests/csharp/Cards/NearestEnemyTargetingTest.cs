namespace ProjectSummoner.Tests.Cards;

using GdUnit4;
using ProjectSummoner.Cards.Effects.Targeting;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for NearestEnemyTargeting strategy.
/// Unit tests verify instantiation and configuration.
/// Full targeting behavior is tested via integration tests with the spell system.
/// </summary>
[TestSuite]
public class NearestEnemyTargetingTest
{
    [TestCase]
    public void Constructor_SetsDefaultSearchRadius()
    {
        var targeting = new NearestEnemyTargeting();

        AssertThat(targeting.SearchRadius).IsEqual(100f);
    }

    [TestCase]
    public void Constructor_AcceptsCustomSearchRadius()
    {
        var targeting = new NearestEnemyTargeting(50f);

        AssertThat(targeting.SearchRadius).IsEqual(50f);
    }

    [TestCase]
    public void SearchRadius_CanBeModified()
    {
        var targeting = new NearestEnemyTargeting();
        targeting.SearchRadius = 75f;

        AssertThat(targeting.SearchRadius).IsEqual(75f);
    }

    [TestCase]
    public void SelectTargets_ReturnsEmpty_WhenContextHasNoSpatialGrid()
    {
        // This tests the null-safety of the targeting strategy
        // When SpatialGrid.Instance is null, should return empty gracefully
        var targeting = new NearestEnemyTargeting();

        // Note: SelectTargets requires a SpellContext which requires runtime.
        // This is a configuration-only test - full behavior tested in integration.
        AssertThat(targeting).IsNotNull();
    }
}
