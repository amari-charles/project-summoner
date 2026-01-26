namespace ProjectSummoner.Tests.Serialization;

using System.Collections.Generic;
using GdUnit4;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Data.Serialization;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for DtoConverters - centralized Dict↔Domain conversions.
/// </summary>
[TestSuite]
public class DtoConvertersTest
{
    // =========================================================================
    // SummonerInstanceData Tests
    // =========================================================================

    [TestCase]
    public void SummonerInstance_RoundTrip_PreservesAllFields()
    {
        var original = new SummonerInstanceData
        {
            SummonerId = "summoner_fire",
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
        AssertThat(result!.SummonerId).IsEqual("summoner_fire");
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
    // CardInstanceData Tests
    // =========================================================================

    [TestCase]
    public void CardInstance_RoundTrip_PreservesAllFields()
    {
        var original = new CardInstanceData
        {
            Id = "card_001",
            CatalogId = "fire_elemental",
            ProfileId = "profile_123",
            Rarity = "epic",
            Level = 3,
            Xp = 250,
            Upgrades = ["upgrade_1", "upgrade_2"],
            RollJson = "{\"variant\":1}",
            CreatedAt = 1700000000,
            Binding = ContentBinding.SummonerBound,
            BoundToSummonerId = "summoner_fire"
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCardDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Id).IsEqual("card_001");
        AssertThat(result.CatalogId).IsEqual("fire_elemental");
        AssertThat(result.ProfileId).IsEqual("profile_123");
        AssertThat(result.Rarity).IsEqual("epic");
        AssertThat(result.Level).IsEqual(3);
        AssertThat(result.Xp).IsEqual(250);
        AssertThat(result.Upgrades).Contains("upgrade_1");
        AssertThat(result.RollJson).IsEqual("{\"variant\":1}");
        AssertThat(result.CreatedAt).IsEqual(1700000000);
        AssertThat(result.Binding).IsEqual(ContentBinding.SummonerBound);
        AssertThat(result.BoundToSummonerId).IsEqual("summoner_fire");
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
            ["catalog_id"] = "fire_elemental"
        };
        AssertThat(DtoConverters.FromCardDict(dict)).IsNull();
    }

    [TestCase]
    public void CardInstance_ToDict_SerializesBindingAsInt()
    {
        var card = new CardInstanceData
        {
            Id = "card_001",
            CatalogId = "test",
            Binding = ContentBinding.SummonerBound
        };

        var dict = DtoConverters.ToDict(card);
        AssertThat(dict["binding"].AsInt32()).IsEqual(1);
    }

    // =========================================================================
    // ItemInstanceData Tests
    // =========================================================================

    [TestCase]
    public void ItemInstance_RoundTrip_PreservesAllFields()
    {
        var original = new ItemInstanceData
        {
            Id = "item_001",
            CatalogId = "sword_of_fire",
            EquippedBySummonerId = "summoner_fire",
            BoundToSummonerId = "summoner_fire",
            EquippedSlot = ItemSlot.Weapon
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromItemDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Id).IsEqual("item_001");
        AssertThat(result.CatalogId).IsEqual("sword_of_fire");
        AssertThat(result.EquippedBySummonerId).IsEqual("summoner_fire");
        AssertThat(result.BoundToSummonerId).IsEqual("summoner_fire");
        AssertThat(result.EquippedSlot).IsEqual(ItemSlot.Weapon);
    }

    [TestCase]
    public void ItemInstance_RoundTrip_HandlesNullSlot()
    {
        var original = new ItemInstanceData
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
        var item = new ItemInstanceData
        {
            Id = "item_001",
            CatalogId = "test",
            EquippedSlot = ItemSlot.Vestments
        };

        var dict = DtoConverters.ToDict(item);
        AssertThat(dict["slot"].AsString()).IsEqual("vestments");
    }

    // =========================================================================
    // DeckData Tests
    // =========================================================================

    [TestCase]
    public void Deck_RoundTrip_PreservesAllFields()
    {
        var original = new DeckData
        {
            Id = "deck_001",
            ProfileId = "profile_123",
            SummonerId = "summoner_fire",
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
        AssertThat(result.SummonerId).IsEqual("summoner_fire");
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
            ["summoner_id"] = "summoner_fire"
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
    // CampaignProgressData Tests
    // =========================================================================

    [TestCase]
    public void CampaignProgress_RoundTrip_PreservesAllFields()
    {
        var original = new CampaignProgressData
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
}
