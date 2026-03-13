using Fateforged.Cards;
using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Units;
using Fateforged.View;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fateforged.View.Debug;

/// <summary>
/// Debug Arena — infinite mana/HP, manual unit spawning, enemy AI toggle.
/// Replaces debug_arena_controller.gd. Used by scenes/battle/battlefield/dev/debug_arena.tscn.
/// </summary>
[GlobalClass]
public partial class DebugArenaScene : TestBattleScene
{
    [Signal]
    public delegate void UnitsClearedEventHandler(int count);
    [Signal]
    public delegate void SpawnLoggedEventHandler(string message);

    private readonly List<SpawnBatch> _spawnHistory = new();
    private const string DebugDeckPath = "res://data/debug/debug_deck.json";
    private const int TeamPlayer = 0;
    private const int TeamEnemy = 1;

    private Node? _spawnerPanel;

    private sealed record SpawnBatch(int Team, int ExpectedUnitCount, string Label);

    protected override Godot.Collections.Dictionary BuildPracticeConfig()
    {
        var playerDeck = LoadDebugDeck();
        var enemyDeck = (Godot.Collections.Array)playerDeck.Duplicate(true);

        return new Godot.Collections.Dictionary
        {
            { "dev_player_deck", playerDeck },
            { "enemy_deck", enemyDeck },
            { "enemy_hp", 999999.0 },
            { "ai_type", "none" },
        };
    }

    private static Godot.Collections.Array LoadDebugDeck()
    {
        if (!FileAccess.FileExists(DebugDeckPath))
        {
            GD.PushWarning(
                $"[DebugArenaScene] Debug deck not found at {DebugDeckPath}, using all catalog summons"
            );
            return BuildFallbackDeckFromCatalogSummons();
        }

        using var file = FileAccess.Open(DebugDeckPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning(
                "[DebugArenaScene] Failed to open debug deck file, using all catalog summons"
            );
            return BuildFallbackDeckFromCatalogSummons();
        }

        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType == Variant.Type.Array)
        {
            var deck = parsed.AsGodotArray();
            if (deck.Count > 0)
                return deck;
        }

