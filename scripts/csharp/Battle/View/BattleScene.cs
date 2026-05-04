using System;
using System.Collections.Generic;
using Fateforged.Constants;
using Fateforged.Multiplayer.Ranking;
using Fateforged.Multiplayer.Transport;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Events;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Thin C# facade that replaces GameController3D.
/// Responsibilities: init sequence, game flow (pause/resume), SimEvent→signal forwarding.
/// No game logic — the simulation owns timers, phases, and win conditions.
/// </summary>
[GlobalClass]
public partial class BattleScene : Node3D
{
    // =========================================================================
    // ENUMS (GDScript consumers reference these via BattleScene.GameState.*)
    // =========================================================================

    public enum GameState
    {
        Setup,
        Playing,
        Paused,
        GameOver,
    }

    public enum BattlePhase
    {
        Preparation,
        Battle,
    }

    // =========================================================================
    // EXPORTS (wired in .tscn)
    // =========================================================================

    [Export]
    public float MatchDuration { get; set; } = 180.0f;

    [Export]
    public float OvertimeDuration { get; set; } = 60.0f;

    [Export]
    public float PreparationDuration { get; set; } = 15.0f;

    [Export]
    public Node3D? Battlefield { get; set; }

    [Export]
    public Node? PlayerSummoner { get; set; }

    [Export]
    public Node? EnemySummoner { get; set; }

    // =========================================================================
    // STATE
    // =========================================================================

    public GameState CurrentState { get; set; } = GameState.Setup;

    // Child components
    public EntityManager? EntityManager { get; private set; }

    /// Typed battle config — built once from BattleContext, used throughout.
    private BattleSessionConfig _config = null!;

    /// Deck card instance IDs for XP rewards (stored locally, no longer in BattleContext).
    private List<string> _deckCardInstanceIds = new();
    private int? _pendingCompletionWinnerTeam;
    private bool _completionHandled;
    private MatchEndReason _matchEndReason = MatchEndReason.SummonerDestroyed;

    /// Max frames to wait for a single scene to load (~5 seconds at 60fps)
    private const int SceneLoadTimeoutFrames = 300;

    // Client defaults — host snapshots overwrite within ~100ms
    private const float DefaultMana = 100f;
    private const float DefaultMaxMana = 100f;
    private const float DefaultCastSpeed = 1.0f;

    // Emergency fallback deck (test mode only)
    private const string EmergencyDeckCardId = "fire_wisp";
    private const int EmergencyDeckSize = 3;

    // =========================================================================
    // SIGNALS (emitted for GDScript UI consumers)
    // =========================================================================

    [Signal]
    public delegate void GameStartedEventHandler();

    [Signal]
    public delegate void GameEndedEventHandler(int winnerTeam);

    [Signal]
    public delegate void TimeUpdatedEventHandler(float remaining);

    [Signal]
    public delegate void StateChangedEventHandler(int newState);

    [Signal]
    public delegate void PhaseChangedEventHandler(int newPhase);

    [Signal]
    public delegate void PrepTimerUpdatedEventHandler(float remaining);

    [Signal]
    public delegate void InitializationCompleteEventHandler();

    [Signal]
    public delegate void ReconnectStateChangedEventHandler(bool reconnecting, string reason);

    [Signal]
    public delegate void ReconnectTimerUpdatedEventHandler(float remainingSeconds);

    private bool _isShowingReconnectState;
    private int _lastReconnectSeconds = -1;
    private int _lastEmittedPhase = -1;
    private bool _scheduledAutoForfeit;
    private bool _scheduledForcedCompletion;
    private bool _scheduledForcedReport;
    private bool _scheduledAutoContinueAfterGameOver;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override async void _Ready()
    {
        AddToGroup(GroupIDs.GameController);
        AddToGroup(GroupIDs.BattleCoordinator);

        // Build typed config from BattleContext (one-time read)
        _config = BuildSessionConfig();
        ApplyPreparationDurationOverride();

        // Wait one frame for all scene nodes to be in tree
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Phase 1: Find battlefield
        Battlefield ??= GetNodeOrNull<Node3D>("Battlefield3D");

        // Phase 1.5: Preload unit scenes asynchronously
        await PreloadUnitScenes();

        // Phase 1.8: Create SimulationNode
        InitSimulationNode();

        // Phase 1.9: Initialize EntityManager
        InitEntityManager();

        // Phase 2: Initialize summoners
        InitSummoners();
        ConnectSimSignals();

        // Phase 4: Load AI for enemy
        LoadAiForEnemy();

        // Phase 6: Initialize UI
        InitUI();

        // Phase 6.5: Set up multiplayer
        SetupMultiplayer();

        // Subscribe to SimEventsEmitted for signal forwarding
        var simNode = GetSimNode();
        if (simNode is IGameSession session)
            session.SimEventsEmitted += OnSimEventsEmitted;

        EmitSignal(SignalName.InitializationComplete);

        // Start the game — client waits for first snapshot from host
        if (_config.IsMpClient)
        {
            var sn = GetSimNode();
            if (sn != null && sn.HasSignal("FirstSnapshotApplied"))
                await ToSignal(sn, "FirstSnapshotApplied");
        }
        if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--e2e-log-battle-start"))
            GD.Print("[RANKED][E2E] Battle scene initialized");

        MaybeScheduleAutoForfeit();
        MaybeScheduleForcedCompletion();
        MaybeScheduleForcedReport();
        MaybeScheduleAutoContinueAfterGameOver();
        StartGame();
    }

