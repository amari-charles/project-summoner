using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Data.Traits;
using ProjectSummoner.Domain.Profile.Summoners;
using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Services.Summoner;

/// <summary>
/// Summoner Progression Service - Handles XP and level management.
///
/// Manages summoner XP gains and level-ups.
/// UI and gameplay code should call this, never the repository directly.
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

	/// <summary>
	/// Grant XP to a summoner.
	/// Returns the new total XP.
	/// </summary>
	public int GrantSummonerXp(string summonerId, int amount)
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

		EmitSignal(SignalName.SummonerXpChanged, summonerId, newXp, currentLevel);

		// Check if summoner can now level up
		if (CanLevelUp(summonerId))
		{
			EmitSignal(SignalName.SummonerReadyToLevelUp, summonerId);
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

	/// <summary>Get XP needed for the next level from current XP.</summary>
	public int GetXpToNextLevel(string summonerId)
	{
		if (_profileRepo == null) return 0;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return 0;

		if (summoner.Level >= MaxLevel)
			return 0;

		var nextLevelXp = GetXpForLevel(summoner.Level + 1);
		return Math.Max(0, nextLevelXp - summoner.Xp);
	}

	/// <summary>Get progress towards next level as a percentage (0.0 - 1.0).</summary>
	public float GetLevelProgress(string summonerId)
	{
		if (_profileRepo == null) return 0f;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return 0f;

		if (summoner.Level >= MaxLevel)
			return 1f;

		var currentLevelXp = GetXpForLevel(summoner.Level);
		var nextLevelXp = GetXpForLevel(summoner.Level + 1);
		var levelRange = nextLevelXp - currentLevelXp;

		if (levelRange <= 0)
			return 1f;

		var progressInLevel = summoner.Xp - currentLevelXp;
		return Math.Clamp((float)progressInLevel / levelRange, 0f, 1f);
	}

	// =========================================================================
	// LEVEL-UP OPERATIONS
	// =========================================================================

	/// <summary>Check if a summoner has enough XP to level up.</summary>
	public bool CanLevelUp(string summonerId)
	{
		if (_profileRepo == null) return false;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return false;

		if (summoner.Level >= MaxLevel)
			return false;

		var nextLevelXp = GetXpForLevel(summoner.Level + 1);
		return summoner.Xp >= nextLevelXp;
	}

	/// <summary>
	/// Level up a summoner (requires only XP threshold met - no gold cost).
	/// Returns true if successful.
	/// </summary>
	public bool LevelUpSummoner(string summonerId)
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

		// Apply level up and save (XP-only, no gold cost)
		summoner.Level = newLevel;
		var saveSuccess = _profileRepo.SaveSummonerInstance(summoner);

		if (!saveSuccess)
		{
			GD.PushError("SummonerProgressionService: Failed to save summoner instance");
			return false;
		}

		EmitSignal(SignalName.SummonerLeveledUp, summonerId, newLevel);
		return true;
	}

	// =========================================================================
	// QUERY HELPERS
	// =========================================================================

	/// <summary>Get summoner progression info (for UI display).</summary>
	public Godot.Collections.Dictionary GetSummonerProgressionInfo(string summonerId)
	{
		if (_profileRepo == null) return [];

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return [];

		return new Godot.Collections.Dictionary
		{
			["summoner_id"] = summonerId,
			["level"] = summoner.Level,
			["max_level"] = MaxLevel,
			["xp"] = summoner.Xp,
			["xp_for_current_level"] = GetXpForLevel(summoner.Level),
			["xp_for_next_level"] = summoner.Level < MaxLevel ? GetXpForLevel(summoner.Level + 1) : 0,
			["xp_to_next_level"] = GetXpToNextLevel(summonerId),
			["xp_progress"] = GetLevelProgress(summonerId),
			["can_level_up"] = CanLevelUp(summonerId),
			["is_max_level"] = summoner.Level >= MaxLevel
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
	// TRAIT SELECTION (for level-up)
	// =========================================================================

	/// <summary>
	/// Get traits available for a summoner to select at level-up.
	/// Uses the unified tag-based eligibility system.
	/// </summary>
	/// <param name="summonerId">Summoner ID</param>
	/// <param name="count">Number of traits to return (0 = all eligible)</param>
	/// <returns>Array of trait dictionaries for UI display</returns>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableTraitsForSummoner(
		string summonerId, int count = 3)
	{
		var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

		if (_profileRepo == null) return result;

		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null) return result;

		var summonerDef = SummonerCatalog.GetSummoner(summonerId);
		if (summonerDef == null) return result;

		var traits = TraitCatalog.GetAvailableTraitsForLevelUp(
			summonerDef,
			summoner.Level,
			summoner.AcquiredTraitIds,
			count
		);

		foreach (var trait in traits)
		{
			result.Add(TraitCatalog.ToDictionary(trait));
		}

		return result;
	}

	/// <summary>
	/// Acquire a trait for a summoner.
	/// </summary>
	/// <param name="summonerId">Summoner ID</param>
	/// <param name="traitIdString">Trait ID to acquire (as string for GDScript interop)</param>
	/// <returns>True if successful</returns>
	public bool AcquireTrait(string summonerId, string traitIdString)
	{
		if (_profileRepo == null) return false;

		var traitId = new Data.Traits.TraitId(traitIdString);
		var summoner = _profileRepo.GetSummonerInstance(summonerId);
		if (summoner == null)
		{
			GD.PushWarning($"SummonerProgressionService: Summoner not found: {summonerId}");
			return false;
		}

		// Check if already acquired
		if (summoner.AcquiredTraitIds.Contains(traitId))
		{
			GD.PushWarning($"SummonerProgressionService: Trait already acquired: {traitIdString}");
			return false;
		}

		// Verify trait exists
		var trait = TraitCatalog.GetTrait(traitIdString);
		if (trait == null)
		{
			GD.PushWarning($"SummonerProgressionService: Trait not found: {traitIdString}");
			return false;
		}

		// Add trait and save
		summoner.AcquiredTraitIds.Add(traitId);
		var success = _profileRepo.SaveSummonerInstance(summoner);

		if (!success)
		{
			GD.PushError("SummonerProgressionService: Failed to save summoner instance");
			summoner.AcquiredTraitIds.Remove(traitId); // Rollback
			return false;
		}

		return true;
	}

	/// <summary>
	/// Get all traits a summoner has acquired.
	/// </summary>
	public Godot.Collections.Array<string> GetAcquiredTraits(string summonerId)
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
	// PRIVATE HELPERS
	// =========================================================================

	private string GetActiveSummonerId()
	{
		return _getActiveSummonerFunc?.Invoke() ?? "";
	}
}
