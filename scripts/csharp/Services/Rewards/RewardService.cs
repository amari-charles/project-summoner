using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Rewards;

/// <summary>
/// Reward Service - Handles reward generation and granting.
///
/// Features:
/// - Flexible reward generation with guaranteed (summoner-themed) and pool options
/// - Collection-aware filtering (exclude owned, exclude duplicates)
/// - Unified reward granting (cards, gold, cosmetics, etc.)
///
/// Usage:
///   var options = RewardServiceCS.Instance.GenerateRewardOptions(config, summonerId, ownedIds);
///   RewardServiceCS.Instance.GrantReward(selectedOption);
///
/// Note: This class is accessed via the "RewardServiceCS" autoload. The [GlobalClass] attribute
/// is intentionally omitted to avoid conflicting with the GDScript "RewardService" autoload wrapper.
/// </summary>
public partial class RewardService : Node
{
    public static RewardService? Instance { get; private set; }

    [Signal]
    public delegate void RewardsGrantedEventHandler(Godot.Collections.Dictionary rewards);

    [Signal]
    public delegate void RewardGrantFailedEventHandler(string reason);

    private IProfileRepository? _profileRepo;
    private readonly Random _random = new();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        GD.Print("RewardService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("RewardService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        GD.Print("RewardService: Ready");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Initialize for testing with mock dependencies.</summary>
    public void InitForTesting(IProfileRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _profileRepo = repo;
    }

    // =========================================================================
    // FLEXIBLE REWARD GENERATION
    // =========================================================================

    /// <summary>
    /// Generate reward options for a battle reward screen.
    /// Returns guaranteed (summoner-themed) options plus pool-drawn options.
    /// </summary>
    /// <param name="config">Battle reward configuration.</param>
    /// <param name="summonerId">Active summoner ID for guaranteed options.</param>
    /// <param name="ownedCatalogIds">Set of catalog IDs the player already owns.</param>
    /// <returns>List of reward options to present to the player.</returns>
    public List<RewardOption> GenerateRewardOptions(
        RewardConfig config,
        string summonerId,
        HashSet<string>? ownedCatalogIds = null)
    {
        var options = new List<RewardOption>();

        // Get summoner element for guaranteed options
        var summoner = SummonerCatalog.GetSummoner(summonerId);
        var element = summoner?.ElementalAffinity ?? Element.Neutral;

        // Generate guaranteed options (summoner element cards)
        if (config.GuaranteedCount > 0)
        {
            var guaranteed = GetGuaranteedOptions(element, config.GuaranteedCount, config.CollectionFilter, ownedCatalogIds);
            options.AddRange(guaranteed);
        }

        // Generate pool options
        if (config.PoolCount > 0 && !string.IsNullOrEmpty(config.PoolId))
        {
            var poolOptions = DrawFromPool(config.PoolId, config.PoolCount, config.CollectionFilter, ownedCatalogIds);
            options.AddRange(poolOptions);
        }

        return options;
    }

    /// <summary>
    /// Get guaranteed reward options matching the summoner's element.
    /// These are marked as "guaranteed" in the UI.
    /// </summary>
    public List<RewardOption> GetGuaranteedOptions(
        Element element,
        int count,
        CollectionFilterMode filterMode = CollectionFilterMode.None,
        HashSet<string>? ownedCatalogIds = null)
    {
        // Get cards matching the summoner's element
        var elementCards = CardCatalog.GetCardsByElement(element)
            .Where(c => c.UnlockCondition != UnlockCondition.DevOnly)
            .ToList();

        // Apply collection filter
        var filteredCards = ApplyCollectionFilter(elementCards, filterMode, ownedCatalogIds);

        // Shuffle and take requested count
        var shuffled = filteredCards.OrderBy(_ => _random.Next()).ToList();
        var selected = shuffled.Take(count);

        return selected.Select(card => CreateCardRewardOption(card, isGuaranteed: true)).ToList();
    }

    /// <summary>
    /// Draw reward options from a named pool.
    /// </summary>
    public List<RewardOption> DrawFromPool(
        string poolId,
        int count,
        CollectionFilterMode filterMode = CollectionFilterMode.None,
        HashSet<string>? ownedCatalogIds = null)
    {
        // Get pool cards
        var excludeIds = GetExcludeIds(filterMode, ownedCatalogIds);
        var poolCards = RewardPoolCatalog.GetCardsForPool(poolId, excludeIds).ToList();

        // Shuffle and take requested count
        var shuffled = poolCards.OrderBy(_ => _random.Next()).ToList();
        var selected = shuffled.Take(count);

        return selected.Select(card => CreateCardRewardOption(card, isGuaranteed: false)).ToList();
    }

    /// <summary>
    /// Get current player's owned catalog IDs for filtering.
    /// </summary>
    public HashSet<string> GetOwnedCatalogIds()
    {
        if (_profileRepo == null)
            return [];

        var cards = _profileRepo.ListCards();
        return cards.Select(c => c.CatalogId).ToHashSet();
    }

    // =========================================================================
    // REWARD GRANTING
    // =========================================================================

    /// <summary>
    /// Grant a single reward option to the player.
    /// </summary>
    public bool GrantReward(RewardOption option)
    {
        return option.Type switch
        {
            RewardType.Card => GrantCardReward(option.Id, option.Rarity, option.Amount),
            RewardType.CampaignGold => GrantCampaignGoldReward(option.Amount),
            RewardType.Gold => GrantGoldReward(option.Amount),
            RewardType.Gems => GrantGemsReward(option.Amount),
            RewardType.Essence => GrantEssenceReward(option.Amount),
            RewardType.Fragments => GrantFragmentsReward(option.Amount),
            RewardType.Summoner => GrantSummonerReward(option.Id),
            RewardType.Cosmetic => GrantCosmeticReward(option.Id),
            RewardType.Emote => GrantEmoteReward(option.Id),
            RewardType.Item => false, // TODO: Implement in Phase 4
            _ => false
        };
    }

    /// <summary>
    /// Grant multiple rewards from a dictionary (legacy format for GDScript compatibility).
    /// </summary>
    /// <param name="rewards">Dictionary with keys: cards, gold, summoner, cosmetic, emote, cosmetics.</param>
    /// <returns>True if all rewards granted successfully.</returns>
    public bool GrantRewards(Godot.Collections.Dictionary rewards)
    {
        if (_profileRepo == null)
        {
            GD.PushError("RewardService: Cannot grant rewards - repository not initialized");
            return false;
        }

        var success = true;

        // Grant gold
        if (rewards.TryGetValue("gold", out var goldVar))
        {
            var gold = (int)goldVar;
            if (gold != 0)
            {
                // For now, gold goes to resources. Campaign gold should use "campaign_gold" key.
                _profileRepo.UpdateResources(new Dictionary<ResourceType, int>
                {
                    { ResourceType.Gold, gold }
                });
            }
        }

        // Grant campaign gold
        if (rewards.TryGetValue("campaign_gold", out var campaignGoldVar))
        {
            var campaignGold = (int)campaignGoldVar;
            if (campaignGold > 0)
            {
                // Get active summoner for campaign gold
                var summonerSelection = GetTree()?.Root?.GetNodeOrNull("/root/SummonerSelection");
                var summonerId = "";
                if (summonerSelection != null)
                {
                    var result = summonerSelection.Call("get_active_summoner_id");
                    if (result.VariantType == Variant.Type.String)
                        summonerId = result.AsString();
                }

                if (!string.IsNullOrEmpty(summonerId))
                {
                    var progress = _profileRepo.GetCampaignProgress(summonerId);
                    progress.Gold += campaignGold;
                    _profileRepo.UpdateCampaignProgress(summonerId, progress);
                }
            }
        }

        // Grant cards
        if (rewards.TryGetValue("cards", out var cardsVar) && cardsVar.AsGodotArray() is { } cardsArray)
        {
            foreach (var cardGrant in cardsArray)
            {
                if (cardGrant.AsGodotDictionary() is not { } cardDict)
                    continue;

                var catalogId = cardDict.TryGetValue("catalog_id", out var idVar) ? idVar.AsString() : "";
                var count = cardDict.TryGetValue("count", out var countVar) ? (int)countVar : 1;
                var rarity = cardDict.TryGetValue("rarity", out var rarityVar) ? rarityVar.AsString() : "common";

                // Parse binding (defaults to AccountWide)
                var binding = cardDict.TryGetValue("binding", out var bindingVar)
                    ? (ContentBinding)bindingVar.AsInt32()
                    : ContentBinding.AccountWide;
                var boundTo = cardDict.TryGetValue("bound_to", out var boundToVar)
                    ? boundToVar.AsString()
                    : null;

                if (string.IsNullOrEmpty(catalogId))
                    continue;

                // Grant multiple copies with binding
                var cardsToGrant = Enumerable.Range(0, count)
                    .Select(_ => (catalogId, rarity, binding, boundTo))
                    .ToList();

                var instanceIds = _profileRepo.GrantCards(cardsToGrant);
                if (instanceIds.Length != count)
                {
                    GD.PushError($"RewardService: Failed to grant all {catalogId} cards (granted {instanceIds.Length}/{count})");
                    success = false;
                }
            }
        }

        // Grant summoner unlock
        if (rewards.TryGetValue("summoner", out var summonerVar))
        {
            var summonerIdToGrant = summonerVar.AsString();
            if (!string.IsNullOrEmpty(summonerIdToGrant))
            {
                if (!_profileRepo.IsSummonerUnlocked(summonerIdToGrant))
                {
                    if (!_profileRepo.UnlockSummoner(summonerIdToGrant))
                    {
                        GD.PushError($"RewardService: Failed to unlock summoner {summonerIdToGrant}");
                        success = false;
                    }
                    else
                    {
                        // Create SummonerInstance for the new summoner
                        var instance = new SummonerInstanceData
                        {
                            SummonerId = summonerIdToGrant,
                            Level = 1,
                            Xp = 0
                        };
                        if (!_profileRepo.SaveSummonerInstance(instance))
                        {
                            GD.PushError($"RewardService: Failed to save summoner instance for {summonerIdToGrant}");
                            // Don't fail the whole grant - summoner is unlocked, just instance save failed
                        }
                        GD.Print($"RewardService: Unlocked summoner '{summonerIdToGrant}'");
                    }
                }
            }
        }

        // Grant cosmetic
        if (rewards.TryGetValue("cosmetic", out var cosmeticVar))
        {
            var cosmeticId = cosmeticVar.AsString();
            if (!string.IsNullOrEmpty(cosmeticId))
            {
                if (!_profileRepo.GrantCosmetic(cosmeticId))
                {
                    GD.PushError($"RewardService: Failed to grant cosmetic {cosmeticId}");
                    success = false;
                }
                else
                {
                    GD.Print($"RewardService: Granted cosmetic '{cosmeticId}'");
                }
            }
        }

        // Grant emote
        if (rewards.TryGetValue("emote", out var emoteVar))
        {
            var emoteId = emoteVar.AsString();
            if (!string.IsNullOrEmpty(emoteId))
            {
                if (!_profileRepo.GrantEmote(emoteId))
                {
                    GD.PushError($"RewardService: Failed to grant emote {emoteId}");
                    success = false;
                }
                else
                {
                    GD.Print($"RewardService: Granted emote '{emoteId}'");
                }
            }
        }

        // Grant cosmetics array (legacy)
        if (rewards.TryGetValue("cosmetics", out var cosmeticsVar) && cosmeticsVar.AsGodotArray() is { } cosmeticsArray)
        {
            foreach (var cosmeticItem in cosmeticsArray)
            {
                var cosmeticId = cosmeticItem.AsString();
                if (!string.IsNullOrEmpty(cosmeticId))
                {
                    if (!_profileRepo.GrantCosmetic(cosmeticId))
                    {
                        GD.PushError($"RewardService: Failed to grant cosmetic {cosmeticId}");
                        success = false;
                    }
                }
            }
        }

        if (success)
        {
            EmitSignal(SignalName.RewardsGranted, rewards);
        }
        else
        {
            EmitSignal(SignalName.RewardGrantFailed, "Some rewards failed to grant");
        }

        return success;
    }

    // =========================================================================
    // INTERNAL HELPERS
    // =========================================================================

    private bool GrantCardReward(string catalogId, string rarity, int count)
    {
        if (_profileRepo == null) return false;

        var cardsToGrant = Enumerable.Range(0, count)
            .Select(_ => (catalogId, rarity))
            .ToList();

        var instanceIds = _profileRepo.GrantCards(cardsToGrant);
        var success = instanceIds.Length == count;

        if (success)
            GD.Print($"RewardService: Granted {count}x {catalogId} ({rarity})");
        else
            GD.PushError($"RewardService: Failed to grant {catalogId} (granted {instanceIds.Length}/{count})");

        return success;
    }

    private bool GrantCampaignGoldReward(int amount)
    {
        if (_profileRepo == null || amount <= 0) return false;

        var summonerSelection = GetTree()?.Root?.GetNodeOrNull("/root/SummonerSelection");
        var summonerId = "";
        if (summonerSelection != null)
        {
            var result = summonerSelection.Call("get_active_summoner_id");
            if (result.VariantType == Variant.Type.String)
                summonerId = result.AsString();
        }

        if (string.IsNullOrEmpty(summonerId))
        {
            GD.PushWarning("RewardService: Cannot grant campaign gold - no active summoner");
            return false;
        }

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.Gold += amount;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"RewardService: Granted {amount} campaign gold");
        return true;
    }

