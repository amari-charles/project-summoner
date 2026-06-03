using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Movement;
using Fateforged.Units;
using Fateforged.View;
using Fateforged.View.Debug.DeckSources;
using Fateforged.View.Debug.SpawnerPanel;
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
    private const int TeamPlayer = 0;
    private const int TeamEnemy = 1;

    private IDebugArenaSpawnerPanelBridge? _spawnerPanelBridge;
    private IDebugArenaDeckProvider? _deckProvider;
    private DebugArenaDeckResolution? _lastDeckResolution;

    private sealed record SpawnBatch(int Team, int ExpectedUnitCount, string Label, int[] SpawnedUnitIds);

    protected override Godot.Collections.Dictionary BuildPracticeConfig()
    {
        var deckResolution = ResolveDeckResolution();
        _lastDeckResolution = deckResolution;

        return new Godot.Collections.Dictionary
        {
            { "dev_player_deck", deckResolution.PlayerDeck },
            { "enemy_deck", deckResolution.EnemyDeck },
            { "enemy_hp", 999999.0 },
            { "ai_type", "none" },
        };
    }

    protected virtual IDebugArenaDeckProvider CreateDeckProvider()
    {
        return new DebugArenaDeckProvider();
    }

    protected virtual IDebugArenaSpawnerPanelBridge? ResolveSpawnerPanelBridge()
    {
        return DebugArenaSpawnerPanelBridgeFactory.TryCreate(this);
    }

    public override async void _Ready()
    {
        base._Ready();

        // Wait one frame for init to complete, then connect spawner panel
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ConnectSpawnerPanel();

        // Check if we should skip prep phase
        if (_spawnerPanelBridge != null && _spawnerPanelBridge.GetSkipPrepPhase())
            SkipPrepPhase();
    }

    protected virtual void ConnectSpawnerPanel()
    {
        _spawnerPanelBridge = ResolveSpawnerPanelBridge();
        if (_spawnerPanelBridge == null)
            return;

        _spawnerPanelBridge.ConnectClearRequested(new Callable(this, MethodName.ClearAllUnits));
        _spawnerPanelBridge.ConnectSkipPrepToggled(new Callable(this, MethodName.OnSkipPrepToggled));
        _spawnerPanelBridge.ConnectEnemyAiToggled(new Callable(this, MethodName.OnEnemyAiToggled));
        _spawnerPanelBridge.ConnectPlayerAiToggled(new Callable(this, MethodName.OnPlayerAiToggled));
        _spawnerPanelBridge.ConnectPlayerHoldAdvanceToggled(
            new Callable(this, MethodName.OnPlayerHoldAdvanceToggled)
        );
        _spawnerPanelBridge.ConnectClearTeamRequested(new Callable(this, MethodName.ClearTeamUnits));
        _spawnerPanelBridge.ConnectUndoRequested(new Callable(this, MethodName.UndoLastSpawnBatch));
        SyncSpawnerPanelDeckEntries();

        OnEnemyAiToggled(_spawnerPanelBridge.GetEnemyAiEnabled());
        OnPlayerAiToggled(_spawnerPanelBridge.GetPlayerAiEnabled());
        OnPlayerHoldAdvanceToggled(_spawnerPanelBridge.GetPlayerHoldAdvanceEnabled());
    }

    public override void _ExitTree()
    {
        SimMovement.DebugHoldPlayerAdvanceEnabled = false;
        base._ExitTree();
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

    public void OnPlayerHoldAdvanceToggled(bool enabled)
    {
        SimMovement.DebugHoldPlayerAdvanceEnabled = enabled;
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

    public void RegisterDebugSpawnBatch(
        int team,
        int expectedUnitCount,
        string label,
        int[]? spawnedUnitIds = null
    )
    {
        int normalizedTeam = team == TeamPlayer ? TeamPlayer : TeamEnemy;
        string safeLabel = string.IsNullOrEmpty(label) ? "Unknown" : label;
        int[] safeSpawnedUnitIds = (spawnedUnitIds ?? Array.Empty<int>())
            .Where(unitId => unitId > 0)
            .Distinct()
            .ToArray();

        _spawnHistory.Add(
            new SpawnBatch(
                normalizedTeam,
                Math.Max(expectedUnitCount, 0),
                safeLabel,
                safeSpawnedUnitIds
            )
        );

        string side = normalizedTeam == TeamPlayer ? "Player" : "Enemy";
        int loggedCount = safeSpawnedUnitIds.Length > 0 ? safeSpawnedUnitIds.Length : expectedUnitCount;
        if (loggedCount > 0)
        {
            AppendSpawnLog($"Spawned {safeLabel} ({side}) x{loggedCount}");
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
        List<int> recentUnitIds;
        if (batch.SpawnedUnitIds.Length > 0)
        {
            recentUnitIds = batch
                .SpawnedUnitIds.Where(unitId => state.Units.ContainsKey(unitId))
                .ToList();
        }
        else
        {
            var targetTeamEnum = (Team)batch.Team;
            recentUnitIds = state
                .Units.Values.Where(unit => unit.Team == targetTeamEnum)
                .OrderByDescending(unit => unit.UnitId)
                .Take(batch.ExpectedUnitCount)
                .Select(unit => unit.UnitId)
                .ToList();
        }

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
        _spawnerPanelBridge?.AppendSpawnLog(message);
        EmitSignal(SignalName.SpawnLogged, message);
    }

    private IDebugArenaDeckProvider DeckProvider => _deckProvider ??= CreateDeckProvider();

    private DebugArenaDeckResolution ResolveDeckResolution()
    {
        var contextConfig = ReadBattleContextConfig();
        var sourceMode = DebugArenaDeckSourceModeResolver.ResolveFromConfig(contextConfig);
        return DeckProvider.Resolve(
            new DebugArenaDeckResolveRequest
            {
                SourceMode = sourceMode,
                ContextConfig = contextConfig,
            }
        );
    }

    private void SyncSpawnerPanelDeckEntries()
    {
        if (_spawnerPanelBridge == null)
            return;

        var resolution = _lastDeckResolution ?? ResolveDeckResolution();
        _lastDeckResolution = resolution;

        if (resolution.PlayerDeck.Count == 0)
            return;

        _spawnerPanelBridge.SetDebugDeckEntries((Godot.Collections.Array)resolution.PlayerDeck.Duplicate(true));
    }

    protected virtual Godot.Collections.Dictionary ReadBattleContextConfig()
    {
        var root = GetTree()?.Root;
        var battleContext = root?.GetNodeOrNull("BattleContext");
        if (battleContext == null)
            return new Godot.Collections.Dictionary();

        var configVar = battleContext.Get("battle_config");
        if (configVar.VariantType != Variant.Type.Dictionary)
            return new Godot.Collections.Dictionary();

        return configVar.AsGodotDictionary();
    }
}
