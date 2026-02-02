using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Domain.Profile;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Summoners;
using ProjectSummoner.Infrastructure.Persistence;

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
        Initialize();
    }

    private void Initialize()
    {
        GD.Print("RewardService: Initializing...");

        _profileRepo = ProfileRepository.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("RewardService: ProfileRepository.Instance not available");
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
    // COLLECTION QUERIES
    // =========================================================================

    /// <summary>
    /// Get current player's owned catalog IDs for filtering.
    /// </summary>
    public HashSet<string> GetOwnedCatalogIds()
    {
        if (_profileRepo == null)
            return [];

        var cards = _profileRepo.ListCards();
        return cards.Select(c => (string)c.CatalogId).ToHashSet();
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
                    var typedSummonerId = new SummonerId(summonerId);
                    var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
                    progress.Gold += campaignGold;
                    _profileRepo.UpdateCampaignProgress(typedSummonerId, progress);
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
                var typedCatalogId = new CardId(catalogId);
                SummonerId? typedBoundTo = !string.IsNullOrEmpty(boundTo) ? new SummonerId(boundTo) : null;
                var cardsToGrant = Enumerable.Range(0, count)
                    .Select(_ => (typedCatalogId, rarity, binding, typedBoundTo))
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
                var typedSummonerIdToGrant = new SummonerId(summonerIdToGrant);
                if (!_profileRepo.IsSummonerUnlocked(typedSummonerIdToGrant))
                {
                    if (!_profileRepo.UnlockSummoner(typedSummonerIdToGrant))
                    {
                        GD.PushError($"RewardService: Failed to unlock summoner {summonerIdToGrant}");
                        success = false;
                    }
                    else
                    {
                        // Create SummonerInstance for the new summoner
                        var instance = new SummonerInstance
                        {
                            SummonerId = new SummonerId(summonerIdToGrant),
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
                if (!_profileRepo.GrantCosmetic(new CosmeticId(cosmeticId)))
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
                if (!_profileRepo.GrantEmote(new EmoteId(emoteId)))
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
                    if (!_profileRepo.GrantCosmetic(new CosmeticId(cosmeticId)))
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

        var typedCatalogId = new CardId(catalogId);
        var cardsToGrant = Enumerable.Range(0, count)
            .Select(_ => (typedCatalogId, rarity))
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

        var typedSummonerId = new SummonerId(summonerId);
        var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
        progress.Gold += amount;
        _profileRepo.UpdateCampaignProgress(typedSummonerId, progress);

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

        var typedSummonerId = new SummonerId(summonerId);
        if (_profileRepo.IsSummonerUnlocked(typedSummonerId))
        {
            GD.Print($"RewardService: Summoner {summonerId} already unlocked");
            return true;
        }

        if (!_profileRepo.UnlockSummoner(typedSummonerId))
        {
            GD.PushError($"RewardService: Failed to unlock summoner {summonerId}");
            return false;
        }

        // Create instance
        var instance = new SummonerInstance
        {
            SummonerId = new SummonerId(summonerId),
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

        if (!_profileRepo.GrantCosmetic(new CosmeticId(cosmeticId)))
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

        if (!_profileRepo.GrantEmote(new EmoteId(emoteId)))
        {
            GD.PushError($"RewardService: Failed to grant emote {emoteId}");
            return false;
        }

        GD.Print($"RewardService: Granted emote '{emoteId}'");
        return true;
    }

    // =========================================================================
    // BATTLE REWARD SPEC
    // =========================================================================

    /// <summary>
    /// Get typed reward specification for a battle.
    /// Filters out owned cards from flexible reward options.
    /// </summary>
    /// <param name="battleId">Battle ID to get spec for.</param>
    /// <param name="isCompleted">Whether the battle is already completed.</param>
    /// <param name="chosenIndex">Previously chosen option index (-1 if not chosen).</param>
    /// <returns>Typed BattleRewardSpec.</returns>
    public BattleRewardSpec GetBattleRewardSpec(string battleId, bool isCompleted = false, int chosenIndex = -1)
    {
        var ownedIds = GetOwnedCatalogIds();
        return BattleRewardSpec.FromBattleId(battleId, isCompleted, chosenIndex, ownedIds);
    }

    /// <summary>
    /// Get reward specification as Dictionary for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary GetBattleRewardSpecAsDict(string battleId, bool isCompleted = false, int chosenIndex = -1)
    {
        var spec = GetBattleRewardSpec(battleId, isCompleted, chosenIndex);
        return spec.ToDictionary();
    }

    // =========================================================================
    // POOL-BASED REWARD DRAWING
    // =========================================================================

    /// <summary>
    /// Draw cards from a predefined pool (enum-based, type-safe).
    /// </summary>
    /// <param name="poolId">Pool enum ID.</param>
    /// <param name="count">Number of cards to draw.</param>
    /// <param name="excludeOwned">Whether to exclude owned cards.</param>
    /// <param name="uniqueOnly">Whether to ensure no duplicates in result.</param>
    /// <returns>List of card IDs.</returns>
    public List<string> DrawFromPool(
        RewardPoolId poolId,
        int count,
        bool excludeOwned = false,
        bool uniqueOnly = true)
    {
        var excludeIds = excludeOwned ? GetOwnedCatalogIds() : null;
        var cards = RewardPoolCatalog.GetCardsForPool(poolId, excludeIds);

        return DrawRandomCards(cards, count, uniqueOnly);
    }

    /// <summary>
    /// Draw cards using inline filter config.
    /// </summary>
    /// <param name="filterConfig">Filter configuration.</param>
    /// <param name="count">Number of cards to draw.</param>
    /// <param name="excludeOwned">Whether to exclude owned cards.</param>
    /// <param name="uniqueOnly">Whether to ensure no duplicates in result.</param>
    /// <returns>List of card IDs.</returns>
    public List<string> DrawWithFilters(
        CardFilterConfig filterConfig,
        int count,
        bool excludeOwned = false,
        bool uniqueOnly = true)
    {
        var excludeIds = excludeOwned ? GetOwnedCatalogIds() : null;
        var cards = RewardPoolCatalog.FilterCards(filterConfig, excludeIds);

        return DrawRandomCards(cards, count, uniqueOnly);
    }

    /// <summary>
    /// Draw random cards from a candidate set.
    /// </summary>
    private List<string> DrawRandomCards(CardDefinition[] cards, int count, bool uniqueOnly)
    {
        var result = new List<string>();
        var remaining = cards.ToList();

        for (int i = 0; i < count && remaining.Count > 0; i++)
        {
            int idx = _random.Next(remaining.Count);
            result.Add(remaining[idx].Id);

            if (uniqueOnly)
                remaining.RemoveAt(idx);
        }

        return result;
    }

    // =========================================================================
    // GDSCRIPT INTEROP - POOL SYSTEM
    // =========================================================================

    /// <summary>
    /// Draw cards from a predefined pool (GDScript-friendly).
    /// </summary>
    /// <param name="poolId">Pool ID string.</param>
    /// <param name="count">Number of cards to draw.</param>
    /// <param name="excludeOwned">Whether to exclude owned cards.</param>
    /// <param name="uniqueOnly">Whether to ensure no duplicates.</param>
    /// <returns>Array of card IDs.</returns>
    public Godot.Collections.Array<string> DrawFromPoolString(
        string poolId,
        int count,
        bool excludeOwned = false,
        bool uniqueOnly = true)
    {
        if (!RewardPoolCatalog.HasPool(poolId))
        {
            GD.PushWarning($"RewardService: Invalid pool ID: {poolId}");
            return [];
        }

        var cards = DrawFromPool(new RewardPoolId(poolId), count, excludeOwned, uniqueOnly);

        var result = new Godot.Collections.Array<string>();
        foreach (var cardId in cards)
            result.Add(cardId);
        return result;
    }

    /// <summary>
    /// Draw cards using inline filter dictionary (GDScript-friendly).
    /// Filter dict can have: element (int), rarity (int), card_type (int)
    /// </summary>
    public Godot.Collections.Array<string> DrawWithFilterDict(
        Godot.Collections.Dictionary filterDict,
        int count,
        bool excludeOwned = false,
        bool uniqueOnly = true)
    {
        var excludeIds = excludeOwned ? GetOwnedCatalogIds() : null;
        var cards = RewardPoolCatalog.DrawWithFilters(filterDict, excludeIds);
        var drawn = DrawRandomCards(cards, count, uniqueOnly);

        var result = new Godot.Collections.Array<string>();
        foreach (var cardId in drawn)
            result.Add(cardId);
        return result;
    }

    // =========================================================================
    // GDSCRIPT INTEROP - REWARD GRANTING
    // =========================================================================

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
