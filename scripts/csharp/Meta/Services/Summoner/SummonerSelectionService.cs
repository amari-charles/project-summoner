using System;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Godot;

namespace Fateforged.Meta.Summoner;

/// <summary>
/// Summoner Selection Service - Manages the currently active summoner.
///
/// The active summoner affects:
/// - Which decks are visible (summoner-bound decks)
/// - Which campaign progress loads (per-summoner progress)
/// - Who receives XP from battles
/// </summary>
[GlobalClass]
public partial class SummonerSelectionService : Node
{
    public static SummonerSelectionService? Instance { get; private set; }

    [Signal]
    public delegate void SummonerChangedEventHandler(string oldSummonerId, string newSummonerId);

    [Signal]
    public delegate void SummonerSelectionBlockedEventHandler(string reason);

    private IProfileRepository? _profileRepo;
    private bool _isInBattle;

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
        GD.Print("SummonerSelectionService: Initializing...");

        _profileRepo = ProfileRepository.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("SummonerSelectionService: ProfileRepository.Instance not available");
            return;
        }

        GD.Print("SummonerSelectionService: Ready");
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
    // ACTIVE SUMMONER QUERIES
    // =========================================================================

    /// <summary>Get the currently active summoner ID.</summary>
    public string GetActiveSummonerId()
    {
        if (_profileRepo == null)
            return "";

        var profile = _profileRepo.GetProfileMetadata();
        if (profile == null)
            return "";

        // Check for selected_summoner in meta
        var selectedSummoner = profile.Meta.SelectedSummoner;
        if (!string.IsNullOrEmpty(selectedSummoner))
            return selectedSummoner;

        // Fallback: return first summoner instance
        var instances = _profileRepo.GetAllSummonerInstances();
        if (instances.Length > 0)
            return instances[0].SummonerId;

        return "";
    }

    /// <summary>Get the active summoner's instance data.</summary>
    public SummonerInstance? GetActiveSummonerInstance()
    {
        var summonerId = GetActiveSummonerId();
        if (string.IsNullOrEmpty(summonerId))
            return null;

        return _profileRepo?.GetSummonerInstance(new SummonerId(summonerId));
    }

    /// <summary>Get list of all unlocked summoner IDs.</summary>
    public string[] GetUnlockedSummonerIds()
    {
        if (_profileRepo == null)
            return [];

        var instances = _profileRepo.GetAllSummonerInstances();
        var ids = new string[instances.Length];
        for (int i = 0; i < instances.Length; i++)
        {
            ids[i] = instances[i].SummonerId;
        }
        return ids;
    }

    // =========================================================================
    // SUMMONER SWITCHING
    // =========================================================================

    /// <summary>Check if summoner switching is currently allowed.</summary>
    public bool CanSwitchSummoner()
    {
        return !_isInBattle;
    }

    /// <summary>
    /// Set the active summoner.
    /// Returns true if successful, false if blocked or invalid.
    /// </summary>
    public bool SetActiveSummoner(string summonerId, string? blockedReason = null)
    {
        if (_profileRepo == null)
            return false;

        // Validate summoner ID
        if (string.IsNullOrEmpty(summonerId))
        {
            GD.PushWarning("SummonerSelectionService: Cannot set empty summoner ID");
            return false;
        }

        // Check if summoner is unlocked
        if (!_profileRepo.IsSummonerUnlocked(new SummonerId(summonerId)))
        {
            GD.PushWarning($"SummonerSelectionService: Summoner not unlocked: {summonerId}");
            return false;
        }

        // Check if switching is allowed
        if (!CanSwitchSummoner())
        {
            EmitSignal(
                SignalName.SummonerSelectionBlocked,
                blockedReason ?? "Cannot switch summoner during battle"
            );
            return false;
        }

        // Get old summoner ID for signal
        var oldSummonerId = GetActiveSummonerId();

        // No change needed
        if (oldSummonerId == summonerId)
            return true;

        // Update selected_summoner in profile meta
        _profileRepo.UpdateProfileMeta(new MetaUpdate { SelectedSummoner = summonerId });

        // Emit signal for UI updates
        EmitSignal(SignalName.SummonerChanged, oldSummonerId, summonerId);

        GD.Print(
            $"SummonerSelectionService: Changed summoner from '{oldSummonerId}' to '{summonerId}'"
        );

        return true;
    }

    // =========================================================================
    // BATTLE STATE (called from GDScript wrapper)
    // =========================================================================

    /// <summary>Set whether we're currently in a battle.</summary>
    public void SetInBattle(bool inBattle)
    {
        _isInBattle = inBattle;
    }

    /// <summary>Get whether we're currently in a battle.</summary>
    public bool IsInBattle()
    {
        return _isInBattle;
    }

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>Check if a specific summoner is unlocked.</summary>
    public bool IsSummonerUnlocked(string summonerId)
    {
        if (_profileRepo == null)
            return false;
        return _profileRepo.IsSummonerUnlocked(new SummonerId(summonerId));
    }

    /// <summary>
    /// Set the starting summoner during initial profile setup.
    /// Unlike SetActiveSummoner, this bypasses unlock checks and records the random flag.
    /// </summary>
    public bool SetStartingSummoner(string summonerId, bool chosenRandom)
    {
        if (_profileRepo == null)
            return false;
        return _profileRepo.SetStartingSummoner(new SummonerId(summonerId), chosenRandom);
    }

    /// <summary>Get a specific summoner's instance data as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetSummonerInstanceDict(string summonerId)
    {
        if (_profileRepo == null)
            return [];
        var instance = _profileRepo.GetSummonerInstance(new SummonerId(summonerId));
        if (instance == null)
            return [];
        return DtoConverters.ToDict(instance);
    }

    /// <summary>Save a summoner instance from GDScript dictionary data.</summary>
    public bool SaveSummonerInstanceDict(Godot.Collections.Dictionary data)
    {
        if (_profileRepo == null)
            return false;
        var instance = DtoConverters.FromSummonerDict(data);
        if (instance == null)
            return false;
        return _profileRepo.SaveSummonerInstance(instance);
    }

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>Get active summoner instance as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetActiveSummonerInstanceDict()
    {
        var instance = GetActiveSummonerInstance();
        if (instance == null)
            return [];

        return DtoConverters.ToDict(instance);
    }

    /// <summary>Get unlocked summoner IDs as array for GDScript.</summary>
    public Godot.Collections.Array<string> GetUnlockedSummonerIdsArray()
    {
        var result = new Godot.Collections.Array<string>();
        foreach (var id in GetUnlockedSummonerIds())
        {
            result.Add(id);
        }
        return result;
    }
}
