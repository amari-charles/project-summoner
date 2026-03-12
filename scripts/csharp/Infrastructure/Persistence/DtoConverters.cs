using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Shop;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Deck;
using Godot;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Centralized converters for Godot.Collections.Dictionary ↔ Domain model conversions.
/// All ProfileRepository conversion logic is consolidated here for consistency and testability.
/// </summary>
public static class DtoConverters
{
    // =========================================================================
    // SummonerInstance
    // =========================================================================

    /// <summary>Convert SummonerInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(SummonerInstance instance)
    {
        var equippedDict = new Godot.Collections.Dictionary();
        foreach (var (slot, itemId) in instance.EquippedItems)
        {
            equippedDict[slot.ToString().ToLowerInvariant()] = itemId.HasValue
                ? (string)itemId.Value
                : "";
        }

        return new Godot.Collections.Dictionary
        {
            ["summoner_id"] = (string)instance.SummonerId,
            ["level"] = instance.Level,
            ["xp"] = instance.Xp,
            ["equipped_items"] = equippedDict,
            ["acquired_trait_ids"] = ToGodotArray(instance.AcquiredTraitIds.Select(t => (string)t)),
            ["unspent_trait_points"] = instance.UnspentTraitPoints,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to SummonerInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static SummonerInstance? FromSummonerDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var summonerId = GetRequiredString(dict, "summoner_id");
        if (summonerId == null)
            return null;

        // Deserialize equipped_items (string keys from GDScript → ItemSlot enum keys)
        var equippedItems = new Dictionary<ItemSlot, ItemId?>
        {
            [ItemSlot.Wand] = null,
            [ItemSlot.Ring1] = null,
            [ItemSlot.Ring2] = null,
            [ItemSlot.Robes] = null,
        };
        if (
            dict.TryGetValue("equipped_items", out var equippedVar)
            && equippedVar.VariantType == Variant.Type.Dictionary
        )
        {
            var equippedDict = equippedVar.AsGodotDictionary();
            foreach (var key in equippedDict.Keys)
            {
                var slotStr = key.AsString();
                var itemIdStr =
                    equippedDict[key].VariantType != Variant.Type.Nil
                        ? equippedDict[key].AsString()
                        : null;
                ItemId? itemId = string.IsNullOrEmpty(itemIdStr) ? null : new ItemId(itemIdStr);

                // Parse slot from string
                if (Enum.TryParse<ItemSlot>(slotStr, ignoreCase: true, out var slot))
                {
                    equippedItems[slot] = itemId;
                }
            }
        }

        var acquiredTraits = new List<TraitId>();
        if (
            dict.TryGetValue("acquired_trait_ids", out var traitsVar)
            && traitsVar.VariantType == Variant.Type.Array
        )
        {
            foreach (var item in traitsVar.AsGodotArray())
            {
                var traitId = item.AsString();
                if (!string.IsNullOrEmpty(traitId))
                    acquiredTraits.Add(new TraitId(traitId));
            }
        }

        return new SummonerInstance
        {
            SummonerId = new SummonerId(summonerId),
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            EquippedItems = equippedItems,
            AcquiredTraitIds = acquiredTraits,
            UnspentTraitPoints = GetInt(dict, "unspent_trait_points", 0),
        };
    }

    // =========================================================================
    // CardInstance
    // =========================================================================

    /// <summary>Convert CardInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CardInstance card)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = (string)card.Id,
            ["catalog_id"] = (string)card.CatalogId,
            ["profile_id"] = (string)card.ProfileId,
            ["rarity"] = card.Rarity,
            ["level"] = card.Level,
            ["xp"] = card.Xp,
            ["upgrades"] = TraitsToGodotArray(card.Traits),
            ["unspent_trait_points"] = card.UnspentTraitPoints,
            ["created_at"] = card.CreatedAt,
            ["binding"] = (int)card.Binding,
        };

        if (card.RollJson != null)
            dict["roll_json"] = card.RollJson;

        if (card.BoundToSummonerId != null)
            dict["bound_to"] = (string)card.BoundToSummonerId.Value;

