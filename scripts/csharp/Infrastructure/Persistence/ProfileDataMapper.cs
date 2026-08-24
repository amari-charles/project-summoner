using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Progression;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Shop;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Domain.Progression;
using Fateforged.Meta.Deck;
using Godot;
using GdArray = Godot.Collections.Array;
using GdDict = Godot.Collections.Dictionary;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Bidirectional conversion between Godot.Collections.Dictionary (JSON format)
/// and ProfileData (typed C# model).
/// Handles the deck_cards junction table normalization.
/// </summary>
public static class ProfileDataMapper
{
    // =========================================================================
    // Dictionary → ProfileData
    // =========================================================================

    /// <summary>
    /// Convert a raw profile dictionary (from JSON) to a typed ProfileData object.
    /// </summary>
    public static ProfileData FromDictionary(GdDict dict, string currentProfileId)
    {
        var data = new ProfileData
        {
            Version = GetInt(dict, "version", ProfileData.CurrentVersion),
            ProfileId = new ProfileId(GetString(dict, "profile_id", currentProfileId)),
            UpdatedAt = GetLong(dict, "updated_at", 0),
            CatalogVersion = GetString(dict, "catalog_version", "1.0.0"),
        };

        // Resources
        if (TryGetDict(dict, "resources", out var resourcesDict))
            data.Resources = DtoConverters.FromResourcesDict(resourcesDict);

        // Collection (cards)
        if (TryGetArray(dict, "collection", out var collectionArr))
        {
            foreach (var item in collectionArr)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                    continue;
                var card = DtoConverters.FromCardDict(item.AsGodotDictionary());
                if (card != null)
                    data.Collection.Add(card);
            }
        }

        // Unlocked summoners
        if (TryGetArray(dict, "unlocked_summoners", out var summonersArr))
        {
            foreach (var s in summonersArr)
                data.UnlockedSummoners.Add(new SummonerId(s.AsString()));
        }

