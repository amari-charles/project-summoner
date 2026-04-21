namespace Fateforged.Tests.View;

using System.Collections.Generic;
using Fateforged.View.Debug.SpawnerPanel;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class DebugArenaSpawnerPanelBridgeFactoryTest
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
    public void TryCreate_UsesUiLayerProbeFallback_WhenTypedPanelNotPresent()
    {
        var owner = AddRootNode(new Node { Name = "OwnerNode" });
        var uiLayer = AddRootNode(new Node { Name = "UiLayerNode" });
        uiLayer.AddToGroup("ui_layer");
        var legacyPanel = new ProbePanelStub { Name = "LegacyProbePanel" };
        uiLayer.AddChild(legacyPanel);
        _createdNodes.Add(legacyPanel);

        var bridge = DebugArenaSpawnerPanelBridgeFactory.TryCreate(owner);

        AssertThat(bridge).IsNotNull();
        AssertThat(bridge!.PanelNode).IsEqual(legacyPanel);
    }

    [TestCase]
    public void TryCreate_FallsBackToOwnerChildren_WhenUiLayerMissing()
    {
        var owner = AddRootNode(new Node { Name = "OwnerNode" });
        var child = new Node { Name = "ChildContainer" };
        owner.AddChild(child);
        _createdNodes.Add(child);

        var legacyPanel = new ProbePanelStub { Name = "LegacyProbePanel" };
        child.AddChild(legacyPanel);
        _createdNodes.Add(legacyPanel);

        var bridge = DebugArenaSpawnerPanelBridgeFactory.TryCreate(owner);

        AssertThat(bridge).IsNotNull();
        AssertThat(bridge!.PanelNode).IsEqual(legacyPanel);
    }

    [TestCase]
    public void TryCreate_ReturnsNull_WhenNoProbePanelExists()
    {
        var owner = AddRootNode(new Node { Name = "OwnerNode" });
        var child = new Node { Name = "ChildContainer" };
        owner.AddChild(child);
        _createdNodes.Add(child);

        var bridge = DebugArenaSpawnerPanelBridgeFactory.TryCreate(owner);

        AssertThat(bridge).IsNull();
    }

    private Node AddRootNode(Node node)
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }

    private sealed partial class ProbePanelStub : Node
    {
        public bool get_skip_prep_phase() => false;
    }
}
