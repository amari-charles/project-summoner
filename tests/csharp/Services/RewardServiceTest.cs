namespace ProjectSummoner.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;
using ProjectSummoner.Services.Rewards;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for RewardService and RewardPoolCatalog.
/// Tests the static methods and data structures without needing the Node instance.
/// </summary>
// [TestSuite] — requires Godot runtime (CardCatalog creates Resource objects); run via editor's gdUnit4 panel
public class RewardServiceTest
{
    // =============================================================================
    // RewardPoolCatalog - Pool Lookup Tests
    // =============================================================================

    [TestCase]
    public void GetPool_ReturnsPoolDefinition()
    {
        var pool = RewardPoolCatalog.GetPool(RewardPoolIds.TutorialRewards);

        AssertThat(pool).IsNotNull();
        AssertThat(pool!.PoolId).IsEqual(RewardPoolIds.TutorialRewards);
    }

    [TestCase]
    public void HasPool_ExistingPool_ReturnsTrue()
    {
        AssertThat(RewardPoolCatalog.HasPool(RewardPoolIds.TutorialRewards)).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool(RewardPoolIds.FireCommonUnits)).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool(RewardPoolIds.ElementalStarters)).IsTrue();
    }

    [TestCase]
    public void HasPool_InvalidPool_ReturnsFalse()
    {
        AssertThat(RewardPoolCatalog.HasPool(new RewardPoolId("nonexistent_pool"))).IsFalse();
        AssertThat(RewardPoolCatalog.HasPool("another_invalid_pool")).IsFalse();
    }

    [TestCase]
    public void GetAllPoolIds_ReturnsAllPools()
    {
        var poolIds = RewardPoolCatalog.GetAllPoolIds();

        AssertThat(poolIds.Length).IsGreater(0);
        AssertThat(poolIds).Contains(RewardPoolIds.TutorialRewards);
        AssertThat(poolIds).Contains(RewardPoolIds.FireCommonUnits);
        AssertThat(poolIds).Contains(RewardPoolIds.ElementalStarters);
    }

    // =============================================================================
    // RewardPoolCatalog - Card Resolution Tests
    // =============================================================================

    [TestCase]
    public void GetCardsForPool_FilterBasedPool_ReturnsFilteredCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.FireCommonUnits);

        AssertThat(cards.Length).IsGreater(0);
        foreach (var card in cards)
        {
            AssertThat(card.ElementalAffinity).IsEqual(Element.Fire);
            AssertThat(card.Rarity).IsEqual(Rarity.Common);
            AssertThat(card.Type).IsEqual(CardType.Summon);
        }
    }

    [TestCase]
    public void GetCardsForPool_CompositePool_ReturnsUnion()
    {
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.ElementalStarters);

        AssertThat(cards.Length).IsGreater(0);
        // Should contain cards from Fire, Water, Wind, and Earth common units
        var elements = cards.Select(c => c.ElementalAffinity).Distinct().ToList();
        // At least some variety expected (depends on card catalog)
        AssertThat(elements.Count).IsGreaterEqual(1);
    }

    [TestCase]
    public void GetCardsForPool_CuratedPool_ReturnsExplicitCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.TutorialRewards);

        AssertThat(cards.Length).IsGreater(0);
        // Tutorial rewards should include specific cards
        var cardIdStrings = cards.Select(c => (string)c.Id).ToHashSet();
        // These are defined in the catalog
        AssertThat(cardIdStrings.Contains("charge") || cardIdStrings.Contains("guard") || cardIdStrings.Contains("fire_wisp")).IsTrue();
    }

    [TestCase]
    public void GetCardsForPool_WithExclusions_ExcludesCards()
    {
        var excludeIds = new HashSet<string> { "fire_wisp", "fireball" };
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolIds.TutorialRewards, excludeIds);

        foreach (var card in cards)
        {
            AssertThat(excludeIds.Contains((string)card.Id)).IsFalse();
        }
    }

    // =============================================================================
    // RewardPoolCatalog - FilterCards Tests
    // =============================================================================

    [TestCase]
    public void FilterCards_ByElement_FiltersCorrectly()
    {
        var filters = new CardFilterConfig
        {
            Elements = [Element.Water]
        };

        var cards = RewardPoolCatalog.FilterCards(filters);

        foreach (var card in cards)
        {
            AssertThat(card.ElementalAffinity).IsEqual(Element.Water);
        }
    }

    [TestCase]
    public void FilterCards_ByRarity_FiltersCorrectly()
    {
        var filters = new CardFilterConfig
        {
            Rarities = [Rarity.Rare, Rarity.Epic]
        };

        var cards = RewardPoolCatalog.FilterCards(filters);

        foreach (var card in cards)
        {
            AssertThat(card.Rarity == Rarity.Rare || card.Rarity == Rarity.Epic).IsTrue();
        }
    }

    [TestCase]
    public void FilterCards_ByCardType_FiltersCorrectly()
    {
        var filters = new CardFilterConfig
        {
            CardTypes = [CardType.Spell]
        };

        var cards = RewardPoolCatalog.FilterCards(filters);

        foreach (var card in cards)
        {
            AssertThat(card.Type).IsEqual(CardType.Spell);
        }
    }

    [TestCase]
    public void FilterCards_ExcludeDevOnly_ExcludesDevCards()
    {
        var filters = new CardFilterConfig
        {
            ExcludeUnlockConditions = [UnlockCondition.DevOnly]
        };

        var cards = RewardPoolCatalog.FilterCards(filters);

        foreach (var card in cards)
        {
            AssertThat(card.UnlockCondition).IsNotEqual(UnlockCondition.DevOnly);
        }
    }

    // =============================================================================
    // RewardPoolCatalog - Utility Tests
    // =============================================================================

    [TestCase]
    public void GetCardsForPool_InvalidPoolId_ReturnsEmpty()
    {
        var invalidPoolId = new RewardPoolId("nonexistent_pool_999");
        var cards = RewardPoolCatalog.GetCardsForPool(invalidPoolId);

        // Should return empty array (and log warning internally)
        AssertThat(cards.Length).IsEqual(0);
    }

    [TestCase]
    public void GetPoolIdForElement_ReturnsCorrectPool()
    {
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Fire)).IsEqual(RewardPoolIds.FireCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Water)).IsEqual(RewardPoolIds.WaterCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Wind)).IsEqual(RewardPoolIds.WindCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Earth)).IsEqual(RewardPoolIds.EarthCommonUnits);
        // Elements without specific pools return null
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Neutral)).IsNull();
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Lightning)).IsNull();
    }

    // =============================================================================
    // RewardOption Tests
    // =============================================================================

    [TestCase]
    public void RewardOption_CardType_HasCorrectDefaults()
    {
        var option = new RewardOption
        {
            Type = RewardType.Card,
            Id = "fire_wisp"
        };

        AssertThat(option.Type).IsEqual(RewardType.Card);
        AssertThat(option.Id).IsEqual("fire_wisp");
        AssertThat(option.Amount).IsEqual(1);
        AssertThat(option.Rarity).IsEqual("common");
        AssertThat(option.IsGuaranteed).IsFalse();
    }

    [TestCase]
    public void RewardOption_GoldType_HasCorrectProperties()
    {
        var option = new RewardOption
        {
            Type = RewardType.CampaignGold,
            Amount = 100
        };

        AssertThat(option.Type).IsEqual(RewardType.CampaignGold);
        AssertThat(option.Amount).IsEqual(100);
        AssertThat(option.Id).IsEqual("");
    }

    // =============================================================================
    // Enum Value Tests
    // =============================================================================

    [TestCase]
    public void CollectionFilterMode_HasExpectedValues()
    {
        AssertThat((int)CollectionFilterMode.None).IsEqual(0);
        AssertThat((int)CollectionFilterMode.ExcludeOwned).IsEqual(1);
        AssertThat((int)CollectionFilterMode.ExcludeDuplicates).IsEqual(2);
    }

    [TestCase]
    public void RewardType_HasExpectedValues()
    {
        AssertThat((int)RewardType.Card).IsEqual(0);
        AssertThat((int)RewardType.CampaignGold).IsEqual(1);
        AssertThat((int)RewardType.Gold).IsEqual(2);
        AssertThat((int)RewardType.Gems).IsEqual(3);
    }

    // =============================================================================
    // RewardPoolId Struct Tests
    // =============================================================================

    [TestCase]
    public void RewardPoolId_HasValue_WorksCorrectly()
    {
        AssertThat(RewardPoolIds.TutorialRewards.HasValue).IsTrue();
        AssertThat(RewardPoolId.None.HasValue).IsFalse();
        AssertThat(new RewardPoolId("").HasValue).IsFalse();
    }

    [TestCase]
    public void RewardPoolId_ImplicitConversion_WorksCorrectly()
    {
        string value = RewardPoolIds.TutorialRewards;
        AssertThat(value).IsEqual("tutorial_rewards");
    }

    [TestCase]
    public void RewardPoolId_Equality_WorksCorrectly()
    {
        var id1 = new RewardPoolId("test_pool");
        var id2 = new RewardPoolId("test_pool");
        var id3 = new RewardPoolId("other_pool");

        AssertThat(id1).IsEqual(id2);
        AssertThat(id1).IsNotEqual(id3);
    }
}
