using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Domain.Profile.Account;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Economy;

/// <summary>
/// Economy Service - Resource Management
///
/// Handles all resource operations (gold, gems, essence, fragments).
/// UI and gameplay code should call this, never the repository directly.
///
/// Usage:
///   EconomyService.Instance.AddGold(50);
///   EconomyService.Instance.AddGems(100);  // From real-money purchase
///   if (EconomyService.Instance.CanAfford(ResourceType.Gold, 100))
///       EconomyService.Instance.Spend(ResourceType.Gold, 100);
/// </summary>
public partial class EconomyService : Node
{
	public static EconomyService? Instance { get; private set; }

	[Signal]
	public delegate void ResourcesChangedEventHandler(int gold, int gems, int essence, int fragments);

	[Signal]
	public delegate void TransactionCompletedEventHandler(Godot.Collections.Dictionary delta);

	[Signal]
	public delegate void TransactionFailedEventHandler(string reason);

	private IProfileRepository? _profileRepo;
	private Node? _summonerSelection;

	public override void _Ready()
	{
		Instance = this;
		Initialize();
	}

	private void Initialize()
	{
		GD.Print("EconomyService (C#): Initializing...");

		// Use the C# ProfileRepository bridge
		_profileRepo = ProfileRepository.Instance;

		if (_profileRepo == null)
		{
			GD.PushError("EconomyService: ProfileRepository.Instance not available");
			return;
		}

		// Cache SummonerSelection autoload reference
		_summonerSelection = GetTree().Root.GetNodeOrNull<Node>("SummonerSelection");

		// Connect to repo signals for reactive updates
		_profileRepo.DataChanged += OnRepoDataChanged;

		GD.Print("EconomyService (C#): Ready");

		// Emit initial state
		EmitCurrentResources();
	}

	/// <summary>
	/// Initialize for unit testing with mock dependencies.
	/// </summary>
	/// <param name="repo">The mock repository to use. Must not be null.</param>
	/// <exception cref="ArgumentNullException">Thrown if repo is null.</exception>
	public void InitForTesting(IProfileRepository repo)
	{
		ArgumentNullException.ThrowIfNull(repo);

		if (_profileRepo != null)
		{
			_profileRepo.DataChanged -= OnRepoDataChanged;
		}

		_profileRepo = repo;
		_profileRepo.DataChanged += OnRepoDataChanged;
	}

	// =========================================================================
	// RESOURCE QUERIES
	// =========================================================================

	/// <summary>
	/// Get current resource values
	/// </summary>
	public Resources GetResources()
	{
		return _profileRepo?.GetResources() ?? new Resources();
	}

	/// <summary>
	/// Get specific resource amount
	/// </summary>
	public int GetGold() => GetResources().Gold;
	public int GetGems() => GetResources().Gems;
	public int GetEssence() => GetResources().Essence;
	public int GetFragments() => GetResources().Fragments;

	/// <summary>
	/// Check if player can afford a specific amount of one resource type
	/// </summary>
	public bool CanAfford(ResourceType type, int amount)
	{
		var resources = GetResources();
		return type switch
		{
			ResourceType.Gold => resources.Gold >= amount,
			ResourceType.Gems => resources.Gems >= amount,
			ResourceType.Essence => resources.Essence >= amount,
			ResourceType.Fragments => resources.Fragments >= amount,
			_ => false
		};
	}

	/// <summary>
	/// Check if player can afford a cost dictionary
	/// </summary>
	public bool CanAfford(Dictionary<ResourceType, int> cost)
	{
		foreach (var kvp in cost)
		{
			if (!CanAfford(kvp.Key, kvp.Value))
				return false;
		}
		return true;
	}

	// =========================================================================
	// RESOURCE OPERATIONS
	// =========================================================================

	/// <summary>
	/// Add gold (positive amount only)
	/// </summary>
	public void AddGold(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning($"EconomyService: AddGold called with non-positive amount: {amount}");
			return;
		}

