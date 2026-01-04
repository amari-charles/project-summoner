using Godot;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Conditions;

/// <summary>
/// Interface for spell conditions.
/// Conditions determine whether a spell effect should execute or branch.
/// Used for conditional spells (e.g., "if target HP &lt; 50%, deal double damage").
/// </summary>
public interface ISpellCondition
{
    /// <summary>
    /// Evaluate whether the condition is met.
    /// </summary>
    /// <param name="context">Spell context with position, team, etc.</param>
    /// <param name="target">Optional specific target for per-target conditions.</param>
    /// <returns>True if condition is met.</returns>
    bool Evaluate(SpellContext context, Node3D? target = null);
}
