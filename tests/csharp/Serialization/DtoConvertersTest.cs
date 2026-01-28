namespace ProjectSummoner.Tests.Serialization;

using System.Collections.Generic;
using GdUnit4;
using Godot;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Domain.Profile.Account;
using ProjectSummoner.Domain.Profile.Campaign;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Domain.Profile.Decks;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;
using ProjectSummoner.Domain.Profile.Summoners;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for DtoConverters - centralized Dict↔Domain conversions.
/// </summary>
[TestSuite]
public class DtoConvertersTest
{
    // =========================================================================
    // SummonerInstance Tests
    // =========================================================================

    [TestCase]
    public void SummonerInstance_RoundTrip_PreservesAllFields()
    {
        var original = new SummonerInstance
        {
            SummonerId = "summoner_cole",
            Level = 5,
            Xp = 1500,
            AcquiredBoonIds = ["boon_1", "boon_2"],
            EquippedItems = new Dictionary<ItemSlot, string?>
            {
                [ItemSlot.Weapon] = "item_001",
                [ItemSlot.Ring1] = null,
                [ItemSlot.Ring2] = "item_002",
                [ItemSlot.Vestments] = null
            }
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromSummonerDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.SummonerId).IsEqual("summoner_cole");
        AssertThat(result.Level).IsEqual(5);
        AssertThat(result.Xp).IsEqual(1500);
        AssertThat(result.AcquiredBoonIds).Contains("boon_1");
        AssertThat(result.AcquiredBoonIds).Contains("boon_2");
        AssertThat(result.EquippedItems[ItemSlot.Weapon]).IsEqual("item_001");
        AssertThat(result.EquippedItems[ItemSlot.Ring1]).IsNull();
        AssertThat(result.EquippedItems[ItemSlot.Ring2]).IsEqual("item_002");
        AssertThat(result.EquippedItems[ItemSlot.Vestments]).IsNull();
    }

    [TestCase]
    public void SummonerInstance_FromDict_ReturnsNullForEmptyDict()
    {
        var result = DtoConverters.FromSummonerDict(new Godot.Collections.Dictionary());
        AssertThat(result).IsNull();
    }

    [TestCase]
    public void SummonerInstance_FromDict_ReturnsNullForNullDict()
    {
        var result = DtoConverters.FromSummonerDict(null);
        AssertThat(result).IsNull();
    }

