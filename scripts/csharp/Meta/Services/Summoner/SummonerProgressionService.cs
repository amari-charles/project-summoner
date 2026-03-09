using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;

namespace Fateforged.Meta.Summoner;

/// <summary>
/// Summoner Progression Service - Handles XP and level management.
///
/// String-accepting facade for GDScript; typed overloads for C# callers.
/// </summary>
[GlobalClass]
public partial class SummonerProgressionService : Node
{
	public static SummonerProgressionService? Instance { get; private set; }

	/// <summary>Maximum summoner level.</summary>
	public const int MaxLevel = 10;

	/// <summary>
	/// XP thresholds for each level (cumulative XP needed).
	/// Index 0 = Level 1 (no XP needed), Index 1 = Level 2, etc.
	/// Note: Summoner leveling requires only XP, not gold.
	/// </summary>
	public static readonly int[] XpThresholds =
	[
		0,      // Level 1 (start)
		100,    // Level 2
		250,    // Level 3
		500,    // Level 4
		850,    // Level 5
		1300,   // Level 6
		1900,   // Level 7
		2700,   // Level 8
		3800,   // Level 9
		5200    // Level 10
	];

	[Signal]
	public delegate void SummonerXpChangedEventHandler(string summonerId, int newXp, int newLevel);

	[Signal]
	public delegate void SummonerLeveledUpEventHandler(string summonerId, int newLevel);

	[Signal]
	public delegate void SummonerReadyToLevelUpEventHandler(string summonerId);

	private IProfileRepository? _profileRepo;

	// Func delegate for active summoner
	private Func<string>? _getActiveSummonerFunc;

	// =========================================================================
	// LIFECYCLE
	// =========================================================================

	public override void _Ready()
	{
		Instance = this;
		Initialize();
		AutoInitializeDependencies();
	}

	/// <summary>
	/// Auto-initialize dependency callbacks by looking up sibling autoloads.
	/// Only sets callbacks that haven't been injected (e.g., via InitForTesting).
	/// </summary>
	private void AutoInitializeDependencies()
	{
		if (_getActiveSummonerFunc == null)
		{
			var summonerSelection = GetNodeOrNull<SummonerSelectionService>("/root/SummonerSelection");
			if (summonerSelection != null)
			{
				_getActiveSummonerFunc = () => summonerSelection.GetActiveSummonerId();
			}
		}
	}

	private void Initialize()
	{
		GD.Print("SummonerProgressionService: Initializing...");

		_profileRepo = ProfileRepository.Instance;

		if (_profileRepo == null)
		{
			GD.PushError("SummonerProgressionService: ProfileRepository.Instance not available");
			return;
		}

		GD.Print("SummonerProgressionService: Ready");
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

	/// <summary>Set active summoner getter (called from GDScript wrapper).</summary>
	public void SetActiveSummonerGetter(Callable getter)
	{
		_getActiveSummonerFunc = () => getter.Call().AsString();
	}

	// =========================================================================
	// XP OPERATIONS
	// =========================================================================

	/// <summary>Grant XP to a summoner (string overload for GDScript boundary).</summary>
	public int GrantSummonerXp(string summonerId, int amount) =>
		GrantSummonerXp(SummonerId.FromString(summonerId), amount);

	/// <summary>Grant XP to a summoner. Returns the new total XP.</summary>
	public int GrantSummonerXp(SummonerId summonerId, int amount)
	{
		if (amount <= 0 || _profileRepo == null)
			return 0;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null)
		{
			GD.PushWarning($"SummonerProgressionService: Summoner instance not found: {summonerId}");
			return 0;
		}

		var currentLevel = summoner.Level;
		var newXp = summoner.Xp + amount;

		summoner.Xp = newXp;
		_profileRepo.SaveSummonerInstance(summoner);

		EmitSignal(SignalName.SummonerXpChanged, summonerId.Value, newXp, currentLevel);

		// Check if summoner can now level up
		if (CanLevelUp(summonerId))
		{
			EmitSignal(SignalName.SummonerReadyToLevelUp, summonerId.Value);
		}

		return newXp;
	}

