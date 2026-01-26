using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Domain.Profile.Campaign;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Handles campaign progress: load, save, battle completion.
/// </summary>
public class CampaignProgressHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly CampaignDataStore _store;
    private readonly Func<string> _getActiveSummonerFunc;

    public CampaignProgressHandler(
        IProfileRepository profileRepo,
        CampaignDataStore store,
        Func<string> getActiveSummonerFunc)
    {
        _profileRepo = profileRepo;
        _store = store;
        _getActiveSummonerFunc = getActiveSummonerFunc;
    }

    // =========================================================================
    // PROGRESS MANAGEMENT
    // =========================================================================

    /// <summary>Load progress from profile repository.</summary>
    public void LoadProgress()
    {
        CampaignProgress campaignProgress;
        if (_store.IsSharedCampaign(_store.CurrentCampaignId))
        {
            campaignProgress = _profileRepo.GetSharedCampaignProgress();
        }
        else
        {
            var summonerId = _getActiveSummonerFunc();
            if (string.IsNullOrEmpty(summonerId)) return;
            campaignProgress = _profileRepo.GetCampaignProgress(summonerId);
        }

        _store.CompletedBattles.Clear();
        _store.CompletedBattles.AddRange(campaignProgress.CompletedBattles);

        GD.Print($"CampaignProgressHandler: Loaded progress for '{_store.CurrentCampaignId}' (shared={_store.IsSharedCampaign(_store.CurrentCampaignId)}) - {_store.CompletedBattles.Count} battles completed");
    }

    /// <summary>Save progress to profile repository.</summary>
    public void SaveProgress()
    {
        if (_store.IsSharedCampaign(_store.CurrentCampaignId))
        {
            var progress = _profileRepo.GetSharedCampaignProgress();
            progress.CompletedBattles = [.. _store.CompletedBattles];
            _profileRepo.UpdateSharedCampaignProgress(progress);
        }
        else
        {
            var summonerId = _getActiveSummonerFunc();
            if (string.IsNullOrEmpty(summonerId)) return;
            var progress = _profileRepo.GetCampaignProgress(summonerId);
            progress.CompletedBattles = [.. _store.CompletedBattles];
            _profileRepo.UpdateCampaignProgress(summonerId, progress);
        }

        GD.Print($"CampaignProgressHandler: Saved progress for '{_store.CurrentCampaignId}' (shared={_store.IsSharedCampaign(_store.CurrentCampaignId)}) - {_store.CompletedBattles.Count} battles completed");
    }

    /// <summary>Set the current campaign ID.</summary>
    public void SetCurrentCampaign(string campaignId)
    {
        if (!_store.Campaigns.ContainsKey(campaignId))
        {
            GD.PushWarning($"CampaignProgressHandler: Invalid campaign ID: {campaignId}");
            return;
        }

        _store.CurrentCampaignId = campaignId;
        LoadProgress();
    }

    /// <summary>Get the current campaign ID.</summary>
    public string GetCurrentCampaignId() => _store.CurrentCampaignId;

    // =========================================================================
    // BATTLE COMPLETION
    // =========================================================================

    /// <summary>Check if a battle is completed.</summary>
    public bool IsBattleCompleted(string battleId) => _store.CompletedBattles.Contains(battleId);

    /// <summary>Complete a battle (marks as completed and saves progress).</summary>
    public void CompleteBattle(string battleId)
    {
        if (IsBattleCompleted(battleId))
        {
            GD.PushWarning($"CampaignProgressHandler: Battle '{battleId}' already completed");
            return;
        }

        _store.CompletedBattles.Add(battleId);
        SaveProgress();

        GD.Print($"CampaignProgressHandler: Battle '{battleId}' completed");
    }

    /// <summary>Check if a campaign is complete (all battles finished).</summary>
    public bool IsCampaignComplete(string campaignId)
    {
        if (!_store.Campaigns.TryGetValue(campaignId, out var campaign))
            return false;

        var battlesVariant = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
        if (battlesVariant.Obj is not Godot.Collections.Array battles || battles.Count == 0)
            return false;

        // Get completed battles for this campaign
        List<string> completed;
        if (_store.IsSharedCampaign(campaignId))
        {
            var sharedProgress = _profileRepo.GetSharedCampaignProgress();
            completed = sharedProgress.CompletedBattles;
        }
        else
        {
            var summonerId = _getActiveSummonerFunc();
            if (string.IsNullOrEmpty(summonerId)) return false;
            var summonerProgress = _profileRepo.GetCampaignProgress(summonerId);
            completed = summonerProgress.CompletedBattles;
        }

        // Check if all battles in campaign are completed
        foreach (var battleVariant in battles)
        {
            if (battleVariant.Obj is Godot.Collections.Dictionary battle)
            {
                var battleId = battle.GetValueOrDefault("id", "").AsString();
                if (!string.IsNullOrEmpty(battleId) && !completed.Contains(battleId))
                    return false;
            }
        }

        return true;
    }

    /// <summary>Check if onboarding is complete.</summary>
    public bool IsOnboardingComplete()
    {
        return IsCampaignComplete("onboarding");
    }
}
