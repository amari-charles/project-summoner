using System.Collections.Generic;
using Godot;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Filters;

/// <summary>
/// Interface for filtering spell targets.
/// Filters refine the target selection beyond basic affinity (e.g., only targets below 50% HP).
/// </summary>
public interface ITargetFilter
{
    /// <summary>
    /// Filter a collection of targets based on this filter's criteria.
    /// </summary>
    /// <param name="targets">Targets to filter.</param>
    /// <param name="context">Spell context for access to battlefield state.</param>
    /// <returns>Filtered subset of targets.</returns>
    IEnumerable<Node3D> Filter(IEnumerable<Node3D> targets, SpellContext context);
}
