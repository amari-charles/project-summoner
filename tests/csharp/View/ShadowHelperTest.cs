namespace Fateforged.Tests.View;

using System.Collections.Generic;
using Fateforged.Visual;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class ShadowHelperTest
{
    private readonly List<Node> _createdNodes = [];

    private static Node GetRootNode()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        return tree.Root;
    }

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
    public void CreateShadow_AppliesProvidedProfileValues()
    {
        var root = GetRootNode();
        var parentSprite = new Sprite3D
        {
            PixelSize = 0.02f,
            Position = new Vector3(0, 1.4f, 0),
        };
        root.AddChild(parentSprite);
        _createdNodes.Add(parentSprite);

        var viewport = new SubViewport { Size = new Vector2I(64, 64) };
        root.AddChild(viewport);
        _createdNodes.Add(viewport);

        var profile = new ShadowProfile(
            Opacity: 0.33f,
            SkewX: 1.2f,
            SkewY: 0.4f,
            OffsetX: 0.31f,
            OffsetZ: 0.41f,
            GroundClearance: 0.02f,
            ScaleY: 1.25f,
            RenderPriority: -77
        );

        var shadow = ShadowHelper.CreateShadow(parentSprite, viewport, profile);
        if (shadow != null)
        {
            parentSprite.AddChild(shadow);
            _createdNodes.Add(shadow);
        }

        AssertThat(shadow).IsNotNull();
        AssertThat(shadow!.PixelSize).IsEqual(parentSprite.PixelSize);
        AssertThat(shadow.Position.X).IsEqual(profile.OffsetX);
        AssertThat(shadow.Position.Y).IsEqual(profile.GroundClearance);
        AssertThat(shadow.Position.Z).IsEqual(parentSprite.Position.Y + profile.OffsetZ);
        AssertThat(shadow.Scale.Y).IsEqual(profile.ScaleY);
        AssertThat(shadow.RenderPriority).IsEqual(profile.RenderPriority);
    }

    [TestCase]
    public void PinToGround_UsesProfileGroundClearance()
    {
        var root = GetRootNode();
        var shadow = new Sprite3D();
        root.AddChild(shadow);
        _createdNodes.Add(shadow);
        var profile = new ShadowProfile(
            Opacity: 0.5f,
            SkewX: 0.8f,
            SkewY: 0.6f,
            OffsetX: 0.2f,
            OffsetZ: 0.2f,
            GroundClearance: 0.015f,
            ScaleY: 1.1f,
            RenderPriority: -100
        );

        ShadowHelper.PinToGround(shadow, 2.0f, profile);

        AssertThat(shadow.Position.Y).IsEqualApprox(-1.985f, 0.0001f);
    }

    [TestCase]
    public void ShadowProfilePreset_Default_IsStable()
    {
        ShadowProfile profile = ShadowProfiles.FromPreset(ShadowProfilePreset.Default);
        AssertThat(profile.Opacity).IsEqual(0.5f);
        AssertThat(profile.SkewX).IsEqual(0.8f);
        AssertThat(profile.SkewY).IsEqual(0.6f);
        AssertThat(profile.OffsetX).IsEqual(0.2f);
        AssertThat(profile.OffsetZ).IsEqual(0.2f);
    }
}
