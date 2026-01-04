using Godot;
using ProjectSummoner.Cards.Effects.Conditions;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Concrete;

/// <summary>
/// Spell effect that branches based on a condition.
/// Use for spells like "if target HP < 30%, deal double damage".
/// </summary>
public class ConditionalEffect : SpellEffect
{
    /// <summary>
    /// Condition to evaluate.
    /// </summary>
    public ISpellCondition? Condition { get; set; }

    /// <summary>
    /// Effect to execute if condition is true.
    /// </summary>
    public ISpellEffect? ThenEffect { get; set; }

    /// <summary>
    /// Effect to execute if condition is false (optional).
    /// </summary>
    public ISpellEffect? ElseEffect { get; set; }

    public override void Execute(SpellContext context)
    {
        bool conditionMet = Condition?.Evaluate(context) ?? false;

        if (conditionMet)
        {
            ThenEffect?.Execute(context);
        }
        else
        {
            ElseEffect?.Execute(context);
        }

        ExecuteOnComplete(context);
    }
}