        GD.PushWarning(
            "[DebugArenaScene] Debug deck JSON invalid/empty, using all catalog summons"
        );
        return BuildFallbackDeckFromCatalogSummons();
    }

    private static Godot.Collections.Array BuildFallbackDeckFromCatalogSummons()
    {
        var entries = new Godot.Collections.Array();
        foreach (var cardDef in CardCatalog.GetAllCardsAsDict())
        {
            if (
                !cardDef.TryGetValue("card_type", out var cardTypeVar)
                || cardTypeVar.AsInt32() != (int)CardType.Summon
            )
                continue;

            string catalogId = cardDef.TryGetValue("catalog_id", out var catalogIdVar)
                ? catalogIdVar.AsString()
                : "";
            entries.Add(
                new Godot.Collections.Dictionary { { "catalog_id", catalogId }, { "count", 1 } }
            );
        }

        return entries;
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
        if (_spawnerPanel == null)
            return;

        if (
            !_spawnerPanel.IsConnected(
                "clear_requested",
                new Callable(this, MethodName.ClearAllUnits)
            )
        )
            _spawnerPanel.Connect("clear_requested", new Callable(this, MethodName.ClearAllUnits));

        if (
            !_spawnerPanel.IsConnected(
                "skip_prep_toggled",
                new Callable(this, MethodName.OnSkipPrepToggled)
            )
        )
            _spawnerPanel.Connect(
                "skip_prep_toggled",
                new Callable(this, MethodName.OnSkipPrepToggled)
            );

        if (
            _spawnerPanel.HasSignal("enemy_ai_toggled")
            && !_spawnerPanel.IsConnected(
                "enemy_ai_toggled",
                new Callable(this, MethodName.OnEnemyAiToggled)
            )
        )
        {
            _spawnerPanel.Connect(
                "enemy_ai_toggled",
                new Callable(this, MethodName.OnEnemyAiToggled)
            );
        }

        if (
            _spawnerPanel.HasSignal("player_ai_toggled")
            && !_spawnerPanel.IsConnected(
                "player_ai_toggled",
                new Callable(this, MethodName.OnPlayerAiToggled)
            )
        )
        {
            _spawnerPanel.Connect(
                "player_ai_toggled",
                new Callable(this, MethodName.OnPlayerAiToggled)
            );
        }

        if (
            _spawnerPanel.HasSignal("clear_team_requested")
            && !_spawnerPanel.IsConnected(
                "clear_team_requested",
                new Callable(this, MethodName.ClearTeamUnits)
            )
        )
        {
            _spawnerPanel.Connect(
                "clear_team_requested",
                new Callable(this, MethodName.ClearTeamUnits)
            );
        }

        if (
            _spawnerPanel.HasSignal("undo_requested")
            && !_spawnerPanel.IsConnected("undo_requested", new Callable(this, MethodName.UndoLastSpawnBatch))
        )
        {
            _spawnerPanel.Connect("undo_requested", new Callable(this, MethodName.UndoLastSpawnBatch));
        }

        if (_spawnerPanel.HasMethod("get_enemy_ai_enabled"))
            OnEnemyAiToggled((bool)_spawnerPanel.Call("get_enemy_ai_enabled"));
        if (_spawnerPanel.HasMethod("get_player_ai_enabled"))
            OnPlayerAiToggled((bool)_spawnerPanel.Call("get_player_ai_enabled"));
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
            if (found != null)
                return found;
        }

        // Search direct children
        foreach (var child in GetChildren())
        {
            var found = FindChildWithMethod(child, "get_skip_prep_phase");
            if (found != null)
                return found;
        }

        return null;
    }

    private static Node? FindChildWithMethod(Node node, string method)
    {
        if (node.HasMethod(method))
            return node;
        foreach (var child in node.GetChildren())
        {
            var found = FindChildWithMethod(child, method);
            if (found != null)
                return found;
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
        ConfigureTeamAi(TeamEnemy, enabled);
    }

    public void OnPlayerAiToggled(bool enabled)
    {
        ConfigureTeamAi(TeamPlayer, enabled);
    }

    private static void ConfigureTeamAi(int team, bool enabled)
    {
        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

        if (!enabled)
        {
            simNode.ConfigureAi(team, AiType.None);
            return;
        }

        simNode.ConfigureAi(team, AiType.Heuristic, AiPersonality.Balanced);
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
        _spawnHistory.Clear();
        AppendSpawnLog("Cleared all units");
    }

    public void ClearTeamUnits(int team)
    {
        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

        int targetTeam = team == TeamPlayer ? TeamPlayer : TeamEnemy;
        var targetTeamEnum = (Team)targetTeam;
        var state = simNode.GetState();

        var unitIdsToRemove = state
            .Units.Values.Where(unit => unit.Team == targetTeamEnum)
            .Select(unit => unit.UnitId)
            .ToList();

        foreach (int unitId in unitIdsToRemove)
            state.Units.Remove(unitId);

        var projectileIdsToRemove = state
            .Projectiles.Where(kvp => kvp.Value.Team == targetTeamEnum)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (int projectileId in projectileIdsToRemove)
            state.Projectiles.Remove(projectileId);

        foreach (var node in GetTree().GetNodesInGroup(targetTeam == TeamPlayer ? GroupIDs.PlayerUnits : GroupIDs.EnemyUnits))
        {
            if (node is UnitVisual visual && unitIdsToRemove.Contains(visual.UnitId))
                visual.QueueFree();
        }

        AppendSpawnLog(
            $"Cleared {(targetTeam == TeamPlayer ? "player" : "enemy")} team units ({unitIdsToRemove.Count})"
        );
    }

    public void RegisterDebugSpawnBatch(int team, int expectedUnitCount, string label)
    {
        int normalizedTeam = team == TeamPlayer ? TeamPlayer : TeamEnemy;
        string safeLabel = string.IsNullOrEmpty(label) ? "Unknown" : label;

        _spawnHistory.Add(new SpawnBatch(normalizedTeam, Math.Max(expectedUnitCount, 0), safeLabel));

        string side = normalizedTeam == TeamPlayer ? "Player" : "Enemy";
        if (expectedUnitCount > 0)
        {
            AppendSpawnLog($"Spawned {safeLabel} ({side}) x{expectedUnitCount}");
        }
        else
        {
            AppendSpawnLog($"Cast {safeLabel} ({side})");
        }
    }

    public void UndoLastSpawnBatch()
    {
        if (_spawnHistory.Count == 0)
        {
            AppendSpawnLog("Undo ignored: no spawn history");
            return;
        }

        var batch = _spawnHistory[^1];
        _spawnHistory.RemoveAt(_spawnHistory.Count - 1);

        if (batch.ExpectedUnitCount <= 0)
        {
            AppendSpawnLog($"Undo skipped for '{batch.Label}' (no unit batch)");
            return;
        }

        var simNode = SimulationNode.Current;
        if (simNode == null)
            return;

        var state = simNode.GetState();
        var targetTeamEnum = (Team)batch.Team;
        var recentUnitIds = state
            .Units.Values.Where(unit => unit.Team == targetTeamEnum)
            .OrderByDescending(unit => unit.UnitId)
            .Take(batch.ExpectedUnitCount)
            .Select(unit => unit.UnitId)
            .ToList();

        foreach (int unitId in recentUnitIds)
            state.Units.Remove(unitId);

        foreach (var node in GetTree().GetNodesInGroup(batch.Team == TeamPlayer ? GroupIDs.PlayerUnits : GroupIDs.EnemyUnits))
        {
            if (node is UnitVisual visual && recentUnitIds.Contains(visual.UnitId))
                visual.QueueFree();
        }

        AppendSpawnLog($"Undo: removed {recentUnitIds.Count} unit(s) from '{batch.Label}'");
    }

    private void AppendSpawnLog(string message)
    {
        if (_spawnerPanel != null && _spawnerPanel.HasMethod("append_spawn_log"))
            _spawnerPanel.Call("append_spawn_log", message);
        EmitSignal(SignalName.SpawnLogged, message);
    }
}
