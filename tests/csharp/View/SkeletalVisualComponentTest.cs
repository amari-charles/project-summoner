namespace Fateforged.Tests.View;

using System.Collections.Generic;
using Fateforged.Visual;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class SkeletalVisualComponentTest
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
    public void SetCombatTilt_RotatesBody_AndUsesManualBillboardMode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var scene = GD.Load<PackedScene>(
            "res://scenes/battle/units/skeletal_character_2d5_component.tscn"
        );
        var visual = scene.Instantiate<SkeletalVisualComponent>();
        root.AddChild(visual);
        _createdNodes.Add(visual);

        var body = visual.GetNode<Sprite3D>("Sprite3D");

        AssertThat(body.Billboard).IsEqual(BaseMaterial3D.BillboardModeEnum.Disabled);

        visual.SetCombatTilt(8f, 6f, 4f);
        visual._Process(1.0 / 60.0);

        AssertThat(Mathf.Abs(body.RotationDegrees.X)).IsGreater(0.01f);
        AssertThat(Mathf.Abs(body.RotationDegrees.Y)).IsGreater(0.01f);
        AssertThat(Mathf.Abs(body.RotationDegrees.Z)).IsGreater(0.01f);
    }
}