		UpdateResources(new Dictionary<ResourceType, int> { { ResourceType.Gold, amount } });
		GD.Print($"EconomyService: Added {amount} gold");
	}

	/// <summary>
	/// Add gems (positive amount only) - typically from real-money purchases
	/// </summary>
	public void AddGems(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning($"EconomyService: AddGems called with non-positive amount: {amount}");
			return;
		}

		UpdateResources(new Dictionary<ResourceType, int> { { ResourceType.Gems, amount } });
		GD.Print($"EconomyService: Added {amount} gems");
	}

	/// <summary>
	/// Add essence (positive amount only)
	/// </summary>
	public void AddEssence(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning($"EconomyService: AddEssence called with non-positive amount: {amount}");
			return;
		}

		UpdateResources(new Dictionary<ResourceType, int> { { ResourceType.Essence, amount } });
		GD.Print($"EconomyService: Added {amount} essence");
	}

	/// <summary>
	/// Add fragments (positive amount only)
	/// </summary>
	public void AddFragments(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning($"EconomyService: AddFragments called with non-positive amount: {amount}");
			return;
		}

		UpdateResources(new Dictionary<ResourceType, int> { { ResourceType.Fragments, amount } });
		GD.Print($"EconomyService: Added {amount} fragments");
	}

	/// <summary>
	/// Spend a single resource type
	/// Returns true if successful, false if can't afford
	/// </summary>
	public bool Spend(ResourceType type, int amount)
	{
		return Spend(new Dictionary<ResourceType, int> { { type, amount } });
	}

	/// <summary>
	/// Spend resources (negative delta)
	/// Returns true if successful, false if can't afford
	/// </summary>
	public bool Spend(Dictionary<ResourceType, int> cost)
	{
		if (!CanAfford(cost))
		{
			var reason = $"Cannot afford: {FormatCost(cost)}";
			GD.PushWarning($"EconomyService: {reason}");
			EmitSignal(SignalName.TransactionFailed, reason);
			return false;
		}

		// Convert to negative delta
		var delta = new Dictionary<ResourceType, int>();
		foreach (var kvp in cost)
		{
			delta[kvp.Key] = -kvp.Value;
		}

		UpdateResources(delta);
		GD.Print($"EconomyService: Spent {FormatCost(cost)}");
		return true;
	}

	/// <summary>
	/// Grant multiple resources at once (for rewards, etc.)
	/// </summary>
	public void GrantRewards(Dictionary<ResourceType, int> rewards)
	{
		UpdateResources(rewards);
		GD.Print($"EconomyService: Granted rewards: {FormatCost(rewards)}");
	}

	// =========================================================================
	// CAMPAIGN GOLD (stored in CampaignProgress, not account resources)
	// =========================================================================

	[Signal]
	public delegate void CampaignGoldChangedEventHandler(string summonerId, int gold);

	/// <summary>
	/// Get campaign gold for a summoner (or active summoner if empty)
	/// </summary>
	public int GetCampaignGold(string summonerId = "")
	{
		if (_profileRepo == null) return 0;

		var targetId = string.IsNullOrEmpty(summonerId)
			? GetActiveSummonerId()
			: summonerId;

		if (string.IsNullOrEmpty(targetId)) return 0;

		var progress = _profileRepo.GetCampaignProgress(targetId);
		return progress?.Gold ?? 0;
	}

	/// <summary>
	/// Add campaign gold (positive amount only)
	/// </summary>
	public void AddCampaignGold(int amount, string summonerId = "")
	{
		if (amount <= 0)
		{
			GD.PushWarning($"EconomyService: AddCampaignGold called with non-positive amount: {amount}");
			return;
		}

		if (_profileRepo == null)
		{
			GD.PushError("EconomyService.AddCampaignGold: Repository not available");
			return;
		}

		var targetId = string.IsNullOrEmpty(summonerId)
			? GetActiveSummonerId()
			: summonerId;

		if (string.IsNullOrEmpty(targetId))
		{
			GD.PushWarning("EconomyService.AddCampaignGold: No summoner specified and no active summoner");
			return;
		}

		var progress = _profileRepo.GetCampaignProgress(targetId);
		if (progress == null)
		{
			GD.PushWarning($"EconomyService.AddCampaignGold: No campaign progress for summoner '{targetId}'");
			return;
		}

		progress.Gold += amount;
		_profileRepo.UpdateCampaignProgress(targetId, progress);

		GD.Print($"EconomyService: Added {amount} campaign gold to '{targetId}' (now: {progress.Gold})");
		EmitSignal(SignalName.CampaignGoldChanged, targetId, progress.Gold);
	}

	/// <summary>
	/// Spend campaign gold. Returns true if successful.
	/// </summary>
	public bool SpendCampaignGold(int amount, string summonerId = "")
	{
		if (!CanAffordCampaignGold(amount, summonerId))
		{
			GD.PushWarning($"EconomyService: Cannot afford {amount} campaign gold");
			EmitSignal(SignalName.TransactionFailed, $"Cannot afford {amount} campaign gold");
			return false;
		}

		if (_profileRepo == null) return false;

		var targetId = string.IsNullOrEmpty(summonerId)
			? GetActiveSummonerId()
			: summonerId;

		if (string.IsNullOrEmpty(targetId)) return false;

		var progress = _profileRepo.GetCampaignProgress(targetId);
		if (progress == null) return false;

		progress.Gold -= amount;
		_profileRepo.UpdateCampaignProgress(targetId, progress);

		GD.Print($"EconomyService: Spent {amount} campaign gold from '{targetId}' (now: {progress.Gold})");
		EmitSignal(SignalName.CampaignGoldChanged, targetId, progress.Gold);
		return true;
	}

	/// <summary>
	/// Check if player can afford campaign gold cost
	/// </summary>
	public bool CanAffordCampaignGold(int amount, string summonerId = "")
	{
		return GetCampaignGold(summonerId) >= amount;
	}

	/// <summary>
	/// Clear all campaign gold (called when campaign ends)
	/// </summary>
	public void ClearCampaignGold(string summonerId = "")
	{
		if (_profileRepo == null) return;

		var targetId = string.IsNullOrEmpty(summonerId)
			? GetActiveSummonerId()
			: summonerId;

		if (string.IsNullOrEmpty(targetId)) return;

		var progress = _profileRepo.GetCampaignProgress(targetId);
		if (progress == null) return;

		var previousGold = progress.Gold;
		progress.Gold = 0;
		_profileRepo.UpdateCampaignProgress(targetId, progress);

		GD.Print($"EconomyService: Cleared {previousGold} campaign gold from '{targetId}'");
		EmitSignal(SignalName.CampaignGoldChanged, targetId, 0);
	}

	private string GetActiveSummonerId()
	{
		// Use cached SummonerSelection autoload reference
		if (_summonerSelection != null && _summonerSelection.HasMethod("GetActiveSummonerId"))
		{
			return _summonerSelection.Call("GetActiveSummonerId").AsString();
		}
		return "";
	}

	// =========================================================================
	// GDSCRIPT INTEROP
	// =========================================================================

	/// <summary>
	/// GDScript-friendly version of GetResources
	/// </summary>
	public Godot.Collections.Dictionary GetResourcesDict()
	{
		var res = GetResources();
		return new Godot.Collections.Dictionary
		{
			["gold"] = res.Gold,
			["gems"] = res.Gems,
			["essence"] = res.Essence,
			["fragments"] = res.Fragments
		};
	}

	/// <summary>
	/// GDScript-friendly version of CanAfford
	/// </summary>
	public bool CanAffordDict(Godot.Collections.Dictionary cost)
	{
		var converted = ConvertCostDict(cost);
		return CanAfford(converted);
	}

	/// <summary>
	/// GDScript-friendly version of Spend
	/// </summary>
	public bool SpendDict(Godot.Collections.Dictionary cost)
	{
		var converted = ConvertCostDict(cost);
		return Spend(converted);
	}

	/// <summary>
	/// GDScript-friendly version of GrantRewards
	/// </summary>
	public void GrantRewardsDict(Godot.Collections.Dictionary rewards)
	{
		var converted = ConvertCostDict(rewards);
		GrantRewards(converted);
	}

	// =========================================================================
	// INTERNAL
	// =========================================================================

	private void UpdateResources(Dictionary<ResourceType, int> delta)
	{
		if (_profileRepo == null)
		{
			GD.PushError("EconomyService.UpdateResources: Cannot update - repository not initialized");
			return;
		}

		_profileRepo.UpdateResources(delta);

		var gdDelta = new Godot.Collections.Dictionary();
		foreach (var kvp in delta)
		{
			gdDelta[kvp.Key.ToKey()] = kvp.Value;
		}
		EmitSignal(SignalName.TransactionCompleted, gdDelta);
		EmitCurrentResources();
	}

	private void EmitCurrentResources()
	{
		var resources = GetResources();
		EmitSignal(SignalName.ResourcesChanged,
			resources.Gold,
			resources.Gems,
			resources.Essence,
			resources.Fragments);
	}

	private void OnRepoDataChanged()
	{
		// Repo data changed (from external source or load)
		EmitCurrentResources();
	}

	private static Dictionary<ResourceType, int> ConvertCostDict(Godot.Collections.Dictionary gdDict)
	{
		var result = new Dictionary<ResourceType, int>();
		foreach (var key in gdDict.Keys)
		{
			var keyStr = key.AsString();
			var type = ResourceTypeExtensions.FromKey(keyStr);
			if (type.HasValue)
			{
				result[type.Value] = (int)gdDict[key];
			}
		}
		return result;
	}

	private static string FormatCost(Dictionary<ResourceType, int> cost)
	{
		var parts = new List<string>();
		foreach (var kvp in cost)
		{
			parts.Add($"{kvp.Key}: {kvp.Value}");
		}
		return string.Join(", ", parts);
	}
}
