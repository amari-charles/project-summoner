using Godot;
using Fateforged.Session;

namespace Fateforged.Input;

/// <summary>
/// Captures player gestures and converts them into Commands.
/// Doesn't validate, doesn't execute — just packages what the player wants
/// to do and calls IGameSession.SubmitCommand().
///
/// Today this is scattered across HandUI, BattlefieldDropZone,
/// Summoner.play_card_3d(), SimulationNode.QueuePlayCard(), and
/// SpellTargetingManager. This consolidates Command-production into one place.
/// </summary>
public partial class InputCollector : Node
{
    private IGameSession? _session;

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
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Spell targeting cursor confirmed.
    /// Produces a PlayCardCommand with target info and submits it.
    /// </summary>
    public void OnSpellTargetConfirmed(int cardIndex, Vector3 position, int? targetUnitId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Player requested to forfeit the match.
    /// Produces a ForfeitCommand and submits it.
    /// </summary>
    public void OnForfeitRequested()
    {
        throw new System.NotImplementedException();
    }
}
