namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.UI;
using Fateforged.Units;
using Fateforged.View;
using Fateforged.Visual;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class UnitVisualStateSyncTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
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
    public void PhysicsProcess_InactiveUnit_ForcesIdleAnimation()
    {
        const int unitId = 100;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(1f, 0f, 2f),
            ActivationState = ActivationState.Inactive,
            BehaviorState = BehaviorState.Chasing,
        };

        var visual = CreateUnitVisualWithState(state, unitId, out var fakeVisual);

        visual._PhysicsProcess(1.0 / 60.0);

        AssertThat(fakeVisual.LastAnimation).IsEqual("idle");
    }

    [TestCase]
    public void PhysicsProcess_InactiveUnit_DoesNotPlayAttackAnimation()
    {
        const int unitId = 101;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            ActivationState = ActivationState.Inactive,
            AttackAnimationTimer = 0.4f,
            BehaviorState = BehaviorState.Attacking,
        };

        var visual = CreateUnitVisualWithState(state, unitId, out var fakeVisual);

        visual._PhysicsProcess(1.0 / 60.0);

        AssertThat(fakeVisual.LastAnimation).IsEqual("idle");
        AssertThat(fakeVisual.LastAnimation).IsNotEqual("attack");
    }

    [TestCase]
    public void BeginDeath_SnapsHpBarToZeroImmediately()
    {
        const int unitId = 102;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            ActivationState = ActivationState.Active,
            CurrentHp = 50f,
            MaxHp = 100f,
        };

        var visual = CreateUnitVisualWithState(state, unitId, out _);
        var hpBar = new FloatingHPBar();
        _createdNodes.Add(hpBar);
        SetPrivateField(visual, "_hpBar", hpBar);

        visual._PhysicsProcess(1.0 / 60.0);
        visual.BeginDeath();

        float target = GetPrivateField<float>(hpBar, "_targetHpPercent");
        float display = GetPrivateField<float>(hpBar, "_displayHpPercent");
        AssertThat(target).IsEqual(0f);
        AssertThat(display).IsEqual(0f);
        AssertThat(visual.CurrentHp).IsEqual(0f);
    }

    private UnitVisual CreateUnitVisualWithState(
        MatchState state,
        int unitId,
        out FakeVisualComponent fakeVisual
    )
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var simNode = new SimulationNode { Name = $"SimulationNode_{_createdNodes.Count}" };
        root.AddChild(simNode);
        _createdNodes.Add(simNode);

        var visual = new UnitVisual { Name = $"UnitVisual_{_createdNodes.Count}" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        fakeVisual = new FakeVisualComponent();
        visual.AddChild(fakeVisual);
        _createdNodes.Add(fakeVisual);
        SetPrivateField(visual, "_visual", fakeVisual);
        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);
        SetPrivateField(visual, "_isAlive", true);
        return visual;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }

    private sealed class StubSession : IGameSession
    {
        private readonly MatchState _state;

        public StubSession(MatchState state)
        {
            _state = state;
        }

        public MatchState GetState() => _state;

        public event Action<IReadOnlyList<SimEvent>> SimEventsEmitted
        {
            add { }
            remove { }
        }

        public void SubmitCommand(ICommand command) { }

        public void Tick(float delta) { }
    }

    private sealed partial class FakeVisualComponent : Node3D, IVisualComponent
    {
        public string LastAnimation { get; private set; } = "";

        public void PlayAnimation(string animName)
        {
            LastAnimation = animName;
        }

        public void PlayAnimation(string animName, bool autoPlay)
        {
            LastAnimation = animName;
        }

        public void StopAnimation() { }

        public string GetCurrentAnimation() => LastAnimation;

        public bool IsPlaying() => false;

        public void SetAnimationSpeed(float speed) { }

        public float GetAnimationDuration(string animName) => 0.25f;

        public float GetSpriteHeight() => 2.0f;

        public float GetSpriteWidth() => 1.0f;

        public float GetHpBarOffsetX() => 0.0f;

        public void FlashWhite() { }

        public void SetFlipH(bool flip) { }

        public void SetRenderPriority(int priority) { }

        public bool IsFullyInitialized() => true;

        public Node3D CreateGhostVisual() => new Node3D();

        public void ApplyGhostTint(Color tint) { }
    }
}
