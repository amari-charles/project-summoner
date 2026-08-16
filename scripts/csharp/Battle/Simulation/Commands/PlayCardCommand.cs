namespace Fateforged.Simulation.Commands;

/// <summary>
/// Command to play a card from a summoner's hand.
/// Processed by Simulation.Tick() → validates, deducts mana, moves card to discard,
/// starts casting, draws replacement.
///
/// For summon cards: SpawnPosition is where units appear.
/// For spell cards: SpawnPosition is the target position (AoE center).
///   TargetUnitId is optionally set for single-target spells.
/// </summary>
public class PlayCardCommand : ICommand
{
    public int Team { get; }
    public int CardIndex { get; }
    public SimVector3 SpawnPosition { get; set; }
    public int NetworkId { get; }
    public int? TargetUnitId { get; set; }
    public int Sequence { get; set; }
    public long IssuedFrame { get; set; }
    public long ExecuteFrame { get; set; }

    public PlayCardCommand(int team, int cardIndex, SimVector3 spawnPosition, int networkId = -1)
    {
        Team = team;
        CardIndex = cardIndex;
        SpawnPosition = spawnPosition;
        NetworkId = networkId;
    }
}
