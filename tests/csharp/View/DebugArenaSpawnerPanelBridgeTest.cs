namespace Fateforged.Tests.View;

using Fateforged.View.Debug.SpawnerPanel;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class DebugArenaSpawnerPanelBridgeTest
{
    [TestCase]
    public void ConnectsSignalsThroughTypedBridge()
    {
        var panel = new SpawnerPanelStub();
        var bridge = new DebugArenaSpawnerPanelBridge(panel);

        bool clearCalled = false;
        bool skipCalled = false;
        bool enemyAiCalled = false;
        bool playerAiCalled = false;
        bool clearTeamCalled = false;
        bool undoCalled = false;

        bridge.ConnectClearRequested(Callable.From(() => clearCalled = true));
        bridge.ConnectSkipPrepToggled(Callable.From<bool>(_ => skipCalled = true));
        bridge.ConnectEnemyAiToggled(Callable.From<bool>(_ => enemyAiCalled = true));
        bridge.ConnectPlayerAiToggled(Callable.From<bool>(_ => playerAiCalled = true));
        bridge.ConnectClearTeamRequested(Callable.From<int>(_ => clearTeamCalled = true));
        bridge.ConnectUndoRequested(Callable.From(() => undoCalled = true));

        panel.EmitSignal("clear_requested");
        panel.EmitSignal("skip_prep_toggled", true);
        panel.EmitSignal("enemy_ai_toggled", true);
        panel.EmitSignal("player_ai_toggled", true);
        panel.EmitSignal("clear_team_requested", 1);
        panel.EmitSignal("undo_requested");

        AssertThat(clearCalled).IsTrue();
        AssertThat(skipCalled).IsTrue();
        AssertThat(enemyAiCalled).IsTrue();
        AssertThat(playerAiCalled).IsTrue();
        AssertThat(clearTeamCalled).IsTrue();
        AssertThat(undoCalled).IsTrue();
    }

    [TestCase]
    public void ReadsToggleStateAndAppendsLogThroughTypedMethods()
    {
        var panel = new SpawnerPanelStub
        {
            SkipPrepPhase = true,
            EnemyAiEnabled = true,
            PlayerAiEnabled = false,
        };
        var bridge = new DebugArenaSpawnerPanelBridge(panel);

        AssertThat(bridge.GetSkipPrepPhase()).IsTrue();
        AssertThat(bridge.GetEnemyAiEnabled()).IsTrue();
        AssertThat(bridge.GetPlayerAiEnabled()).IsFalse();

        bridge.AppendSpawnLog("hello");
        AssertThat(panel.LastLog).IsEqual("hello");

        var deckEntries = new Godot.Collections.Array
        {
            new Godot.Collections.Dictionary { { "catalog_id", "fire_wisp" }, { "count", 2 } },
        };
        bridge.SetDebugDeckEntries(deckEntries);
        AssertThat(panel.LastDeckEntries.Count).IsEqual(1);
    }

    private sealed partial class SpawnerPanelStub : Node
    {
        public bool SkipPrepPhase { get; set; }
        public bool EnemyAiEnabled { get; set; }
        public bool PlayerAiEnabled { get; set; }
        public string LastLog { get; private set; } = "";
        public Godot.Collections.Array LastDeckEntries { get; private set; } = new();

        public SpawnerPanelStub()
        {
            AddUserSignal("clear_requested");
            AddUserSignal("skip_prep_toggled");
            AddUserSignal("enemy_ai_toggled");
            AddUserSignal("player_ai_toggled");
            AddUserSignal("clear_team_requested");
            AddUserSignal("undo_requested");
        }

        public bool get_skip_prep_phase() => SkipPrepPhase;

        public bool get_enemy_ai_enabled() => EnemyAiEnabled;

        public bool get_player_ai_enabled() => PlayerAiEnabled;

        public void append_spawn_log(string message)
        {
            LastLog = message;
        }

        public void set_debug_deck_entries(Godot.Collections.Array entries)
        {
            LastDeckEntries = (Godot.Collections.Array)entries.Duplicate(true);
        }
    }
}
