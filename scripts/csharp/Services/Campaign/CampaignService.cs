using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Campaign;

/// <summary>
/// Campaign Service - Manages campaign progression and battle rewards.
///
/// Tracks which battles have been completed and handles reward distribution.
/// Battle definitions are loaded from GDScript data files.
/// </summary>
[GlobalClass]
public partial class CampaignService : Node
{
    public static CampaignService? Instance { get; private set; }

    [Signal]
    public delegate void BattleCompletedEventHandler(string battleId);

    [Signal]
    public delegate void BattleUnlockedEventHandler(string battleId);

    [Signal]
    public delegate void CampaignProgressChangedEventHandler();

    [Signal]
    public delegate void CampaignChangedEventHandler(string oldCampaignId, string newCampaignId);

    private IProfileRepository? _profileRepo;

    // Campaign data (loaded from GDScript)
    private readonly Dictionary<string, Godot.Collections.Dictionary> _campaigns = [];
    private readonly Dictionary<string, Godot.Collections.Dictionary> _battles = [];
    private readonly HashSet<string> _sharedCampaigns = [];
    private string _currentCampaignId = "";
    private readonly List<string> _completedBattles = [];

    // Callbacks for GDScript dependencies
    private Func<int>? _getCampaignGoldFunc;
    private Action<int>? _addCampaignGoldFunc;
    private Func<string, string, string>? _grantCardFunc; // (catalogId, rarity) -> instanceId
    private Func<string>? _getActiveSummonerFunc;
    private Action<string>? _clearCampaignGoldFunc;

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
        GD.Print("CampaignService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("CampaignService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        GD.Print("CampaignService: Ready");
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
    // CALLBACK INJECTION (from GDScript wrapper)
    // =========================================================================

    /// <summary>Set Economy service callbacks.</summary>
    public void SetEconomyCallbacks(Callable getCampaignGold, Callable addCampaignGold, Callable clearCampaignGold)
    {
        _getCampaignGoldFunc = () => getCampaignGold.Call().AsInt32();
        _addCampaignGoldFunc = (amount) => addCampaignGold.Call(amount);
        _clearCampaignGoldFunc = (summonerId) => clearCampaignGold.Call(summonerId);
    }

    /// <summary>Set Collection service callbacks.</summary>
    public void SetCollectionCallbacks(Callable grantCard)
    {
        _grantCardFunc = (catalogId, rarity) => grantCard.Call(catalogId, rarity).AsString();
    }

    /// <summary>Set active summoner getter.</summary>
    public void SetActiveSummonerGetter(Callable getter)
    {
        _getActiveSummonerFunc = () => getter.Call().AsString();
    }

    // =========================================================================
    // CAMPAIGN DATA LOADING (from GDScript)
    // =========================================================================

    /// <summary>Load campaign data from GDScript (called by GDScript wrapper after loading data files).</summary>
    public void LoadCampaignsFromGDScript(Godot.Collections.Array<Godot.Collections.Dictionary> campaigns)
    {
        _campaigns.Clear();
        _battles.Clear();
        _sharedCampaigns.Clear();

        foreach (var campaign in campaigns)
        {
            var campaignId = campaign.GetValueOrDefault("campaign_id", "").AsString();
            if (string.IsNullOrEmpty(campaignId))
                continue;

            _campaigns[campaignId] = campaign;

            // Track shared campaigns
            if (campaign.GetValueOrDefault("is_shared", false).AsBool())
            {
                _sharedCampaigns.Add(campaignId);
            }

            // Load battles from campaign
            var battlesArray = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
            if (battlesArray.Obj is Godot.Collections.Array battles)
            {
                foreach (var battleVariant in battles)
                {
                    if (battleVariant.Obj is Godot.Collections.Dictionary battle)
                    {
                        var battleId = battle.GetValueOrDefault("id", "").AsString();
                        if (!string.IsNullOrEmpty(battleId))
                        {
                            _battles[battleId] = battle;
                        }
                    }
                }
            }
        }

        GD.Print($"CampaignService: Loaded {_campaigns.Count} campaigns with {_battles.Count} total battles");
    }

    /// <summary>Set the current campaign ID.</summary>
    public void SetCurrentCampaign(string campaignId)
    {
        if (!_campaigns.ContainsKey(campaignId))
        {
            GD.PushWarning($"CampaignService: Invalid campaign ID: {campaignId}");
            return;
        }

        var oldId = _currentCampaignId;
        _currentCampaignId = campaignId;
        LoadProgress();

        if (oldId != campaignId)
        {
            EmitSignal(SignalName.CampaignChanged, oldId, campaignId);
        }
    }

    /// <summary>Get the current campaign ID.</summary>
    public string GetCurrentCampaignId() => _currentCampaignId;

    // =========================================================================
    // PROGRESS MANAGEMENT
    // =========================================================================

    /// <summary>Load progress from profile repository.</summary>
    public void LoadProgress()
    {
        if (_profileRepo == null) return;

        CampaignProgressData campaignProgress;
        if (IsSharedCampaign(_currentCampaignId))
        {
            campaignProgress = _profileRepo.GetSharedCampaignProgress();
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return;
            campaignProgress = _profileRepo.GetCampaignProgress(summonerId);
        }

        _completedBattles.Clear();
        _completedBattles.AddRange(campaignProgress.CompletedBattles);

        GD.Print($"CampaignService: Loaded progress for '{_currentCampaignId}' (shared={IsSharedCampaign(_currentCampaignId)}) - {_completedBattles.Count} battles completed");
    }

    /// <summary>Save progress to profile repository.</summary>
    public void SaveProgress()
    {
        if (_profileRepo == null) return;

        if (IsSharedCampaign(_currentCampaignId))
        {
            var progress = _profileRepo.GetSharedCampaignProgress();
            progress.CompletedBattles = [.. _completedBattles];
            _profileRepo.UpdateSharedCampaignProgress(progress);
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return;
            var progress = _profileRepo.GetCampaignProgress(summonerId);
            progress.CompletedBattles = [.. _completedBattles];
            _profileRepo.UpdateCampaignProgress(summonerId, progress);
        }

        GD.Print($"CampaignService: Saved progress for '{_currentCampaignId}' (shared={IsSharedCampaign(_currentCampaignId)}) - {_completedBattles.Count} battles completed");
    }

    /// <summary>Check if a campaign uses shared (account-wide) progress.</summary>
    public bool IsSharedCampaign(string campaignId) => _sharedCampaigns.Contains(campaignId);

    // =========================================================================
    // CAMPAIGN QUERIES
    // =========================================================================

    /// <summary>Get all campaigns with unlock status.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCampaigns()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var kvp in _campaigns)
        {
            var campaign = new Godot.Collections.Dictionary();
            foreach (var key in kvp.Value.Keys)
            {
                campaign[key] = kvp.Value[key];
            }
            campaign["is_unlocked"] = IsCampaignUnlocked(kvp.Key);
            result.Add(campaign);
        }

        // Sort by sort_order
        var sorted = result.OrderBy(c => c.GetValueOrDefault("sort_order", 999).AsInt32()).ToList();
        result.Clear();
        foreach (var c in sorted)
        {
            result.Add(c);
        }

        return result;
    }

