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
            MinLevel = 1,
        };

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "rare",
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
            MinLevel = 1,
        };

        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "water_wisp",
            CardRarity = "common",
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
            OwnedTraitIds = new HashSet<string>
            {
                TraitIds.Legion,
                TraitIds.LegionII,
                TraitIds.LegionIII,
            },
            CurrentLevel = 8,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "rare",
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(
            TraitDefinitions.LegionIV,
            context
        );
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
            CardRarity = "epic",
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(
            TraitDefinitions.LegionII,
            context
        );
        AssertThat(evaluation.CanUnlockNow).IsTrue();
    }

    [TestCase]
    public void EvaluateProgressionTrait_SummonerExclusiveTrait_RejectsWrongSummonerTag()
    {
        var context = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summoner,
            EligibilityTags = new HashSet<string>
            {
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Fire,
                TraitTags.Cole,
            },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
        };

        var evaluation = TraitTreeEvaluator.EvaluateProgressionTrait(
            TraitDefinitions.SeleneHealthI,
            context
        );
        AssertThat(evaluation.CanUnlockNow).IsFalse();
        AssertThat(evaluation.LockedReason).Contains("owner");
    }

    [TestCase]
    public void EvaluateProgressionTrait_SummonerTierII_RequiresTierI()
    {
        var blockedContext = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summoner,
            EligibilityTags = new HashSet<string>
            {
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Fire,
                TraitTags.Cole,
            },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
        };

        var blocked = TraitTreeEvaluator.EvaluateProgressionTrait(
            TraitDefinitions.ColeSoulStrengthII,
            blockedContext
        );
        AssertThat(blocked.CanUnlockNow).IsFalse();
        AssertThat(blocked.MissingPrerequisiteIds).Contains(TraitIds.ColeSoulStrengthI);

        var unlockedContext = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summoner,
            EligibilityTags = new HashSet<string>
            {
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Fire,
                TraitTags.Cole,
            },
            OwnedTraitIds = new HashSet<string> { TraitIds.ColeSoulStrengthI },
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
        };

        var unlocked = TraitTreeEvaluator.EvaluateProgressionTrait(
            TraitDefinitions.ColeSoulStrengthII,
            unlockedContext
        );
        AssertThat(unlocked.CanUnlockNow).IsTrue();
    }

    [TestCase]
    public void EvaluateProgressionTrait_GlobalOnlyTagTrait_UsesRequiredTagsForOwnerScope()
    {
        var trait = new TraitDefinition
        {
            Id = TraitId.FromString("test_global_required_owner"),
            NameKey = "trait.test.global_required_owner.name",
            DescriptionKey = "trait.test.global_required_owner.description",
            Category = TraitCategory.Utility,
            Tags = [TraitTags.Global],
            RequiredTags = [TraitTags.Summon],
            MinLevel = 1,
        };

        var summonContext = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Summon,
            EligibilityTags = new HashSet<string> { TraitTags.Summon, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "fire_wisp",
            CardRarity = "common",
        };

        var summonEvaluation = TraitTreeEvaluator.EvaluateProgressionTrait(trait, summonContext);
        AssertThat(summonEvaluation.CanUnlockNow).IsTrue();

        var spellContext = new TraitTreeOwnerContext
        {
            OwnerTypeTag = TraitTags.Spell,
            EligibilityTags = new HashSet<string> { TraitTags.Spell, TraitTags.Global },
            OwnedTraitIds = new HashSet<string>(),
            CurrentLevel = 3,
            UnspentTraitPoints = 1,
            CardCatalogId = "fireball",
            CardRarity = "common",
        };

        var spellEvaluation = TraitTreeEvaluator.EvaluateProgressionTrait(trait, spellContext);
        AssertThat(spellEvaluation.CanUnlockNow).IsFalse();
        AssertThat(spellEvaluation.LockedReason).Contains("owner");
    }
}