    private bool GrantGoldReward(int amount)
    {
        if (_profileRepo == null || amount == 0) return false;

        _profileRepo.UpdateResources(new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold, amount }
        });

        GD.Print($"RewardService: Granted {amount} gold");
        return true;
    }

    private bool GrantGemsReward(int amount)
    {
        if (_profileRepo == null || amount <= 0) return false;

        _profileRepo.UpdateResources(new Dictionary<ResourceType, int>
        {
            { ResourceType.Gems, amount }
        });

        GD.Print($"RewardService: Granted {amount} gems");
        return true;
    }

    private bool GrantEssenceReward(int amount)
    {
        if (_profileRepo == null || amount <= 0) return false;

        _profileRepo.UpdateResources(new Dictionary<ResourceType, int>
        {
            { ResourceType.Essence, amount }
        });

        GD.Print($"RewardService: Granted {amount} essence");
        return true;
    }

    private bool GrantFragmentsReward(int amount)
    {
        if (_profileRepo == null || amount <= 0) return false;

        _profileRepo.UpdateResources(new Dictionary<ResourceType, int>
        {
            { ResourceType.Fragments, amount }
        });

        GD.Print($"RewardService: Granted {amount} fragments");
        return true;
    }

    private bool GrantSummonerReward(string summonerId)
    {
        if (_profileRepo == null || string.IsNullOrEmpty(summonerId)) return false;

        if (_profileRepo.IsSummonerUnlocked(summonerId))
        {
            GD.Print($"RewardService: Summoner {summonerId} already unlocked");
            return true;
        }

        if (!_profileRepo.UnlockSummoner(summonerId))
        {
            GD.PushError($"RewardService: Failed to unlock summoner {summonerId}");
            return false;
        }

        // Create instance
        var instance = new SummonerInstanceData
        {
            SummonerId = summonerId,
            Level = 1,
            Xp = 0
        };
        _profileRepo.SaveSummonerInstance(instance);

        GD.Print($"RewardService: Unlocked summoner '{summonerId}'");
        return true;
    }

    private bool GrantCosmeticReward(string cosmeticId)
    {
        if (_profileRepo == null || string.IsNullOrEmpty(cosmeticId)) return false;

        if (!_profileRepo.GrantCosmetic(cosmeticId))
        {
            GD.PushError($"RewardService: Failed to grant cosmetic {cosmeticId}");
            return false;
        }

        GD.Print($"RewardService: Granted cosmetic '{cosmeticId}'");
        return true;
    }

    private bool GrantEmoteReward(string emoteId)
    {
        if (_profileRepo == null || string.IsNullOrEmpty(emoteId)) return false;

        if (!_profileRepo.GrantEmote(emoteId))
        {
            GD.PushError($"RewardService: Failed to grant emote {emoteId}");
            return false;
        }

        GD.Print($"RewardService: Granted emote '{emoteId}'");
        return true;
    }

    private List<CardDefinition> ApplyCollectionFilter(
        List<CardDefinition> cards,
        CollectionFilterMode filterMode,
        HashSet<string>? ownedCatalogIds)
    {
        if (filterMode == CollectionFilterMode.None || ownedCatalogIds == null)
            return cards;

        return filterMode switch
        {
            CollectionFilterMode.ExcludeOwned => cards.Where(c => !ownedCatalogIds.Contains(c.Id)).ToList(),
            CollectionFilterMode.ExcludeDuplicates => cards, // TODO: More nuanced duplicate handling
            _ => cards
        };
    }

    private HashSet<string>? GetExcludeIds(CollectionFilterMode filterMode, HashSet<string>? ownedCatalogIds)
    {
        return filterMode switch
        {
            CollectionFilterMode.ExcludeOwned => ownedCatalogIds,
            _ => null
        };
    }

    private static RewardOption CreateCardRewardOption(CardDefinition card, bool isGuaranteed)
    {
        return new RewardOption
        {
            Type = RewardType.Card,
            Id = card.Id,
            Amount = 1,
            Rarity = card.Rarity.ToString().ToLowerInvariant(),
            IsGuaranteed = isGuaranteed,
            DisplayName = card.Name,
            Description = card.Description,
            IconPath = card.CardIconPath,
            Element = card.ElementalAffinity.ToString().ToLowerInvariant()
        };
    }

    // =========================================================================
    // GDSCRIPT INTEROP
    // =========================================================================

    /// <summary>GDScript-friendly version of GenerateRewardOptions.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GenerateRewardOptionsDict(
        Godot.Collections.Dictionary config,
        string summonerId)
    {
        var rewardConfig = RewardConfig.FromGodotDict(config);
        var ownedIds = GetOwnedCatalogIds();
        var options = GenerateRewardOptions(rewardConfig, summonerId, ownedIds);

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var option in options)
        {
            result.Add(RewardOptionToDict(option));
        }
        return result;
    }

    /// <summary>GDScript-friendly version of GrantReward.</summary>
    public bool GrantRewardDict(Godot.Collections.Dictionary optionDict)
    {
        var option = RewardOptionFromDict(optionDict);
        return GrantReward(option);
    }

    private static Godot.Collections.Dictionary RewardOptionToDict(RewardOption option)
    {
        return new Godot.Collections.Dictionary
        {
            ["type"] = option.Type.ToString().ToLowerInvariant(),
            ["id"] = option.Id,
            ["amount"] = option.Amount,
            ["rarity"] = option.Rarity,
            ["is_guaranteed"] = option.IsGuaranteed,
            ["display_name"] = option.DisplayName,
            ["description"] = option.Description,
            ["icon_path"] = option.IconPath,
            ["element"] = option.Element
        };
    }

    private static RewardOption RewardOptionFromDict(Godot.Collections.Dictionary dict)
    {
        var typeStr = dict.TryGetValue("type", out var typeVar) ? typeVar.AsString() : "card";
        Enum.TryParse<RewardType>(typeStr, ignoreCase: true, out var type);

        return new RewardOption
        {
            Type = type,
            Id = dict.TryGetValue("id", out var idVar) ? idVar.AsString() : "",
            Amount = dict.TryGetValue("amount", out var amtVar) ? (int)amtVar : 1,
            Rarity = dict.TryGetValue("rarity", out var rarVar) ? rarVar.AsString() : "common",
            IsGuaranteed = dict.TryGetValue("is_guaranteed", out var guarVar) && (bool)guarVar,
            DisplayName = dict.TryGetValue("display_name", out var nameVar) ? nameVar.AsString() : "",
            Description = dict.TryGetValue("description", out var descVar) ? descVar.AsString() : "",
            IconPath = dict.TryGetValue("icon_path", out var iconVar) ? iconVar.AsString() : "",
            Element = dict.TryGetValue("element", out var elemVar) ? elemVar.AsString() : ""
        };
    }
}

