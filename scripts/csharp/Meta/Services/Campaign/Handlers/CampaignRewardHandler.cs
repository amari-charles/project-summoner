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
/// Fully typed API — facades handle string conversion.
/// </summary>
public class CampaignRewardHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly CampaignDataStore _store;
    private readonly Func<SummonerId> _getActiveSummonerFunc;
    private readonly Func<string, string, string>? _grantCardFunc;

    public CampaignRewardHandler(
        IProfileRepository profileRepo,
        CampaignDataStore store,
        Func<SummonerId> getActiveSummonerFunc,
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
    public void SetPendingReward(BattleId battleId, RewardType rewardType, int choiceIndex = -1)
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue) return;

        var pending = new PendingRewardData
        {
            BattleId = battleId,
            RewardType = rewardType,
            ChoiceIndex = choiceIndex
        };

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.PendingReward = pending;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignRewardHandler: Set pending reward for battle '{battleId}' (type: {rewardType})");
    }

    /// <summary>Get the current pending reward as typed data.</summary>
    public PendingRewardData? GetPendingRewardData()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue) return null;

        return _profileRepo.GetCampaignProgress(summonerId).PendingReward;
    }

    /// <summary>Get the current pending reward as Godot Dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetPendingReward()
    {
        var pending = GetPendingRewardData();
        return pending != null ? DtoConverters.ToDict(pending) : new Godot.Collections.Dictionary();
    }

    /// <summary>Update choice index for a pending choice reward.</summary>
    public void UpdatePendingChoice(int choiceIndex)
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue) return;

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        if (progress.PendingReward == null)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to update choice for");
            return;
        }
        progress.PendingReward.ChoiceIndex = choiceIndex;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignRewardHandler: Updated pending choice to index {choiceIndex}");
    }

    /// <summary>Clear the pending reward.</summary>
    public void ClearPendingReward()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue) return;

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
    /// Note: Gold and flexible rewards are now granted via RewardService.grant_battle_rewards() in GDScript.
    /// This method handles FIXED reward card granting only.
    /// Uses typed EventDefinition from the store instead of string-keyed dict access.
    /// </remarks>
    public Godot.Collections.Dictionary GrantBattleReward(BattleId battleId, int chosenIndex = 0)
    {
        var eventId = new EventId(battleId.Value);
        if (!_store.Events.TryGetValue(eventId, out var evt))
        {
            GD.PushError($"CampaignRewardHandler: Battle not found: {battleId}");
            return new Godot.Collections.Dictionary();
        }

        // Only battle-type events have rewards
        if (evt is not BattleEventDefinition battleEvt)
        {
            return new Godot.Collections.Dictionary();
        }

        // Only handle FIXED rewards - FLEXIBLE rewards are granted via RewardService
        if (battleEvt.Rewards.Type != RewardType.Fixed)
        {
            return new Godot.Collections.Dictionary();
        }

        var fixedCards = battleEvt.Rewards.FixedCards;
        if (fixedCards.Count == 0)
        {
            GD.PushWarning($"CampaignRewardHandler: No card rewards defined for battle '{battleId}'");
            return new Godot.Collections.Dictionary();
        }

        var grantedCard = new Godot.Collections.Dictionary();
        var grantedInstanceIds = new Godot.Collections.Array<string>();

        // Grant all reward cards
        foreach (var rewardCard in fixedCards)
        {
            var ids = GrantRewardCard(rewardCard);
            foreach (var id in ids)
            {
                grantedInstanceIds.Add(id);
            }
        }

        if (fixedCards.Count > 0)
        {
            var first = fixedCards[0];
            grantedCard["catalog_id"] = (string)first.CardId;
            grantedCard["rarity"] = first.Rarity;
            grantedCard["count"] = first.Count;
        }

        grantedCard["instance_ids"] = grantedInstanceIds;

        return grantedCard;
    }

    private List<string> GrantRewardCard(FixedRewardEntry reward)
    {
        var instanceIds = new List<string>();

        if (_grantCardFunc == null)
        {
            GD.PushWarning("CampaignRewardHandler: No collection service - skipping card grant");
            return instanceIds;
        }

        for (var i = 0; i < reward.Count; i++)
        {
            var instanceId = _grantCardFunc(reward.CardId, reward.Rarity);
            instanceIds.Add(instanceId);
        }

        GD.Print($"CampaignRewardHandler: Granted {reward.Count}x {reward.CardId} ({reward.Rarity})");
        return instanceIds;
    }

    /// <summary>Claim the pending reward (grants cards and marks battle complete).</summary>
    public (Godot.Collections.Dictionary grantedCard, BattleId battleId) ClaimPendingReward()
    {
        var pending = GetPendingRewardData();
        if (pending == null)
        {
            GD.PushWarning("CampaignRewardHandler: No pending reward to claim");
            return (new Godot.Collections.Dictionary(), BattleId.None);
        }

        if (!pending.BattleId.HasValue)
        {
            GD.PushError("CampaignRewardHandler: Invalid pending reward - no battle_id");
            return (new Godot.Collections.Dictionary(), BattleId.None);
        }

        var grantedCard = GrantBattleReward(pending.BattleId, pending.ChoiceIndex);

        GD.Print($"CampaignRewardHandler: Claimed reward for battle '{pending.BattleId}'");
        return (grantedCard, pending.BattleId);
    }
}
