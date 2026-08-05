using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Godot;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Handles campaign progress: load, save, battle completion.
/// All progress is per-summoner (no shared/account-wide campaigns).
/// Fully typed API — facades handle string conversion.
/// </summary>
public class CampaignProgressHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly CampaignDataStore _store;
    private readonly Func<SummonerId> _getActiveSummonerFunc;
    private readonly ChoiceTracker? _choiceTracker;
    private readonly CampaignGraphStore? _graphStore;

    public CampaignProgressHandler(
        IProfileRepository profileRepo,
        CampaignDataStore store,
        Func<SummonerId> getActiveSummonerFunc,
        ChoiceTracker? choiceTracker = null,
        CampaignGraphStore? graphStore = null
    )
    {
        _profileRepo = profileRepo;
        _store = store;
        _getActiveSummonerFunc = getActiveSummonerFunc;
        _choiceTracker = choiceTracker;
        _graphStore = graphStore;
    }

    // =========================================================================
    // PROGRESS MANAGEMENT
    // =========================================================================

    /// <summary>Load progress from profile repository.</summary>
    public void LoadProgress()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return;

        var campaignProgress = _profileRepo.GetCampaignProgress(summonerId);

        _store.CompletedBattles.Clear();
        _store.CompletedBattles.AddRange(campaignProgress.CompletedBattles);

        // Load choices into ChoiceTracker
        if (_choiceTracker != null)
            _choiceTracker.LoadChoices(campaignProgress.Choices);

        GD.Print(
            $"CampaignProgressHandler: Loaded progress for '{_store.CurrentCampaignId}' summoner '{summonerId}' - {_store.CompletedBattles.Count} nodes completed, {campaignProgress.Choices.Count} choices"
        );
    }

    /// <summary>Save progress to profile repository.</summary>
    public void SaveProgress()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return;

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.CompletedBattles = new List<BattleId>(_store.CompletedBattles);

        // Save choices from ChoiceTracker
        if (_choiceTracker != null)
        {
            progress.Choices = _choiceTracker.GetAllChoices();
        }

        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print(
            $"CampaignProgressHandler: Saved progress for '{_store.CurrentCampaignId}' summoner '{summonerId}' - {_store.CompletedBattles.Count} nodes completed, {progress.Choices.Count} choices"
        );
    }

    /// <summary>Set the current campaign ID.</summary>
    public void SetCurrentCampaign(CampaignId campaignId)
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
    public CampaignId GetCurrentCampaignId() => _store.CurrentCampaignId;

    // =========================================================================
    // BATTLE COMPLETION
    // =========================================================================

    /// <summary>Check if a battle is completed.</summary>
    public bool IsBattleCompleted(BattleId battleId) => _store.CompletedBattles.Contains(battleId);

    /// <summary>Complete a battle (marks as completed and saves progress).</summary>
    public void CompleteBattle(BattleId battleId)
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

    /// <summary>
    /// Check if a campaign is complete.
    /// A campaign is complete if ANY end node (node with no outgoing edges) is completed.
    /// </summary>
    public bool IsCampaignComplete(CampaignId campaignId)
    {
        if (_graphStore != null)
        {
            var graph = _graphStore.GetGraph(campaignId);
            if (graph != null)
            {
                var endNodes = graph.GetEndNodes();
                if (endNodes.Count > 0)
                {
                    foreach (var endNode in endNodes)
                    {
                        if (_store.CompletedBattles.Any(b => b.Value == endNode.Id.Value))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        return false;
    }

    /// <summary>Clear all progress data for the current summoner.</summary>
    public void ResetProgress()
    {
        var summonerId = _getActiveSummonerFunc();
        if (!summonerId.HasValue)
            return;

        _store.CompletedBattles.Clear();
        _choiceTracker?.ClearAll();

        var progress = _profileRepo.GetCampaignProgress(summonerId);
        progress.CompletedBattles = [];
        progress.Choices = [];
        progress.ActiveBattleAttempt = null;
        progress.BattleAttemptCompletions.Clear();
        _profileRepo.UpdateCampaignProgress(summonerId, progress);

        GD.Print($"CampaignProgressHandler: Reset progress for summoner '{summonerId}'");
    }
}
