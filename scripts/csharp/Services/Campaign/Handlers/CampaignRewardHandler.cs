using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Services.Economy;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Handles campaign rewards: pending rewards, reward granting, claiming.
/// All rewards are per-summoner (no shared/account-wide campaigns).
/// </summary>
public class CampaignRewardHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly CampaignDataStore _store;
    private readonly Func<string> _getActiveSummonerFunc;
    private readonly Func<string, string, string>? _grantCardFunc;

    public CampaignRewardHandler(
        IProfileRepository profileRepo,
        CampaignDataStore store,
        Func<string> getActiveSummonerFunc,
        Func<string, string, string>? grantCardFunc = null)
    {
        _profileRepo = profileRepo;
        _store = store;
        _getActiveSummonerFunc = getActiveSummonerFunc;
        _grantCardFunc = grantCardFunc;
    }

    /// <summary>Create a new handler with updated callbacks.</summary>
    public CampaignRewardHandler WithCallbacks(Func<string, string, string>? grantCardFunc)
    {
        return new CampaignRewardHandler(
            _profileRepo,
            _store,
            _getActiveSummonerFunc,
            grantCardFunc ?? _grantCardFunc);
    }

    // =========================================================================
    // PENDING REWARD MANAGEMENT
    // =========================================================================

    /// <summary>Set a pending reward for a battle.</summary>
    public void SetPendingReward(string battleId, string rewardType, int choiceIndex = -1)
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return;

        var pending = new System.Collections.Generic.Dictionary<string, object>
        {
            ["battle_id"] = battleId,
            ["reward_type"] = rewardType,
            ["choice_index"] = choiceIndex
        };

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.PendingReward = pending;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignRewardHandler: Set pending reward for battle '{battleId}' (type: {rewardType})");
    }

    /// <summary>Get the current pending reward.</summary>
    public Godot.Collections.Dictionary GetPendingReward()
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return new Godot.Collections.Dictionary();

        var campaignProgress = _profileRepo.GetCampaignProgress(summonerId);

        if (campaignProgress.PendingReward == null || campaignProgress.PendingReward.Count == 0)
            return new Godot.Collections.Dictionary();

        // Convert to Godot Dictionary for GDScript compatibility
        var result = new Godot.Collections.Dictionary();
        foreach (var kvp in campaignProgress.PendingReward)
        {
            result[kvp.Key] = DtoConverters.ObjectToVariant(kvp.Value);
        }
        return result;
    }

    /// <summary>Update choice index for a pending choice reward.</summary>
    public void UpdatePendingChoice(int choiceIndex)
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return;

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        if (progress.PendingReward == null || progress.PendingReward.Count == 0)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to update choice for");
            return;
        }
        progress.PendingReward["choice_index"] = choiceIndex;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignRewardHandler: Updated pending choice to index {choiceIndex}");
    }

    /// <summary>Clear the pending reward.</summary>
    public void ClearPendingReward()
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return;

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.PendingReward = null;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print("CampaignRewardHandler: Cleared pending reward");
    }

    // =========================================================================
    // REWARD GRANTING
    // =========================================================================

    /// <summary>Grant battle reward and return granted card info.</summary>
    /// <remarks>
    /// Note: Gold is now granted via RewardService.grant_battle_rewards() in GDScript.
    /// This method only handles card granting for legacy specific_options support.
    /// </remarks>
    public Godot.Collections.Dictionary GrantBattleReward(string battleId, int chosenIndex = 0)
    {
        if (!_store.Battles.TryGetValue(battleId, out var battle))
        {
            GD.PushError($"CampaignRewardHandler: Battle not found: {battleId}");
            return new Godot.Collections.Dictionary();
        }

        var rewardType = battle.GetValueOrDefault("reward_type", "fixed").AsString();
        var rewardCardsVariant = battle.GetValueOrDefault("reward_cards", new Godot.Collections.Array());

        // Handle case where there are no card rewards
        var rewardCards = rewardCardsVariant.Obj is Godot.Collections.Array arr ? arr : new Godot.Collections.Array();
        if (rewardCards.Count == 0)
        {
            GD.PushWarning($"CampaignRewardHandler: No card rewards defined for battle '{battleId}'");
            return new Godot.Collections.Dictionary();
        }

        var grantedCard = new Godot.Collections.Dictionary();
        var grantedInstanceIds = new Godot.Collections.Array<string>();

        if (rewardType == "fixed")
        {
            // Grant all reward cards
            foreach (var rewardVariant in rewardCards)
            {
                if (rewardVariant.Obj is Godot.Collections.Dictionary reward)
                {
                    var ids = GrantRewardCard(reward);
                    foreach (var id in ids)
                    {
                        grantedInstanceIds.Add(id);
                    }
                }
            }

            if (rewardCards.Count > 0 && rewardCards[0].Obj is Godot.Collections.Dictionary firstCard)
            {
                grantedCard = firstCard;
            }
        }
        else if (rewardType == "flexible")
        {
            // FLEXIBLE rewards with specific_options (legacy CHOICE/RANDOM behavior)
            var specificOptionsVariant = battle.GetValueOrDefault("specific_options", new Godot.Collections.Array());
            if (specificOptionsVariant.Obj is Godot.Collections.Array specificOptions && specificOptions.Count > 0)
            {
                var playerSelects = battle.GetValueOrDefault("player_selects", true).AsBool();

                if (chosenIndex >= 0 && chosenIndex < specificOptions.Count &&
                    specificOptions[chosenIndex].Obj is Godot.Collections.Dictionary chosenReward)
                {
                    var ids = GrantRewardCard(chosenReward);
                    foreach (var id in ids) grantedInstanceIds.Add(id);
                    grantedCard = chosenReward;
                }
                else if (!playerSelects && specificOptions.Count > 0)
                {
                    // Legacy RANDOM: auto-grant random option
                    var randomIndex = new Random().Next(specificOptions.Count);
                    if (specificOptions[randomIndex].Obj is Godot.Collections.Dictionary randomReward)
                    {
                        var ids = GrantRewardCard(randomReward);
                        foreach (var id in ids) grantedInstanceIds.Add(id);
                        grantedCard = randomReward;
                    }
                }
            }
            // Dynamic pool-based FLEXIBLE rewards are granted directly by reward_screen via RewardService
        }

        // Add instance IDs to return value
        grantedCard["instance_ids"] = grantedInstanceIds;

        return grantedCard;
    }

    private List<string> GrantRewardCard(Godot.Collections.Dictionary reward)
    {
        var instanceIds = new List<string>();

        var catalogId = reward.GetValueOrDefault("catalog_id", "").AsString();
        var rarity = reward.GetValueOrDefault("rarity", "common").AsString();
        var count = reward.GetValueOrDefault("count", 1).AsInt32();

        if (_grantCardFunc == null)
        {
            GD.PushWarning("CampaignRewardHandler: No collection service - skipping card grant");
            return instanceIds;
        }

        for (var i = 0; i < count; i++)
        {
            var instanceId = _grantCardFunc(catalogId, rarity);
            instanceIds.Add(instanceId);
        }

        GD.Print($"CampaignRewardHandler: Granted {count}x {catalogId} ({rarity})");
        return instanceIds;
    }

    /// <summary>Claim the pending reward (grants cards and marks battle complete).</summary>
    public (Godot.Collections.Dictionary grantedCard, string battleId) ClaimPendingReward()
    {
        var pending = GetPendingReward();
        if (pending.Count == 0)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to claim");
            return (new Godot.Collections.Dictionary(), "");
        }

        var battleId = pending.GetValueOrDefault("battle_id", "").AsString();
        var rewardType = pending.GetValueOrDefault("reward_type", "").AsString();
        var choiceIndex = pending.GetValueOrDefault("choice_index", 0).AsInt32();

        if (string.IsNullOrEmpty(battleId))
        {
            GD.PushError("CampaignRewardHandler: Invalid pending reward - no battle_id");
            return (new Godot.Collections.Dictionary(), "");
        }

        // For flexible rewards with player selection, ensure a choice was made
        if (rewardType == "flexible" && choiceIndex < 0)
        {
            if (_store.Battles.TryGetValue(battleId, out var battle))
            {
                var hasSpecificOptions = battle.GetValueOrDefault("specific_options", new Godot.Collections.Array()).Obj is Godot.Collections.Array arr && arr.Count > 0;
                var playerSelects = battle.GetValueOrDefault("player_selects", true).AsBool();

                if (hasSpecificOptions && playerSelects)
                {
                    GD.PushError("CampaignRewardHandler: Cannot claim flexible reward without making a choice");
                    return (new Godot.Collections.Dictionary(), "");
                }
            }
        }

        // Grant the reward
        var grantedCard = GrantBattleReward(battleId, choiceIndex);

        GD.Print($"CampaignRewardHandler: Claimed reward for battle '{battleId}'");
        return (grantedCard, battleId);
    }
}
