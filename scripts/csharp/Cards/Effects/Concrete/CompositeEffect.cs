using System.Collections.Generic;
using Godot;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Concrete;

/// <summary>
/// Spell effect that sequences multiple sub-effects with optional timing delays.
/// Use for complex spells like "freeze enemies for 2 seconds, then heal allies".
/// </summary>
public class CompositeEffect : SpellEffect
{
    /// <summary>
    /// List of effects to execute in sequence.
    /// Each effect's Delay property controls when it starts relative to the previous effect.
    /// </summary>
    public List<ISpellEffect> Effects { get; set; } = new();

    public override void Execute(SpellContext context)
    {
        if (Effects == null || Effects.Count == 0)
        {
            ExecuteOnComplete(context);
            return;
        }

        // Execute effects with delays using coroutine-style pattern
        ExecuteEffectsSequentially(context);
    }

    /// <summary>
    /// Execute effects in sequence, respecting delay between each.
    /// Uses fire-and-forget async for timing but caller doesn't need to await.
    /// </summary>
    private async void ExecuteEffectsSequentially(SpellContext context)
    {
        foreach (var effect in Effects)
        {
            // Handle delay if specified
            if (effect.Delay > 0 && context.SceneTree != null)
            {
                var timer = context.SceneTree.CreateTimer(effect.Delay);
                await context.SceneTree.ToSignal(timer, "timeout");
            }

            effect.Execute(context);
        }

        ExecuteOnComplete(context);
    }
}
