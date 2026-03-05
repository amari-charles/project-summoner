using Godot;
using Fateforged.Constants;
using Fateforged.View;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;

namespace Fateforged.View.Debug;

/// <summary>
/// Debug Arena — infinite mana/HP, manual unit spawning, enemy AI toggle.
/// Replaces debug_arena_controller.gd. Used by scenes/battle/battlefield/dev/debug_arena.tscn.
/// </summary>
[GlobalClass]
public partial class DebugArenaScene : TestBattleScene
{
    [Signal] public delegate void UnitsClearedEventHandler(int count);
    private const string DebugDeckPath = "res://data/debug/debug_deck.json";

    private Node? _spawnerPanel;

    protected override Godot.Collections.Dictionary BuildPracticeConfig()
    {
        var playerDeck = LoadDebugDeck();
        var enemyDeck = (Godot.Collections.Array)playerDeck.Duplicate(true);

        return new Godot.Collections.Dictionary
        {
            { "dev_player_deck", playerDeck },
            { "enemy_deck", enemyDeck },
            { "enemy_hp", 999999.0 },
            { "ai_type", "none" }
        };
    }

    private static Godot.Collections.Array LoadDebugDeck()
    {
        if (!FileAccess.FileExists(DebugDeckPath))
        {
            GD.PushWarning($"[DebugArenaScene] Debug deck not found at {DebugDeckPath}, using fallback deck");
            return BuildDeck("fire_wisp", 30);
        }

        using var file = FileAccess.Open(DebugDeckPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning("[DebugArenaScene] Failed to open debug deck file, using fallback deck");
            return BuildDeck("fire_wisp", 30);
        }

        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType == Variant.Type.Array)
        {
            var deck = parsed.AsGodotArray();
            if (deck.Count > 0)
                return deck;
        }

        GD.PushWarning("[DebugArenaScene] Debug deck JSON invalid/empty, using fallback deck");
        return BuildDeck("fire_wisp", 30);
    }

    public override async void _Ready()
    {
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

        if (_spawnerPanel.HasSignal("enemy_ai_toggled") &&
            !_spawnerPanel.IsConnected("enemy_ai_toggled", new Callable(this, MethodName.OnEnemyAiToggled)))
        {
            _spawnerPanel.Connect("enemy_ai_toggled", new Callable(this, MethodName.OnEnemyAiToggled));
        }

        if (_spawnerPanel.HasMethod("get_enemy_ai_enabled"))
            OnEnemyAiToggled((bool)_spawnerPanel.Call("get_enemy_ai_enabled"));
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

    public void OnEnemyAiToggled(bool enabled)
    {
        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

        if (!enabled)
        {
            simNode.ConfigureAi(1, AiType.None);
            return;
        }

        simNode.ConfigureAi(
            1,
            AiType.Heuristic,
            AiPersonality.Balanced
        );
    }

    public void ClearAllUnits()
    {
        int count = 0;

        // Clear simulation source-of-truth first so visuals don't immediately respawn.
        var simNode = SimulationNode.Current;
        if (simNode != null)
        {
            var state = simNode.GetState();
            count = state.Units.Count;
            state.Units.Clear();
            state.Projectiles.Clear();
        }

        var entityManager = GetNodeOrNull<EntityManager>("EntityManager");
        if (entityManager != null)
        {
            foreach (var child in entityManager.GetChildren())
            {
                if (child is UnitVisual || child is ProjectileVisual)
                    child.QueueFree();
            }
        }

        var units = GetTree().GetNodesInGroup(GroupIDs.Units);
        foreach (var unit in units)
        {
            if (IsInstanceValid(unit))
                unit.QueueFree();
        }

        EmitSignal(SignalName.UnitsCleared, count);
    }
}
