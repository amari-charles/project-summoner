using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Economy;

namespace Fateforged.Meta.Campaign.Handlers;

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

        var pending = new PendingRewardData
        {
            BattleId = battleId,
            RewardType = rewardType,
            ChoiceIndex = choiceIndex
        };

        var typedSummonerId = new SummonerId(summonerId);
        var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
        progress.PendingReward = pending;
        _profileRepo.UpdateCampaignProgress(typedSummonerId, progress);

        GD.Print($"CampaignRewardHandler: Set pending reward for battle '{battleId}' (type: {rewardType})");
    }

    /// <summary>Get the current pending reward as typed data.</summary>
    public PendingRewardData? GetPendingRewardData()
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return null;

        return _profileRepo.GetCampaignProgress(new SummonerId(summonerId)).PendingReward;
    }

    /// <summary>Get the current pending reward as Godot Dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetPendingReward()
    {
        var pending = GetPendingRewardData();
        if (pending == null) return new Godot.Collections.Dictionary();

        var result = new Godot.Collections.Dictionary
        {
            ["battle_id"] = pending.BattleId,
            ["reward_type"] = pending.RewardType,
            ["choice_index"] = pending.ChoiceIndex
        };

        if (pending.CaravanPurchases.Count > 0)
        {
            var arr = new Godot.Collections.Array();
            foreach (var p in pending.CaravanPurchases) arr.Add(p);
            result["caravan_purchases"] = arr;
        }

        return result;
    }

    /// <summary>Update choice index for a pending choice reward.</summary>
    public void UpdatePendingChoice(int choiceIndex)
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return;

        var typedSummonerId = new SummonerId(summonerId);
        var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
        if (progress.PendingReward == null)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to update choice for");
            return;
        }
        progress.PendingReward.ChoiceIndex = choiceIndex;
        _profileRepo.UpdateCampaignProgress(typedSummonerId, progress);

        GD.Print($"CampaignRewardHandler: Updated pending choice to index {choiceIndex}");
    }

    /// <summary>Clear the pending reward.</summary>
    public void ClearPendingReward()
    {
        var summonerId = _getActiveSummonerFunc();
        if (string.IsNullOrEmpty(summonerId)) return;

        var typedSummonerId = new SummonerId(summonerId);
        var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
        progress.PendingReward = null;
        _profileRepo.UpdateCampaignProgress(typedSummonerId, progress);

        GD.Print("CampaignRewardHandler: Cleared pending reward");
    }

    // =========================================================================
    // REWARD GRANTING
    // =========================================================================

    /// <summary>Grant battle reward and return granted card info.</summary>
    /// <remarks>
    /// Note: Gold and flexible rewards are now granted via RewardService.grant_battle_rewards() in GDScript.
    /// This method handles FIXED reward card granting only.
    /// </remarks>
    public Godot.Collections.Dictionary GrantBattleReward(string battleId, int chosenIndex = 0)
    {
        if (!_store.Battles.TryGetValue(battleId, out var battle))
        {
            GD.PushError($"CampaignRewardHandler: Battle not found: {battleId}");
            return new Godot.Collections.Dictionary();
        }

        var rewardTypeStr = battle.GetValueOrDefault("reward_type", "fixed").AsString();
        var rewardType = RewardTypeExtensions.FromStringId(rewardTypeStr);

        // Only handle FIXED rewards - FLEXIBLE rewards are granted via RewardService
        if (rewardType != RewardType.Fixed)
        {
            return new Godot.Collections.Dictionary();
        }

        var rewardCardsVariant = battle.GetValueOrDefault("reward_cards", new Godot.Collections.Array());
        var rewardCards = rewardCardsVariant.Obj is Godot.Collections.Array arr ? arr : new Godot.Collections.Array();

        if (rewardCards.Count == 0)
        {
            GD.PushWarning($"CampaignRewardHandler: No card rewards defined for battle '{battleId}'");
            return new Godot.Collections.Dictionary();
        }

        var grantedCard = new Godot.Collections.Dictionary();
        var grantedInstanceIds = new Godot.Collections.Array<string>();

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
        var pending = GetPendingRewardData();
        if (pending == null)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to claim");
            return (new Godot.Collections.Dictionary(), "");
        }

        if (string.IsNullOrEmpty(pending.BattleId))
        {
            GD.PushError("CampaignRewardHandler: Invalid pending reward - no battle_id");
            return (new Godot.Collections.Dictionary(), "");
        }

        // Grant the reward (only FIXED rewards - FLEXIBLE handled by RewardService)
        var grantedCard = GrantBattleReward(pending.BattleId, pending.ChoiceIndex);

        GD.Print($"CampaignRewardHandler: Claimed reward for battle '{pending.BattleId}'");
        return (grantedCard, pending.BattleId);
    }
}
