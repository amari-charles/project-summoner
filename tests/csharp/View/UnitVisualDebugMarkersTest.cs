namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Infrastructure.Debug;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using Fateforged.Visual;
using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class UnitVisualDebugMarkersTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (var i = _createdNodes.Count - 1; i >= 0; i--)
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
    public void Process_HurtboxDebugFlag_CreatesAndQueuesMarkerForRemoval()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int unitId = 42;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            NavigationRadius = 0.9f,
            HurtboxRadius = 0.9f,
            AttackRange = 3f,
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);

        var markerField = typeof(UnitVisual).GetField(
            "_debugHurtboxMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(markerField).IsNotNull();

        debugService.HurtboxEnabled = true;
        visual._Process(1.0 / 60.0);

        var marker = markerField!.GetValue(visual) as MeshInstance3D;
        AssertThat(marker).IsNotNull();

        debugService.HurtboxEnabled = false;
        visual._Process(1.0 / 60.0);

        var markerAfterDisable = markerField.GetValue(visual) as MeshInstance3D;
        AssertThat(markerAfterDisable).IsNull();
        AssertThat(marker!.IsQueuedForDeletion()).IsTrue();
    }

    [TestCase]
    public void Process_EngageRangeAndDamageShape_UseIndependentDebugMarkers()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualRangeShapeDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int unitId = 77;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            AttackRange = 4f,
            Attack = new AttackVectorState
            {
                Selection = new AttackSelectionState
                {
                    Mode = AttackSelectionMode.LineCollect,
                    TargetLimit = 3,
                },
                Area = new AttackAreaState { LineLength = 6f, LineHalfWidth = 0.6f },
            },
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);

        var engageField = typeof(UnitVisual).GetField(
            "_debugEngageRangeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        var damageShapeField = typeof(UnitVisual).GetField(
            "_debugDamageShapeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(engageField).IsNotNull();
        AssertThat(damageShapeField).IsNotNull();

        debugService.EngageRangeEnabled = true;
        debugService.DamageShapeEnabled = false;
        visual._Process(1.0 / 60.0);

        var engageMarker = engageField!.GetValue(visual) as MeshInstance3D;
        var damageShapeMarker = damageShapeField!.GetValue(visual) as MeshInstance3D;
        AssertThat(engageMarker).IsNotNull();
        AssertThat(damageShapeMarker).IsNull();

        debugService.DamageShapeEnabled = true;
        visual._Process(1.0 / 60.0);

        damageShapeMarker = damageShapeField.GetValue(visual) as MeshInstance3D;
        AssertThat(damageShapeMarker).IsNotNull();
        AssertThat(damageShapeMarker!.Mesh is BoxMesh).IsTrue();
    }

    [TestCase]
    public void Process_ForwardRectEngageRange_RendersRectAndCloseBubbleMarkers()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualForwardRectEngageDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int unitId = 88;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            AttackRange = 3f,
            EngageShape = EngageShape.ForwardRect,
            EngageRectLength = 2.7f,
            EngageRectHalfWidth = 0.6f,
            EngageRectForwardOffset = 0.2f,
            EngageCloseRadius = 0.45f,
            IsFacingRight = true,
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);
        SetPrivateField(visual, "_isFacingRight", true);

        var primaryField = typeof(UnitVisual).GetField(
            "_debugEngageRangeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        var secondaryField = typeof(UnitVisual).GetField(
            "_debugEngageRangeSecondaryMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(primaryField).IsNotNull();
        AssertThat(secondaryField).IsNotNull();

        debugService.EngageRangeEnabled = true;
        visual._Process(1.0 / 60.0);

        var primary = primaryField!.GetValue(visual) as MeshInstance3D;
        var secondary = secondaryField!.GetValue(visual) as MeshInstance3D;
        AssertThat(primary).IsNotNull();
        AssertThat(primary!.Mesh is BoxMesh).IsTrue();
        AssertThat(secondary).IsNotNull();
        AssertThat(secondary!.Mesh is CylinderMesh).IsTrue();
    }

    [TestCase]
    public void DebugService_NavigationFootprintToggle_UsesCanonicalApi()
    {
        var debugService = new BattlefieldDebugService();

        debugService.SetDebugNavigationFootprintEnabled(true);
        AssertThat(debugService.NavigationFootprintEnabled).IsTrue();
        AssertThat(debugService.IsDebugNavigationFootprintEnabled()).IsTrue();

        debugService.ToggleDebugNavigationFootprint();
        AssertThat(debugService.NavigationFootprintEnabled).IsFalse();
        AssertThat(debugService.IsDebugNavigationFootprintEnabled()).IsFalse();

        debugService.SetDebugNavigationFootprintEnabled(true);
        AssertThat(debugService.NavigationFootprintEnabled).IsTrue();

        // This test does not add the service to the scene tree; free it explicitly
        // before framework orphan checks run.
        debugService.Free();
    }

    [TestCase]
    public void Process_DamageShapeSingleTarget_CentersDiscOnAttacker()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualSingleTargetShapeDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int attackerId = 101;
        const int targetId = 202;
        var state = new MatchState();
        state.Units[attackerId] = new UnitData
        {
            UnitId = attackerId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            Engagement = new CombatEngagementState { TargetUnitId = targetId },
            NavigationRadius = 0.8f,
            HurtboxRadius = 0.9f,
            AttackRange = 3f,
            Attack = new AttackVectorState
            {
                Selection = new AttackSelectionState
                {
                    Mode = AttackSelectionMode.Single,
                    TargetLimit = 1,
                },
                Area = new AttackAreaState { SingleTargetRadius = 1.4f },
            },
        };
        state.Units[targetId] = new UnitData
        {
            UnitId = targetId,
            IsAlive = true,
            Position = new SimVector3(2.5f, 0f, 0.6f),
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", attackerId);

        var damageShapeField = typeof(UnitVisual).GetField(
            "_debugDamageShapeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(damageShapeField).IsNotNull();

        debugService.DamageShapeEnabled = true;
        visual._Process(1.0 / 60.0);

        var marker = damageShapeField!.GetValue(visual) as MeshInstance3D;
        AssertThat(marker).IsNotNull();
        AssertThat(marker!.Mesh is CylinderMesh).IsTrue();
        var disc = marker.Mesh as CylinderMesh;
        AssertThat(disc).IsNotNull();
        AssertThat(Mathf.Abs(disc!.TopRadius - 1.4f) < 0.001f).IsTrue();
        AssertThat(Mathf.Abs(marker.Scale.Z - 0.62f) < 0.001f).IsTrue();
        AssertThat(Mathf.Abs(marker.GlobalPosition.X - visual.GlobalPosition.X) < 0.001f).IsTrue();
        AssertThat(Mathf.Abs(marker.GlobalPosition.Z - visual.GlobalPosition.Z) < 0.001f).IsTrue();
    }

    [TestCase]
    public void Process_DamageShapeSingleTarget_UsesSpriteWidthFallbackForRadius()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualSingleTargetSpriteWidthFallbackDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        var stubVisual = new StubVisualComponent(spriteWidth: 2.4f, spriteHeight: 2.0f);
        visual.AddChild(stubVisual);
        _createdNodes.Add(stubVisual);
        SetPrivateField(visual, "_visual", stubVisual);

        const int attackerId = 301;
        const int targetId = 302;
        var state = new MatchState();
        state.Units[attackerId] = new UnitData
        {
            UnitId = attackerId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            Engagement = new CombatEngagementState { TargetUnitId = targetId },
            NavigationRadius = 0.4f,
            HurtboxRadius = 0.5f,
            Attack = new AttackVectorState
            {
                Selection = new AttackSelectionState
                {
                    Mode = AttackSelectionMode.Single,
                    TargetLimit = 1,
                },
                Area = new AttackAreaState { SingleTargetRadius = 0f },
            },
        };
        state.Units[targetId] = new UnitData
        {
            UnitId = targetId,
            IsAlive = true,
            Position = new SimVector3(1.8f, 0f, 0.2f),
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", attackerId);

        var damageShapeField = typeof(UnitVisual).GetField(
            "_debugDamageShapeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(damageShapeField).IsNotNull();

        debugService.DamageShapeEnabled = true;
        visual._Process(1.0 / 60.0);

        var marker = damageShapeField!.GetValue(visual) as MeshInstance3D;
        AssertThat(marker).IsNotNull();
        var disc = marker!.Mesh as CylinderMesh;
        AssertThat(disc).IsNotNull();
        AssertThat(Mathf.Abs(disc!.TopRadius - 1.2f) < 0.001f).IsTrue();
        AssertThat(Mathf.Abs(marker.Scale.Z - 0.62f) < 0.001f).IsTrue();
    }

    [TestCase]
    public void Process_ForwardOffsetDamageShape_RemainsVisibleOutsideAttackWindow()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualForwardOffsetDamageShapeDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int unitId = 404;
        var unit = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            AttackRange = 3f,
            AttackPhase = AttackPhase.None,
            Attack = new AttackVectorState
            {
                Selection = new AttackSelectionState
                {
                    Mode = AttackSelectionMode.AreaCollect,
                    TargetLimit = 3,
                },
                Area = new AttackAreaState
                {
                    Shape = AttackAreaShape.Box,
                    Size = new SimVector3(5.4f, 1f, 2.6f),
                    ForwardOffset = 2.1f,
                },
            },
        };
        var state = new MatchState();
        state.Units[unitId] = unit;
        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);

        var primaryField = typeof(UnitVisual).GetField(
            "_debugDamageShapeMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        var secondaryField = typeof(UnitVisual).GetField(
            "_debugDamageShapeSecondaryMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(primaryField).IsNotNull();
        AssertThat(secondaryField).IsNotNull();

        debugService.DamageShapeEnabled = true;
        visual._Process(1.0 / 60.0);
        AssertThat(primaryField!.GetValue(visual) as MeshInstance3D).IsNotNull();
        AssertThat(secondaryField!.GetValue(visual) as MeshInstance3D).IsNotNull();

        unit.AttackPhase = AttackPhase.Windup;
        visual._Process(1.0 / 60.0);
        AssertThat(primaryField.GetValue(visual) as MeshInstance3D).IsNotNull();
        AssertThat(secondaryField.GetValue(visual) as MeshInstance3D).IsNotNull();
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }

    private sealed class StubSession : IGameSession
    {
        private readonly MatchState _state;

        public StubSession(MatchState state)
        {
            _state = state;
        }

        public event Action<IReadOnlyList<SimEvent>> SimEventsEmitted
        {
            add { }
            remove { }
        }

        public MatchState GetState() => _state;

        public void SubmitCommand(ICommand command) { }

        public void Tick(float delta) { }
    }

    private sealed partial class StubVisualComponent : Node3D, IVisualComponent
    {
        private readonly float _spriteWidth;
        private readonly float _spriteHeight;

        public StubVisualComponent(float spriteWidth, float spriteHeight)
        {
            _spriteWidth = spriteWidth;
            _spriteHeight = spriteHeight;
        }

        public void PlayAnimation(string animName) { }

        public void PlayAnimation(string animName, bool autoPlay) { }

        public void StopAnimation() { }

        public string GetCurrentAnimation() => "";

        public bool IsPlaying() => false;

        public void SetAnimationSpeed(float speed) { }

        public float GetAnimationDuration(string animName) => 0f;

        public float GetSpriteHeight() => _spriteHeight;

        public float GetSpriteWidth() => _spriteWidth;

        public float GetHpBarOffsetX() => 0f;

        public void FlashWhite() { }

        public void SetFlipH(bool flip) { }

        public void SetRenderPriority(int priority) { }

        public bool IsFullyInitialized() => true;

        public Node3D CreateGhostVisual() => new Node3D();

        public void ApplyGhostTint(Color tint) { }
    }
}
