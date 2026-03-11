namespace Fateforged.Tests.Traits;

using System.Collections.Generic;
using Fateforged.Data.Traits;
using Fateforged.Meta.Services.Traits;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class TraitTreeEvaluatorEligibilityTest
{
    [TestCase]
    public void EvaluateProgressionTrait_RejectsTrait_WhenCardRarityNotAllowed()
    {
        var trait = new TraitDefinition
        {
            Id = TraitId.FromString("test_rarity_gate"),
            NameKey = "trait.test.rarity_gate.name",
            DescriptionKey = "trait.test.rarity_gate.description",
            Category = TraitCategory.Utility,
            Tags = [TraitTags.Summon, TraitTags.Global],
            AllowedRarities = ["common"],
            MinLevel = 1
        };

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "rare"
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(trait, context);
        AssertThat(evaluation.CanUnlockNow).IsFalse();
        AssertThat(evaluation.LockedReason).Contains("rarity");
    }

    [TestCase]
    public void EvaluateProgressionTrait_RejectsTrait_WhenCardCatalogNotAllowed()
    {
        var trait = new TraitDefinition
        {
            Id = TraitId.FromString("test_card_gate"),
            NameKey = "trait.test.card_gate.name",
            DescriptionKey = "trait.test.card_gate.description",
            Category = TraitCategory.Utility,
            Tags = [TraitTags.Summon, TraitTags.Global],
            AllowedCardCatalogIds = ["fire_wisp"],
            MinLevel = 1
        };

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "water_wisp",
            CardRarity = "common"
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(trait, context);
        AssertThat(evaluation.CanUnlockNow).IsFalse();
        AssertThat(evaluation.LockedReason).Contains("card");
    }

    [TestCase]
    public void EvaluateProgressionTrait_LegionIV_RejectsRareRarity()
    {
        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string> { TraitIds.Legion, TraitIds.LegionII, TraitIds.LegionIII },
            CurrentLevel = 8,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "rare"
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(TraitDefinitions.LegionIV, context);
        AssertThat(evaluation.CanUnlockNow).IsFalse();
        AssertThat(evaluation.LockedReason).Contains("rarity");
    }

    [TestCase]
    public void EvaluateProgressionTrait_LegionII_AllowsEpicRarity()
    {
        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string> { TraitIds.Legion },
            CurrentLevel = 4,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "epic"
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(TraitDefinitions.LegionII, context);
        AssertThat(evaluation.CanUnlockNow).IsTrue();
    }
}
