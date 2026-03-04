namespace Fateforged.Simulation.Commands;

/// <summary>
/// Command to forfeit the match.
/// Processed by Simulation.Tick() → sets winner to opposing team, transitions to GameOver.
/// </summary>
public class ForfeitCommand : ICommand
{
    public int Team { get; }
    public long ExecuteFrame { get; set; }

    public ForfeitCommand(int team)
    {
        Team = team;
    }
}
