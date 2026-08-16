using System;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Account;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign.Handlers;
using Fateforged.Meta.Economy;
using Fateforged.Meta.Rewards;
using Fateforged.Meta.Summoner;
using Godot;

namespace Fateforged.Meta.Campaign;

/// <summary>
/// Campaign Service - Manages campaign catalogs and non-authoritative navigation state.
///
/// Durable battle completion and rewards are owned by IProgressionAuthority.
/// String-accepting facade for GDScript; delegates to typed handlers internally.
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
    private TutorialHandler? _tutorial;
    private AcademyProgressHandler? _academy;

    // Graph handlers (for node-based campaigns)
    private CampaignGraphStore? _graphStore;
    private NodeUnlockHandler? _nodeUnlockHandler;
    private ChoiceTracker? _choiceTracker;

    // Callbacks for GDScript dependencies
    private Func<SummonerId>? _getActiveSummonerFunc;

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
            var summonerSelection = GetNodeOrNull<SummonerSelectionService>(
                "/root/SummonerSelection"
            );
            if (summonerSelection != null)
            {
                _getActiveSummonerFunc = () =>
                    SummonerId.FromString(summonerSelection.GetActiveSummonerId());
            }
        }

        // Initialize catalogs if not already done
        InitializeCatalogs();
    }

    private void Initialize()
    {
        GD.Print("CampaignService: Initializing...");

        SetProfileRepository(ProfileRepository.Instance);

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
        if (_profileRepo == null)
            return;

        // Create shared data store
        _store = new CampaignDataStore();

        // Create graph handlers first (progress handler depends on them)
        _graphStore = new CampaignGraphStore();
        _choiceTracker = new ChoiceTracker();
        _nodeUnlockHandler = new NodeUnlockHandler(_graphStore, _choiceTracker);

        // Create handlers (order matters - some depend on others)
        _progress = new CampaignProgressHandler(
            _profileRepo,
            _store,
            GetActiveSummonerId,
            _choiceTracker,
            _graphStore
        );
        _catalog = new CampaignCatalogHandler(_store, _progress);
        _tutorial = new TutorialHandler(_store, _catalog, _progress);
        _academy = new AcademyProgressHandler(
            _profileRepo,
            GetActiveSummonerId,
            GetAcademyRewardRuntime()
        );
    }

    public override void _ExitTree()
    {
        if (_profileRepo != null)
            _profileRepo.DataChanged -= OnProfileDataChanged;
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Initialize for testing with mock dependencies.</summary>
    public void InitForTesting(IProfileRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        SetProfileRepository(repo);
        InitializeHandlers();
    }

    private void SetProfileRepository(IProfileRepository? repo)
    {
        if (_profileRepo != null)
            _profileRepo.DataChanged -= OnProfileDataChanged;
        _profileRepo = repo;
        if (_profileRepo != null)
            _profileRepo.DataChanged += OnProfileDataChanged;
    }

    private void OnProfileDataChanged()
    {
        // The profile is authoritative. Keep navigation/unlock caches synchronized
        // when progression is committed by IProgressionAuthority or another owner.
        LoadProgress();
    }

    // =========================================================================
    // CALLBACK INJECTION (from GDScript wrapper)
    // =========================================================================

    private UniversalRewardRuntime? GetAcademyRewardRuntime()
    {
        var runtime = RewardService.Instance?.UniversalRuntime;
        return runtime != null && ReferenceEquals(runtime.ProfileStore, _profileRepo)
            ? runtime
            : null;
    }

    /// <summary>Set active summoner getter.</summary>
    public void SetActiveSummonerGetter(Callable getter)
    {
        _getActiveSummonerFunc = () => SummonerId.FromString(getter.Call().AsString());
    }

    // =========================================================================
    // CAMPAIGN DATA LOADING (delegates to CampaignCatalogHandler)
    // =========================================================================

    /// <summary>
    /// Initialize campaign data from C# EventCatalog and CampaignCatalog.
    /// </summary>
    public void InitializeCatalogs()
    {
        _catalog?.Initialize();

        // Also load graphs for node-based unlock logic
        _graphStore?.InitializeFromCatalog();

        // Restore the last selected campaign from profile metadata when available.
        var persistedCampaignId = _profileRepo?.GetProfileMetadata()?.Meta.SelectedCampaign ?? "";
        if (
            !string.IsNullOrEmpty(persistedCampaignId)
            && Data.Events.CampaignCatalog.HasCampaign(CampaignId.FromString(persistedCampaignId))
        )
        {
            SetCurrentCampaign(persistedCampaignId);
            return;
        }

        // Fallback for first launch / old saves / invalid saved campaign.
        if (!GetCurrentCampaignIdTyped().HasValue)
            SetCurrentCampaign(Data.Events.CampaignIds.Default.Value);
    }

    /// <summary>Check if a campaign exists.</summary>
    public bool HasCampaign(string campaignId)
    {
        return Data.Events.CampaignCatalog.HasCampaign(CampaignId.FromString(campaignId));
    }

    /// <summary>Set the current campaign ID. Returns true if the campaign exists and was set.</summary>
    public bool SetCurrentCampaign(string campaignId)
    {
        var typedId = CampaignId.FromString(campaignId);
        if (!Data.Events.CampaignCatalog.HasCampaign(typedId))
            return false;

        var oldId = _store?.CurrentCampaignId ?? CampaignId.None;
        _progress?.SetCurrentCampaign(typedId);
        _graphStore?.SetCurrentCampaign(campaignId);

        // Sync completed nodes to graph store for unlock evaluation
        if (_graphStore != null && _store != null)
        {
            _graphStore.LoadCompletedNodes(_store.CompletedBattles.ConvertAll(b => b.Value));
        }

        if (oldId != typedId)
        {
            _profileRepo?.UpdateProfileMeta(new MetaUpdate { SelectedCampaign = campaignId });
            _profileRepo?.SaveProfile(immediate: true);
            EmitSignal(SignalName.CampaignChanged, oldId.Value, campaignId);
        }

        return true;
    }

    /// <summary>Get the current campaign ID (string for GDScript).</summary>
    public string GetCurrentCampaignId() => _progress?.GetCurrentCampaignId().Value ?? "";

    /// <summary>Get the current campaign ID (typed for C# callers).</summary>
    public CampaignId GetCurrentCampaignIdTyped() =>
        _progress?.GetCurrentCampaignId() ?? CampaignId.None;

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
            _graphStore.LoadCompletedNodes(_store.CompletedBattles.ConvertAll(b => b.Value));
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
        return _catalog?.GetCampaign(CampaignId.FromString(campaignId)) ?? [];
    }

    /// <summary>Check if a campaign is unlocked.</summary>
    public bool IsCampaignUnlocked(string campaignId)
    {
        return _catalog?.IsCampaignUnlocked(CampaignId.FromString(campaignId)) ?? false;
    }

    /// <summary>Check if a campaign is complete.</summary>
    public bool IsCampaignComplete(string campaignId)
    {
        return _progress?.IsCampaignComplete(CampaignId.FromString(campaignId)) ?? false;
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
        return _catalog?.GetBattle(EventId.FromString(battleId)) ?? [];
    }

    /// <summary>Check if a battle is completed.</summary>
    public bool IsBattleCompleted(string battleId)
    {
        return _progress?.IsBattleCompleted(BattleId.FromString(battleId)) ?? false;
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
    // BATTLE COMPLETION & REWARDS
    // =========================================================================

    /// <summary>Complete a battle (marks as completed and saves progress).</summary>
    public void CompleteBattle(string battleId)
    {
        _progress?.CompleteBattle(BattleId.FromString(battleId));

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

    // =========================================================================
    // TUTORIAL HELPERS (delegates to TutorialHandler)
    // =========================================================================

    /// <summary>Check if a specific battle is a tutorial battle.</summary>
    public bool IsBattleTutorial(string battleId)
    {
        return _tutorial?.IsBattleTutorial(EventId.FromString(battleId)) ?? false;
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
        return EconomyService.Instance?.GetCampaignGold() ?? 0;
    }

    /// <summary>End a campaign (victory or defeat). Clears all campaign-scoped resources.</summary>
    public void EndCampaign(string summonerId = "", bool victory = false)
    {
        var targetId = summonerId;
        if (string.IsNullOrEmpty(targetId))
        {
            targetId = GetActiveSummonerId().Value;
        }

        var finalGold = GetCampaignGold();

        // Clear campaign gold via EconomyService
        EconomyService.Instance?.ClearCampaignGold(targetId);

        if (victory)
        {
            GD.Print(
                $"CampaignService: Campaign completed victoriously for '{targetId}' (lost {finalGold} unspent gold)"
            );
        }
        else
        {
            GD.Print(
                $"CampaignService: Campaign ended in defeat for '{targetId}' (lost {finalGold} gold)"
            );
        }
    }

    /// <summary>Notify that progress changed.</summary>
    public void NotifyProgressChanged()
    {
        EmitSignal(SignalName.CampaignProgressChanged);
    }

    // =========================================================================
    // ACADEMY CAMPAIGN
    // =========================================================================

    public Godot.Collections.Dictionary GetAcademyProgress()
    {
        return _academy?.GetProgress() ?? [];
    }

    public Godot.Collections.Dictionary GetQuestJournalState()
    {
        return _academy?.GetQuestJournalState() ?? [];
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableAcademyCourses()
    {
        return _academy?.GetAvailableCourses() ?? [];
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAcademyCoursesForSemester(
        int year,
        int semester
    )
    {
        return _academy?.GetCoursesForSemester(year, semester) ?? [];
    }

    public Godot.Collections.Dictionary GetAcademyCourse(string courseId)
    {
        return _academy?.GetCourse(courseId) ?? [];
    }

    public Godot.Collections.Dictionary GetAcademyCourseFlowState(string courseId)
    {
        return _academy?.GetCourse(courseId) ?? [];
    }

    public Godot.Collections.Dictionary GetAcademyActivityPreparationState(
        string courseId,
        string activityId
    )
    {
        return _academy?.GetActivityLaunchState(courseId, activityId) ?? [];
    }

    public bool UpdateAcademyActivityLoadout(
        string courseId,
        string activityId,
        Godot.Collections.Array<Godot.Collections.Dictionary> slots
    ) => _academy?.UpdateActivityLoadout(courseId, activityId, slots) ?? false;

    public Godot.Collections.Dictionary FillAcademyActivityLoadoutFromDeck(
        string courseId,
        string activityId,
        string sourceDeckId
    ) => _academy?.FillActivityLoadoutFromDeck(courseId, activityId, sourceDeckId) ?? [];

    public Godot.Collections.Dictionary SaveAcademyActivityLoadoutToDeck(
        string courseId,
        string activityId,
        string targetDeckId,
        string newDeckName
    ) => _academy?.SaveActivityLoadoutToDeck(courseId, activityId, targetDeckId, newDeckName) ?? [];

    public Godot.Collections.Dictionary GetAcademyActivityLaunchState(
        string courseId,
        string activityId
    )
    {
        return _academy?.GetActivityLaunchState(courseId, activityId) ?? [];
    }

    public Godot.Collections.Dictionary ResolveAcademyActivityBattleConfig(
        string courseId,
        string activityId
    )
    {
        return _academy?.ResolveActivityBattleConfig(courseId, activityId) ?? [];
    }

    public Godot.Collections.Dictionary GetLastAcademyCompletionSummary()
    {
        return _academy?.GetLastCompletionSummary() ?? [];
    }

    public Godot.Collections.Dictionary ConsumeLastAcademyCompletionSummary()
    {
        return _academy?.ConsumeLastCompletionSummary() ?? [];
    }

    public bool EnrollAcademyCourse(string courseId)
    {
        var enrolled = _academy?.EnrollCourse(courseId) ?? false;
        if (enrolled)
            EmitSignal(SignalName.CampaignProgressChanged);
        return enrolled;
    }

    public bool CompleteAcademyCourse(string courseId, string grade = "pass", bool honors = false)
    {
        var completed = _academy?.CompleteCourse(courseId, grade, honors) ?? false;
        if (completed)
            EmitSignal(SignalName.CampaignProgressChanged);
        return completed;
    }

    public bool CompleteAcademyActivity(
        string courseId,
        string activityId,
        int outcome = (int)AcademyActivityOutcome.Victory
    )
    {
        if (!Enum.IsDefined(typeof(AcademyActivityOutcome), outcome))
            return false;
        var completed =
            _academy?.CompleteActivity(courseId, activityId, (AcademyActivityOutcome)outcome)
            ?? false;
        if (completed)
            EmitSignal(SignalName.CampaignProgressChanged);
        return completed;
    }

    public Godot.Collections.Dictionary ClaimAcademyReward(
        string claimId,
        Godot.Collections.Array<string> selectedOptionIds
    )
    {
        var result = _academy?.ClaimReward(claimId, selectedOptionIds) ?? [];
        if (result.TryGetValue("success", out var success) && success.AsBool())
            EmitSignal(SignalName.CampaignProgressChanged);
        return result;
    }

    public bool AdvanceAcademySemester()
    {
        var advanced = _academy?.AdvanceSemester() ?? false;
        if (advanced)
            EmitSignal(SignalName.CampaignProgressChanged);
        return advanced;
    }

    // =========================================================================
    // CHOICE RECORDING (for branching paths)
    // =========================================================================

    /// <summary>Record a choice made at a choice node.</summary>
    public void RecordChoice(string nodeId, string choiceId)
    {
        _choiceTracker?.RecordChoiceFromString(nodeId, choiceId);

        // Save progress to persist the choice
        SaveProgress();

        GD.Print($"CampaignService: Recorded choice '{choiceId}' at node '{nodeId}'");
    }

    /// <summary>Get the choice made at a specific node.</summary>
    public string GetChoice(string nodeId)
    {
        return _choiceTracker?.GetChoiceAsString(nodeId) ?? "";
    }

    /// <summary>Check if a choice has been made at a specific node.</summary>
    public bool HasChoice(string nodeId)
    {
        return _choiceTracker?.HasChoiceFromString(nodeId) ?? false;
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

    private SummonerId GetActiveSummonerId()
    {
        return _getActiveSummonerFunc?.Invoke() ?? SummonerId.None;
    }
}