        // Summoner instances
        if (TryGetArray(dict, "summoner_instances", out var instancesArr))
        {
            foreach (var item in instancesArr)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                    continue;
                var instance = DtoConverters.FromSummonerDict(item.AsGodotDictionary());
                if (instance != null)
                    data.SummonerInstances.Add(instance);
            }
        }

        // Items
        if (TryGetArray(dict, "items", out var itemsArr))
        {
            foreach (var item in itemsArr)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                    continue;
                var itemData = DtoConverters.FromItemDict(item.AsGodotDictionary());
                if (itemData != null)
                    data.Items.Add(itemData);
            }
        }

        // Decks + deck_cards junction table → inline CardInstanceIds
        MapDecksFromDict(dict, data);

        // Quest and authored-battle progress (per-summoner map)
        if (TryGetDict(dict, "summoner_progress", out var progressDict))
        {
            foreach (var key in progressDict.Keys)
            {
                var keyStr = key.AsString();
                if (keyStr.StartsWith("_"))
                    continue; // Skip legacy keys
                var val = progressDict[key];
                if (val.VariantType != Variant.Type.Dictionary)
                    continue;
                var progress = DtoConverters.FromSummonerProgressDict(val.AsGodotDictionary());
                if (progress != null)
                    data.SummonerProgressMap[keyStr] = progress;
            }
        }

        // Shop purchases
        if (TryGetDict(dict, "shop_purchases", out var purchasesDict))
        {
            foreach (var key in purchasesDict.Keys)
                data.ShopPurchases[key.AsString()] = purchasesDict[key].AsInt32();
        }

        // Shop refresh state
        if (TryGetDict(dict, "shop_refresh_state", out var refreshDict))
        {
            foreach (var key in refreshDict.Keys)
            {
                if (refreshDict[key].VariantType != Variant.Type.Dictionary)
                    continue;
                data.ShopRefreshStateMap[key.AsString()] = DtoConverters.FromShopRefreshStateDict(
                    refreshDict[key].AsGodotDictionary()
                );
            }
        }

        // Cosmetics
        if (TryGetDict(dict, "cosmetics", out var cosmeticsDict))
            data.Cosmetics = MapCosmeticsFromDict(cosmeticsDict);

        // Emotes
        if (TryGetDict(dict, "emotes", out var emotesDict))
            data.Emotes = MapEmotesFromDict(emotesDict);

        // Universal rewards
        if (TryGetDict(dict, "rewards", out var rewardsDict))
            data.Rewards = RewardStateMapper.FromDictionary(rewardsDict);

        // Meta
        if (TryGetDict(dict, "meta", out var metaDict))
            data.Meta = DtoConverters.FromMetaDict(metaDict);

        // Last match
        if (TryGetDict(dict, "last_match", out var matchDict))
            data.LastMatch = MapLastMatchFromDict(matchDict);

        // Settings
        if (TryGetDict(dict, "settings", out var settingsDict))
            data.Settings = DtoConverters.FromSettingsDict(settingsDict);

        // WAL
        if (TryGetArray(dict, "wal", out var walArr))
        {
            foreach (var entry in walArr)
            {
                if (entry.VariantType != Variant.Type.Dictionary)
                    continue;
                var walEntry = new Dictionary<string, object>();
                var walDict = entry.AsGodotDictionary();
                foreach (var key in walDict.Keys)
                    walEntry[key.AsString()] = walDict[key].Obj ?? walDict[key].AsString();
                data.Wal.Add(walEntry);
            }
        }

        return data;
    }

    // =========================================================================
    // ProfileData → Dictionary
    // =========================================================================

    /// <summary>
    /// Convert typed ProfileData to a raw dictionary for JSON serialization.
    /// Produces the normalized deck + deck_cards format for file compatibility.
    /// </summary>
    public static GdDict ToDictionary(ProfileData data)
    {
        var dict = new GdDict
        {
            ["version"] = data.Version,
            ["profile_id"] = (string)data.ProfileId,
            ["updated_at"] = data.UpdatedAt,
            ["catalog_version"] = data.CatalogVersion,
        };

        // Resources
        dict["resources"] = DtoConverters.ToDict(data.Resources);

        // Collection (cards)
        var collection = new GdArray();
        foreach (var card in data.Collection)
            collection.Add(DtoConverters.ToDict(card));
        dict["collection"] = collection;

        // Unlocked summoners
        var summoners = new GdArray();
        foreach (var s in data.UnlockedSummoners)
            summoners.Add((string)s);
        dict["unlocked_summoners"] = summoners;

        // Summoner instances
        var instances = new GdArray();
        foreach (var inst in data.SummonerInstances)
            instances.Add(DtoConverters.ToDict(inst));
        dict["summoner_instances"] = instances;

        // Items
        var items = new GdArray();
        foreach (var item in data.Items)
            items.Add(DtoConverters.ToDict(item));
        dict["items"] = items;

        // Decks + deck_cards junction table (normalized format)
        MapDecksToDict(data, dict);

        // Quest and authored-battle progress (per-summoner)
        var progressDict = new GdDict();
        foreach (var (summonerId, progress) in data.SummonerProgressMap)
            progressDict[summonerId] = DtoConverters.ToDict(progress);
        dict["summoner_progress"] = progressDict;

        // Shop purchases
        var purchasesDict = new GdDict();
        foreach (var (key, count) in data.ShopPurchases)
            purchasesDict[key] = count;
        dict["shop_purchases"] = purchasesDict;

        // Shop refresh state
        var refreshDict = new GdDict();
        foreach (var (key, state) in data.ShopRefreshStateMap)
            refreshDict[key] = DtoConverters.ToDict(state);
        dict["shop_refresh_state"] = refreshDict;

        // Cosmetics
        dict["cosmetics"] = MapCosmeticsToDict(data.Cosmetics);

        // Emotes
        dict["emotes"] = MapEmotesToDict(data.Emotes);

        // Universal rewards
        dict["rewards"] = RewardStateMapper.ToDictionary(data.Rewards);

        // Meta
        dict["meta"] = DtoConverters.ToDict(data.Meta);

        // Last match
        dict["last_match"] = MapLastMatchToDict(data.LastMatch);

        // Settings
        dict["settings"] = DtoConverters.ToDict(data.Settings);

        // WAL
        var walArr = new GdArray();
        foreach (var entry in data.Wal)
        {
            var walDict = new GdDict();
            foreach (var (key, value) in entry)
                walDict[key] = DtoConverters.ObjectToVariant(value);
            walArr.Add(walDict);
        }
        dict["wal"] = walArr;

        return dict;
    }

    // =========================================================================
    // Deck + deck_cards junction table handling
    // =========================================================================

    /// <summary>
    /// Load decks from normalized format (decks + deck_cards tables).
    /// GDScript stores card_instance_ids separately in a deck_cards junction table.
    /// C# Deck stores CardInstanceIds inline.
    /// </summary>
    private static void MapDecksFromDict(GdDict dict, ProfileData data)
    {
        if (!TryGetArray(dict, "decks", out var decksArr))
            return;

        // Build deck_cards lookup: deck_id → ordered card_instance_ids
        var deckCardsMap = new Dictionary<string, List<string>>();
        if (TryGetArray(dict, "deck_cards", out var deckCardsArr))
        {
            // Collect entries grouped by deck_id, preserving slot_index order
            var entries = new List<(string deckId, string cardInstanceId, int slotIndex)>();
            foreach (var item in deckCardsArr)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                    continue;
                var dc = item.AsGodotDictionary();
                var deckId = GetString(dc, "deck_id", "");
                var cardId = GetString(dc, "card_instance_id", "");
                var slot = GetInt(dc, "slot_index", 0);
                if (!string.IsNullOrEmpty(deckId))
                    entries.Add((deckId, cardId, slot));
            }

            // Group by deck_id and sort by slot_index
            foreach (var group in entries.GroupBy(e => e.deckId))
            {
                deckCardsMap[group.Key] = group
                    .OrderBy(e => e.slotIndex)
                    .Select(e => e.cardInstanceId)
                    .ToList();
            }
        }

        // Build Deck objects with inline CardInstanceIds
        foreach (var item in decksArr)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;
            var deckDict = item.AsGodotDictionary();
            var deck = DtoConverters.FromDeckDict(deckDict);
            if (deck == null)
                continue;

            // If deck has no inline card_instance_ids (old format), use junction table
            if (deck.CardInstanceIds.Count == 0)
            {
                var deckIdStr = (string)deck.Id;
                if (deckCardsMap.TryGetValue(deckIdStr, out var cardIds))
                    deck.CardInstanceIds = cardIds.Select(id => new CardInstanceId(id)).ToList();
            }

            data.Decks.Add(deck);
        }
    }

    /// <summary>
    /// Save decks in normalized format (decks + deck_cards junction table).
    /// Splits inline CardInstanceIds back to the junction table for JSON compatibility.
    /// </summary>
    private static void MapDecksToDict(ProfileData data, GdDict dict)
    {
        var decksArr = new GdArray();
        var deckCardsArr = new GdArray();

        foreach (var deck in data.Decks)
        {
            // Write deck without card_instance_ids (they go in junction table)
            var deckDict = new GdDict
            {
                ["id"] = (string)deck.Id,
                ["profile_id"] = (string)deck.ProfileId,
                ["summoner_id"] = (string)deck.SummonerId,
                ["name"] = deck.Name,
                ["slot"] = deck.Slot,
                ["is_active"] = deck.IsActive,
                ["updated_at"] = deck.UpdatedAt,
            };
            decksArr.Add(deckDict);

            // Write card_instance_ids to junction table
            for (int i = 0; i < deck.CardInstanceIds.Count; i++)
            {
                deckCardsArr.Add(
                    new GdDict
                    {
                        ["deck_id"] = (string)deck.Id,
                        ["card_instance_id"] = (string)deck.CardInstanceIds[i],
                        ["slot_index"] = i,
                    }
                );
            }
        }

        dict["decks"] = decksArr;
        dict["deck_cards"] = deckCardsArr;
    }

    // =========================================================================
    // Cosmetics mapping
    // =========================================================================

    private static Cosmetics MapCosmeticsFromDict(GdDict dict)
    {
        var cosmetics = new Cosmetics();

        if (TryGetArray(dict, "owned", out var ownedArr))
        {
            foreach (var item in ownedArr)
                cosmetics.Owned.Add(new CosmeticId(item.AsString()));
        }

        if (TryGetDict(dict, "equipped", out var equippedDict))
        {
            cosmetics.Equipped = new EquippedCosmetics
            {
                CardBack = new CosmeticId(GetString(equippedDict, "card_back", "")),
                UiTheme = new CosmeticId(GetString(equippedDict, "ui_theme", "")),
            };
        }

        if (TryGetDict(dict, "summoner_skins", out var skinsDict))
        {
            foreach (var key in skinsDict.Keys)
                cosmetics.SummonerSkins[key.AsString()] = new SkinId(skinsDict[key].AsString());
        }

        return cosmetics;
    }

    private static GdDict MapCosmeticsToDict(Cosmetics cosmetics)
    {
        var ownedArr = new GdArray();
        foreach (var id in cosmetics.Owned)
            ownedArr.Add((string)id);

        var equippedDict = new GdDict
        {
            ["card_back"] = (string)cosmetics.Equipped.CardBack,
            ["ui_theme"] = (string)cosmetics.Equipped.UiTheme,
        };

        var skinsDict = new GdDict();
        foreach (var (key, skinId) in cosmetics.SummonerSkins)
            skinsDict[key] = (string)skinId;

        return new GdDict
        {
            ["owned"] = ownedArr,
            ["equipped"] = equippedDict,
            ["summoner_skins"] = skinsDict,
        };
    }

    // =========================================================================
    // Emotes mapping
    // =========================================================================

    private static Emotes MapEmotesFromDict(GdDict dict)
    {
        var emotes = new Emotes();

        if (TryGetArray(dict, "owned", out var ownedArr))
        {
            emotes.Owned.Clear();
            foreach (var item in ownedArr)
                emotes.Owned.Add(new EmoteId(item.AsString()));
        }

        if (TryGetArray(dict, "equipped", out var equippedArr))
        {
            emotes.Equipped.Clear();
            foreach (var item in equippedArr)
                emotes.Equipped.Add(new EmoteId(item.AsString()));
            // Ensure exactly 4 slots
            while (emotes.Equipped.Count < 4)
                emotes.Equipped.Add(EmoteId.None);
        }

        return emotes;
    }

    private static GdDict MapEmotesToDict(Emotes emotes)
    {
        var ownedArr = new GdArray();
        foreach (var id in emotes.Owned)
            ownedArr.Add((string)id);

        var equippedArr = new GdArray();
        foreach (var id in emotes.Equipped)
            equippedArr.Add((string)id);

        return new GdDict { ["owned"] = ownedArr, ["equipped"] = equippedArr };
    }

    // =========================================================================
    // LastMatch mapping
    // =========================================================================

    private static LastMatch MapLastMatchFromDict(GdDict dict)
    {
        var match = new LastMatch();

        if (dict.TryGetValue("seed", out var seedVar) && seedVar.VariantType == Variant.Type.Int)
            match.Seed = seedVar.AsInt64();

        if (
            dict.TryGetValue("result", out var resultVar)
            && resultVar.VariantType == Variant.Type.String
        )
            match.Result = resultVar.AsString();

        if (dict.TryGetValue("duration_s", out var durationVar))
        {
            if (durationVar.VariantType == Variant.Type.Float)
                match.DurationSeconds = (float)durationVar.AsDouble();
            else if (durationVar.VariantType == Variant.Type.Int)
                match.DurationSeconds = durationVar.AsInt32();
        }

        return match;
    }

    private static GdDict MapLastMatchToDict(LastMatch match)
    {
        var dict = new GdDict();
        dict["seed"] = match.Seed.HasValue ? Variant.From(match.Seed.Value) : new Variant();
        dict["result"] = match.Result != null ? Variant.From(match.Result) : new Variant();
        dict["duration_s"] = match.DurationSeconds.HasValue
            ? Variant.From(match.DurationSeconds.Value)
            : new Variant();
        return dict;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static bool TryGetDict(GdDict dict, string key, out GdDict result)
    {
        if (dict.TryGetValue(key, out var val) && val.VariantType == Variant.Type.Dictionary)
        {
            result = val.AsGodotDictionary();
            return true;
        }
        result = new GdDict();
        return false;
    }

    private static bool TryGetArray(GdDict dict, string key, out GdArray result)
    {
        if (dict.TryGetValue(key, out var val) && val.VariantType == Variant.Type.Array)
        {
            result = val.AsGodotArray();
            return true;
        }
        result = new GdArray();
        return false;
    }

    private static string GetString(GdDict dict, string key, string defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        if (value.VariantType == Variant.Type.Nil)
            return defaultValue;
        return value.AsString();
    }

    private static int GetInt(GdDict dict, string key, int defaultValue)
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

    private static long GetLong(GdDict dict, string key, long defaultValue)
    {
        if (!dict.TryGetValue(key, out var value))
            return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt64(),
            Variant.Type.Float => (long)value.AsDouble(),
            _ => defaultValue,
        };
    }
}
