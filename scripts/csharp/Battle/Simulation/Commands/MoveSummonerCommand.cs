namespace Fateforged.Simulation.Commands;

/// <summary>
/// Requests an authoritative summoner position change.
/// Initially used by the compact-room skirmish prototype so movement can be
/// tested without letting the view mutate MatchState directly.
/// </summary>
public sealed class MoveSummonerCommand : ICommand
{
    public int Team { get; }
    public SimVector3 TargetPosition { get; }
    public long ExecuteFrame { get; set; }

    public MoveSummonerCommand(int team, SimVector3 targetPosition)
    {
        Team = team;
        TargetPosition = targetPosition;
    }
}
