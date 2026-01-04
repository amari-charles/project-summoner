namespace ProjectSummoner.Cards.Effects.Core;

/// <summary>
/// Core interface for all spell effects.
/// Spell effects define what happens when a spell is cast.
/// </summary>
public interface ISpellEffect
{
    /// <summary>
    /// Delay before this effect executes (seconds).
    /// Used for sequencing effects in composite spells.
    /// </summary>
    float Delay { get; }

    /// <summary>
    /// Execute the spell effect with the given context.
    /// </summary>
    /// <param name="context">Context containing position, team, battlefield, etc.</param>
    void Execute(SpellContext context);
}
