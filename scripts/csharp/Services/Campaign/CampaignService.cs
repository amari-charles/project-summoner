using System;
using Godot;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Services.Campaign.Handlers;

namespace ProjectSummoner.Services.Campaign;

/// <summary>
/// Campaign Service - Manages campaign progression and battle rewards.
///
/// Tracks which battles have been completed and handles reward distribution.
/// Battle definitions are loaded from GDScript data files.
///
/// Uses Facade + Handlers pattern for clean separation of concerns.
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

	// Handlers
	private CampaignDataStore? _store;
	private CampaignCatalogHandler? _catalog;
	private CampaignProgressHandler? _progress;
	private CampaignRewardHandler? _rewards;
	private TutorialHandler? _tutorial;

	// Graph handlers (for node-based campaigns)
	private CampaignGraphStore? _graphStore;
	private NodeUnlockHandler? _nodeUnlockHandler;
	private ChoiceTracker? _choiceTracker;

	// Callbacks for GDScript dependencies
	private Func<int>? _getCampaignGoldFunc;
	private Func<string, string, string>? _grantCardFunc;
	private Func<string>? _getActiveSummonerFunc;
	private Action<string>? _clearCampaignGoldFunc;

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
		GD.Print("CampaignService: Initializing...");

		_profileRepo = ProfileRepository.Instance;

		if (_profileRepo == null)
		{
			GD.PushError("CampaignService: ProfileRepository.Instance not available");
			return;
		}

		InitializeHandlers();

		GD.Print("CampaignService: Ready");
	}

	private void InitializeHandlers()
	{
		if (_profileRepo == null) return;

		// Create shared data store
		_store = new CampaignDataStore();

		// Create graph handlers first (progress handler depends on them)
		_graphStore = new CampaignGraphStore();
		_choiceTracker = new ChoiceTracker();
		_nodeUnlockHandler = new NodeUnlockHandler(_graphStore, _choiceTracker);

		// Create handlers (order matters - some depend on others)
		_progress = new CampaignProgressHandler(_profileRepo, _store, GetActiveSummonerId, _choiceTracker, _graphStore);
		_catalog = new CampaignCatalogHandler(_store, _progress);
		_rewards = new CampaignRewardHandler(_profileRepo, _store, GetActiveSummonerId, _grantCardFunc);
		_tutorial = new TutorialHandler(_store, _catalog, _progress);
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
		InitializeHandlers();
	}

	// =========================================================================
	// CALLBACK INJECTION (from GDScript wrapper)
	// =========================================================================

	/// <summary>Set Economy service callbacks.</summary>
	/// <remarks>
	/// Note: addCampaignGold parameter is kept for API compatibility but not used.
	/// Gold is granted directly via EconomyService.Instance in CampaignRewardHandler.
	/// </remarks>
	public void SetEconomyCallbacks(Callable getCampaignGold, Callable addCampaignGold, Callable clearCampaignGold)
	{
		_getCampaignGoldFunc = () => getCampaignGold.Call().AsInt32();
		// addCampaignGold not used - gold granted directly via EconomyService.Instance
		_clearCampaignGoldFunc = (summonerId) => clearCampaignGold.Call(summonerId);
	}

	/// <summary>Set Collection service callbacks.</summary>
	public void SetCollectionCallbacks(Callable grantCard)
	{
		_grantCardFunc = (catalogId, rarity) => grantCard.Call(catalogId, rarity).AsString();

		// Rebuild rewards handler with new callbacks
		if (_profileRepo != null && _store != null)
		{
			_rewards = new CampaignRewardHandler(_profileRepo, _store, GetActiveSummonerId, _grantCardFunc);
		}
	}

	/// <summary>Set active summoner getter.</summary>
	public void SetActiveSummonerGetter(Callable getter)
	{
		_getActiveSummonerFunc = () => getter.Call().AsString();
	}

	// =========================================================================
	// CAMPAIGN DATA LOADING (delegates to CampaignCatalogHandler)
	// =========================================================================

	/// <summary>Load campaign data from GDScript.</summary>
	public void LoadCampaignsFromGDScript(Godot.Collections.Array<Godot.Collections.Dictionary> campaigns)
	{
		_catalog?.LoadCampaignsFromGDScript(campaigns);

		// Also load graphs for node-based unlock logic
		_graphStore?.LoadGraphsFromGDScript(campaigns);
	}

	/// <summary>Set the current campaign ID.</summary>
	public void SetCurrentCampaign(string campaignId)
	{
		var oldId = _store?.CurrentCampaignId ?? "";
		_progress?.SetCurrentCampaign(campaignId);
		_graphStore?.SetCurrentCampaign(campaignId);

		// Sync completed nodes to graph store for unlock evaluation
		if (_graphStore != null && _store != null)
		{
			_graphStore.LoadCompletedNodes(_store.CompletedBattles);
		}

		if (oldId != campaignId)
		{
			EmitSignal(SignalName.CampaignChanged, oldId, campaignId);
		}
	}

	/// <summary>Get the current campaign ID.</summary>
	public string GetCurrentCampaignId() => _progress?.GetCurrentCampaignId() ?? "";

	// =========================================================================
	// PROGRESS MANAGEMENT (delegates to CampaignProgressHandler)
	// =========================================================================

	/// <summary>Load progress from profile repository.</summary>
	public void LoadProgress()
	{
		_progress?.LoadProgress();

		// Sync completed nodes to graph store for unlock evaluation
		if (_graphStore != null && _store != null)
		{
			_graphStore.LoadCompletedNodes(_store.CompletedBattles);
		}
	}

	/// <summary>Save progress to profile repository.</summary>
	public void SaveProgress()
	{
		_progress?.SaveProgress();
	}


	// =========================================================================
	// CAMPAIGN QUERIES (delegates to CampaignCatalogHandler)
	// =========================================================================

	/// <summary>Get all campaigns with unlock status.</summary>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCampaigns()
	{
		return _catalog?.GetAllCampaigns() ?? [];
	}

	/// <summary>Get a specific campaign's metadata.</summary>
	public Godot.Collections.Dictionary GetCampaign(string campaignId)
	{
		return _catalog?.GetCampaign(campaignId) ?? [];
	}

	/// <summary>Check if a campaign is unlocked.</summary>
	public bool IsCampaignUnlocked(string campaignId)
	{
		return _catalog?.IsCampaignUnlocked(campaignId) ?? false;
	}

	/// <summary>Check if a campaign is complete.</summary>
	public bool IsCampaignComplete(string campaignId)
	{
		return _progress?.IsCampaignComplete(campaignId) ?? false;
	}

	// =========================================================================
	// BATTLE QUERIES (delegates to CampaignCatalogHandler)
	// =========================================================================

	/// <summary>Get all battles for the current campaign.</summary>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllBattles()
	{
		return _catalog?.GetAllBattles() ?? [];
	}

	/// <summary>Get a specific battle by ID.</summary>
	public Godot.Collections.Dictionary GetBattle(string battleId)
	{
		return _catalog?.GetBattle(battleId) ?? [];
	}

	/// <summary>Check if a battle is completed.</summary>
	public bool IsBattleCompleted(string battleId)
	{
		return _progress?.IsBattleCompleted(battleId) ?? false;
	}

	/// <summary>Check if a node is unlocked using graph-based unlock logic.</summary>
	public bool IsBattleUnlocked(string battleId)
	{
		return _nodeUnlockHandler?.IsNodeUnlocked(battleId) ?? false;
	}

	/// <summary>Get all available (unlocked but not completed) nodes.</summary>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableBattles()
	{
		var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

		foreach (Godot.Collections.Dictionary battle in GetAllBattles())
		{
			if (battle.TryGetValue("id", out var idValue))
			{
				var battleId = idValue.AsString();
				if (IsBattleUnlocked(battleId) && !IsBattleCompleted(battleId))
				{
					result.Add(battle);
				}
			}
		}

		return result;
	}

	/// <summary>Get all completed battles.</summary>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetCompletedBattles()
	{
		return _catalog?.GetCompletedBattles() ?? [];
	}

	// =========================================================================
	// PENDING REWARD MANAGEMENT (delegates to CampaignRewardHandler)
	// =========================================================================

	/// <summary>Set a pending reward for a battle.</summary>
	public void SetPendingReward(string battleId, string rewardType, int choiceIndex = -1)
	{
		_rewards?.SetPendingReward(battleId, rewardType, choiceIndex);
	}

	/// <summary>Get the current pending reward.</summary>
	public Godot.Collections.Dictionary GetPendingReward()
	{
		return _rewards?.GetPendingReward() ?? [];
	}

	/// <summary>Update choice index for a pending choice reward.</summary>
	public void UpdatePendingChoice(int choiceIndex)
	{
		_rewards?.UpdatePendingChoice(choiceIndex);
	}

	/// <summary>Clear the pending reward.</summary>
	public void ClearPendingReward()
	{
		_rewards?.ClearPendingReward();
	}

	// =========================================================================
	// BATTLE COMPLETION & REWARDS
	// =========================================================================

	/// <summary>Complete a battle (marks as completed and saves progress).</summary>
	public void CompleteBattle(string battleId)
	{
		_progress?.CompleteBattle(battleId);

		// Also mark as completed in graph store for unlock evaluation
		_graphStore?.CompleteNode(battleId);

		EmitSignal(SignalName.BattleCompleted, battleId);

		// Check for newly unlocked battles
		CheckUnlockedBattles();
	}

	private void CheckUnlockedBattles()
	{
		foreach (Godot.Collections.Dictionary battle in GetAllBattles())
		{
			if (battle.TryGetValue("id", out var idValue))
			{
				var battleId = idValue.AsString();
				if (IsBattleUnlocked(battleId) && !IsBattleCompleted(battleId))
				{
					EmitSignal(SignalName.BattleUnlocked, battleId);
				}
			}
		}
	}

	/// <summary>Complete a battle without granting rewards.</summary>
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
		return _rewards?.GrantBattleReward(battleId, chosenIndex) ?? [];
	}

	/// <summary>Claim the pending reward.</summary>
	public Godot.Collections.Dictionary ClaimPendingReward()
	{
		var (grantedCard, battleId) = _rewards?.ClaimPendingReward() ?? ([], "");

		if (!string.IsNullOrEmpty(battleId))
		{
			CompleteBattle(battleId);
			ClearPendingReward();
		}

		return grantedCard;
	}

	// =========================================================================
	// TUTORIAL HELPERS (delegates to TutorialHandler)
	// =========================================================================

	/// <summary>Check if a specific battle is a tutorial battle.</summary>
	public bool IsBattleTutorial(string battleId)
	{
		return _tutorial?.IsBattleTutorial(battleId) ?? false;
	}

	/// <summary>Check if all tutorial battles have been completed.</summary>
	public bool IsTutorialComplete()
	{
		return _tutorial?.IsTutorialComplete() ?? false;
	}

	/// <summary>Get list of all tutorial battle IDs.</summary>
	public Godot.Collections.Array<string> GetTutorialBattles()
	{
		return _tutorial?.GetTutorialBattles() ?? [];
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

	/// <summary>Notify that progress changed.</summary>
	public void NotifyProgressChanged()
	{
		EmitSignal(SignalName.CampaignProgressChanged);
	}

	// =========================================================================
	// CHOICE RECORDING (for branching paths)
	// =========================================================================

	/// <summary>Record a choice made at a choice node.</summary>
	public void RecordChoice(string nodeId, string choiceId)
	{
		_choiceTracker?.RecordChoice(nodeId, choiceId);

		// Save progress to persist the choice
		SaveProgress();

		GD.Print($"CampaignService: Recorded choice '{choiceId}' at node '{nodeId}'");
	}

	/// <summary>Get the choice made at a specific node.</summary>
	public string GetChoice(string nodeId)
	{
		return _choiceTracker?.GetChoice(nodeId) ?? "";
	}

	/// <summary>Check if a choice has been made at a specific node.</summary>
	public bool HasChoice(string nodeId)
	{
		return _choiceTracker?.HasChoice(nodeId) ?? false;
	}

	/// <summary>Get all choices as a dictionary (for serialization).</summary>
	public Godot.Collections.Dictionary GetAllChoices()
	{
		return _choiceTracker?.ToGodotDictionary() ?? [];
	}

	// =========================================================================
	// PROGRESS RESET
	// =========================================================================

	/// <summary>Reset all campaign progress for the current summoner.</summary>
	public void ResetProgress()
	{
		_progress?.ResetProgress();
		_graphStore?.ClearProgress();

		EmitSignal(SignalName.CampaignProgressChanged);

		GD.Print("CampaignService: Progress reset for current summoner");
	}

	// =========================================================================
	// PRIVATE HELPERS
	// =========================================================================

	private string GetActiveSummonerId()
	{
		return _getActiveSummonerFunc?.Invoke() ?? "";
	}
}
