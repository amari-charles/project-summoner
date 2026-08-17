namespace Fateforged.Tests.View;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Summoner;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Enums;
using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class BattleSceneTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Paused = false;

        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void EndGame_WaitsForContinueAndDoesNotCompleteImmediately()
    {
        var scene = CreateBattleScene();

        scene.EndGame(winnerTeam: 0);

        AssertThat(scene.CurrentState).IsEqual(BattleScene.GameState.GameOver);
        AssertThat(((SceneTree)Engine.GetMainLoop()).Paused).IsTrue();
        var pendingWinner = GetPrivateField<int?>(scene, "_pendingCompletionWinnerTeam");
        AssertThat(pendingWinner.HasValue).IsTrue();
        AssertThat(pendingWinner!.Value).IsEqual(0);
        AssertThat(GetPrivateField<bool>(scene, "_completionHandled")).IsFalse();
    }

    [TestCase]
    public void ContinueAfterGameOver_CompletesOnce_AndIgnoresSecondCall()
    {
        var scene = CreateBattleScene();
        scene.EndGame(winnerTeam: 1);

        scene.ContinueAfterGameOver();

        AssertThat(((SceneTree)Engine.GetMainLoop()).Paused).IsFalse();
        AssertThat(GetPrivateField<bool>(scene, "_completionHandled")).IsTrue();
        AssertThat(GetPrivateField<int?>(scene, "_pendingCompletionWinnerTeam").HasValue).IsFalse();

        // Must be idempotent to avoid double completion from repeated UI presses.
        scene.ContinueAfterGameOver();

        AssertThat(((SceneTree)Engine.GetMainLoop()).Paused).IsFalse();
        AssertThat(GetPrivateField<bool>(scene, "_completionHandled")).IsTrue();
        AssertThat(GetPrivateField<int?>(scene, "_pendingCompletionWinnerTeam").HasValue).IsFalse();
    }

    [TestCase]
    public void StartGame_DedupesRepeatedPhaseSignals()
    {
        var scene = CreateBattleScene();
        var simNode = CreateSimulationNode();
        simNode.GetState().Phase = GamePhase.Battle;
        var phases = new List<int>();
        scene.Connect(
            BattleScene.SignalName.PhaseChanged,
            Callable.From<int>(phase => phases.Add(phase))
        );

        scene.StartGame();
        scene.StartGame();

        AssertThat(phases.Count).IsEqual(1);
        AssertThat(phases[0]).IsEqual((int)BattleScene.BattlePhase.Battle);
    }

    [TestCase]
    public void StartGame_MapsPreparationPhase_FromSimulation()
    {
        var scene = CreateBattleScene();
        var simNode = CreateSimulationNode();
        simNode.GetState().Phase = GamePhase.Preparation;
        var phases = new List<int>();
        scene.Connect(
            BattleScene.SignalName.PhaseChanged,
            Callable.From<int>(phase => phases.Add(phase))
        );

        scene.StartGame();

        AssertThat(phases.Count).IsEqual(1);
        AssertThat(phases[0]).IsEqual((int)BattleScene.BattlePhase.Preparation);
    }

    [TestCase]
    public void StartGame_MapsGameOverPhase_ToBattleUiPhase()
    {
        var scene = CreateBattleScene();
        var simNode = CreateSimulationNode();
        simNode.GetState().Phase = GamePhase.GameOver;
        var phases = new List<int>();
        scene.Connect(
            BattleScene.SignalName.PhaseChanged,
            Callable.From<int>(phase => phases.Add(phase))
        );

        scene.StartGame();

        AssertThat(phases.Count).IsEqual(1);
        AssertThat(phases[0]).IsEqual((int)BattleScene.BattlePhase.Battle);
    }

    [TestCase]
    public void NewBattleScene_DefaultPreparationDuration_Is15Seconds()
    {
        var scene = CreateBattleScene();
        AssertThat(scene.PreparationDuration).IsEqual(15.0f);
    }

    [TestCase]
    public void BattleSideResolver_OpeningHandContainsOnlySummons()
    {
        var scene = CreateBattleScene();
        var staticDeck = new Godot.Collections.Array<Resource>();

        var spell = BattleSideResolver.CreateCardFromCatalog("mana_bolt");
        var summonA = BattleSideResolver.CreateCardFromCatalog("fire_wisp");
        var summonB = BattleSideResolver.CreateCardFromCatalog("puff");
        AssertThat(spell).IsNotNull();
        AssertThat(summonA).IsNotNull();
        AssertThat(summonB).IsNotNull();

        staticDeck.Add((Resource)spell!);
        staticDeck.Add((Resource)summonA!);
        staticDeck.Add((Resource)summonB!);

        var result = BattleSideResolver.Resolve(
            scene,
            new BattleSessionConfig
            {
                PlayerSide = new BattleSideDefinition
                {
                    Team = 0,
                    Source = BattleSideSource.Authored,
                    Summoner = new BattleSummonerDefinition
                    {
                        Source = BattleSideSource.SceneDefault,
                    },
                    Deck = new BattleDeckDefinition
                    {
                        Source = BattleDeckSource.Authored,
                        Cards =
                        [
                            new BattleDeckEntryDefinition { CatalogId = "mana_bolt", Count = 1 },
                            new BattleDeckEntryDefinition { CatalogId = "fire_wisp", Count = 1 },
                            new BattleDeckEntryDefinition { CatalogId = "puff", Count = 1 },
                        ],
                    },
                    Controller = new BattleControllerDefinition
                    {
                        Kind = BattleControllerKind.Player,
                    },
                },
            },
            localTeam: 0,
            sceneDefaultMaxHp: 100f,
            maxHandSize: 2,
            sceneFallbackDeck: staticDeck
        );

        AssertThat(result.Deck.HandCards.Count).IsEqual(2);
        var handTypes = result.Deck.HandCards.OfType<Card>().Select(card => card.Type).ToList();
        AssertThat(handTypes.Count).IsEqual(2);
        AssertThat(handTypes.All(type => type == (int)CardType.Summon)).IsTrue();

        AssertThat(result.Deck.DeckCards.Count).IsEqual(1);
        var remainingCard = result.Deck.DeckCards[0] as Card;
        AssertThat(remainingCard).IsNotNull();
        AssertThat(remainingCard!.Type).IsEqual((int)CardType.Spell);
    }

    [TestCase]
    public void AcademyBattleContext_ResolvesAuthoredPlayerSideDeck()
    {
        var scene = CreateBattleScene();
        var context = CreateBattleContext();
        var battleConfig = new Godot.Collections.Dictionary
        {
            ["player_side"] = new Godot.Collections.Dictionary
            {
                ["team"] = 0,
                ["source"] = "profile",
                ["summoner"] = new Godot.Collections.Dictionary { ["source"] = "profile" },
                ["deck"] = new Godot.Collections.Dictionary
                {
                    ["source"] = "authored",
                    ["cards"] = new Godot.Collections.Array
                    {
                        new Godot.Collections.Dictionary
                        {
                            ["catalog_id"] = "neutral_starter_unit",
                            ["count"] = 2,
                        },
                        new Godot.Collections.Dictionary
                        {
                            ["catalog_id"] = "magic_bolt",
                            ["count"] = 1,
                        },
                    },
                },
                ["controller"] = new Godot.Collections.Dictionary { ["kind"] = "player" },
            },
            ["enemy_side"] = AuthoredEnemyConfigDict("weak_enemy_unit", count: 1),
        };

        context.Call("configure_encounter_battle", "intro_spell_practice", battleConfig);

        var config = BattleSessionConfig.FromBattleContext(context);
        var resolved = BattleSideResolver.Resolve(
            scene,
            config,
            localTeam: 0,
            sceneDefaultMaxHp: 100f,
            maxHandSize: 4,
            sceneFallbackDeck: new Godot.Collections.Array<Resource>()
        );

        var allIds = resolved.Deck.CatalogIds(includeHand: true);
        AssertThat(allIds).Contains("neutral_starter_unit");
        AssertThat(allIds).Contains("magic_bolt");
        AssertThat(resolved.Deck.HandCatalogIds()).Contains("neutral_starter_unit");
        AssertThat(resolved.Deck.CatalogIds(includeHand: false)).Contains("magic_bolt");
    }

    [TestCase]
    public void EncounterBattleContext_DoesNotAllowCallerMutationToChangeStoredDeck()
    {
        var context = CreateBattleContext();
        var cards = new Godot.Collections.Array
        {
            new Godot.Collections.Dictionary
            {
                ["catalog_id"] = "neutral_starter_unit",
                ["count"] = 1,
            },
        };
        var battleConfig = new Godot.Collections.Dictionary
        {
            ["player_side"] = new Godot.Collections.Dictionary
            {
                ["team"] = 0,
                ["source"] = "profile",
                ["summoner"] = new Godot.Collections.Dictionary { ["source"] = "profile" },
                ["deck"] = new Godot.Collections.Dictionary
                {
                    ["source"] = "authored",
                    ["cards"] = cards,
                },
                ["controller"] = new Godot.Collections.Dictionary { ["kind"] = "player" },
            },
            ["enemy_side"] = AuthoredEnemyConfigDict("weak_enemy_unit", count: 1),
        };

        context.Call("configure_encounter_battle", "intro_summoning_practice", battleConfig);
        cards.Clear();

        var storedConfig = context.Get("battle_config").AsGodotDictionary();
        var playerSide = storedConfig["player_side"].AsGodotDictionary();
        var playerDeck = playerSide["deck"].AsGodotDictionary();
        var storedCards = playerDeck["cards"].AsGodotArray();
        AssertThat(storedCards).HasSize(1);
    }

    private Node CreateBattleContext()
    {
        var script = GD.Load<GDScript>("res://scripts/application/battle_context.gd");
        var context = (Node)script.New();
        _createdNodes.Add(context);
        return context;
    }

    private static Godot.Collections.Dictionary AuthoredEnemyConfigDict(string catalogId, int count)
    {
        return new Godot.Collections.Dictionary
        {
            ["team"] = 1,
            ["source"] = "authored",
            ["summoner"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["hp"] = 20f,
                ["max_hp"] = 20f,
            },
            ["deck"] = new Godot.Collections.Dictionary
            {
                ["source"] = "authored",
                ["cards"] = new Godot.Collections.Array
                {
                    new Godot.Collections.Dictionary
                    {
                        ["catalog_id"] = catalogId,
                        ["count"] = count,
                    },
                },
            },
            ["controller"] = new Godot.Collections.Dictionary
            {
                ["kind"] = "trainer_ai",
                ["ai_type"] = "none",
            },
        };
    }

    [TestCase]
    public void BattleSideResolver_AuthoredEnemy_UsesConfiguredSummonerStats()
    {
        var scene = CreateBattleScene();
        var staticDeck = new Godot.Collections.Array<Resource>();
        var starterCard = BattleSideResolver.CreateCardFromCatalog("fire_wisp");
        AssertThat(starterCard).IsNotNull();
        staticDeck.Add((Resource)starterCard!);

        var result = BattleSideResolver.Resolve(
            scene,
            new BattleSessionConfig
            {
                EnemySide = AuthoredEnemySide(hp: 35f, maxMana: 80f, mana: 40f, castSpeed: 1.25f),
            },
            localTeam: 1,
            sceneDefaultMaxHp: 300f,
            maxHandSize: 4,
            sceneFallbackDeck: staticDeck
        );

        AssertThat(result.Summoner.Hp).IsEqual(35f);
        AssertThat(result.Summoner.MaxHp).IsEqual(35f);
        AssertThat(result.Summoner.Mana).IsEqual(40f);
        AssertThat(result.Summoner.MaxMana).IsEqual(80f);
        AssertThat(result.Summoner.CastSpeed).IsEqual(1.25f);
    }

    [TestCase]
    public void BattleSideResolver_AuthoredEnemy_DoesNotUseLocalProfileStats()
    {
        var scene = CreateBattleScene();
        var repo = CreateProfileRepository("battle_scene_enemy_profile_bleed");
        repo.UnlockSummoner(SummonerIds.ManaTest);
        repo.UpdateProfileMeta(new MetaUpdate { SelectedSummoner = (string)SummonerIds.ManaTest });
        CreateSummonerSelection(repo);

        var staticDeck = new Godot.Collections.Array<Resource>();
        var starterCard = BattleSideResolver.CreateCardFromCatalog("fire_wisp");
        AssertThat(starterCard).IsNotNull();
        staticDeck.Add((Resource)starterCard!);

        var result = BattleSideResolver.Resolve(
            scene,
            new BattleSessionConfig { EnemySide = AuthoredEnemySide(hp: 300f) },
            localTeam: 1,
            sceneDefaultMaxHp: 300f,
            maxHandSize: 4,
            sceneFallbackDeck: staticDeck
        );

        AssertThat(result.Summoner.Hp).IsEqual(300f);
        AssertThat(result.Summoner.MaxHp).IsEqual(300f);
        AssertThat(result.Summoner.Mana).IsEqual(100f);
        AssertThat(result.Summoner.MaxMana).IsEqual(100f);
        AssertThat(result.Summoner.CastSpeed).IsEqual(1f);
        AssertThat(result.Summoner.DamageBonus).IsEqual(0f);
        AssertThat(result.Summoner.DamageReduction).IsEqual(0f);
        AssertThat(result.Summoner.SoulStrength).IsEqual(0f);
    }

    [TestCase]
    public void BattleSideResolver_EncounterSideWithDeferredDeck_StaysDeferred()
    {
        var scene = CreateBattleScene();
        var staticDeck = new Godot.Collections.Array<Resource>();
        var starterCard = BattleSideResolver.CreateCardFromCatalog("fire_wisp");
        AssertThat(starterCard).IsNotNull();
        staticDeck.Add((Resource)starterCard!);

        var result = BattleSideResolver.Resolve(
            scene,
            new BattleSessionConfig
            {
                EnemySide = AuthoredEnemySide(
                    hp: 300f,
                    deferredDeck: true,
                    controllerKind: BattleControllerKind.EncounterAi
                ),
            },
            localTeam: 1,
            sceneDefaultMaxHp: 300f,
            maxHandSize: 4,
            sceneFallbackDeck: staticDeck
        );

        AssertThat(result.Deck.IsDeferred).IsTrue();
        AssertThat(result.Deck.DeckCards).IsEmpty();
        AssertThat(result.Deck.HandCards).IsEmpty();
    }

    [TestCase]
    public void ConfigureEncounterAi_PreloadsEncounterSpawnCards()
    {
        var simNode = CreateSimulationNode();
        var config = EncounterAiConfig.ScriptedEncounter();
        config.Rules.Add(
            new EncounterRule
            {
                Kind = EncounterRuleKind.EventRule,
                Actions =
                [
                    new EncounterAction
                    {
                        Kind = EncounterActionKind.SpawnUnits,
                        Source = EncounterActionSource.Encounter,
                        CardId = "training_target",
                        Position = new SimVector3(10f, 0f, 2f),
                    },
                ],
            }
        );

        simNode.ConfigureEncounterAi(config);

        AssertThat(simNode.State.EncounterAi).IsEqual(config);
        AssertThat(simNode.State.CardDataMap.ContainsKey("training_target")).IsTrue();
        AssertThat(config.Rules[0].Actions[0].Position.HasValue).IsTrue();
    }

    [TestCase]
    public void InitSummonerHost_AppliesComputedCombatModifiersToSimulationState()
    {
        var scene = CreateBattleScene();
        var simNode = CreateSimulationNode();
        var summoner = new SummonerVisual
        {
            Name = $"SummonerVisualTest_{_createdNodes.Count}",
            Team = 1,
            MaxHpExport = 300f,
            MaxHandSize = 4,
            DeckLoadStrategy = DeckLoadStrategy.Static,
        };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(summoner);
        _createdNodes.Add(summoner);

        var starterCard = BattleSideResolver.CreateCardFromCatalog("fire_wisp");
        AssertThat(starterCard).IsNotNull();
        summoner.StartingDeck.Add((Resource)starterCard!);

        var opponentSummoner = new Godot.Collections.Dictionary
        {
            ["summoner_id"] = "summoner_teo",
            ["level"] = 1,
            ["xp"] = 0,
            ["acquired_trait_ids"] = new Godot.Collections.Array(),
            ["unspent_trait_points"] = 0,
        };

        var rawConfig = new Godot.Collections.Dictionary
        {
            ["opponent_summoner_data"] = opponentSummoner,
        };
        var config = new BattleSessionConfig
        {
            Mode = BattleMode.Multiplayer,
            IsMultiplayer = true,
            HasAuthority = true,
            RawConfig = rawConfig,
            EnemySide = new BattleSideDefinition
            {
                Team = 1,
                Source = BattleSideSource.MultiplayerOpponent,
                Summoner = new BattleSummonerDefinition
                {
                    Source = BattleSideSource.MultiplayerOpponent,
                },
                Deck = new BattleDeckDefinition { Source = BattleDeckSource.Authored },
                Controller = new BattleControllerDefinition { Kind = BattleControllerKind.Network },
            },
        };
        SetPrivateField(scene, "_config", config);

        // Build expected values from the same loader path to avoid brittle hard-coded trait numbers.
        var expected = BattleSideResolver.Resolve(
            scene,
            config,
            1,
            sceneDefaultMaxHp: summoner.MaxHpExport,
            maxHandSize: summoner.MaxHandSize,
            sceneFallbackDeck: summoner.StartingDeck
        );

        InvokePrivateMethod(scene, "InitSummonerHost", summoner, 1, simNode);

        var summonerState = simNode.State.Summoners[1];
        AssertThat(summonerState.DamageBonus).IsEqual(expected.Summoner.DamageBonus);
        AssertThat(summonerState.DamageReduction).IsEqual(expected.Summoner.DamageReduction);
        AssertThat(summonerState.SoulStrength).IsEqual(expected.Summoner.SoulStrength);

        var actualElementalBonuses = summonerState
            .EnumerateElementalDamageBonuses()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        AssertThat(actualElementalBonuses.Count)
            .IsEqual(expected.Summoner.ElementalDamageBonuses.Count);
        foreach (var kvp in expected.Summoner.ElementalDamageBonuses)
        {
            AssertThat(actualElementalBonuses.ContainsKey(kvp.Key)).IsTrue();
            AssertThat(actualElementalBonuses[kvp.Key]).IsEqual(kvp.Value);
        }
    }

    private BattleScene CreateBattleScene()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var scene = new BattleScene { Name = $"BattleSceneTest_{_createdNodes.Count}" };
        root.AddChild(scene);
        _createdNodes.Add(scene);

        SetPrivateField(scene, "_config", BattleSessionConfig.ForPractice());
        return scene;
    }

    private static BattleSideDefinition AuthoredEnemySide(
        float hp,
        float maxMana = 100f,
        float? mana = null,
        float castSpeed = 1f,
        bool deferredDeck = false,
        BattleControllerKind controllerKind = BattleControllerKind.TrainerAi
    ) =>
        new()
        {
            Team = 1,
            Source = BattleSideSource.Authored,
            Summoner = new BattleSummonerDefinition
            {
                Source = BattleSideSource.Authored,
                Id = "test_enemy",
                DisplayName = "Test Enemy",
                Hp = hp,
                MaxHp = hp,
                Mana = mana ?? maxMana,
                MaxMana = maxMana,
                CastSpeed = castSpeed,
            },
            Deck = new BattleDeckDefinition
            {
                Source = BattleDeckSource.Authored,
                Deferred = deferredDeck,
                Cards =
                [
                    new BattleDeckEntryDefinition
                    {
                        CatalogId = "fire_wisp",
                        Count = deferredDeck ? 0 : 1,
                    },
                ],
            },
            Controller = new BattleControllerDefinition
            {
                Kind = controllerKind,
                EncounterAi =
                    controllerKind == BattleControllerKind.EncounterAi
                        ? EncounterAiConfig.ScriptedEncounter()
                        : null,
            },
        };

    private SimulationNode CreateSimulationNode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var simNode = new SimulationNode { Name = $"SimulationNodeTest_{_createdNodes.Count}" };
        root.AddChild(simNode);
        _createdNodes.Add(simNode);
        return simNode;
    }

    private ProfileRepository CreateProfileRepository(string profileId)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;
        var repo = new ProfileRepository { Name = $"ProfileRepositoryTest_{_createdNodes.Count}" };
        root.AddChild(repo);
        _createdNodes.Add(repo);
        repo.LoadProfile(new ProfileId(profileId));
        repo.ResetProfile();
        return repo;
    }

    private SummonerSelectionService CreateSummonerSelection(ProfileRepository repo)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;
        var selection = new SummonerSelectionService { Name = "SummonerSelection" };
        root.AddChild(selection);
        _createdNodes.Add(selection);
        selection.InitForTesting(repo);
        return selection;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName, params object?[] args)
    {
        var method = target
            .GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(target, args);
    }
}
