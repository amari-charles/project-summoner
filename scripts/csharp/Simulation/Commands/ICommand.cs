namespace Fateforged.Simulation;

/// <summary>
/// Interface for commands that mutate MatchState through Simulation.Tick().
/// Only player-initiated actions are commands (card plays, forfeit).
/// Damage is a consequence of unit behavior, not a command.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The frame at which this command should be executed.
    /// Commands with ExecuteFrame <= current FrameNumber are drained and processed.
    /// </summary>
    long ExecuteFrame { get; set; }
}
