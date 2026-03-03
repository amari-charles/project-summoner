using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Events;
using Fateforged.Session;
using Godot;
using ProjectSummoner.Constants;

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

    public enum GameState { Setup, Playing, Paused, GameOver }
    public enum BattlePhase { Preparation, Battle }

    // =========================================================================
    // EXPORTS (wired in .tscn)
    // =========================================================================

    [Export] public float MatchDuration { get; set; } = 180.0f;
    [Export] public float OvertimeDuration { get; set; } = 60.0f;
    [Export] public float PreparationDuration { get; set; } = 30.0f;

    [Export] public Node3D? Battlefield { get; set; }
    [Export] public Node? PlayerSummoner { get; set; }
    [Export] public Node? EnemySummoner { get; set; }

    // =========================================================================
    // STATE
    // =========================================================================

    public int CurrentState { get; set; } = (int)GameState.Setup;

    // Child components
    public EntityManager? EntityManager { get; private set; }

    /// Max frames to wait for a single scene to load (~5 seconds at 60fps)
    private const int SceneLoadTimeoutFrames = 300;

    // =========================================================================
    // SIGNALS (emitted for GDScript UI consumers)
    // =========================================================================

    [Signal] public delegate void GameStartedEventHandler();
    [Signal] public delegate void GameEndedEventHandler(int winnerTeam);
    [Signal] public delegate void TimeUpdatedEventHandler(float remaining);
    [Signal] public delegate void StateChangedEventHandler(int newState);
    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);
    [Signal] public delegate void PrepTimerUpdatedEventHandler(float remaining);
    [Signal] public delegate void InitializationCompleteEventHandler();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override async void _Ready()
    {
        AddToGroup(GroupIDs.GameController);
        AddToGroup("battle_coordinator");

        // Validate BattleContext
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            bool configured = (bool)battleContext.Call("is_configured");
            if (!configured)
            {
                GD.PushError("BattleScene: BattleContext was NEVER configured!");
                GD.PushError("BattleScene: Did you run the battle scene directly (F6)?");
                GD.PushError("BattleScene: Configuring with practice mode defaults...");
                battleContext.Call("configure_practice_battle");
            }
        }

        // Wait one frame for all scene nodes to be in tree
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Phase 1: Find battlefield
        Battlefield ??= GetNodeOrNull<Node3D>("Battlefield3D");

        // Phase 1.5: Preload unit scenes asynchronously
        await PreloadUnitScenes();

        // Phase 1.75: Read win condition config from BattleContext
        string winCondition = "destroy_base";
        float winConditionTimeLimit = 0f;
        int winConditionKillTarget = 0;
        if (battleContext != null)
        {
            var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");
            if (config != null && config.Count > 0)
            {
                var condStr = config.GetValueOrDefault("win_condition", "").ToString();
                if (!string.IsNullOrEmpty(condStr))
                    winCondition = condStr;
                winConditionTimeLimit = (float)config.GetValueOrDefault("time_limit", 0.0f);
                winConditionKillTarget = (int)config.GetValueOrDefault("kill_target", 0);
            }
        }

        // Phase 1.8: Create SimulationNode
        long battleSeed = 0;
        if (battleContext != null)
        {
            var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");
            if (config != null)
                battleSeed = (long)config.GetValueOrDefault("battle_seed", 0);
        }
        InitSimulationNode(winCondition, winConditionTimeLimit, winConditionKillTarget, battleSeed);

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
        bool isMpClient = IsMpClient();
        if (isMpClient)
        {
            var sn = GetSimNode();
            if (sn != null && sn.HasSignal("FirstSnapshotApplied"))
                await ToSignal(sn, "FirstSnapshotApplied");
        }
        StartGame();
    }

    public override void _ExitTree()
    {
        var simNode = GetSimNode();
        if (simNode is IGameSession session)
            session.SimEventsEmitted -= OnSimEventsEmitted;
    }

    public override void _Process(double delta)
    {
        if (CurrentState != (int)GameState.Playing)
            return;

        // MP client: poll MatchState for timer/phase sync
        if (IsMpClient())
            PollMatchState();
    }

    // =========================================================================
    // GAME FLOW METHODS
    // =========================================================================

    public void StartGame()
    {
        CurrentState = (int)GameState.Playing;

        // Mark battle as in progress
        var battleContext = GetNode("/root/BattleContext");
        battleContext?.Call("start_battle");

        // Start battle music
        var audio = GetNodeOrNull("/root/AudioManager");
        audio?.Call("play_music", audio.Get("MUSIC_BATTLE"));

        EmitSignal(SignalName.GameStarted);
        EmitSignal(SignalName.StateChanged, CurrentState);
        EmitSignal(SignalName.PhaseChanged, (int)BattlePhase.Preparation);
        EmitSignal(SignalName.PrepTimerUpdated, PreparationDuration);
    }

    public void PauseGame()
    {
        if (CurrentState == (int)GameState.Playing)
        {
            CurrentState = (int)GameState.Paused;
            GetTree().Paused = true;
            EmitSignal(SignalName.StateChanged, CurrentState);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == (int)GameState.Paused)
        {
            CurrentState = (int)GameState.Playing;
            GetTree().Paused = false;
            EmitSignal(SignalName.StateChanged, CurrentState);
        }
    }

    public void FreezeGame()
    {
        GetTree().Paused = true;
    }

    public void UnfreezeGame()
    {
        if (CurrentState != (int)GameState.Paused)
            GetTree().Paused = false;
    }

    public void RestartGame()
    {
        GetTree().Paused = false;
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
            battleContext.Set("battle_state", 1); // CONFIGURED
        GetTree().ReloadCurrentScene();
    }

    public async void EndGame(int winnerTeam)
    {
        if (CurrentState == (int)GameState.GameOver)
            return;

        CurrentState = (int)GameState.GameOver;
        EmitSignal(SignalName.StateChanged, CurrentState);
        EmitSignal(SignalName.GameEnded, winnerTeam);
        GetTree().Paused = true;

        // Stop battle music
        var audio = GetNodeOrNull("/root/AudioManager");
        audio?.Call("stop_music");

        // Multiplayer: broadcast match end
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
            bool hasAuth = (bool)battleContext.Call("has_authority");
            if (isMp && hasAuth)
                BroadcastMatchEnd(winnerTeam);

            // Update BattleContext state
            if (winnerTeam == 0) // PLAYER
                battleContext.Call("end_battle_victory");
            else
                battleContext.Call("end_battle_defeat");

            // Delegate to completion callback
            var callback = battleContext.Get("completion_callback");
            if (callback.VariantType == Variant.Type.Callable)
            {
                var callable = callback.AsCallable();
                await ToSignal(GetTree().CreateTimer(2.0, true), SceneTreeTimer.SignalName.Timeout);
                GetTree().Paused = false;
                callable.Call(winnerTeam);
            }
        }
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
        if (simNode == null) return;

        foreach (var evt in events)
        {
            switch (evt)
            {
                case GameOverEvent e:
                    EndGame(simNode.RemapTeam(e.WinnerTeam));
                    break;
                case PhaseChangedEvent e:
                    EmitSignal(SignalName.PhaseChanged, (int)e.NewPhase);
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
        if (simNode == null) return;

        // Poll prep timer
        float prepRemaining = (float)simNode.Call("GetPrepTimeRemaining");
        EmitSignal(SignalName.PrepTimerUpdated, prepRemaining);

        // Poll match time
        float matchTime = (float)simNode.Call("GetMatchTime");
        float remaining = MatchDuration - matchTime;
        EmitSignal(SignalName.TimeUpdated, remaining);

        // Poll phase
        int phase = (int)simNode.Call("GetPhase");
        if (phase == 1) // Battle
            EmitSignal(SignalName.PhaseChanged, (int)BattlePhase.Battle);
    }

    // =========================================================================
    // INITIALIZATION HELPERS
    // =========================================================================

    private async System.Threading.Tasks.Task PreloadUnitScenes()
    {
        var cardCatalog = GetNodeOrNull("/root/CardCatalog");
        if (cardCatalog == null) return;

        var allIds = (Godot.Collections.Array<string>)cardCatalog.Call("get_all_card_ids");
        var scenePaths = new List<string>();

        foreach (var cardId in allIds)
        {
            var cardDef = (Godot.Collections.Dictionary)cardCatalog.Call("get_card", cardId);
            if (cardDef == null) continue;
            var cardType = cardDef.GetValueOrDefault("type", "").ToString();
            if (cardType != "summon") continue;
            var unitScene = cardDef.GetValueOrDefault("unit_scene", "").ToString();
            if (!string.IsNullOrEmpty(unitScene) && !scenePaths.Contains(unitScene))
                scenePaths.Add(unitScene);
        }

        if (scenePaths.Count == 0) return;

        // Start async loading
        foreach (var path in scenePaths)
            ResourceLoader.LoadThreadedRequest(path, "PackedScene");

        // Wait for all to finish
        foreach (var path in scenePaths)
        {
            int framesWaited = 0;
            while (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (++framesWaited >= SceneLoadTimeoutFrames)
                {
                    GD.PushWarning($"BattleScene: Timeout loading unit scene: {path}");
                    break;
                }
            }

            if (ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded)
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

    private void InitSimulationNode(string winCondition, float timeLimit, int killTarget, long seed)
    {
        var simNode = new SimulationNode();
        AddChild(simNode);

        simNode.Initialize(PreparationDuration, MatchDuration, winCondition, timeLimit, killTarget, seed);

        // Client setup
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
            bool hasAuth = (bool)battleContext.Call("has_authority");
            if (isMp && !hasAuth)
            {
                simNode.Set("IsHost", false);
                simNode.Set("LocalPlayerIndex", 1);
            }
        }
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
        em.Initialize(session);
        EntityManager = em;
    }

    private void InitSummoners()
    {
        PlayerSummoner ??= GetTree().GetFirstNodeInGroup(GroupIDs.PlayerSummoners);
        EnemySummoner ??= GetTree().GetFirstNodeInGroup(GroupIDs.EnemySummoners);

        var battleContext = GetNode("/root/BattleContext");
        if (battleContext == null) return;

        bool isMpClient = IsMpClient();

        if (isMpClient)
        {
            PlayerSummoner?.Call("init_as_client");
            EnemySummoner?.Call("init_as_client");
        }
        else
        {
            // Apply enemy stats before init
            if (EnemySummoner != null)
            {
                bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
                var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");

                if (isMp)
                {
                    var opponentData = (Godot.Collections.Dictionary)config.GetValueOrDefault("opponent_summoner_data", new Godot.Collections.Dictionary());
                    if (opponentData != null && opponentData.Count > 0)
                    {
                        var siClass = GD.Load<Script>("res://scripts/core/summoner_instance.gd");
                        if (siClass != null)
                        {
                            var opponent = siClass.Call("from_dict", opponentData);
                            if (opponent.VariantType != Variant.Type.Nil)
                                EnemySummoner.Call("set_summoner_instance", opponent);
                        }
                    }
                }
                else if (config.ContainsKey("enemy_hp"))
                {
                    float customHp = (float)config["enemy_hp"];
                    EnemySummoner.Set("max_hp", customHp);
                    EnemySummoner.Set("current_hp", customHp);
                }
            }

            PlayerSummoner?.Call("init");
            EnemySummoner?.Call("init");

            // Populate card data after both summoners registered
            var simNode = GetSimNode();
            simNode?.Call("PopulateCardData");
        }
    }

    private void ConnectSimSignals()
    {
        var simNode = GetSimNode();
        if (simNode == null)
        {
            GD.PushWarning("BattleScene: No SimulationNode found for signal connection");
            return;
        }

        // GameOver handled via SimEventsEmitted subscription

        // Legacy summoner_destroyed fallback
        if (PlayerSummoner != null && PlayerSummoner.HasSignal("summoner_destroyed"))
            PlayerSummoner.Connect("summoner_destroyed", new Callable(this, MethodName.OnSummonerDestroyed));
        if (EnemySummoner != null && EnemySummoner.HasSignal("summoner_destroyed"))
            EnemySummoner.Connect("summoner_destroyed", new Callable(this, MethodName.OnSummonerDestroyed));
    }

    private void OnSummonerDestroyed(Node summoner)
    {
        // During Battle phase, sim handles win conditions via GameOverEvent
        var simNode = GetSimNode();
        if (simNode != null)
        {
            int phase = (int)simNode.Call("GetPhase");
            if (phase == 1) return; // Battle phase
        }

        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null && !(bool)battleContext.Call("has_authority"))
            return;

        if (summoner == PlayerSummoner)
            EndGame(1); // ENEMY wins
        else if (summoner == EnemySummoner)
            EndGame(0); // PLAYER wins
    }

    private void LoadAiForEnemy()
    {
        if (EnemySummoner == null) return;

        var battleContext = GetNode("/root/BattleContext");
        if (battleContext == null) return;

        bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
        if (isMp) return;

        var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");
        if (config == null || config.Count == 0) return;

        // Remove existing AI
        foreach (var child in EnemySummoner.GetChildren())
        {
            if (child.HasMethod("decide_next_play"))
                child.QueueFree();
        }

        // Create and attach AI
        var aiLoaderScript = GD.Load<Script>("res://scripts/ai/ai_loader.gd");
        if (aiLoaderScript == null) return;

        var ai = (Node?)aiLoaderScript.Call("create_ai_for_battle", config, EnemySummoner);
        if (ai != null)
            EnemySummoner.AddChild(ai);
        else
            GD.PushError("BattleScene: Failed to create AI!");
    }

    private void InitUI()
    {
        // Find and init HandUI
        var handUi = GetTree().GetFirstNodeInGroup(GroupIDs.HandUI);
        if (handUi != null && handUi.HasMethod("init") && PlayerSummoner != null)
            handUi.Call("init", PlayerSummoner);

        // Find and init GameUI
        var gameUi = GetNodeOrNull("UI");
        if (gameUi != null && gameUi.HasMethod("init") && PlayerSummoner != null)
            gameUi.Call("init", this, PlayerSummoner, EnemySummoner!);

        // Find and init BattlefieldDropZone
        var dropZone = GetNodeOrNull("UI/BattlefieldDropZone");
        if (dropZone != null && dropZone.HasMethod("init") && PlayerSummoner != null)
            dropZone.Call("init", PlayerSummoner);
    }

    private void SetupMultiplayer()
    {
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext == null) return;

        bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
        if (!isMp) return;

        var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");
        bool isHost = (bool)battleContext.Call("has_authority");
        int localPeerId = isHost ? 1 : 2;
        int localPlayerIndex = isHost ? 0 : 1;

        // Create NakamaMatchTransport
        var transport = new Fateforged.Multiplayer.Transport.NakamaMatchTransport();
        AddChild(transport);

        // Get match ID
        var nakama = GetTree().Root.GetNodeOrNull("NakamaGameClient");
        string matchId = "";
        if (nakama != null)
        {
            var activeMatchId = nakama.Get("ActiveMatchId");
            if (activeMatchId.VariantType != Variant.Type.Nil)
                matchId = activeMatchId.ToString();
        }

        transport.Initialize(matchId, isHost, localPeerId);

        // MatchSession was deleted in M2. Multiplayer session wiring will be
        // handled by HostSession/ClientSession in a future milestone.
        GD.Print("[BattleScene] Multiplayer transport initialized (MatchSession pending future milestone)");
    }

    private void BroadcastMatchEnd(int winnerTeam)
    {
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext == null) return;

        var authProvider = battleContext.Get("authority_provider");
        if (authProvider.VariantType == Variant.Type.Nil) return;

        var provider = authProvider.AsGodotObject() as Node;
        if (provider == null) return;

        var matchSession = provider.Call("get_match_session");
        if (matchSession.VariantType == Variant.Type.Nil) return;

        var ms = matchSession.AsGodotObject() as Node;
        if (ms != null && ms.HasMethod("BroadcastMatchEnd"))
            ms.Call("BroadcastMatchEnd", winnerTeam, "Summoner destroyed");
    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    private Node? GetSimNode() => GetTree().GetFirstNodeInGroup("simulation_node");

    private bool IsMpClient()
    {
        var battleContext = GetNodeOrNull("/root/BattleContext");
        if (battleContext == null) return false;
        bool isMp = (bool)battleContext.Call("is_multiplayer_battle");
        bool hasAuth = (bool)battleContext.Call("has_authority");
        return isMp && !hasAuth;
    }
}