        return dict;
    }

    /// <summary>
    /// Convert Godot Dictionary to CardInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static CardInstance? FromCardDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var idStr = GetRequiredString(dict, "id");
        var catalogIdStr = GetRequiredString(dict, "catalog_id");
        if (idStr == null || catalogIdStr == null)
            return null;

        var traits = new List<CardTraitId>();
        if (dict.TryGetValue("upgrades", out var upgradesVar))
        {
            var upgradesArr = upgradesVar.AsGodotArray();
            foreach (var u in upgradesArr)
            {
                var traitStr = u.AsString();
                if (!string.IsNullOrEmpty(traitStr))
                {
                    traits.Add(new CardTraitId(traitStr));
                }
            }
        }

        // Parse binding with validation
        var binding = ContentBinding.AccountWide;
        if (dict.TryGetValue("binding", out var bindingVar))
        {
            var bindingInt = bindingVar.AsInt32();
            if (Enum.IsDefined(typeof(ContentBinding), bindingInt))
                binding = (ContentBinding)bindingInt;
        }

        var boundToStr = GetNullableString(dict, "bound_to");
        var profileIdStr = GetString(dict, "profile_id", "");

        return new CardInstance
        {
            Id = new CardInstanceId(idStr),
            CatalogId = new CardId(catalogIdStr),
            ProfileId = string.IsNullOrEmpty(profileIdStr)
                ? ProfileId.None
                : new ProfileId(profileIdStr),
            Rarity = GetString(dict, "rarity", "common"),
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            Traits = traits,
            UnspentTraitPoints = GetInt(dict, "unspent_trait_points", 0),
            RollJson = GetNullableString(dict, "roll_json"),
            CreatedAt = GetLong(dict, "created_at", 0),
            Binding = binding,
            BoundToSummonerId = string.IsNullOrEmpty(boundToStr)
                ? null
                : new SummonerId(boundToStr),
        };
    }

    // =========================================================================
    // ItemInstance
    // =========================================================================

    /// <summary>Convert ItemInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(ItemInstance item)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)item.Id,
            ["catalog_id"] = (string)item.CatalogId,
            ["equipped_by"] = item.EquippedBySummonerId.HasValue
                ? (string)item.EquippedBySummonerId.Value
                : "",
            ["bound_to"] = item.BoundToSummonerId.HasValue
                ? (string)item.BoundToSummonerId.Value
                : "",
            ["slot"] = item.EquippedSlot.HasValue
                ? item.EquippedSlot.Value.ToString().ToLowerInvariant()
                : "",
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to ItemInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static ItemInstance? FromItemDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var idStr = GetRequiredString(dict, "id");
        var catalogIdStr = GetRequiredString(dict, "catalog_id");
        if (idStr == null || catalogIdStr == null)
            return null;

        var equippedByStr = GetNullableString(dict, "equipped_by");
        var boundToStr = GetNullableString(dict, "bound_to");

        return new ItemInstance
        {
            Id = new ItemId(idStr),
            CatalogId = new ItemId(catalogIdStr),
            EquippedBySummonerId = string.IsNullOrEmpty(equippedByStr)
                ? null
                : new SummonerId(equippedByStr),
            BoundToSummonerId = string.IsNullOrEmpty(boundToStr)
                ? null
                : new SummonerId(boundToStr),
            EquippedSlot = ParseNullableSlot(GetNullableString(dict, "slot")),
        };
    }

    // =========================================================================
    // Deck
    // =========================================================================

    /// <summary>Convert Deck to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Deck deck)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = (string)deck.Id,
            ["profile_id"] = (string)deck.ProfileId,
            ["summoner_id"] = (string)deck.SummonerId,
            ["name"] = deck.Name,
            ["slot"] = deck.Slot,
            ["is_active"] = deck.IsActive,
            ["card_instance_ids"] = CardInstanceIdsToGodotArray(deck.CardInstanceIds),
            ["updated_at"] = deck.UpdatedAt,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Deck.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static Deck? FromDeckDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var idStr = GetRequiredString(dict, "id");
        var summonerIdStr = GetRequiredString(dict, "summoner_id");
        if (idStr == null || summonerIdStr == null)
            return null;

        var cardIds = new List<CardInstanceId>();
        if (dict.TryGetValue("card_instance_ids", out var cardsVar))
        {
            var cardsArr = cardsVar.AsGodotArray();
            foreach (var c in cardsArr)
            {
                cardIds.Add(new CardInstanceId(c.AsString()));
            }
        }

        var profileIdStr = GetString(dict, "profile_id", "");

        return new Deck
        {
            Id = new DeckId(idStr),
            ProfileId = string.IsNullOrEmpty(profileIdStr)
                ? ProfileId.None
                : new ProfileId(profileIdStr),
            SummonerId = new SummonerId(summonerIdStr),
            Name = GetString(dict, "name", "Deck"),
            Slot = GetInt(dict, "slot", 0),
            IsActive = GetBool(dict, "is_active", false),
            CardInstanceIds = cardIds,
            UpdatedAt = GetLong(dict, "updated_at", 0),
        };
    }

    // =========================================================================
    // CampaignProgress
    // =========================================================================

    /// <summary>Convert CampaignProgress to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CampaignProgress progress)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["completed_battles"] = ToGodotArray(progress.CompletedBattles.Select(b => (string)b)),
            ["current_battle"] = progress.CurrentBattle.HasValue
                ? (string)progress.CurrentBattle.Value
                : "",
            ["gold"] = progress.Gold,
        };

        // Add choices if present
        if (progress.Choices.Count > 0)
        {
            var choicesDict = new Godot.Collections.Dictionary();
            foreach (var (nodeId, choiceId) in progress.Choices)
            {
                choicesDict[(string)nodeId] = (string)choiceId;
            }
            dict["choices"] = choicesDict;
        }

        // Add pending_reward if present
        if (progress.PendingReward != null)
        {
            dict["pending_reward"] = ToDict(progress.PendingReward);
        }

        // Add story_arcs if present
        if (progress.StoryArcs.Count > 0)
        {
            var arcsDict = new Godot.Collections.Dictionary();
            foreach (var (arcId, arcProgress) in progress.StoryArcs)
            {
                arcsDict[arcId] = ToDict(arcProgress);
            }
            dict["story_arcs"] = arcsDict;
        }

        return dict;
    }

    /// <summary>Convert PendingRewardData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(PendingRewardData pending)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["battle_id"] = (string)pending.BattleId,
            ["reward_type"] = pending.RewardType.ToStringId(),
            ["choice_index"] = pending.ChoiceIndex,
            ["chosen_catalog_id"] = pending.ChosenCatalogId,
        };

        if (pending.CaravanPurchases.Count > 0)
        {
            var arr = new Godot.Collections.Array();
            foreach (var p in pending.CaravanPurchases)
                arr.Add(p);
            dict["caravan_purchases"] = arr;
        }

        return dict;
    }

    /// <summary>Convert StoryArcProgress to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(StoryArcProgress arcProgress)
    {
        var flagsDict = new Godot.Collections.Dictionary();
        foreach (var (key, value) in arcProgress.Flags)
        {
            flagsDict[key] = ObjectToVariant(value);
        }

        return new Godot.Collections.Dictionary
        {
            ["completed_events"] = ToGodotArray(arcProgress.CompletedEvents.Select(e => (string)e)),
            ["current_event"] = arcProgress.CurrentEvent.HasValue
                ? (string)arcProgress.CurrentEvent.Value
                : "",
            ["flags"] = flagsDict,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to CampaignProgress.
    /// Returns null if dict is null (but empty dict returns default data).
    /// </summary>
    public static CampaignProgress? FromCampaignDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null)
            return null;
        if (dict.Count == 0)
            return new CampaignProgress();

        var completed = new List<BattleId>();
        if (dict.TryGetValue("completed_battles", out var completedVar))
        {
            var completedArr = completedVar.AsGodotArray();
            foreach (var c in completedArr)
            {
                completed.Add(new BattleId(c.AsString()));
            }
        }

        // Parse pending_reward if present
        PendingRewardData? pendingReward = null;
        if (
            dict.TryGetValue("pending_reward", out var rewardVar)
            && rewardVar.VariantType == Variant.Type.Dictionary
        )
        {
            var rewardDict = rewardVar.AsGodotDictionary();
            pendingReward = new PendingRewardData
            {
                BattleId = new BattleId(GetString(rewardDict, "battle_id", "")),
                RewardType = RewardTypeExtensions.FromStringId(
                    GetString(rewardDict, "reward_type", "fixed")
                ),
                ChoiceIndex = GetInt(rewardDict, "choice_index", -1),
                ChosenCatalogId = GetString(rewardDict, "chosen_catalog_id", ""),
            };
            if (
                rewardDict.TryGetValue("caravan_purchases", out var purchasesVar)
                && purchasesVar.VariantType == Variant.Type.Array
            )
            {
                foreach (var p in purchasesVar.AsGodotArray())
                    pendingReward.CaravanPurchases.Add(p.AsString());
            }
        }

        // Parse story_arcs if present
        var storyArcs = new Dictionary<string, StoryArcProgress>();
        if (
            dict.TryGetValue("story_arcs", out var arcsVar)
            && arcsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var arcsDict = arcsVar.AsGodotDictionary();
            foreach (var key in arcsDict.Keys)
            {
                var arcDict = arcsDict[key].AsGodotDictionary();
                var arcProgress = FromStoryArcDict(arcDict);
                if (arcProgress != null)
                {
                    storyArcs[key.AsString()] = arcProgress;
                }
            }
        }

        // Parse choices if present
        var choices = new Dictionary<NodeId, ChoiceId>();
        if (
            dict.TryGetValue("choices", out var choicesVar)
            && choicesVar.VariantType == Variant.Type.Dictionary
        )
        {
            var choicesDict = choicesVar.AsGodotDictionary();
            foreach (var key in choicesDict.Keys)
            {
                var choiceValue = choicesDict[key];
                if (choiceValue.VariantType != Variant.Type.Nil)
                {
                    choices[new NodeId(key.AsString())] = new ChoiceId(choiceValue.AsString());
                }
            }
        }

        // Parse current_battle (nullable)
        var currentBattleStr = GetNullableString(dict, "current_battle");
        BattleId? currentBattle = string.IsNullOrEmpty(currentBattleStr)
            ? null
            : new BattleId(currentBattleStr);

        return new CampaignProgress
        {
            CompletedBattles = completed,
            CurrentBattle = currentBattle,
            Gold = GetInt(dict, "gold", 0),
            PendingReward = pendingReward,
            StoryArcs = storyArcs,
            Choices = choices,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to StoryArcProgress.
    /// </summary>
    public static StoryArcProgress? FromStoryArcDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var completedEvents = new List<EventId>();
        if (dict.TryGetValue("completed_events", out var eventsVar))
        {
            var eventsArr = eventsVar.AsGodotArray();
            foreach (var e in eventsArr)
            {
                completedEvents.Add(new EventId(e.AsString()));
            }
        }

        var flags = new Dictionary<string, object>();
        if (
            dict.TryGetValue("flags", out var flagsVar)
            && flagsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var flagsDict = flagsVar.AsGodotDictionary();
            foreach (var key in flagsDict.Keys)
            {
                flags[key.AsString()] = flagsDict[key].Obj ?? flagsDict[key].AsString();
            }
        }

        // Parse current_event (nullable)
        var currentEventStr = GetNullableString(dict, "current_event");
        EventId? currentEvent = string.IsNullOrEmpty(currentEventStr)
            ? null
            : new EventId(currentEventStr);

        return new StoryArcProgress
        {
            CompletedEvents = completedEvents,
            CurrentEvent = currentEvent,
            Flags = flags,
        };
    }

    // =========================================================================
    // Resources
    // =========================================================================

    /// <summary>Convert Resources to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Resources resources)
    {
        return new Godot.Collections.Dictionary
        {
            ["gold"] = resources.Gold,
            ["gems"] = resources.Gems,
            ["essence"] = resources.Essence,
            ["fragments"] = resources.Fragments,
            ["profile_id"] = (string)resources.ProfileId,
            ["updated_at"] = resources.UpdatedAt,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Resources.
    /// Returns default Resources if dict is null or empty.
    /// </summary>
    public static Resources FromResourcesDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new Resources();

        var profileIdStr = GetString(dict, "profile_id", "");

        return new Resources
        {
            Gold = GetInt(dict, "gold", 0),
            Gems = GetInt(dict, "gems", 0),
            Essence = GetInt(dict, "essence", 0),
            Fragments = GetInt(dict, "fragments", 0),
            ProfileId = string.IsNullOrEmpty(profileIdStr)
                ? ProfileId.None
                : new ProfileId(profileIdStr),
            UpdatedAt = GetLong(dict, "updated_at", 0),
        };
    }

    // =========================================================================
    // Settings
    // =========================================================================

    /// <summary>Convert Settings to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Settings settings)
    {
        return new Godot.Collections.Dictionary
        {
            ["sfx_volume"] = settings.SfxVolume,
            ["music_volume"] = settings.MusicVolume,
            ["lang"] = settings.Lang,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Settings.
    /// Returns default Settings if dict is null or empty.
    /// </summary>
    public static Settings FromSettingsDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new Settings();

        return new Settings
        {
            SfxVolume = GetFloat(dict, "sfx_volume", 1.0f),
            MusicVolume = GetFloat(dict, "music_volume", 1.0f),
            Lang = GetString(dict, "lang", "en"),
        };
    }

    // =========================================================================
    // ShopRefreshState
    // =========================================================================

    /// <summary>Convert ShopRefreshState to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(ShopRefreshState state)
    {
        return new Godot.Collections.Dictionary
        {
            ["refresh_epoch"] = state.RefreshEpoch,
            ["last_refresh_at"] = state.LastRefreshAt,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to ShopRefreshState.
    /// Returns default ShopRefreshState if dict is null or empty.
    /// </summary>
    public static ShopRefreshState FromShopRefreshStateDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new ShopRefreshState();

        return new ShopRefreshState
        {
            RefreshEpoch = GetInt(dict, "refresh_epoch", 0),
            LastRefreshAt = GetString(dict, "last_refresh_at", ""),
        };
    }

    // =========================================================================
    // Meta
    // =========================================================================

    /// <summary>Convert Meta to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(AccountMeta meta)
    {
        var tutorialDict = new Godot.Collections.Dictionary();
        foreach (var (key, value) in meta.TutorialFlags)
        {
            tutorialDict[key] = value;
        }

        var achievementsDict = new Godot.Collections.Dictionary();
        foreach (var (key, value) in meta.Achievements)
        {
            achievementsDict[key] = ObjectToVariant(value);
        }

        return new Godot.Collections.Dictionary
        {
            ["selected_deck"] = meta.SelectedDeck,
            ["selected_summoner"] = meta.SelectedSummoner,
            ["analytics_opt_in"] = meta.AnalyticsOptIn,
            ["tutorial_flags"] = tutorialDict,
            ["achievements"] = achievementsDict,
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Meta.
    /// Returns default Meta if dict is null or empty.
    /// </summary>
    public static AccountMeta FromMetaDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new AccountMeta();

        var meta = new AccountMeta
        {
            SelectedDeck = GetString(dict, "selected_deck", ""),
            SelectedSummoner = GetString(dict, "selected_summoner", ""),
            AnalyticsOptIn = GetBool(dict, "analytics_opt_in", false),
        };

        // Convert tutorial_flags if present
        if (
            dict.TryGetValue("tutorial_flags", out var tutorialVar)
            && tutorialVar.VariantType == Variant.Type.Dictionary
        )
        {
            var tutorialDict = tutorialVar.AsGodotDictionary();
            foreach (var key in tutorialDict.Keys)
            {
                var value = tutorialDict[key];
                if (value.VariantType == Variant.Type.Bool)
                {
                    meta.TutorialFlags[key.AsString()] = value.AsBool();
                }
            }
        }

        // Convert achievements if present
        if (
            dict.TryGetValue("achievements", out var achievementsVar)
            && achievementsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var achievementsDict = achievementsVar.AsGodotDictionary();
            foreach (var key in achievementsDict.Keys)
            {
                var value = achievementsDict[key];
                // Properly handle different value types to preserve type information
                object achievementValue = value.VariantType switch
                {
                    Variant.Type.Int => value.AsInt64(),
                    Variant.Type.Float => value.AsDouble(),
                    Variant.Type.Bool => value.AsBool(),
                    Variant.Type.String => value.AsString(),
                    _ => value.AsString(), // Fallback for unexpected types
                };
                meta.Achievements[key.AsString()] = achievementValue;
            }
        }

        return meta;
    }

    /// <summary>
    /// Convert MetaUpdate to Godot Dictionary for GDScript.
    /// Only includes non-null fields.
    /// </summary>
    public static Godot.Collections.Dictionary ToDict(MetaUpdate update)
    {
        var dict = new Godot.Collections.Dictionary();

        if (update.SelectedDeck != null)
            dict["selected_deck"] = update.SelectedDeck;

        if (update.SelectedSummoner != null)
            dict["selected_summoner"] = update.SelectedSummoner;

        if (update.AnalyticsOptIn.HasValue)
            dict["analytics_opt_in"] = update.AnalyticsOptIn.Value;

        if (update.TutorialFlags != null)
        {
            var tutorialDict = new Godot.Collections.Dictionary();
            foreach (var (key, value) in update.TutorialFlags)
                tutorialDict[key] = value;
            dict["tutorial_flags"] = tutorialDict;
        }

        if (update.Achievements != null)
        {
            var achievementsDict = new Godot.Collections.Dictionary();
            foreach (var (key, value) in update.Achievements)
                achievementsDict[key] = ObjectToVariant(value);
            dict["achievements"] = achievementsDict;
        }

        return dict;
    }

    // =========================================================================
    // CardUpdate
    // =========================================================================

    /// <summary>
    /// Convert CardUpdate to Godot Dictionary for GDScript.
    /// Only includes non-null fields.
    /// </summary>
    public static Godot.Collections.Dictionary ToDict(CardUpdate update)
    {
        var dict = new Godot.Collections.Dictionary();

        if (update.Xp.HasValue)
            dict["xp"] = update.Xp.Value;

        if (update.Level.HasValue)
            dict["level"] = update.Level.Value;

        if (update.Traits != null)
            dict["upgrades"] = TraitsToGodotArray(update.Traits);

        if (update.UnspentTraitPoints.HasValue)
            dict["unspent_trait_points"] = update.UnspentTraitPoints.Value;

        return dict;
    }

    // =========================================================================
    // ProfileData (partial - for snapshot)
    // =========================================================================

    /// <summary>
    /// Convert Godot Dictionary to partial ProfileData (for snapshot).
    /// NOTE: This is a partial conversion. For complete data, use individual accessor methods.
    /// Populated fields: Version, ProfileId, UpdatedAt, CatalogVersion, Resources, UnlockedSummoners, Meta.
    /// </summary>
    public static ProfileData? FromProfileDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;

        var profileIdStr = GetString(dict, "profile_id", "");

        var profileData = new ProfileData
        {
            Version = GetInt(dict, "version", ProfileData.CurrentVersion),
            ProfileId = string.IsNullOrEmpty(profileIdStr)
                ? ProfileId.None
                : new ProfileId(profileIdStr),
            UpdatedAt = GetLong(dict, "updated_at", 0),
            CatalogVersion = GetString(dict, "catalog_version", "1.0.0"),
        };

        // Convert resources if present
        if (
            dict.TryGetValue("resources", out var resourcesVar)
            && resourcesVar.VariantType == Variant.Type.Dictionary
        )
        {
            var resourcesDict = resourcesVar.AsGodotDictionary();
            profileData.Resources = FromResourcesDict(resourcesDict);
        }

        // Convert unlocked summoners if present
        if (
            dict.TryGetValue("unlocked_summoners", out var summonersVar)
            && summonersVar.VariantType == Variant.Type.Array
        )
        {
            var summonersArr = summonersVar.AsGodotArray();
            foreach (var s in summonersArr)
            {
                profileData.UnlockedSummoners.Add(new SummonerId(s.AsString()));
            }
        }

        // Convert meta if present (contains selected_summoner)
        if (
            dict.TryGetValue("meta", out var metaVar)
            && metaVar.VariantType == Variant.Type.Dictionary
        )
        {
            var metaDict = metaVar.AsGodotDictionary();
            profileData.Meta = FromMetaDict(metaDict);
        }

        return profileData;
    }

    // =========================================================================
    // Helpers - Array Conversion
    // =========================================================================

    /// <summary>Convert IEnumerable of strings to Godot Array.</summary>
    public static Godot.Collections.Array ToGodotArray(IEnumerable<string> items)
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in items)
        {
            arr.Add(item);
        }
        return arr;
    }

    /// <summary>Convert IEnumerable of CardTraitId to Godot Array (as strings).</summary>
    public static Godot.Collections.Array TraitsToGodotArray(IEnumerable<CardTraitId> traits)
    {
        var arr = new Godot.Collections.Array();
        foreach (var trait in traits)
        {
            arr.Add(trait.Value);
        }
        return arr;
    }

    /// <summary>Convert IEnumerable of CardInstanceId to Godot Array (as strings).</summary>
    public static Godot.Collections.Array CardInstanceIdsToGodotArray(
        IEnumerable<CardInstanceId> ids
    )
    {
        var arr = new Godot.Collections.Array();
        foreach (var id in ids)
        {
            arr.Add((string)id);
        }
        return arr;
    }

    // =========================================================================
    // Helpers - Dictionary Value Extraction
    // =========================================================================

    /// <summary>Get required string from dictionary, returns null and logs warning if missing/empty.</summary>
    private static string? GetRequiredString(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            GD.PushWarning($"DtoConverters: Missing required field '{key}'");
            return null;
        }
        if (value.VariantType == Variant.Type.Nil)
        {
            GD.PushWarning($"DtoConverters: Required field '{key}' is null");
            return null;
        }
        var str = value.AsString();
        if (string.IsNullOrEmpty(str))
        {
            GD.PushWarning($"DtoConverters: Required field '{key}' is empty");
            return null;
        }
        return str;
    }

    /// <summary>Get string from dictionary with default value.</summary>
    private static string GetString(
        Godot.Collections.Dictionary dict,
        string key,
        string defaultValue
    )
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        if (value.VariantType == Variant.Type.Nil)
            return defaultValue;
        return value.AsString();
    }

    /// <summary>Get nullable string from dictionary, treating empty strings as null.</summary>
    private static string? GetNullableString(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
            return null;
        if (value.VariantType == Variant.Type.Nil)
            return null;
        var str = value.AsString();
        return string.IsNullOrEmpty(str) ? null : str;
    }

    /// <summary>Get int from dictionary with default value.</summary>
    private static int GetInt(Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsDouble(),
            _ => defaultValue,
        };
    }

    /// <summary>Get long from dictionary with default value.</summary>
    private static long GetLong(Godot.Collections.Dictionary dict, string key, long defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Int => (long)value.AsInt32(),
            Variant.Type.Float => (long)value.AsDouble(),
            _ => defaultValue,
        };
    }

    /// <summary>Get float from dictionary with default value.</summary>
    private static float GetFloat(Godot.Collections.Dictionary dict, string key, float defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue,
        };
    }

    /// <summary>Get bool from dictionary with default value.</summary>
    private static bool GetBool(Godot.Collections.Dictionary dict, string key, bool defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        if (value.VariantType == Variant.Type.Bool)
            return value.AsBool();
        return defaultValue;
    }

    /// <summary>Convert object to Variant-compatible type for Godot Dictionary.</summary>
    public static Variant ObjectToVariant(object? value)
    {
        return value switch
        {
            string s => s,
            int i => i,
            float f => f,
            double d => (float)d,
            bool b => b,
            long l => (int)l,
            _ => value?.ToString() ?? "",
        };
    }

    /// <summary>Parse nullable ItemSlot from string.</summary>
    private static ItemSlot? ParseNullableSlot(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        return Enum.TryParse<ItemSlot>(value, ignoreCase: true, out var slot) ? slot : null;
    }
}
