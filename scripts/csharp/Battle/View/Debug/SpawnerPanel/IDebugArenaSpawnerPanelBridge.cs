using Godot;

namespace Fateforged.View.Debug.SpawnerPanel;

public interface IDebugArenaSpawnerPanelBridge
{
    Node PanelNode { get; }

    bool ConnectClearRequested(Callable handler);

    bool ConnectSkipPrepToggled(Callable handler);

    bool ConnectEnemyAiToggled(Callable handler);

    bool ConnectPlayerAiToggled(Callable handler);

    bool ConnectPlayerHoldAdvanceToggled(Callable handler);

    bool ConnectClearTeamRequested(Callable handler);

    bool ConnectUndoRequested(Callable handler);

    bool GetSkipPrepPhase();

    bool GetEnemyAiEnabled();

    bool GetPlayerAiEnabled();

    bool GetPlayerHoldAdvanceEnabled();

    void AppendSpawnLog(string message);

    void SetDebugDeckEntries(Godot.Collections.Array deckEntries);
}
