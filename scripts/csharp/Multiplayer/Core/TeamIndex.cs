namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Team index in local perspective (0=PLAYER, 1=ENEMY).
/// Used by GDScript nodes and SimulationNode's public API.
/// Must be explicitly converted to NetworkTeam for MatchState/protocol operations.
/// </summary>
public readonly record struct LocalTeam(int Value)
{
    public override string ToString() => Value == 0 ? "Local(PLAYER)" : "Local(ENEMY)";
}

/// <summary>
/// Team index in network perspective (0=host/player1, 1=client/player2).
/// Used by MatchState, protocol messages, and snapshots.
/// Must be explicitly converted to LocalTeam for GDScript/signal operations.
/// </summary>
public readonly record struct NetworkTeam(int Value)
{
    public override string ToString() => $"Network({Value})";
}
