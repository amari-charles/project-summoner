namespace Fateforged.Tests.Units;

using GdUnit4;
using Fateforged.Cards;
using Fateforged.Units;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for DamageProfile class.
/// </summary>
[TestSuite]
public class DamageProfileTest
{
    [TestCase]
    public void Default_IsPurePhysical()
    {
        var profile = new DamageProfile();

        AssertThat(profile.PhysicalRatio).IsEqual(1.0f);
        AssertThat(profile.ElementalRatio).IsEqual(0.0f);
        AssertThat(profile.Element).IsNull();
    }

    [TestCase]
    public void Physical_ReturnsPurePhysicalProfile()
    {
        var profile = DamageProfile.Physical;

        AssertThat(profile.PhysicalRatio).IsEqual(1.0f);
        AssertThat(profile.ElementalRatio).IsEqual(0.0f);
        AssertThat(profile.Element).IsNull();
    }

    [TestCase]
    public void PureElemental_ReturnsFullElementalProfile()
    {
        var profile = DamageProfile.PureElemental(Element.Fire);

        AssertThat(profile.PhysicalRatio).IsEqual(0.0f);
        AssertThat(profile.ElementalRatio).IsEqual(1.0f);
        AssertThat(profile.Element).IsEqual(Element.Fire);
    }

    [TestCase]
    public void Mixed_SplitsDamageCorrectly()
    {
        var profile = DamageProfile.Mixed(0.6f, Element.Water);

        AssertThat(profile.PhysicalRatio).IsEqual(0.6f);
        AssertThat(profile.ElementalRatio).IsEqualApprox(0.4f, 0.001f);
        AssertThat(profile.Element).IsEqual(Element.Water);
    }

    [TestCase]
    public void Mixed_FiftyFiftySplit()
    {
        var profile = DamageProfile.Mixed(0.5f, Element.Fire);

        AssertThat(profile.PhysicalRatio).IsEqual(0.5f);
        AssertThat(profile.ElementalRatio).IsEqual(0.5f);
    }

    [TestCase]
    public void IsValid_TrueWhenRatiosSumToOne()
    {
        var physical = DamageProfile.Physical;
        var elemental = DamageProfile.PureElemental(Element.Fire);
        var mixed = DamageProfile.Mixed(0.3f, Element.Earth);

        AssertThat(physical.IsValid()).IsTrue();
        AssertThat(elemental.IsValid()).IsTrue();
        AssertThat(mixed.IsValid()).IsTrue();
    }

    [TestCase]
    public void IsValid_FalseWhenRatiosDoNotSumToOne()
    {
        var invalid = new DamageProfile
        {
            PhysicalRatio = 0.5f,
            ElementalRatio = 0.3f
        };

        AssertThat(invalid.IsValid()).IsFalse();
    }

    [TestCase]
    public void ToString_PhysicalOnly()
    {
        var profile = DamageProfile.Physical;

        AssertThat(profile.ToString()).IsEqual("Physical");
    }

    [TestCase]
    public void ToString_ElementalOnly()
    {
        var profile = DamageProfile.PureElemental(Element.Fire);

        AssertThat(profile.ToString()).IsEqual("Fire");
    }

    [TestCase]
    public void ToString_Mixed()
    {
        var profile = DamageProfile.Mixed(0.5f, Element.Water);

        var str = profile.ToString();
        AssertThat(str).Contains("50%");
        AssertThat(str).Contains("Physical");
        AssertThat(str).Contains("Water");
    }

    [TestCase]
    public void RecordEquality_SameValuesAreEqual()
    {
        var profile1 = DamageProfile.PureElemental(Element.Fire);
        var profile2 = DamageProfile.PureElemental(Element.Fire);

        AssertThat(profile1).IsEqual(profile2);
    }

    [TestCase]
    public void RecordEquality_DifferentValuesAreNotEqual()
    {
        var fire = DamageProfile.PureElemental(Element.Fire);
        var water = DamageProfile.PureElemental(Element.Water);

        AssertThat(fire).IsNotEqual(water);
    }
}