/// <summary>
/// Configuration for flexible reward generation.
/// </summary>
public class RewardConfig
{
    /// <summary>Number of guaranteed (summoner-themed) options.</summary>
    public int GuaranteedCount { get; init; }

    /// <summary>Number of options drawn from the pool.</summary>
    public int PoolCount { get; init; }

    /// <summary>Pool ID for non-guaranteed options.</summary>
    public string PoolId { get; init; } = "standard_cards";

    /// <summary>How to filter based on player's collection.</summary>
    public CollectionFilterMode CollectionFilter { get; init; } = CollectionFilterMode.None;

    /// <summary>Create from Godot Dictionary (for GDScript interop).</summary>
    public static RewardConfig FromGodotDict(Godot.Collections.Dictionary dict)
    {
        var filterStr = dict.TryGetValue("collection_filter", out var filterVar) ? filterVar.AsString() : "none";
        Enum.TryParse<CollectionFilterMode>(filterStr.Replace("_", ""), ignoreCase: true, out var filterMode);

        return new RewardConfig
        {
            GuaranteedCount = dict.TryGetValue("guaranteed_count", out var guarVar) ? (int)guarVar : 0,
            PoolCount = dict.TryGetValue("pool_count", out var poolVar) ? (int)poolVar : 0,
            PoolId = dict.TryGetValue("pool_id", out var poolIdVar) ? poolIdVar.AsString() : "standard_cards",
            CollectionFilter = filterMode
        };
    }
}