	/// <summary>
	/// Grant XP to the active summoner.
	/// Returns the new total XP, or 0 if no active summoner.
	/// </summary>
	public int GrantActiveSummonerXp(int amount)
	{
		var activeSummonerId = GetActiveSummonerId();
		if (string.IsNullOrEmpty(activeSummonerId))
		{
			GD.PushWarning("SummonerProgressionService: No active summoner to grant XP to");
			return 0;
		}
		return GrantSummonerXp(activeSummonerId, amount);
	}

	/// <summary>Get XP required to reach a specific level.</summary>
	public static int GetXpForLevel(int level)
	{
		if (level < 1 || level > MaxLevel)
			return 0;
		return XpThresholds[level - 1];
	}

	/// <summary>Get XP needed for the next level (string overload for GDScript boundary).</summary>
	public int GetXpToNextLevel(string summonerId) =>
		GetXpToNextLevel(SummonerId.FromString(summonerId));

	/// <summary>Get XP needed for the next level from current XP.</summary>
	public int GetXpToNextLevel(SummonerId summonerId)
	{
		if (_profileRepo == null) return 0;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return 0;

		if (summoner.Level >= MaxLevel)
			return 0;

		var xpCost = GetXpCostForNextLevel(summoner.Level);
		return Math.Max(0, xpCost - summoner.Xp);
	}

	/// <summary>Get progress towards next level (string overload for GDScript boundary).</summary>
	public float GetLevelProgress(string summonerId) =>
		GetLevelProgress(SummonerId.FromString(summonerId));

	/// <summary>Get progress towards next level as a percentage (0.0 - 1.0).</summary>
	public float GetLevelProgress(SummonerId summonerId)
	{
		if (_profileRepo == null) return 0f;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return 0f;

		if (summoner.Level >= MaxLevel)
			return 1f;

		var xpCost = GetXpCostForNextLevel(summoner.Level);
		if (xpCost <= 0)
			return 1f;

		return Math.Clamp((float)summoner.Xp / xpCost, 0f, 1f);
	}

	// =========================================================================
	// LEVEL-UP OPERATIONS
	// =========================================================================

	/// <summary>Check if a summoner can level up (string overload for GDScript boundary).</summary>
	public bool CanLevelUp(string summonerId) =>
		CanLevelUp(SummonerId.FromString(summonerId));

	/// <summary>Check if a summoner has enough XP to level up.</summary>
	public bool CanLevelUp(SummonerId summonerId)
	{
		if (_profileRepo == null) return false;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return false;

		if (summoner.Level >= MaxLevel)
			return false;

		var xpCost = GetXpCostForNextLevel(summoner.Level);
		return xpCost > 0 && summoner.Xp >= xpCost;
	}

	/// <summary>Level up a summoner (string overload for GDScript boundary).</summary>
	public bool LevelUpSummoner(string summonerId) =>
		LevelUpSummoner(SummonerId.FromString(summonerId));

	/// <summary>
	/// Level up a summoner (requires only XP threshold met - no gold cost).
	/// Returns true if successful.
	/// </summary>
	public bool LevelUpSummoner(SummonerId summonerId)
	{
		if (_profileRepo == null) return false;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null)
		{
			GD.PushWarning($"SummonerProgressionService: Summoner not found: {summonerId}");
			return false;
		}

		// Check XP requirement
		if (!CanLevelUp(summonerId))
		{
			GD.PushWarning("SummonerProgressionService: Summoner does not have enough XP to level up");
			return false;
		}

		// Validate new level
		var newLevel = summoner.Level + 1;
		if (newLevel > MaxLevel)
		{
			GD.PushError("SummonerProgressionService: Cannot level beyond MaxLevel");
			return false;
		}

