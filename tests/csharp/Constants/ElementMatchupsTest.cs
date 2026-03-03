namespace Fateforged.Tests.Constants;

using GdUnit4;
using Fateforged.Cards;
using Fateforged.Constants;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for ElementMatchups damage multiplier system.
/// </summary>
[TestSuite]
public class ElementMatchupsTest
{
    // =========================================================================
    // Core Element Cycle Tests: Fire > Wind > Earth > Water > Fire
    // =========================================================================

    [TestCase]
    public void GetMultiplier_FireVsWind_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Fire, Element.Wind);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_FireVsWater_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Fire, Element.Water);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    [TestCase]
    public void GetMultiplier_WindVsEarth_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Wind, Element.Earth);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_WindVsFire_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Wind, Element.Fire);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    [TestCase]
    public void GetMultiplier_EarthVsWater_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Earth, Element.Water);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_EarthVsWind_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Earth, Element.Wind);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    [TestCase]
    public void GetMultiplier_WaterVsFire_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Water, Element.Fire);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_WaterVsEarth_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Water, Element.Earth);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    // =========================================================================
    // Outer Element Tests
    // =========================================================================

    [TestCase]
    public void GetMultiplier_LightningVsWater_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Lightning, Element.Water);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_LightningVsEarth_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Lightning, Element.Earth);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    // =========================================================================
    // Life/Death/Shadow Triangle Tests
    // =========================================================================

    [TestCase]
    public void GetMultiplier_LifeVsDeath_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Life, Element.Death);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_LifeVsShadow_ReturnsDisadvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Life, Element.Shadow);

        AssertThat(multiplier).IsEqual(0.8f);
    }

    [TestCase]
    public void GetMultiplier_DeathVsLife_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Death, Element.Life);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    [TestCase]
    public void GetMultiplier_ShadowVsLife_ReturnsAdvantage()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Shadow, Element.Life);

        AssertThat(multiplier).IsEqual(1.25f);
    }

    // =========================================================================
    // Neutral Element Tests
    // =========================================================================

    [TestCase]
    public void GetMultiplier_NeutralAttacker_ReturnsDefault()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Neutral, Element.Fire);

        AssertThat(multiplier).IsEqual(1.0f);
    }

    [TestCase]
    public void GetMultiplier_NeutralDefender_ReturnsDefault()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Fire, Element.Neutral);

        AssertThat(multiplier).IsEqual(1.0f);
    }

    [TestCase]
    public void GetMultiplier_BothNeutral_ReturnsDefault()
    {
        var multiplier = ElementMatchups.GetMultiplier(Element.Neutral, Element.Neutral);

        AssertThat(multiplier).IsEqual(1.0f);
    }

    // =========================================================================
    // Same Element Tests
    // =========================================================================

    [TestCase]
    public void GetMultiplier_SameElement_ReturnsDefault()
    {
        AssertThat(ElementMatchups.GetMultiplier(Element.Fire, Element.Fire)).IsEqual(1.0f);
        AssertThat(ElementMatchups.GetMultiplier(Element.Water, Element.Water)).IsEqual(1.0f);
        AssertThat(ElementMatchups.GetMultiplier(Element.Wind, Element.Wind)).IsEqual(1.0f);
        AssertThat(ElementMatchups.GetMultiplier(Element.Earth, Element.Earth)).IsEqual(1.0f);
    }

    // =========================================================================
    // Undefined Matchup Tests
    // =========================================================================

    [TestCase]
    public void GetMultiplier_UndefinedMatchup_ReturnsDefault()
    {
        // Fire vs Earth has no defined matchup
        var multiplier = ElementMatchups.GetMultiplier(Element.Fire, Element.Earth);

        AssertThat(multiplier).IsEqual(1.0f);
    }

    [TestCase]
    public void GetMultiplier_PoisonElement_ReturnsDefault()
    {
        // Poison has no matchups defined yet
        AssertThat(ElementMatchups.GetMultiplier(Element.Poison, Element.Fire)).IsEqual(1.0f);
        AssertThat(ElementMatchups.GetMultiplier(Element.Fire, Element.Poison)).IsEqual(1.0f);
    }

    // =========================================================================
    // HasAdvantage Tests
    // =========================================================================

    [TestCase]
    public void HasAdvantage_FireVsWind_ReturnsTrue()
    {
        AssertThat(ElementMatchups.HasAdvantage(Element.Fire, Element.Wind)).IsTrue();
    }

    [TestCase]
    public void HasAdvantage_FireVsWater_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasAdvantage(Element.Fire, Element.Water)).IsFalse();
    }

    [TestCase]
    public void HasAdvantage_FireVsFire_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasAdvantage(Element.Fire, Element.Fire)).IsFalse();
    }

    [TestCase]
    public void HasAdvantage_NeutralVsFire_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasAdvantage(Element.Neutral, Element.Fire)).IsFalse();
    }

    // =========================================================================
    // HasDisadvantage Tests
    // =========================================================================

    [TestCase]
    public void HasDisadvantage_FireVsWater_ReturnsTrue()
    {
        AssertThat(ElementMatchups.HasDisadvantage(Element.Fire, Element.Water)).IsTrue();
    }

    [TestCase]
    public void HasDisadvantage_FireVsWind_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasDisadvantage(Element.Fire, Element.Wind)).IsFalse();
    }

    [TestCase]
    public void HasDisadvantage_FireVsFire_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasDisadvantage(Element.Fire, Element.Fire)).IsFalse();
    }

    [TestCase]
    public void HasDisadvantage_NeutralVsFire_ReturnsFalse()
    {
        AssertThat(ElementMatchups.HasDisadvantage(Element.Neutral, Element.Fire)).IsFalse();
    }

    // =========================================================================
    // Symmetry Tests (Life/Death mutual advantage)
    // =========================================================================

    [TestCase]
    public void GetMultiplier_LifeAndDeath_BothHaveAdvantage()
    {
        // Life vs Death and Death vs Life both have advantage
        AssertThat(ElementMatchups.GetMultiplier(Element.Life, Element.Death)).IsEqual(1.25f);
        AssertThat(ElementMatchups.GetMultiplier(Element.Death, Element.Life)).IsEqual(1.25f);
    }
}
