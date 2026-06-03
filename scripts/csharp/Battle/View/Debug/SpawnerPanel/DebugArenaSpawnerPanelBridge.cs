using Godot;

namespace Fateforged.View.Debug.SpawnerPanel;

public sealed class DebugArenaSpawnerPanelBridge : IDebugArenaSpawnerPanelBridge
{
    private const string SignalClearRequested = "clear_requested";
    private const string SignalSkipPrepToggled = "skip_prep_toggled";
    private const string SignalEnemyAiToggled = "enemy_ai_toggled";
    private const string SignalPlayerAiToggled = "player_ai_toggled";
    private const string SignalPlayerHoldAdvanceToggled = "player_hold_advance_toggled";
    private const string SignalClearTeamRequested = "clear_team_requested";
    private const string SignalUndoRequested = "undo_requested";

    private const string MethodGetSkipPrepPhase = "get_skip_prep_phase";
    private const string MethodGetEnemyAiEnabled = "get_enemy_ai_enabled";
    private const string MethodGetPlayerAiEnabled = "get_player_ai_enabled";
    private const string MethodGetPlayerHoldAdvanceEnabled = "get_player_hold_advance_enabled";
    private const string MethodAppendSpawnLog = "append_spawn_log";
    private const string MethodSetDebugDeckEntries = "set_debug_deck_entries";

    public Node PanelNode { get; }

    public DebugArenaSpawnerPanelBridge(Node panelNode)
    {
        PanelNode = panelNode;
    }

    public bool ConnectClearRequested(Callable handler)
    {
        return ConnectSignal(SignalClearRequested, handler, required: true);
    }

    public bool ConnectSkipPrepToggled(Callable handler)
    {
        return ConnectSignal(SignalSkipPrepToggled, handler, required: true);
    }

    public bool ConnectEnemyAiToggled(Callable handler)
    {
        return ConnectSignal(SignalEnemyAiToggled, handler, required: false);
    }

    public bool ConnectPlayerAiToggled(Callable handler)
    {
        return ConnectSignal(SignalPlayerAiToggled, handler, required: false);
    }

    public bool ConnectPlayerHoldAdvanceToggled(Callable handler)
    {
        return ConnectSignal(SignalPlayerHoldAdvanceToggled, handler, required: false);
    }

    public bool ConnectClearTeamRequested(Callable handler)
    {
        return ConnectSignal(SignalClearTeamRequested, handler, required: false);
    }

    public bool ConnectUndoRequested(Callable handler)
    {
        return ConnectSignal(SignalUndoRequested, handler, required: false);
    }

    public bool GetSkipPrepPhase()
    {
        return CallBoolMethod(MethodGetSkipPrepPhase);
    }

    public bool GetEnemyAiEnabled()
    {
        return CallBoolMethod(MethodGetEnemyAiEnabled);
    }

    public bool GetPlayerAiEnabled()
    {
        return CallBoolMethod(MethodGetPlayerAiEnabled);
    }

    public bool GetPlayerHoldAdvanceEnabled()
    {
        return CallBoolMethod(MethodGetPlayerHoldAdvanceEnabled);
    }

    public void AppendSpawnLog(string message)
    {
        if (!PanelNode.HasMethod(MethodAppendSpawnLog))
            return;

        PanelNode.Call(MethodAppendSpawnLog, message);
    }

    public void SetDebugDeckEntries(Godot.Collections.Array deckEntries)
    {
        if (!PanelNode.HasMethod(MethodSetDebugDeckEntries))
            return;

        PanelNode.Call(MethodSetDebugDeckEntries, deckEntries);
    }

    private bool ConnectSignal(string signal, Callable handler, bool required)
    {
        if (!PanelNode.HasSignal(signal))
        {
            if (required)
                GD.PushWarning($"[DebugArenaSpawnerPanelBridge] Missing required signal '{signal}'");
            return false;
        }

        if (!PanelNode.IsConnected(signal, handler))
            PanelNode.Connect(signal, handler);
        return true;
    }

    private bool CallBoolMethod(string method)
    {
        if (!PanelNode.HasMethod(method))
            return false;

        var result = PanelNode.Call(method);
        return result.VariantType == Variant.Type.Bool && result.AsBool();
    }
}
