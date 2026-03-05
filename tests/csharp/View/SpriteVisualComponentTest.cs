namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using Fateforged.Visual;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class SpriteVisualComponentTest
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
    public void SetFlipH_MirrorsViewportPivot_AndKeepsWorldAnchorCentered()
    {
        var visual = CreateVisual();
        visual.AnimationPivotOffsets["idle"] = new Vector2(-40f, 0f);
        visual.SetSpriteFrames(CreateFrames([(128, 128)]));

        var sprite3D = visual.GetNode<Sprite3D>("Sprite3D");
        var viewport = visual.GetNode<SubViewport>("Sprite3D/SubViewport");
        var sprite = visual.GetNode<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");
        sprite.Animation = "idle";

        visual.SetFlipH(false);
        visual._Process(1.0 / 60.0);
        var dxUnflipped = sprite.Position.X - (viewport.Size.X / 2.0f);
        var worldXUnflipped = sprite3D.Position.X;

        visual.SetFlipH(true);
        visual._Process(1.0 / 60.0);
        var dxFlipped = sprite.Position.X - (viewport.Size.X / 2.0f);
        var worldXFlipped = sprite3D.Position.X;

        AssertThat(worldXUnflipped).IsEqual(0f);
        AssertThat(worldXFlipped).IsEqual(0f);
        AssertThat(dxFlipped).IsEqualApprox(-dxUnflipped, 0.01f);
    }

    [TestCase]
    public void Process_FrameChange_ReappliesAlignmentUsingCurrentFrameSize()
    {
        var visual = CreateVisual();
        visual.SetSpriteFrames(CreateFrames([(128, 100), (128, 220)]));

        var sprite = visual.GetNode<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");
        sprite.Animation = "idle";

        sprite.Frame = 0;
        visual._Process(1.0 / 60.0);
        var yFrame0 = sprite.Position.Y;

        sprite.Frame = 1;
        visual._Process(1.0 / 60.0);
        var yFrame1 = sprite.Position.Y;

        AssertThat(yFrame1).IsNotEqual(yFrame0);
    }

    [TestCase]
    public void Process_ScaleChange_ReappliesPivotOffset()
    {
        var visual = CreateVisual();
        visual.AnimationPivotOffsets["idle"] = new Vector2(-30f, 0f);
        visual.SetSpriteFrames(CreateFrames([(128, 128)]));

        var viewport = visual.GetNode<SubViewport>("Sprite3D/SubViewport");
        var sprite = visual.GetNode<AnimatedSprite2D>("Sprite3D/SubViewport/Model2D/CharacterSprite");
        sprite.Animation = "idle";
        sprite.Scale = Vector2.One;

        visual._Process(1.0 / 60.0);
        var centerX = viewport.Size.X / 2.0f;
        var dxScale1 = sprite.Position.X - centerX;

        sprite.Scale = new Vector2(2f, 2f);
        visual._Process(1.0 / 60.0);
        var dxScale2 = sprite.Position.X - centerX;

        AssertThat(dxScale2).IsEqualApprox(dxScale1 * 2f, 0.01f);
    }

    private SpriteVisualComponent CreateVisual()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;
        var scene = GD.Load<PackedScene>("res://scenes/battle/units/sprite_character_2d5_component.tscn");
        var visual = scene.Instantiate<SpriteVisualComponent>();
        root.AddChild(visual);
        _createdNodes.Add(visual);
        return visual;
    }

    private static SpriteFrames CreateFrames((int width, int height)[] sizes)
    {
        var frames = new SpriteFrames();
        frames.AddAnimation("idle");

        foreach (var (width, height) in sizes)
        {
            frames.AddFrame("idle", CreateTexture(width, height));
        }

        return frames;
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        return ImageTexture.CreateFromImage(image);
    }
}
