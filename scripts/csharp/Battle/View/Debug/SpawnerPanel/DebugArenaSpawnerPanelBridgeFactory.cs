using Godot;

namespace Fateforged.View.Debug.SpawnerPanel;

public static class DebugArenaSpawnerPanelBridgeFactory
{
    private const string CandidateTypeName = "UnitSpawnerPanel";
    private const string CandidateProbeMethod = "get_skip_prep_phase";

    public static IDebugArenaSpawnerPanelBridge? TryCreate(Node owner)
    {
        var panelNode = FindPanelNode(owner);
        if (panelNode == null)
            return null;

        return new DebugArenaSpawnerPanelBridge(panelNode);
    }

    private static Node? FindPanelNode(Node owner)
    {
        var uiNodes = owner.GetTree().GetNodesInGroup("ui_layer");
        foreach (var node in uiNodes)
        {
            if (node.GetType().Name == CandidateTypeName || node.HasMethod(CandidateProbeMethod))
                return node;

            var found = FindChildWithProbeMethod(node, CandidateProbeMethod);
            if (found != null)
                return found;
        }

        foreach (var child in owner.GetChildren())
        {
            var found = FindChildWithProbeMethod(child, CandidateProbeMethod);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Node? FindChildWithProbeMethod(Node node, string method)
    {
        if (node.HasMethod(method))
            return node;

        foreach (var child in node.GetChildren())
        {
            var found = FindChildWithProbeMethod(child, method);
            if (found != null)
                return found;
        }

        return null;
    }
}
