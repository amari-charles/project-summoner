namespace ProjectSummoner.Cards.Effects.Core;

/// <summary>
/// Determines which units a spell effect targets based on team relationship.
/// </summary>
public enum Affinity
{
    /// <summary>Only affects enemy units (relative to caster's team).</summary>
    Enemies,

    /// <summary>Only affects allied units (same team as caster).</summary>
    Allies,

    /// <summary>Affects all units regardless of team.</summary>
    Both
}