    private BattleSessionConfig BuildSessionConfig()
    {
        var battleContext = GetNodeOrNull("/root/BattleContext");
        if (battleContext == null)
            return BattleSessionConfig.ForPractice();

        bool configured = (bool)battleContext.Call("is_configured");
        if (!configured)
        {
            GD.PushError("BattleScene: BattleContext was NEVER configured!");
            GD.PushError("BattleScene: Did you run the battle scene directly (F6)?");
            GD.PushError("BattleScene: Configuring with practice mode defaults...");
            battleContext.Call("configure_practice_battle");
        }

        return BattleSessionConfig.FromBattleContext(battleContext);
    }

    private void ApplyPreparationDurationOverride()
    {
        if (_config.RawConfig == null || !_config.RawConfig.ContainsKey("prep_duration"))
            return;
        if (_config.PreparationDuration < 0f)
            return;

        PreparationDuration = _config.PreparationDuration;
    }

    public override void _ExitTree()
    {
        var simNode = GetSimNode();
        if (simNode is IGameSession session)
            session.SimEventsEmitted -= OnSimEventsEmitted;
    }

    public override void _Process(double delta)
    {
        if (CurrentState != GameState.Playing)
            return;

        // MP client: poll MatchState for timer/phase sync
        if (_config.IsMpClient)
            PollMatchState();

        if (_config.IsMultiplayer)
            PollReconnectState();
    }

    // =========================================================================
    // GAME FLOW METHODS
    // =========================================================================

