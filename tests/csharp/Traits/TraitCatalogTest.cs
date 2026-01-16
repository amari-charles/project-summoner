namespace ProjectSummoner.Tests.Traits;

using System.Linq;
using GdUnit4;
using ProjectSummoner.Data.Traits;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for the TraitCatalog static class.
/// These tests run without Godot runtime for speed.
/// </summary>
[TestSuite]
public class TraitCatalogTest
{
    [TestCase]
    public void GetTrait_ReturnsTraitDefinition_WhenTraitExists()
    {
        var trait = TraitCatalog.GetTrait(TraitId.FireAffinity);

        AssertThat(trait).IsNotNull();
        AssertThat(trait!.Id).IsEqual(TraitId.FireAffinity);
        AssertThat(trait.Category).IsEqual("elemental");
        AssertThat(trait.IsInnate).IsTrue();
    }

    [TestCase]
    public void GetTrait_ReturnsNull_WhenTraitDoesNotExist()
    {
        var trait = TraitCatalog.GetTrait("nonexistent_trait");

        AssertThat(trait).IsNull();
    }

    [TestCase]
    public void HasTrait_ReturnsTrue_WhenTraitExists()
    {
        var exists = TraitCatalog.HasTrait(TraitId.WaterAffinity);

        AssertThat(exists).IsTrue();
    }

    [TestCase]
    public void HasTrait_ReturnsFalse_WhenTraitDoesNotExist()
    {
        var exists = TraitCatalog.HasTrait("nonexistent_trait");

        AssertThat(exists).IsFalse();
    }

    [TestCase]
    public void GetAllTraitIds_ReturnsNonEmptyArray()
    {
        var ids = TraitCatalog.GetAllTraitIds();

        AssertThat(ids).IsNotNull();
        AssertThat(ids.Length).IsGreater(0);
    }

    [TestCase]
    public void Count_MatchesGetAllTraits()
    {
        var allTraits = TraitCatalog.GetAllTraits();

        AssertThat(TraitCatalog.Count).IsEqual(allTraits.Length);
    }

    [TestCase]
    public void GetTraitsByCategory_ReturnsElementalTraits()
    {
        var elementalTraits = TraitCatalog.GetTraitsByCategory("elemental");

        AssertThat(elementalTraits).IsNotNull();
        AssertThat(elementalTraits.Length).IsGreater(0);

        foreach (var trait in elementalTraits)
        {
            AssertThat(trait.Category).IsEqual("elemental");
        }
    }

    [TestCase]
    public void GetInnateTraits_ReturnsOnlyInnateTraits()
    {
        var innateTraits = TraitCatalog.GetInnateTraits();

        AssertThat(innateTraits).IsNotNull();
        AssertThat(innateTraits.Length).IsGreater(0);

        foreach (var trait in innateTraits)
        {
            AssertThat(trait.IsInnate).IsTrue();
        }
    }

    [TestCase]
    public void GetAcquirableBoons_ReturnsOnlyNonInnateTraits()
    {
        var boons = TraitCatalog.GetAcquirableBoons();

        AssertThat(boons).IsNotNull();
        AssertThat(boons.Length).IsGreater(0);

        foreach (var boon in boons)
        {
            AssertThat(boon.IsInnate).IsFalse();
        }
    }

    [TestCase]
    public void GetUnitModifiersForTrait_ReturnsModifiers_ForElementalAffinity()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitId.FireAffinity);

        AssertThat(modifiers).IsNotNull();
        AssertThat(modifiers.Count).IsGreater(0);

        // Fire affinity should buff fire units
        var firstMod = modifiers[0];
        AssertThat(firstMod.Conditions.ContainsKey("elemental_affinity")).IsTrue();
        AssertThat(firstMod.Conditions["elemental_affinity"]).IsEqual("fire");
        AssertThat(firstMod.StatMults.ContainsKey("attack_damage")).IsTrue();
    }

    [TestCase]
    public void GetUnitModifiersForTrait_ReturnsEmpty_ForTraitWithoutUnitModifiers()
    {
        // BurningSpirit only has summoner stat modifiers, no unit modifiers
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitId.BurningSpirit);

        AssertThat(modifiers).IsNotNull();
        AssertThat(modifiers.Count).IsEqual(0);
    }

    [TestCase]
    public void GetUnitModifiersForTrait_ReturnsEmpty_ForNonexistentTrait()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait("nonexistent_trait");

        AssertThat(modifiers).IsNotNull();
        AssertThat(modifiers.Count).IsEqual(0);
    }

    [TestCase]
    public void TraitDefinition_HasValidModifiers()
    {
        var trait = TraitCatalog.GetTrait(TraitId.StoneFortitude);

        AssertThat(trait).IsNotNull();
        AssertThat(trait!.Modifiers).IsNotNull();
        AssertThat(trait.Modifiers).IsNotEmpty();

        var modifier = trait.Modifiers[0];
        AssertThat(modifier.Stat).IsEqual("damage_reduction");
        AssertThat(modifier.Type).IsEqual("flat");
        AssertThat(modifier.Value).IsEqual(5.0f);
    }

    [TestCase]
    public void TraitIds_AreAllUnique()
    {
        var ids = TraitCatalog.GetAllTraitIds();
        var uniqueIds = ids.Distinct().ToArray();

        AssertThat(ids.Length).IsEqual(uniqueIds.Length);
    }

    [TestCase]
    public void AllSummonerAffinityTraits_HaveUnitModifiers()
    {
        var affinityTraitIds = new[]
        {
            TraitId.FireAffinity,
            TraitId.WaterAffinity,
            TraitId.WindAffinity,
            TraitId.EarthAffinity,
            TraitId.LightningAffinity,
            TraitId.LifeAffinity
        };

        foreach (var traitId in affinityTraitIds)
        {
            var modifiers = TraitCatalog.GetUnitModifiersForTrait(traitId);
            AssertThat(modifiers.Count).IsGreater(0);
        }
    }
}
