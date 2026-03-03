using Godot;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;

namespace Fateforged.Input;

/// <summary>
/// Captures player gestures and converts them into Commands.
/// Doesn't validate, doesn't execute — just packages what the player wants
/// to do and calls IGameSession.SubmitCommand().
/// </summary>
public partial class InputCollector : Node
{
    private IGameSession? _session;

    // Drag state (read by View components)
    public bool IsDragging { get; private set; }
    public int DraggedCardIndex { get; private set; } = -1;
    public Vector3 DragPosition { get; private set; }

    // Spell targeting state (read by View components)
    public bool IsTargeting { get; private set; }
    public Vector3 SpellTargetPosition { get; private set; }
    public float SpellTargetRadius { get; private set; }

    public void Initialize(IGameSession session)
    {
        _session = session;
    }

    /// <summary>
    /// Card dragged and dropped onto the battlefield.
    /// Produces a PlayCardCommand and submits it.
    /// </summary>
    public void OnCardDropped(int cardIndex, Vector3 position)
    {
        if (_session == null) return;
        var simPos = new SimVector3(position.X, position.Y, position.Z);
        _session.SubmitCommand(new PlayCardCommand(0, cardIndex, simPos));
    }

    /// <summary>
    /// Spell targeting cursor confirmed.
    /// Produces a PlayCardCommand with target info and submits it.
    /// </summary>
    public void OnSpellTargetConfirmed(int cardIndex, Vector3 position, int? targetUnitId)
    {
        if (_session == null) return;
        var simPos = new SimVector3(position.X, position.Y, position.Z);
        var cmd = new PlayCardCommand(0, cardIndex, simPos) { TargetUnitId = targetUnitId };
        _session.SubmitCommand(cmd);
    }

    /// <summary>
    /// Player requested to forfeit the match.
    /// Produces a ForfeitCommand and submits it.
    /// </summary>
    public void OnForfeitRequested()
    {
        if (_session == null) return;
        _session.SubmitCommand(new ForfeitCommand(0));
    }

    // =========================================================================
    // Drag state management (called by UI)
    // =========================================================================

    public void OnDragStarted(int cardIndex)
    {
        IsDragging = true;
        DraggedCardIndex = cardIndex;
    }

    public void OnDragMoved(Vector3 position)
    {
        DragPosition = position;
    }

    public void OnDragEnded()
    {
        IsDragging = false;
        DraggedCardIndex = -1;
        DragPosition = Vector3.Zero;
    }
}