		var xpCost = GetXpCostForNextLevel(summoner.Level);
		if (xpCost <= 0 || summoner.Xp < xpCost)
		{
			GD.PushWarning("SummonerProgressionService: Invalid XP cost for level up");
			return false;
		}

		// Apply level up and consume required XP.
		summoner.Xp -= xpCost;
		summoner.Level = newLevel;
		summoner.UnspentTraitPoints += 1;
		var saveSuccess = _profileRepo.SaveSummonerInstance(summoner);

		if (!saveSuccess)
		{
			GD.PushError("SummonerProgressionService: Failed to save summoner instance");
			return false;
		}

		EmitSignal(SignalName.SummonerLeveledUp, summonerId.Value, newLevel);
		return true;
	}

	// =========================================================================
	// QUERY HELPERS
	// =========================================================================

	/// <summary>Get summoner progression info (for UI display).</summary>
	public Godot.Collections.Dictionary GetSummonerProgressionInfo(string summonerId)
	{
		return GetSummonerProgressionInfo(SummonerId.FromString(summonerId));
	}

	/// <summary>Get summoner progression info (typed).</summary>
	public Godot.Collections.Dictionary GetSummonerProgressionInfo(SummonerId summonerId)
	{
		if (_profileRepo == null) return [];

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return [];

		return new Godot.Collections.Dictionary
		{
			["summoner_id"] = summonerId.Value,
			["level"] = summoner.Level,
			["max_level"] = MaxLevel,
			["xp"] = summoner.Xp,
			["xp_for_current_level"] = 0,
			["xp_for_next_level"] = summoner.Level < MaxLevel ? GetXpCostForNextLevel(summoner.Level) : 0,
			["xp_to_next_level"] = GetXpToNextLevel(summonerId),
			["xp_progress"] = GetLevelProgress(summonerId),
			["can_level_up"] = CanLevelUp(summonerId),
			["is_max_level"] = summoner.Level >= MaxLevel,
			["unspent_trait_points"] = summoner.UnspentTraitPoints
		};
	}

	/// <summary>Get active summoner's progression info.</summary>
	public Godot.Collections.Dictionary GetActiveSummonerProgressionInfo()
	{
		var activeSummonerId = GetActiveSummonerId();
		if (string.IsNullOrEmpty(activeSummonerId)) return [];
		return GetSummonerProgressionInfo(activeSummonerId);
	}

	/// <summary>Get all summoners that can level up.</summary>
	public Godot.Collections.Array<string> GetSummonersReadyToLevelUp()
	{
		var result = new Godot.Collections.Array<string>();

		if (_profileRepo == null) return result;

		foreach (var summoner in _profileRepo.GetAllSummonerInstances())
		{
			if (CanLevelUp(summoner.SummonerId))
			{
				result.Add(summoner.SummonerId);
			}
		}

		return result;
	}

	// =========================================================================
	// UNIFIED TRAIT POINT LEDGER (Pass 2 stubs)
	// =========================================================================

	public int GetUnspentTraitPoints(string summonerId) =>
		GetUnspentTraitPoints(SummonerId.FromString(summonerId));

	public int GetUnspentTraitPoints(SummonerId summonerId)
	{
		if (_profileRepo == null) return 0;
		return _profileRepo.GetSummonerInstance(summonerId)?.UnspentTraitPoints ?? 0;
	}

	public int GrantTraitPoints(string summonerId, int amount, string source = "") =>
		GrantTraitPoints(SummonerId.FromString(summonerId), amount, source);

	public int GrantTraitPoints(SummonerId summonerId, int amount, string source = "")
	{
		if (_profileRepo == null || amount <= 0) return 0;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return 0;

		summoner.UnspentTraitPoints += amount;
		if (!_profileRepo.SaveSummonerInstance(summoner))
			return 0;

		if (!string.IsNullOrEmpty(source))
			GD.Print($"SummonerProgressionService: Granted {amount} trait points to '{summonerId}' from source='{source}'");

		return summoner.UnspentTraitPoints;
	}

	public Godot.Collections.Array<Godot.Collections.Dictionary> RollTraitOffers(string summonerId, int count = 3)
	{
		var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		if (_profileRepo == null || count <= 0)
			return result;

		var typedSummonerId = SummonerId.FromString(summonerId);
		var summoner = _profileRepo.GetSummonerInstance(typedSummonerId);
		if (summoner == null)
			return result;

		var summonerDef = SummonerCatalog.GetSummoner(typedSummonerId);
		if (summonerDef == null)
			return result;

		var evaluationLevel = summoner.Level;
		if (summoner.UnspentTraitPoints <= 0 && CanLevelUp(typedSummonerId))
			evaluationLevel = Math.Min(MaxLevel, summoner.Level + 1);

		var ownedTraitSet = new HashSet<string>(summoner.GetAllTraitIds());
		var summonerTagSet = new HashSet<string>(summonerDef.TraitEligibilityTags);
		var eligible = new List<TraitDefinition>();
		foreach (var trait in TraitCatalog.GetAllTraits())
		{
			if (trait.IsInnate)
				continue;
			if (ownedTraitSet.Contains(trait.Id))
				continue;
			if (!trait.Tags.Contains(TraitTags.Summoner))
				continue;

			var hasAnyEligibilityTag = trait.Tags.Length == 0 || trait.Tags.Any(tag => summonerTagSet.Contains(tag));
			var hasAllRequiredTags = trait.RequiredTags.All(tag => summonerTagSet.Contains(tag));
			if (!hasAnyEligibilityTag || !hasAllRequiredTags)
				continue;

			if (evaluationLevel < trait.MinLevel)
				continue;
			if (trait.MaxLevel > 0 && evaluationLevel > trait.MaxLevel)
				continue;
			if (trait.Prerequisites.Any(prereq => !ownedTraitSet.Contains(prereq)))
				continue;

			eligible.Add(trait);
		}

		var ordered = eligible
			.OrderBy(trait => ComputeStableOfferOrder($"{typedSummonerId.Value}|{evaluationLevel}", trait.Id.Value))
			.ThenBy(trait => trait.Id.Value, StringComparer.Ordinal)
			.Take(count);

		foreach (var trait in ordered)
		{
			result.Add(new Godot.Collections.Dictionary
			{
				["trait_id"] = (string)trait.Id,
				["display_name"] = ResolveLoc(trait.NameKey),
				["description"] = ResolveLoc(trait.DescriptionKey),
				["weight"] = 1
			});
		}

		return result;
	}

	public bool SpendTraitPoint(string summonerId, string traitId) =>
		SpendTraitPoint(SummonerId.FromString(summonerId), traitId);

	public bool SpendTraitPoint(SummonerId summonerId, string traitId)
	{
		if (_profileRepo == null) return false;
		if (string.IsNullOrWhiteSpace(traitId)) return false;
		var trimmedTraitId = traitId.Trim();

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return false;
		if (summoner.UnspentTraitPoints <= 0) return false;

		var typedTraitId = TraitId.FromString(trimmedTraitId);
		if (typedTraitId == TraitId.None)
			return false;

		if (summoner.AcquiredTraitIds.Contains(typedTraitId))
			return false;

		var traitDef = TraitCatalog.GetTrait(typedTraitId);
		if (traitDef == null || traitDef.IsInnate)
			return false;
		if (!traitDef.Tags.Contains(TraitTags.Summoner))
			return false;

		var summonerDef = SummonerCatalog.GetSummoner(summonerId);
		if (summonerDef == null)
			return false;

		var summonerTagSet = new HashSet<string>(summonerDef.TraitEligibilityTags);
		var hasAnyEligibilityTag = traitDef.Tags.Length == 0 || traitDef.Tags.Any(tag => summonerTagSet.Contains(tag));
		var hasAllRequiredTags = traitDef.RequiredTags.All(tag => summonerTagSet.Contains(tag));
		if (!hasAnyEligibilityTag || !hasAllRequiredTags)
			return false;

		if (summoner.Level < traitDef.MinLevel)
			return false;
		if (traitDef.MaxLevel > 0 && summoner.Level > traitDef.MaxLevel)
			return false;

		var ownedTraitSet = new HashSet<string>(summoner.GetAllTraitIds());
		if (traitDef.Prerequisites.Any(prereq => !ownedTraitSet.Contains(prereq)))
			return false;

		summoner.UnspentTraitPoints -= 1;
		summoner.AcquiredTraitIds.Add(typedTraitId);
		if (_profileRepo.SaveSummonerInstance(summoner))
			return true;

		// Rollback on save failure.
		summoner.AcquiredTraitIds.Remove(typedTraitId);
		summoner.UnspentTraitPoints += 1;
		return false;
	}

	/// <summary>Get all traits a summoner has acquired (string overload for GDScript).</summary>
	public Godot.Collections.Array<string> GetAcquiredTraits(string summonerId) =>
		GetAcquiredTraits(SummonerId.FromString(summonerId));

	/// <summary>Get all traits a summoner has acquired (typed).</summary>
	public Godot.Collections.Array<string> GetAcquiredTraits(SummonerId summonerId)
	{
		var result = new Godot.Collections.Array<string>();

		if (_profileRepo == null) return result;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return result;

		foreach (var traitId in summoner.AcquiredTraitIds)
		{
			result.Add(traitId);
		}

		return result;
	}

	// =========================================================================
	// STAT COMPUTATION (for GDScript callers)
	// =========================================================================

	/// <summary>Get computed stats for a summoner with all trait modifiers applied.</summary>
	public Godot.Collections.Dictionary GetComputedStatsForSummoner(string summonerId)
	{
		if (_profileRepo == null) return [];

		var summoner = _profileRepo.GetSummonerInstance(SummonerId.FromString(summonerId));
		if (summoner == null) return [];

		var stats = summoner.GetComputedStats();
		var result = new Godot.Collections.Dictionary();
		foreach (var kvp in stats)
			result[kvp.Key] = kvp.Value;
		return result;
	}

	/// <summary>Get all trait IDs for a summoner (innate + acquired).</summary>
	public Godot.Collections.Array<string> GetAllTraitIdsForSummoner(string summonerId)
	{
		if (_profileRepo == null) return [];

		var summoner = _profileRepo.GetSummonerInstance(SummonerId.FromString(summonerId));
		if (summoner == null) return [];

		var result = new Godot.Collections.Array<string>();
		foreach (var traitId in summoner.GetAllTraitIds())
			result.Add(traitId);
		return result;
	}

	// =========================================================================
	// PRIVATE HELPERS
	// =========================================================================

	private string GetActiveSummonerId()
	{
		return _getActiveSummonerFunc?.Invoke() ?? "";
	}

	private string ResolveLoc(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
			return "";

		var loc = GetNodeOrNull<Node>("/root/Loc");
		if (loc != null && loc.HasMethod("t"))
			return loc.Call("t", key).AsString();

		return key;
	}

	private static int ComputeStableOfferOrder(string context, string traitId)
	{
		return DeterministicStringHash($"{context}|{traitId}");
	}

	private static int GetXpCostForNextLevel(int currentLevel)
	{
		if (currentLevel >= MaxLevel)
			return 0;

		var currentLevelThreshold = GetXpForLevel(currentLevel);
		var nextLevelThreshold = GetXpForLevel(currentLevel + 1);
		return Math.Max(0, nextLevelThreshold - currentLevelThreshold);
	}

	private static int DeterministicStringHash(string value)
	{
		unchecked
		{
			var hash = (int)2166136261;
			foreach (var c in value)
			{
				hash ^= c;
				hash *= 16777619;
			}
			return hash;
		}
	}
}
