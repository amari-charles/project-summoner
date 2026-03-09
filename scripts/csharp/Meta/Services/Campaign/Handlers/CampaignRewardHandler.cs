using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;

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
    public void SetPendingReward(BattleId battleId, Data.Events.RewardType rewardType, int choiceIndex = -1)
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

    /// <summary>Update choice selection for a pending choice reward.</summary>
    public void UpdatePendingChoice(int choiceIndex, string chosenCatalogId = "")
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
        progress.PendingReward.ChosenCatalogId = chosenCatalogId ?? "";
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignRewardHandler: Updated pending choice to index {choiceIndex}, catalog_id '{progress.PendingReward.ChosenCatalogId}'");
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

    /// <summary>
    /// Grant battle reward and return granted reward payload.
    /// Uses BattleRewardSpec so grant behavior matches the reward screen (filtering + choice index semantics).
    /// </summary>
    public Godot.Collections.Dictionary GrantBattleReward(BattleId battleId, int chosenIndex = 0, string chosenCatalogId = "")
    {
        var granted = new Godot.Collections.Dictionary();
        var grantedCards = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var grantedInstanceIds = new Godot.Collections.Array<string>();

        var spec = BuildRewardSpecForClaim(battleId, chosenIndex);
        GrantCampaignGold(spec.GoldReward);
        granted["campaign_gold"] = spec.GoldReward;

        foreach (var option in ResolveCardOptionsToGrant(spec, chosenIndex, chosenCatalogId))
        {
            var ids = GrantRewardCard(option.CatalogId, option.Rarity, option.Count);
            foreach (var id in ids)
                grantedInstanceIds.Add(id);

            grantedCards.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = option.CatalogId,
                ["rarity"] = option.Rarity,
                ["count"] = option.Count
            });
        }

        if (grantedCards.Count > 0)
        {
            var first = grantedCards[0];
            granted["catalog_id"] = first["catalog_id"];
            granted["rarity"] = first["rarity"];
            granted["count"] = first["count"];
        }

        granted["cards"] = grantedCards;
        granted["instance_ids"] = grantedInstanceIds;
        return granted;
    }

    private BattleRewardSpec BuildRewardSpecForClaim(BattleId battleId, int chosenIndex)
    {
        var ownedCatalogIds = _profileRepo
            .ListCards()
            .Select(card => (string)card.CatalogId)
            .ToHashSet();

        return BattleRewardSpec.FromBattleId((string)battleId, isCompleted: false, chosenIndex, ownedCatalogIds);
    }

    private IEnumerable<CardRewardOption> ResolveCardOptionsToGrant(BattleRewardSpec spec, int chosenIndex, string chosenCatalogId)
    {
        if (spec.CardOptions.Count == 0)
            yield break;

        switch (spec.Type)
        {
            case Data.Events.RewardType.Fixed:
                foreach (var option in spec.CardOptions)
                    yield return option;
                yield break;

            case Data.Events.RewardType.Flexible:
                if (!string.IsNullOrEmpty(chosenCatalogId))
                {
                    var selected = spec.CardOptions.FirstOrDefault(o => o.CatalogId == chosenCatalogId);
                    if (selected != null)
                    {
                        yield return selected;
                        yield break;
                    }

                    GD.PushWarning($"CampaignRewardHandler: Chosen catalog_id '{chosenCatalogId}' not present in current options, falling back to index");
                }

                var index = chosenIndex >= 0 ? chosenIndex : 0;
                if (index >= spec.CardOptions.Count)
                {
                    GD.PushWarning($"CampaignRewardHandler: Choice index {chosenIndex} out of range for flexible reward, defaulting to first option");
                    index = 0;
                }
                yield return spec.CardOptions[index];
                yield break;

            default:
                yield break;
        }
    }

    private List<string> GrantRewardCard(string catalogId, string rarity, int count)
    {
        var instanceIds = new List<string>();
        if (count <= 0)
            return instanceIds;

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

    private void GrantCampaignGold(int amount)
    {
        if (amount <= 0)
            return;

        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
        {
            GD.PushWarning("CampaignRewardHandler: Cannot grant campaign gold - no active summoner");
            return;
        }

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.Gold += amount;
        _profileRepo.UpdateCampaignProgress(summonerId, progress);
        GD.Print($"CampaignRewardHandler: Granted {amount} campaign gold");
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

        var grantedCard = GrantBattleReward(pending.BattleId, pending.ChoiceIndex, pending.ChosenCatalogId);

        GD.Print($"CampaignRewardHandler: Claimed reward for battle '{pending.BattleId}'");
        return (grantedCard, pending.BattleId);
    }
}
