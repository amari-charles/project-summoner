using Godot;
using ProjectSummoner.Constants;
using Fateforged.View;

namespace Fateforged.View.Debug;

/// <summary>
/// Debug Arena — infinite mana/HP, empty enemy deck, passive AI, manual unit spawning.
/// Replaces debug_arena_controller.gd. Used by scenes/battlefield/dev/debug_arena.tscn.
/// </summary>
[GlobalClass]
public partial class DebugArenaScene : TestBattleScene
{
    [Signal] public delegate void UnitsClearedEventHandler(int count);

    private Node? _spawnerPanel;

    public override async void _Ready()
    {
        // Configure BattleContext for debug arena before parent init
        var battleContext = GetNode("/root/BattleContext");
        if (battleContext != null)
        {
            var config = new Godot.Collections.Dictionary
            {
                { "enemy_deck", new Godot.Collections.Array() }, // Empty = no AI spawning
                { "enemy_hp", 999999.0 },
                { "ai_type", "passive" }
            };
            battleContext.Call("configure_practice_battle", config);
        }

        base._Ready();

        // Wait one frame for init to complete, then connect spawner panel
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ConnectSpawnerPanel();

        // Check if we should skip prep phase
        if (_spawnerPanel != null && (bool)_spawnerPanel.Call("get_skip_prep_phase"))
            SkipPrepPhase();
    }

    private void ConnectSpawnerPanel()
    {
        _spawnerPanel = FindSpawnerPanel();
        if (_spawnerPanel == null) return;

        if (!_spawnerPanel.IsConnected("clear_requested", new Callable(this, MethodName.ClearAllUnits)))
            _spawnerPanel.Connect("clear_requested", new Callable(this, MethodName.ClearAllUnits));

        if (!_spawnerPanel.IsConnected("skip_prep_toggled", new Callable(this, MethodName.OnSkipPrepToggled)))
            _spawnerPanel.Connect("skip_prep_toggled", new Callable(this, MethodName.OnSkipPrepToggled));
    }

    private Node? FindSpawnerPanel()
    {
        // Search in ui_layer group
        var uiNodes = GetTree().GetNodesInGroup("ui_layer");
        foreach (var node in uiNodes)
        {
            if (node.GetType().Name == "UnitSpawnerPanel" || node.HasMethod("get_skip_prep_phase"))
                return node;
            var found = FindChildWithMethod(node, "get_skip_prep_phase");
            if (found != null) return found;
        }

        // Search direct children
        foreach (var child in GetChildren())
        {
            var found = FindChildWithMethod(child, "get_skip_prep_phase");
            if (found != null) return found;
        }

        return null;
    }

    private static Node? FindChildWithMethod(Node node, string method)
    {
        if (node.HasMethod(method)) return node;
        foreach (var child in node.GetChildren())
        {
            var found = FindChildWithMethod(child, method);
            if (found != null) return found;
        }
        return null;
    }

    public void OnSkipPrepToggled(bool skip)
    {
        if (skip)
            SkipPrepPhase();
    }

    public void ClearAllUnits()
    {
        var units = GetTree().GetNodesInGroup(GroupIDs.Units);
        int count = units.Count;

        foreach (var unit in units)
        {
            if (IsInstanceValid(unit))
                unit.QueueFree();
        }

        EmitSignal(SignalName.UnitsCleared, count);
    }
}
