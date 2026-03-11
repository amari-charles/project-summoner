namespace Fateforged.Tests.Traits;

using System.Linq;
using GdUnit4;
using Fateforged.Data.Traits;
using Fateforged.Stats;
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
        var trait = TraitCatalog.GetTrait(TraitIds.FireAffinity);

        AssertThat(trait).IsNotNull();
        AssertThat((string)trait!.Id).IsEqual((string)TraitIds.FireAffinity);
        AssertThat(trait.Category).IsEqual(TraitCategory.Elemental);
        AssertThat(trait.IsInnate).IsTrue();
        AssertThat(trait.AcquisitionMode).IsEqual(TraitAcquisitionMode.LevelUpOffer);
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
        var exists = TraitCatalog.HasTrait(TraitIds.WaterAffinity);

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
            AssertThat(trait.Category).IsEqual(TraitCategory.Elemental);
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
    public void GetTraitsByAcquisitionMode_ReturnsOnlyGrantedOnlyTraits()
    {
        var grantedTraits = TraitCatalog.GetTraitsByAcquisitionMode(TraitAcquisitionMode.GrantedOnly);

        AssertThat(grantedTraits.Length).IsGreater(0);
        foreach (var trait in grantedTraits)
        {
            AssertThat(trait.AcquisitionMode).IsEqual(TraitAcquisitionMode.GrantedOnly);
        }
    }

    [TestCase]
    public void GetUnitModifiersForTrait_ReturnsModifiers_ForElementalAffinity()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitIds.FireAffinity);

        AssertThat(modifiers).IsNotNull();
        AssertThat(modifiers.Count).IsGreater(0);

        // Fire affinity should buff fire units
        var firstMod = modifiers[0];
        AssertThat(firstMod.Conditions.ContainsKey("elemental_affinity")).IsTrue();
        AssertThat(firstMod.Conditions["elemental_affinity"]).IsEqual("fire");
        AssertThat(firstMod.StatMults.ContainsKey(StatKey.AttackDamage)).IsTrue();
    }

    [TestCase]
    public void GetUnitModifiersForTrait_ReturnsEmpty_ForTraitWithoutUnitModifiers()
    {
        // BurningSpirit only has summoner stat modifiers, no unit modifiers
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitIds.BurningSpirit);

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
        var trait = TraitCatalog.GetTrait(TraitIds.StoneFortitude);

        AssertThat(trait).IsNotNull();
        AssertThat(trait!.Modifiers).IsNotNull();
        AssertThat(trait.Modifiers).IsNotEmpty();

        var modifier = trait.Modifiers[0];
        AssertThat(modifier.Stat).IsEqual(StatKey.DamageReduction);
        AssertThat(modifier.Type).IsEqual(ModifierType.Flat);
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
            TraitIds.FireAffinity,
            TraitIds.WaterAffinity,
            TraitIds.WindAffinity,
            TraitIds.EarthAffinity,
            TraitIds.LightningAffinity,
            TraitIds.LifeAffinity
        };

        foreach (var traitId in affinityTraitIds)
        {
            var modifiers = TraitCatalog.GetUnitModifiersForTrait(traitId);
            AssertThat(modifiers.Count).IsGreater(0);
        }
    }

    // =========================================================================
    // TRIGGERED TRAIT TESTS
    // =========================================================================

    [TestCase]
    public void BerserkerTrait_HasBelowHpTrigger()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitIds.Berserker);

        AssertThat(modifiers.Count).IsGreater(0);

        var mod = modifiers[0];
        AssertThat(mod.Trigger).IsEqual(TriggerCondition.BelowHpPercent);
        AssertThat(mod.TriggerThreshold).IsEqual(0.5f);
        AssertThat(mod.StatMults.ContainsKey(StatKey.AttackDamage)).IsTrue();
        AssertThat(mod.StatMults[StatKey.AttackDamage]).IsEqual(1.2f);
    }

    [TestCase]
    public void VengefulTrait_HasOnTakeHitTrigger()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitIds.Vengeful);

        AssertThat(modifiers.Count).IsGreater(0);

        var mod = modifiers[0];
        AssertThat(mod.Trigger).IsEqual(TriggerCondition.OnTakeHit);
        AssertThat(mod.TriggerDuration).IsEqual(5.0f);
        AssertThat(mod.TriggerCooldown).IsEqual(1.0f);
        AssertThat(mod.StatMults.ContainsKey(StatKey.AttackSpeed)).IsTrue();
    }

    [TestCase]
    public void SoulHarvestTrait_HasOnKillTrigger()
    {
        var modifiers = TraitCatalog.GetUnitModifiersForTrait(TraitIds.SoulHarvest);

        AssertThat(modifiers.Count).IsGreater(0);

        var mod = modifiers[0];
        AssertThat(mod.Trigger).IsEqual(TriggerCondition.OnKill);
        AssertThat(mod.StatAdds.ContainsKey(StatKey.HealOnKill)).IsTrue();
        AssertThat(mod.StatAdds[StatKey.HealOnKill]).IsEqual(5.0f);
    }

    [TestCase]
    public void TriggeredTraits_ExistInCatalog()
    {
        AssertThat(TraitCatalog.HasTrait(TraitIds.Berserker)).IsTrue();
        AssertThat(TraitCatalog.HasTrait(TraitIds.Vengeful)).IsTrue();
        AssertThat(TraitCatalog.HasTrait(TraitIds.SoulHarvest)).IsTrue();
    }

    [TestCase]
    public void FortuneFavorsTheBold_IsGrantedOnlySpecialTrait()
    {
        var trait = TraitCatalog.GetTrait(TraitIds.FortuneFavorsTheBold);

        AssertThat(trait).IsNotNull();
        AssertThat(trait!.AcquisitionMode).IsEqual(TraitAcquisitionMode.GrantedOnly);
        AssertThat(trait.Category).IsEqual(TraitCategory.Special);
    }

    [TestCase]
    public void TraitDefinition_ResolveStatMultipliersForCard_UsesMostSpecificOverride()
    {
        var trait = new TraitDefinition
        {
            Id = TraitId.FromString("test_trait_override"),
            NameKey = "trait.test.name",
            DescriptionKey = "trait.test.description",
            Category = TraitCategory.Combat,
            Tags = [TraitTags.Summon, TraitTags.Global],
            Modifiers =
            [
                new TraitModifier
                {
                    Target = "unit",
                    StatMults = new() { [StatKey.AttackDamage] = 1.06f }
                }
            ],
            ValueOverrides =
            [
                new TraitValueOverride
                {
                    Rarities = ["common"],
                    StatMults = new() { [StatKey.AttackDamage] = 1.05f }
                },
                new TraitValueOverride
                {
                    CardCatalogIds = ["fire_wisp"],
                    StatMults = new() { [StatKey.AttackDamage] = 1.07f }
                },
                new TraitValueOverride
                {
                    CardCatalogIds = ["fire_wisp"],
                    Rarities = ["common"],
                    StatMults = new() { [StatKey.AttackDamage] = 1.09f }
                }
            ]
        };

        var exact = trait.ResolveStatMultipliersForCard("fire_wisp", "common");
        var cardOnly = trait.ResolveStatMultipliersForCard("fire_wisp", "epic");
        var rarityOnly = trait.ResolveStatMultipliersForCard("water_wisp", "common");
        var baseOnly = trait.ResolveStatMultipliersForCard("water_wisp", "rare");

        AssertThat(exact[StatKey.AttackDamage]).IsEqual(1.09f);
        AssertThat(cardOnly[StatKey.AttackDamage]).IsEqual(1.07f);
        AssertThat(rarityOnly[StatKey.AttackDamage]).IsEqual(1.05f);
        AssertThat(baseOnly[StatKey.AttackDamage]).IsEqual(1.06f);
    }

    [TestCase]
    public void TraitDefinition_ResolveSpawnCountAddForCard_AppliesOverride()
    {
        var trait = new TraitDefinition
        {
            Id = TraitId.FromString("test_trait_unit_count"),
            NameKey = "trait.test.unit_count.name",
            DescriptionKey = "trait.test.unit_count.description",
            Category = TraitCategory.Utility,
            Tags = [TraitTags.Summon, TraitTags.Global],
            Modifiers =
            [
                new TraitModifier
                {
                    Target = "unit",
                    StatAdds = new() { [StatKey.UnitCount] = 1f }
                }
            ],
            ValueOverrides =
            [
                new TraitValueOverride
                {
                    CardCatalogIds = ["fire_wisp"],
                    Rarities = ["rare"],
                    UnitCountAdd = 2
                }
            ]
        };

        AssertThat(trait.ResolveSpawnCountAddForCard("fire_wisp", "rare")).IsEqual(2);
        AssertThat(trait.ResolveSpawnCountAddForCard("fire_wisp", "common")).IsEqual(1);
        AssertThat(trait.ResolveSpawnCountAddForCard("water_wisp", "rare")).IsEqual(1);
    }
}
