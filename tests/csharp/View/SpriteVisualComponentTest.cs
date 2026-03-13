namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using Fateforged.View;
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
        var sprite = visual.GetNode<AnimatedSprite2D>(
            "Sprite3D/SubViewport/Model2D/CharacterSprite"
        );
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

        var sprite = visual.GetNode<AnimatedSprite2D>(
            "Sprite3D/SubViewport/Model2D/CharacterSprite"
        );
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
        var sprite = visual.GetNode<AnimatedSprite2D>(
            "Sprite3D/SubViewport/Model2D/CharacterSprite"
        );
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

    [TestCase]
    public void SceneConfiguration_UsesAlphaCutDiscard_ForStableDepthOcclusion()
    {
        int spriteAlphaCut = GetSpriteAlphaCut(
            "res://scenes/battle/units/sprite_character_2d5_component.tscn"
        );
        int skeletalAlphaCut = GetSpriteAlphaCut(
            "res://scenes/battle/units/skeletal_character_2d5_component.tscn"
        );

        AssertThat(spriteAlphaCut).IsEqual(2);
        AssertThat(skeletalAlphaCut).IsEqual(2);
    }

    [TestCase]
    public void Ready_CreatesShadow_WhenUnitVisualIsAncestorThroughWrapper()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var unitVisual = new UnitVisual();
        var wrapper = new Node3D();
        var visual = InstantiateVisualScene();

        root.AddChild(unitVisual);
        _createdNodes.Add(unitVisual);
        unitVisual.AddChild(wrapper);
        wrapper.AddChild(visual);

        int sprite3DCount = CountDirectChildSprite3Ds(visual);
        AssertThat(sprite3DCount).IsEqual(2);
    }

    [TestCase]
    public void SetCombatTilt_RotatesBodyOnly_AndKeepsShadowGroundPlane()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var unitVisual = new UnitVisual();
        var wrapper = new Node3D();
        var visual = InstantiateVisualScene();

        root.AddChild(unitVisual);
        _createdNodes.Add(unitVisual);
        unitVisual.AddChild(wrapper);
        wrapper.AddChild(visual);

        var body = visual.GetNode<Sprite3D>("Sprite3D");
        var shadow = FindShadowSprite(visual, body);

        AssertThat(body.Billboard).IsEqual(BaseMaterial3D.BillboardModeEnum.Disabled);

        visual.SetCombatTilt(8f, 6f, 4f);
        visual._Process(1.0 / 60.0);

        AssertThat(Mathf.Abs(body.RotationDegrees.X)).IsGreater(0.01f);
        AssertThat(Mathf.Abs(body.RotationDegrees.Y)).IsGreater(0.01f);
        AssertThat(Mathf.Abs(body.RotationDegrees.Z)).IsGreater(0.01f);
        AssertThat(shadow).IsNotNull();
        AssertThat(shadow!.RotationDegrees.X).IsEqualApprox(-90f, 0.01f);
        AssertThat(Mathf.Abs(shadow.RotationDegrees.Y)).IsLessEqual(0.01f);
        AssertThat(Mathf.Abs(shadow.RotationDegrees.Z)).IsLessEqual(0.01f);
    }

    private SpriteVisualComponent CreateVisual()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;
        var visual = InstantiateVisualScene();
        root.AddChild(visual);
        _createdNodes.Add(visual);
        return visual;
    }

    private static SpriteVisualComponent InstantiateVisualScene()
    {
        var scene = GD.Load<PackedScene>(
            "res://scenes/battle/units/sprite_character_2d5_component.tscn"
        );
        return scene.Instantiate<SpriteVisualComponent>();
    }

    private static int GetSpriteAlphaCut(string scenePath)
    {
        var scene = GD.Load<PackedScene>(scenePath);
        var root = scene.Instantiate<Node3D>();
        var sprite3D = root.GetNode<Sprite3D>("Sprite3D");
        int alphaCut = sprite3D.Get("alpha_cut").AsInt32();
        root.Free();
        return alphaCut;
    }

    private static int CountDirectChildSprite3Ds(Node node)
    {
        int count = 0;
        foreach (Node child in node.GetChildren())
        {
            if (child is Sprite3D)
                count++;
        }

        return count;
    }

    private static Sprite3D? FindShadowSprite(Node node, Sprite3D bodySprite)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Sprite3D sprite && sprite != bodySprite)
                return sprite;
        }

        return null;
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
