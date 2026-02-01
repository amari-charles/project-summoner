namespace ProjectSummoner.Cards.Spawning;

/// <summary>
/// Defines how units in a spawn entry are positioned relative to the spawn point.
/// </summary>
public enum SpawnPlacement
{
    /// <summary>
    /// Use the card's formation strategy to position units.
    /// Units are placed in a formation centered on the spawn position.
    /// </summary>
    Formation,

    /// <summary>
    /// Spawn behind the leader unit (entry index 0).
    /// Used for companion units that follow a leader (e.g., ducklings behind mama).
    /// Direction is based on team: player team spawns behind = -X, enemy = +X.
    /// </summary>
    BehindLeader,

    /// <summary>
    /// Spawn around the leader unit (entry index 0).
    /// Units are spread in a circle around the leader position.
    /// </summary>
    AroundLeader
}
