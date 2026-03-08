namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Session;
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
    public void EmitPhaseIfChanged_DedupesRepeatedPhaseSignals()
    {
        var scene = CreateBattleScene();
        var phases = new List<int>();
        scene.Connect(BattleScene.SignalName.PhaseChanged, Callable.From<int>(phase => phases.Add(phase)));

        InvokePrivateMethod(scene, "EmitPhaseIfChanged", (int)BattleScene.BattlePhase.Preparation);
        InvokePrivateMethod(scene, "EmitPhaseIfChanged", (int)BattleScene.BattlePhase.Preparation);
        InvokePrivateMethod(scene, "EmitPhaseIfChanged", (int)BattleScene.BattlePhase.Battle);

        AssertThat(phases.Count).IsEqual(2);
        AssertThat(phases[0]).IsEqual((int)BattleScene.BattlePhase.Preparation);
        AssertThat(phases[1]).IsEqual((int)BattleScene.BattlePhase.Battle);
    }

    [TestCase]
    public void ToUiPhaseValue_MapsSimulationPhasesToUiPhases()
    {
        int prepUi = (int)InvokePrivateStaticMethod(typeof(BattleScene), "ToUiPhaseValue", GamePhase.Preparation);
        int battleUi = (int)InvokePrivateStaticMethod(typeof(BattleScene), "ToUiPhaseValue", GamePhase.Battle);
        int gameOverUi = (int)InvokePrivateStaticMethod(typeof(BattleScene), "ToUiPhaseValue", GamePhase.GameOver);

        AssertThat(prepUi).IsEqual((int)BattleScene.BattlePhase.Preparation);
        AssertThat(battleUi).IsEqual((int)BattleScene.BattlePhase.Battle);
        AssertThat(gameOverUi).IsEqual((int)BattleScene.BattlePhase.Battle);
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static object? InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        return method!.Invoke(target, args);
    }

    private static object? InvokePrivateStaticMethod(Type targetType, string methodName, params object[] args)
    {
        var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        return method!.Invoke(null, args);
    }
}
