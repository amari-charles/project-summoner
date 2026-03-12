namespace Fateforged.Tests.View;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fateforged.Cards;
using Fateforged.Session;
using Fateforged.Simulation;
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
    public void LoadSummonerData_OpeningHandContainsOnlySummons()
    {
        var scene = CreateBattleScene();
        var staticDeck = new Godot.Collections.Array<Resource>();

        var spell = BattleSessionFactory.CreateCardFromCatalog("mana_bolt");
        var summonA = BattleSessionFactory.CreateCardFromCatalog("fire_wisp");
        var summonB = BattleSessionFactory.CreateCardFromCatalog("water_wisp");
        AssertThat(spell).IsNotNull();
        AssertThat(summonA).IsNotNull();
        AssertThat(summonB).IsNotNull();

        staticDeck.Add((Resource)spell!);
        staticDeck.Add((Resource)summonA!);
        staticDeck.Add((Resource)summonB!);

        var result = BattleSessionFactory.LoadSummonerData(
            scene,
            BattleSessionConfig.ForPractice(),
            localTeam: 0,
            deckLoadStrategy: DeckLoadStrategy.Static,
            defaultMaxHp: 100f,
            maxHandSize: 2,
            staticDeck: staticDeck
        );

        AssertThat(result.Hand.Count).IsEqual(2);
        var handTypes = result.Hand.OfType<Card>().Select(card => card.Type).ToList();
        AssertThat(handTypes.Count).IsEqual(2);
        AssertThat(handTypes.All(type => type == (int)CardType.Summon)).IsTrue();

        AssertThat(result.Deck.Count).IsEqual(1);
        var remainingCard = result.Deck[0] as Card;
        AssertThat(remainingCard).IsNotNull();
        AssertThat(remainingCard!.Type).IsEqual((int)CardType.Spell);
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

        var starterCard = BattleSessionFactory.CreateCardFromCatalog("fire_wisp");
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
        };
        SetPrivateField(scene, "_config", config);

        // Build expected values from the same loader path to avoid brittle hard-coded trait numbers.
        var expected = BattleSessionFactory.LoadSummonerData(
            scene,
            config,
            1,
            summoner.DeckLoadStrategy,
            summoner.MaxHpExport,
            summoner.MaxHandSize,
            summoner.StartingDeck
        );

        InvokePrivateMethod(scene, "InitSummonerHost", summoner, 1, simNode);

        var summonerState = simNode.State.Summoners[1];
        AssertThat(summonerState.DamageBonus).IsEqual(expected.DamageBonus);
        AssertThat(summonerState.DamageReduction).IsEqual(expected.DamageReduction);
        AssertThat(summonerState.SoulStrength).IsEqual(expected.SoulStrength);

        var actualElementalBonuses = summonerState
            .EnumerateElementalDamageBonuses()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        AssertThat(actualElementalBonuses.Count).IsEqual(expected.ElementalDamageBonuses.Count);
        foreach (var (element, bonus) in expected.ElementalDamageBonuses)
        {
            AssertThat(actualElementalBonuses.ContainsKey(element)).IsTrue();
            AssertThat(actualElementalBonuses[element]).IsEqual(bonus);
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

    private SimulationNode CreateSimulationNode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var simNode = new SimulationNode { Name = $"SimulationNodeTest_{_createdNodes.Count}" };
        root.AddChild(simNode);
        _createdNodes.Add(simNode);
        return simNode;
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
