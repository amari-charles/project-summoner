namespace Fateforged.Tests.View;

using Fateforged.Simulation;
using Fateforged.View.Debug;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CompactRuinSkirmishSceneTest
{
    [TestCase]
    public void MovementCalculation_UsesWasdScreenDirectionAndRoomBounds()
    {
        var bounds = new Rect2(-40f, -22f, 80f, 44f);

        var movedUp = CompactRuinSkirmishScene.CalculateBoundedTarget(
            new SimVector3(0f, 0f, 0f),
            new Vector2(0f, -1f),
            12f,
            1f,
            bounds
        );
        var clampedRight = CompactRuinSkirmishScene.CalculateBoundedTarget(
            new SimVector3(35f, 0f, 0f),
            Vector2.Right,
            12f,
            1f,
            bounds
        );

        AssertThat(movedUp).IsEqual(new SimVector3(0f, 0f, 12f));
        AssertThat(clampedRight).IsEqual(new SimVector3(40f, 0f, 0f));
    }

    [TestCase]
    public void Scene_ContainsCompactGreyboxFixedCameraAndMovementToggle()
    {
        var packed = GD.Load<PackedScene>(
            "res://scenes/battle/battlefield/dev/compact_ruin_skirmish.tscn"
        );
        AssertThat(packed).IsNotNull();

        var scene = packed!.Instantiate<Node3D>();
        try
        {
            var room = scene.GetNode<Node3D>("Battlefield3D");
            var camera = room.GetNode<Camera3D>("Camera3D");
            var cameraProfile = (Resource)camera.Get("perspective_camera_profile");
            var background = room.GetNode<MeshInstance3D>("Background");
            var ruinCore = scene.GetNode<Node3D>("RuinCore");
            var handUi = scene.GetNode<Control>("UI/HandUI");
            var toggle = scene.GetNode<CheckButton>(
                "UI/MovementPanel/VBox/MovementToggle"
            );

            AssertThat((bool)camera.Get("keyboard_pan_enabled")).IsFalse();
            AssertThat((bool)camera.Get("mouse_pan_enabled")).IsFalse();
            AssertThat((bool)camera.Get("touch_pan_enabled")).IsFalse();
            AssertThat((bool)camera.Get("zoom_enabled")).IsTrue();
            AssertThat((bool)camera.Get("edge_pan_enabled")).IsFalse();
            AssertThat((bool)camera.Get("zoom_respects_map_bounds")).IsFalse();
            AssertThat((float)cameraProfile.Get("min_zoom")).IsEqual(32f);
            AssertThat((float)cameraProfile.Get("max_zoom")).IsEqual(58f);
            AssertThat(camera.Position).IsEqual(new Vector3(0f, 56f, -89f));
            AssertThat((bool)room.Get("apply_biome_from_context")).IsFalse();
            AssertThat(((PlaneMesh)background.Mesh).Size).IsEqual(new Vector2(84f, 48f));
            AssertThat(room.HasNode("GroundLayer/NorthWall")).IsFalse();
            AssertThat(room.HasNode("GroundLayer/SouthWall")).IsFalse();
            AssertThat(room.HasNode("GroundLayer/WestWall")).IsFalse();
            AssertThat(room.HasNode("GroundLayer/EastWall")).IsFalse();
            AssertThat(scene.HasNode("EnemySummoner")).IsFalse();
            AssertThat(ruinCore.HasNode("Pedestal")).IsTrue();
            AssertThat(ruinCore.HasNode("Core")).IsTrue();
            AssertThat(handUi).IsNotNull();
            AssertThat(toggle.ButtonPressed).IsTrue();
        }
        finally
        {
            scene.Free();
        }
    }
}
