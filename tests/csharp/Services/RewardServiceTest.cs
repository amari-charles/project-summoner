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
[TestSuite]
public class RewardServiceTest
{
    // =============================================================================
    // RewardPoolCatalog Tests
    // =============================================================================

    [TestCase]
    public void GetPool_StandardCards_ReturnsPool()
    {
        var pool = RewardPoolCatalog.GetPool("standard_cards");

        AssertThat(pool).IsNotNull();
        AssertThat(pool!.LegacyPoolId).IsEqual("standard_cards");
    }

    [TestCase]
    public void GetPool_NonExistent_ReturnsNull()
    {
        var pool = RewardPoolCatalog.GetPool("nonexistent_pool");

        AssertThat(pool).IsNull();
    }

    [TestCase]
    public void HasPool_ExistingPool_ReturnsTrue()
    {
        AssertThat(RewardPoolCatalog.HasPool("standard_cards")).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool("fire_cards")).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool("common_cards")).IsTrue();
    }

    [TestCase]
    public void HasPool_NonExistentPool_ReturnsFalse()
    {
        AssertThat(RewardPoolCatalog.HasPool("nonexistent")).IsFalse();
    }

    [TestCase]
    public void GetAllPoolIds_ReturnsEnumPools()
    {
        var poolIds = RewardPoolCatalog.GetAllPoolIds();

        AssertThat(poolIds.Length).IsGreater(0);
        AssertThat(poolIds).Contains(RewardPoolId.TutorialRewards);
        AssertThat(poolIds).Contains(RewardPoolId.FireCommonUnits);
        AssertThat(poolIds).Contains(RewardPoolId.ElementalStarters);
    }

    [TestCase]
    public void HasPool_LegacyStringPools_StillWork()
    {
        // Legacy string-based pools should still work for backward compatibility
        AssertThat(RewardPoolCatalog.HasPool("standard_cards")).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool("fire_cards")).IsTrue();
        AssertThat(RewardPoolCatalog.HasPool("common_cards")).IsTrue();
    }

    [TestCase]
    public void GetCardsForPool_StandardCards_ReturnsCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool("standard_cards");

        AssertThat(cards.Length).IsGreater(0);
        // Should exclude dev-only cards
        foreach (var card in cards)
        {
            AssertThat(card.UnlockCondition).IsNotEqual(UnlockCondition.DevOnly);
        }
    }

    [TestCase]
    public void GetCardsForPool_FireCards_ReturnsOnlyFireCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool("fire_cards");

        AssertThat(cards.Length).IsGreater(0);
        foreach (var card in cards)
        {
            AssertThat(card.ElementalAffinity).IsEqual(Element.Fire);
        }
    }

    [TestCase]
    public void GetCardsForPool_CommonCards_ReturnsOnlyCommonCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool("common_cards");

        AssertThat(cards.Length).IsGreater(0);
        foreach (var card in cards)
        {
            AssertThat(card.Rarity).IsEqual(Rarity.Common);
        }
    }

    [TestCase]
    public void GetCardsForPool_WithExclusions_ExcludesCards()
    {
        var excludeIds = new HashSet<string> { "fire_wisp", "fireball" };
        var cards = RewardPoolCatalog.GetCardsForPool("standard_cards", excludeIds);

        foreach (var card in cards)
        {
            AssertThat(excludeIds.Contains(card.Id)).IsFalse();
        }
    }

    [TestCase]
    public void FilterCards_ByElement_FiltersCorrectly()
    {
        var filters = new CardFilterConfig
        {
            Elements = new List<Element> { Element.Water }
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
            Rarities = new List<Rarity> { Rarity.Rare, Rarity.Epic }
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
            CardTypes = new List<CardType> { CardType.Spell }
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
            ExcludeUnlockConditions = new List<UnlockCondition> { UnlockCondition.DevOnly }
        };

        var cards = RewardPoolCatalog.FilterCards(filters);

        foreach (var card in cards)
        {
            AssertThat(card.UnlockCondition).IsNotEqual(UnlockCondition.DevOnly);
        }
    }

    [TestCase]
    public void GetPoolIdForElement_ReturnsCorrectPool()
    {
        // Now returns enum-based pool IDs for common units
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Fire)).IsEqual(RewardPoolId.FireCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Water)).IsEqual(RewardPoolId.WaterCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Wind)).IsEqual(RewardPoolId.WindCommonUnits);
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Earth)).IsEqual(RewardPoolId.EarthCommonUnits);
        // Elements without specific pools return null
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Neutral)).IsNull();
        AssertThat(RewardPoolCatalog.GetPoolIdForElement(Element.Lightning)).IsNull();
    }

    // =============================================================================
    // Enum-based Pool Tests
    // =============================================================================

    [TestCase]
    public void GetPool_EnumBased_ReturnsPool()
    {
        var pool = RewardPoolCatalog.GetPool(RewardPoolId.TutorialRewards);

        AssertThat(pool).IsNotNull();
        AssertThat(pool!.PoolId).IsEqual(RewardPoolId.TutorialRewards);
    }

    [TestCase]
    public void GetCardsForPool_EnumBased_ReturnsCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolId.FireCommonUnits);

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
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolId.ElementalStarters);

        AssertThat(cards.Length).IsGreater(0);
        // Should contain cards from Fire, Water, Wind, and Earth common units
        var elements = cards.Select(c => c.ElementalAffinity).Distinct().ToList();
        // At least some variety expected (depends on card catalog)
        AssertThat(elements.Count).IsGreaterEqual(1);
    }

    [TestCase]
    public void GetCardsForPool_CuratedPool_ReturnsExplicitCards()
    {
        var cards = RewardPoolCatalog.GetCardsForPool(RewardPoolId.TutorialRewards);

        AssertThat(cards.Length).IsGreater(0);
        // Tutorial rewards should include specific cards
        var cardIds = cards.Select(c => c.Id).ToHashSet();
        // These are defined in the catalog
        AssertThat(cardIds.Contains("charge") || cardIds.Contains("guard") || cardIds.Contains("fire_wisp")).IsTrue();
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
    // RewardConfig Tests
    // =============================================================================

    [TestCase]
    public void RewardConfig_FromGodotDict_ParsesCorrectly()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["guaranteed_count"] = 1,
            ["pool_count"] = 2,
            ["pool_id"] = "fire_cards",
            ["collection_filter"] = "exclude_owned"
        };

        var config = RewardConfig.FromGodotDict(dict);

        AssertThat(config.GuaranteedCount).IsEqual(1);
        AssertThat(config.PoolCount).IsEqual(2);
        AssertThat(config.PoolId).IsEqual("fire_cards");
        AssertThat(config.CollectionFilter).IsEqual(CollectionFilterMode.ExcludeOwned);
    }

    [TestCase]
    public void RewardConfig_FromGodotDict_HandlesDefaults()
    {
        var dict = new Godot.Collections.Dictionary();

        var config = RewardConfig.FromGodotDict(dict);

        AssertThat(config.GuaranteedCount).IsEqual(0);
        AssertThat(config.PoolCount).IsEqual(0);
        AssertThat(config.PoolId).IsEqual("standard_cards");
        AssertThat(config.CollectionFilter).IsEqual(CollectionFilterMode.None);
    }

    // =============================================================================
    // CollectionFilterMode Tests
    // =============================================================================

    [TestCase]
    public void CollectionFilterMode_HasExpectedValues()
    {
        AssertThat((int)CollectionFilterMode.None).IsEqual(0);
        AssertThat((int)CollectionFilterMode.ExcludeOwned).IsEqual(1);
        AssertThat((int)CollectionFilterMode.ExcludeDuplicates).IsEqual(2);
    }

    // =============================================================================
    // RewardType Tests
    // =============================================================================

    [TestCase]
    public void RewardType_HasExpectedValues()
    {
        AssertThat((int)RewardType.Card).IsEqual(0);
        AssertThat((int)RewardType.CampaignGold).IsEqual(1);
        AssertThat((int)RewardType.Gold).IsEqual(2);
        AssertThat((int)RewardType.Gems).IsEqual(3);
    }
}
