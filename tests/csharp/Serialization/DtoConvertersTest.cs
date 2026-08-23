namespace Fateforged.Tests.Serialization;

using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Deck;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;

/// <summary>
/// Tests for DtoConverters - centralized Dict↔Domain conversions.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
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
            SummonerId = SummonerIds.Cole,
            Level = 5,
            Xp = 1500,
            EquippedItems = new Dictionary<ItemSlot, ItemId?>
            {
                [ItemSlot.Wand] = new ItemId("item_001"),
                [ItemSlot.Ring1] = null,
                [ItemSlot.Ring2] = new ItemId("item_002"),
                [ItemSlot.Robes] = null,
            },
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromSummonerDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat((string)result!.SummonerId).IsEqual("summoner_cole");
        AssertThat(result.Level).IsEqual(5);
        AssertThat(result.Xp).IsEqual(1500);
        AssertThat((string?)result.EquippedItems[ItemSlot.Wand]).IsEqual("item_001");
        AssertThat(result.EquippedItems[ItemSlot.Ring1]).IsNull();
        AssertThat((string?)result.EquippedItems[ItemSlot.Ring2]).IsEqual("item_002");
        AssertThat(result.EquippedItems[ItemSlot.Robes]).IsNull();
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
        var dict = new Godot.Collections.Dictionary { ["level"] = 1, ["xp"] = 0 };
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
            Id = new CardInstanceId("card_001"),
            CatalogId = new CardId("fire_wisp"),
            ProfileId = new ProfileId("profile_123"),
            Rarity = "epic",
            Level = 3,
            Xp = 250,
            Traits = [new CardTraitId("upgrade_1"), new CardTraitId("upgrade_2")],
            RollJson = "{\"variant\":1}",
            CreatedAt = 1700000000,
            Binding = ContentBinding.SummonerBound,
            BoundToSummonerId = new SummonerId("summoner_cole"),
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCardDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat((string)result!.Id).IsEqual("card_001");
        AssertThat((string)result.CatalogId).IsEqual("fire_wisp");
        AssertThat((string)result.ProfileId).IsEqual("profile_123");
        AssertThat(result.Rarity).IsEqual("epic");
        AssertThat(result.Level).IsEqual(3);
        AssertThat(result.Xp).IsEqual(250);
        AssertThat(result.Traits).Contains(new CardTraitId("upgrade_1"));
        AssertThat(result.RollJson).IsEqual("{\"variant\":1}");
        AssertThat(result.CreatedAt).IsEqual(1700000000);
        AssertThat(result.Binding).IsEqual(ContentBinding.SummonerBound);
        AssertThat((string?)result.BoundToSummonerId).IsEqual("summoner_cole");
    }

    [TestCase]
    public void CardInstance_FromDict_ReturnsNullForMissingRequiredFields()
    {
        // Missing catalog_id
        var dict = new Godot.Collections.Dictionary { ["id"] = "card_001" };
        AssertThat(DtoConverters.FromCardDict(dict)).IsNull();

        // Missing id
        dict = new Godot.Collections.Dictionary { ["catalog_id"] = "fire_wisp" };
        AssertThat(DtoConverters.FromCardDict(dict)).IsNull();
    }

    [TestCase]
    public void CardInstance_ToDict_SerializesBindingAsInt()
    {
        var card = new CardInstance
        {
            Id = new CardInstanceId("card_001"),
            CatalogId = new CardId("test"),
            Binding = ContentBinding.SummonerBound,
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
            Id = new ItemId("item_001"),
            CatalogId = new ItemId("sword_of_fire"),
            EquippedBySummonerId = new SummonerId("summoner_cole"),
            BoundToSummonerId = new SummonerId("summoner_cole"),
            EquippedSlot = ItemSlot.Wand,
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromItemDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat((string)result!.Id).IsEqual("item_001");
        AssertThat((string)result.CatalogId).IsEqual("sword_of_fire");
        AssertThat((string?)result.EquippedBySummonerId).IsEqual("summoner_cole");
        AssertThat((string?)result.BoundToSummonerId).IsEqual("summoner_cole");
        AssertThat(result.EquippedSlot).IsEqual(ItemSlot.Wand);
    }

    [TestCase]
    public void ItemInstance_RoundTrip_HandlesNullSlot()
    {
        var original = new ItemInstance
        {
            Id = new ItemId("item_001"),
            CatalogId = new ItemId("ring_of_power"),
            EquippedSlot = null,
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
            Id = new ItemId("item_001"),
            CatalogId = new ItemId("test"),
            EquippedSlot = ItemSlot.Robes,
        };

        var dict = DtoConverters.ToDict(item);
        AssertThat(dict["slot"].AsString()).IsEqual("robes");
    }

    // =========================================================================
    // Deck Tests
    // =========================================================================

    [TestCase]
    public void Deck_RoundTrip_PreservesAllFields()
    {
        var original = new Deck
        {
            Id = new DeckId("deck_001"),
            ProfileId = new ProfileId("profile_123"),
            SummonerId = new SummonerId("summoner_cole"),
            Name = "My Fire Deck",
            Slot = 2,
            IsActive = true,
            CardInstanceIds =
            [
                new CardInstanceId("card_1"),
                new CardInstanceId("card_2"),
                new CardInstanceId("card_3"),
            ],
            UpdatedAt = 1700000000,
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromDeckDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat((string)result!.Id).IsEqual("deck_001");
        AssertThat((string)result.ProfileId).IsEqual("profile_123");
        AssertThat((string)result.SummonerId).IsEqual("summoner_cole");
        AssertThat(result.Name).IsEqual("My Fire Deck");
        AssertThat(result.Slot).IsEqual(2);
        AssertThat(result.IsActive).IsTrue();
        AssertThat(result.CardInstanceIds).Contains(new CardInstanceId("card_1"));
        AssertThat(result.CardInstanceIds).Contains(new CardInstanceId("card_2"));
        AssertThat(result.CardInstanceIds).Contains(new CardInstanceId("card_3"));
        AssertThat(result.UpdatedAt).IsEqual(1700000000);
    }

    [TestCase]
    public void Deck_FromDict_ReturnsNullForMissingRequiredFields()
    {
        // Missing id
        var dict = new Godot.Collections.Dictionary { ["summoner_id"] = "summoner_cole" };
        AssertThat(DtoConverters.FromDeckDict(dict)).IsNull();

        // Missing summoner_id
        dict = new Godot.Collections.Dictionary { ["id"] = "deck_001" };
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
            CompletedBattles = [new BattleId("battle_1"), new BattleId("battle_2")],
            Gold = 500,
            Academy = new AcademyProgress
            {
                CurrentYear = 1,
                CurrentSemester = 2,
                RemainingEnrollments = 3,
                CompletedCourses = [CourseIds.IntroductionToMagic101],
                EnrolledCourses = [CourseIds.PracticalSpellcraft],
                DiscoveredCourses = [CourseIds.IntroToFire],
                TrackedQuestId = "academy:practical_spellcraft",
                AssessmentOutcomes = new Dictionary<string, AcademyActivityOutcome>
                {
                    ["magic_101_exam"] = AcademyActivityOutcome.Victory,
                },
                ActivityLoadouts = new Dictionary<string, AcademyActivityLoadoutState>
                {
                    ["practical_spellcraft:practice"] = new AcademyActivityLoadoutState
                    {
                        SelectedCardInstanceIds = [new CardInstanceId("card_1")],
                    },
                },
                RewardFlags = new Dictionary<string, int> { ["spellcraft_intro"] = 1 },
                Transcript =
                [
                    new AcademyTranscriptEntry
                    {
                        CourseId = CourseIds.IntroductionToMagic101,
                        Grade = "pass",
                        Honors = false,
                        SemesterKey = "year_1_semester_1",
                    },
                ],
                HonorsEligibility = new Dictionary<string, bool> { ["affinity_fire"] = true },
                ShopPurchases = new Dictionary<string, int> { ["starter_reagent"] = 1 },
                CourseActivityIndex = new Dictionary<string, int>
                {
                    [(string)CourseIds.PracticalSpellcraft] = 2,
                },
            },
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.CompletedBattles).Contains(new BattleId("battle_1"));
        AssertThat(result.CompletedBattles).Contains(new BattleId("battle_2"));
        AssertThat(result.Gold).IsEqual(500);
        AssertThat(result.Academy.CurrentYear).IsEqual(1);
        AssertThat(result.Academy.CurrentSemester).IsEqual(2);
        AssertThat(result.Academy.RemainingEnrollments).IsEqual(3);
        AssertThat(result.Academy.CompletedCourses).Contains(CourseIds.IntroductionToMagic101);
        AssertThat(result.Academy.EnrolledCourses).Contains(CourseIds.PracticalSpellcraft);
        AssertThat(result.Academy.DiscoveredCourses).Contains(CourseIds.IntroToFire);
        AssertThat(result.Academy.TrackedQuestId).IsEqual("academy:practical_spellcraft");
        AssertThat(result.Academy.AssessmentOutcomes["magic_101_exam"])
            .IsEqual(AcademyActivityOutcome.Victory);
        AssertThat(
                result
                    .Academy
                    .ActivityLoadouts["practical_spellcraft:practice"]
                    .SelectedCardInstanceIds
            )
            .Contains(new CardInstanceId("card_1"));
        AssertThat(result.Academy.RewardFlags["spellcraft_intro"]).IsEqual(1);
        AssertThat(result.Academy.Transcript).HasSize(1);
        AssertThat(result.Academy.Transcript[0].CourseId).IsEqual(CourseIds.IntroductionToMagic101);
        AssertThat(result.Academy.Transcript[0].Grade).IsEqual("pass");
        AssertThat(result.Academy.HonorsEligibility["affinity_fire"]).IsTrue();
        AssertThat(result.Academy.ShopPurchases["starter_reagent"]).IsEqual(1);
        AssertThat(result.Academy.CourseActivityIndex[(string)CourseIds.PracticalSpellcraft])
            .IsEqual(2);
    }

    [TestCase]
    public void CampaignProgress_FromDict_ReturnsDefaultForEmptyDict()
    {
        var result = DtoConverters.FromCampaignDict(new Godot.Collections.Dictionary());
        AssertThat(result).IsNotNull();
        AssertThat(result!.CompletedBattles).IsEmpty();
        AssertThat(result.Gold).IsEqual(0);
        AssertThat(result.Academy.CurrentYear).IsEqual(1);
        AssertThat(result.Academy.CurrentSemester).IsEqual(1);
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
            CompletedBattles = [new BattleId("battle_1")],
            Gold = 100,
            Choices = new Dictionary<NodeId, ChoiceId>
            {
                [new NodeId("node_choice_1")] = new ChoiceId("option_a"),
                [new NodeId("node_choice_2")] = new ChoiceId("option_b"),
            },
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.Choices).HasSize(2);
        AssertThat(result.Choices[new NodeId("node_choice_1")]).IsEqual(new ChoiceId("option_a"));
        AssertThat(result.Choices[new NodeId("node_choice_2")]).IsEqual(new ChoiceId("option_b"));
    }

    [TestCase]
    public void CampaignProgress_RoundTrip_PreservesCaravanPurchases()
    {
        var original = new CampaignProgress
        {
            CompletedBattles = [new BattleId("battle_1")],
            Gold = 200,
            CaravanPurchases = ["offering_sword", "offering_shield"],
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromCampaignDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result!.CaravanPurchases).HasSize(2);
        AssertThat(result.CaravanPurchases).Contains("offering_sword");
        AssertThat(result.CaravanPurchases).Contains("offering_shield");
    }

    [TestCase]
    public void CampaignProgress_RoundTrip_HandlesEmptyChoices()
    {
        var original = new CampaignProgress
        {
            CompletedBattles = [new BattleId("battle_1")],
            Gold = 50,
            Choices = [],
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
        var original = new AccountMeta
        {
            SelectedDeck = "deck_001",
            RankedDecksBySummoner = new Dictionary<string, string>
            {
                ["summoner_cole"] = "ranked_deck_001",
                ["summoner_selene"] = "ranked_deck_002",
            },
            SelectedSummoner = "summoner_cole",
            AnalyticsOptIn = true,
            TutorialFlags = new Dictionary<string, bool>
            {
                ["intro_completed"] = true,
                ["combat_tutorial"] = false,
            },
            NarrativeFlags = new Dictionary<string, bool> { ["merlin_intro"] = true },
            Achievements = new Dictionary<string, object>
            {
                ["kills"] = 42L,
                ["win_rate"] = 0.75,
                ["has_trophy"] = true,
                ["title"] = "Champion",
            },
        };

        var dict = DtoConverters.ToDict(original);
        var result = DtoConverters.FromMetaDict(dict);

        AssertThat(result).IsNotNull();
        AssertThat(result.SelectedDeck).IsEqual("deck_001");
        AssertThat(result.RankedDecksBySummoner["summoner_cole"]).IsEqual("ranked_deck_001");
        AssertThat(result.RankedDecksBySummoner["summoner_selene"]).IsEqual("ranked_deck_002");
        AssertThat(result.SelectedSummoner).IsEqual("summoner_cole");
        AssertThat(result.AnalyticsOptIn).IsTrue();
        AssertThat(result.TutorialFlags["intro_completed"]).IsTrue();
        AssertThat(result.TutorialFlags["combat_tutorial"]).IsFalse();
        AssertThat(result.NarrativeFlags["merlin_intro"]).IsTrue();
        // Achievement values preserve their types
        AssertThat((long)result.Achievements["kills"]).IsEqual(42L);
        AssertThat((double)result.Achievements["win_rate"]).IsEqual(0.75);
        AssertThat((bool)result.Achievements["has_trophy"]).IsEqual(true);
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
            ["string_value"] = "test",
        };
        var dict = new Godot.Collections.Dictionary
        {
            ["selected_deck"] = "",
            ["selected_summoner"] = "",
            ["analytics_opt_in"] = false,
            ["achievements"] = achievementsDict,
        };

        var result = DtoConverters.FromMetaDict(dict);

        // Verify types are preserved (int becomes long in C#)
        AssertThat(result.Achievements["int_value"] is long).IsTrue();
        AssertThat(result.Achievements["float_value"] is double).IsTrue();
        AssertThat(result.Achievements["bool_value"] is bool).IsTrue();
        AssertThat(result.Achievements["string_value"] is string).IsTrue();
    }

    // =========================================================================
    // Settings Tests
    // =========================================================================

    [TestCase]
    public void Settings_RoundTrip_PreservesSupportedValues()
    {
        var original = new Settings
        {
            MasterVolume = 0.8f,
            MusicVolume = 0.7f,
            SfxVolume = 0.6f,
            MuteWhenUnfocused = true,
            WindowMode = "windowed",
            ResolutionWidth = 1600,
            ResolutionHeight = 900,
            VsyncEnabled = false,
            FpsLimit = 120,
            EdgePanEnabled = false,
            CameraSpeed = 1.5f,
            ReduceCameraMotion = true,
            UiScale = 1.15f,
            Lang = "en",
        };

        var result = DtoConverters.FromSettingsDict(DtoConverters.ToDict(original));

        AssertThat(result.MasterVolume).IsEqual(0.8f);
        AssertThat(result.MusicVolume).IsEqual(0.7f);
        AssertThat(result.SfxVolume).IsEqual(0.6f);
        AssertThat(result.MuteWhenUnfocused).IsTrue();
        AssertThat(result.WindowMode).IsEqual("windowed");
        AssertThat(result.ResolutionWidth).IsEqual(1600);
        AssertThat(result.ResolutionHeight).IsEqual(900);
        AssertThat(result.VsyncEnabled).IsFalse();
        AssertThat(result.FpsLimit).IsEqual(120);
        AssertThat(result.EdgePanEnabled).IsFalse();
        AssertThat(result.CameraSpeed).IsEqual(1.5f);
        AssertThat(result.ReduceCameraMotion).IsTrue();
        AssertThat(result.UiScale).IsEqual(1.15f);
        AssertThat(result.Lang).IsEqual("en");
    }

    // =========================================================================
    // MetaUpdate Tests
    // =========================================================================

    [TestCase]
    public void MetaUpdate_ToDict_OnlyIncludesNonNullFields()
    {
        var update = new MetaUpdate
        {
            SelectedSummoner = "summoner_selene",
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
            RankedDecksBySummoner = new Dictionary<string, string>
            {
                ["summoner_cole"] = "ranked_deck_003",
            },
            SelectedSummoner = "summoner_cole",
            AnalyticsOptIn = true,
            TutorialFlags = new Dictionary<string, bool> { ["flag1"] = true },
            Achievements = new Dictionary<string, object> { ["score"] = 100 },
        };

        var dict = DtoConverters.ToDict(update);

        AssertThat(dict.ContainsKey("selected_deck")).IsTrue();
        AssertThat(dict.ContainsKey("selected_summoner")).IsTrue();
        AssertThat(dict.ContainsKey("ranked_decks_by_summoner")).IsTrue();
        AssertThat(dict.ContainsKey("analytics_opt_in")).IsTrue();
        AssertThat(dict.ContainsKey("tutorial_flags")).IsTrue();
        AssertThat(dict.ContainsKey("achievements")).IsTrue();
        AssertThat(dict["selected_deck"].AsString()).IsEqual("deck_002");
        AssertThat(
                dict["ranked_decks_by_summoner"]
                    .AsGodotDictionary()["summoner_cole"]
                    .AsString()
            )
            .IsEqual("ranked_deck_003");
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
            Xp = 500,
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
            Traits = [new CardTraitId("upgrade_1"), new CardTraitId("upgrade_2")],
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
