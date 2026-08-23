using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
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
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Profile.Shop;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Domain.Progression;
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

        var narrativeFlags = new Godot.Collections.Dictionary();
        foreach (var (key, value) in instance.NarrativeFlags)
            narrativeFlags[key] = value;

        return new Godot.Collections.Dictionary
        {
            ["summoner_id"] = (string)instance.SummonerId,
            ["level"] = instance.Level,
            ["xp"] = instance.Xp,
            ["equipped_items"] = equippedDict,
            ["acquired_trait_ids"] = ToGodotArray(instance.AcquiredTraitIds.Select(t => (string)t)),
            ["unspent_trait_points"] = instance.UnspentTraitPoints,
            ["narrative_flags"] = narrativeFlags,
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

        var narrativeFlags = new Dictionary<string, bool>();
        if (
            dict.TryGetValue("narrative_flags", out var narrativeVar)
            && narrativeVar.VariantType == Variant.Type.Dictionary
        )
        {
            var narrativeDict = narrativeVar.AsGodotDictionary();
            foreach (var key in narrativeDict.Keys)
                narrativeFlags[key.AsString()] = narrativeDict[key].AsBool();
        }

        return new SummonerInstance
        {
            SummonerId = new SummonerId(summonerId),
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            EquippedItems = equippedItems,
            AcquiredTraitIds = acquiredTraits,
            UnspentTraitPoints = GetInt(dict, "unspent_trait_points", 0),
            NarrativeFlags = narrativeFlags,
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
            ["gold"] = progress.Gold,
            ["academy"] = ToDict(progress.Academy),
            ["quests"] = ToDict(progress.Quests),
            ["caravan_purchases"] = ToGodotArray(progress.CaravanPurchases),
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

        if (progress.ActiveBattleAttempt != null)
            dict["active_battle_attempt"] = ToDict(progress.ActiveBattleAttempt);

        if (progress.BattleAttemptCompletions.Count > 0)
        {
            var completions = new Godot.Collections.Dictionary();
            foreach (var (attemptId, completion) in progress.BattleAttemptCompletions)
                completions[attemptId] = ToDict(completion);
            dict["battle_attempt_completions"] = completions;
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

    /// <summary>Convert generic quest progress to a Godot Dictionary.</summary>
    public static Godot.Collections.Dictionary ToDict(QuestProgress quests)
    {
        var stepIndices = new Godot.Collections.Dictionary();
        foreach (var (questId, stepIndex) in quests.CurrentStepByQuestId)
            stepIndices[questId] = stepIndex;

        return new Godot.Collections.Dictionary
        {
            ["discovered_quest_ids"] = ToGodotArray(quests.DiscoveredQuestIds),
            ["active_quest_ids"] = ToGodotArray(quests.ActiveQuestIds),
            ["completed_quest_ids"] = ToGodotArray(quests.CompletedQuestIds),
            ["current_step_by_quest_id"] = stepIndices,
            ["tracked_quest_id"] = quests.TrackedQuestId,
        };
    }

    /// <summary>Convert AcademyProgress to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(AcademyProgress academy)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["current_year"] = academy.CurrentYear,
            ["current_semester"] = academy.CurrentSemester,
            ["remaining_enrollments"] = academy.RemainingEnrollments,
            ["completed_courses"] = ToGodotArray(
                academy.CompletedCourses.Select(course => (string)course)
            ),
            ["enrolled_courses"] = ToGodotArray(
                academy.EnrolledCourses.Select(course => (string)course)
            ),
            ["discovered_courses"] = ToGodotArray(
                academy.DiscoveredCourses.Select(course => (string)course)
            ),
            ["tracked_quest_id"] = academy.TrackedQuestId,
        };

        var assessmentOutcomes = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.AssessmentOutcomes)
            assessmentOutcomes[key] = value.ToString();
        dict["assessment_outcomes"] = assessmentOutcomes;

        var activityLoadouts = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.ActivityLoadouts)
        {
            activityLoadouts[key] = new Godot.Collections.Dictionary
            {
                ["selected_card_instance_ids"] = CardInstanceIdsToGodotArray(
                    value.SelectedCardInstanceIds
                ),
            };
        }
        dict["activity_loadouts"] = activityLoadouts;

        var transcript = new Godot.Collections.Array();
        foreach (var entry in academy.Transcript)
        {
            transcript.Add(ToDict(entry));
        }
        dict["transcript"] = transcript;

        var honors = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.HonorsEligibility)
        {
            honors[key] = value;
        }
        dict["honors_eligibility"] = honors;

        var shopPurchases = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.ShopPurchases)
        {
            shopPurchases[key] = value;
        }
        dict["shop_purchases"] = shopPurchases;

        var rewardFlags = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.RewardFlags)
        {
            rewardFlags[key] = value;
        }
        dict["reward_flags"] = rewardFlags;

        var activityIndex = new Godot.Collections.Dictionary();
        foreach (var (key, value) in academy.CourseActivityIndex)
        {
            activityIndex[key] = value;
        }
        dict["course_activity_index"] = activityIndex;

        return dict;
    }

    /// <summary>Convert AcademyTranscriptEntry to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(AcademyTranscriptEntry entry)
    {
        return new Godot.Collections.Dictionary
        {
            ["course_id"] = (string)entry.CourseId,
            ["grade"] = entry.Grade,
            ["honors"] = entry.Honors,
            ["semester_key"] = entry.SemesterKey,
        };
    }

    public static Godot.Collections.Dictionary ToDict(BattleAttempt attempt) =>
        new()
        {
            ["attempt_id"] = attempt.AttemptId.Value,
            ["summoner_id"] = (string)attempt.SummonerId,
            ["campaign_id"] = attempt.CampaignId.Value,
            ["battle_id"] = attempt.BattleId.Value,
            ["deck_id"] = attempt.DeckId.Value,
            ["deck_card_instance_ids"] = CardInstanceIdsToGodotArray(attempt.DeckCardInstanceIds),
            ["card_xp_reward"] = attempt.CardXpReward,
            ["summoner_xp_reward"] = attempt.SummonerXpReward,
            ["first_clear_reward_snapshots"] = ToRewardSnapshotArray(
                attempt.FirstClearRewardSnapshots
            ),
            ["started_at"] = attempt.StartedAtUnixSeconds,
        };

    private static Godot.Collections.Array ToRewardSnapshotArray(
        IEnumerable<ResolvedRewardOfferSnapshot> snapshots
    )
    {
        var result = new Godot.Collections.Array();
        foreach (var snapshot in snapshots)
            result.Add(RewardStateMapper.ToDictionary(snapshot));
        return result;
    }

    public static Godot.Collections.Dictionary ToDict(BattleAttemptCompletion completion)
    {
        var claimIds = new Godot.Collections.Array<string>(
            completion.ClaimIds.Select(id => id.Value)
        );
        var pendingClaimIds = new Godot.Collections.Array<string>(
            completion.PendingClaimIds.Select(id => id.Value)
        );
        return new Godot.Collections.Dictionary
        {
            ["attempt_id"] = completion.AttemptId.Value,
            ["outcome"] = completion.Outcome.ToString().ToLowerInvariant(),
            ["completed_at"] = completion.CompletedAtUnixSeconds,
            ["claim_ids"] = claimIds,
            ["pending_claim_ids"] = pendingClaimIds,
        };
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

        var caravanPurchases = new List<string>();
        if (dict.TryGetValue("caravan_purchases", out var caravanVar))
            foreach (var purchase in caravanVar.AsGodotArray())
                caravanPurchases.Add(purchase.AsString());

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

        var academy = new AcademyProgress();
        if (
            dict.TryGetValue("academy", out var academyVar)
            && academyVar.VariantType == Variant.Type.Dictionary
        )
        {
            academy = FromAcademyDict(academyVar.AsGodotDictionary());
        }

        var quests = new QuestProgress();
        if (
            dict.TryGetValue("quests", out var questsVar)
            && questsVar.VariantType == Variant.Type.Dictionary
        )
        {
            quests = FromQuestDict(questsVar.AsGodotDictionary());
        }

        BattleAttempt? activeBattleAttempt = null;
        if (
            dict.TryGetValue("active_battle_attempt", out var attemptVar)
            && attemptVar.VariantType == Variant.Type.Dictionary
        )
        {
            activeBattleAttempt = FromBattleAttemptDict(attemptVar.AsGodotDictionary());
        }

        var attemptCompletions = new Dictionary<string, BattleAttemptCompletion>();
        if (
            dict.TryGetValue("battle_attempt_completions", out var completionsVar)
            && completionsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var completions = completionsVar.AsGodotDictionary();
            foreach (var key in completions.Keys)
            {
                var completionVar = completions[key];
                if (completionVar.VariantType != Variant.Type.Dictionary)
                    continue;
                var completion = FromBattleAttemptCompletionDict(completionVar.AsGodotDictionary());
                if (completion != null)
                    attemptCompletions[key.AsString()] = completion;
            }
        }

        return new CampaignProgress
        {
            CompletedBattles = completed,
            Gold = GetInt(dict, "gold", 0),
            CaravanPurchases = caravanPurchases,
            StoryArcs = storyArcs,
            Choices = choices,
            Academy = academy,
            Quests = quests,
            ActiveBattleAttempt = activeBattleAttempt,
            BattleAttemptCompletions = attemptCompletions,
        };
    }

    /// <summary>Convert a Godot Dictionary to generic quest progress.</summary>
    public static QuestProgress FromQuestDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new QuestProgress();

        static List<string> ReadIds(Godot.Collections.Dictionary source, string key)
        {
            var ids = new List<string>();
            if (source.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Array)
            {
                foreach (var item in value.AsGodotArray())
                    ids.Add(item.AsString());
            }

            return ids;
        }

        var stepIndices = new Dictionary<string, int>();
        if (
            dict.TryGetValue("current_step_by_quest_id", out var indicesVar)
            && indicesVar.VariantType == Variant.Type.Dictionary
        )
        {
            var indices = indicesVar.AsGodotDictionary();
            foreach (var key in indices.Keys)
                stepIndices[key.AsString()] = indices[key].AsInt32();
        }

        return new QuestProgress
        {
            DiscoveredQuestIds = ReadIds(dict, "discovered_quest_ids"),
            ActiveQuestIds = ReadIds(dict, "active_quest_ids"),
            CompletedQuestIds = ReadIds(dict, "completed_quest_ids"),
            CurrentStepByQuestId = stepIndices,
            TrackedQuestId = GetString(dict, "tracked_quest_id", ""),
        };
    }

    public static BattleAttempt? FromBattleAttemptDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null)
            return null;
        var attemptId = GetString(dict, "attempt_id", "");
        var summonerId = GetString(dict, "summoner_id", "");
        var campaignId = GetString(dict, "campaign_id", "");
        var battleId = GetString(dict, "battle_id", "");
        if (
            string.IsNullOrWhiteSpace(attemptId)
            || string.IsNullOrWhiteSpace(summonerId)
            || string.IsNullOrWhiteSpace(campaignId)
            || string.IsNullOrWhiteSpace(battleId)
        )
            return null;

        var cardIds = new List<CardInstanceId>();
        if (dict.TryGetValue("deck_card_instance_ids", out var cardsVar))
        {
            foreach (var card in cardsVar.AsGodotArray())
                cardIds.Add(new CardInstanceId(card.AsString()));
        }

        var firstClearSnapshots = new List<ResolvedRewardOfferSnapshot>();
        if (dict.TryGetValue("first_clear_reward_snapshots", out var snapshotsVar))
        {
            foreach (var snapshotVar in snapshotsVar.AsGodotArray())
            {
                if (snapshotVar.VariantType != Variant.Type.Dictionary)
                    continue;
                var snapshot = RewardStateMapper.FromSnapshotDictionary(
                    snapshotVar.AsGodotDictionary()
                );
                if (snapshot != null)
                    firstClearSnapshots.Add(snapshot);
            }
        }

        return new BattleAttempt
        {
            AttemptId = new BattleAttemptId(attemptId),
            SummonerId = new SummonerId(summonerId),
            CampaignId = new CampaignId(campaignId),
            BattleId = new BattleId(battleId),
            DeckId = new Fateforged.Meta.Deck.DeckId(GetString(dict, "deck_id", "")),
            DeckCardInstanceIds = cardIds,
            CardXpReward = GetInt(dict, "card_xp_reward", 0),
            SummonerXpReward = GetInt(dict, "summoner_xp_reward", 0),
            FirstClearRewardSnapshots = firstClearSnapshots,
            StartedAtUnixSeconds = GetLong(dict, "started_at", 0),
        };
    }

    public static BattleAttemptCompletion? FromBattleAttemptCompletionDict(
        Godot.Collections.Dictionary? dict
    )
    {
        if (dict == null)
            return null;
        var attemptId = GetString(dict, "attempt_id", "");
        if (string.IsNullOrWhiteSpace(attemptId))
            return null;

        var outcome = Enum.TryParse<BattleTerminalOutcome>(
            GetString(dict, "outcome", "abandoned"),
            true,
            out var parsedOutcome
        )
            ? parsedOutcome
            : BattleTerminalOutcome.Abandoned;

        var claimIds = new List<RewardClaimId>();
        if (dict.TryGetValue("claim_ids", out var claimsVar))
        {
            foreach (var claim in claimsVar.AsGodotArray())
                claimIds.Add(new RewardClaimId(claim.AsString()));
        }

        var pendingClaimIds = new List<RewardClaimId>();
        if (dict.TryGetValue("pending_claim_ids", out var pendingClaimsVar))
        {
            foreach (var claim in pendingClaimsVar.AsGodotArray())
                pendingClaimIds.Add(new RewardClaimId(claim.AsString()));
        }

        return new BattleAttemptCompletion
        {
            AttemptId = new BattleAttemptId(attemptId),
            Outcome = outcome,
            CompletedAtUnixSeconds = GetLong(dict, "completed_at", 0),
            ClaimIds = claimIds,
            PendingClaimIds = pendingClaimIds,
        };
    }

    /// <summary>Convert Godot Dictionary to AcademyProgress.</summary>
    public static AcademyProgress FromAcademyDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0)
            return new AcademyProgress();

        var completedCourses = new List<CourseId>();
        if (
            dict.TryGetValue("completed_courses", out var coursesVar)
            && coursesVar.VariantType == Variant.Type.Array
        )
        {
            foreach (var course in coursesVar.AsGodotArray())
            {
                completedCourses.Add(new CourseId(course.AsString()));
            }
        }

        var assessmentOutcomes = new Dictionary<string, AcademyActivityOutcome>();
        if (
            dict.TryGetValue("assessment_outcomes", out var assessmentsVar)
            && assessmentsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var assessmentDict = assessmentsVar.AsGodotDictionary();
            foreach (var key in assessmentDict.Keys)
            {
                if (
                    Enum.TryParse<AcademyActivityOutcome>(
                        assessmentDict[key].AsString(),
                        ignoreCase: true,
                        out var outcome
                    )
                )
                    assessmentOutcomes[key.AsString()] = outcome;
            }
        }

        var activityLoadouts = new Dictionary<string, AcademyActivityLoadoutState>();
        if (
            dict.TryGetValue("activity_loadouts", out var loadoutsVar)
            && loadoutsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var loadoutsDict = loadoutsVar.AsGodotDictionary();
            foreach (var key in loadoutsDict.Keys)
            {
                if (loadoutsDict[key].VariantType != Variant.Type.Dictionary)
                    continue;
                var loadoutDict = loadoutsDict[key].AsGodotDictionary();
                var selectedIds = new List<CardInstanceId>();
                if (
                    loadoutDict.TryGetValue("selected_card_instance_ids", out var idsVar)
                    && idsVar.VariantType == Variant.Type.Array
                )
                {
                    foreach (var id in idsVar.AsGodotArray())
                        selectedIds.Add(CardInstanceId.FromString(id.AsString()));
                }
                activityLoadouts[key.AsString()] = new AcademyActivityLoadoutState
                {
                    SelectedCardInstanceIds = selectedIds,
                };
            }
        }

        var enrolledCourses = new List<CourseId>();
        if (
            dict.TryGetValue("enrolled_courses", out var enrolledVar)
            && enrolledVar.VariantType == Variant.Type.Array
        )
        {
            foreach (var course in enrolledVar.AsGodotArray())
            {
                enrolledCourses.Add(new CourseId(course.AsString()));
            }
        }

        var discoveredCourses = new List<CourseId>();
        if (
            dict.TryGetValue("discovered_courses", out var discoveredVar)
            && discoveredVar.VariantType == Variant.Type.Array
        )
        {
            foreach (var course in discoveredVar.AsGodotArray())
                discoveredCourses.Add(CourseId.FromString(course.AsString()));
        }

        var transcript = new List<AcademyTranscriptEntry>();
        if (
            dict.TryGetValue("transcript", out var transcriptVar)
            && transcriptVar.VariantType == Variant.Type.Array
        )
        {
            foreach (var entryVar in transcriptVar.AsGodotArray())
            {
                if (entryVar.VariantType == Variant.Type.Dictionary)
                {
                    transcript.Add(FromAcademyTranscriptDict(entryVar.AsGodotDictionary()));
                }
            }
        }

        var honorsEligibility = new Dictionary<string, bool>();
        if (
            dict.TryGetValue("honors_eligibility", out var honorsVar)
            && honorsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var honorsDict = honorsVar.AsGodotDictionary();
            foreach (var key in honorsDict.Keys)
            {
                honorsEligibility[key.AsString()] = honorsDict[key].AsBool();
            }
        }

        var shopPurchases = new Dictionary<string, int>();
        if (
            dict.TryGetValue("shop_purchases", out var shopVar)
            && shopVar.VariantType == Variant.Type.Dictionary
        )
        {
            var shopDict = shopVar.AsGodotDictionary();
            foreach (var key in shopDict.Keys)
            {
                shopPurchases[key.AsString()] = shopDict[key].AsInt32();
            }
        }

        var courseActivityIndex = new Dictionary<string, int>();
        if (
            dict.TryGetValue("course_activity_index", out var activityVar)
            && activityVar.VariantType == Variant.Type.Dictionary
        )
        {
            var activityDict = activityVar.AsGodotDictionary();
            foreach (var key in activityDict.Keys)
            {
                courseActivityIndex[key.AsString()] = activityDict[key].AsInt32();
            }
        }

        var rewardFlags = new Dictionary<string, int>();
        if (
            dict.TryGetValue("reward_flags", out var rewardFlagsVar)
            && rewardFlagsVar.VariantType == Variant.Type.Dictionary
        )
        {
            var rewardFlagsDict = rewardFlagsVar.AsGodotDictionary();
            foreach (var key in rewardFlagsDict.Keys)
                rewardFlags[key.AsString()] = rewardFlagsDict[key].AsInt32();
        }

        return new AcademyProgress
        {
            CurrentYear = GetInt(dict, "current_year", 1),
            CurrentSemester = GetInt(dict, "current_semester", 1),
            RemainingEnrollments = GetInt(dict, "remaining_enrollments", 0),
            CompletedCourses = completedCourses,
            EnrolledCourses = enrolledCourses,
            DiscoveredCourses = discoveredCourses,
            TrackedQuestId = GetString(dict, "tracked_quest_id", ""),
            AssessmentOutcomes = assessmentOutcomes,
            ActivityLoadouts = activityLoadouts,
            Transcript = transcript,
            HonorsEligibility = honorsEligibility,
            ShopPurchases = shopPurchases,
            RewardFlags = rewardFlags,
            CourseActivityIndex = courseActivityIndex,
        };
    }

    /// <summary>Convert Godot Dictionary to AcademyTranscriptEntry.</summary>
    public static AcademyTranscriptEntry FromAcademyTranscriptDict(
        Godot.Collections.Dictionary dict
    )
    {
        return new AcademyTranscriptEntry
        {
            CourseId = new CourseId(GetString(dict, "course_id", "")),
            Grade = GetString(dict, "grade", ""),
            Honors = GetBool(dict, "honors", false),
            SemesterKey = GetString(dict, "semester_key", ""),
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
            ["master_volume"] = settings.MasterVolume,
            ["sfx_volume"] = settings.SfxVolume,
            ["music_volume"] = settings.MusicVolume,
            ["mute_when_unfocused"] = settings.MuteWhenUnfocused,
            ["window_mode"] = settings.WindowMode,
            ["resolution_width"] = settings.ResolutionWidth,
            ["resolution_height"] = settings.ResolutionHeight,
            ["vsync_enabled"] = settings.VsyncEnabled,
            ["fps_limit"] = settings.FpsLimit,
            ["edge_pan_enabled"] = settings.EdgePanEnabled,
            ["camera_speed"] = settings.CameraSpeed,
            ["reduce_camera_motion"] = settings.ReduceCameraMotion,
            ["ui_scale"] = settings.UiScale,
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
            MasterVolume = GetFloat(dict, "master_volume", 1.0f),
            SfxVolume = GetFloat(dict, "sfx_volume", 1.0f),
            MusicVolume = GetFloat(dict, "music_volume", 1.0f),
            MuteWhenUnfocused = GetBool(dict, "mute_when_unfocused", false),
            WindowMode = GetString(dict, "window_mode", "fullscreen"),
            ResolutionWidth = GetInt(dict, "resolution_width", 1920),
            ResolutionHeight = GetInt(dict, "resolution_height", 1080),
            VsyncEnabled = GetBool(dict, "vsync_enabled", true),
            FpsLimit = GetInt(dict, "fps_limit", 60),
            EdgePanEnabled = GetBool(dict, "edge_pan_enabled", true),
            CameraSpeed = GetFloat(dict, "camera_speed", 1.0f),
            ReduceCameraMotion = GetBool(dict, "reduce_camera_motion", false),
            UiScale = GetFloat(dict, "ui_scale", 1.0f),
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

        var narrativeDict = new Godot.Collections.Dictionary();
        foreach (var (key, value) in meta.NarrativeFlags)
            narrativeDict[key] = value;

        return new Godot.Collections.Dictionary
        {
            ["selected_deck"] = meta.SelectedDeck,
            ["selected_summoner"] = meta.SelectedSummoner,
            ["selected_campaign"] = meta.SelectedCampaign,
            ["analytics_opt_in"] = meta.AnalyticsOptIn,
            ["tutorial_flags"] = tutorialDict,
            ["narrative_flags"] = narrativeDict,
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
            SelectedCampaign = GetString(dict, "selected_campaign", ""),
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
            dict.TryGetValue("narrative_flags", out var narrativeVar)
            && narrativeVar.VariantType == Variant.Type.Dictionary
        )
        {
            var narrativeDict = narrativeVar.AsGodotDictionary();
            foreach (var key in narrativeDict.Keys)
                meta.NarrativeFlags[key.AsString()] = narrativeDict[key].AsBool();
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

        if (update.SelectedCampaign != null)
            dict["selected_campaign"] = update.SelectedCampaign;

        if (update.AnalyticsOptIn.HasValue)
            dict["analytics_opt_in"] = update.AnalyticsOptIn.Value;

        if (update.TutorialFlags != null)
        {
            var tutorialDict = new Godot.Collections.Dictionary();
            foreach (var (key, value) in update.TutorialFlags)
                tutorialDict[key] = value;
            dict["tutorial_flags"] = tutorialDict;
        }

        if (update.NarrativeFlags != null)
        {
            var narrativeDict = new Godot.Collections.Dictionary();
            foreach (var (key, value) in update.NarrativeFlags)
                narrativeDict[key] = value;
            dict["narrative_flags"] = narrativeDict;
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