    public void StartGame()
    {
        CurrentState = GameState.Playing;

        // Mark battle as in progress on BattleContext
        GetNodeOrNull("/root/BattleContext")?.Call("start_battle");

        // Start battle music
        var audio = GetNodeOrNull("/root/AudioManager");
        audio?.Call("play_music", audio.Get("MUSIC_BATTLE"));

        EmitSignal(SignalName.GameStarted);
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        var simNode = GetSimNode() as SimulationNode;
        var currentPhase =
            simNode != null
                ? ToUiPhaseValue((Fateforged.Simulation.Enums.GamePhase)simNode.GetPhase())
                : (int)BattlePhase.Preparation;
        EmitPhaseIfChanged(currentPhase);
        EmitSignal(SignalName.PrepTimerUpdated, PreparationDuration);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            GetTree().Paused = true;
            EmitSignal(SignalName.StateChanged, (int)CurrentState);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            GetTree().Paused = false;
            EmitSignal(SignalName.StateChanged, (int)CurrentState);
        }
    }

    public void FreezeGame()
    {
        GetTree().Paused = true;
    }

    public void UnfreezeGame()
    {
        if (CurrentState != GameState.Paused)
            GetTree().Paused = false;
    }

    public void EndGame(int winnerTeam)
    {
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = GameState.GameOver;
        _pendingCompletionWinnerTeam = winnerTeam;
        _completionHandled = false;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        EmitSignal(SignalName.GameEnded, winnerTeam);
        GetTree().Paused = true;

        // Stop battle music
        var audio = GetNodeOrNull("/root/AudioManager");
        audio?.Call("stop_music");

        // Multiplayer: broadcast match end
        if (_config.IsMultiplayer && _config.HasAuthority)
            BroadcastMatchEnd(winnerTeam);

        // Update BattleContext state
        var battleContext = GetNodeOrNull("/root/BattleContext");
        if (battleContext != null)
        {
            if (winnerTeam == 0)
                battleContext.Call("end_battle_victory");
            else
                battleContext.Call("end_battle_defeat");
        }

        // Wait for local UI confirmation before transitioning.
        // Multiplayer clients confirm independently on their own end-game UI.
    }

    public void ContinueAfterGameOver()
    {
        if (CurrentState != GameState.GameOver)
            return;
        if (_completionHandled || !_pendingCompletionWinnerTeam.HasValue)
            return;

        _completionHandled = true;
        int winnerTeam = _pendingCompletionWinnerTeam.Value;
        _pendingCompletionWinnerTeam = null;

        GetTree().Paused = false;
        HandleCompletion(winnerTeam);
    }

    /// <summary>
    /// Abandon the current battle — called by pause menu when player quits.
    /// Handles service cleanup (profile state, campaign) then delegates state cleanup to BattleContext.
    /// </summary>
    public void AbandonBattle()
    {
        var root = GetTree().Root;

        // Clear current_battle from profile to prevent stale state
        var profileRepo = root.GetNodeOrNull("ProfileRepo");
        if (profileRepo != null && profileRepo.HasMethod("UpdateCampaignProgressDict"))
        {
            var clearDict = new Godot.Collections.Dictionary { { "current_battle", "" } };
            profileRepo.Call("UpdateCampaignProgressDict", clearDict, "");
        }

        // Clear any pending reward
        var campaign = root.GetNodeOrNull("Campaign");
        if (campaign != null && campaign.HasMethod("ClearPendingReward"))
            campaign.Call("ClearPendingReward");

        // Delegate state cleanup to BattleContext
        var battleContext = root.GetNodeOrNull("BattleContext");
        battleContext?.Call("abandon_battle");

        _deckCardInstanceIds.Clear();
    }

    public void SkipPrepPhase()
    {
        var simNode = GetSimNode();
        simNode?.Call("SkipPreparation");
    }

    // =========================================================================
    // SIM EVENT → GODOT SIGNAL FORWARDING
    // =========================================================================

    private void OnSimEventsEmitted(IReadOnlyList<SimEvent> events)
    {
        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        foreach (var evt in events)
        {
            switch (evt)
            {
                case GameOverEvent e:
                    _matchEndReason = MapEndReason(e.Reason);
                    EndGame(simNode.RemapTeam(e.WinnerTeam));
                    break;
                case PhaseChangedEvent e:
                    EmitPhaseIfChanged(ToUiPhaseValue(e.NewPhase));
                    break;
                case PrepTimerUpdatedEvent e:
                    EmitSignal(SignalName.PrepTimerUpdated, e.Remaining);
                    break;
                case MatchTimeUpdatedEvent e:
                    float remaining = MatchDuration - e.MatchTime;
                    EmitSignal(SignalName.TimeUpdated, remaining);
                    break;
            }
        }
    }

    // =========================================================================
    // MP CLIENT POLLING
    // =========================================================================

    private void PollMatchState()
    {
        var simNode = GetSimNode();
        if (simNode == null)
            return;

        // Poll prep timer
        float prepRemaining = (float)simNode.Call("GetPrepTimeRemaining");
        EmitSignal(SignalName.PrepTimerUpdated, prepRemaining);

        // Poll match time
        float matchTime = (float)simNode.Call("GetMatchTime");
        float remaining = MatchDuration - matchTime;
        EmitSignal(SignalName.TimeUpdated, remaining);

        // Poll phase
        int phase = (int)simNode.Call("GetPhase");
        EmitPhaseIfChanged(ToUiPhaseValue((Fateforged.Simulation.Enums.GamePhase)phase));
    }

    private void EmitPhaseIfChanged(int phase)
    {
        if (_lastEmittedPhase == phase)
            return;
        _lastEmittedPhase = phase;
        EmitSignal(SignalName.PhaseChanged, phase);
    }

    private static int ToUiPhaseValue(Fateforged.Simulation.Enums.GamePhase phase)
    {
        return phase == Fateforged.Simulation.Enums.GamePhase.Preparation
            ? (int)BattlePhase.Preparation
            : (int)BattlePhase.Battle;
    }

    private void PollReconnectState()
    {
        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        bool reconnecting = simNode.IsReconnecting();
        if (reconnecting != _isShowingReconnectState)
        {
            _isShowingReconnectState = reconnecting;
            _lastReconnectSeconds = -1;
            EmitSignal(
                SignalName.ReconnectStateChanged,
                reconnecting,
                simNode.GetReconnectReason()
            );
        }

        if (!reconnecting)
            return;

        int remaining = Mathf.CeilToInt(simNode.GetReconnectRemainingSeconds());
        if (remaining != _lastReconnectSeconds)
        {
            _lastReconnectSeconds = remaining;
            EmitSignal(SignalName.ReconnectTimerUpdated, remaining);
        }
    }

    // =========================================================================
    // INITIALIZATION HELPERS
    // =========================================================================

    private async System.Threading.Tasks.Task PreloadUnitScenes()
    {
        if (
            GetNodeOrNull("/root/CardCatalog") is not Fateforged.Cards.CardCatalogBridge cardCatalog
        )
            return;

        var allIds = cardCatalog.GetAllCardIds();
        var scenePaths = new List<string>();

        foreach (var cardId in allIds)
        {
            var cardDef = cardCatalog.GetCardAsDict(cardId);
            if (cardDef == null)
                continue;
            var cardType = cardDef.GetValueOrDefault("type", "").ToString();
            if (cardType != "summon")
                continue;
            var unitScene = cardDef.GetValueOrDefault("unit_scene", "").ToString();
            if (!string.IsNullOrEmpty(unitScene) && !scenePaths.Contains(unitScene))
                scenePaths.Add(unitScene);
        }

        if (scenePaths.Count == 0)
            return;

        // Start async loading
        foreach (var path in scenePaths)
            ResourceLoader.LoadThreadedRequest(path, "PackedScene");

        // Wait for all to finish
        foreach (var path in scenePaths)
        {
            int framesWaited = 0;
            while (
                ResourceLoader.LoadThreadedGetStatus(path)
                == ResourceLoader.ThreadLoadStatus.InProgress
            )
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (++framesWaited >= SceneLoadTimeoutFrames)
                {
                    GD.PushWarning($"BattleScene: Timeout loading unit scene: {path}");
                    break;
                }
            }

            if (
                ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded
            )
            {
                var scene = ResourceLoader.LoadThreadedGet(path) as PackedScene;
                if (scene != null)
                {
                    var instance = scene.Instantiate();
                    instance.QueueFree();
                }
            }
        }
    }

    private void InitSimulationNode()
    {
        var simNode = new SimulationNode();
        AddChild(simNode);

        // Configure local perspective before init-dependent coordinate remapping.
        simNode.IsHost = _config.HasAuthority;
        simNode.LocalPlayerIndex = _config.HasAuthority ? 0 : 1;

        simNode.Initialize(
            PreparationDuration,
            MatchDuration,
            _config.WinCondition,
            _config.TimeLimit,
            _config.KillTarget,
            _config.BattleSeed
        );
    }

    private void InitEntityManager()
    {
        var simNode = GetSimNode();
        if (simNode is not IGameSession session)
        {
            GD.PushWarning("BattleScene: No SimulationNode found, skipping EntityManager init");
            return;
        }

        var em = new EntityManager();
        em.Name = "EntityManager";
        AddChild(em);
        var vfxManager = GetTree().Root.GetNodeOrNull("VFXManager");
        IBattleVfxService vfxService =
            vfxManager != null
                ? new GodotVfxManagerService(vfxManager)
                : NullBattleVfxService.Instance;
        em.Initialize(session, vfxService);
        EntityManager = em;
    }

    private void InitSummoners()
    {
        PlayerSummoner ??= GetTree().GetFirstNodeInGroup(GroupIDs.PlayerSummoners);
        EnemySummoner ??= GetTree().GetFirstNodeInGroup(GroupIDs.EnemySummoners);

        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        if (_config.IsMpClient)
        {
            // Client: register with defaults, skip deck loading
            InitSummonerAsClient(PlayerSummoner as SummonerVisual, simNode);
            InitSummonerAsClient(EnemySummoner as SummonerVisual, simNode);
        }
        else
        {
            // Initialize player summoner (team 0)
            InitSummonerHost(PlayerSummoner as SummonerVisual, 0, simNode);

            // Apply enemy stats before init
            ApplyEnemyStats(EnemySummoner as SummonerVisual);

            // Initialize enemy summoner (team 1)
            InitSummonerHost(EnemySummoner as SummonerVisual, 1, simNode);

            // Populate card data after BOTH summoners registered
            simNode.PopulateCardData();
        }

        // Initialize SummonerVisual and register with EntityManager
        if (PlayerSummoner is SummonerVisual playerSv)
        {
            playerSv.Initialize(simNode, simNode.RemapTeam(0));
            EntityManager?.RegisterSummonerVisual(playerSv, simNode.RemapTeam(0));
        }
        if (EnemySummoner is SummonerVisual enemySv)
        {
            enemySv.Initialize(simNode, simNode.RemapTeam(1));
            EntityManager?.RegisterSummonerVisual(enemySv, simNode.RemapTeam(1));
        }
    }

    private void InitSummonerAsClient(SummonerVisual? sv, SimulationNode simNode)
    {
        if (sv == null)
            return;

        // Register with scene defaults to prevent 0/0 HP/mana race
        // Host snapshots overwrite these values within ~100ms
        simNode.RegisterSummoner(
            sv.Team,
            sv.MaxHpExport,
            sv.MaxHpExport,
            DefaultMana,
            DefaultMaxMana,
            DefaultCastSpeed,
            Array.Empty<string>(),
            sv.MaxHandSize,
            sv.GlobalPosition,
            sv.GetTargetPointGlobalPosition()
        );
    }

    private void InitSummonerHost(SummonerVisual? sv, int localTeam, SimulationNode simNode)
    {
        if (sv == null)
            return;

        var result = BattleSessionFactory.LoadSummonerData(
            this,
            _config,
            localTeam,
            sv.DeckLoadStrategy,
            sv.MaxHpExport,
            sv.MaxHandSize,
            sv.StartingDeck
        );

        int totalCards = result.Deck.Count + result.Hand.Count;
        if (totalCards == 0 && !result.IsDeferred)
        {
            if (IsTestMode())
            {
                GD.PushWarning(
                    "[BattleScene] Empty deck in test mode, creating emergency fallback"
                );
                var emergency = CreateEmergencyDeck();
                foreach (var c in emergency)
                    result.Deck.Add(c);
            }
            else
            {
                GD.PushError("[BattleScene] CRITICAL - No deck loaded!");
                sv.IsEnabled = false;
                return;
            }
        }

        // Cache data locally for post-battle rewards
        if (localTeam == 0)
        {
            if (result.LoadedFromProfile)
            {
                // Extract instance IDs from hand + deck for XP tracking
                _deckCardInstanceIds.Clear();
                var allCards = new Godot.Collections.Array<Resource>(result.Hand);
                foreach (var c in result.Deck)
                    allCards.Add(c);
                foreach (var card in allCards)
                {
                    var go = card as GodotObject;
                    if (go == null)
                        continue;
                    string instanceId = go.Get("InstanceId").AsString();
                    if (!string.IsNullOrEmpty(instanceId))
                        _deckCardInstanceIds.Add(instanceId);
                }

                // Also store in BattleContext for reward_screen to read
                var bc = GetNodeOrNull("/root/BattleContext");
                bc?.Call("store_deck_card_ids", allCards);
            }

            if (result.SummonerStats != null)
            {
                var bc = GetNodeOrNull("/root/BattleContext");
                bc?.Call("set_player_summoner_stats", result.SummonerStats);
            }
        }

        // Register with simulation
        var deckIds = ExtractCatalogIds(result.Deck);
        var handIds = ExtractCatalogIds(result.Hand);
        var deckRefs = ExtractCardRefs(result.Deck);
        var handRefs = ExtractCardRefs(result.Hand);
        simNode.RegisterSummoner(
            localTeam,
            result.Hp,
            result.MaxHp,
            result.Mana,
            result.MaxMana,
            result.CastSpeed,
            deckIds,
            sv.MaxHandSize,
            sv.GlobalPosition,
            sv.GetTargetPointGlobalPosition()
        );
        simNode.SetSummonerCombatModifiers(
            localTeam,
            result.DamageBonus,
            result.DamageReduction,
            result.SoulStrength,
            result.ElementalDamageBonuses
        );
        simNode.SetSummonerHand(localTeam, handIds);
        simNode.SetSummonerCardRefs(localTeam, deckRefs, handRefs);
    }

    private void ApplyEnemyStats(SummonerVisual? sv)
    {
        if (sv == null)
            return;

        if (_config.IsMultiplayer)
        {
            // MP enemy stats applied via summoner instance bonuses during init
        }
        else if (_config.EnemyHp > 0)
        {
            sv.MaxHpExport = _config.EnemyHp;
        }
    }

    private static string[] ExtractCatalogIds(Godot.Collections.Array<Resource> cards)
    {
        var ids = new string[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i] as GodotObject;
            if (card != null)
                ids[i] = card.Get("CatalogId").AsString();
            else
                ids[i] = "";
        }
        return ids;
    }

    private static SimCardRuntimeRef[] ExtractCardRefs(Godot.Collections.Array<Resource> cards)
    {
        var refs = new SimCardRuntimeRef[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i] as GodotObject;
            if (card == null)
            {
                refs[i] = new SimCardRuntimeRef();
                continue;
            }

            refs[i] = new SimCardRuntimeRef
            {
                CatalogId = card.Get("CatalogId").AsString(),
                InstanceId = card.Get("InstanceId").AsString(),
            };
        }
        return refs;
    }

    private Godot.Collections.Array<Resource> CreateEmergencyDeck()
    {
        var deck = new Godot.Collections.Array<Resource>();
        for (int i = 0; i < EmergencyDeckSize; i++)
        {
            var card = BattleSessionFactory.CreateCardFromCatalog(EmergencyDeckCardId);
            if (card != null)
                deck.Add(card);
        }
        return deck;
    }

    private bool IsTestMode()
    {
        // Check if scene root is TestBattleScene
        var root = GetTree().CurrentScene;
        if (root is Debug.TestBattleScene)
            return true;

        // Check BattleContext practice mode
        return _config.Mode == BattleMode.Practice;
    }

    private void ConnectSimSignals()
    {
        // Connect SummonerVisual.SummonerDestroyed signals
        if (PlayerSummoner != null && PlayerSummoner.HasSignal("SummonerDestroyed"))
            PlayerSummoner.Connect(
                "SummonerDestroyed",
                new Callable(this, MethodName.OnSummonerDestroyed)
            );
        if (EnemySummoner != null && EnemySummoner.HasSignal("SummonerDestroyed"))
            EnemySummoner.Connect(
                "SummonerDestroyed",
                new Callable(this, MethodName.OnSummonerDestroyed)
            );
    }

    private void OnSummonerDestroyed(Node3D summoner)
    {
        // During Battle phase, sim handles win conditions via GameOverEvent
        var simNode = GetSimNode() as SimulationNode;
        if (simNode != null)
        {
            if (simNode.GetPhase() == (int)Fateforged.Simulation.Enums.GamePhase.Battle)
                return;
        }

        if (!_config.HasAuthority)
            return;

        if (summoner == PlayerSummoner)
            EndGame(1); // ENEMY wins
        else if (summoner == EnemySummoner)
            EndGame(0); // PLAYER wins
    }

    private void LoadAiForEnemy()
    {
        if (EnemySummoner == null)
            return;
        if (_config.IsMultiplayer)
            return;

        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        simNode.ConfigureAi(
            1,
            _config.AiType,
            _config.AiPersonality,
            _config.AiDifficulty,
            _config.AiIntervalMin,
            _config.AiIntervalMax,
            _config.AiScript
        );
    }

    private void InitUI()
    {
        // Find and init HandUI (pass SummonerVisual node)
        var handUi = GetTree().GetFirstNodeInGroup(GroupIDs.HandUI);
        if (handUi != null && handUi.HasMethod("init") && PlayerSummoner != null)
            handUi.Call("init", PlayerSummoner);

        // Find and init GameUI
        var gameUi = GetNodeOrNull("UI");
        if (gameUi != null && gameUi.HasMethod("init") && PlayerSummoner != null)
            gameUi.Call("init", this, PlayerSummoner, EnemySummoner!);

        // Find and init InputCollector
        var inputCollector = GetNodeOrNull<Fateforged.Input.InputCollector>("UI/InputCollector");
        inputCollector?.Initialize(PlayerSummoner!);
    }

    private void SetupMultiplayer()
    {
        if (!_config.IsMultiplayer)
            return;

        IMatchTransport? transport = null;
        Node? transportNode = null;

        // Reuse handoff transport from P2P lobby when present.
        var handoffNode = GetTree().GetFirstNodeInGroup(GroupIDs.MatchTransport);
        if (handoffNode is IMatchTransport existingTransport && existingTransport.IsConnected)
        {
            transport = existingTransport;
            transportNode = handoffNode;
            if (handoffNode.GetParent() != this)
                handoffNode.Reparent(this);
        }
        else
        {
            if (handoffNode != null)
                handoffNode.QueueFree();

            // Ranked/Nakama path creates transport directly in battle scene.
            var nakamaTransport = new NakamaMatchTransport();
            AddChild(nakamaTransport);
            transportNode = nakamaTransport;
            transport = nakamaTransport;

            var nakama = GetTree().Root.GetNodeOrNull("NakamaGameClient");
            string matchId = "";
            if (nakama != null)
            {
                var activeMatchId = nakama.Get("ActiveMatchId");
                if (activeMatchId.VariantType != Variant.Type.Nil)
                    matchId = activeMatchId.ToString();
            }

            nakamaTransport.Initialize(matchId, _config.HasAuthority, _config.HasAuthority ? 1 : 2);
        }

        if (transportNode != null && !transportNode.IsInGroup(GroupIDs.MatchTransport))
            transportNode.AddToGroup(GroupIDs.MatchTransport);

        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null || transport == null)
        {
            GD.PrintErr(
                "[BattleScene] Failed to configure multiplayer session (missing sim node or transport)"
            );
            return;
        }

        simNode.ConfigureMultiplayerSession(transport, _config.HasAuthority);
        GD.Print("[BattleScene] Multiplayer session configured via HostSession/ClientSession");
    }

    private void BroadcastMatchEnd(int winnerTeam)
    {
        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        simNode.BroadcastMatchEnded(winnerTeam, "BattleEnded");
    }

    // =========================================================================
    // POST-BATTLE COMPLETION
    // =========================================================================

    private void HandleCompletion(int winnerTeam)
    {
        switch (_config.Mode)
        {
            case BattleMode.Campaign:
                HandleCampaignCompletion(winnerTeam);
                break;
            case BattleMode.Arena:
                GD.Print($"[BattleScene] Arena battle ended, winner: {winnerTeam}");
                break;
            case BattleMode.Endless:
                GD.Print($"[BattleScene] Endless battle ended, winner: {winnerTeam}");
                break;
            case BattleMode.Practice:
                GD.Print($"[BattleScene] Practice battle ended, winner: {winnerTeam}");
                break;
            case BattleMode.Multiplayer:
                HandleMultiplayerCompletion(winnerTeam);
                break;
            case BattleMode.Academy:
                HandleAcademyCompletion(winnerTeam);
                break;
        }
    }

    private void HandleCampaignCompletion(int winnerTeam)
    {
        if (winnerTeam == 0) // Player won
        {
            GrantCardXp();
            GrantSummonerXp();
            NavigateToScene("res://scenes/meta/screens/reward_screen.tscn");
        }
        else // Player lost
        {
            NavigateToScene("res://scenes/meta/screens/academy_hub.tscn");
        }
    }

    private void HandleAcademyCompletion(int winnerTeam)
    {
        if (winnerTeam == 0)
        {
            var battleContext = GetNodeOrNull("/root/BattleContext");
            var courseId = battleContext?.Get("academy_course_id").AsString() ?? "";
            var campaign = GetNodeOrNull("/root/Campaign");
            if (!string.IsNullOrEmpty(courseId) && campaign != null)
                campaign.Call("CompleteNextAcademyActivity", courseId);
        }

        NavigateToScene("res://scenes/meta/screens/academy_course_path.tscn");
    }

    private void HandleMultiplayerCompletion(int winnerTeam)
    {
        GD.Print($"[BattleScene] Multiplayer battle ended, winner: {winnerTeam}");

        bool playerWon = winnerTeam == 0;

        if (_config.IsRankedMatch)
            ReportRankedMatch(playerWon);

        if (_config.IsRankedMatch)
            NavigateToScene("res://scenes/meta/screens/online_screen.tscn");
        else
            NavigateToScene("res://scenes/meta/screens/multiplayer_lobby.tscn");
    }

    /// <summary>Grant XP to all deck cards used in battle.</summary>
    internal void GrantCardXp()
    {
        if (_config.CardXpReward <= 0)
            return;
        if (_deckCardInstanceIds.Count == 0)
            return;

        var cardService = GetNodeOrNull("/root/CardService");
        if (cardService == null || !cardService.HasMethod("GrantXpToCardsArray"))
        {
            GD.PushWarning("[BattleScene] CardService.GrantXpToCardsArray not available");
            return;
        }

        var idsArray = new Godot.Collections.Array();
        foreach (var id in _deckCardInstanceIds)
            idsArray.Add(id);

        GD.Print(
            $"[BattleScene] Granting {_config.CardXpReward} XP to {_deckCardInstanceIds.Count} deck cards"
        );
        cardService.Call("GrantXpToCardsArray", idsArray, _config.CardXpReward);
    }

    /// <summary>Grant XP to the active summoner.</summary>
    internal void GrantSummonerXp()
    {
        if (_config.SummonerXpReward <= 0)
            return;

        var summonerProg = GetNodeOrNull("/root/SummonerProgression");
        if (summonerProg == null || !summonerProg.HasMethod("GrantActiveSummonerXp"))
        {
            GD.PushWarning("[BattleScene] SummonerProgression.GrantActiveSummonerXp not available");
            return;
        }

        GD.Print($"[BattleScene] Granting {_config.SummonerXpReward} XP to active summoner");
        summonerProg.Call("GrantActiveSummonerXp", _config.SummonerXpReward);
    }

    private void ReportRankedMatch(bool playerWon)
    {
        var matchInfo = _config.RankedMatchInfo;
        if (matchInfo == null || matchInfo.Count == 0)
        {
            GD.Print("[BattleScene] No ranked match info to report");
            return;
        }

        var rankingService = GetNodeOrNull<RankingService>("/root/RankingService");
        if (rankingService == null)
        {
            GD.Print("[BattleScene] RankingService not available");
            return;
        }

        string matchId = matchInfo.GetValueOrDefault("match_id", "").ToString();
        string opponentId = matchInfo.GetValueOrDefault("opponent_user_id", "").ToString();
        int opponentRating = (int)
            matchInfo.GetValueOrDefault("opponent_rating", EloCalculator.StartingElo);
        float duration = (GetSimNode() as SimulationNode)?.GetMatchTime() ?? 0f;

        GD.Print(
            $"[RANKED][REPORT] Reporting ranked match — won: {playerWon}, reason: {_matchEndReason}, match={matchId}, opponent={opponentId}, opponent_rating={opponentRating}"
        );
        rankingService.ReportMatch(
            playerWon,
            opponentRating,
            matchId,
            opponentId,
            duration,
            _matchEndReason
        );
    }

    private static MatchEndReason MapEndReason(string reason)
    {
        if (reason == "Forfeit")
            return MatchEndReason.Forfeit;
        if (reason.StartsWith("Disconnected"))
            return MatchEndReason.Disconnect;
        if (reason is "Time expired" or "Survived")
            return MatchEndReason.Timeout;
        return MatchEndReason.SummonerDestroyed;
    }

    private void NavigateToScene(string scenePath)
    {
        var sceneManager = GetNodeOrNull("/root/SceneManager");
        if (sceneManager != null)
            sceneManager.Call("transition_to", scenePath);
        else
            GetTree().ChangeSceneToFile(scenePath);
    }

    private void MaybeScheduleAutoForfeit()
    {
        if (_scheduledAutoForfeit)
            return;

        float delaySeconds = 0f;
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            const string Prefix = "--e2e-auto-forfeit-seconds=";
            if (!arg.StartsWith(Prefix))
                continue;

            var value = arg.Substring(Prefix.Length);
            if (float.TryParse(value, out var parsedDelay))
                delaySeconds = parsedDelay;
            break;
        }

        if (delaySeconds <= 0f)
            return;

        _scheduledAutoForfeit = true;
        _ = AutoForfeitAfterDelayAsync(delaySeconds);
    }

    private async System.Threading.Tasks.Task AutoForfeitAfterDelayAsync(float delaySeconds)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);

        var simNode = GetSimNode() as SimulationNode;
        if (simNode == null)
            return;

        int localTeam = _config.HasAuthority ? 0 : 1;
        GD.Print($"[RANKED][E2E] Auto-forfeit issuing for local team {localTeam}");
        simNode.SubmitCommand(new ForfeitCommand(localTeam));
    }

    private void MaybeScheduleForcedCompletion()
    {
        if (_scheduledForcedCompletion)
            return;

        float delaySeconds = 0f;
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            const string Prefix = "--e2e-force-complete-seconds=";
            if (!arg.StartsWith(Prefix))
                continue;

            var value = arg.Substring(Prefix.Length);
            if (float.TryParse(value, out var parsedDelay))
                delaySeconds = parsedDelay;
            break;
        }

        if (delaySeconds <= 0f)
            return;

        _scheduledForcedCompletion = true;
        GD.Print($"[RANKED][E2E] Scheduled forced completion in {delaySeconds:0.0}s");
        _ = ForceCompletionAfterDelayAsync(delaySeconds);
    }

    private void ExecuteForcedCompletion()
    {
        if (CurrentState == GameState.GameOver)
            return;

        int winnerTeam = _config.HasAuthority ? 0 : 1;
        GD.Print($"[RANKED][E2E] Forcing local completion, winner team {winnerTeam}");
        EndGame(winnerTeam);
        ContinueAfterGameOver();
    }

    private async System.Threading.Tasks.Task ForceCompletionAfterDelayAsync(float delaySeconds)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
        CallDeferred(MethodName.ExecuteForcedCompletion);
    }

    private void MaybeScheduleForcedReport()
    {
        if (_scheduledForcedReport)
            return;

        float delaySeconds = 0f;
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            const string Prefix = "--e2e-force-report-seconds=";
            if (!arg.StartsWith(Prefix))
                continue;

            var value = arg.Substring(Prefix.Length);
            if (float.TryParse(value, out var parsedDelay))
                delaySeconds = parsedDelay;
            break;
        }

        if (delaySeconds <= 0f)
            return;

        _scheduledForcedReport = true;
        GD.Print($"[RANKED][E2E] Scheduled forced report in {delaySeconds:0.0}s");
        _ = ForceReportAfterDelayAsync(delaySeconds);
    }

    private void ExecuteForcedReport()
    {
        if (!_config.IsRankedMatch)
            return;

        GD.Print("[RANKED][E2E] Forcing ranked report checkpoint");
        ReportRankedMatch(playerWon: true);
    }

    private async System.Threading.Tasks.Task ForceReportAfterDelayAsync(float delaySeconds)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
        CallDeferred(MethodName.ExecuteForcedReport);
    }

    private void MaybeScheduleAutoContinueAfterGameOver()
    {
        if (_scheduledAutoContinueAfterGameOver)
            return;

        bool hasE2EFlag = Array.Exists(
            OS.GetCmdlineUserArgs(),
            arg =>
                arg == "--e2e-log-battle-start"
                || arg.StartsWith("--e2e-auto-forfeit-seconds=")
                || arg.StartsWith("--e2e-force-complete-seconds=")
                || arg.StartsWith("--e2e-force-report-seconds=")
        );
        if (!hasE2EFlag)
            return;

        _scheduledAutoContinueAfterGameOver = true;
        _ = AutoContinueAfterGameOverAsync();
    }

    private async System.Threading.Tasks.Task AutoContinueAfterGameOverAsync()
    {
        while (IsInsideTree() && CurrentState != GameState.GameOver)
        {
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        }

        if (!IsInsideTree() || CurrentState != GameState.GameOver)
            return;

        GD.Print("[RANKED][E2E] Auto-continuing after game over");
        CallDeferred(MethodName.ContinueAfterGameOver);
    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    private Node? GetSimNode() => GetTree().GetFirstNodeInGroup(GroupIDs.SimulationNode);
}
