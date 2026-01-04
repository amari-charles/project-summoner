using System.Collections.Generic;
using Godot;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Targeting;

/// <summary>
/// Interface for spell targeting strategies.
/// Determines which units are affected by a spell based on area, shape, or other criteria.
/// </summary>
public interface ITargetingStrategy
{
    /// <summary>
    /// Select all potential targets based on this targeting strategy.
    /// Does not filter by team affinity - that's handled separately.
    /// </summary>
    /// <param name="context">Spell context containing position and battlefield reference.</param>
    /// <returns>All units within the targeting area.</returns>
    IEnumerable<Node3D> SelectTargets(SpellContext context);
}