    [TestCase]
    public void SummonerInstance_FromDict_ReturnsNullForMissingSummonerId()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["level"] = 1,
            ["xp"] = 0
        };
        var result = DtoConverters.FromSummonerDict(dict);
        AssertThat(result).IsNull();
    }

    // =========================================================================
    // CardInstance Tests
    // =========================================================================

    [TestCase]
    public void CardInstance_RoundTrip_PreservesAllFields()
    {
        var original = new CardInstance
        {
            Id = "card_001",
            CatalogId = "fire_wisp",
            ProfileId = "profile_123",
            Rarity = "epic",
            Level = 3,
            Xp = 250,
            Upgrades = ["upgrade_1", "upgrade_2"],
            RollJson = "{\"variant\":1}",
            CreatedAt = 1700000000,
            Binding = ContentBinding.SummonerBound,
            BoundToSummonerId = "summoner_cole"
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCardDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Id).IsEqual("card_001");
        AssertThat(result.CatalogId).IsEqual("fire_wisp");
        AssertThat(result.ProfileId).IsEqual("profile_123");
        AssertThat(result.Rarity).IsEqual("epic");
        AssertThat(result.Level).IsEqual(3);
        AssertThat(result.Xp).IsEqual(250);
        AssertThat(result.Upgrades).Contains("upgrade_1");
        AssertThat(result.RollJson).IsEqual("{\"variant\":1}");
        AssertThat(result.CreatedAt).IsEqual(1700000000);
        AssertThat(result.Binding).IsEqual(ContentBinding.SummonerBound);
        AssertThat(result.BoundToSummonerId).IsEqual("summoner_cole");
    }

    [TestCase]
    public void CardInstance_FromDict_ReturnsNullForMissingRequiredFields()
    {
        // Missing catalog_id
        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = "card_001"
        };
        AssertThat(DtoConverters.FromCardDict(dict)).IsNull();

        // Missing id
        dict = new Godot.Collections.Dictionary
        {
            ["catalog_id"] = "fire_wisp"
        };
        AssertThat(DtoConverters.FromCardDict(dict)).IsNull();
    }

    [TestCase]
    public void CardInstance_ToDict_SerializesBindingAsInt()
    {
        var card = new CardInstance
        {
            Id = "card_001",
            CatalogId = "test",
            Binding = ContentBinding.SummonerBound
        };

        var dict = DtoConverters.ToDict(card);
        AssertThat(dict["binding"].AsInt32()).IsEqual(1);
    }

    // =========================================================================
    // ItemInstance Tests
    // =========================================================================

    [TestCase]
    public void ItemInstance_RoundTrip_PreservesAllFields()
    {
        var original = new ItemInstance
        {
            Id = "item_001",
            CatalogId = "sword_of_fire",
            EquippedBySummonerId = "summoner_cole",
            BoundToSummonerId = "summoner_cole",
            EquippedSlot = ItemSlot.Weapon
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromItemDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Id).IsEqual("item_001");
        AssertThat(result.CatalogId).IsEqual("sword_of_fire");
        AssertThat(result.EquippedBySummonerId).IsEqual("summoner_cole");
        AssertThat(result.BoundToSummonerId).IsEqual("summoner_cole");
        AssertThat(result.EquippedSlot).IsEqual(ItemSlot.Weapon);
    }

    [TestCase]
    public void ItemInstance_RoundTrip_HandlesNullSlot()
    {
        var original = new ItemInstance
        {
            Id = "item_001",
            CatalogId = "ring_of_power",
            EquippedSlot = null
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromItemDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.EquippedSlot).IsNull();
    }

    [TestCase]
    public void ItemInstance_ToDict_SerializesSlotAsString()
    {
        var item = new ItemInstance
        {
            Id = "item_001",
            CatalogId = "test",
            EquippedSlot = ItemSlot.Vestments
        };

        var dict = DtoConverters.ToDict(item);
        AssertThat(dict["slot"].AsString()).IsEqual("vestments");
    }

    // =========================================================================
    // Deck Tests
    // =========================================================================

    [TestCase]
    public void Deck_RoundTrip_PreservesAllFields()
    {
        var original = new Deck
        {
            Id = "deck_001",
            ProfileId = "profile_123",
            SummonerId = "summoner_cole",
            Name = "My Fire Deck",
            Slot = 2,
            IsActive = true,
            CardInstanceIds = ["card_1", "card_2", "card_3"],
            UpdatedAt = 1700000000
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromDeckDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Id).IsEqual("deck_001");
        AssertThat(result.ProfileId).IsEqual("profile_123");
        AssertThat(result.SummonerId).IsEqual("summoner_cole");
        AssertThat(result.Name).IsEqual("My Fire Deck");
        AssertThat(result.Slot).IsEqual(2);
        AssertThat(result.IsActive).IsTrue();
        AssertThat(result.CardInstanceIds).Contains("card_1");
        AssertThat(result.CardInstanceIds).Contains("card_2");
        AssertThat(result.CardInstanceIds).Contains("card_3");
        AssertThat(result.UpdatedAt).IsEqual(1700000000);
    }

    [TestCase]
    public void Deck_FromDict_ReturnsNullForMissingRequiredFields()
    {
        // Missing id
        var dict = new Godot.Collections.Dictionary
        {
            ["summoner_id"] = "summoner_cole"
        };
        AssertThat(DtoConverters.FromDeckDict(dict)).IsNull();

        // Missing summoner_id
        dict = new Godot.Collections.Dictionary
        {
            ["id"] = "deck_001"
        };
        AssertThat(DtoConverters.FromDeckDict(dict)).IsNull();
    }

    // =========================================================================
    // CampaignProgress Tests
    // =========================================================================

    [TestCase]
    public void CampaignProgress_RoundTrip_PreservesAllFields()
    {
        var original = new CampaignProgress
        {
            CompletedBattles = ["battle_1", "battle_2"],
            CurrentBattle = "battle_3",
            Gold = 500
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.CompletedBattles).Contains("battle_1");
        AssertThat(result.CompletedBattles).Contains("battle_2");
        AssertThat(result.CurrentBattle).IsEqual("battle_3");
        AssertThat(result.Gold).IsEqual(500);
    }

    [TestCase]
    public void CampaignProgress_FromDict_ReturnsDefaultForEmptyDict()
    {
        var result = DtoConverters.FromCampaignDict(new Godot.Collections.Dictionary());
        AssertThat(result).IsNotNull();
        AssertThat(result!.CompletedBattles).IsEmpty();
        AssertThat(result.Gold).IsEqual(0);
    }

    [TestCase]
    public void CampaignProgress_FromDict_ReturnsNullForNullDict()
    {
        var result = DtoConverters.FromCampaignDict(null);
        AssertThat(result).IsNull();
    }

    [TestCase]
    public void CampaignProgress_RoundTrip_PreservesChoices()
    {
        var original = new CampaignProgress
        {
            CompletedBattles = ["battle_1"],
            CurrentBattle = "battle_2",
            Gold = 100,
            Choices = new Dictionary<string, string>
            {
                ["node_choice_1"] = "option_a",
                ["node_choice_2"] = "option_b"
            }
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Choices).HasSize(2);
        AssertThat(result.Choices["node_choice_1"]).IsEqual("option_a");
        AssertThat(result.Choices["node_choice_2"]).IsEqual("option_b");
    }

    [TestCase]
    public void CampaignProgress_RoundTrip_HandlesEmptyChoices()
    {
        var original = new CampaignProgress
        {
            CompletedBattles = ["battle_1"],
            Gold = 50,
            Choices = []
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Choices).IsEmpty();
    }

    // =========================================================================
    // ToGodotArray Tests
    // =========================================================================

    [TestCase]
    public void ToGodotArray_ConvertsStringList()
    {
        var list = new List<string> { "a", "b", "c" };
        var arr = DtoConverters.ToGodotArray(list);

        AssertThat(arr.Count).IsEqual(3);
        AssertThat(arr[0].AsString()).IsEqual("a");
        AssertThat(arr[1].AsString()).IsEqual("b");
        AssertThat(arr[2].AsString()).IsEqual("c");
    }

    [TestCase]
    public void ToGodotArray_HandlesEmptyList()
    {
        var arr = DtoConverters.ToGodotArray(new List<string>());
        AssertThat(arr.Count).IsEqual(0);
    }

    // =========================================================================
    // Meta Tests
    // =========================================================================

    [TestCase]
    public void Meta_RoundTrip_PreservesAllFields()
    {
        var original = new Meta
        {
            SelectedDeck = "deck_001",
            SelectedSummoner = "summoner_cole",
            AnalyticsOptIn = true,
            TutorialFlags = new Dictionary<string, bool>
            {
                ["intro_completed"] = true,
                ["combat_tutorial"] = false
            },
            Achievements = new Dictionary<string, object>
            {
                ["kills"] = 42L,
                ["win_rate"] = 0.75,
                ["has_trophy"] = true,
                ["title"] = "Champion"
            }
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromMetaDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result.SelectedDeck).IsEqual("deck_001");
        AssertThat(result.SelectedSummoner).IsEqual("summoner_cole");
        AssertThat(result.AnalyticsOptIn).IsTrue();
        AssertThat(result.TutorialFlags["intro_completed"]).IsTrue();
        AssertThat(result.TutorialFlags["combat_tutorial"]).IsFalse();
        // Achievement values preserve their types
        AssertThat(result.Achievements["kills"]).IsEqual(42L);
        AssertThat(result.Achievements["win_rate"]).IsEqual(0.75);
        AssertThat(result.Achievements["has_trophy"]).IsEqual(true);
        AssertThat(result.Achievements["title"]).IsEqual("Champion");
    }

    [TestCase]
    public void Meta_FromDict_ReturnsDefaultForNullDict()
    {
        var result = DtoConverters.FromMetaDict(null);
        AssertThat(result).IsNotNull();
        AssertThat(result.SelectedDeck).IsEqual("");
        AssertThat(result.SelectedSummoner).IsEqual("");
        AssertThat(result.AnalyticsOptIn).IsFalse();
        AssertThat(result.TutorialFlags).IsEmpty();
        AssertThat(result.Achievements).IsEmpty();
    }

    [TestCase]
    public void Meta_FromDict_ReturnsDefaultForEmptyDict()
    {
        var result = DtoConverters.FromMetaDict(new Godot.Collections.Dictionary());
        AssertThat(result).IsNotNull();
        AssertThat(result.SelectedDeck).IsEqual("");
        AssertThat(result.SelectedSummoner).IsEqual("");
    }

    [TestCase]
    public void Meta_FromDict_PreservesAchievementTypes()
    {
        // Create dict with typed achievement values
        var achievementsDict = new Godot.Collections.Dictionary
        {
            ["int_value"] = 100,
            ["float_value"] = 3.14,
            ["bool_value"] = true,
            ["string_value"] = "test"
        };
        var dict = new Godot.Collections.Dictionary
        {
            ["selected_deck"] = "",
            ["selected_summoner"] = "",
            ["analytics_opt_in"] = false,
            ["achievements"] = achievementsDict
        };

        var result = DtoConverters.FromMetaDict(dict);

        // Verify types are preserved (int becomes long in C#)
        AssertThat(result.Achievements["int_value"]).IsInstanceOf<long>();
        AssertThat(result.Achievements["float_value"]).IsInstanceOf<double>();
        AssertThat(result.Achievements["bool_value"]).IsInstanceOf<bool>();
        AssertThat(result.Achievements["string_value"]).IsInstanceOf<string>();
    }

    // =========================================================================
    // MetaUpdate Tests
    // =========================================================================

    [TestCase]
    public void MetaUpdate_ToDict_OnlyIncludesNonNullFields()
    {
        var update = new MetaUpdate
        {
            SelectedSummoner = "summoner_selene"
            // Other fields are null/not set
        };

        var dict = DtoConverters.ToDict(update);

        AssertThat(dict.ContainsKey("selected_summoner")).IsTrue();
        AssertThat(dict["selected_summoner"].AsString()).IsEqual("summoner_selene");
        AssertThat(dict.ContainsKey("selected_deck")).IsFalse();
        AssertThat(dict.ContainsKey("analytics_opt_in")).IsFalse();
        AssertThat(dict.ContainsKey("tutorial_flags")).IsFalse();
        AssertThat(dict.ContainsKey("achievements")).IsFalse();
    }

    [TestCase]
    public void MetaUpdate_ToDict_IncludesAllSetFields()
    {
        var update = new MetaUpdate
        {
            SelectedDeck = "deck_002",
            SelectedSummoner = "summoner_cole",
            AnalyticsOptIn = true,
            TutorialFlags = new Dictionary<string, bool> { ["flag1"] = true },
            Achievements = new Dictionary<string, object> { ["score"] = 100 }
        };

        var dict = DtoConverters.ToDict(update);

        AssertThat(dict.ContainsKey("selected_deck")).IsTrue();
        AssertThat(dict.ContainsKey("selected_summoner")).IsTrue();
        AssertThat(dict.ContainsKey("analytics_opt_in")).IsTrue();
        AssertThat(dict.ContainsKey("tutorial_flags")).IsTrue();
        AssertThat(dict.ContainsKey("achievements")).IsTrue();
        AssertThat(dict["selected_deck"].AsString()).IsEqual("deck_002");
        AssertThat(dict["analytics_opt_in"].AsBool()).IsTrue();
    }

    [TestCase]
    public void MetaUpdate_ToDict_EmptyUpdateReturnsEmptyDict()
    {
        var update = new MetaUpdate();
        var dict = DtoConverters.ToDict(update);
        AssertThat(dict.Count).IsEqual(0);
    }

    // =========================================================================
    // CardUpdate Tests
    // =========================================================================

    [TestCase]
    public void CardUpdate_ToDict_OnlyIncludesNonNullFields()
    {
        var update = new CardUpdate
        {
            Xp = 500
            // Level and Upgrades are null
        };

        var dict = DtoConverters.ToDict(update);

        AssertThat(dict.ContainsKey("xp")).IsTrue();
        AssertThat(dict["xp"].AsInt32()).IsEqual(500);
        AssertThat(dict.ContainsKey("level")).IsFalse();
        AssertThat(dict.ContainsKey("upgrades")).IsFalse();
    }

    [TestCase]
    public void CardUpdate_ToDict_IncludesAllSetFields()
    {
        var update = new CardUpdate
        {
            Xp = 1000,
            Level = 5,
            Upgrades = ["upgrade_1", "upgrade_2"]
        };

        var dict = DtoConverters.ToDict(update);

        AssertThat(dict.ContainsKey("xp")).IsTrue();
        AssertThat(dict.ContainsKey("level")).IsTrue();
        AssertThat(dict.ContainsKey("upgrades")).IsTrue();
        AssertThat(dict["xp"].AsInt32()).IsEqual(1000);
        AssertThat(dict["level"].AsInt32()).IsEqual(5);
        var upgrades = dict["upgrades"].AsGodotArray();
        AssertThat(upgrades.Count).IsEqual(2);
        AssertThat(upgrades[0].AsString()).IsEqual("upgrade_1");
    }

    [TestCase]
    public void CardUpdate_ToDict_EmptyUpdateReturnsEmptyDict()
    {
        var update = new CardUpdate();
        var dict = DtoConverters.ToDict(update);
        AssertThat(dict.Count).IsEqual(0);
    }
}
