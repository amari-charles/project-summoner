using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Events;
using Fateforged.Session;
using Godot;
using Fateforged.Constants;

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
		if (GetNodeOrNull("/root/CardCatalog") is not Fateforged.Cards.CardCatalogBridge cardCatalog) return;

		var allIds = cardCatalog.GetAllCardIds();
		var scenePaths = new List<string>();

		foreach (var cardId in allIds)
		{
			var cardDef = cardCatalog.GetCardAsDict(cardId);
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

		var simNode = GetSimNode() as SimulationNode;
		if (simNode == null) return;

		bool isMpClient = IsMpClient();

		if (isMpClient)
		{
			// Client: register with defaults, skip deck loading
			InitSummonerAsClient(PlayerSummoner as SummonerVisual, simNode);
			InitSummonerAsClient(EnemySummoner as SummonerVisual, simNode);
		}
		else
		{
			var config = (Godot.Collections.Dictionary)battleContext.Get("battle_config");

			// Initialize player summoner (team 0)
			InitSummonerHost(PlayerSummoner as SummonerVisual, 0, battleContext, config, simNode);

			// Apply enemy stats from BattleContext before init
			ApplyEnemyStats(EnemySummoner as SummonerVisual, battleContext, config);

			// Initialize enemy summoner (team 1)
			InitSummonerHost(EnemySummoner as SummonerVisual, 1, battleContext, config, simNode);

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
		if (sv == null) return;

		// Register with scene defaults to prevent 0/0 HP/mana race
		// Host snapshots overwrite these values within ~100ms
		simNode.RegisterSummoner(
			sv.Team, sv.MaxHpExport, sv.MaxHpExport,
			100f, 100f, 1.0f,
			Array.Empty<string>(), sv.MaxHandSize,
			sv.GlobalPosition
		);
	}

	private void InitSummonerHost(SummonerVisual? sv, int localTeam, Node battleContext,
		Godot.Collections.Dictionary? config, SimulationNode simNode)
	{
		if (sv == null) return;

		int strategy = sv.DeckLoadStrategy;

		// Auto-correct strategy based on team
		if (localTeam == 0 && strategy == 1) // Player with BATTLE_CONTEXT
			strategy = 2; // Switch to PROFILE
		else if (localTeam == 1 && strategy == 2) // Enemy with PROFILE
			strategy = 1; // Switch to BATTLE_CONTEXT

		// Auto-detect event_sequence battles
		if (localTeam == 1 && strategy == 1 && config != null)
		{
			if (config.ContainsKey("event_sequence") && config.ContainsKey("enemy_deck"))
			{
				var enemyDeckVar = config["enemy_deck"];
				if (enemyDeckVar.VariantType == Variant.Type.Array)
				{
					var arr = enemyDeckVar.AsGodotArray();
					if (arr.Count == 0)
					{
						GD.Print("[BattleScene] Battle uses event_sequence with empty enemy_deck - switching to DEFERRED");
						strategy = 3; // DEFERRED
					}
				}
			}
		}

		// Load deck by strategy
		float hp = sv.MaxHpExport;
		float maxHp = sv.MaxHpExport;
		float mana = 100f; // SummonerConfig.DEFAULT_MAX_MANA
		float maxMana = 100f;
		float castSpeed = 1.0f; // SummonerConfig.DEFAULT_BASE_CAST_SPEED
		var deck = new Godot.Collections.Array<Resource>();
		GodotObject? summonerInstance = null;

		switch (strategy)
		{
			case 0: // STATIC
				GD.Print("[BattleScene] Using STATIC starting deck");
				deck = sv.StartingDeck;
				break;

			case 1: // BATTLE_CONTEXT
				GD.Print("[BattleScene] Loading enemy deck from BattleContext...");
				if (config != null && config.ContainsKey("enemy_deck"))
				{
					var enemyDeckVar = config["enemy_deck"];
					if (enemyDeckVar.VariantType == Variant.Type.Array)
					{
						var entries = enemyDeckVar.AsGodotArray();
						foreach (var entry in entries)
						{
							if (entry.VariantType != Variant.Type.Dictionary) continue;
							var entryDict = entry.AsGodotDictionary();
							string catalogId = entryDict.GetValueOrDefault("catalog_id", "").ToString();
							int count = (int)entryDict.GetValueOrDefault("count", 1);
							for (int i = 0; i < count; i++)
							{
								var card = CreateCardFromCatalog(catalogId);
								if (card != null) deck.Add(card);
							}
						}
					}
				}
				if (deck.Count == 0)
				{
					GD.PushWarning("[BattleScene] Failed to load from BattleContext, using STATIC deck");
					deck = sv.StartingDeck;
				}
				break;

			case 2: // PROFILE
				GD.Print("[BattleScene] Loading deck from player profile...");
				LoadProfileDeck(sv, localTeam, config, ref deck, ref mana, ref maxMana, ref castSpeed, ref hp, ref maxHp, out summonerInstance);
				break;

			case 3: // DEFERRED
				GD.Print("[BattleScene] Using DEFERRED strategy - deck will be set manually later");
				break;
		}

		if (deck.Count == 0 && strategy != 3)
		{
			if (IsTestMode())
			{
				GD.PushWarning("[BattleScene] Empty deck in test mode, creating emergency fallback");
				deck = CreateEmergencyDeck();
			}
			else if (strategy != 3)
			{
				GD.PushError("[BattleScene] CRITICAL - No deck loaded! Strategy=" + strategy);
				sv.IsEnabled = false;
				return;
			}
		}

		if (deck.Count > 0)
			GD.Print($"[BattleScene] Loaded {deck.Count} cards (strategy={strategy})");

		// Note: Deck shuffling is handled by the simulation layer via DeterministicRng
		// when RecycleDeck() is called. Initial deck order is preserved as-is.

		// Draw starting hand
		var hand = new Godot.Collections.Array<Resource>();
		int drawCount = Math.Min(sv.MaxHandSize, deck.Count);
		for (int i = 0; i < drawCount; i++)
		{
			hand.Add(deck[0]);
			deck.RemoveAt(0);
		}

		// Extract catalog IDs
		var deckIds = ExtractCatalogIds(deck);
		var handIds = ExtractCatalogIds(hand);

		// Register with simulation
		simNode.RegisterSummoner(localTeam, hp, maxHp, mana, maxMana, castSpeed,
			deckIds, sv.MaxHandSize, sv.GlobalPosition);
		simNode.SetSummonerHand(localTeam, handIds);
	}

	private void LoadProfileDeck(SummonerVisual sv, int localTeam,
		Godot.Collections.Dictionary? config,
		ref Godot.Collections.Array<Resource> deck,
		ref float mana, ref float maxMana, ref float castSpeed,
		ref float hp, ref float maxHp,
		out GodotObject? summonerInstance)
	{
		summonerInstance = null;

		// Check for dev test deck override
		if (config != null && config.ContainsKey("dev_player_deck"))
		{
			GD.Print("[BattleScene] Loading DEV TEST deck...");
			var devDeckConfig = config["dev_player_deck"];
			if (devDeckConfig.VariantType == Variant.Type.Array)
			{
				var entries = devDeckConfig.AsGodotArray();
				foreach (var entry in entries)
				{
					if (entry.VariantType != Variant.Type.Dictionary) continue;
					var entryDict = entry.AsGodotDictionary();
					string catalogId = entryDict.GetValueOrDefault("catalog_id", "").ToString();
					int count = (int)entryDict.GetValueOrDefault("count", 1);
					for (int i = 0; i < count; i++)
					{
						var card = CreateCardFromCatalog(catalogId);
						if (card != null) deck.Add(card);
					}
				}
			}
		}
		else
		{
			// Normal path: load deck from profile via C# services
			var decksService = GetNodeOrNull("/root/Decks");
			var cardServiceCS = GetNodeOrNull("/root/CardServiceCS");
			if (decksService != null && cardServiceCS != null)
			{
				// Get selected deck ID from profile
				string deckId = "";
				var profileRepo = GetNodeOrNull("/root/ProfileRepo");
				if (profileRepo != null)
				{
					var profile = profileRepo.Call("get_active_profile");
					if (profile.VariantType == Variant.Type.Dictionary)
					{
						var profileDict = profile.AsGodotDictionary();
						var metaVar = profileDict.GetValueOrDefault("meta", new Godot.Collections.Dictionary());
						if (metaVar.VariantType == Variant.Type.Dictionary)
						{
							var metaDict = metaVar.AsGodotDictionary();
							var selectedDeck = metaDict.GetValueOrDefault("selected_deck", "");
							if (selectedDeck.VariantType == Variant.Type.String)
								deckId = selectedDeck.ToString();
						}
					}
				}

				// Fallback to first available deck
				if (string.IsNullOrEmpty(deckId))
				{
					var deckList = decksService.Call("list_decks");
					if (deckList.VariantType == Variant.Type.Array)
					{
						var list = deckList.AsGodotArray();
						if (list.Count > 0)
						{
							var firstDeck = list[0].AsGodotDictionary();
							deckId = firstDeck.GetValueOrDefault("id", "").ToString();
						}
					}
				}

				// Load deck cards
				if (!string.IsNullOrEmpty(deckId))
				{
					var deckData = decksService.Call("get_deck", deckId);
					if (deckData.VariantType == Variant.Type.Dictionary)
					{
						var deckDict = deckData.AsGodotDictionary();
						var instanceIdsVar = deckDict.GetValueOrDefault("card_instance_ids", new Godot.Collections.Array());
						if (instanceIdsVar.VariantType == Variant.Type.Array)
						{
							var ids = instanceIdsVar.AsGodotArray();
							foreach (var instanceId in ids)
							{
								var cardData = cardServiceCS.Call("GetCardDict", instanceId.ToString());
								if (cardData.VariantType != Variant.Type.Dictionary) continue;
								var cardDict = cardData.AsGodotDictionary();
								string catalogId = cardDict.GetValueOrDefault("catalog_id", "").ToString();
								if (!string.IsNullOrEmpty(catalogId))
								{
									var card = CreateCardFromCatalog(catalogId);
									if (card != null) deck.Add(card);
								}
							}
						}
					}
				}
			}
		}

		if (deck.Count == 0)
		{
			GD.PushWarning("[BattleScene] Failed to load from profile, using STATIC deck");
			deck = sv.StartingDeck;
		}
		else
		{
			// Store deck card IDs in BattleContext
			var battleContext = GetNode("/root/BattleContext");
			battleContext?.Call("store_deck_card_ids", deck);
		}

		// Load summoner instance from profile
		LoadSummonerFromProfile(localTeam, ref mana, ref maxMana, ref castSpeed, ref hp, ref maxHp, out summonerInstance);
	}

	private void LoadSummonerFromProfile(int localTeam,
		ref float mana, ref float maxMana, ref float castSpeed,
		ref float hp, ref float maxHp,
		out GodotObject? summonerInstance)
	{
		summonerInstance = null;

		// Get active summoner ID
		var summonerSelection = GetNodeOrNull("/root/SummonerSelection");
		if (summonerSelection == null) return;

		string summonerId = summonerSelection.Call("GetActiveSummonerId").AsString();
		if (string.IsNullOrEmpty(summonerId)) return;

		GD.Print($"[BattleScene] Active summoner ID: '{summonerId}'");

		// Load summoner instance data
		var profileRepo = GetNodeOrNull("/root/ProfileRepo");
		if (profileRepo == null) return;

		var instanceData = profileRepo.Call("get_summoner_instance", summonerId);
		GodotObject? loadedInstance = null;

		if (instanceData.VariantType == Variant.Type.Dictionary)
		{
			var dict = instanceData.AsGodotDictionary();
			if (dict.Count > 0)
			{
				var siClass = GD.Load<Script>("res://scripts/core/summoner_instance.gd");
				if (siClass != null)
				{
					var created = siClass.Call("from_dict", dict);
					if (created.VariantType != Variant.Type.Nil)
						loadedInstance = created.AsGodotObject();
				}
			}
		}

		if (loadedInstance == null)
		{
			// Create from catalog config
			var summonerCatalog = GetNodeOrNull("/root/SummonerCatalog");
			if (summonerCatalog != null)
			{
				var configObj = summonerCatalog.Call("get_summoner_config", summonerId);
				if (configObj.VariantType != Variant.Type.Nil)
				{
					var siClass = GD.Load<Script>("res://scripts/core/summoner_instance.gd");
					if (siClass != null)
					{
						loadedInstance = siClass.Call("new").AsGodotObject();
						if (loadedInstance != null)
							loadedInstance.Call("init_from_config", configObj);
					}
				}
			}
		}

		if (loadedInstance == null) return;
		summonerInstance = loadedInstance;

		// Get computed stats
		var stats = loadedInstance.Call("get_computed_stats");
		if (stats.VariantType != Variant.Type.Dictionary) return;
		var statsDict = stats.AsGodotDictionary();

		maxMana = (float)statsDict.GetValueOrDefault("max_mana", 100.0f);
		mana = maxMana;
		castSpeed = (float)statsDict.GetValueOrDefault("cast_speed", 1.0f);
		float health = (float)statsDict.GetValueOrDefault("health", 300.0f);
		maxHp = health;
		hp = health;

		// Cache summoner stats in BattleContext
		if (localTeam == 0)
		{
			var battleContext = GetNode("/root/BattleContext");
			battleContext?.Call("set_player_summoner_stats", statsDict);
		}

		GD.Print($"[BattleScene] Applied summoner bonuses - Max HP: {maxHp:F0}, Max Mana: {maxMana:F0}, Cast Speed: {castSpeed:F2}");
	}

	private void ApplyEnemyStats(SummonerVisual? sv, Node battleContext, Godot.Collections.Dictionary? config)
	{
		if (sv == null || config == null) return;

		bool isMp = (bool)battleContext.Call("is_multiplayer_battle");

		if (isMp)
		{
			var opponentData = (Godot.Collections.Dictionary)config.GetValueOrDefault("opponent_summoner_data", new Godot.Collections.Dictionary());
			if (opponentData != null && opponentData.Count > 0)
			{
				// MP enemy stats applied via summoner instance bonuses during init
				// (handled by LoadSummonerFromProfile equivalent for MP opponent)
			}
		}
		else if (config.ContainsKey("enemy_hp"))
		{
			float customHp = (float)config["enemy_hp"];
			sv.MaxHpExport = customHp;
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

	private static Resource? CreateCardFromCatalog(string catalogId)
	{
		var cardCatalogBridge = Fateforged.Cards.CardCatalog.GetCard(catalogId);
		if (cardCatalogBridge == null) return null;
		return Fateforged.Cards.Card.FromDefinition(cardCatalogBridge);
	}

	private Godot.Collections.Array<Resource> CreateEmergencyDeck()
	{
		var deck = new Godot.Collections.Array<Resource>();
		for (int i = 0; i < 3; i++)
		{
			var card = CreateCardFromCatalog("fire_wisp");
			if (card != null) deck.Add(card);
		}
		return deck;
	}

	private bool IsTestMode()
	{
		// Check if scene root is TestBattleScene
		var root = GetTree().CurrentScene;
		if (root is Debug.TestBattleScene) return true;

		// Check BattleContext practice mode
		var battleContext = GetNodeOrNull("/root/BattleContext");
		if (battleContext != null)
		{
			var mode = (int)battleContext.Get("current_mode");
			if (mode == 1) return true; // PRACTICE
		}

		return false;
	}

	private void ConnectSimSignals()
	{
		// GameOver handled via SimEventsEmitted subscription

		// Connect SummonerVisual.SummonerDestroyed signals
		if (PlayerSummoner != null && PlayerSummoner.HasSignal("SummonerDestroyed"))
			PlayerSummoner.Connect("SummonerDestroyed", new Callable(this, MethodName.OnSummonerDestroyed));
		if (EnemySummoner != null && EnemySummoner.HasSignal("SummonerDestroyed"))
			EnemySummoner.Connect("SummonerDestroyed", new Callable(this, MethodName.OnSummonerDestroyed));
	}

	private void OnSummonerDestroyed(Node3D summoner)
	{
		// During Battle phase, sim handles win conditions via GameOverEvent
		var simNode = GetSimNode() as SimulationNode;
		if (simNode != null)
		{
			if (simNode.GetPhase() == 1) return; // Battle phase
		}

		var battleContext = GetNodeOrNull("/root/BattleContext");
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

		var simNode = GetSimNode() as SimulationNode;
		if (simNode == null) return;

		// Extract AI config from battle_config
		string aiType = config.GetValueOrDefault("ai_type", "heuristic").ToString();
		string personality = config.GetValueOrDefault("ai_personality", "balanced").ToString();
		int difficulty = (int)config.GetValueOrDefault("ai_difficulty", 3);

		// Extract interval config
		float intervalMin = 3.0f;
		float intervalMax = 6.0f;
		var aiConfigVar = config.GetValueOrDefault("ai_config", default);
		if (aiConfigVar.VariantType == Variant.Type.Dictionary)
		{
			var aiCfg = aiConfigVar.AsGodotDictionary();
			intervalMin = (float)aiCfg.GetValueOrDefault("play_interval_min", 3.0f);
			intervalMax = (float)aiCfg.GetValueOrDefault("play_interval_max", 6.0f);
		}

		// Extract script steps for scripted AI
		Godot.Collections.Array? scriptSteps = null;
		var scriptVar = config.GetValueOrDefault("ai_script", default);
		if (scriptVar.VariantType == Variant.Type.Array)
			scriptSteps = scriptVar.AsGodotArray();

		// Configure AI via SimulationNode (team 1 = enemy in local coordinates)
		simNode.ConfigureAi(1, aiType, personality, difficulty, intervalMin, intervalMax, scriptSteps);
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

		// Multiplayer session wiring (HostSession/ClientSession) deferred to future milestone.
		GD.Print("[BattleScene] Multiplayer transport initialized (session wiring pending future milestone)");
	}

	private void BroadcastMatchEnd(int winnerTeam)
	{
		// TODO: Broadcast match end via HostSession/ClientSession transport
		GD.Print($"[BattleScene] BroadcastMatchEnd called (winner={winnerTeam}) — pending multiplayer transport milestone");
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