    /// <summary>Get a specific campaign's metadata.</summary>
    public Godot.Collections.Dictionary GetCampaign(string campaignId)
    {
        return _campaigns.GetValueOrDefault(campaignId, []);
    }

    /// <summary>Check if a campaign is unlocked.</summary>
    public bool IsCampaignUnlocked(string campaignId)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign))
            return false;

        var requirements = campaign.GetValueOrDefault("unlock_requirements", new Godot.Collections.Array());
        if (requirements.Obj is not Godot.Collections.Array reqArray || reqArray.Count == 0)
            return true;

        foreach (var req in reqArray)
        {
            var reqStr = req.AsString();
            if (!IsCampaignComplete(reqStr))
                return false;
        }

        return true;
    }

    /// <summary>Check if a campaign is complete (all battles finished).</summary>
    public bool IsCampaignComplete(string campaignId)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign))
            return false;

        var battlesVariant = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
        if (battlesVariant.Obj is not Godot.Collections.Array battles || battles.Count == 0)
            return false;

        // Get completed battles for this campaign
        List<string> completed;
        if (IsSharedCampaign(campaignId))
        {
            var sharedProgress = _profileRepo?.GetSharedCampaignProgress() ?? new CampaignProgressData();
            completed = sharedProgress.CompletedBattles;
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return false;
            var summonerProgress = _profileRepo?.GetCampaignProgress(summonerId) ?? new CampaignProgressData();
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

    // =========================================================================
    // BATTLE QUERIES
    // =========================================================================

    /// <summary>Get all battles for the current campaign.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!_campaigns.TryGetValue(_currentCampaignId, out var campaign))
            return result;

        var battlesVariant = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
        if (battlesVariant.Obj is Godot.Collections.Array battles)
        {
            foreach (var battleVariant in battles)
            {
                if (battleVariant.Obj is Godot.Collections.Dictionary battle)
                {
                    result.Add(battle);
                }
            }
        }

        return result;
    }

    /// <summary>Get a specific battle by ID.</summary>
    public Godot.Collections.Dictionary GetBattle(string battleId)
    {
        return _battles.GetValueOrDefault(battleId, []);
    }

    /// <summary>Check if a battle is completed.</summary>
    public bool IsBattleCompleted(string battleId) => _completedBattles.Contains(battleId);

    /// <summary>Check if a battle is unlocked.</summary>
    public bool IsBattleUnlocked(string battleId)
    {
        if (!_battles.TryGetValue(battleId, out var battle))
            return false;

        var requirements = battle.GetValueOrDefault("unlock_requirements", new Godot.Collections.Array());
        if (requirements.Obj is not Godot.Collections.Array reqArray || reqArray.Count == 0)
            return true;

        foreach (var req in reqArray)
        {
            var reqStr = req.AsString();
            if (!IsBattleCompleted(reqStr))
                return false;
        }

        return true;
    }

    /// <summary>Get all available (unlocked but not completed) battles.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var battle in GetAllBattles())
        {
            var battleId = battle.GetValueOrDefault("id", "").AsString();
            if (IsBattleUnlocked(battleId) && !IsBattleCompleted(battleId))
            {
                result.Add(battle);
            }
        }

        return result;
    }

    /// <summary>Get all completed battles.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCompletedBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var battleId in _completedBattles)
        {
            if (_battles.TryGetValue(battleId, out var battle))
            {
                result.Add(battle);
            }
        }

        return result;
    }

    // =========================================================================
    // PENDING REWARD MANAGEMENT
    // =========================================================================

    /// <summary>Set a pending reward for a battle.</summary>
    public void SetPendingReward(string battleId, string rewardType, int choiceIndex = -1)
    {
        if (_profileRepo == null) return;

        var pending = new Dictionary<string, object>
        {
            ["battle_id"] = battleId,
            ["reward_type"] = rewardType,
            ["choice_index"] = choiceIndex
        };

        if (IsSharedCampaign(_currentCampaignId))
        {
            var progress = _profileRepo.GetSharedCampaignProgress();
            progress.PendingReward = pending;
            _profileRepo.UpdateSharedCampaignProgress(progress);
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return;
            var progress = _profileRepo.GetCampaignProgress(summonerId);
            progress.PendingReward = pending;
            _profileRepo.UpdateCampaignProgress(summonerId, progress);
        }

        GD.Print($"CampaignService: Set pending reward for battle '{battleId}' (type: {rewardType})");
    }

    /// <summary>Get the current pending reward.</summary>
    public Godot.Collections.Dictionary GetPendingReward()
    {
        if (_profileRepo == null) return [];

        CampaignProgressData campaignProgress;
        if (IsSharedCampaign(_currentCampaignId))
        {
            campaignProgress = _profileRepo.GetSharedCampaignProgress();
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return [];
            campaignProgress = _profileRepo.GetCampaignProgress(summonerId);
        }

        if (campaignProgress.PendingReward == null || campaignProgress.PendingReward.Count == 0)
            return [];

        // Convert to Godot Dictionary for GDScript compatibility
        var result = new Godot.Collections.Dictionary();
        foreach (var kvp in campaignProgress.PendingReward)
        {
            result[kvp.Key] = Variant.From(kvp.Value);
        }
        return result;
    }

    /// <summary>Update choice index for a pending choice reward.</summary>
    public void UpdatePendingChoice(int choiceIndex)
    {
        if (_profileRepo == null) return;

        if (IsSharedCampaign(_currentCampaignId))
        {
            var progress = _profileRepo.GetSharedCampaignProgress();
            if (progress.PendingReward == null || progress.PendingReward.Count == 0)
            {
                GD.PushWarning("CampaignService: No pending reward to update choice for");
                return;
            }
            progress.PendingReward["choice_index"] = choiceIndex;
            _profileRepo.UpdateSharedCampaignProgress(progress);
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return;
            var progress = _profileRepo.GetCampaignProgress(summonerId);
            if (progress.PendingReward == null || progress.PendingReward.Count == 0)
            {
                GD.PushWarning("CampaignService: No pending reward to update choice for");
                return;
            }
            progress.PendingReward["choice_index"] = choiceIndex;
            _profileRepo.UpdateCampaignProgress(summonerId, progress);
        }

        GD.Print($"CampaignService: Updated pending choice to index {choiceIndex}");
    }

    /// <summary>Clear the pending reward.</summary>
    public void ClearPendingReward()
    {
        if (_profileRepo == null) return;

        if (IsSharedCampaign(_currentCampaignId))
        {
            var progress = _profileRepo.GetSharedCampaignProgress();
            progress.PendingReward = null;
            _profileRepo.UpdateSharedCampaignProgress(progress);
        }
        else
        {
            var summonerId = GetActiveSummonerId();
            if (string.IsNullOrEmpty(summonerId)) return;
            var progress = _profileRepo.GetCampaignProgress(summonerId);
            progress.PendingReward = null;
            _profileRepo.UpdateCampaignProgress(summonerId, progress);
        }

        GD.Print("CampaignService: Cleared pending reward");
    }

    // =========================================================================
    // BATTLE COMPLETION & REWARDS
    // =========================================================================

    /// <summary>Complete a battle (marks as completed and saves progress).</summary>
    public void CompleteBattle(string battleId)
    {
        if (IsBattleCompleted(battleId))
        {
            GD.PushWarning($"CampaignService: Battle '{battleId}' already completed");
            return;
        }

        _completedBattles.Add(battleId);
        SaveProgress();
        EmitSignal(SignalName.BattleCompleted, battleId);

        // Check for newly unlocked battles
        CheckUnlockedBattles();

        GD.Print($"CampaignService: Battle '{battleId}' completed");
    }

    private void CheckUnlockedBattles()
    {
        foreach (var battle in GetAllBattles())
        {
            var battleId = battle.GetValueOrDefault("id", "").AsString();
            if (IsBattleUnlocked(battleId) && !IsBattleCompleted(battleId))
            {
                EmitSignal(SignalName.BattleUnlocked, battleId);
            }
        }
    }

    /// <summary>Complete a battle without granting rewards (used when rewards are granted externally).</summary>
    public void CompleteBattleWithoutReward(string battleId)
    {
        if (string.IsNullOrEmpty(battleId))
        {
            GD.PushError("CampaignService: Invalid battle_id for CompleteBattleWithoutReward");
            return;
        }

        CompleteBattle(battleId);
        ClearPendingReward();

        GD.Print($"CampaignService: Completed battle '{battleId}' (reward granted externally)");
    }

    /// <summary>Grant battle reward and return granted card info.</summary>
    public Godot.Collections.Dictionary GrantBattleReward(string battleId, int chosenIndex = 0)
    {
        if (!_battles.TryGetValue(battleId, out var battle))
        {
            GD.PushError($"CampaignService: Battle not found: {battleId}");
            return [];
        }

        var rewardType = battle.GetValueOrDefault("reward_type", "fixed").AsString();
        var rewardCardsVariant = battle.GetValueOrDefault("reward_cards", new Godot.Collections.Array());

        // Grant campaign-scoped gold reward
        var goldReward = battle.GetValueOrDefault("gold_reward", 0).AsInt32();
        if (goldReward > 0)
        {
            _addCampaignGoldFunc?.Invoke(goldReward);
            GD.Print($"CampaignService: Granted {goldReward} campaign gold for battle '{battleId}'");
        }

        // Handle case where there are no card rewards but there is gold
        var rewardCards = rewardCardsVariant.Obj is Godot.Collections.Array arr ? arr : [];
        if (rewardCards.Count == 0)
        {
            if (goldReward > 0)
            {
                return new Godot.Collections.Dictionary
                {
                    ["gold_reward"] = goldReward,
                    ["instance_ids"] = new Godot.Collections.Array<string>()
                };
            }
            GD.PushWarning($"CampaignService: No rewards defined for battle '{battleId}'");
            return [];
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

        // Add instance IDs and gold to return value
        grantedCard["instance_ids"] = grantedInstanceIds;
        grantedCard["gold_reward"] = goldReward;

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
            GD.PushWarning("CampaignService: No collection service - skipping card grant");
            return instanceIds;
        }

        for (var i = 0; i < count; i++)
        {
            var instanceId = _grantCardFunc(catalogId, rarity);
            instanceIds.Add(instanceId);
        }

        GD.Print($"CampaignService: Granted {count}x {catalogId} ({rarity})");
        return instanceIds;
    }

    /// <summary>Claim the pending reward (grants cards and marks battle complete).</summary>
    public Godot.Collections.Dictionary ClaimPendingReward()
    {
        var pending = GetPendingReward();
        if (pending.Count == 0)
        {
            GD.PushWarning("CampaignService: No pending reward to claim");
            return [];
        }

        var battleId = pending.GetValueOrDefault("battle_id", "").AsString();
        var rewardType = pending.GetValueOrDefault("reward_type", "").AsString();
        var choiceIndex = pending.GetValueOrDefault("choice_index", 0).AsInt32();

        if (string.IsNullOrEmpty(battleId))
        {
            GD.PushError("CampaignService: Invalid pending reward - no battle_id");
            return [];
        }

        // For flexible rewards with player selection, ensure a choice was made
        if (rewardType == "flexible" && choiceIndex < 0)
        {
            var battle = GetBattle(battleId);
            var hasSpecificOptions = battle.GetValueOrDefault("specific_options", new Godot.Collections.Array()).Obj is Godot.Collections.Array arr && arr.Count > 0;
            var playerSelects = battle.GetValueOrDefault("player_selects", true).AsBool();

            if (hasSpecificOptions && playerSelects)
            {
                GD.PushError("CampaignService: Cannot claim flexible reward without making a choice");
                return [];
            }
        }

        // Grant the reward
        var grantedCard = GrantBattleReward(battleId, choiceIndex);

        // Mark battle as completed
        CompleteBattle(battleId);

        // Clear the pending reward
        ClearPendingReward();

        GD.Print($"CampaignService: Claimed reward for battle '{battleId}'");
        return grantedCard;
    }

    // =========================================================================
    // TUTORIAL HELPERS
    // =========================================================================

    /// <summary>Check if a specific battle is a tutorial battle.</summary>
    public bool IsBattleTutorial(string battleId)
    {
        if (!_battles.TryGetValue(battleId, out var battle))
            return false;

        return battle.GetValueOrDefault("is_tutorial", false).AsBool();
    }

    /// <summary>Check if all tutorial battles have been completed.</summary>
    public bool IsTutorialComplete()
    {
        foreach (var battle in GetAllBattles())
        {
            if (battle.GetValueOrDefault("is_tutorial", false).AsBool())
            {
                var battleId = battle.GetValueOrDefault("id", "").AsString();
                if (!IsBattleCompleted(battleId))
                    return false;
            }
        }

        return true;
    }

    /// <summary>Get list of all tutorial battle IDs.</summary>
    public Godot.Collections.Array<string> GetTutorialBattles()
    {
        var result = new Godot.Collections.Array<string>();

        foreach (var battle in GetAllBattles())
        {
            if (battle.GetValueOrDefault("is_tutorial", false).AsBool())
            {
                var battleId = battle.GetValueOrDefault("id", "").AsString();
                result.Add(battleId);
            }
        }

        return result;
    }

    // =========================================================================
    // CAMPAIGN ECONOMY
    // =========================================================================

    /// <summary>Get current campaign gold.</summary>
    public int GetCampaignGold()
    {
        return _getCampaignGoldFunc?.Invoke() ?? 0;
    }

    /// <summary>End a campaign (victory or defeat). Clears all campaign-scoped resources.</summary>
    public void EndCampaign(string summonerId = "", bool victory = false)
    {
        var targetId = summonerId;
        if (string.IsNullOrEmpty(targetId))
        {
            targetId = _getActiveSummonerFunc?.Invoke() ?? "";
        }

        var finalGold = GetCampaignGold();

        // Clear campaign gold
        _clearCampaignGoldFunc?.Invoke(targetId);

        if (victory)
        {
            GD.Print($"CampaignService: Campaign completed victoriously for '{targetId}' (lost {finalGold} unspent gold)");
        }
        else
        {
            GD.Print($"CampaignService: Campaign ended in defeat for '{targetId}' (lost {finalGold} gold)");
        }
    }

    // =========================================================================
    // ONBOARDING
    // =========================================================================

    /// <summary>Check if onboarding is complete.</summary>
    public bool IsOnboardingComplete()
    {
        return IsCampaignComplete("onboarding");
    }

    /// <summary>Notify that progress changed (called from GDScript).</summary>
    public void NotifyProgressChanged()
    {
        EmitSignal(SignalName.CampaignProgressChanged);
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private string GetActiveSummonerId()
    {
        return _getActiveSummonerFunc?.Invoke() ?? "";
    }
}
