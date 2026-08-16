namespace Fateforged.Tests.View;

using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.View.Debug;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class DebugSummonerMovementControllerTest
{
    [TestCase]
    public void MovementCalculation_UsesWasdDirectionAndBattlefieldBounds()
    {
        var bounds = new Rect2(
            BattlefieldBounds.MinX,
            BattlefieldBounds.MinZ,
            BattlefieldBounds.MaxX - BattlefieldBounds.MinX,
            BattlefieldBounds.MaxZ - BattlefieldBounds.MinZ
        );

        var movedUp = DebugSummonerMovementController.CalculateBoundedTarget(
            new SimVector3(0f, 0f, 0f),
            Vector2.Up,
            12f,
            1f,
            bounds
        );
        var clampedRight = DebugSummonerMovementController.CalculateBoundedTarget(
            new SimVector3(49f, 0f, 0f),
            Vector2.Right,
            12f,
            1f,
            bounds
        );

        AssertThat(movedUp).IsEqual(new SimVector3(0f, 0f, 12f));
        AssertThat(clampedRight.X).IsEqual(BattlefieldBounds.MaxX);
    }

    [TestCase]
    public void NormalBattle_ContainsMovementToggleThatDefaultsOff()
    {
        var packed = GD.Load<PackedScene>("res://scenes/battle/battlefield/battle_3d.tscn");
        AssertThat(packed).IsNotNull();

        using var scene = packed!.Instantiate<Node3D>();
        var controller = scene.GetNode<DebugSummonerMovementController>("DebugSummonerMovement");
        var toggle = scene.GetNode<CheckButton>(
            "UI/MovementTestPanel/VBox/MovementToggle"
        );

        AssertThat(controller.MovementEnabled).IsFalse();
        AssertThat(toggle.ButtonPressed).IsFalse();
    }

    [TestCase]
    public void MovementMode_ReservesWasdAndLeavesEdgePanForCamera()
    {
        var camera = new Camera3D();
        try
        {
            camera.SetScript(
                GD.Load<Script>(
                    "res://scripts/battle/battlefield/camera_controller_3d.gd"
                )
            );
            camera.Set("keyboard_pan_enabled", true);
            camera.Set("edge_pan_enabled", false);

            DebugSummonerMovementController.ApplyCameraInputMode(
                camera,
                keyboardPanEnabled: false,
                edgePanEnabled: true
            );

            AssertThat((bool)camera.Get("keyboard_pan_enabled")).IsFalse();
            AssertThat((bool)camera.Get("edge_pan_enabled")).IsTrue();
        }
        finally
        {
            camera.Free();
        }
    }
}
